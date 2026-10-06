# Blender headless script: retargets a Kimodo (kimodo.cpp) SOMA-30 skeleton-only GLB
# animation onto the project's Tripo Humanoid rig and exports a Humanoid-ready FBX for
# CharacterAnimationImporter (Assets/TripoModels/**/Animations/).
#
# Usage:
#   blender --background --python tools/kimodo/retarget_to_tripo.py -- \
#       --tripo-fbx "Assets/TripoModels/anime_character_3d_model/anime_character_3d_model.fbx" \
#       --glb "D:/Projetos/kimodo-animations/<id>/animation.glb" \
#       --out "Assets/TripoModels/anime_character_3d_model/Animations/Kimodo/bb_free_idle.fbx" \
#       --take bb_free_idle
#
# Math: Kimodo's SOMA-30 rig is a "zero pose" skeleton -- every joint's local rotation is
# identity at rest (only the per-joint translation offset encodes the T-pose; confirmed by
# the demo's /api/models joint offsets and by skeletons_extra.go). So each source joint's
# *world* rotation at frame t already equals its motion delta from a shared world-identity
# rest frame -- the same reference every rig's bind pose can be expressed in. For each
# mapped bone, in armature space:
#   target_matrix(t) = source_world_rotation(t) @ target_rest_matrix        (all bones)
#   Hips also gets: target_matrix(t).translation += source_hips_world(t) - source_hips_world(rest)
# assigned top-down (parents before children) via pose_bone.matrix, which lets Blender
# derive the correct parent-relative pose automatically.
import bpy
import sys
import mathutils

def arg(name, default=None):
    argv = sys.argv[sys.argv.index("--") + 1:]
    if name in argv:
        return argv[argv.index(name) + 1]
    return default

TRIPO_FBX = arg("--tripo-fbx")
GLB_PATH = arg("--glb")
OUT_FBX = arg("--out")
TAKE_NAME = arg("--take", "KimodoMotion")

# SOMA-30 (Kimodo) joint name -> Tripo Humanoid bone name. Fingers/jaw are left unmapped
# (Kimodo only predicts two terminal hand markers, not real finger joints; Unity's Humanoid
# avatar treats fingers as optional) -- they stay at the Tripo rest pose, which is fine for
# a basketball game that never animates fingers.
JOINT_MAP = {
    "Hips": "Hips",
    "Spine1": "Spine",
    "Spine2": "Chest",
    "Chest": "UpperChest",
    "Neck1": "Neck",
    "Head": "Head",
    "LeftEye": "Left_Eye",
    "RightEye": "Right_Eye",
    "LeftShoulder": "Left_Shoulder",
    "LeftArm": "Left_UpperArm",
    "LeftForeArm": "Left_LowerArm",
    "LeftHand": "Left_Hand",
    "RightShoulder": "Right_Shoulder",
    "RightArm": "Right_UpperArm",
    "RightForeArm": "Right_LowerArm",
    "RightHand": "Right_Hand",
    "LeftLeg": "Left_UpperLeg",
    "LeftShin": "Left_LowerLeg",
    "LeftFoot": "Left_Foot",
    "LeftToeBase": "Left_Toes",
    "RightLeg": "Right_UpperLeg",
    "RightShin": "Right_LowerLeg",
    "RightFoot": "Right_Foot",
    "RightToeBase": "Right_Toes",
}

# Parents before children, matching the SOMA hierarchy (see skeletons_extra.go).
JOINT_ORDER = [
    "Hips", "Spine1", "Spine2", "Chest", "Neck1", "Head", "LeftEye", "RightEye",
    "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
    "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    "LeftLeg", "LeftShin", "LeftFoot", "LeftToeBase",
    "RightLeg", "RightShin", "RightFoot", "RightToeBase",
]

# Right-side bone -> its Left-side counterpart. The Tripo rig's actual Right_* rest
# rotations turned out to be authored in a way that made the straightforward retarget
# formula below produce a stiff, barely-moving right arm/leg while the left side (using
# its own rest rotation) retargeted correctly. Diagnosed with renders at
# tools/kimodo/ (frames from bb_free_idle/_test_wave): translation/bone-length data was
# fine and symmetric on both sides, determinants were all +1 (no mirroring/reflection in
# either rig), swapping the world-composition order and a parent-relative reformulation
# both made the LEFT side wrong too (arms spread toward T-pose) instead of fixing the
# right side alone -- so the issue is specific to how Right_*'s own rest quaternion reads,
# not the overall formula. Workaround: mirror the (working) Left rest rotation across the
# sagittal plane and use that as the Right bone's rotation reference instead of its own;
# keep the Right bone's own rest TRANSLATION (that part was never wrong).
MIRROR_OF = {
    "Right_Shoulder": "Left_Shoulder", "Right_UpperArm": "Left_UpperArm",
    "Right_LowerArm": "Left_LowerArm", "Right_Hand": "Left_Hand",
    "Right_UpperLeg": "Left_UpperLeg", "Right_LowerLeg": "Left_LowerLeg",
    "Right_Foot": "Left_Foot", "Right_Toes": "Left_Toes", "Right_Eye": "Left_Eye",
}


