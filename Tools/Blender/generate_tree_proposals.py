import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parent
TREE_ROOT = PROJECT / "Assets" / "Valendia" / "Art" / "Environment" / "Trees"
BLENDER_DIR = PROJECT / "SourceAssets" / "Valendia" / "Art" / "Environment" / "Trees" / "Blender"
FBX_DIR = TREE_ROOT / "Exports" / "FBX"
PREVIEW_DIR = TREE_ROOT / "Previews"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def mat(name, color, roughness=0.7):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    return material


MAT_DARK_TRUNK = None
MAT_WARM = None
MAT_GOLD = None
MAT_ROSE = None
MAT_OLIVE_SHADOW = None
MAT_SHADOW = None
MAT_LABEL = None


def set_flat(obj):
    obj.data.polygons.foreach_set("use_smooth", [False] * len(obj.data.polygons))
    obj.data.update()


def make_collection(name):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def link_to(collection, obj):
    for existing in obj.users_collection:
        existing.objects.unlink(obj)
    collection.objects.link(obj)


def tapered_cylinder_between(name, collection, radius_base, radius_tip, start, end, material, vertices=7):
    start_vec = Vector(start)
    end_vec = Vector(end)
    midpoint = (start_vec + end_vec) * 0.5
    direction = end_vec - start_vec
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_base,
        radius2=radius_tip,
        depth=direction.length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(material)
    set_flat(obj)
    link_to(collection, obj)
    return obj


def ico_blob(name, collection, location, scale, material, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.scale = scale
    obj.data.materials.append(material)
    set_flat(obj)
    link_to(collection, obj)
    return obj


def cube(name, collection, location, scale, rotation, material):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.scale = scale
    obj.data.materials.append(material)
    set_flat(obj)
    link_to(collection, obj)
    return obj


def add_label(text, x):
    bpy.ops.object.text_add(location=(x - 1.35, -2.35, 0.05), rotation=(math.radians(75), 0, 0))
    obj = bpy.context.object
    obj.name = f"Label_{text}"
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.size = 0.34
    obj.data.align_y = "CENTER"
    obj.data.materials.append(MAT_LABEL)
    return obj


def varied_leaf_scale(scale, index):
    variation = 0.8 + (index * 37 % 61) / 60.0 * 0.6
    multiplier = 1.8 * variation
    return (scale[0] * multiplier, scale[1] * multiplier, scale[2] * multiplier)


def make_reference_oak(x):
    col = make_collection("tree_reference_oak_01")
    trunk_base = (x, 0.0, -0.16)
    lower_fork = (x, 0.0, 1.28)
    upper_fork = (x, 0.0, 1.78)
    tapered_cylinder_between("trunk_reference_oak", col, 0.34, 0.18, trunk_base, upper_fork, MAT_DARK_TRUNK, 8)

    branch_specs = [
        ("north", 90, 1.22, 2.46, lower_fork, 0.080, 0.032),
        ("north_east", 38, 1.34, 2.72, lower_fork, 0.070, 0.028),
        ("east", 0, 1.26, 2.54, lower_fork, 0.076, 0.030),
        ("south_east", -44, 1.08, 2.96, upper_fork, 0.058, 0.024),
        ("south", -96, 1.16, 2.68, lower_fork, 0.068, 0.027),
        ("south_west", -142, 1.28, 2.52, lower_fork, 0.074, 0.030),
        ("west", 180, 1.22, 2.62, lower_fork, 0.074, 0.030),
        ("north_west", 136, 1.08, 3.02, upper_fork, 0.058, 0.024),
        ("crown_back", 66, 0.70, 3.40, upper_fork, 0.050, 0.021),
        ("crown_front", -116, 0.70, 3.36, upper_fork, 0.050, 0.021),
    ]

    branch_ends = []
    for suffix, angle_degrees, radius, end_z, start, radius_base, radius_tip in branch_specs:
        angle = math.radians(angle_degrees)
        end = (x + math.cos(angle) * radius, math.sin(angle) * radius, end_z)
        branch_ends.append((suffix, angle, end))
        tapered_cylinder_between(f"branch_reference_oak_{suffix}", col, radius_base, radius_tip, start, end, MAT_DARK_TRUNK, 6)

        side_angle = angle + math.radians(22)
        twig_end = (
            x + math.cos(side_angle) * (radius + 0.34),
            math.sin(side_angle) * (radius + 0.34),
            end_z + 0.42,
        )
        tapered_cylinder_between(f"twig_reference_oak_{suffix}", col, radius_tip * 0.82, radius_tip * 0.36, end, twig_end, MAT_DARK_TRUNK, 5)

    canopy_lobes = []
    for index, (suffix, angle, end) in enumerate(branch_ends):
        ex, ey, ez = end
        material = [MAT_WARM, MAT_GOLD, MAT_ROSE, MAT_OLIVE_SHADOW][index % 4]
        canopy_lobes.append((f"outer_{suffix}", (ex, ey, ez + 0.32), (0.66, 0.56, 0.54), material))

    canopy_lobes.extend([
        ("middle_north", (x + 0.10, 0.68, 3.12), (0.82, 0.72, 0.58), MAT_GOLD),
        ("middle_east", (x + 0.74, 0.08, 3.15), (0.82, 0.70, 0.58), MAT_WARM),
        ("middle_south", (x - 0.04, -0.68, 3.06), (0.82, 0.72, 0.58), MAT_ROSE),
        ("middle_west", (x - 0.74, -0.04, 3.12), (0.82, 0.70, 0.58), MAT_GOLD),
        ("core", (x, 0.04, 3.38), (0.92, 0.82, 0.66), MAT_GOLD),
        ("upper_north", (x + 0.08, 0.46, 3.82), (0.66, 0.56, 0.54), MAT_WARM),
        ("upper_east", (x + 0.44, -0.04, 3.86), (0.60, 0.52, 0.50), MAT_GOLD),
        ("upper_south", (x - 0.08, -0.42, 3.78), (0.62, 0.54, 0.50), MAT_WARM),
        ("upper_west", (x - 0.44, 0.04, 3.82), (0.60, 0.52, 0.50), MAT_GOLD),
        ("top_peak", (x + 0.02, 0.08, 4.28), (0.54, 0.46, 0.46), MAT_WARM),
    ])

    for index, (suffix, location, scale, material) in enumerate(canopy_lobes):
        ico_blob(f"leaf_reference_{suffix}", col, location, varied_leaf_scale(scale, index), material)

    add_label("Reference oak", x)
    return col


def export_collection(collection):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = collection.objects[0]
    filepath = FBX_DIR / f"{collection.name}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(filepath),
        use_selection=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        add_leaf_bones=False,
        object_types={"MESH"},
    )


