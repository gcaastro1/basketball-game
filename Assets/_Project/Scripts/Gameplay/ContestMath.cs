using UnityEngine;

namespace Basket.Gameplay
{
    public static class ContestMath
    {
        // 0..1 contest of one defender on a shooter: closer, more between shooter and
        // rim, and in the air (hand up) all increase it.
        public static float Contest(Vector3 shooterFeet, Vector3 rim, Vector3 defenderFeet, bool defenderAirborne, float contestRadius, float airborneBonus)
        {
            Vector3 toDefender = Flat(defenderFeet - shooterFeet);
            float distance = toDefender.magnitude;
            if (distance >= contestRadius) return 0f;

            float closeness = 1f - distance / contestRadius;
            Vector3 toRim = Flat(rim - shooterFeet);
            float alignment = distance > 0.001f && toRim.sqrMagnitude > 0.0001f
                ? Mathf.Max(0f, Vector3.Dot(toRim.normalized, toDefender / distance))
                : 1f;
            float front = 0.3f + 0.7f * alignment;
            float air = defenderAirborne ? 1f + airborneBonus : 1f;
            return Mathf.Clamp01(closeness * front * air);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
