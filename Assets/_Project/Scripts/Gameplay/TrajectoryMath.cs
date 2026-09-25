using UnityEngine;

namespace Basket.Gameplay
{
    public static class TrajectoryMath
    {
        public static Vector3 ComputeArcVelocity(Vector3 origin, Vector3 target, float apexHeight, float gravity)
        {
            float g = Mathf.Abs(gravity);
            Vector3 displacement = target - origin;
            Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
            float peakHeight = Mathf.Max(apexHeight, 0.01f);

            // Apex sits peakHeight above whichever of origin/target is higher, so the
            // trajectory is guaranteed to still be rising until it clears both endpoints.
            // (An earlier version measured peakHeight from origin only, which silently
            // produced a below-target apex — and a physically broken landing — whenever
            // the target sat more than apexHeight above the origin, e.g. a shot arc to a
            // rim well above the shooter's release height.)
            float apexAboveOrigin = peakHeight + Mathf.Max(0f, displacement.y);
            float apexAboveTarget = apexAboveOrigin - displacement.y;

            float timeUp = Mathf.Sqrt(2f * apexAboveOrigin / g);
            float timeDown = Mathf.Sqrt(2f * apexAboveTarget / g);
            float totalTime = timeUp + timeDown;

            Vector3 velocityXZ = displacementXZ / totalTime;
            float velocityY = g * timeUp;

            return velocityXZ + Vector3.up * velocityY;
        }
    }
}