def setup_scene():
    global MAT_DARK_TRUNK, MAT_WARM, MAT_GOLD, MAT_ROSE, MAT_OLIVE_SHADOW, MAT_SHADOW, MAT_LABEL

    MAT_DARK_TRUNK = mat("valendia_dark_bark", (0.18, 0.10, 0.06, 1))
    MAT_WARM = mat("valendia_img2_warm_leaf", (0.70, 0.46, 0.18, 1))
    MAT_GOLD = mat("valendia_img2_gold_leaf", (0.78, 0.60, 0.22, 1))
    MAT_ROSE = mat("valendia_img2_russet_shadow", (0.45, 0.25, 0.18, 1))
    MAT_OLIVE_SHADOW = mat("valendia_img2_olive_shadow", (0.25, 0.30, 0.18, 1))
    MAT_SHADOW = mat("valendia_preview_ground", (0.30, 0.44, 0.28, 1))
    MAT_LABEL = mat("valendia_preview_label", (0.95, 0.86, 0.58, 1))

    cube("preview_ground", bpy.context.scene.collection, (0, 0.35, -0.04), (18.0, 3.4, 0.04), (0, 0, 0), MAT_SHADOW)

    bpy.ops.object.light_add(type="SUN", location=(0, -6, 8))
    sun = bpy.context.object
    sun.name = "Preview Warm Sun"
    sun.data.energy = 3.1
    sun.rotation_euler = (math.radians(42), math.radians(0), math.radians(32))

    bpy.ops.object.camera_add(location=(0, -12.0, 6.4))
    camera = bpy.context.object
    look_target = Vector((0, -0.2, 2.35))
    camera.rotation_euler = (look_target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 16.5
    bpy.context.scene.camera = camera

    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1800
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.eevee.taa_render_samples = 64
    bpy.context.scene.world.color = (0.11, 0.28, 0.32)


def main():
    raise SystemExit("Use generate_oak_variations.py to export the final Valendia tree set.")


if __name__ == "__main__":
    main()
