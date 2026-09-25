using UnityEngine;

namespace Basket.Gameplay
{
    public static class BlockMath
    {
        // Raised arms modelled as a vertical segment from head to fingertips.
        public static bool IsWithinArms(Vector3 ball, Vector3 defenderFeet, float headHeight, float reach, float radius)
        {
            float y = Mathf.Clamp(ball.y, defenderFeet.y + headHeight, defenderFeet.y + reach);
            Vector3 closest = new Vector3(defenderFeet.x, y, defenderFeet.z);
            return (ball - closest).sqrMagnitude <= radius * radius;
        }

        // Swats the ball away from the defender, down toward the floor.
        public static Vector3 DeflectVelocity(Vector3 ball, Vector3 defenderFeet, float speed)
        {
            Vector3 away = new Vector3(ball.x - defenderFeet.x, 0f, ball.z - defenderFeet.z);
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            return away.normalized * speed + Vector3.down * (speed * 0.5f);
        }

        public static bool IsFacing(Vector3 defenderForward, Vector3 defenderFeet, Vector3 targetFeet, float minDot)
        {
            Vector3 to = new Vector3(targetFeet.x - defenderFeet.x, 0f, targetFeet.z - defenderFeet.z);
            Vector3 fwd = new Vector3(defenderForward.x, 0f, defenderForward.z);
            if (to.sqrMagnitude < 0.0001f || fwd.sqrMagnitude < 0.0001f) return true;
            return Vector3.Dot(fwd.normalized, to.normalized) >= minDot;
        }
    }
}
