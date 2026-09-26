namespace Basket.Presentation
{
    // How a locomotion clip set is mixed for the body's movement:
    //   idle -> moving by speed (full at the set's moveSpeed);
    //   moving split by direction relative to the facing: forward / backward / left / right;
    //   the forward share turns into running above moveSpeed (or at any speed when the set has
    //   no forward walk clip), and running bends into the turn clips by the turning rate.
    // Missing clips hand their share to the closest one that exists. Playback follows the real
    // speed only a little (0.8-1.15x): scaling it all the way made runs look fast-forwarded.
    public struct LocomotionInput
    {
        public float Speed;        // horizontal m/s
        public float Forward;      // m/s along the facing
        public float Right;        // m/s to the body's right
        public float YawRate;      // degrees/s, positive = turning right
        public float MoveSpeed;
        public float RunSpeed;
        public bool HasForward, HasBackward, HasLeft, HasRight, HasRun, HasTurnLeft, HasTurnRight;
    }

    public struct LocomotionMix
    {
        public float Idle, Forward, Backward, Left, Right, Run, TurnLeft, TurnRight;
        public float MoveRate, RunRate;

        public float Sum => Idle + Forward + Backward + Left + Right + Run + TurnLeft + TurnRight;
        public float Running => Run + TurnLeft + TurnRight;
    }

    public static class LocomotionBlend
    {
        public const float MinRate = 0.8f;
        public const float MaxRate = 1.15f;
        // Turning this fast (degrees/s) or more while running uses the turn clip alone.
        public const float FullTurnRate = 180f;

        public static LocomotionMix Compute(in LocomotionInput i)
        {
            var mix = new LocomotionMix { MoveRate = 1f, RunRate = 1f };
            float moveSpeed = i.MoveSpeed > 0.1f ? i.MoveSpeed : 1.5f;
            float runSpeed = i.RunSpeed > moveSpeed ? i.RunSpeed : moveSpeed + 1f;
            float speed = i.Speed > 0f ? i.Speed : 0f;
            if (speed < 0.01f)
            {
                mix.Idle = 1f;
                return mix;
            }

            float moving = Clamp01(speed / moveSpeed);
            mix.Idle = 1f - moving;

            // Direction shares (cosine lobes), missing clips folded into the nearest.
            float cf = Clamp(i.Forward / speed, -1f, 1f), cr = Clamp(i.Right / speed, -1f, 1f);
            float f = Max0(cf), b = Max0(-cf), r = Max0(cr), l = Max0(-cr);
            bool canForward = i.HasForward || i.HasRun;
            if (!i.HasRight) { if (cf >= 0f || !i.HasBackward) f += r; else b += r; r = 0f; }
            if (!i.HasLeft) { if (cf >= 0f || !i.HasBackward) f += l; else b += l; l = 0f; }
            if (!i.HasBackward) { f += b; b = 0f; }
            if (!canForward) { b += f; f = 0f; }
            float total = f + b + l + r;
            if (total < 0.0001f)
            {
                mix.Idle = 1f;
                return mix;
            }
            f *= moving / total;
            b *= moving / total;
            l *= moving / total;
            r *= moving / total;

            // Forward share: walk clip below moveSpeed, run above (always run without a walk clip).
            float runShare = !i.HasRun ? 0f : !i.HasForward ? 1f : Clamp01((speed - moveSpeed) / (runSpeed - moveSpeed));
            float run = f * runShare;
            mix.Forward = f - run;
            mix.Backward = b;
            mix.Left = l;
            mix.Right = r;

            float turn = Clamp(i.YawRate / FullTurnRate, -1f, 1f);
            mix.TurnRight = i.HasTurnRight ? run * Max0(turn) : 0f;
            mix.TurnLeft = i.HasTurnLeft ? run * Max0(-turn) : 0f;
            mix.Run = run - mix.TurnRight - mix.TurnLeft;

            mix.MoveRate = Rate(speed, moveSpeed);
            mix.RunRate = Rate(speed, runSpeed);
            return mix;
        }

        private static float Rate(float speed, float clipSpeed)
        {
            float r = speed / clipSpeed;
            return r < MinRate ? MinRate : (r > MaxRate ? MaxRate : r);
        }

        private static float Max0(float v) => v > 0f ? v : 0f;
        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
