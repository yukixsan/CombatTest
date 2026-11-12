"""Split an MMD city PMX into per-building FBX files (Blender 3.6 + mmd_tools 2.10).

Usage:
  blender -b --factory-startup -P split_env.py -- <pmx> <out_dir> <prefix> <mode> [<blend_out>]
  mode = ruined   (ruined_town: material-categorised, buildings = footprint-touching parts)
  mode = cityapo  (CityApo face soup: height rules + spatial clustering)

Every exported FBX: one mesh, pivot at bottom-centre, metres (MMD x0.08),
Unity axes (Y up), materials renamed to their texture stem.
If <blend_out> is given, a .blend is also saved with every split object at its original
position (pivot bottom-centre), grouped in collections, textures linked relatively.
"""
import sys, os, json, math, addon_utils, bpy, bmesh
from collections import defaultdict
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
PMX, OUT, PREFIX, MODE = args[0], args[1], args[2], args[3]
BLEND_OUT = args[4] if len(args) > 4 else None
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
addon_utils.enable("mmd_tools", default_set=True, persistent=False)
bpy.ops.mmd_tools.import_model(filepath=PMX, scale=0.08, types={"MESH"}, clean_model=True)
SRC = [o for o in bpy.data.objects if o.type == "MESH"][0]
# Flatten into a plain object (drop MMD rig parenting) with world transform applied.
SRC.parent = None
bpy.context.view_layer.objects.active = SRC
SRC.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.data.objects):
    if o != SRC: bpy.data.objects.remove(o, do_unlink=True)

me = SRC.data

# ---------------------------------------------------------------- materials
def tex_stem(mat):
    if mat and mat.use_nodes:
        for n in mat.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image:
                stem = os.path.splitext(os.path.basename(n.image.filepath))[0]
                if not stem.lower().startswith("toon"):
                    return stem
    return mat.name if mat else "None"

# Merge material slots that share a texture, rename to the texture stem.
stem_of_slot = [tex_stem(m) for m in me.materials]
orig_names = [m.name if m else "" for m in me.materials]
# Category per face comes from the ORIGINAL material (several categories share a texture,
# e.g. ruined_town highway and street lights both use road_prop.png).
FACE_SRC_MAT = [orig_names[p.material_index] for p in me.polygons]
FACE_SRC_STEM = [stem_of_slot[p.material_index] for p in me.polygons]
unique = []
for s in stem_of_slot:
    if s not in unique: unique.append(s)
slot_map = [unique.index(s) for s in stem_of_slot]
new_mats = []
for s in unique:
    src = me.materials[stem_of_slot.index(s)]
    src.name = s
    new_mats.append(src)
new_idx = [slot_map[p.material_index] for p in me.polygons]
me.materials.clear()          # NB: also resets every polygon's material_index to 0
for m in new_mats: me.materials.append(m)
me.polygons.foreach_set("material_index", new_idx)
me.update()
cat_of_mat = {}
for i, s in enumerate(stem_of_slot):
    cat_of_mat.setdefault(slot_map[i], orig_names[i])

bm = bmesh.new(); bm.from_mesh(me)
bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table()
NF = len(bm.faces)

def face_bounds(f):
    xs = [v.co.x for v in f.verts]; ys = [v.co.y for v in f.verts]; zs = [v.co.z for v in f.verts]
    return (min(xs), min(ys), min(zs), max(xs), max(ys), max(zs))
FB = [face_bounds(f) for f in bm.faces]

# ---------------------------------------------------------------- union-find
class UF:
    def __init__(s, n): s.p = list(range(n))
    def f(s, a):
        while s.p[a] != a:
            s.p[a] = s.p[s.p[a]]; a = s.p[a]
        return a
    def u(s, a, b):
        a, b = s.f(a), s.f(b)
        if a != b: s.p[b] = a

def connected_parts(face_ids):
    """Faces sharing vertices -> same part."""
    uf = UF(NF); fs = set(face_ids)
    for fi in face_ids:
        for v in bm.faces[fi].verts:
            for lf in v.link_faces:
                if lf.index in fs: uf.u(fi, lf.index)
    parts = defaultdict(list)
    for fi in face_ids: parts[uf.f(fi)].append(fi)
    return list(parts.values())

