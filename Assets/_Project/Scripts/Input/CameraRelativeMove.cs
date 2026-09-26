using UnityEngine;

namespace Basket.Input
{
    // Stick/WASD input is relative to the camera: "up" moves where the camera looks, whichever
    // end of the court it faces. PlayerCommand.Move is in world axes (x, z), so the input is
    // turned by the camera's heading on the floor.
    public static class CameraRelativeMove
    {
        public static Vector2 Rotate(Vector2 input, Vector3 viewForward)
        {
            float fx = viewForward.x, fz = viewForward.z;
            float length = Mathf.Sqrt(fx * fx + fz * fz);
            if (length < 0.01f) return input;
            fx /= length;
            fz /= length;
            // right = (fz, -fx); world = right * x + forward * y
            return new Vector2(fz * input.x + fx * input.y, -fx * input.x + fz * input.y);
        }
    }
}
