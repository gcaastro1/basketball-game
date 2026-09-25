using UnityEngine;

namespace Basket.Gameplay
{
    public static class HoopMath
    {
        // True when the ball's center moved from at/above the rim plane to below it, and
        // crossed the plane inside the rim. Balls coming up from below never count.
        public static bool IsScoringCrossing(Vector3 previous, Vector3 current, Vector3 rimCenter, float rimRadius)
        {
            if (!(previous.y >= rimCenter.y && current.y < rimCenter.y)) return false;

            float t = (previous.y - rimCenter.y) / (previous.y - current.y);
            Vector3 atPlane = Vector3.Lerp(previous, current, t);
            float dx = atPlane.x - rimCenter.x;
            float dz = atPlane.z - rimCenter.z;
            return dx * dx + dz * dz < rimRadius * rimRadius;
        }
    }
}
