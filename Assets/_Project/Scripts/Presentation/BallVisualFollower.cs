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
        // Caught (off the floor, a pass, a rebound): the model travels from where it was to the
        // hands at this rate instead of jumping to the gameplay ball in the hand.
        private const float CatchRate = 12f;

        private BallController ball;
        private Vector3 baseLocal;
        private Vector3 offset;
        private Transform lastHolder;
        private PlayerAnimationDriver holderDriver;
        private bool catching;

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
            Vector3 home = transform.parent.TransformPoint(baseLocal);
            if (holder != lastHolder)
            {
                // New holder: keep the model where it was this frame and let it travel in.
                if (holder != null && lastHolder == null) offset = transform.position - home;
                lastHolder = holder;
                holderDriver = holder != null ? holder.GetComponentInChildren<PlayerAnimationDriver>() : null;
                catching = holder != null;
            }
            float rate = catching ? CatchRate : AttachRate;
            if (holderDriver != null && holderDriver.TryGetHeldBallCenter(out Vector3 center))
            {
                Vector3 target = center - ball.transform.position;
                offset = Vector3.Lerp(offset, target, 1f - Mathf.Exp(-rate * dt));
                if ((offset - target).sqrMagnitude < 0.0004f) catching = false;
            }
            else if (holder != null && catching)
            {
                // Held but not in the hands' control (dribbling): still travel in, not snap.
                offset *= Mathf.Exp(-CatchRate * dt);
                if (offset.sqrMagnitude < 0.0004f) catching = false;
            }
            else
            {
                offset *= Mathf.Exp(-ReleaseRate * dt);
            }
            if (offset.sqrMagnitude < 1e-6f) offset = Vector3.zero;
            transform.position = home + offset;
        }
    }
}
