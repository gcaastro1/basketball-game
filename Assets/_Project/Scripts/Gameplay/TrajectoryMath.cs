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

            float timeUp = Mathf.Sqrt(2f * peakHeight / g);
            float timeDown = Mathf.Sqrt(2f * Mathf.Max(peakHeight - displacement.y, 0.01f) / g);
            float totalTime = timeUp + timeDown;

            Vector3 velocityXZ = displacementXZ / totalTime;
            float velocityY = g * timeUp;

            return velocityXZ + Vector3.up * velocityY;
        }
    }
}
