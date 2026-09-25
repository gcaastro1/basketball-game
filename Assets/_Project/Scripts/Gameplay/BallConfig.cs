using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Basket/Ball Config")]
    public class BallConfig : ScriptableObject
    {
        [Header("Physics")]
        public float mass = 0.62f;
        public float drag = 0.05f;
        public float angularDrag = 0.3f;
        public float bounciness = 0.75f;

        [Header("Holding (placeholder until hand sockets/IK exist)")]
        // Fallback for holders that are not PlayerEntity (e.g. bare transforms in tests):
        // ball sits this far above the holder's pivot.
        public float handHeightOffset = 1.1f;
        // For PlayerEntity holders: ball anchor relative to the player's feet and facing.
        public float holdHeightAboveFeet = 1.0f;
        public float holdForwardOffset = 0.45f;
        public float holdSideOffset = 0.2f;

        [Header("Catching")]
        public float catchRadius = 1.0f;

        [Header("Dribble")]
        public float dribbleBounceHeight = 0.85f;
        public float dribbleFrequency = 2.2f;

        [Header("Pass")]
        public float passApexHeight = 0.6f;
    }
}
