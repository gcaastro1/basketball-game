using UnityEngine;
using Basket.Gameplay;

namespace Basket.Presentation
{
    // On the ball model: while a character holds the ball (or gathers it for a shot) the model
    // sits in his hands (PlayerAnimationDriver.TryGetHeldBallCenter) instead of at the
    // gameplay ball's held point; it eases back onto the gameplay ball once it leaves them.
    // Visual only: the physics ball does not move.
    [DefaultExecutionOrder(200)]
    public class BallVisualFollower : MonoBehaviour
    {
        private const float AttachRate = 30f;
        private const float ReleaseRate = 10f;

        private BallController ball;
        private Vector3 baseLocal;
        private Vector3 offset;
        private Transform lastHolder;
        private PlayerAnimationDriver holderDriver;

        public Vector3 Offset => offset;

        private void Start()
        {
            ball = GetComponentInParent<BallController>();
            baseLocal = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (ball == null) return;
            float dt = Time.deltaTime;
            Transform holder = ball.CurrentHolder;
            if (holder != lastHolder)
            {
                lastHolder = holder;
                holderDriver = holder != null ? holder.GetComponentInChildren<PlayerAnimationDriver>() : null;
            }
            Vector3 home = transform.parent.TransformPoint(baseLocal);
            if (holderDriver != null && holderDriver.TryGetHeldBallCenter(out Vector3 center))
                offset = Vector3.Lerp(offset, center - ball.transform.position, 1f - Mathf.Exp(-AttachRate * dt));
            else
                offset *= Mathf.Exp(-ReleaseRate * dt);
            if (offset.sqrMagnitude < 1e-6f) offset = Vector3.zero;
            transform.position = home + offset;
        }
    }
}
