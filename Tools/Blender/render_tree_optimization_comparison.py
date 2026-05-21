from pathlib import Path

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


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def mat(name, color, roughness=0.7):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        return material
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def import_tree(path, offset_x, collection_name):
    collection = bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(collection)
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]
    for obj in imported:
        if obj.type == "MESH":
            obj.location.x += offset_x
            for existing in list(obj.users_collection):
                existing.objects.unlink(obj)
            collection.objects.link(obj)
    return collection


def add_label(text, location):
    bpy.ops.object.text_add(location=location, rotation=(1.25, 0, 0))
    obj = bpy.context.object
    obj.name = f"Label_{text}"
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = 0.34
    obj.data.materials.append(mat(f"Label_{text}_mat", (0.94, 0.86, 0.64, 1)))


def setup_scene():
    ground = mat("preview_ground", (0.30, 0.44, 0.28, 1))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.35, -0.18))
    ground_obj = bpy.context.object
    ground_obj.name = "preview_ground"
    ground_obj.scale = (18.0, 4.4, 0.04)
    ground_obj.data.materials.append(ground)

    bpy.ops.object.light_add(type="SUN", location=(0, -6, 8))
    sun = bpy.context.object
    sun.name = "Preview Warm Sun"
    sun.data.energy = 3.0
    sun.rotation_euler = (0.74, 0.0, 0.56)

    bpy.ops.object.camera_add(location=(0, -18.0, 8.2))
    camera = bpy.context.object
    look_target = Vector((0, -0.15, 2.0))
    camera.rotation_euler = (look_target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 10.8
    bpy.context.scene.camera = camera

    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 2200
    bpy.context.scene.render.resolution_y = 980
    bpy.context.scene.eevee.taa_render_samples = 64
    bpy.context.scene.world.color = (0.12, 0.28, 0.32)


def main():
    clear_scene()
    setup_scene()
    x = -7.0
    for name in TREE_NAMES:
        import_tree(SOURCE_FBX_DIR / f"{name}.fbx", x, f"{name}_source")
        import_tree(OPTIMIZED_FBX_DIR / f"{name}_optimized.fbx", x + 1.25, f"{name}_optimized")
        label = name.replace("tree_reference_oak_", "").replace("_01", "")
        add_label(f"{label} src", (x, -2.55, 0.08))
        add_label(f"{label} opt", (x + 1.25, -2.55, 0.08))
        x += 3.4

    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(PREVIEW_DIR / "tree_optimization_source_vs_optimized.png")
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
