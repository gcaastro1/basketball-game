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
        public float stealIntervalSeconds = 2.5f;

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

        [Header("Movement")]
        public float arrivalDistance = 0.2f;
        public bool sprintWhenChasing = true;
    }
}
