from pathlib import Path
import math

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parent
SOURCE = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees" / "OptimizedFBX" / "tree_reference_oak_broad_01_optimized.fbx"
OUTPUT = PROJECT / "Logs" / "tree_hlod_comparison_broad.png"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def material_brightness(material):
    if not material:
        return 3.0
    color = material.diffuse_color
    return color[0] + color[1] + color[2]


def world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    min_v = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    max_v = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return min_v, max_v


def bounds_center_size(min_v, max_v):
    return (min_v + max_v) * 0.5, max_v - min_v


def make_canopy_blob(name, center, size, material, sides=5):
    radius_x = max(0.08, size.x * 0.5)
    radius_y = max(0.12, size.z * 0.5)
    half_height = max(0.12, size.y * 0.5)
    verts = [(center.x, center.y, center.z + half_height)]
    for i in range(sides):
        angle = math.tau * i / sides
        jitter = 1.0 + math.sin(angle * 2.31) * 0.09
        verts.append((
            center.x + math.cos(angle) * radius_x * jitter,
            center.y + math.sin(angle) * radius_y * jitter,
            center.z + math.sin(angle * 1.73) * half_height * 0.14,
        ))
    bottom_index = len(verts)
    verts.append((center.x, center.y, center.z - half_height))

    faces = []
    for i in range(sides):
        current = 1 + i
        nxt = 1 + ((i + 1) % sides)
        faces.append((0, current, nxt))
        faces.append((bottom_index, nxt, current))

    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if material:
        obj.data.materials.append(material)
    return obj


def make_trunk_prism(name, center, size, material, sides=5):
    radius_x = max(0.08, size.x * 0.5)
    radius_y = max(0.08, size.z * 0.5)
    half_height = max(0.12, size.y * 0.5)
    verts = []
    for z, scale in [(-half_height, 1.0), (half_height, 0.62)]:
        for i in range(sides):
            angle = math.tau * i / sides
            verts.append((
                center.x + math.cos(angle) * radius_x * scale,
                center.y + math.sin(angle) * radius_y * scale,
                center.z + z,
            ))

    faces = []
    top = sides
    for i in range(sides):
        nxt = (i + 1) % sides
        faces.append((i, top + i, nxt))
        faces.append((nxt, top + i, top + nxt))

    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if material:
        obj.data.materials.append(material)
    return obj


def main():
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(SOURCE))
    imported = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]

    for obj in imported:
        obj.location.x -= 2.8

    low_objects = []
    for index, obj in enumerate(imported):
        min_v, max_v = world_bounds(obj)
        min_v.x += 5.6
        max_v.x += 5.6
        center, size = bounds_center_size(min_v, max_v)
        material = obj.material_slots[0].material if obj.material_slots else None
        is_trunk = material_brightness(material) < 1.25 or size.z > max(size.x, size.y) * 1.4
        if is_trunk:
            low_objects.append(make_trunk_prism(f"low_trunk_{index}", center, size, material, 5))
        else:
            low_objects.append(make_canopy_blob(f"low_canopy_{index}", center, size, material, 5))

    for obj in imported + low_objects:
        obj.rotation_euler[2] = 0

    bpy.ops.object.light_add(type="SUN", location=(0, -5, 8))
    bpy.context.object.data.energy = 3.0
    bpy.context.object.rotation_euler = (math.radians(42), 0, math.radians(32))

    bpy.ops.object.camera_add(location=(0, -12, 5.6), rotation=(math.radians(63), 0, 0))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 7.2
    bpy.context.scene.camera = camera

    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1400
    bpy.context.scene.render.resolution_y = 720
    bpy.context.scene.world.color = (0.78, 0.86, 0.90)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(OUTPUT)
    bpy.ops.render.render(write_still=True)
    print(f"Wrote {OUTPUT}")


if __name__ == "__main__":
    main()
