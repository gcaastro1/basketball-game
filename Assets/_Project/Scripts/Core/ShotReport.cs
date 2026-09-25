using System;

namespace Basket.Core
{
    // What went into one shot's accuracy. Emitted when the ball leaves the hand; used by
    // the debug HUD now and by telemetry/balancing later.
    public readonly struct ShotReport
    {
        public readonly int ShooterIndex;
        public readonly ShotType Type;
        public readonly float Distance;
        // Seconds relative to the jump apex: negative = early, positive = late.
        public readonly float TimingError;
        // 0 = open, 1 = fully contested.
        public readonly float Contest;
        // Radius (m) of the aim-error disc on the rim plane that this shot sampled from.
        public readonly float ErrorRadius;

        public ShotReport(int shooterIndex, ShotType type, float distance, float timingError, float contest, float errorRadius)
        {
            ShooterIndex = shooterIndex;
            Type = type;
            Distance = distance;
            TimingError = timingError;
            Contest = contest;
            ErrorRadius = errorRadius;
        }
    }

    public interface IShotReportSource
    {
        event Action<ShotReport> OnShotTaken;
    }
}
