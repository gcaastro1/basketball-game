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

    // Humanoid clips per gameplay pose. Idle + run enable the clip backend; any other
    // missing clip falls back to locomotion.
    [Serializable]
    public class CharacterAnimationClips
    {
        public AnimationClip idle;
        public AnimationClip run;
        public AnimationClip dribble;
        public AnimationClip holdBall;
        public AnimationClip defense;
        public AnimationClip jumpShot;
        public AnimationClip layup;
        public AnimationClip dunk;
        public AnimationClip pass;
        public AnimationClip block;
        public AnimationClip airborne;
        public AnimationClip celebrate;

        public bool HasLocomotion => idle != null && run != null;
    }
}
