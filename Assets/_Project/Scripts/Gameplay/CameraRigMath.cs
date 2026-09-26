using UnityEngine;

namespace Basket.Gameplay
{
    // Broadcast-style follow camera ("2K cam"): behind and above the followed player,
    // always looking down the court toward the basket being attacked. Pure math; the
    // CameraController smooths it.
    public static class CameraRigMath
    {
        public readonly struct Rig
        {
            public readonly Vector3 Position;
            public readonly Vector3 LookAt;

            public Rig(Vector3 position, Vector3 lookAt)
            {
                Position = position;
                LookAt = lookAt;
            }
        }

        // Unit direction the camera looks along (flat): the court axis toward the attacked
        // basket, turned toward the basket itself by `aimAtHoop` (0 = straight down the
        // court like a broadcast, 1 = always right behind the player's line to the rim).
        public static Vector3 Direction(Vector3 focus, Vector3 hoop, Vector3 courtCenter, float aimAtHoop)
        {
            Vector3 axis = Flat(hoop - courtCenter);
            axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
            Vector3 toHoop = Flat(hoop - focus);
            if (toHoop.sqrMagnitude < 0.0001f) return axis;
            Vector3 dir = Vector3.Lerp(axis, toHoop.normalized, Mathf.Clamp01(aimAtHoop));
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : axis;
        }

        // Camera behind `focus` along `direction`, looking at a point ahead of the player
        // toward the basket (never past it), so the rim stays in frame.
        public static Rig Place(Vector3 focus, Vector3 hoop, Vector3 direction, CameraConfig c)
        {
            float toHoop = Vector3.Dot(Flat(hoop - focus), direction);
            float ahead = Mathf.Clamp(toHoop * c.lookAhead, 0f, c.maxLookAhead);
            Vector3 lookAt = focus + direction * ahead + Vector3.up * c.lookHeight;
            Vector3 position = focus - direction * c.distance + Vector3.up * c.height;
            return new Rig(position, lookAt);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
