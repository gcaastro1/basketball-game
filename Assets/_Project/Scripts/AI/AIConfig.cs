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
        // Within this distance of the ball handler (not sprinting), the defender takes the guard stance.
        public float guardStanceDistance = 3f;
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
        // A receiver meets an incoming pass where it comes down to this height (m).
        public float meetPassCatchHeight = 1.0f;

        [Header("Rules awareness")]
        // Shoot from wherever when the shot clock drops below this.
        public float shotClockUrgencySeconds = 2.5f;
        // How far beyond the arc to take the ball when it must be cleared.
        public float clearMargin = 0.8f;
        // Bend the clear toward the top of the arc (0 = straight out from the rim; out of a
        // corner that would be past the sideline on an NBA line).
        public float clearTowardTop = 0.6f;

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
        // Distance from the rim of perimeter spacing spots (just beyond a 6.75 m arc); on a
        // longer line they go spacingBeyondArc past it.
        public float spacingRadius = 7.3f;
        public float spacingBeyondArc = 0.4f;
        // Off-ball spots closer than this to the ball handler are left empty (no crowding him).
        public float spacingMinFromHandler = 4f;
        // Hand-placed spacing spots (Inspector); empty = computed on the arc above.
        public SpacingLayout spacingLayout;
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

        [Header("Team: off-ball defense (man-to-man)")]
        // Guarding a man this close to the ball (one pass away): deny, stay tight.
        public float denyDistance = 5.5f;
        // How far the denying defender steps into the passing lane, toward the ball.
        public float denyLaneStep = 0.6f;
        // At this distance from the ball (two passes away, weak side) the defender sags fully...
        public float helpFullDistance = 10f;
        // ...toward the help line: this fraction of the way from the rim to the ball...
        public float helpLineFraction = 0.35f;
        // ...by at most this share of the way from his man to that point...
        [Range(0f, 1f)] public float maxSag = 0.7f;
        // ...and never farther than this from his man (he must be able to close out).
        public float maxSagFromMan = 5f;

        [Header("Team: zone defense")]
        // Chance of playing zone on a defensive possession (0 = always man-to-man).
        [Range(0f, 1f)] public float zoneDefenseChance = 0f;
        // Zone spots slide toward the ball by this share of its offset from them...
        public float zoneShift = 0.35f;
        // ...by at most this far.
        public float zoneShiftMax = 2.5f;
        // An attacker this close to a defender's zone spot is his to mark (between him and the rim).
        public float zoneMarkRadius = 2.2f;
        public float zoneMarkDistance = 1f;

        [Header("Movement")]
        public float arrivalDistance = 0.2f;
        public bool sprintWhenChasing = true;
        // Steer around players closer than this in the direction of travel.
        public float avoidanceRadius = 1.1f;
    }
}
