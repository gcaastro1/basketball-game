using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "DefenseConfig", menuName = "Basket/Defense Config")]
    public class DefenseConfig : ScriptableObject
    {
        [Header("Steal")]
        public float stealRange = 1.3f;
        // Minimum dot(defender facing, direction to handler).
        public float stealFacingDot = 0.3f;
        [Range(0f, 1f)] public float stealBaseChance = 0.3f;
        // A handler dribbling on the move exposes the ball more than one standing still.
        [Range(0f, 1f)] public float stealMovingHandlerBonus = 0.15f;
        // After any attempt (the "reach"), the defender cannot try again for this long.
        public float stealCooldownSeconds = 1f;
        public float stealKnockSpeed = 3f;

        [Header("Fouls (provisional chance-based contact model)")]
        // A missed steal attempt is called a reach-in foul this often.
        [Range(0f, 1f)] public float reachInFoulChance = 0.15f;
        // Defender body within this distance of a shooter at release...
        public float shootingContactDistance = 0.8f;
        // ...who is airborne or closing in at least this fast...
        public float shootingContactClosingSpeed = 1.5f;
        // ...is called for a shooting foul this often.
        [Range(0f, 1f)] public float shootingFoulChance = 0.35f;

        [Header("Block")]
        // A shot can be blocked for this long after release.
        public float blockWindowSeconds = 0.35f;
        // Ball within this distance of the defender's raised arms (head to fingertips).
        public float blockRadius = 0.45f;
        public float headHeight = 1.8f;
        public float blockDeflectSpeed = 4f;
    }
}
