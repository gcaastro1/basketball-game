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
        [Tooltip("Jump shots and free throws: the arc brings the ball into the rim at this angle " +
                 "(real shooters: ~45 degrees). The apex grows with the distance.")]
        public float entryAngleDegrees = 47f;
        [Tooltip("Apex never lower than this above the rim (short shots)...")]
        public float minArcHeight = 0.9f;
        [Tooltip("...nor higher than this (long shots).")]
        public float maxArcHeight = 2.6f;
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
        // Calibrated against the physical rim (ShotCalibrationTests): shots within ~0.09 m
        // of the rim center always go in, none beyond ~0.2 m. That curve behaves like a
        // clean "make radius" of calibratedMakeRadius: make chance ~ (makeRadius / errorRadius)^2.
        // Targets for an average shooter (rating 0.75) with perfect timing: open 3PT ~40%,
        // open 4.5 m ~50%, free throw ~75%, fully contested 3PT ~18%; open layup ~86%,
        // fully contested layup ~38%.
        public float calibratedMakeRadius = 0.124f;
        // Layups (low arc) have a wider curve: always in up to ~0.12 m, ~13% at 0.15 m.
        public float calibratedLayupMakeRadius = 0.141f;
        public float jumpShotBaseError = 0.165f;
        public float jumpShotErrorPerMeter = 0.012f;
        public float layupBaseError = 0.19f;
        // Free throws: timed like a jump shot, never contested.
        public float freeThrowBaseError = 0.18f;
        // Multiplier added per second of release timing error beyond the perfect window.
        public float timingPenaltyPerSecond = 3f;
        // Multiplier added at full contest (0.5 = 1.5x the error).
        public float contestPenalty = 0.5f;
        // Multiplier added when moving at full speed.
        public float movePenalty = 0.3f;

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
