using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Basket.Core;
using Basket.Characters;
using Basket.Gameplay;
using Basket.AI;
using Basket.Input;
using Basket.Presentation;
using Basket.UI;
using Basket.Meta;

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
        [SerializeField] private ProgressionConfig progressionConfig;
        [SerializeField] private AttributeTuning attributeTuning;
        [SerializeField] private ItemCatalog itemCatalog;
        [SerializeField] private CharacterCatalog characterCatalog;
        [SerializeField] private CharacterObtainRules obtainRules;
        [SerializeField] private MatchRewardRules rewardRules;

        public MatchSimulation Simulation { get; private set; }
        private readonly List<System.IDisposable> disposables = new List<System.IDisposable>();
        private ProfileRuntimeService profileService;

        private void Awake()
        {
            EnsureConfigs();

            // Meta (Etapa 8.5): the profile's real roster replaces the Inspector's exhibition
            // MatchSetup when the player already owns enough characters for every slot.
            profileService = new ProfileRuntimeService(
                new SaveService(new FileSaveStorage(Application.persistentDataPath), new JsonSaveSerializer()),
                itemCatalog, characterCatalog, progressionConfig, obtainRules, rewardRules);
            profileService.LoadOrCreate();

            List<CharacterDefinition> ownedRoster = profileService.BuildRosterOrNull(matchSetup.slots.Count);
            if (ownedRoster != null)
            {
                var liveSetup = ScriptableObject.CreateInstance<MatchSetup>();
                liveSetup.slots = new List<MatchSetup.PlayerSlot>();
                for (int i = 0; i < matchSetup.slots.Count; i++)
                {
                    MatchSetup.PlayerSlot original = matchSetup.slots[i];
                    CharacterInstance instance = profileService.Profile.Inventory.GetCharacter(ownedRoster[i].characterId);
                    liveSetup.slots.Add(new MatchSetup.PlayerSlot(original.team, original.control, ownedRoster[i],
                        instance?.level ?? 1, instance?.limitBreak ?? 0, instance?.dupes ?? 0));
                }
                matchSetup = liveSetup;
            }
            // Senão, matchSetup continua sendo o asset configurado no Inspector (fallback: perfil
            // ainda não tem personagens suficientes para preencher todos os slots).

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
                string label = slot.character != null ? slot.character.archetype : slot.control.ToString();
                PlayerEntity player = PlaceholderPlayerFactory.Create(
                    $"{slot.team}_{i}_{label}", slot.team, movementConfig,
                    slot.team == TeamId.Home ? HomeColor : AwayColor, human ? HumanMarker : AIMarker);
                ApplyCharacter(player, slot);
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
                    var ai = new AIAgentController(aiConfig, rng, brain, player.Tendencies);
                    controllers.Add(ai);
                    aiControllers.Add(ai);
                }
            }

            Simulation = new MatchSimulation(players, controllers, arena.Ball, arena.Hoop,
                courtConfig, matchRules, ballConfig, shotConfig, defenseConfig, rng, attributeTuning, arena.SecondHoop);

            // Meta (Etapa 8.5): recompensa de partida pros personagens que jogaram, ao fim da
            // partida. Simulation.Match (o MatchManager) já existe aqui.
            Simulation.Match.OnMatchEnded += finalState =>
            {
                IEnumerable<string> playedIds = matchSetup.slots
                    .Where(s => s.character != null)
                    .Select(s => s.character.characterId);
                profileService.ApplyMatchReward(finalState.ScoreHome, finalState.ScoreAway, playedIds);
            };

            // Character models (Etapa 6): presentation only, attached once the simulation exists.
            for (int i = 0; i < players.Count; i++)
            {
                MatchSetup.PlayerSlot slot = matchSetup.slots[i];
                if (slot.character == null || slot.character.visual == null) continue;
                bool human = slot.control == AgentControlType.Human;
                Color ring = human ? HumanMarker : (slot.team == TeamId.Home ? HomeColor : AwayColor);
                CharacterVisual.Attach(players[i], Simulation, slot.character.visual, ring);
            }

            if (cameraTarget == null && players.Count > 0) cameraTarget = players[0];
            BuildCamera(cameraTarget != null ? cameraTarget.transform : arena.Ball.transform);

            var hud = new GameObject("DebugHud").AddComponent<DebugHud>();
            hud.Configure(Simulation.Match.State, arena.Ball, aiControllers, Simulation, Simulation.Stats);

            Simulation.Begin();
        }

        // Character definition + slot progression -> attributes, abilities, AI tendencies.
        private void ApplyCharacter(PlayerEntity player, MatchSetup.PlayerSlot slot)
        {
            if (slot.character == null) return;
            var instance = new CharacterInstance(slot.character.characterId, Mathf.Max(1, slot.level), slot.limitBreak, slot.dupes);
            AttributeSet attributes = CharacterStatsCalculator.Compute(slot.character, instance, progressionConfig);
            var abilities = new PlayerAbilities(attributes,
                CharacterStatsCalculator.UnlockedAbilities(slot.character, instance),
                CharacterStatsCalculator.AbilityLevel(instance, progressionConfig),
                CharacterStatsCalculator.CooldownMultiplier(instance, progressionConfig));
            player.SetCharacter(slot.character.displayName, attributes, abilities, slot.character.aiTendencies);
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
            profileService?.Save();
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
            progressionConfig = OrDefault(progressionConfig);
            attributeTuning = OrDefault(attributeTuning);
            itemCatalog = OrDefault(itemCatalog);
            characterCatalog = OrDefault(characterCatalog);
            obtainRules = OrDefault(obtainRules);
            rewardRules = OrDefault(rewardRules);
        }

        private static T OrDefault<T>(T asset) where T : ScriptableObject =>
            asset != null ? asset : ScriptableObject.CreateInstance<T>();
    }
}
