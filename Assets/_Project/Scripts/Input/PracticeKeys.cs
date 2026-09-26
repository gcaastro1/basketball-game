using UnityEngine.InputSystem;

namespace Basket.Input
{
    // Keys of the practice court's tools (Z / X / C: free in the game's bindings).
    public static class PracticeKeys
    {
        public static bool CycleSpeed => Pressed(Keyboard.current?.zKey);
        public static bool TogglePause => Pressed(Keyboard.current?.xKey);
        public static bool CycleView => Pressed(Keyboard.current?.cKey);

        private static bool Pressed(UnityEngine.InputSystem.Controls.KeyControl key) => key != null && key.wasPressedThisFrame;
    }
}
