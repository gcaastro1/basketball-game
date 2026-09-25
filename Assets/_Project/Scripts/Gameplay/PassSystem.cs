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
            Vector3 destination = target.TryGetComponent<PlayerEntity>(out var receiver)
                ? receiver.FeetPosition + Vector3.up * config.holdHeightAboveFeet
                : target.position + Vector3.up * config.handHeightOffset;
            Vector3 velocity = TrajectoryMath.ComputeCompensatedArcVelocity(origin, destination, config.passApexHeight,
                Physics.gravity.y, ball.LinearDamping, Time.fixedDeltaTime);

            ball.Release(BallState.Passing, velocity);
            return true;
        }
    }
}
