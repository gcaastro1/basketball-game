using System;
using UnityEngine;

namespace Basket.Characters
{
    // How a character looks and moves (Etapa 6). Purely presentation: gameplay never reads
    // it, so swapping models or clips cannot change a match.
    [CreateAssetMenu(fileName = "CharacterVisual", menuName = "Basket/Character Visual")]
    public class CharacterVisualDefinition : ScriptableObject
    {
        [Tooltip("Humanoid model (e.g. a Tripo3D import under Assets/TripoModels).")]
        public GameObject modelPrefab;
        [Tooltip("Model height in meters after fitting (the gameplay body is 1.9 m).")]
        public float modelHeight = 1.85f;
        [Tooltip("Turns the model if it does not face +Z.")]
        public float yawOffsetDegrees;

        [Header("Material")]
        [Tooltip("Replace the model's materials with the render pipeline's default lit material " +
                 "using baseMap (Tripo's generated material uses the Built-in Standard shader, pink under URP).")]
        public bool overrideMaterials = true;
        public Texture2D baseMap;

        [Header("Animation")]
        [Tooltip("Optional clips. Poses without a clip are animated procedurally.")]
        public CharacterAnimationClips clips = new CharacterAnimationClips();
    }

    // Humanoid clips per gameplay pose. The free locomotion set's idle + run enable the clip
    // backend; poses without a clip keep their procedural animation on top of the clips.
    //
    // Long mocap takes are used in parts: every clip reference carries a window (0..1 of the
    // clip) -- a loop plays only that stretch, a shot follows the gameplay shot through it.
    [Serializable]
    public class CharacterAnimationClips
    {
        [Header("Locomotion: without the ball / with the ball / in defensive guard")]
        public LocomotionClipSet free = new LocomotionClipSet();
        public LocomotionClipSet withBall = new LocomotionClipSet();
        public LocomotionClipSet guard = new LocomotionClipSet();

        [Header("With the ball")]
        [Tooltip("Upper body over the running legs when the ball handler runs faster than the set's moveSpeed.")]
        public LoopClip dribbleUpperBody;
        [Tooltip("Standing still holding the ball (full body).")]
        public LoopClip holdBall;

        [Header("Shots (window: gather .. ball leaves the hand)")]
        [Tooltip("Standing jump shots; one per shot, in turn.")]
        public ActionClip[] jumpShots = new ActionClip[0];
        [Tooltip("Pull-up jump shots when the shooter was moving.")]
        public ActionClip[] jumpShotsMoving = new ActionClip[0];
        public ActionClip layup;
        public ActionClip dunk;
        public ActionClip freeThrow;

        [Header("Other actions (window: start .. end, played once)")]
        public ActionClip pass;
        public ActionClip block;
        public ActionClip airborne;
        public ActionClip celebrate;

        [Tooltip("Plays every clip faster (>1) or slower (<1).")]
        [Range(0.5f, 1.5f)] public float playbackSpeed = 1f;

        public bool HasLocomotion => free != null && free.idle.clip != null && free.run.clip != null;
    }

    // Idle, directional moves at walking pace, and forward running with turns.
    [Serializable]
    public class LocomotionClipSet
    {
        public LoopClip idle;
        public LoopClip forward;
        public LoopClip backward;
        public LoopClip left;
        public LoopClip right;
        public LoopClip run;
        public LoopClip runTurnLeft;
        public LoopClip runTurnRight;
        [Tooltip("Speed (m/s) the directional clips' feet move at.")]
        public float moveSpeed = 1.5f;
        [Tooltip("Speed (m/s) the run clips' feet move at.")]
        public float runSpeed = 4.5f;

        public bool Any => idle.clip != null || forward.clip != null || run.clip != null;
    }

    // A clip played in a loop over part of it (from == to holds one frame).
    [Serializable]
    public struct LoopClip
    {
        public AnimationClip clip;
        [Range(0f, 1f)] public float from;
        [Range(0f, 1f)] public float to;
        [Tooltip("Play it backwards (e.g. a forward shuffle used for backing up).")]
        public bool reverse;

        public LoopClip(AnimationClip clip, float from = 0f, float to = 1f, bool reverse = false)
        {
            this.clip = clip;
            this.from = from;
            this.to = to;
            this.reverse = reverse;
        }

        // An unset window (0..0 on a clip that was never windowed) means the whole clip.
        public float From => to <= 0f && from <= 0f ? 0f : from;
        public float To => to <= 0f && from <= 0f ? 1f : to;
    }

    // A one-shot clip and the part of it to use: for shots start = gather and release = the
    // ball leaving the hand; for other actions release = where the action ends.
    [Serializable]
    public struct ActionClip
    {
        public AnimationClip clip;
        public ClipWindow window;

        public ActionClip(AnimationClip clip, ClipWindow window)
        {
            this.clip = clip;
            this.window = window;
        }
    }

    // A stretch of a clip in normalized time (0 = first frame, 1 = last).
    [Serializable]
    public struct ClipWindow
    {
        [Range(0f, 1f)] public float start;
        [Range(0f, 1f)] public float release;

        public ClipWindow(float start, float release)
        {
            this.start = start;
            this.release = release;
        }

        public static ClipWindow Default => new ClipWindow(0f, 0.5f);
    }
}
