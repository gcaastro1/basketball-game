using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class PassSystem : MonoBehaviour
    {
        private BallController ball;
        private BallConfig config;

        public void Configure(BallController ballController, BallConfig ballConfig)
        {
            ball = ballController;
            config = ballConfig;
        }

        public bool TryPass(Transform passer, Transform target)
        {
            if (ball.CurrentState != BallState.Held || ball.CurrentHolder != passer) return false;

            Vector3 origin = passer.position + Vector3.up * config.handHeightOffset;
            Vector3 destination = target.position + Vector3.up * config.handHeightOffset;
            Vector3 velocity = TrajectoryMath.ComputeArcVelocity(origin, destination, apexHeight: 1.2f, gravity: Physics.gravity.y);

            ball.Release(BallState.Passing, velocity);
            return true;
        }
    }
}
