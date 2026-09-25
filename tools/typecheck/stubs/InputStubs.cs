namespace UnityEngine.InputSystem.Controls {
  public class ButtonControl { public bool isPressed => false; public bool wasPressedThisFrame => false; public bool wasReleasedThisFrame => false; }
  public class KeyControl : ButtonControl {}
  public class StickControl { public UnityEngine.Vector2 ReadValue() => default; }
}
namespace UnityEngine.InputSystem {
  using UnityEngine.InputSystem.Controls;
  public class InputDevice {}
  public class Keyboard : InputDevice { public static Keyboard current; public KeyControl wKey,aKey,sKey,dKey,eKey,qKey,rKey,fKey,spaceKey,leftShiftKey,tabKey,enterKey,escapeKey,f1Key; }
  public class Gamepad : InputDevice { public static Gamepad current; public StickControl leftStick,rightStick; public ButtonControl leftStickButton,buttonWest,buttonSouth,buttonNorth,buttonEast,leftShoulder,rightShoulder,startButton,selectButton; }
  public static class InputSystem { public static T AddDevice<T>() where T: InputDevice, new() => new T(); }
  public class InputTestFixture { public virtual void Setup(){} public virtual void TearDown(){} public void Press(ButtonControl c){} public void Release(ButtonControl c){} }
}
namespace UnityEngine.InputSystem.LowLevel { public class _Dummy {} }
