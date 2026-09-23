using System;
using UnityEngine;

namespace Basket.Gameplay
{
    public static class ShotMath
    {
        public static Vector3 ComputeMissOffset(float baseRadius, float rating, System.Random rng)
        {
            float effectiveRadius = baseRadius * (1f - Mathf.Clamp01(rating));
            if (effectiveRadius <= 0f) return Vector3.zero;

            double angle = rng.NextDouble() * Mathf.PI * 2;
            double dist = rng.NextDouble() * effectiveRadius;
            return new Vector3((float)(Math.Cos(angle) * dist), 0f, (float)(Math.Sin(angle) * dist));
        }
    }
}
