using UnityEngine;

namespace Basket.Gameplay
{
    public static class ScoringMath
    {
        // Distance is measured on the floor plane from the releaser's feet to the point
        // under the rim center. On the line counts as beyond it for now (provisional).
        public static int PointsForRelease(Vector3 releaseFeetPosition, Vector3 rimCenter, float threePointRadius, int pointsInside, int pointsBeyond)
        {
            float dx = releaseFeetPosition.x - rimCenter.x;
            float dz = releaseFeetPosition.z - rimCenter.z;
            return dx * dx + dz * dz >= threePointRadius * threePointRadius ? pointsBeyond : pointsInside;
        }
    }
}
