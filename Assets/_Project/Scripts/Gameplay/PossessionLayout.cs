using UnityEngine;

namespace Basket.Gameplay
{
    // Where players stand when a possession (re)starts. Placeholder for real inbound /
    // check-ball formations, which will come from tactics data.
    public static class PossessionLayout
    {
        // k = 0 is the ball handler at the check spot; k >= 1 alternate left/right around
        // the hoop at the same distance, spreadDegrees further out each pair.
        public static Vector3 OffenseSpot(Vector3 hoopFloor, Vector3 checkSpot, int k, float spreadDegrees)
        {
            if (k <= 0) return checkSpot;
            int side = k % 2 == 1 ? 1 : -1;
            int ring = (k + 1) / 2;
            Quaternion rotation = Quaternion.AngleAxis(side * ring * spreadDegrees, Vector3.up);
            return hoopFloor + rotation * (checkSpot - hoopFloor);
        }

        // Between the attacker and the hoop, gap meters from the attacker.
        public static Vector3 DefenseSpot(Vector3 hoopFloor, Vector3 attackerSpot, float gap)
        {
            Vector3 toHoop = hoopFloor - attackerSpot;
            toHoop.y = 0f;
            float distance = toHoop.magnitude;
            if (distance < 0.001f) return attackerSpot;
            return attackerSpot + toHoop / distance * Mathf.Min(gap, distance);
        }

        // On the line from the hoop toward the check spot, `distance` meters out.
        public static Vector3 AlongCourtAxis(Vector3 hoopFloor, Vector3 checkSpot, float distance)
        {
            Vector3 dir = checkSpot - hoopFloor;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.back;
            return hoopFloor + dir.normalized * distance;
        }

        // Free-throw lane slots: alternating sides of the lane, stepping away from the hoop.
        public static Vector3 LaneSlot(Vector3 hoopFloor, Vector3 checkSpot, int k)
        {
            Vector3 axis = checkSpot - hoopFloor;
            axis.y = 0f;
            axis = axis.sqrMagnitude < 0.0001f ? Vector3.back : axis.normalized;
            Vector3 side = new Vector3(axis.z, 0f, -axis.x);
            float along = 1.8f + (k / 2) * 1.0f;
            float across = (k % 2 == 0 ? 1f : -1f) * 2.3f;
            return hoopFloor + axis * along + side * across;
        }

        public static Vector3 ClampToCourt(Vector3 spot, float width, float depth, float margin)
        {
            float halfWidth = width * 0.5f - margin;
            spot.x = Mathf.Clamp(spot.x, -halfWidth, halfWidth);
            spot.z = Mathf.Clamp(spot.z, margin, depth - margin);
            return spot;
        }
    }
}
