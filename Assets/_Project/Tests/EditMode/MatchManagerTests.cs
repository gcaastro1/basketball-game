using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class MatchManagerTests
{
    private static readonly Vector3 Rim = new Vector3(0f, 3.05f, 13.1f);

    private static MatchManager CreateLive(out MatchRules rules, System.Action<TeamId> onRestart = null)
    {
        rules = ScriptableObject.CreateInstance<MatchRules>();
        rules.winningScore = 5;
        rules.restartDelaySeconds = 1f;
        var match = new MatchManager(rules, Rim);
        if (onRestart != null) match.OnPossessionRestart += onRestart;
        match.BeginMatch();
        return match;
    }

    private static ScoreEvent Make(TeamId? team, float releaseZ) =>
        new ScoreEvent(null, team, new Vector3(0f, 0f, releaseZ), BallState.Shooting);

    [Test]
    public void BeginMatch_RestartsWithFirstPossessionAndGoesLive()
    {
        TeamId? restarted = null;
        var match = CreateLive(out _, t => restarted = t);

        Assert.AreEqual(TeamId.Home, restarted);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void Score_CreditsReleasingTeam_WithPointsByDistance()
    {
        var match = CreateLive(out _);

        match.HandleScore(Make(TeamId.Away, releaseZ: 10f));

        Assert.AreEqual(2, match.State.ScoreAway);
        Assert.AreEqual(0, match.State.ScoreHome);
    }

    [Test]
    public void Score_FromBeyondArc_IsWorthBeyondArcPoints()
    {
        var match = CreateLive(out _);
        match.HandleScore(Make(TeamId.Home, releaseZ: 5.5f));
        Assert.AreEqual(3, match.State.ScoreHome);
    }

    [Test]
    public void Score_WithoutTeam_IsIgnored()
    {
        var match = CreateLive(out _);
        match.HandleScore(Make(null, releaseZ: 10f));
        Assert.AreEqual(0, match.State.ScoreHome + match.State.ScoreAway);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void AfterScore_OtherTeamGetsBallOnlyAfterDelay()
    {
        TeamId? restarted = null;
        var match = CreateLive(out _, t => restarted = t);
        restarted = null;

        match.HandleScore(Make(TeamId.Home, releaseZ: 10f));
        match.Tick(0.5f);
        Assert.IsNull(restarted);
        Assert.AreEqual(MatchPhase.Scored, match.State.Phase);

        match.Tick(0.6f);
        Assert.AreEqual(TeamId.Away, restarted);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void BallOutOfPlay_GivesBallToOtherTeamAfterDelay_WithoutScoring()
    {
        TeamId? restarted = null;
        var match = CreateLive(out _, t => restarted = t);
        restarted = null;

        match.HandleBallOutOfPlay(TeamId.Home);
        Assert.AreEqual(MatchPhase.WaitingForInbound, match.State.Phase);
        match.Tick(1.1f);

        Assert.AreEqual(TeamId.Away, restarted);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
        Assert.AreEqual(0, match.State.ScoreHome + match.State.ScoreAway);
    }

    [Test]
    public void ReachingWinningScore_EndsMatchAndNeverRestarts()
    {
        bool restartedAfterEnd = false;
        var match = CreateLive(out _);
        match.HandleScore(Make(TeamId.Home, releaseZ: 5.5f));
        match.Tick(2f);
        match.HandleScore(Make(TeamId.Home, releaseZ: 5.5f));
        match.OnPossessionRestart += _ => restartedAfterEnd = true;
        match.Tick(5f);

        Assert.AreEqual(MatchPhase.Ended, match.State.Phase);
        Assert.AreEqual(TeamId.Home, match.State.Winner);
        Assert.IsFalse(restartedAfterEnd);
    }
}
