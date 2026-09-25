namespace Basket.Gameplay
{
    // Period clock. Counts down only while told to run; expiry is reported, the caller
    // decides what it means (buzzer beaters, end of period, overtime).
    public sealed class GameClock
    {
        public float Remaining { get; private set; }
        public int Period { get; private set; }
        public bool IsOvertime { get; private set; }
        public bool Expired => Remaining <= 0f;

        public void StartPeriod(int period, float lengthSeconds, bool overtime)
        {
            Period = period;
            Remaining = lengthSeconds;
            IsOvertime = overtime;
        }

        public void Tick(float dt)
        {
            if (Remaining > 0f) Remaining = System.Math.Max(0f, Remaining - dt);
        }
    }

    // Counts down only while a team has the ball in hand or in a pass.
    public sealed class ShotClock
    {
        public float Remaining { get; private set; }
        public bool Expired => Remaining <= 0f;

        public void Reset(float seconds) => Remaining = seconds;

        public void Tick(float dt, bool possessed)
        {
            if (possessed && Remaining > 0f) Remaining = System.Math.Max(0f, Remaining - dt);
        }
    }
}
