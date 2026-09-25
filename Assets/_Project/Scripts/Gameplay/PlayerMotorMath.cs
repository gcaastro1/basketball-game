using UnityEngine;

namespace Basket.Gameplay
{
    public static class PlayerMotorMath
    {
        public static Vector3 ComputeVelocity(Vector3 currentVelocity, Vector3 desiredDirection, float maxSpeed, float acceleration, float deceleration, float dt)
        {
            Vector3 desiredVelocity = desiredDirection.sqrMagnitude > 0.0001f
                ? desiredDirection.normalized * maxSpeed
                : Vector3.zero;
            float rate = desiredDirection.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            return Vector3.MoveTowards(currentVelocity, desiredVelocity, rate * dt);
        }
    }
}
