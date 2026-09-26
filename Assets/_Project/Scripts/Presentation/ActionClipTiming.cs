namespace Basket.Presentation
{
    // Keeps a shot clip in step with the gameplay shot: gameplay says how far the shot is
    // (0 = started .. 1 = release at the jump's apex); the clip goes from its gather (`start`)
    // to its own release point over that span, whatever the clip's length. Mocap shots carry
    // seconds of standing and dribbling before the gather, which this skips.
    public static class ActionClipTiming
    {
        // Seconds into the clip for this shot progress (window in normalized clip time).
        public static float TimeFor(float progress, float start, float release, float clipLength)
        {
            float p = Clamp01(progress);
            float s = Clamp01(start);
            float r = Clamp01(release);
            if (r < s) r = s;
            return clipLength * (s + (r - s) * p);
        }

        // Seconds into the clip `elapsed` seconds after starting at `from` (normalized), holding
        // at `until` (normalized): one-shot actions, and a shot's follow-through after release.
        public static float Play(float from, float until, float elapsed, float clipLength)
        {
            float f = Clamp01(from), u = Clamp01(until);
            if (u < f) u = f;
            float t = f * clipLength + (elapsed > 0f ? elapsed : 0f);
            return t < u * clipLength ? t : u * clipLength;
        }

        // Driven while the shot is on its way to the release; after it the clip plays freely.
        public static bool IsDriven(float progress) => progress >= 0f && progress < 1f;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
