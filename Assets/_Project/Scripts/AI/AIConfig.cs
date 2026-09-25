using UnityEngine;

namespace Basket.AI
{
    [CreateAssetMenu(fileName = "AIConfig", menuName = "Basket/AI Config")]
    public class AIConfig : ScriptableObject
    {
        [Header("Defense")]
        // Closer than this to the ball handler -> contest instead of positional guard.
        public float contestDistance = 2.5f;
        // How close a contest closes in (keeps the two bodies from clipping).
        public float contestStandoff = 1.1f;
        // Positional guard: stand this far from the man, on the line to the hoop.
        public float guardDistance = 1.5f;
        // Jump to block when the shooter is airborne within this distance...
        public float blockRange = 1.6f;
        // ...and their vertical speed has dropped below this (reaction delay).
        public float blockTriggerVerticalSpeed = 2.0f;
        public float stealRange = 1.2f;
        // Average seconds between steal attempts while pressuring the handler.
        public float stealIntervalSeconds = 4f;

        [Header("Offense")]
        // Horizontal distance to the rim at which the AI stops and takes a jump shot.
        public float shootRange = 5.5f;
        // Chance per possession to attack the rim (layup/dunk) instead of shooting a jumper.
        [Range(0f, 1f)] public float driveChance = 0.4f;
        // How close to the rim a drive goes before finishing.
        public float driveFinishDistance = 1.8f;
        // Minimum time holding the ball before a shot (no instant catch-and-fire loops).
        public float minHoldSecondsBeforeShot = 0.5f;
        // Std-dev of release timing around the jump apex (0 = perfect every time).
        public float releaseTimingJitterSeconds = 0.06f;
        // A jump shot starts only once the shooter has slowed below this (m/s): they stop
        // and set their feet instead of shooting on the run.
        public float setFeetSpeed = 1.0f;
        // A receiver meets an incoming pass at where the ball will be this far ahead (s).
        public float meetPassLookAhead = 0.3f;

        [Header("Rules awareness")]
        // Shoot from wherever when the shot clock drops below this.
        public float shotClockUrgencySeconds = 2.5f;
        // How far beyond the arc to take the ball when it must be cleared.
        public float clearMargin = 0.8f;

        [Header("Rebound")]
        // Jump for a descending loose ball this high above the feet...
        public float reboundJumpMinHeight = 2.3f;
        // ...within this horizontal distance.
        public float reboundJumpRange = 1.2f;
        // Chase the ball's projected position this far ahead.
        public float reboundLeadSeconds = 0.3f;

        [Header("Team: planning")]
        public float teamReplanInterval = 0.1f;
        // Relative weights for choosing each possession's play.
        public float spacingPlayWeight = 0.4f;
        public float pickAndRollPlayWeight = 0.4f;
        public float isolationPlayWeight = 0.2f;

        [Header("Team: offense")]
        // Distance from the rim of perimeter spacing spots (just beyond a 6.75 m arc).
        public float spacingRadius = 7.3f;
        // A cutter goes when their defender is at least this far from them.
        public float cutTriggerDistance = 2.4f;
        public float cutDurationSeconds = 1.6f;
        public float cutCooldownSeconds = 4f;
        public float screenOffset = 0.8f;
        public float screenHoldMaxSeconds = 2.5f;
        public float rollDurationSeconds = 1.5f;

        [Header("Team: full court")]
        // Farther than this from the rim, the handler brings the ball up instead of
        // running the half-court decision.
        public float advanceDistance = 11f;
        // Where the ball is brought to: this far from the rim, on the court's axis.
        public float advanceSpotDistance = 7f;
        // Sprint to a spot farther away than this (transition).
        public float sprintDistance = 6f;
        // With 4+ teammates off the ball, one plays in the post (4 out, 1 in).
        public float postSpotDistance = 2.6f;

        [Header("Team: ball handler utility")]
        // Shoot when the estimated shot value reaches this.
        public float shootQualityThreshold = 0.45f;
        // Pass when a teammate's shot value beats ours by this much...
        public float passAdvantage = 0.12f;
        // ...and is at least this good.
        public float passMinQuality = 0.4f;
        // Nobody within this distance of the path to the rim -> drive.
        public float driveLaneClearance = 1.2f;
        // After holding this long, take the best available option.
        public float forceDecisionSeconds = 3f;
        public float minForcedQuality = 0.3f;
        // ...and only when the AI's contest read is at most this; otherwise it passes or
        // attacks the rim instead of forcing a smothered jumper.
        public float forcedShotMaxContest = 0.4f;

        [Header("Team: shot quality estimate (the AI's own read, not the real model)")]
        public float qualityAtRim = 0.8f;
        public float qualityFalloffPerMeter = 0.065f;
        public float contestReadRadius = 2f;
        public float contestWeight = 0.8f;
        // Used only when the rules' point values are unknown.
        public float threePointValueMultiplier = 1.5f;
        // How much the shooter's attribute for that shot moves the estimate (x at 0 / 99).
        public float shotSkillAtZero = 0.55f;
        public float shotSkillAtMax = 1.3f;
        // A defender this close to the passing lane makes the pass risky.
        public float passLaneDanger = 0.9f;

        [Header("Team: defense")]
        public float helpRadius = 3.5f;
        // The handler is "beaten" when their defender is this much farther from the rim.
        public float beatenMargin = 0.4f;
        public float helpDepth = 1f;
        public float screenSwitchDistance = 1.0f;
        public float switchCooldownSeconds = 1.5f;
        public float boxOutDistance = 0.8f;

        [Header("Movement")]
        public float arrivalDistance = 0.2f;
        public bool sprintWhenChasing = true;
        // Steer around players closer than this in the direction of travel.
        public float avoidanceRadius = 1.1f;
    }
}
