using UnityEngine;
using Basket.Input;
using Basket.Presentation;

namespace Basket.Bootstrap
{
    // Practice court (scene 03): tools to look at the animations while playing alone.
    //   Z: game speed 1 / 0.5 / 0.25 / 0.1   X: pause   C: camera (broadcast / side / front / close)
    // The panel shows what the character's animation is playing (PlayerAnimationDriver).
    // Off the broadcast view, movement keeps the broadcast camera's "forward".
    [DefaultExecutionOrder(1000)]
    public class PracticeTools : MonoBehaviour
    {
        private static readonly float[] Speeds = { 1f, 0.5f, 0.25f, 0.1f };
        private static readonly string[] Views = { "broadcast", "side", "front", "close" };

        private PlayerAnimationDriver driver;
        private Transform player;
        private Camera cam;
        private Behaviour broadcast;
        private int speedIndex, view;
        private bool paused;
        private Vector3 moveForward = Vector3.forward;
        private GUIStyle style;

        public Vector3 MoveForward => view == 0 && cam != null ? cam.transform.forward : moveForward;

        public void Configure(PlayerAnimationDriver animationDriver, Transform followed, Camera camera, Behaviour broadcastCamera)
        {
            driver = animationDriver;
            player = followed;
            cam = camera;
            broadcast = broadcastCamera;
        }

        private void Update()
        {
            if (PracticeKeys.CycleSpeed) speedIndex = (speedIndex + 1) % Speeds.Length;
            if (PracticeKeys.TogglePause) paused = !paused;
            if (PracticeKeys.CycleView)
            {
                if (view == 0 && cam != null)
                {
                    Vector3 f = cam.transform.forward;
                    f.y = 0f;
                    if (f.sqrMagnitude > 0.0001f) moveForward = f.normalized;
                }
                view = (view + 1) % Views.Length;
                if (broadcast != null) broadcast.enabled = view == 0;
            }
            Time.timeScale = paused ? 0f : Speeds[speedIndex];
        }

        private void LateUpdate()
        {
            if (view == 0 || cam == null || player == null) return;
            Vector3 fwd = player.forward;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 focus = player.position + Vector3.up * 1.0f;
            Vector3 offset = view switch
            {
                1 => right * 4.5f + Vector3.up * 0.4f,   // side
                2 => fwd * 4.5f + Vector3.up * 0.4f,     // front
                _ => -fwd * 2.2f + right * 0.8f + Vector3.up * 0.9f, // close, over the shoulder
            };
            cam.transform.position = focus + offset;
            cam.transform.LookAt(focus);
        }

        private void OnDestroy() => Time.timeScale = 1f;

        private void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, wordWrap = false };
                style.normal.textColor = Color.white;
            }
            string status = $"TREINO  velocidade x{Speeds[speedIndex]:0.##}{(paused ? "  PAUSADO" : "")}  camera: {Views[view]}\n" +
                            "Z velocidade | X pausa | C camera\n\n" +
                            (driver != null ? driver.DebugDescription() : "(sem modelo de personagem)");
            var content = new GUIContent(status);
            Vector2 size = style.CalcSize(content);
            GUI.Box(new Rect(Screen.width - size.x - 12f, 10f, size.x + 4f, size.y + 4f), content, style);
        }
    }
}
