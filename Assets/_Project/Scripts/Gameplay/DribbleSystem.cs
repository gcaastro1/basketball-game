using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class DribbleSystem : MonoBehaviour
    {
        [SerializeField] private float bounceHeight = 0.35f;
        [SerializeField] private float bounceFrequency = 2.2f;

        private BallController ball;
        private float phase;

        public void Configure(BallController ballController)
        {
            ball = ballController;
        }

        public void Tick(bool isMoving, float dt)
        {
            if (ball.CurrentState != BallState.Held)
            {
                phase = 0f;
                return;
            }
            if (!isMoving)
            {
                ball.SetHeldLocalOffset(Vector3.zero);
                return;
            }
            phase += dt * bounceFrequency * Mathf.PI * 2f;
            float offsetY = DribbleMath.ComputeBounceOffsetY(phase, bounceHeight);
            ball.SetHeldLocalOffset(Vector3.up * offsetY);
        }
    }
}
