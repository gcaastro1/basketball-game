using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.AI;
using Basket.Core;
using Basket.Gameplay;

// Etapa 7: full court (two baskets) with FIBA 5v5 style rules.
public class FullCourtTests
{
    private static void UseFullCourt(CourtConfig court)
    {
        court.width = 15f;
        court.depth = 28f;
        court.fullCourt = true;
        court.rimCenter = new Vector3(0f, 3.05f, 26.425f);
        court.backboardCenter = new Vector3(0f, 3.425f, 26.8f);
        court.checkBallSpot = new Vector3(0f, 0f, 19.35f);
    }

    private static void UseFiba5v5(MatchRules rules)
    {
        rules.pointsInsideArc = 2;
        rules.pointsBeyondArc = 3;
        rules.winningScore = 0;
        rules.useGameClock = true;
        rules.periods = 4;
        rules.periodLengthSeconds = 600f;
        rules.overtimeMode = OvertimeMode.TimedPeriod;
        rules.useShotClock = true;
        rules.shotClockSeconds = 24f;
        rules.shotClockAfterRimTouch = 14f;
        rules.fullCourt = true;
        rules.useBoundaryLines = true;
        rules.frontcourtSeconds = 8f;
        rules.useBackcourtRule = true;
        rules.startWithJumpBall = true;
        rules.switchSidesAfterPeriod = 2;
        rules.afterMadeBasket = RestartKind.BaselineInbound;
        rules.clearBallOnChangeOfPossession = false;
        rules.teamFoulPenaltyThreshold = 5;
        rules.teamFoulBonusPossessionThreshold = 0;
    }

    [UnityTest]
    public IEnumerator FullCourtArena_BallThroughEachHoop_CreditsTheTeamAttackingIt()
    {
        var court = ScriptableObject.CreateInstance<CourtConfig>();
        UseFullCourt(court);
        Arena arena = PlaceholderArenaBuilder.Build(court, ScriptableObject.CreateInstance<BallConfig>(), 6.75f);
        var holder = new GameObject("Holder");
        try
        {
            Assert.IsNotNull(arena.SecondHoop, "full court builds a second basket");
            Assert.AreEqual(court.Mirror(court.rimCenter).z, arena.SecondHoop.RimCenter.z, 0.01f, "mirrored across the center line");
            arena.Hoop.AttackingTeam = TeamId.Home;
            arena.SecondHoop.AttackingTeam = TeamId.Away;

            var scores = new List<ScoreEvent>();
            arena.Ball.OnScored += scores.Add;
            yield return new WaitForFixedUpdate();

            foreach (HoopController hoop in new[] { arena.Hoop, arena.SecondHoop })
            {
                int before = scores.Count;
                arena.Ball.Catch(holder.transform);
                arena.Ball.ReleaseAt(BallState.Shooting, hoop.RimCenter + Vector3.up * 1f, Vector3.zero);
                for (int i = 0; i < 90 && scores.Count == before; i++) yield return new WaitForFixedUpdate();

                Assert.AreEqual(before + 1, scores.Count, $"scored through {hoop.name}");
                Assert.AreEqual(hoop.AttackingTeam, scores[before].HoopTeam);
                Assert.AreEqual(hoop.RimCenter.z, scores[before].HoopCenter.Value.z, 0.01f);
            }
        }
        finally
        {
            Object.Destroy(arena.Root);
            Object.Destroy(holder);
        }
    }

    [UnityTest]
    public IEnumerator FullCourt_JumpBallStartsTheGame_AndTheTipIsWon()
    {
        using var match = new TestMatch();
        UseFullCourt(match.Court);
        UseFiba5v5(match.Rules);
        var teams = new List<TeamId>();
        var controllers = new List<IAgentController>();
        var config = ScriptableObject.CreateInstance<AIConfig>();
        var rng = new System.Random(3);
        for (int i = 0; i < 2; i++)
        {
            teams.Add(i == 0 ? TeamId.Home : TeamId.Away);
            controllers.Add(new AIAgentController(config, rng));
        }
        match.Start(teams, controllers);

        yield return match.RunUntil(() => match.Sim.Snapshot.BallHolderIndex >= 0, 6f);

        Assert.GreaterOrEqual(match.Sim.Snapshot.BallHolderIndex, 0, "someone controls the tip\n" + string.Join("\n", match.Events));
    }

