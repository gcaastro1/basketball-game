namespace Basket.Presentation
{
    // Weights and playback rates of the locomotion clips for a given ground speed: idle ->
    // walk -> run by speed, with the backward clip taking over as the movement turns away
    // from where the body faces. Playback follows the real speed only a little (0.8-1.15x):
    // scaling it all the way (up to 1.6x) made every run and sprint look fast-forwarded.
    public readonly struct LocomotionWeights
    {
        public readonly float Idle, Walk, Run, Back;
        public readonly float WalkRate, RunRate, BackRate;

        public LocomotionWeights(float idle, float walk, float run, float back, float walkRate, float runRate, float backRate)
        {
            Idle = idle;
            Walk = walk;
            Run = run;
            Back = back;
            WalkRate = walkRate;
            RunRate = runRate;
            BackRate = backRate;
        }
    }

    public static class LocomotionBlend
    {
        public const float MinRate = 0.8f;
        public const float MaxRate = 1.15f;

        // speed: horizontal m/s; forward: its component along the body's facing (m/s).
        public static LocomotionWeights Compute(float speed, float forward, float walkSpeed, float runSpeed, bool hasWalk, bool hasBack)
        {
            speed = speed < 0f ? 0f : speed;
            walkSpeed = walkSpeed > 0.1f ? walkSpeed : 1.5f;
            runSpeed = runSpeed > walkSpeed ? runSpeed : walkSpeed + 1f;

            // Forward (or sideways) gait: idle -> walk -> run.
            float idle, walk = 0f, run;
            if (hasWalk)
            {
                if (speed <= walkSpeed)
                {
                    walk = speed / walkSpeed;
                    idle = 1f - walk;
                    run = 0f;
                }
                else
                {
                    run = Clamp01((speed - walkSpeed) / (runSpeed - walkSpeed));
                    walk = 1f - run;
                    idle = 0f;
                }
            }
            else
            {
                run = Clamp01(speed / runSpeed);
                idle = 1f - run;
            }

            // Backpedal: share of the moving weight that goes to the backward clip.
            float back = 0f;
            if (hasBack && speed > 0.01f)
            {
                float away = Clamp01(-forward / speed);          // 1 straight back, 0 sideways
                back = Clamp01((away - 0.3f) / 0.5f) * (1f - idle);
                float keep = 1f - idle > 0.0001f ? (1f - idle - back) / (1f - idle) : 1f;
                walk *= keep;
                run *= keep;
            }

            float walkRate = Rate(speed, walkSpeed);
            float runRate = Rate(speed, runSpeed);
            return new LocomotionWeights(idle, walk, run, back, walkRate, runRate, runRate);
        }

        private static float Rate(float speed, float clipSpeed)
        {
            float r = speed / clipSpeed;
            return r < MinRate ? MinRate : (r > MaxRate ? MaxRate : r);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
