using System;
using UnityEngine.InputSystem;

namespace Basket.Input
{
    // Input actions defined in code (rebindable at runtime with ApplyBindingOverride; a
    // remapping screen is Etapa 9). Primary/Secondary are context-sensitive: with the
    // ball they shoot/pass, without it they jump(block/rebound)/steal.
    public sealed class BasketInputActions : IDisposable
    {
        public readonly InputAction Move;
        public readonly InputAction Sprint;
        public readonly InputAction Primary;
        public readonly InputAction Secondary;

        public BasketInputActions()
        {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Sprint = new InputAction("Sprint", InputActionType.Button);
            Sprint.AddBinding("<Keyboard>/leftShift");
            Sprint.AddBinding("<Gamepad>/rightTrigger");
            Sprint.AddBinding("<Gamepad>/leftStickPress");

            Primary = new InputAction("Primary", InputActionType.Button);
            Primary.AddBinding("<Keyboard>/space");
            Primary.AddBinding("<Gamepad>/buttonWest");

            Secondary = new InputAction("Secondary", InputActionType.Button);
            Secondary.AddBinding("<Keyboard>/e");
            Secondary.AddBinding("<Gamepad>/buttonSouth");

            Move.Enable();
            Sprint.Enable();
            Primary.Enable();
            Secondary.Enable();
        }

        public void Dispose()
        {
            Move.Dispose();
            Sprint.Dispose();
            Primary.Dispose();
            Secondary.Dispose();
        }
    }
}
