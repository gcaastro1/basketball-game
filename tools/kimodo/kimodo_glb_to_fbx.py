# Blender headless script: converts a Kimodo (kimodo.cpp) SOMA-30 skeleton-only GLB
# animation into an FBX with a REAL bone armature, using Kimodo's own joint names and
# hierarchy unchanged (no retargeting math). Unity's Humanoid Avatar system -- the same
# mechanism already used for the project's existing mocap pack, which has its own,
# different skeleton than the playable character -- does the actual muscle-space
# retargeting at runtime; this script's only job is to hand it a valid, animated skeleton.
#
# Usage:
#   blender --background --python tools/kimodo/kimodo_glb_to_fbx.py -- \
#       --glb "D:/Projetos/kimodo-animations/<id>/animation.glb" \
#       --out "Assets/TripoModels/anime_character_3d_model/Animations/Kimodo/bb_free_idle.fbx" \
#       --take bb_free_idle
import bpy
import sys
import mathutils

def arg(name, default=None):
    argv = sys.argv[sys.argv.index("--") + 1:]
    if name in argv:
        return argv[argv.index(name) + 1]
    return default

GLB_PATH = arg("--glb")
OUT_FBX = arg("--out")
TAKE_NAME = arg("--take", "KimodoMotion")
FRAME_COUNT = int(arg("--frames", "150"))

# SOMA-30 hierarchy: (name, parent_name_or_None, rest_offset_from_parent). Names and parent
# links from kimodo-ai/demo/skeletons_extra.go; offsets from a demo /api/models response
# (both are fixed skeleton constants, the same for every Kimodo generation).
SKELETON = [
    ("Hips", None, (0, 0, 0)),
    ("Spine1", "Hips", (-0.000137, 0.050038, -0.000537)),
    ("Spine2", "Spine1", (-0.0000000187, 0.071253, -0.000298)),
    ("Chest", "Spine2", (-0.00000000575, 0.075501, -0.008160)),
    ("Neck1", "Chest", (-0.001817, 0.263113, -0.005533)),
    ("Neck2", "Neck1", (-0.0000000285, 0.077094, 0.023026)),
    ("Head", "Neck2", (-0.0000000460, 0.061289, 0.019537)),
    ("Jaw", "Head", (0.0000264, 0.004756, 0.030949)),
    ("LeftEye", "Head", (0.032064, 0.053802, 0.075869)),
    ("RightEye", "Head", (-0.032224, 0.053619, 0.075582)),
    ("LeftShoulder", "Chest", (0.016217, 0.232372, 0.051134)),
    ("LeftArm", "LeftShoulder", (0.149198, 0.0000000219, -0.055023)),
    ("LeftForeArm", "LeftArm", (0.287393, 0.0000000025, -0.0000259)),
    ("LeftHand", "LeftForeArm", (0.270940, -0.00000000707, 0.0000261)),
    ("LeftHandThumbEnd", "LeftHand", (0.122686, -0.032202, 0.048331)),
    ("LeftHandMiddleEnd", "LeftHand", (0.190120, -0.003129, -0.000340)),
    ("RightShoulder", "Chest", (-0.013801, 0.231803, 0.052142)),
    ("RightArm", "RightShoulder", (-0.150372, 0.000000117, -0.055456)),
    ("RightForeArm", "RightArm", (-0.287366, 0.0000000188, -0.0000260)),
    ("RightHand", "RightForeArm", (-0.271336, -0.00000000117, 0.0000261)),
    ("RightHandThumbEnd", "RightHand", (-0.122642, -0.032115, 0.048040)),
    ("RightHandMiddleEnd", "RightHand", (-0.190006, -0.003066, -0.000316)),
    ("LeftLeg", "Hips", (0.100432, -0.084345, 0.025957)),
    ("LeftShin", "LeftLeg", (-0.00000001, -0.432218, -0.008029)),
    ("LeftFoot", "LeftShin", (0.00000001, -0.421551, -0.034815)),
    ("LeftToeBase", "LeftFoot", (0, -0.050595, 0.132315)),
    ("RightLeg", "Hips", (-0.100473, -0.082953, 0.026203)),
    ("RightShin", "RightLeg", (0.00000001, -0.433622, -0.008056)),
    ("RightFoot", "RightShin", (0.00000002, -0.421174, -0.034784)),
    ("RightToeBase", "RightFoot", (-0.00000000343, -0.050796, 0.132842)),
]


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block_list in (bpy.data.actions, bpy.data.armatures, bpy.data.meshes, bpy.data.objects):
        for block in list(block_list):
            if block.users == 0:
                block_list.remove(block)


def gltf_to_blender(offset):
    # SKELETON's offsets are straight from Kimodo's own /api/models response, which uses
    # glTF's Y-up convention (matches the demo source too). Blender is Z-up; its own glTF
    # importer converts this automatically for the source Empties, but these hand-copied
    # rest offsets need the same conversion applied manually: (x, y, z) -> (x, -z, y).
    # Skipping this made the whole rest armature come out lying on its side, since (say)
    # the spine's "mostly +Y" (source-up) offset landed in Blender's Y axis, which is
    # horizontal in Z-up space, not vertical.
    x, y, z = offset
    return (x, -z, y)


