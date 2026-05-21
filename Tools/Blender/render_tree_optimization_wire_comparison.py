from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parent
TREE_ROOT = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees"
SOURCE_FBX = TREE_ROOT / "Exports" / "FBX" / "tree_reference_oak_core_01.fbx"
OPTIMIZED_FBX = TREE_ROOT / "OptimizedFBX" / "tree_reference_oak_core_01_optimized.fbx"
PREVIEW_PATH = TREE_ROOT / "Previews" / "tree_optimization_wire_source_vs_optimized.png"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def mat(name, color, roughness=0.7):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
    return material


def mesh_stats(objects):
    meshes = [obj for obj in objects if obj.type == "MESH"]
    vertices = sum(len(obj.data.vertices) for obj in meshes)
    triangles = sum(sum(len(poly.vertices) - 2 for poly in obj.data.polygons) for obj in meshes)
    return len(meshes), vertices, triangles


def import_tree(path, offset_x, yaw_degrees):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]
    for obj in imported:
        if obj.type == "MESH":
            obj.location.x += offset_x
            obj.rotation_euler.z += yaw_degrees * 0.01745329252
    return imported


def add_wire_overlay(objects, material):
    for obj in objects:
        if obj.type != "MESH":
            continue

        obj.data.materials.append(material)
        wire = obj.modifiers.new("black_wire_triangle_check", "WIREFRAME")
        wire.thickness = 0.025
        wire.use_even_offset = True
        wire.use_replace = False
        wire.material_offset = len(obj.data.materials) - 1


def add_label(text, x, z=0.05, size=0.28):
    bpy.ops.object.text_add(location=(x, -2.7, z), rotation=(1.32, 0, 0))
    obj = bpy.context.object
    obj.name = f"Label_{text[:20]}"
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.materials.append(mat(f"label_{text[:20]}", (0.98, 0.90, 0.66, 1)))
    return obj


def setup_scene():
    ground = mat("preview_ground", (0.29, 0.42, 0.30, 1))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.45, -0.18))
    ground_obj = bpy.context.object
    ground_obj.name = "preview_ground"
    ground_obj.scale = (16.0, 5.8, 0.04)
    ground_obj.data.materials.append(ground)

    bpy.ops.object.light_add(type="SUN", location=(0, -6, 8))
    sun = bpy.context.object
    sun.name = "Preview Warm Sun"
    sun.data.energy = 3.0
    sun.rotation_euler = (0.74, 0.0, 0.56)

    bpy.ops.object.camera_add(location=(0, -18.0, 8.4))
    camera = bpy.context.object
    look_target = Vector((0, -0.15, 2.0))
    camera.rotation_euler = (look_target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 11.0
    bpy.context.scene.camera = camera

    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1800
    bpy.context.scene.render.resolution_y = 1100
    bpy.context.scene.eevee.taa_render_samples = 64
    bpy.context.scene.world.color = (0.12, 0.26, 0.30)


def main():
    clear_scene()
    setup_scene()
    wire_material = mat("wire_black", (0.015, 0.012, 0.010, 1), 0.5)

    source = import_tree(SOURCE_FBX, -3.4, 14.0)
    optimized = import_tree(OPTIMIZED_FBX, 3.4, 14.0)
    add_wire_overlay(source, wire_material)
    add_wire_overlay(optimized, wire_material)

    source_stats = mesh_stats(source)
    optimized_stats = mesh_stats(optimized)
    add_label(f"SOURCE - {source_stats[2]} tris / {source_stats[0]} objets", -3.4)
    add_label(f"OPTIMISE - {optimized_stats[2]} tris / {optimized_stats[0]} objets", 3.4)

    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
