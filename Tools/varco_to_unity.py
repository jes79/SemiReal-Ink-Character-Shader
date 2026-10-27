# VARCO 3D에서 받은 glb를 Unity용 FBX·PNG로 바꾸는 Blender 배치 스크립트
#
# 사용법 (Blender 4.x / 5.x):
#   blender -b --factory-startup -P Tools/varco_to_unity.py -- <glb 폴더>
#
# <glb 폴더>에 아래 이름으로 glb를 넣어 둔다 (VARCO 3D 노드 결과를 내려받아 이름만 바꾼 것)
#   Char_Armor_Rigged.glb · Char_Hanbok_Rigged.glb · Char_MageRobe_Rigged.glb   (Rig 노드 결과)
#   Weapon_Sword.glb · Weapon_Bow.glb · Weapon_Staff.glb                         (Generate 3D 결과)
#   Anim_idle.glb · Anim_walk.glb · …                                            (Animate 노드 결과, ANIMS 참고)
#
# 하는 일
#   1. 캐릭터·무기 → Models/Varco/<이름>.fbx, 텍스처 → Textures/Varco/<이름>_BaseColor/ORM/Normal.png
#   2. 애니메이션 → Models/Varco/Animations/<클립>.fbx (뼈대만, 클립 하나씩)
#   3. 로브·지팡이 베이스 컬러에서 밝은 하늘색(룬 문자·수정)만 골라 _Emission.png
#   4. ORM(R AO, G 러프니스, B 메탈릭) → URP Lit용 _MetallicSmoothness.png (R 메탈릭, G AO, A 스무스니스)
import bpy, os, sys
import numpy as np

SRC = sys.argv[sys.argv.index("--") + 1]
PROJ = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "CharacterShaderTest")
MODEL_DIR = os.path.join(PROJ, "Models", "Varco")
TEX_DIR = os.path.join(PROJ, "Textures", "Varco")
os.makedirs(os.path.join(MODEL_DIR, "Animations"), exist_ok=True)
os.makedirs(TEX_DIR, exist_ok=True)

CHARS = {"Char_Armor_Rigged": "Armor", "Char_Hanbok_Rigged": "Hanbok", "Char_MageRobe_Rigged": "MageRobe"}
WEAPONS = {"Weapon_Sword": "Sword", "Weapon_Bow": "Bow", "Weapon_Staff": "Staff"}
ANIMS = {"idle": "Idle", "walk": "Walk", "run": "Run", "jump": "Jump", "sword": "SwordSlash", "combo": "ComboSlash",
         "bow": "ShootBow", "staff": "StaffSpin", "magic": "PalmMagic", "hit": "HitReact"}
EMISSIVE = ["MageRobe", "Staff"]


def load(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(SRC, name + ".glb"))
    # VARCO glb에 들어 있는 미리보기용 구(Icosphere)는 버린다
    for o in list(bpy.data.objects):
        if o.name.startswith("Icosphere"):
            bpy.data.objects.remove(o, do_unlink=True)


def tex_path(prefix, kind):
    return os.path.join(TEX_DIR, f"{prefix}_{kind}.png")


def save_textures(prefix):
    m = bpy.data.materials["material_0"]
    m.name = prefix + "_Mat"
    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")

    def image_from(socket_name):
        node = bsdf.inputs[socket_name].links[0].from_node
        while node.type != "TEX_IMAGE":
            node = next(i for i in node.inputs if i.is_linked).links[0].from_node
        return node.image

    for kind, img in (("BaseColor", image_from("Base Color")), ("ORM", image_from("Roughness")), ("Normal", image_from("Normal"))):
        img.filepath_raw = tex_path(prefix, kind)
        img.file_format = "PNG"
        img.save()


def export(path, types, anim):
    bpy.ops.export_scene.fbx(filepath=path, object_types=types, add_leaf_bones=False, bake_anim=anim,
                             bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             path_mode="STRIP", embed_textures=False, apply_scale_options="FBX_SCALE_ALL",
                             armature_nodetype="NULL")


def read_pixels(path, non_color):
    img = bpy.data.images.load(path)
    if non_color:
        img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    return np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4), w, h


def write_pixels(path, px, w, h, non_color):
    img = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    if non_color:
        img.colorspace_settings.name = "Non-Color"
    img.pixels[:] = px.ravel()
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()


# ---- 1. 캐릭터·무기 ----
for src, prefix in {**CHARS, **WEAPONS}.items():
    load(src)
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.name = prefix + ("_Body" if src in CHARS else "")
        elif o.type == "ARMATURE":
            o.name = "Armature"
    save_textures(prefix)
    export(os.path.join(MODEL_DIR, prefix + ".fbx"), {"ARMATURE", "MESH"} if src in CHARS else {"MESH"}, False)

# ---- 2. 애니메이션 (뼈대만) ----
for key, clip in ANIMS.items():
    load("Anim_" + key)
    for o in list(bpy.data.objects):
        if o.type == "MESH":
            bpy.data.objects.remove(o, do_unlink=True)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    arm.name = "Armature"
    act = arm.animation_data.action
    act.name = clip
    bpy.context.scene.frame_start = int(act.frame_range[0])
    bpy.context.scene.frame_end = int(act.frame_range[1])
    export(os.path.join(MODEL_DIR, "Animations", clip + ".fbx"), {"ARMATURE"}, True)

# ---- 3. 이미시브 맵: 밝은 하늘색만 ----
for prefix in EMISSIVE:
    px, w, h = read_pixels(tex_path(prefix, "BaseColor"), False)
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    mx = np.maximum(np.maximum(r, g), b)
    sat = (mx - np.minimum(np.minimum(r, g), b)) / np.maximum(mx, 1e-4)
    mask = ((b > r + 0.15) & (g > r + 0.1) & (mx > 0.55) & (sat > 0.35)).astype(np.float32)
    out = np.zeros_like(px)
    out[..., :3] = px[..., :3] * mask[..., None]
    out[..., 3] = 1.0
    write_pixels(tex_path(prefix, "Emission"), out, w, h, False)

# ---- 4. URP Lit용 메탈릭 맵 ----
for prefix in list(CHARS.values()) + list(WEAPONS.values()):
    px, w, h = read_pixels(tex_path(prefix, "ORM"), True)
    out = np.zeros_like(px)
    out[..., 0] = px[..., 2]          # 메탈릭
    out[..., 1] = px[..., 0]          # AO (URP Lit Occlusion Map은 G를 읽음)
    out[..., 3] = 1.0 - px[..., 1]    # 스무스니스
    write_pixels(tex_path(prefix, "MetallicSmoothness"), out, w, h, True)

print("VARCO → Unity 변환 완료")
