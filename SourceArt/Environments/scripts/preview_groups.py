"""Re-assemble exported FBX groups at their original positions, colour each group, render top + persp.
Usage: blender -b --factory-startup -P preview_groups.py -- <dir> <prefix> <out_prefix>
"""
import sys, os, json, random, colorsys, bpy
from mathutils import Vector
d, prefix, out = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
rep = json.load(open(os.path.join(d, prefix + "_split_report.json"), encoding="utf-8"))
random.seed(3)
for name, info in rep.items():
    bpy.ops.import_scene.fbx(filepath=os.path.join(d, name + ".fbx"))
    o = bpy.context.selected_objects[0]
    ux, uy, uz = info["origin_unity"]          # Unity (x, y-up, z) -> Blender (x, -z, y)
    o.location = (ux, -uz, uy)
    if "_Building_" in name:
        col = colorsys.hsv_to_rgb(random.random(), 0.75, 0.95)
    elif name.endswith("Ground"): col = (0.25, 0.25, 0.25)
    elif name.endswith("Highway"): col = (0.9, 0.9, 0.9)
    else: col = (0.4, 0.4, 0.5)
    o.color = (*col, 1)
sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"; sc.display.shading.color_type = "OBJECT"; sc.display.shading.light = "STUDIO"
sc.render.resolution_x = sc.render.resolution_y = 1400
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
mn = Vector([min(p[i] for p in pts) for i in range(3)]); mx = Vector([max(p[i] for p in pts) for i in range(3)])
ctr = (mn + mx) / 2; size = max(mx.x - mn.x, mx.y - mn.y)
cd = bpy.data.cameras.new("c"); cam = bpy.data.objects.new("c", cd); sc.collection.objects.link(cam); sc.camera = cam
cd.type = "ORTHO"; cd.ortho_scale = size * 1.05; cd.clip_end = 3000
cam.location = (ctr.x, ctr.y, mx.z + 100); cam.rotation_euler = (0, 0, 0)
sc.render.filepath = out + "_groups_top.png"; bpy.ops.render.render(write_still=True)
cd.type = "PERSP"; cd.lens = 28
cam.location = (ctr.x + size * 0.45, ctr.y - size * 0.65, mx.z + size * 0.3)
cam.rotation_euler = (ctr - cam.location).to_track_quat("-Z", "Y").to_euler()
sc.render.filepath = out + "_groups_persp.png"; bpy.ops.render.render(write_still=True)
