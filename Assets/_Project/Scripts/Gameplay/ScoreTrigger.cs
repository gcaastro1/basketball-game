using UnityEngine;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class ScoreTrigger : MonoBehaviour
    {
        private BallController ball;

        public void Configure(BallController ballController)
        {
            ball = ballController;
        }

        private void OnTriggerEnter(Collider other)
        {
            var otherBall = other.GetComponentInParent<BallController>();
            if (otherBall != null && otherBall == ball)
            {
                ball.NotifyScored();
            }
        }
    }
}
