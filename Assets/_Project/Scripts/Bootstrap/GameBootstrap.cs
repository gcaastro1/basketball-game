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
        [SerializeField] private DefenseConfig defenseConfig;
        [SerializeField] private PlayerMovementConfig movementConfig;
        [SerializeField] private CameraConfig cameraConfig;
        [SerializeField] private AIConfig aiConfig;

        public MatchSimulation Simulation { get; private set; }
        private readonly List<System.IDisposable> disposables = new List<System.IDisposable>();

        private void Awake()
        {
            EnsureConfigs();

            Arena arena = PlaceholderArenaBuilder.Build(courtConfig, ballConfig, matchRules.threePointRadius);

            // One brain per team coordinates its AI players (and the human's AI teammates).
            var rng = new System.Random();
            var brains = new Dictionary<TeamId, TeamBrain>
            {
                [TeamId.Home] = new TeamBrain(TeamId.Home, matchSetup.slots.Count, aiConfig, rng: rng),
                [TeamId.Away] = new TeamBrain(TeamId.Away, matchSetup.slots.Count, aiConfig, rng: rng),
            };
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
                    var input = new HumanInputProvider();
                    disposables.Add(input);
                    controllers.Add(input);
                    if (cameraTarget == null) cameraTarget = player;
                }
                else
                {
                    // A lone player (1v1) plays without a team brain.
                    TeamBrain brain = CountTeam(slot.team) > 1 ? brains[slot.team] : null;
                    var ai = new AIAgentController(aiConfig, rng, brain);
                    controllers.Add(ai);
                    aiControllers.Add(ai);
                }
            }

            Simulation = new MatchSimulation(players, controllers, arena.Ball, arena.Hoop,
                courtConfig, matchRules, ballConfig, shotConfig, defenseConfig);

            if (cameraTarget == null && players.Count > 0) cameraTarget = players[0];
            BuildCamera(cameraTarget != null ? cameraTarget.transform : arena.Ball.transform);

            var hud = new GameObject("DebugHud").AddComponent<DebugHud>();
            hud.Configure(Simulation.Match.State, arena.Ball, aiControllers, Simulation, Simulation.Stats);

            Simulation.Begin();
        }

        private int CountTeam(TeamId team)
        {
            int n = 0;
            foreach (var s in matchSetup.slots) if (s.team == team) n++;
            return n;
        }

        private void Update()
        {
            Simulation?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Simulation?.Dispose();
            foreach (var d in disposables) d.Dispose();
            disposables.Clear();
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
            defenseConfig = OrDefault(defenseConfig);
            movementConfig = OrDefault(movementConfig);
            cameraConfig = OrDefault(cameraConfig);
            aiConfig = OrDefault(aiConfig);
        }

        private static T OrDefault<T>(T asset) where T : ScriptableObject =>
            asset != null ? asset : ScriptableObject.CreateInstance<T>();
    }
}
