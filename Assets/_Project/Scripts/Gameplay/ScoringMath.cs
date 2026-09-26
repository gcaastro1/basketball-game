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

        // The three-point line (arc + straight corners): Core.ThreePointLine.
        public static bool IsBeyondArc(Vector3 feet, Vector3 rimCenter, float radius, float cornerDistance = 0f) =>
            Basket.Core.ThreePointLine.IsBeyond(feet, rimCenter, radius, cornerDistance);
    }
}
