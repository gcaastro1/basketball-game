using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // Sits at the rim center. The rim itself is physical (a ring of colliders built by the
    // arena); this component only detects a make, by watching the ball's center cross the
    // rim plane downward inside the ring on consecutive physics steps.
    public class HoopController : MonoBehaviour
    {
        private BallController ball;
        private float rimRadius;
        private Vector3 previousBallPosition;
        private bool hasPrevious;

        public Vector3 RimCenter => transform.position;
        public float RimRadius => rimRadius;
        // Team attacking this basket (full court); null = both (half court).
        public TeamId? AttackingTeam { get; set; }

        public void Configure(BallController ballController, float radius)
        {
            ball = ballController;
            rimRadius = radius;
            hasPrevious = false;
        }

        private void FixedUpdate()
        {
            if (ball == null) return;

            Vector3 current = ball.PhysicsPosition;
            if (hasPrevious && ball.CurrentState != BallState.Held &&
                HoopMath.IsScoringCrossing(previousBallPosition, current, RimCenter, rimRadius))
            {
                ball.NotifyScored(RimCenter, AttackingTeam);
            }
            previousBallPosition = current;
            hasPrevious = true;
        }
    }
}
