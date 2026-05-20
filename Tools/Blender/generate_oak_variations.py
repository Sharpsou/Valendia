import importlib.util
from pathlib import Path

import bpy
from mathutils import Vector


SCRIPT_DIR = Path(__file__).resolve().parent
BASE_SCRIPT = SCRIPT_DIR / "generate_tree_proposals.py"

spec = importlib.util.spec_from_file_location("tree_proposals", BASE_SCRIPT)
tree_proposals = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tree_proposals)


VARIANTS = [
    {
        "name": "tree_reference_oak_broad_01",
        "label": "Broad",
        "x": -5.0,
        "width": 1.12,
        "height": 0.96,
        "lean": -0.04,
    },
    {
        "name": "tree_reference_oak_tall_01",
        "label": "Tall",
        "x": -2.5,
        "width": 0.92,
        "height": 1.16,
        "lean": 0.03,
    },
    {
        "name": "tree_reference_oak_core_01",
        "label": "Core",
        "x": 0.0,
        "width": 1.00,
        "height": 1.00,
        "lean": 0.00,
    },
    {
        "name": "tree_reference_oak_low_01",
        "label": "Low",
        "x": 2.5,
        "width": 1.18,
        "height": 0.88,
        "lean": 0.04,
    },
    {
        "name": "tree_reference_oak_slim_01",
        "label": "Slim",
        "x": 5.0,
        "width": 0.84,
        "height": 1.06,
        "lean": -0.03,
    },
]


def relabel_last(label):
    labels = [obj for obj in bpy.context.scene.objects if obj.type == "FONT" and obj.name.startswith("Label_")]
    if labels:
        labels[-1].data.body = label


def remove_source_labels():
    for obj in list(bpy.context.scene.objects):
        if obj.type == "FONT" and obj.name.startswith("Label_"):
            bpy.data.objects.remove(obj, do_unlink=True)


def apply_variant_shape(collection, anchor_x, width, height, lean):
    for obj in collection.objects:
        dx = obj.location.x - anchor_x
        obj.location.x = anchor_x + dx * width + lean * max(obj.location.z - 1.2, 0.0)
        obj.location.z *= height
        if obj.type == "MESH":
            obj.scale.x *= width
            obj.scale.z *= height


def duplicate_collection(source_collection, target_name, offset_x):
    preview_collection = bpy.data.collections.new(f"{target_name}_preview")
    bpy.context.scene.collection.children.link(preview_collection)
    for source in source_collection.objects:
        copy = source.copy()
        if source.data:
            copy.data = source.data.copy()
        copy.location.x += offset_x
        preview_collection.objects.link(copy)
    return preview_collection


def place_label(label, x):
    tree_proposals.add_label(label, x)


def main():
    tree_proposals.clear_scene()
    tree_proposals.setup_scene()
    tree_proposals.FBX_DIR.mkdir(parents=True, exist_ok=True)
    tree_proposals.BLENDER_DIR.mkdir(parents=True, exist_ok=True)
    tree_proposals.PREVIEW_DIR.mkdir(parents=True, exist_ok=True)

    collections = []
    for variant in VARIANTS:
        collection = tree_proposals.make_reference_oak(0.0)
        remove_source_labels()
        collection.name = variant["name"]
        apply_variant_shape(collection, 0.0, variant["width"], variant["height"], variant["lean"])
        collections.append(collection)

    for collection in collections:
        tree_proposals.export_collection(collection)

    preview_offsets = [variant["x"] for variant in VARIANTS]
    for collection, variant, offset_x in zip(collections, VARIANTS, preview_offsets):
        duplicate_collection(collection, variant["name"], offset_x)
        place_label(variant["label"], offset_x)
        for obj in collection.objects:
            obj.hide_viewport = True
            obj.hide_render = True

    bpy.ops.wm.save_as_mainfile(filepath=str(tree_proposals.BLENDER_DIR / "tree_reference_oak_variations.blend"))

    camera = bpy.context.scene.camera
    look_target = Vector((0.0, -0.15, 2.55))
    camera.location = (0.0, -12.5, 6.2)
    camera.rotation_euler = (look_target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.ortho_scale = 15.2
    bpy.context.scene.render.filepath = str(tree_proposals.PREVIEW_DIR / "tree_reference_oak_variations_v01.png")
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