def build_armature():
    armature_data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", armature_data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')

    world_pos = {}
    edit_bones = {}
    for name, parent, offset in SKELETON:
        parent_pos = world_pos[parent] if parent else mathutils.Vector((0, 0, 0))
        pos = parent_pos + mathutils.Vector(gltf_to_blender(offset))
        world_pos[name] = pos
        eb = armature_data.edit_bones.new(name)
        eb.head = pos
        # Tail placement is cosmetic only (Copy Location/Rotation constraints below drive
        # the actual pose in world space, overriding whatever rest orientation this gives
        # the bone) -- just needs to be non-zero length and distinct from head.
        eb.tail = pos + mathutils.Vector((0, 0.03, 0))
        if parent:
            eb.parent = edit_bones[parent]
        edit_bones[name] = eb

    bpy.ops.object.mode_set(mode='OBJECT')
    return arm


def import_kimodo_source():
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.gltf(filepath=GLB_PATH)
    new_objects = [bpy.data.objects[n] for n in bpy.data.objects.keys() if n not in before]
    return {o.name: o for o in new_objects if o.type == 'EMPTY'}


def main():
    clear_scene()
    arm = build_armature()
    source = import_kimodo_source()

    scene = bpy.context.scene
    # Project rule: 30 fps (docs/animacoes-lista.md #5). Neither scene.frame_end (after
    # the glTF import) nor the source action's own frame_range reliably reflect the real
    # ~150-frame/5s clip in this Blender version -- both kept reporting Blender's default
    # empty-scene range (250 frames at 24 fps = ~10.4s), baking a ~5s frozen hold onto the
    # back of every export (confirmed via Unity's animation debug panel showing clips at
    # [0.00-10.38s] instead of ~5s). Use the frame count the Kimodo generation request
    # actually asked for instead -- it is the ground truth, not something to detect.
    scene.render.fps = 30
    frame_start = 1
    frame_end = frame_start + FRAME_COUNT - 1
    # The FBX exporter's own bake_anim pass (below) re-bakes over scene.frame_start/end,
    # not whatever local variables computed it -- must be written back or it silently
    # bakes Blender's leftover default range again.
    scene.frame_start = frame_start
    scene.frame_end = frame_end

    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    for name, _parent, _offset in SKELETON:
        if name not in source:
            continue
        pb = arm.pose.bones[name]
        if name == "Hips":
            # Only the root carries position; every other bone's placement must come
            # purely from FK (parent transform + fixed rest length + rotation) like a
            # normal skeleton. Copying absolute world location onto every joint (as a
            # first version of this script did) stretched bones badly for the run/walk
            # clips: the in-place fix below holds Hips near the origin while a raw
            # Copy Location on, say, a foot would still chase its ORIGINAL far-away
            # world position (Kimodo's run travelled ~9 m over the clip), so the "bone"
            # between them stretched to match that gap.
            loc = pb.constraints.new('COPY_LOCATION')
            loc.target = source[name]
            loc.target_space = 'WORLD'
            loc.owner_space = 'WORLD'
            # In-place rule (project docs/animacoes-lista.md #1): the game's own movement
            # code drives horizontal travel. Kimodo does not reliably honor "in place" in
            # the prompt -- locomotion prompts came back with real, large forward/lateral
            # drift -- so only inherit the vertical (Z, Blender is Z-up here) bob and leave
            # X/Y at the bone's rest value (0, since Hips is the skeleton origin).
            loc.use_x = False
            loc.use_y = False
        rot = pb.constraints.new('COPY_ROTATION')
        rot.target = source[name]
        rot.target_space = 'WORLD'
        rot.owner_space = 'WORLD'

    bpy.ops.nla.bake(
        frame_start=frame_start,
        frame_end=frame_end,
        only_selected=False,
        visual_keying=True,
        clear_constraints=True,
        clear_parents=False,
        bake_types={'POSE'},
    )
    # Setting scene.frame_start/end alone never stuck (exports kept coming out at
    # Blender's leftover default range even right before the export call) -- the FBX
    # exporter's bake_anim pass apparently reads the baked Action's own manual playback
    # range instead of the scene's. Set that explicitly.
    scene.frame_start = frame_start
    scene.frame_end = frame_end
    baked_action = arm.animation_data.action
    baked_action.use_frame_range = True
    baked_action.frame_start = frame_start
    baked_action.frame_end = frame_end

    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in bpy.data.objects:
        obj.select_set(obj == arm)
    bpy.context.view_layer.objects.active = arm
    if arm.animation_data and arm.animation_data.action:
        arm.animation_data.action.name = TAKE_NAME

    print(f"DEBUG_PRE_EXPORT scene=({scene.frame_start},{scene.frame_end}) "
          f"action=({baked_action.frame_start},{baked_action.frame_end},use_frame_range={baked_action.use_frame_range}) "
          f"fps={scene.render.fps}")

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
    print(f"CONVERT_OK {OUT_FBX}")


main()
