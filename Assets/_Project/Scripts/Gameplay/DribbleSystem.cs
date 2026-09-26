using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // Visual dribble only (placeholder): bounces the held ball while the holder moves.
    public sealed class DribbleSystem
    {
        private readonly BallController ball;
        private readonly BallConfig config;
        private float phase;
        private Transform lastHolder;

        public DribbleSystem(BallController ballController, BallConfig ballConfig)
        {
            ball = ballController;
            config = ballConfig;
        }

        public void Tick(bool isMoving, float dt)
        {
            if (ball.CurrentState != BallState.Held)
            {
                phase = 0f;
                lastHolder = null;
                return;
            }
            if (ball.CurrentHolder != lastHolder)
            {
                phase = 0f;
                lastHolder = ball.CurrentHolder;
            }
            if (!isMoving)
            {
                ball.SetHeldLocalOffset(Vector3.zero);
                return;
            }
            // |sin| bounces twice per 2*pi: advance pi per bounce so the setting is bounces/s.
            phase += dt * config.dribbleFrequency * Mathf.PI;
            float offsetY = DribbleMath.ComputeBounceOffsetY(phase, config.dribbleBounceHeight);
            ball.SetHeldLocalOffset(Vector3.up * offsetY);
        }
    }
}
