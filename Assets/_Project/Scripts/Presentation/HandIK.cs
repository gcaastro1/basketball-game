using UnityEngine;

namespace Basket.Presentation
{
    // Puts a hand on the ball after the body pose is set (LateUpdate): the ball stays where
    // gameplay puts it and the arm reaches for it, never the other way round.
    public sealed class HandIK
    {
        private readonly Transform upper, lower, hand;
        private readonly float side; // -1 left, +1 right

        public HandIK(Animator animator, bool right)
        {
            upper = animator.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
            lower = animator.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            hand = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            side = right ? 1f : -1f;
        }

        public bool IsValid => upper != null && lower != null && hand != null;
        public Vector3 HandPosition => hand != null ? hand.position : Vector3.zero;

        // weight 0 = leave the animated arm, 1 = hand on target.
        public void Reach(Vector3 target, Transform body, float weight)
        {
            if (!IsValid || weight <= 0f) return;
            Vector3 root = upper.position, mid = lower.position, end = hand.position;
            Vector3 goal = Vector3.Lerp(end, target, Mathf.Clamp01(weight));
            // Elbow down, a little out and back: how arms carry a ball.
            Vector3 pole = root + Vector3.down * 0.5f + body.right * (0.3f * side) - body.forward * 0.2f;
            TwoBoneIK.Solve(root, mid, end, goal, pole, out Vector3 newMid, out Vector3 newEnd);

            upper.rotation = Quaternion.FromToRotation(mid - root, newMid - root) * upper.rotation;
            Vector3 midNow = lower.position, endNow = hand.position;
            lower.rotation = Quaternion.FromToRotation(endNow - midNow, newEnd - midNow) * lower.rotation;
        }
    }
}
