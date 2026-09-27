using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // Bounces the held ball once the holder starts dribbling (by moving), and keeps bouncing
    // when he stops -- a dribbler does not pick the ball up by standing still -- until the
    // ball leaves him or he gathers it for a shot (PickUp).
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

        public bool IsDribbling { get; private set; }
        // Bounces since the dribble started: x.0 = ball on the floor, x.5 = ball up in the hand.
        public float Bounces => phase / Mathf.PI;

        public void Tick(bool isMoving, float dt)
        {
            if (ball.CurrentState != BallState.Held)
            {
                phase = 0f;
                lastHolder = null;
                IsDribbling = false;
                return;
            }
            if (ball.CurrentHolder != lastHolder)
            {
                phase = 0f;
                lastHolder = ball.CurrentHolder;
                IsDribbling = false;
            }
            if (isMoving) IsDribbling = true;
            if (!IsDribbling)
            {
                ball.SetHeldLocalOffset(Vector3.zero);
                return;
            }
            // |sin| bounces twice per 2*pi: advance pi per bounce so the setting is bounces/s.
            phase += dt * config.dribbleFrequency * Mathf.PI;
            float offsetY = DribbleMath.ComputeBounceOffsetY(phase, config.dribbleBounceHeight);
            ball.SetHeldLocalOffset(Vector3.up * offsetY);
        }

        // Gathering the ball (shot): the dribble ends, the ball comes back to the hands.
        public void PickUp()
        {
            if (!IsDribbling && phase == 0f) return;
            IsDribbling = false;
            phase = 0f;
            if (ball.CurrentState == BallState.Held) ball.SetHeldLocalOffset(Vector3.zero);
        }
    }
}
