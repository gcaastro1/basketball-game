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

    // Humanoid clips per gameplay pose. Idle + run enable the clip backend; poses without a
    // clip keep their procedural animation on top of the clips (shots, passes, ...).
    [Serializable]
    public class CharacterAnimationClips
    {
        [Header("Locomotion (loops, in place)")]
        public AnimationClip idle;
        public AnimationClip walk;
        public AnimationClip run;
        [Tooltip("Moving backward (backpedal on defense).")]
        public AnimationClip runBackward;
        [Tooltip("Speed (m/s) the walk clip's feet move at: playback is scaled to the real speed.")]
        public float walkSpeed = 1.5f;
        [Tooltip("Speed (m/s) the run clip's feet move at.")]
        public float runSpeed = 4.5f;

        [Header("Upper body over locomotion (legs keep running)")]
        public AnimationClip dribble;
        public AnimationClip holdBall;

        [Header("Full body")]
        public AnimationClip defense;
        [Tooltip("Defensive slide, used while a defender moves (defense = the stance when still).")]
        public AnimationClip defenseMove;
        public AnimationClip jumpShot;
        public AnimationClip layup;
        public AnimationClip dunk;
        public AnimationClip freeThrow;
        public AnimationClip pass;
        public AnimationClip block;
        public AnimationClip airborne;
        public AnimationClip celebrate;

        [Header("Shot timing (0..1 of each clip): from the gather to the ball leaving the hand")]
        [Tooltip("Between start and release the clip follows the gameplay shot, so the hand releases " +
                 "the ball on the same frame the game does; after the release the clip plays on.")]
        public ClipWindow jumpShotWindow = ClipWindow.Default;
        public ClipWindow layupWindow = ClipWindow.Default;
        public ClipWindow dunkWindow = ClipWindow.Default;
        public ClipWindow freeThrowWindow = ClipWindow.Default;

        public bool HasLocomotion => idle != null && run != null;
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
