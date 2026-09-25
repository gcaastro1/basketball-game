using System.Collections.Generic;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;
using Basket.AI;
using Basket.Input;
using Basket.UI;

namespace Basket.Bootstrap
{
    // Composition root only: builds the arena, spawns the roster from MatchSetup, picks a
    // controller per slot, wires systems together, then forwards Update to the
    // simulation. No gameplay logic lives here.
    // Any config left unassigned falls back to that config's code defaults, so the scene
    // stays valid even before the assets are wired.
    public class GameBootstrap : MonoBehaviour
    {
        private static readonly Color HomeColor = new Color(0.2f, 0.4f, 0.9f);
        private static readonly Color AwayColor = new Color(0.9f, 0.25f, 0.2f);
        private static readonly Color HumanMarker = new Color(1f, 0.9f, 0.1f);
        private static readonly Color AIMarker = new Color(0.15f, 0.15f, 0.15f);

        [SerializeField] private MatchSetup matchSetup;
        [SerializeField] private MatchRules matchRules;
        [SerializeField] private CourtConfig courtConfig;
        [SerializeField] private BallConfig ballConfig;
        [SerializeField] private ShotConfig shotConfig;
        [SerializeField] private PlayerMovementConfig movementConfig;
        [SerializeField] private CameraConfig cameraConfig;
        [SerializeField] private AIConfig aiConfig;

        public MatchSimulation Simulation { get; private set; }

        private void Awake()
        {
            EnsureConfigs();

            Arena arena = PlaceholderArenaBuilder.Build(courtConfig, ballConfig, matchRules.threePointRadius);

            var players = new List<PlayerEntity>();
            var controllers = new List<IAgentController>();
            var aiControllers = new List<IAIController>();
            PlayerEntity cameraTarget = null;

            for (int i = 0; i < matchSetup.slots.Count; i++)
            {
                MatchSetup.PlayerSlot slot = matchSetup.slots[i];
                bool human = slot.control == AgentControlType.Human;
                PlayerEntity player = PlaceholderPlayerFactory.Create(
                    $"{slot.team}_{i}_{slot.control}", slot.team, movementConfig,
                    slot.team == TeamId.Home ? HomeColor : AwayColor, human ? HumanMarker : AIMarker);
                players.Add(player);

                if (human)
                {
                    controllers.Add(new HumanInputProvider());
                    if (cameraTarget == null) cameraTarget = player;
                }
                else
                {
                    var ai = new AIAgentController(aiConfig);
                    controllers.Add(ai);
                    aiControllers.Add(ai);
                }
            }

            Simulation = new MatchSimulation(players, controllers, arena.Ball, arena.Hoop,
                courtConfig, matchRules, ballConfig, shotConfig);

            if (cameraTarget == null && players.Count > 0) cameraTarget = players[0];
            BuildCamera(cameraTarget != null ? cameraTarget.transform : arena.Ball.transform);

            var hud = new GameObject("DebugHud").AddComponent<DebugHud>();
            hud.Configure(Simulation.Match.State, arena.Ball, aiControllers);

            Simulation.Begin();
        }

        private void Update()
        {
            Simulation?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Simulation?.Dispose();
        }

        private void BuildCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            if (!cam.TryGetComponent<CameraController>(out var controller))
            {
                controller = cam.gameObject.AddComponent<CameraController>();
            }
            controller.Configure(target, cameraConfig);
        }

        private void EnsureConfigs()
        {
            matchSetup = OrDefault(matchSetup);
            matchRules = OrDefault(matchRules);
            courtConfig = OrDefault(courtConfig);
            ballConfig = OrDefault(ballConfig);
            shotConfig = OrDefault(shotConfig);
            movementConfig = OrDefault(movementConfig);
            cameraConfig = OrDefault(cameraConfig);
            aiConfig = OrDefault(aiConfig);
        }

        private static T OrDefault<T>(T asset) where T : ScriptableObject =>
            asset != null ? asset : ScriptableObject.CreateInstance<T>();
    }
}
