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

        public ScoreEvent(PlayerEntity releaser, TeamId? team, Vector3 releasePosition, BallState releaseKind)
        {
            Releaser = releaser;
            Team = team;
            ReleasePosition = releasePosition;
            ReleaseKind = releaseKind;
        }
    }
}
