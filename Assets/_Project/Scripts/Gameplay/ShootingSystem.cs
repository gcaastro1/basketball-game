using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class ShootingSystem
    {
        private readonly BallController ball;
        private readonly ShotConfig config;
        private readonly Vector3 rimCenter;
        private readonly System.Random rng;

        public ShootingSystem(BallController ballController, ShotConfig shotConfig, Vector3 rimCenter, System.Random random = null)
        {
            ball = ballController;
            config = shotConfig;
            this.rimCenter = rimCenter;
            rng = random ?? new System.Random();
        }

        public bool TryShoot(Transform shooter)
        {
            if (ball.CurrentState != BallState.Held || ball.CurrentHolder != shooter) return false;

            Vector3 origin = ball.Position;
            Vector3 velocity = TrajectoryMath.ComputeCompensatedArcVelocity(origin, rimCenter, config.arcHeight,
                Physics.gravity.y, ball.LinearDamping, Time.fixedDeltaTime);
            velocity += ShotMath.ComputeMissOffset(config.baseAccuracyRadius, config.defaultShooterRating, rng);

            ball.Release(BallState.Shooting, velocity);
            return true;
        }
    }
}
