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
    // The same events stamped with the simulated time and the shot clock, to see where a
    // long simulation spent its time.
    public readonly List<string> Timeline = new List<string>();
    public float SimulatedTime { get; private set; }
    public readonly List<ShotReport> Shots = new List<ShotReport>();

    public Arena Arena { get; private set; }
    public MatchSimulation Sim { get; private set; }
    public List<PlayerEntity> Players { get; } = new List<PlayerEntity>();

    public TestMatch()
    {
        ShotConfig.jumpShotBaseError = 0f;
        ShotConfig.jumpShotErrorPerMeter = 0f;
        ShotConfig.layupBaseError = 0f;
        ShotConfig.freeThrowBaseError = 0f;
        Rules.restartDelaySeconds = 0.5f;
    }

    public void Start(params (TeamId team, Func<MatchSnapshot, int, PlayerCommand> decide)[] roster)
    {
        var teams = new List<TeamId>();
        var controllers = new List<IAgentController>();
        foreach (var (team, decide) in roster)
        {
            teams.Add(team);
            controllers.Add(new Scripted(decide));
        }
        Start(teams, controllers);
    }

    public void Start(IList<TeamId> teams, IList<IAgentController> controllers)
    {
        Arena = PlaceholderArenaBuilder.Build(Court, BallConfig, Rules.threePointRadius);
        var movement = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        foreach (TeamId team in teams)
        {
            Players.Add(PlaceholderPlayerFactory.Create($"{team}_{Players.Count}", team, movement, Color.gray, Color.yellow));
        }
        Sim = new MatchSimulation(Players, new List<IAgentController>(controllers), Arena.Ball, Arena.Hoop, Court, Rules, BallConfig, ShotConfig, DefenseConfig, new System.Random(1),
            secondHoop: Arena.SecondHoop);
        Sim.OnMatchEvent += Events.Add;
        Sim.OnMatchEvent += e => Timeline.Add($"{SimulatedTime,6:F1}s sc {Sim.Match.State.ShotClock,4:F1} {e}");
        Sim.ShotReports.OnShotTaken += Shots.Add;
        Sim.Match.OnBasketCounted += MarkMade;
        Sim.Begin();
    }

    // Ticks the simulation once per frame until the condition holds or time runs out.
    public IEnumerator RunUntil(Func<bool> condition, float timeoutSeconds)
    {
        float elapsed = 0f;
        while (!condition() && elapsed < timeoutSeconds)
        {
            yield return null;
            SimulatedTime += Time.deltaTime;
            Sim.Tick(Time.deltaTime);
            elapsed += Time.deltaTime;
        }
    }

    // Which reported shots went in (a basket credits the scoring team's last shot).
    public readonly HashSet<int> MadeShots = new HashSet<int>();

    private void MarkMade(TeamId team, int points, ShotType? type)
    {
        if (type == null) return;
        for (int i = Shots.Count - 1; i >= 0; i--)
        {
            if (Players[Shots[i].ShooterIndex].Team != team) continue;
            if (Shots[i].Type == type.Value || (type.Value == ShotType.Layup && Shots[i].Type == ShotType.Dunk)) MadeShots.Add(i);
            return;
        }
    }

    // Per shot type: attempts, makes and the averages that drive the aim error -- for
    // calibrating accuracy from the AI-vs-AI runs.
    public string ShotSummary()
    {
        var sb = new System.Text.StringBuilder("SHOTS type: made/att  dist  |timing|  contest  errorRadius\n");
        foreach (ShotType t in Enum.GetValues(typeof(ShotType)))
        {
            int n = 0, made = 0;
            float dist = 0f, timing = 0f, contest = 0f, error = 0f;
            for (int i = 0; i < Shots.Count; i++)
            {
                ShotReport r = Shots[i];
                if (r.Type != t) continue;
                n++;
                if (MadeShots.Contains(i)) made++;
                dist += r.Distance; timing += Mathf.Abs(r.TimingError); contest += r.Contest; error += r.ErrorRadius;
            }
            if (n == 0) continue;
            sb.AppendLine($"  {t}: {made}/{n}  {dist / n:0.00}m  {timing / n:0.000}s  {contest / n:0.00}  {error / n:0.000}m");
        }
        for (int i = 0; i < Shots.Count; i++)
        {
            ShotReport r = Shots[i];
            sb.AppendLine($"    #{i} p{r.ShooterIndex} {r.Type} d={r.Distance:0.0} t={r.TimingError:+0.00;-0.00} c={r.Contest:0.00} e={r.ErrorRadius:0.000}{(MadeShots.Contains(i) ? " MADE" : "")}");
        }
        return sb.ToString();
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
