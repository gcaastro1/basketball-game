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
    }
}