def bounds_of(face_ids):
    b = [FB[i] for i in face_ids]
    return (min(x[0] for x in b), min(x[1] for x in b), min(x[2] for x in b),
            max(x[3] for x in b), max(x[4] for x in b), max(x[5] for x in b))

def merge_touching(groups, tol, cell=4.0):
    """Union groups whose AABBs overlap (expanded by tol). Spatial hash on XY."""
    bnds = [bounds_of(g) for g in groups]
    uf = UF(len(groups)); grid = defaultdict(list)
    for gi, b in enumerate(bnds):
        for cx in range(int(math.floor((b[0]-tol)/cell)), int(math.floor((b[3]+tol)/cell)) + 1):
            for cy in range(int(math.floor((b[1]-tol)/cell)), int(math.floor((b[4]+tol)/cell)) + 1):
                grid[(cx, cy)].append(gi)
    for ids in grid.values():
        for i in range(len(ids)):
            a = bnds[ids[i]]
            for j in range(i + 1, len(ids)):
                b = bnds[ids[j]]
                if (a[0]-tol <= b[3] and b[0]-tol <= a[3] and a[1]-tol <= b[4] and b[1]-tol <= a[4]
                        and a[2]-tol <= b[5] and b[2]-tol <= a[5]):
                    uf.u(ids[i], ids[j])
    merged = defaultdict(list)
    for gi, g in enumerate(groups): merged[uf.f(gi)].extend(g)
    return list(merged.values())

def merge_by_vertex_contact(groups, tol=0.3):
    """Union groups that have vertices within ~tol of each other (real contact, not AABB).
    Robust for rotated buildings whose AABBs overlap their neighbours."""
    uf = UF(len(groups)); cell_owner = {}
    for gi, g in enumerate(groups):
        for fi in g:
            for v in bm.faces[fi].verts:
                k = (int(math.floor(v.co.x / tol)), int(math.floor(v.co.y / tol)), int(math.floor(v.co.z / tol)))
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        for dz in (-1, 0, 1):
                            o = cell_owner.get((k[0]+dx, k[1]+dy, k[2]+dz))
                            if o is not None: uf.u(gi, o)
                cell_owner.setdefault(k, gi)
    merged = defaultdict(list)
    for gi, g in enumerate(groups): merged[uf.f(gi)].extend(g)
    return list(merged.values())

def merge_by_footprint(groups, cell=1.0, step=0.5):
    """Union groups whose real top-down footprints touch. Every face edge is sampled every
    `step` metres and rasterised into `cell` XY cells (8-neighbourhood). Rotated buildings keep
    their diamond footprint, so neighbours only merge when their walls are < ~1 cell apart."""
    uf = UF(len(groups)); owner = {}
    for gi, g in enumerate(groups):
        cells = set()
        for fi in g:
            vs = [v.co for v in bm.faces[fi].verts]
            for a, b in zip(vs, vs[1:] + vs[:1]):
                n = max(1, int((Vector((b.x - a.x, b.y - a.y)).length) / step))
                for k in range(n + 1):
                    t = k / n
                    cells.add((int(math.floor((a.x + (b.x - a.x) * t) / cell)),
                               int(math.floor((a.y + (b.y - a.y) * t) / cell))))
        for (cx, cy) in cells:
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    o = owner.get((cx + dx, cy + dy))
                    if o is not None: uf.u(gi, o)
        for c in cells: owner.setdefault(c, gi)
    merged = defaultdict(list)
    for gi, g in enumerate(groups): merged[uf.f(gi)].extend(g)
    return list(merged.values())

