using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class ShootingSystem : MonoBehaviour
    {
        private BallController ball;
        private ShotConfig config;
        private Transform rimTarget;
        private readonly System.Random rng = new();

        public void Configure(BallController ballController, ShotConfig shotConfig, Transform rim)
        {
            ball = ballController;
            config = shotConfig;
            rimTarget = rim;
        }

        public bool TryShoot(Transform shooter)
        {
            if (ball.CurrentState != BallState.Held || ball.CurrentHolder != shooter) return false;

            Vector3 origin = ball.Position;
            Vector3 velocity = TrajectoryMath.ComputeArcVelocity(origin, rimTarget.position, config.arcHeight, Physics.gravity.y);
            velocity += ShotMath.ComputeMissOffset(config.baseAccuracyRadius, config.defaultShooterRating, rng);

            ball.Release(BallState.Shooting, velocity);
            return true;
        }
    }
}
