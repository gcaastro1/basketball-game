using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

// Builds a real arena + players + MatchSimulation driven by scripted controllers, with
// aim error switched off so outcomes are deterministic.
public sealed class TestMatch : IDisposable
{
    public readonly CourtConfig Court = ScriptableObject.CreateInstance<CourtConfig>();
    public readonly MatchRules Rules = ScriptableObject.CreateInstance<MatchRules>();
    public readonly BallConfig BallConfig = ScriptableObject.CreateInstance<BallConfig>();
    public readonly ShotConfig ShotConfig = ScriptableObject.CreateInstance<ShotConfig>();
    public readonly DefenseConfig DefenseConfig = ScriptableObject.CreateInstance<DefenseConfig>();
    public readonly List<string> Events = new List<string>();
    public readonly List<ShotReport> Shots = new List<ShotReport>();

    public Arena Arena { get; private set; }
    public MatchSimulation Sim { get; private set; }
    public List<PlayerEntity> Players { get; } = new List<PlayerEntity>();

    public TestMatch()
    {
        ShotConfig.jumpShotBaseError = 0f;
        ShotConfig.jumpShotErrorPerMeter = 0f;
        ShotConfig.layupBaseError = 0f;
        Rules.restartDelaySeconds = 0.5f;
    }

    public void Start(params (TeamId team, Func<MatchSnapshot, int, PlayerCommand> decide)[] roster)
    {
        Arena = PlaceholderArenaBuilder.Build(Court, BallConfig, Rules.threePointRadius);
        var controllers = new List<IAgentController>();
        var movement = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        foreach (var (team, decide) in roster)
        {
            Players.Add(PlaceholderPlayerFactory.Create($"{team}_{Players.Count}", team, movement, Color.gray, Color.yellow));
            controllers.Add(new Scripted(decide));
        }
        Sim = new MatchSimulation(Players, controllers, Arena.Ball, Arena.Hoop, Court, Rules, BallConfig, ShotConfig, DefenseConfig, new System.Random(1));
        Sim.OnMatchEvent += Events.Add;
        Sim.ShotReports.OnShotTaken += Shots.Add;
        Sim.Begin();
    }

    // Ticks the simulation once per frame until the condition holds or time runs out.
    public IEnumerator RunUntil(Func<bool> condition, float timeoutSeconds)
    {
        float elapsed = 0f;
        while (!condition() && elapsed < timeoutSeconds)
        {
            yield return null;
            Sim.Tick(Time.deltaTime);
            elapsed += Time.deltaTime;
        }
    }

    public bool HasEvent(string prefix) => Events.Exists(e => e.StartsWith(prefix));

    public void Dispose()
    {
        Sim?.Dispose();
        if (Arena.Root != null) UnityEngine.Object.Destroy(Arena.Root);
        foreach (var p in Players) if (p != null) UnityEngine.Object.Destroy(p.gameObject);
    }

    private sealed class Scripted : IAgentController
    {
        private readonly Func<MatchSnapshot, int, PlayerCommand> decide;
        public Scripted(Func<MatchSnapshot, int, PlayerCommand> decide) => this.decide = decide;
        public PlayerCommand Decide(MatchSnapshot s, int self) => decide(s, self);
    }

    // ---- common scripts ----

    public static PlayerCommand Idle(MatchSnapshot s, int self) => PlayerCommand.None;

    // Holds shoot once it has had the ball for a moment; lets go at the jump apex.
    public static Func<MatchSnapshot, int, PlayerCommand> ShootAtApex(bool sprint = false)
    {
        float heldSince = -1f;
        return (s, self) =>
        {
            if (s.BallHolderIndex != self) { heldSince = -1f; return PlayerCommand.None; }
            if (heldSince < 0f) heldSince = s.Time;
            if (s.Time - heldSince < 0.3f) return PlayerCommand.None;
            bool atApex = !s.IsGrounded(self) && s.GetVelocity(self).y <= 0f;
            return new PlayerCommand(Vector2.zero, sprint: sprint, shootHeld: !atApex);
        };
    }
}
