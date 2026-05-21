from pathlib import Path
import math

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parent
SOURCE_FBX_DIR = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees" / "Exports" / "FBX"
HLOD_FBX_DIR = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees" / "HlodFBX"

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


def material_brightness(material):
    if material is None:
        return 3.0
    color = material.diffuse_color
    return color[0] + color[1] + color[2]


def world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    min_v = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    max_v = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return min_v, max_v


def center_size(min_v, max_v):
    return (min_v + max_v) * 0.5, max_v - min_v


def create_mesh_object(name, verts, faces, material):
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if material is not None:
        obj.data.materials.append(material)
    return obj


def add_canopy_blob(name, center, size, material, sides=8):
    radius_x = max(0.08, size.x * 0.5)
    radius_y = max(0.08, size.y * 0.5)
    half_height = max(0.08, size.z * 0.5)
    verts = [center + Vector((0, 0, half_height))]
    for i in range(sides):
        angle = math.tau * i / sides
        jitter = 1.0 + math.sin(angle * 2.31) * 0.08
        verts.append(center + Vector((
            math.cos(angle) * radius_x * jitter,
            math.sin(angle) * radius_y * jitter,
            math.sin(angle * 1.73) * half_height * 0.12,
        )))
    bottom = len(verts)
    verts.append(center - Vector((0, 0, half_height)))

    faces = []
    for i in range(sides):
        current = 1 + i
        nxt = 1 + ((i + 1) % sides)
        faces.append((0, current, nxt))
        faces.append((bottom, nxt, current))
    return create_mesh_object(name, verts, faces, material)


def add_trunk_prism(name, center, size, material, sides=6):
    radius_x = max(0.035, size.x * 0.5)
    radius_y = max(0.035, size.y * 0.5)
    half_height = max(0.08, size.z * 0.5)
    verts = []
    for z, scale in [(-half_height, 1.0), (half_height, 0.72)]:
        for i in range(sides):
            angle = math.tau * i / sides
            verts.append(center + Vector((
                math.cos(angle) * radius_x * scale,
                math.sin(angle) * radius_y * scale,
                z,
            )))

    faces = []
    top = sides
    for i in range(sides):
        nxt = (i + 1) % sides
        faces.append((i, top + i, nxt))
        faces.append((nxt, top + i, top + nxt))
    return create_mesh_object(name, verts, faces, material)


def optimize_tree(source_name):
    clear_scene()
    source_path = SOURCE_FBX_DIR / f"{source_name}.fbx"
    target_path = HLOD_FBX_DIR / f"{source_name}_hlod.fbx"
    bpy.ops.import_scene.fbx(filepath=str(source_path))

    source_objects = [obj for obj in list(bpy.context.scene.objects) if obj.type == "MESH"]
    generated = []

    trunk_candidates = []
    leaf_objects = []
    for obj in source_objects:
        name = obj.name.lower()
        material = obj.material_slots[0].material if obj.material_slots else None
        brightness = material_brightness(material)
        min_v, max_v = world_bounds(obj)
        center, size = center_size(min_v, max_v)
        is_bark = brightness < 1.25
        if is_bark and name.startswith("trunk_"):
            trunk_candidates.append((obj, material, min_v, max_v, center, size))
        elif not is_bark:
            leaf_objects.append((obj, material, min_v, max_v, center, size))

    if not trunk_candidates:
        bark_objects = []
        for obj in source_objects:
            material = obj.material_slots[0].material if obj.material_slots else None
            if material_brightness(material) >= 1.25:
                continue

            min_v, max_v = world_bounds(obj)
            center, size = center_size(min_v, max_v)
            bark_objects.append((obj, material, min_v, max_v, center, size))

        if bark_objects:
            trunk_candidates.append(max(bark_objects, key=lambda item: item[5].z))

    for index, (_, material, min_v, max_v, center, size) in enumerate(trunk_candidates[:1]):
        generated.append(add_trunk_prism(f"{source_name}_hlod_trunk_{index:02d}", center, size, material, sides=6))

    for index, (_, material, min_v, max_v, center, size) in enumerate(leaf_objects):
        generated.append(add_canopy_blob(f"{source_name}_hlod_canopy_{index:02d}", center, size * 1.02, material, sides=8))

    for obj in source_objects:
        bpy.data.objects.remove(obj, do_unlink=True)
    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in generated:
        obj.select_set(True)

    HLOD_FBX_DIR.mkdir(parents=True, exist_ok=True)
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

    verts = sum(len(obj.data.vertices) for obj in generated)
    tris = sum(len(obj.data.polygons) for obj in generated)
    print(f"{source_name}: HLOD objects={len(generated)}, vertices={verts}, triangles={tris}, export={target_path}")


def main():
    for source_name in TREE_NAMES:
        optimize_tree(source_name)


if __name__ == "__main__":
    main()