def attach_flat_inside(groups, flat_h=0.5):
    """Flat leftovers (floor slabs) join the solid group whose XY bounds contain their centre."""
    solid = [g for g in groups if bounds_of(g)[5] - bounds_of(g)[2] > flat_h]
    flats = [g for g in groups if bounds_of(g)[5] - bounds_of(g)[2] <= flat_h]
    sb = [bounds_of(g) for g in solid]
    for g in flats:
        b = bounds_of(g); cx, cy, cz = (b[0]+b[3])/2, (b[1]+b[4])/2, b[2]
        host = [i for i, s in enumerate(sb) if s[0] <= cx <= s[3] and s[1] <= cy <= s[4] and s[2]-0.5 <= cz <= s[5]+0.5]
        if host:
            best = min(host, key=lambda i: (sb[i][3]-sb[i][0]) * (sb[i][4]-sb[i][1]))
            solid[best].extend(g)
        else:
            solid.append(g); sb.append(b)
    return solid

# ---------------------------------------------------------------- classify
groups = {}   # name -> face ids

def sorted_by_pos(gs):
    return sorted(gs, key=lambda g: (round(bounds_of(g)[1] / 10), bounds_of(g)[0]))

if MODE == "ruined":
    CAT = {"地面": "Ground", "背景": "Backdrop", "高速道路": "Highway", "ビル": "Building",
           "車": "Car", "街灯": "StreetLight", "樹木": "Tree"}
    by_cat = defaultdict(list)
    for f in bm.faces:
        jp = FACE_SRC_MAT[f.index]
        cat = next((v for k, v in CAT.items() if jp.startswith(k)), "Misc")
        by_cat[cat].append(f.index)
    for cat in ("Ground", "Backdrop", "Highway", "Misc"):
        if by_cat.get(cat): groups[f"{PREFIX}_{cat}"] = by_cat[cat]
    blds = merge_by_footprint(connected_parts(by_cat["Building"]), cell=0.5, step=0.25)
    blds = attach_flat_inside(blds)
    blds = [b for b in blds if len(b) >= 4]
    for i, g in enumerate(sorted_by_pos(blds), 1):
        groups[f"{PREFIX}_Building_{i:02d}"] = g
    # Props: one representative per unique shape.
    for cat in ("Car", "StreetLight", "Tree"):
        items = merge_touching(connected_parts(by_cat.get(cat, [])), tol=0.05)
        seen = {}
        for g in items:
            b = bounds_of(g)
            sig = (len(g),) + tuple(sorted(round(b[k+3]-b[k], 1) for k in range(3)))
            if sig not in seen: seen[sig] = g
        for i, g in enumerate(seen.values(), 1):
            groups[f"{PREFIX}_{cat}_{i:02d}"] = g

elif MODE == "cityapo":
    ground, deck, rest = [], [], []
    for f in bm.faces:
        b = FB[f.index]; cz = (b[2] + b[5]) / 2
        nz = abs(f.normal.z)
        flat_big = nz > 0.8 and f.calc_area() > 30 and b[5] < 0.6
        stem = FACE_SRC_STEM[f.index]
        # Ground: slab, cliff sides, big flat tiles and everything below 1 m (curbs, sidewalks,
        # road decals) - those low strips otherwise chain every building along a street.
        if stem.startswith("sunatile") or cz < -0.5 or flat_big or b[5] < 1.0:
            ground.append(f.index)
        # Highway: upper deck, plus road-textured surfaces above ground (lower ramp/deck).
        elif cz > 13.6 or (stem.startswith("groad") and b[2] > 0.6):
            deck.append(f.index)
        else:
            rest.append(f.index)
    clusters = merge_touching([[i] for i in rest], tol=0.02, cell=3.0)
    buildings, pillars, debris, flats = [], [], [], []
    for g in clusters:
        b = bounds_of(g); w, d, h = b[3]-b[0], b[4]-b[1], b[5]-b[2]
        if h > 8 and max(w, d) < 4.5: pillars.append(g)
        elif max(w, d) > 40 and min(w, d) < 8: pillars.append(g)   # long rails / barriers
        elif h < 0.6: flats.append(g)
        elif math.sqrt(w*w + d*d + h*h) < 3.0: debris.append(g)
        else: buildings.append(g)
    # attach debris to nearest building (by XY centre distance, within 6 m)
    def ctr(g):
        b = bounds_of(g); return Vector(((b[0]+b[3])/2, (b[1]+b[4])/2))
    bctr = [ctr(g) for g in buildings]
    loose = []
    for g in debris:
        c = ctr(g); best = min(range(len(buildings)), key=lambda i: (bctr[i]-c).length) if buildings else -1
        if best >= 0 and (bctr[best]-c).length < 12: buildings[best].extend(g)
        else: loose.extend(g)
    groups[f"{PREFIX}_Ground"] = ground + [i for g in flats for i in g]
    groups[f"{PREFIX}_Highway"] = deck + [i for g in pillars for i in g]
    if loose: groups[f"{PREFIX}_Debris"] = loose
    for i, g in enumerate(sorted_by_pos(buildings), 1):
        groups[f"{PREFIX}_Building_{i:02d}"] = g
