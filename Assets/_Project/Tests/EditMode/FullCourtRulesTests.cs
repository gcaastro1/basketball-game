using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class FullCourtRulesTests
{
    private static readonly Vector3 HomeRim = new Vector3(0f, 3.05f, 26.425f);
    private const float Delay = 1f;

    private MatchRules rules;
    private MatchManager match;
    private readonly List<(TeamId team, RestartKind kind)> restarts = new List<(TeamId, RestartKind)>();
    private readonly List<string> events = new List<string>();
    private int sideSwitches;

    private static readonly BallStatus Possessed = new BallStatus(possessed: true, shotInFlight: false, liveRelease: false);
    private static readonly BallStatus Loose = new BallStatus(possessed: false, shotInFlight: false, liveRelease: false);

    [SetUp]
    public void SetUp()
    {
        rules = ScriptableObject.CreateInstance<MatchRules>();
        rules.pointsInsideArc = 2;
        rules.pointsBeyondArc = 3;
        rules.winningScore = 0;
        rules.fullCourt = true;
        rules.useBoundaryLines = true;
        rules.frontcourtSeconds = 8f;
        rules.useBackcourtRule = true;
        rules.startWithJumpBall = true;
        rules.switchSidesAfterPeriod = 2;
        rules.useGameClock = true;
        rules.periods = 4;
        rules.periodLengthSeconds = 10f;
        rules.overtimeMode = OvertimeMode.TimedPeriod;
        rules.overtimeLengthSeconds = 5f;
        rules.useShotClock = true;
        rules.shotClockSeconds = 24f;
        rules.shotClockAfterRimTouch = 14f;
        rules.afterMadeBasket = RestartKind.BaselineInbound;
        rules.teamFoulPenaltyThreshold = 5;
        rules.teamFoulBonusPossessionThreshold = 0;
        rules.restartDelaySeconds = Delay;

        match = new MatchManager(rules, HomeRim);
        restarts.Clear();
        events.Clear();
        sideSwitches = 0;
        match.OnPossessionRestart += (t, k) => restarts.Add((t, k));
        match.OnRuleEvent += events.Add;
        match.OnSidesSwitched += () => sideSwitches++;
        match.BeginMatch();
    }

    [Test]
    public void Match_StartsWithAJumpBall_NobodyHasPossession()
    {
        Assert.AreEqual(RestartKind.JumpBall, restarts[0].kind);
        Assert.IsNull(match.State.PossessionTeam);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void MadeBasket_IsInboundedFromTheBaselineByTheOtherTeam()
    {
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: true);
        var basket = new ScoreEvent(null, TeamId.Home, new Vector3(0f, 0f, 24f), BallState.Shooting, ShotType.JumpShot, HomeRim, TeamId.Home);
        match.HandleScore(basket);
        match.Tick(Delay * 1.1f, Loose);

        Assert.AreEqual(2, match.State.ScoreHome);
        Assert.AreEqual((TeamId.Away, RestartKind.BaselineInbound), restarts[restarts.Count - 1]);
    }

    [Test]
    public void Basket_CountsForTheTeamThatAttacksThatHoop_AndDistanceToThatHoop()
    {
        var awayRim = new Vector3(0f, 3.05f, 1.575f);
        // Released 7.6 m from the Away basket -> three for Away.
        var basket = new ScoreEvent(null, TeamId.Away, new Vector3(0f, 0f, 9.2f), BallState.Shooting, ShotType.JumpShot, awayRim, TeamId.Away);
        match.HandleScore(basket);
        Assert.AreEqual(3, match.State.ScoreAway);
    }

    [Test]
    public void EightSeconds_WithoutCrossingMidcourt_IsAViolation()
    {
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: false);
        for (int i = 0; i < 9; i++) match.Tick(1f, Possessed);

        Assert.Contains("8 SECONDS VIOLATION", events);
        match.Tick(Delay * 1.1f, Loose);
        Assert.AreEqual((TeamId.Away, RestartKind.SidelineInbound), restarts[restarts.Count - 1]);
    }

    [Test]
    public void CrossingMidcourt_StopsTheEightSecondCount()
    {
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: false);
        match.Tick(5f, Possessed);
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: true);
        match.Tick(5f, Possessed);
        Assert.IsFalse(events.Contains("8 SECONDS VIOLATION"));
    }

    [Test]
    public void BackIntoTheBackcourt_IsAViolation()
    {
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: true);
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: false, lastTouchedByOpponent: false);
        Assert.Contains("BACKCOURT VIOLATION", events);
    }

    [Test]
    public void BackcourtAfterADefensiveDeflection_IsLegal()
    {
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: true);
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: false, lastTouchedByOpponent: true);
        Assert.IsFalse(events.Contains("BACKCOURT VIOLATION"));
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void FourPeriods_SwitchSidesAtHalftime_ThenFinal()
    {
        match.NotifyPossession(TeamId.Home, false, inFrontcourt: true);
        var basket = new ScoreEvent(null, TeamId.Home, new Vector3(0f, 0f, 24f), BallState.Shooting, ShotType.JumpShot, HomeRim, TeamId.Home);
        match.HandleScore(basket);
        match.Tick(Delay * 1.1f, Loose);

        for (int period = 1; period <= 4; period++)
        {
            Assert.AreEqual(period, match.State.Period);
            match.Tick(11f, Loose);             // period ends
            if (period < 4) match.Tick(Delay * 1.1f, Loose); // next period restart
        }

        Assert.AreEqual(1, sideSwitches, "switch only at halftime");
        Assert.AreEqual(MatchPhase.Ended, match.State.Phase);
        Assert.AreEqual(TeamId.Home, match.State.Winner);
        Assert.Contains((TeamId.Away, RestartKind.MidcourtInbound), restarts, "even periods start with the other team inbounding at midcourt");
    }

    [Test]
    public void TiedAfterRegulation_PlaysATimedOvertime()
    {
        for (int period = 1; period <= 4; period++)
        {
            match.Tick(11f, Loose);
            match.Tick(Delay * 1.1f, Loose);
        }
        Assert.IsTrue(match.State.IsOvertime);
        Assert.AreNotEqual(MatchPhase.Ended, match.State.Phase);
        Assert.AreEqual(RestartKind.JumpBall, restarts[restarts.Count - 1].kind);
    }

    [Test]
    public void FifthTeamFoul_GoesToFreeThrows()
    {
        match.NotifyPossession(TeamId.Away, false, inFrontcourt: true);
        for (int i = 0; i < 4; i++)
        {
            match.HandleFoul(new FoulEvent(TeamId.Home, 7, TeamId.Away, shooting: false));
            match.Tick(Delay * 1.1f, Loose);
            Assert.AreEqual(RestartKind.SidelineInbound, restarts[restarts.Count - 1].kind);
        }
        match.HandleFoul(new FoulEvent(TeamId.Home, 7, TeamId.Away, shooting: false));
        match.Tick(Delay * 1.1f, Loose);
        Assert.AreEqual(MatchPhase.FreeThrow, match.State.Phase);
    }
}
