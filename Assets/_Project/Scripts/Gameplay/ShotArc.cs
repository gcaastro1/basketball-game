using UnityEngine;

namespace Basket.Gameplay
{
    // How high a jump shot flies: a parabola whose apex sits H above the rim, over a
    // horizontal distance D, comes down into the rim at an angle with tan = 4H / D. So a fixed
    // entry angle gives H = D * tan(angle) / 4 -- ~1.9 m above the rim for a three, ~1 m for a
    // free throw (real shots enter at ~45 degrees). A fixed 3.5 m apex sent every shot up to
    // ~6.5 m and into the rim at ~63 degrees.
    public static class ShotArc
    {
        public static float ApexAboveRim(float horizontalDistance, ShotConfig c)
        {
            float angle = Mathf.Clamp(c.entryAngleDegrees, 20f, 80f) * Mathf.Deg2Rad;
            float h = Mathf.Max(0f, horizontalDistance) * Mathf.Tan(angle) / 4f;
            return Mathf.Clamp(h, c.minArcHeight, Mathf.Max(c.minArcHeight, c.maxArcHeight));
        }
    }
}
