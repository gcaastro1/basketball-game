using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.AI;
using Basket.Core;
using Basket.Gameplay;

// Headless AI-vs-AI 3v3 at accelerated time: the AI has to actually play basketball
// (move the ball, shoot, rebound, score) without getting stuck. The box score is logged
// for balancing.
public class AISimulationTests
{
    private const float SimulatedSeconds = 60f;
    private const float TimeScale = 4f;

    [UnityTest]
    public IEnumerator AIvsAI_3v3_PlaysBasketball()
    {
        using var match = new TestMatch();
        // Real accuracy (the helper zeroes it for deterministic tests) and 3x3-style rules.
        var defaults = ScriptableObject.CreateInstance<ShotConfig>();
        match.ShotConfig.jumpShotBaseError = defaults.jumpShotBaseError;
        match.ShotConfig.jumpShotErrorPerMeter = defaults.jumpShotErrorPerMeter;
        match.ShotConfig.layupBaseError = defaults.layupBaseError;
        match.ShotConfig.freeThrowBaseError = defaults.freeThrowBaseError;
        match.Rules.pointsInsideArc = 1;
        match.Rules.pointsBeyondArc = 2;
        match.Rules.winningScore = 0;
        match.Rules.useShotClock = true;
        match.Rules.clearBallOnChangeOfPossession = true;
        match.Rules.afterMadeBasket = RestartKind.UnderBasket;

        var config = ScriptableObject.CreateInstance<AIConfig>();
        var rng = new System.Random(7);
        var home = new TeamBrain(TeamId.Home, 6, config, rng: rng);
        var away = new TeamBrain(TeamId.Away, 6, config, rng: rng);
        var teams = new List<TeamId>();
        var controllers = new List<IAgentController>();
        for (int i = 0; i < 6; i++)
        {
            TeamId team = i < 3 ? TeamId.Home : TeamId.Away;
            teams.Add(team);
            controllers.Add(new AIAgentController(config, rng, team == TeamId.Home ? home : away));
        }
        match.Start(teams, controllers);

        float previousScale = Time.timeScale;
        Time.timeScale = TimeScale;
        try
        {
            yield return match.RunUntil(() => false, SimulatedSeconds);
        }
        finally
        {
            Time.timeScale = previousScale;
        }

        TeamStats h = match.Sim.Stats.Get(TeamId.Home);
        TeamStats a = match.Sim.Stats.Get(TeamId.Away);
        Debug.Log($"AI vs AI {SimulatedSeconds}s: HOME {match.Sim.Match.State.ScoreHome} - {match.Sim.Match.State.ScoreAway} AWAY\nHOME {h}\nAWAY {a}\n{match.ShotSummary()}\n{string.Join("\n", match.Events)}");

        int shots = h.FieldGoalsAttempted + a.FieldGoalsAttempted;
        int passes = h.Passes + a.Passes;
        int rebounds = h.OffensiveRebounds + h.DefensiveRebounds + a.OffensiveRebounds + a.DefensiveRebounds;
        int points = match.Sim.Match.State.ScoreHome + match.Sim.Match.State.ScoreAway;

        Assert.GreaterOrEqual(shots, 8, "the AI takes shots");
        Assert.GreaterOrEqual(passes, 4, "the AI moves the ball");
        Assert.GreaterOrEqual(rebounds, 2, "missed shots are rebounded");
        Assert.Greater(points, 0, "somebody scores");
        Assert.Less(h.Turnovers + a.Turnovers, shots, "fewer turnovers than shots");
        Assert.Greater(h.FieldGoalsAttempted, 0, "both teams get shots up");
        Assert.Greater(a.FieldGoalsAttempted, 0, "both teams get shots up");
    }
}
