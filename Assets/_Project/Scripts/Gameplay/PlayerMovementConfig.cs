using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Basket/Player Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        public float maxSpeed = 6f;
        public float sprintMultiplier = 1.6f;
        public float acceleration = 30f;
        public float deceleration = 40f;
        public float turnSpeedDegrees = 720f;

        [Header("Jump (future: driven by the Vertical attribute)")]
        public float jumpHeight = 0.8f;
        // Fraction of ground acceleration available for steering while airborne.
        public float airControl = 0.15f;

        [Header("Reach")]
        // Fingertip height above the feet with arms raised, standing.
        public float standingReach = 2.45f;
    }
}
