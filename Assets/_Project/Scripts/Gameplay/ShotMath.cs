using System;
using UnityEngine;

namespace Basket.Gameplay
{
    public static class ShotMath
    {
        // Uniformly distributed point in a horizontal disc of the given radius.
        public static Vector3 SampleDiscOffset(float radius, System.Random rng)
        {
            if (radius <= 0f) return Vector3.zero;
            double angle = rng.NextDouble() * Math.PI * 2;
            double dist = Math.Sqrt(rng.NextDouble()) * radius;
            return new Vector3((float)(Math.Cos(angle) * dist), 0f, (float)(Math.Sin(angle) * dist));
        }
    }
}
