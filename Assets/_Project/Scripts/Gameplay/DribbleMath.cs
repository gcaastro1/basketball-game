using UnityEngine;

namespace Basket.Gameplay
{
    public static class DribbleMath
    {
        public static float ComputeBounceOffsetY(float phase, float bounceHeight)
        {
            float bob = Mathf.Abs(Mathf.Sin(phase)) * bounceHeight;
            return -(bounceHeight - bob);
        }
    }
}
