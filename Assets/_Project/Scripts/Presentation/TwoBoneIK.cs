using UnityEngine;

namespace Basket.Presentation
{
    // Analytic two-bone IK (shoulder-elbow-hand, hip-knee-foot) on positions: bone
    // lengths are kept, the chain end goes to the target (or as far as it reaches) and
    // the middle joint bends toward the pole hint. Rig-independent: the caller rotates
    // its bones onto the solved positions.
    public static class TwoBoneIK
    {
        public static void Solve(Vector3 root, Vector3 mid, Vector3 end, Vector3 target, Vector3 pole,
            out Vector3 newMid, out Vector3 newEnd)
        {
            float upper = (mid - root).magnitude;
            float lower = (end - mid).magnitude;
            Vector3 toTarget = target - root;
            float distance = toTarget.magnitude;
            Vector3 dir = distance > 1e-5f ? toTarget / distance : (end - root).normalized;

            // Keep a sliver of bend at both extremes so the joint never flips.
            float minReach = Mathf.Abs(upper - lower) + 1e-4f;
            float maxReach = upper + lower - 1e-4f;
            float d = Mathf.Clamp(distance, minReach, maxReach);

            // Angle at the root between the chain direction and the upper bone.
            float cosA = Mathf.Clamp((upper * upper + d * d - lower * lower) / (2f * upper * d), -1f, 1f);
            float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));

            Vector3 bend = Perpendicular(pole - root, dir);
            if (bend.sqrMagnitude < 1e-8f) bend = Perpendicular(mid - root, dir);
            if (bend.sqrMagnitude < 1e-8f) bend = Perpendicular(Vector3.up, dir);
            if (bend.sqrMagnitude < 1e-8f) bend = Perpendicular(Vector3.forward, dir);
            bend = bend.normalized;

            newMid = root + dir * (upper * cosA) + bend * (upper * sinA);
            newEnd = root + dir * d;
        }

        // Component of v perpendicular to the unit vector axis.
        private static Vector3 Perpendicular(Vector3 v, Vector3 axis) => v - axis * Vector3.Dot(v, axis);
    }

    public static class ModelFitting
    {
        // Uniform scale that makes a model of the given bounds `targetHeight` tall, and
        // the vertical offset (after scaling) that puts its lowest point on the feet.
        public static void Fit(float boundsMinY, float boundsMaxY, float targetHeight, out float scale, out float yOffset)
        {
            float height = boundsMaxY - boundsMinY;
            scale = height > 1e-4f ? targetHeight / height : 1f;
            yOffset = -boundsMinY * scale;
        }
    }
}