def mirror_quat(q):
    # Reflecting a rotation across the sagittal (X=0) plane: axis (nx,ny,nz) -> (nx,-ny,-nz),
    # angle unchanged. Derived from M@R@M with M=diag(-1,1,1) for the 3 basis rotations and
    # verified against each; matches quaternion (w,x,y,z) -> (w,x,-y,-z).
    return mathutils.Quaternion((q.w, q.x, -q.y, -q.z))


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block_list in (bpy.data.actions, bpy.data.armatures, bpy.data.meshes, bpy.data.objects):
        for block in list(block_list):
            if block.users == 0:
                block_list.remove(block)


def import_tripo_armature():
    bpy.ops.import_scene.fbx(filepath=TRIPO_FBX)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    for obj in list(bpy.data.objects):
        if obj.type == 'MESH':
            bpy.data.objects.remove(obj, do_unlink=True)
    # Keep the root object named "Armature" (Tripo's own convention): if Unity ever
    # defaults a new clip to "Copy From Other Avatar" against the main model's avatar,
    # that avatar's HumanDescription expects the parent of Hips to be named "Armature" --
    # a different root name (e.g. the export filename) makes that copy fail with a rig
    # error, even though "Create From This Model" (self-contained avatar) never cares.
    arm.name = "Armature"
    return arm


def import_kimodo_source():
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.gltf(filepath=GLB_PATH)
    new_objects = [bpy.data.objects[n] for n in bpy.data.objects.keys() if n not in before]
    return {o.name: o for o in new_objects if o.type == 'EMPTY'}


def main():
    clear_scene()
    target_arm = import_tripo_armature()
    source = import_kimodo_source()

    scene = bpy.context.scene
    frame_start, frame_end = int(scene.frame_start), int(scene.frame_end)
    if frame_end <= frame_start:
        frame_end = frame_start + 150

    bpy.context.view_layer.objects.active = target_arm
    bpy.ops.object.mode_set(mode='POSE')
    for pb in target_arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'

    # Rest pose (armature-space) matrix of every mapped target bone, captured once.
    target_rest = {tb: target_arm.data.bones[tb].matrix_local.copy() for tb in JOINT_MAP.values()}

    # Rotation reference used in the retarget formula: the bone's own rest rotation,
    # except for the Right_* bones in MIRROR_OF, which use their Left counterpart's rest
    # rotation mirrored across the sagittal plane instead (see MIRROR_OF comment above).
    # Translation always stays the bone's own (that part was correct on both sides).
    rest_rotation_ref = {}
    for tb in JOINT_MAP.values():
        if tb in MIRROR_OF:
            rest_rotation_ref[tb] = mirror_quat(target_rest[MIRROR_OF[tb]].to_quaternion())
        else:
            rest_rotation_ref[tb] = target_rest[tb].to_quaternion()

    scene.frame_set(frame_start)
    hips_rest_world = source["Hips"].matrix_world.translation.copy()

    for frame in range(frame_start, frame_end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()

        hips_delta_raw = source["Hips"].matrix_world.translation - hips_rest_world
        # In-place rule (project docs/animacoes-lista.md #1): the game's own movement code
        # drives horizontal travel; a clip must not carry the character across the floor.
        # Kimodo does not reliably honor "in place" in the prompt -- walk/run prompts came
        # back with real forward/lateral drift that accumulated over 150 frames and sent the
        # character flying off-screen in Unity's clip preview. Zero the horizontal (X/Y in
        # this Blender-space, Z-up) component and keep only the natural vertical bob.
        hips_delta = mathutils.Vector((0.0, 0.0, hips_delta_raw.z))

        for src_name in JOINT_ORDER:
            if src_name not in source or src_name not in JOINT_MAP:
                continue
            tb_name = JOINT_MAP[src_name]
            source_rot_q = source[src_name].matrix_world.to_quaternion()
            rest = target_rest[tb_name]
            # Compose rotations only (source's world delta on top of the target's rest
            # orientation); keep the rest TRANSLATION as-is -- only Hips carries position,
            # every other bone's placement comes purely from the FK chain + bone length.
            # (A previous version multiplied the full rest matrix by a translation-less
            # rotation matrix, which also rotates the rest translation column and distorts
            # every bone's position -- that produced the leaning/tiptoe pose.)
            new_rotation = (source_rot_q @ rest_rotation_ref[tb_name]).to_matrix().to_4x4()
            target_matrix = mathutils.Matrix.Translation(rest.translation) @ new_rotation
            if tb_name == "Hips":
                target_matrix.translation = rest.translation + hips_delta

            pb = target_arm.pose.bones[tb_name]
            pb.matrix = target_matrix
            bpy.context.view_layer.update()
            pb.keyframe_insert("rotation_quaternion", frame=frame)
            if tb_name == "Hips":
                pb.keyframe_insert("location", frame=frame)

    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in bpy.data.objects:
        obj.select_set(obj == target_arm)
    bpy.context.view_layer.objects.active = target_arm
    if target_arm.animation_data and target_arm.animation_data.action:
        target_arm.animation_data.action.name = TAKE_NAME

    bpy.ops.export_scene.fbx(
        filepath=OUT_FBX,
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
    )
    print(f"RETARGET_OK {OUT_FBX}")


main()
