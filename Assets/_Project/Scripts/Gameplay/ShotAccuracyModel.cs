using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public readonly struct ShotAccuracyInput
    {
        public readonly ShotType Type;
        public readonly float Distance;
        public readonly float TimingError;
        public readonly float Contest;
        public readonly float SpeedRatio;
        public readonly float Rating;

        public ShotAccuracyInput(ShotType type, float distance, float timingError, float contest, float speedRatio, float rating)
        {
            Type = type;
            Distance = distance;
            TimingError = timingError;
            Contest = contest;
            SpeedRatio = speedRatio;
            Rating = rating;
        }
    }

    // Aim error radius (m) for a shot. Each factor is a separate multiplier so each one
    // can be tuned, tested and later driven by character attributes independently.
    public static class ShotAccuracyModel
    {
        public static float ErrorRadius(in ShotAccuracyInput input, ShotConfig c)
        {
            float baseError = input.Type switch
            {
                ShotType.Dunk => 0f,
                ShotType.Layup => c.layupBaseError,
                ShotType.FreeThrow => c.freeThrowBaseError,
                _ => c.jumpShotBaseError + c.jumpShotErrorPerMeter * Mathf.Max(0f, input.Distance)
            };
            return baseError
                   * TimingFactor(input.Type, input.TimingError, c)
                   * (1f + c.contestPenalty * Mathf.Clamp01(input.Contest))
                   * (1f + c.movePenalty * Mathf.Clamp01(input.SpeedRatio))
                   * Mathf.Lerp(c.ratingErrorScaleAtZero, c.ratingErrorScaleAtOne, Mathf.Clamp01(input.Rating));
        }

        // Expected make chance for an aim-error radius, from the rim's calibrated make radius
        // (uniform sample in the error disc). For the HUD, balancing and tests.
        public static float EstimatedMakeChance(float errorRadius, ShotConfig c, ShotType type = ShotType.JumpShot)
        {
            if (type == ShotType.Dunk) return 1f;
            float makeRadius = type == ShotType.Layup ? c.calibratedLayupMakeRadius : c.calibratedMakeRadius;
            if (errorRadius <= makeRadius) return 1f;
            float ratio = makeRadius / errorRadius;
            return ratio * ratio;
        }

        // Shot meter: half-width (s) of the green window around the jump apex for this shot
        // (0 = no meter: layups and dunks release by themselves). Rating is the shot's
        // attribute (0..1): 3PT beyond the arc, mid-range, close shot, free throw.
        public static float GreenHalfWidth(ShotType type, float distance, float contest, float speedRatio, float rating, ShotConfig c)
        {
            if (!c.useGreenWindow || (type != ShotType.JumpShot && type != ShotType.FreeThrow)) return 0f;
            float width = Mathf.Lerp(c.greenHalfWidthAtRatingZero, c.greenHalfWidthAtRatingOne, Mathf.Clamp01(rating));
            float scale = (1f - c.greenContestShrink * Mathf.Clamp01(contest))
                          * (1f - c.greenMoveShrink * Mathf.Clamp01(speedRatio))
                          * (1f - c.greenShrinkPerMeter * Mathf.Max(0f, distance - c.greenFreeDistance));
            return width * Mathf.Max(c.greenMinScale, scale);
        }

        public static bool IsGreen(float timingError, float greenHalfWidth) =>
            greenHalfWidth > 0f && Mathf.Abs(timingError) <= greenHalfWidth;

        // With the shot meter: a green release has no aim error; outside the green the timing
        // penalty counts from its edge (instead of from the fixed perfect window).
        public static float ErrorRadius(in ShotAccuracyInput input, float greenHalfWidth, ShotConfig c)
        {
            if (greenHalfWidth <= 0f) return ErrorRadius(input, c);
            if (IsGreen(input.TimingError, greenHalfWidth)) return 0f;
            float beyondGreen = Mathf.Abs(input.TimingError) - greenHalfWidth;
            float timingFactor = 1f + c.timingPenaltyPerSecond * beyondGreen;
            float legacyTiming = TimingFactor(input.Type, input.TimingError, c);
            return ErrorRadius(input, c) / Mathf.Max(0.0001f, legacyTiming) * timingFactor;
        }

        // Jump shots and free throws are player-timed; layups/dunks release automatically.
        public static float TimingFactor(ShotType type, float timingError, ShotConfig c)
        {
            if (type != ShotType.JumpShot && type != ShotType.FreeThrow) return 1f;
            float beyondWindow = Mathf.Max(0f, Mathf.Abs(timingError) - c.perfectReleaseWindow);
            return 1f + c.timingPenaltyPerSecond * beyondWindow;
        }

        public static ShotType Classify(float distanceToRim, bool sprinting, float reachAtApex, float rimHeight, ShotConfig c)
        {
            bool canReachRim = reachAtApex >= rimHeight + c.dunkReachClearance;
            if (distanceToRim <= c.dunkRange && sprinting && canReachRim) return ShotType.Dunk;
            if (distanceToRim <= c.layupRange) return ShotType.Layup;
            return ShotType.JumpShot;
        }
    }
}
