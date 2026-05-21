from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parent
SOURCE_FBX_DIR = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees" / "Exports" / "FBX"
OPTIMIZED_FBX_DIR = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees" / "OptimizedFBX"

TREE_NAMES = [
    "tree_reference_oak_broad_01",
    "tree_reference_oak_tall_01",
    "tree_reference_oak_core_01",
    "tree_reference_oak_low_01",
    "tree_reference_oak_slim_01",
]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def mesh_stats(objects):
    mesh_objects = [obj for obj in objects if obj.type == "MESH"]
    vertices = sum(len(obj.data.vertices) for obj in mesh_objects)
    triangles = sum(sum(len(poly.vertices) - 2 for poly in obj.data.polygons) for obj in mesh_objects)
    materials = {slot.material.name for obj in mesh_objects for slot in obj.material_slots if slot.material}
    return len(mesh_objects), vertices, triangles, len(materials)


def primary_material_key(obj):
    if not obj.material_slots or not obj.material_slots[0].material:
        return "__no_material__"

    return obj.material_slots[0].material.name


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

    # Remove imported empties/cameras/lights. The exported tree should keep only render geometry,
    # grouped by material so Unity's static bake does not drop secondary submeshes.
    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in optimized_objects:
        obj.select_set(True)

    after = mesh_stats(optimized_objects)
    OPTIMIZED_FBX_DIR.mkdir(parents=True, exist_ok=True)
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
        f"materials {before[3]} -> {after[3]}, export={target_path}"
    )


def main():
    for source_name in TREE_NAMES:
        optimize_tree(source_name)


if __name__ == "__main__":
    main()
