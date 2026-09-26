using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Basket/Player Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        public float maxSpeed = 4.5f;
        public float sprintMultiplier = 1.45f;
        public float acceleration = 30f;
        public float deceleration = 40f;
        public float turnSpeedDegrees = 720f;
        [Tooltip("Speed multiplier in defensive guard (stance held with the Guard button).")]
        public float guardSpeedMultiplier = 0.75f;
        [Tooltip("Turning speed multiplier when facing a target (the basket on a shot, the ball in guard).")]
        public float faceTurnMultiplier = 1.5f;

        [Header("Jump (future: driven by the Vertical attribute)")]
        public float jumpHeight = 0.8f;
        // Fraction of ground acceleration available for steering while airborne.
        public float airControl = 0.15f;
        // Turning toward the movement while airborne (share of turnSpeedDegrees): a body in the
        // air cannot twist around by itself (0 = keeps its facing until it lands). Turning to a
        // face target (the rim while shooting) is not affected.
        [Range(0f, 1f)] public float airTurnMultiplier = 0f;

        [Header("Reach")]
        // Fingertip height above the feet with arms raised, standing.
        public float standingReach = 2.45f;
    }
}