    private const float SimulatedSeconds = 90f;
    private const float TimeScale = 4f;

    // Headless AI-vs-AI 5v5 on the full court: both teams have to bring the ball up past
    // midcourt, get shots up and score.
    [UnityTest]
    public IEnumerator AIvsAI_5v5_FullCourt_PlaysBasketball()
    {
        using var match = new TestMatch();
        UseFullCourt(match.Court);
        UseFiba5v5(match.Rules);
        match.Rules.restartDelaySeconds = 1f;
        var defaults = ScriptableObject.CreateInstance<ShotConfig>();
        match.ShotConfig.jumpShotBaseError = defaults.jumpShotBaseError;
        match.ShotConfig.jumpShotErrorPerMeter = defaults.jumpShotErrorPerMeter;
        match.ShotConfig.layupBaseError = defaults.layupBaseError;
        match.ShotConfig.freeThrowBaseError = defaults.freeThrowBaseError;

        var config = ScriptableObject.CreateInstance<AIConfig>();
        var rng = new System.Random(11);
        var home = new TeamBrain(TeamId.Home, 10, config, rng: rng);
        var away = new TeamBrain(TeamId.Away, 10, config, rng: rng);
        var teams = new List<TeamId>();
        var controllers = new List<IAgentController>();
        for (int i = 0; i < 10; i++)
        {
            TeamId team = i < 5 ? TeamId.Home : TeamId.Away;
            teams.Add(team);
            controllers.Add(new AIAgentController(config, rng, team == TeamId.Home ? home : away));
        }
        match.Start(teams, controllers);

        // A team "crossed" when its ball handler was seen in its frontcourt.
        var crossed = new HashSet<TeamId>();
        float previousScale = Time.timeScale;
        Time.timeScale = TimeScale;
        try
        {
            yield return match.RunUntil(() =>
            {
                MatchSnapshot s = match.Sim.Snapshot;
                int h = s.BallHolderIndex;
                if (h >= 0)
                {
                    TeamId team = s.GetTeam(h);
                    float toHoop = s.GetAttackingHoop(team).z - s.CourtCenter.z;
                    if ((s.GetPosition(h).z - s.CourtCenter.z) * toHoop > 1f) crossed.Add(team);
                }
                return false;
            }, SimulatedSeconds);
        }
        finally
        {
            Time.timeScale = previousScale;
        }

        TeamStats hs = match.Sim.Stats.Get(TeamId.Home);
        TeamStats a = match.Sim.Stats.Get(TeamId.Away);
        Debug.Log($"AI vs AI 5v5 {SimulatedSeconds}s: HOME {match.Sim.Match.State.ScoreHome} - {match.Sim.Match.State.ScoreAway} AWAY\nHOME {hs}\nAWAY {a}\n"
            + string.Join("\n", match.Events.GetRange(0, Mathf.Min(40, match.Events.Count))));

        Assert.IsTrue(crossed.Contains(TeamId.Home), "HOME brings the ball past midcourt");
        Assert.IsTrue(crossed.Contains(TeamId.Away), "AWAY brings the ball past midcourt");
        Assert.Greater(hs.FieldGoalsAttempted, 0, "HOME gets shots up");
        Assert.Greater(a.FieldGoalsAttempted, 0, "AWAY gets shots up");
        Assert.Greater(match.Sim.Match.State.ScoreHome + match.Sim.Match.State.ScoreAway, 0, "somebody scores");
    }
}
