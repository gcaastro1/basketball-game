using UnityEngine;
using UnityEngine.InputSystem;
using Basket.Core;

namespace Basket.Input
{
    public class HumanInputProvider : MonoBehaviour, IPlayerAgent
    {
        public Vector2 GetMoveInput()
        {
            Vector2 kb = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) kb.y += 1f;
                if (Keyboard.current.sKey.isPressed) kb.y -= 1f;
                if (Keyboard.current.dKey.isPressed) kb.x += 1f;
                if (Keyboard.current.aKey.isPressed) kb.x -= 1f;
            }
            Vector2 pad = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            Vector2 combined = kb.sqrMagnitude >= pad.sqrMagnitude ? kb : pad;
            return Vector2.ClampMagnitude(combined, 1f);
        }

        public bool WantsSprint() =>
            (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);

        public bool WantsDribbleAction() => false;

        public bool WantsPass() =>
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);

        public bool WantsShoot() =>
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }
}
