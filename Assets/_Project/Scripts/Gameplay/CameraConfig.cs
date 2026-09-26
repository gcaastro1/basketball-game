using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Basket/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Broadcast follow (behind the player, facing the attack)")]
        [Tooltip("Distance behind the player (m).")]
        public float distance = 7f;
        [Tooltip("Camera height above the player's feet (m).")]
        public float height = 3.6f;
        [Tooltip("Height of the point the camera looks at (m).")]
        public float lookHeight = 1.4f;
        [Tooltip("Look this fraction of the way from the player to the basket...")]
        [Range(0f, 1f)] public float lookAhead = 0.35f;
        [Tooltip("...but never more than this (m).")]
        public float maxLookAhead = 4f;
        [Tooltip("0 = straight down the court (broadcast), 1 = right behind the player's line to the rim.")]
        [Range(0f, 1f)] public float aimAtHoop = 0.3f;
        [Tooltip("How fast the camera turns (e.g. when the attack changes ends in full court).")]
        public float turnSmoothing = 3f;
        public float fieldOfView = 55f;

        [Header("Follow")]
        public float followSmoothing = 8f;
        [Tooltip("Plain follow offset, used when no basket to face is known.")]
        public Vector3 offset = new Vector3(0f, 6f, -8f);
    }
}
