using UnityEngine;
using Basket.Gameplay;

namespace Basket.Presentation
{
    // NBA 2K-style shot meter beside the controlled player: a bar that fills from the jump
    // (bottom) to the apex (the top of the green window's middle); the green window's size is
    // the one this shot would have now (attribute, contest, distance, movement). After the
    // release it shows where the release landed: GREEN / early / late.
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

        // Bar position (0 bottom .. 1 top) of a time since takeoff: the apex sits at 0.8, so the
        // green window and a late release both stay on the bar.
        public static float BarPosition(float elapsed, float apexTime) =>
            apexTime > 0.0001f ? Mathf.Clamp01(elapsed / apexTime * 0.8f) : 1f;

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
            // Green window around the apex.
            float gTop = BarPosition(r.ApexTime + r.GreenHalfWidth, r.ApexTime);
            float gBottom = BarPosition(r.ApexTime - r.GreenHalfWidth, r.ApexTime);
            float gHeight = Mathf.Max(2f, (gTop - gBottom) * bar.height);
            Draw(new Rect(bar.x, bar.yMax - gTop * bar.height, bar.width, gHeight), new Color(0.1f, 0.85f, 0.2f, 0.95f));

            if (live)
            {
                float fill = BarPosition(r.Elapsed, r.ApexTime);
                Color c = Color.Lerp(new Color(0.9f, 0.2f, 0.1f, 0.85f), new Color(0.95f, 0.85f, 0.2f, 0.85f), fill / 0.8f);
                Draw(new Rect(bar.x + 3f, bar.yMax - fill * bar.height, bar.width - 6f, fill * bar.height), c);
                return;
            }

            sim.TryGetLastShotRelease(player.Index, ResultSeconds, out float timing, out float green);
            float at = BarPosition(r.ApexTime + timing, r.ApexTime);
            Draw(new Rect(bar.x - 4f, bar.yMax - at * bar.height - 1.5f, bar.width + 8f, 3f), Color.white);
            bool isGreen = green > 0f && Mathf.Abs(timing) <= green;
            string label = isGreen ? "VERDE!" : timing < 0f ? "cedo" : "tarde";
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            style.normal.textColor = isGreen ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.6f, 0.3f);
            GUI.Label(new Rect(bar.xMax + 6f, bar.yMax - at * bar.height - 10f, 90f, 22f), label, style);
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
