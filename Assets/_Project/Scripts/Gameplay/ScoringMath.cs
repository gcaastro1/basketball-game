using UnityEngine;

namespace Basket.Gameplay
{
    public static class ScoringMath
    {
        // Distance is measured on the floor plane from the releaser's feet to the point
        // under the rim center. On the line counts as beyond it for now (provisional).
        public static int PointsForRelease(Vector3 releaseFeetPosition, Vector3 rimCenter, float threePointRadius, int pointsInside, int pointsBeyond,
            float cornerDistance = 0f)
        {
            return IsBeyondArc(releaseFeetPosition, rimCenter, threePointRadius, cornerDistance) ? pointsBeyond : pointsInside;
        }

        // The three-point line: an arc of `radius` around the point under the rim, cut by two
        // straight corner lines `cornerDistance` to each side of it (NBA court: 7.24 m / 6.71 m).
        // The corner lines run from the baseline to where they meet the arc, i.e. while the
        // along-court distance to the rim is at most sqrt(radius^2 - corner^2). There the side
        // distance decides; elsewhere the plain distance does. cornerDistance 0 = arc only.
        public static bool IsBeyondArc(Vector3 feet, Vector3 rimCenter, float radius, float cornerDistance = 0f)
        {
            float dx = feet.x - rimCenter.x;
            float dz = feet.z - rimCenter.z;
            if (cornerDistance > 0f && cornerDistance < radius)
            {
                float cornerLength = Mathf.Sqrt(radius * radius - cornerDistance * cornerDistance);
                if (Mathf.Abs(dz) <= cornerLength) return Mathf.Abs(dx) >= cornerDistance;
            }
            return dx * dx + dz * dz >= radius * radius;
        }
    }
}
