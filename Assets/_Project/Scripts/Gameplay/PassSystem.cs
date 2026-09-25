using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class PassSystem
    {
        private readonly BallController ball;
        private readonly BallConfig config;

        public PassSystem(BallController ballController, BallConfig ballConfig)
        {
            ball = ballController;
            config = ballConfig;
        }

        public bool TryPass(Transform passer, Transform target)
        {
            if (ball.CurrentState != BallState.Held || ball.CurrentHolder != passer) return false;

            Vector3 origin = ball.Position;
            float apex = config.passApexHeight;
            if (passer.TryGetComponent<PlayerEntity>(out var passerEntity) && passerEntity.Tuning != null)
                apex *= passerEntity.AttributeMult(AttributeId.Passing, passerEntity.Tuning.passApex);
            Vector3 destination;
            if (target.TryGetComponent<PlayerEntity>(out var receiver))
            {
                destination = receiver.FeetPosition + Vector3.up * config.holdHeightAboveFeet;
                // Lead a moving receiver (a cutter): throw to where they will be. Found by
                // the AI-vs-AI simulation: passes aimed at a cutter's current position
                // landed behind them and became loose balls.
                float flight = TrajectoryMath.EstimateFlightTime(origin, destination, apex, Physics.gravity.y);
                Vector3 lead = Vector3.ClampMagnitude(receiver.Motor.HorizontalVelocity * flight, config.maxPassLead);
                destination += lead;
            }
            else
            {
                destination = target.position + Vector3.up * config.handHeightOffset;
            }
            Vector3 velocity = TrajectoryMath.ComputeCompensatedArcVelocity(origin, destination, apex,
                Physics.gravity.y, ball.LinearDamping, Time.fixedDeltaTime);

            ball.Release(BallState.Passing, velocity);
            return true;
        }
    }
}
