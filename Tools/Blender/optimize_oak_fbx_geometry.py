from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parent
TREE_ROOT = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees"
SOURCE_FBX_DIR = TREE_ROOT / "Exports" / "FBX"
OPTIMIZED_FBX_DIR = TREE_ROOT / "OptimizedFBX"
PREVIEW_DIR = TREE_ROOT / "Previews"

TREE_NAMES = [
    "tree_reference_oak_broad_01",
    "tree_reference_oak_tall_01",
    "tree_reference_oak_core_01",
    "tree_reference_oak_low_01",
    "tree_reference_oak_slim_01",
]

LEAF_DECIMATE_RATIO = 0.72
INTERNAL_LEAF_RADIUS = 0.90


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def mesh_stats(objects):
    mesh_objects = [obj for obj in objects if obj.type == "MESH"]
    vertices = sum(len(obj.data.vertices) for obj in mesh_objects)
    triangles = sum(sum(len(poly.vertices) - 2 for poly in obj.data.polygons) for obj in mesh_objects)
    materials = {slot.material.name for obj in mesh_objects for slot in obj.material_slots if slot.material}
    return len(mesh_objects), vertices, triangles, len(materials)


def is_leaf(obj):
    name = obj.name.lower()
    material_names = " ".join(
        slot.material.name.lower() for slot in obj.material_slots if slot.material
    )
    return name.startswith("leaf_") or "leaf" in material_names or "shadow" in material_names


def primary_material_key(obj):
    if not obj.material_slots or not obj.material_slots[0].material:
        return "__no_material__"

    return obj.material_slots[0].material.name


def delete_internal_leaf_faces(leaf_objects):
    infos = []
    for obj in leaf_objects:
        infos.append((obj, obj.matrix_world.copy(), obj.matrix_world.inverted()))

    removed = 0
    for obj, matrix, _ in infos:
        mesh = obj.data
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bm.faces.ensure_lookup_table()

        faces_to_remove = []
        for face in bm.faces:
            center = matrix @ face.calc_center_median()
            for other, _, other_inverse in infos:
                if other == obj:
                    continue

                local = other_inverse @ center
                if local.length < INTERNAL_LEAF_RADIUS:
                    faces_to_remove.append(face)
                    break

        if faces_to_remove:
            removed += len(faces_to_remove)
            bmesh.ops.delete(bm, geom=faces_to_remove, context="FACES")
            bm.to_mesh(mesh)
            mesh.update()

        bm.free()

    return removed


def decimate_leaf_meshes(leaf_objects):
    for obj in leaf_objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        modifier = obj.modifiers.new("Valendia leaf exterior simplification", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = LEAF_DECIMATE_RATIO
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)


def triangulate(objects):
    for obj in objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_add(type="TRIANGULATE")
        bpy.ops.object.modifier_apply(modifier=obj.modifiers[-1].name)
        obj.select_set(False)


def join_objects(objects, name):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    joined.data.name = f"{name}_mesh"
    return joined


def optimize_tree(source_name):
    clear_scene()
    source_path = SOURCE_FBX_DIR / f"{source_name}.fbx"
    target_path = OPTIMIZED_FBX_DIR / f"{source_name}_optimized.fbx"

    bpy.ops.import_scene.fbx(filepath=str(source_path))
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    before = mesh_stats(mesh_objects)

    if not mesh_objects:
        raise RuntimeError(f"No mesh objects found in {source_path}")

    leaf_objects = [obj for obj in mesh_objects if is_leaf(obj)]
    removed_internal_faces = delete_internal_leaf_faces(leaf_objects)
    decimate_leaf_meshes(leaf_objects)
    triangulate(mesh_objects)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_objects[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    groups = {}
    for obj in mesh_objects:
        groups.setdefault(primary_material_key(obj), []).append(obj)

    optimized_objects = []
    for index, (material_name, objects) in enumerate(sorted(groups.items())):
        safe_material_name = "".join(ch if ch.isalnum() else "_" for ch in material_name).strip("_")
        optimized_objects.append(join_objects(objects, f"{source_name}_{index:02d}_{safe_material_name}"))

    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    after = mesh_stats(optimized_objects)
    OPTIMIZED_FBX_DIR.mkdir(parents=True, exist_ok=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in optimized_objects:
        obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=str(target_path),
        use_selection=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        add_leaf_bones=False,
        object_types={"MESH"},
    )

    print(
        f"{source_name}: objects {before[0]} -> {after[0]}, "
        f"vertices {before[1]} -> {after[1]}, triangles {before[2]} -> {after[2]}, "
        f"materials {before[3]} -> {after[3]}, internal_leaf_faces_removed={removed_internal_faces}, "
        f"export={target_path}"
    )


def main():
    for source_name in TREE_NAMES:
        optimize_tree(source_name)


if __name__ == "__main__":
    main()
