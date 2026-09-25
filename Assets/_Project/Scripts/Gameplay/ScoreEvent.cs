using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // A made basket, with enough context for the rules to decide who scored and how much.
    public readonly struct ScoreEvent
    {
        public readonly PlayerEntity Releaser;
        public readonly TeamId? Team;
        public readonly Vector3 ReleasePosition;
        public readonly BallState ReleaseKind;
        // Null for a pass that went in.
        public readonly ShotType? ShotType;
        // The basket it went through (full court has two) and the team that attacks it:
        // that team gets the points, even on an own-basket accident. Null = single hoop.
        public readonly Vector3? HoopCenter;
        public readonly TeamId? HoopTeam;

        public ScoreEvent(PlayerEntity releaser, TeamId? team, Vector3 releasePosition, BallState releaseKind, ShotType? shotType = null,
            Vector3? hoopCenter = null, TeamId? hoopTeam = null)
        {
            ShotType = shotType;
            HoopCenter = hoopCenter;
            HoopTeam = hoopTeam;
            Releaser = releaser;
            Team = team;
            ReleasePosition = releasePosition;
            ReleaseKind = releaseKind;
        }
    }
}
