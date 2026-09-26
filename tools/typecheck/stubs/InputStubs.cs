namespace UnityEngine.InputSystem.Controls {
  public class ButtonControl { public bool isPressed => false; public bool wasPressedThisFrame => false; public bool wasReleasedThisFrame => false; }
  public class KeyControl : ButtonControl {}
  public class StickControl { public UnityEngine.Vector2 ReadValue() => default; }
}
namespace UnityEngine.InputSystem {
  using UnityEngine.InputSystem.Controls;
  public class InputDevice {}
  public class Keyboard : InputDevice { public static Keyboard current; public KeyControl wKey,aKey,sKey,dKey,eKey,qKey,rKey,fKey,zKey,xKey,cKey,spaceKey,leftShiftKey,tabKey,enterKey,escapeKey,f1Key; }
  public class Gamepad : InputDevice { public static Gamepad current; public StickControl leftStick,rightStick; public ButtonControl leftStickButton,buttonWest,buttonSouth,buttonNorth,buttonEast,leftShoulder,rightShoulder,startButton,selectButton; }
  public static class InputSystem { public static T AddDevice<T>() where T: InputDevice, new() => new T(); public static void Update(){} }
  public class InputTestFixture { public virtual void Setup(){} public virtual void TearDown(){} public void Press(ButtonControl c, double time = -1, double timeOffset = 0, bool queueEventOnly = false){} public void Release(ButtonControl c, double time = -1, double timeOffset = 0, bool queueEventOnly = false){} public void PressAndRelease(ButtonControl c, double time = -1, double timeOffset = 0, bool queueEventOnly = false){} }
}
namespace UnityEngine.InputSystem.LowLevel { public class _Dummy {} }
namespace UnityEngine.InputSystem {
  public enum InputActionType { Value, Button, PassThrough }
  public class InputAction : System.IDisposable {
    public InputAction(string name = null, InputActionType type = default, string binding = null) {}
    public BindingSyntax AddBinding(string path) => default;
    public CompositeSyntax AddCompositeBinding(string composite) => default;
    public void Enable() {} public void Disable() {} public void Dispose() {}
    public T ReadValue<T>() where T : struct => default;
    public bool IsPressed() => false; public bool WasPressedThisFrame() => false; public bool WasReleasedThisFrame() => false;
    public struct BindingSyntax {}
    public struct CompositeSyntax { public CompositeSyntax With(string part, string path) => this; }
  }
}
