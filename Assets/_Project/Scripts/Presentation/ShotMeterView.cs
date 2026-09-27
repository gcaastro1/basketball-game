using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

namespace Basket.Presentation
{
    // NBA 2K-style shot meter beside the controlled player: the bar rises from the jump
    // (bottom) to the top at the jump apex, where the green window is; holding on past it the
    // bar falls back down (late). The green's size is the one this shot would have now
    // (attribute, contest, distance, movement). After the release it shows where the release
    // landed: muito cedo / cedo / pouco cedo / perfeito / pouco tarde / tarde / muito tarde.
    public class ShotMeterView : MonoBehaviour
    {
        private const float ResultSeconds = 1.2f;
        private const float BarWidth = 14f, BarHeight = 120f;

        private MatchSimulation sim;
        private PlayerEntity player;
        private Camera cam;
        private Texture2D pixel;
        private ShotMeterReading lastReading;
        private bool hadReading;

        public void Configure(MatchSimulation simulation, PlayerEntity followed)
        {
            sim = simulation;
            player = followed;
        }

        // Bar level (0 bottom .. 1 top) at a time since takeoff: up to the top at the apex, then
        // back down at the same pace (a late release shows as a falling bar).
        public static float BarPosition(float elapsed, float apexTime)
        {
            if (apexTime <= 0.0001f) return 1f;
            float up = elapsed / apexTime;
            return Mathf.Clamp01(up <= 1f ? up : 2f - up);
        }

        // Green zone at the top of the bar: from the level of (apex - green) to the top.
        public static float GreenBottom(float apexTime, float greenHalfWidth) =>
            BarPosition(apexTime - greenHalfWidth, apexTime);

        public static string Label(ShotTimingGrade grade) => grade switch
        {
            ShotTimingGrade.VeryEarly => "MUITO CEDO",
            ShotTimingGrade.Early => "CEDO",
            ShotTimingGrade.SlightlyEarly => "POUCO CEDO",
            ShotTimingGrade.Perfect => "PERFEITO!",
            ShotTimingGrade.SlightlyLate => "POUCO TARDE",
            ShotTimingGrade.Late => "TARDE",
            ShotTimingGrade.VeryLate => "MUITO TARDE",
            _ => "",
        };

        private void OnGUI()
        {
            if (sim == null || player == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }

            bool live = sim.TryGetShotMeter(player.Index, out ShotMeterReading reading);
            if (live)
            {
                lastReading = reading;
                hadReading = true;
            }
            bool result = !live && hadReading && sim.TryGetLastShotRelease(player.Index, ResultSeconds, out _, out _);
            if (!live && !result) return;

            Vector3 screen = cam.WorldToScreenPoint(player.transform.position + Vector3.up * 1.4f + cam.transform.right * 0.7f);
            if (screen.z <= 0f) return;
            var bar = new Rect(screen.x, Screen.height - screen.y - BarHeight * 0.5f, BarWidth, BarHeight);
            ShotMeterReading r = live ? reading : lastReading;

            Draw(new Rect(bar.x - 2f, bar.y - 2f, bar.width + 4f, bar.height + 4f), new Color(0f, 0f, 0f, 0.6f));
            // Green window at the top.
            float gBottom = GreenBottom(r.ApexTime, r.GreenHalfWidth);
            float gHeight = Mathf.Max(3f, (1f - gBottom) * bar.height);
            Draw(new Rect(bar.x, bar.y, bar.width, gHeight), new Color(0.1f, 0.85f, 0.2f, 0.95f));

            float elapsed = live ? r.Elapsed : r.ApexTime + LastTiming();
            float level = BarPosition(elapsed, r.ApexTime);
            bool falling = elapsed > r.ApexTime;
            Color fillColor = falling ? new Color(0.95f, 0.45f, 0.15f, 0.85f)
                : Color.Lerp(new Color(0.9f, 0.2f, 0.1f, 0.85f), new Color(0.95f, 0.85f, 0.2f, 0.85f), level);
            Draw(new Rect(bar.x + 3f, bar.yMax - level * bar.height, bar.width - 6f, level * bar.height), fillColor);
            if (live) return;

            sim.TryGetLastShotRelease(player.Index, ResultSeconds, out float timing, out float green);
            ShotTimingGrade grade = sim.GradeRelease(timing, green);
            Draw(new Rect(bar.x - 4f, bar.yMax - level * bar.height - 1.5f, bar.width + 8f, 3f), Color.white);
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            style.normal.textColor = grade == ShotTimingGrade.Perfect ? new Color(0.3f, 1f, 0.3f)
                : grade == ShotTimingGrade.SlightlyEarly || grade == ShotTimingGrade.SlightlyLate ? new Color(1f, 0.9f, 0.3f)
                : new Color(1f, 0.55f, 0.3f);
            GUI.Label(new Rect(bar.xMax + 6f, bar.yMax - level * bar.height - 10f, 130f, 22f), Label(grade), style);
        }

        private float LastTiming()
        {
            sim.TryGetLastShotRelease(player.Index, ResultSeconds, out float timing, out _);
            return timing;
        }

        private void Draw(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = old;
        }
    }
}
