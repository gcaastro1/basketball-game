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
