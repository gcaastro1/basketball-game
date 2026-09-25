namespace Basket.Input
{
    // Remembers a button press for a short window so a press slightly before the action
    // becomes possible (e.g. jump just before landing, steal just before reaching the
    // handler) still counts.
    public sealed class InputBuffer
    {
        private readonly float windowSeconds;
        private float lastPressTime = float.NegativeInfinity;

        public InputBuffer(float windowSeconds)
        {
            this.windowSeconds = windowSeconds;
        }

        public void Press(float time) => lastPressTime = time;
        public bool IsBuffered(float time) => time - lastPressTime <= windowSeconds;
        public void Clear() => lastPressTime = float.NegativeInfinity;
    }
}