else:
    raise SystemExit("unknown mode " + MODE)

# ---------------------------------------------------------------- export
bm.free()
report = {}
for name, fids in groups.items():
    if not fids: continue
    obj = SRC.copy(); obj.data = SRC.data.copy(); obj.name = name; obj.data.name = name
    bpy.context.scene.collection.objects.link(obj)
    keep = set(fids)
    b2 = bmesh.new(); b2.from_mesh(obj.data); b2.faces.ensure_lookup_table()
    bmesh.ops.delete(b2, geom=[f for f in b2.faces if f.index not in keep], context="FACES")
    loose_v = [v for v in b2.verts if not v.link_faces]
    if loose_v: bmesh.ops.delete(b2, geom=loose_v, context="VERTS")
    xs = [v.co.x for v in b2.verts]; ys = [v.co.y for v in b2.verts]; zs = [v.co.z for v in b2.verts]
    pivot = Vector(((min(xs)+max(xs))/2, (min(ys)+max(ys))/2, min(zs)))
    for v in b2.verts: v.co -= pivot
    b2.to_mesh(obj.data); b2.free()
    # drop unused material slots (remap face indices explicitly; slot removal doesn't)
    old_mats = list(obj.data.materials)
    used = sorted({p.material_index for p in obj.data.polygons})
    remap = {old: new for new, old in enumerate(used)}
    idx_arr = [remap[p.material_index] for p in obj.data.polygons]
    obj.data.materials.clear()
    for old in used: obj.data.materials.append(old_mats[old])
    obj.data.polygons.foreach_set("material_index", idx_arr)
    obj.data.update()
    obj.location = (0, 0, 0)
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
                             mesh_smooth_type="FACE", use_mesh_modifiers=False,
                             path_mode="STRIP", embed_textures=False, add_leaf_bones=False)
    dims = (max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs))
    # Blender (x, y, z-up) -> Unity (x, z-up->y, -y->z)
    report[name] = {"faces": len(fids), "size_m": [round(d, 1) for d in dims],
                    "origin_unity": [round(pivot.x, 2), round(pivot.z, 2), round(-pivot.y, 2)],
                    "materials": [m.name for m in obj.data.materials]}
    if BLEND_OUT:
        # keep for the .blend: back at its original place, sorted into a collection
        obj.location = pivot
        kind = name[len(PREFIX) + 1:].split("_")[0]          # Building / Ground / Car ...
        coll = bpy.data.collections.get(kind) or bpy.data.collections.new(kind)
        if coll.name not in bpy.context.scene.collection.children:
            bpy.context.scene.collection.children.link(coll)
        bpy.context.scene.collection.objects.unlink(obj)
        coll.objects.link(obj)
    else:
        bpy.data.objects.remove(obj, do_unlink=True)

with open(os.path.join(OUT, PREFIX + "_split_report.json"), "w", encoding="utf-8") as fp:
    json.dump(report, fp, ensure_ascii=False, indent=1)

if BLEND_OUT:
    bpy.data.objects.remove(SRC, do_unlink=True)
    os.makedirs(os.path.dirname(BLEND_OUT), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT, compress=True)
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_mainfile(compress=True)
    print("BLEND saved", BLEND_OUT)
print("EXPORTED", len(report), "groups ->", OUT)
for k, v in report.items():
    print(f"  {k}: faces={v['faces']} size={v['size_m']} mats={v['materials']}")
