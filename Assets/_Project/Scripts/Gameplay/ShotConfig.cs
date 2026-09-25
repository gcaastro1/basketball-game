using UnityEngine;

namespace Basket.Gameplay
{
    // All numbers here are first-pass estimates for tuning (see the shot report on the
    // debug HUD). Aim error is a radius in meters on the rim plane: ~0.1 m still goes in
    // clean, ~0.2 m hits the rim.
    [CreateAssetMenu(fileName = "ShotConfig", menuName = "Basket/Shot Config")]
    public class ShotConfig : ScriptableObject
    {
        [Header("Arcs (apex height above the higher of release point / rim)")]
        public float arcHeight = 3.5f;
        public float layupArcHeight = 0.9f;

        [Header("Shot selection by horizontal distance to the rim")]
        public float layupRange = 2.6f;
        public float dunkRange = 2.2f;
        // Reach at the jump apex must clear the rim by this much to dunk.
        public float dunkReachClearance = 0.1f;
        // Horizontal distance from the raised hand to the rim center at the apex to finish a dunk.
        public float dunkFinishReach = 0.9f;
        public float maxDunkLungeSpeed = 6f;
        public float dunkBallDropSpeed = 4f;

        [Header("Release")]
        // Ball height above the feet while rising into a shot.
        public float shotPocketHeight = 2.1f;
        // Releasing within this many seconds of the jump apex costs nothing.
        public float perfectReleaseWindow = 0.05f;

        [Header("Accuracy: aim error radius (m) = base x modifiers")]
        public float jumpShotBaseError = 0.09f;
        public float jumpShotErrorPerMeter = 0.014f;
        public float layupBaseError = 0.06f;
        // Free throws: timed like a jump shot, never contested.
        public float freeThrowBaseError = 0.07f;
        // Multiplier added per second of release timing error beyond the perfect window.
        public float timingPenaltyPerSecond = 3f;
        // Multiplier added at full contest (1.0 doubles the error).
        public float contestPenalty = 1f;
        // Multiplier added when moving at full speed.
        public float movePenalty = 0.5f;

        [Header("Contest")]
        public float contestRadius = 2f;
        // Extra contest from a defender who is in the air (hand up).
        public float airborneContestBonus = 0.3f;

        [Header("Shooter rating (placeholder until character attributes, Etapa 5)")]
        [Range(0f, 1f)] public float defaultShooterRating = 0.75f;
        public float ratingErrorScaleAtZero = 1.4f;
        public float ratingErrorScaleAtOne = 0.6f;
    }
}
