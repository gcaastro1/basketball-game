using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class MatchManagerTests
{
    private static readonly Vector3 Rim = new Vector3(0f, 3.05f, 13.1f);
    private const float Delay = 1f;

    private MatchRules rules;
    private MatchManager match;
    private readonly List<(TeamId team, RestartKind kind)> restarts = new List<(TeamId, RestartKind)>();
    private readonly List<int> freeThrowSetups = new List<int>();
    private readonly List<string> events = new List<string>();

    private static readonly BallStatus Possessed = new BallStatus(possessed: true, shotInFlight: false, liveRelease: false);
    private static readonly BallStatus Loose = new BallStatus(possessed: false, shotInFlight: false, liveRelease: false);
    private static readonly BallStatus ShotInAir = new BallStatus(possessed: false, shotInFlight: true, liveRelease: true);
    private static readonly BallStatus BouncingOnRim = new BallStatus(possessed: false, shotInFlight: false, liveRelease: true);

    private void Create(bool fiba3x3 = false, int winningScore = 5)
    {
        rules = ScriptableObject.CreateInstance<MatchRules>();
        rules.restartDelaySeconds = Delay;
        rules.winningScore = winningScore;
        if (fiba3x3)
        {
            rules.pointsInsideArc = 1;
            rules.pointsBeyondArc = 2;
            rules.winningScore = 21;
            rules.useGameClock = true;
            rules.periodLengthSeconds = 600f;
            rules.useShotClock = true;
            rules.shotClockSeconds = 12f;
            rules.shotClockAfterRimTouch = 12f;
            rules.clearBallOnChangeOfPossession = true;
            rules.afterMadeBasket = RestartKind.UnderBasket;
            rules.shootingFoulFreeThrowsInsideArc = 1;
            rules.shootingFoulFreeThrowsBeyondArc = 2;
            rules.overtimeMode = OvertimeMode.SuddenDeathPoints;
            rules.overtimePointsToWin = 2;
        }
        match = new MatchManager(rules, Rim);
        restarts.Clear();
        freeThrowSetups.Clear();
        events.Clear();
        match.OnPossessionRestart += (t, k) => restarts.Add((t, k));
        match.OnFreeThrowSetup += freeThrowSetups.Add;
        match.OnRuleEvent += events.Add;
        match.BeginMatch();
    }

    private static ScoreEvent Shot(TeamId? team, float releaseZ, ShotType type = ShotType.JumpShot) =>
        new ScoreEvent(null, team, new Vector3(0f, 0f, releaseZ), BallState.Shooting, type);

    private const float Inside = 10f;   // 3.1 m from the rim
    private const float Beyond = 5.5f;  // 7.6 m from the rim

    // ---------- scoring and restarts ----------

    [Test]
    public void BeginMatch_RestartsWithFirstPossessionAndGoesLive()
    {
        Create();
        Assert.AreEqual((TeamId.Home, RestartKind.CheckBall), restarts[0]);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
        Assert.AreEqual(TeamId.Home, match.State.PossessionTeam);
    }

    [Test]
    public void Score_CreditsReleasingTeam_WithPointsByDistance()
    {
        Create();
        match.HandleScore(Shot(TeamId.Away, Inside));
        Assert.AreEqual(2, match.State.ScoreAway);
        Assert.AreEqual(0, match.State.ScoreHome);
    }

    [Test]
    public void Score_FromBeyondArc_IsWorthBeyondArcPoints()
    {
        Create();
        match.HandleScore(Shot(TeamId.Home, Beyond));
        Assert.AreEqual(3, match.State.ScoreHome);
    }

    [Test]
    public void Score_WithoutTeam_IsIgnored()
    {
        Create();
        match.HandleScore(Shot(null, Inside));
        Assert.AreEqual(0, match.State.ScoreHome + match.State.ScoreAway);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void AfterScore_OtherTeamGetsBallOnlyAfterDelay()
    {
        Create();
        match.HandleScore(Shot(TeamId.Home, Inside));
        match.Tick(Delay * 0.5f);
        Assert.AreEqual(1, restarts.Count);
        Assert.AreEqual(MatchPhase.Scored, match.State.Phase);

        match.Tick(Delay);
        Assert.AreEqual((TeamId.Away, RestartKind.CheckBall), restarts[1]);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void ReachingWinningScore_EndsMatchAndNeverRestarts()
    {
        Create(winningScore: 5);
        match.HandleScore(Shot(TeamId.Home, Beyond));
        match.Tick(Delay * 2f);
        match.HandleScore(Shot(TeamId.Home, Beyond));
        int restartsBefore = restarts.Count;
        match.Tick(Delay * 5f);

        Assert.AreEqual(MatchPhase.Ended, match.State.Phase);
        Assert.AreEqual(TeamId.Home, match.State.Winner);
        Assert.AreEqual(restartsBefore, restarts.Count);
    }

    [Test]
    public void BallOutOfPlay_GivesBallToOtherTeamAfterDelay_WithoutScoring()
    {
        Create();
        match.HandleBallOutOfPlay(TeamId.Home);
        Assert.AreEqual(MatchPhase.WaitingForInbound, match.State.Phase);
        match.Tick(Delay * 1.1f);

        Assert.AreEqual((TeamId.Away, RestartKind.CheckBall), restarts[1]);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
        Assert.AreEqual(0, match.State.ScoreHome + match.State.ScoreAway);
    }

    // ---------- 3x3: scoring and clearing ----------

    [Test]
    public void Fiba3x3_PointsAreOneAndTwo_AndMadeBasketRestartsUnderTheBasket()
    {
        Create(fiba3x3: true);
        match.HandleScore(Shot(TeamId.Home, Inside));
        Assert.AreEqual(1, match.State.ScoreHome);
        match.Tick(Delay * 1.1f);
        Assert.AreEqual((TeamId.Away, RestartKind.UnderBasket), restarts[1]);
        Assert.IsTrue(match.State.BallMustBeCleared, "inbound under the basket must be cleared");

        match.HandleScore(Shot(TeamId.Home, Beyond));
        Assert.AreEqual(3, match.State.ScoreHome);
    }

    [Test]
    public void Fiba3x3_DefensiveReboundNotCleared_BasketDoesNotCount_AndBallChangesHands()
    {
        Create(fiba3x3: true);
        match.NotifyPossession(TeamId.Away, beyondArc: false); // live change of possession
        Assert.IsTrue(match.State.BallMustBeCleared);

        match.HandleScore(Shot(TeamId.Away, Inside));
        Assert.AreEqual(0, match.State.ScoreAway);
        match.Tick(Delay * 1.1f);
        Assert.AreEqual((TeamId.Home, RestartKind.CheckBall), restarts[1]);
    }

    [Test]
    public void Fiba3x3_ClearedBall_BasketCounts()
    {
        Create(fiba3x3: true);
        match.NotifyPossession(TeamId.Away, beyondArc: false);
        match.NotifyPossession(TeamId.Away, beyondArc: true);
        Assert.IsFalse(match.State.BallMustBeCleared);

        match.HandleScore(Shot(TeamId.Away, Inside));
        Assert.AreEqual(1, match.State.ScoreAway);
    }

    [Test]
    public void Fiba3x3_OffensiveRebound_NeedsNoClearing()
    {
        Create(fiba3x3: true);
        match.NotifyPossession(TeamId.Home, beyondArc: false);
        Assert.IsFalse(match.State.BallMustBeCleared);
    }

    // ---------- shot clock ----------

    [Test]
    public void ShotClock_Expiring_GivesBallToOpponent()
    {
        Create(fiba3x3: true);
        for (int i = 0; i < 13; i++) match.Tick(1f, Possessed);

        Assert.Contains("SHOT CLOCK VIOLATION", events);
        match.Tick(Delay * 1.1f);
        Assert.AreEqual((TeamId.Away, RestartKind.CheckBall), restarts[1]);
    }

    [Test]
    public void ShotClock_OnlyRunsWhileTheBallIsPossessed()
    {
        Create(fiba3x3: true);
        match.Tick(5f, Loose);
        Assert.AreEqual(12f, match.State.ShotClock, 1e-4f);
        match.Tick(5f, Possessed);
        Assert.AreEqual(7f, match.State.ShotClock, 1e-4f);
    }

    [Test]
    public void ShotClock_ResetsOnRimTouchAndOnChangeOfPossession()
    {
        Create(fiba3x3: true);
        match.Tick(8f, Possessed);
        match.NotifyRimTouched();
        Assert.AreEqual(12f, match.State.ShotClock, 1e-4f);

        match.Tick(8f, Possessed);
        match.NotifyPossession(TeamId.Away, beyondArc: true);
        Assert.AreEqual(12f, match.State.ShotClock, 1e-4f);
    }

    // ---------- game clock ----------

    [Test]
    public void GameClock_Expiring_EndsMatchWithLeaderAsWinner()
    {
        Create(fiba3x3: true);
        rules.periodLengthSeconds = 10f;
        match = new MatchManager(rules, Rim);
        match.BeginMatch();
        match.HandleScore(Shot(TeamId.Home, Inside));
        match.Tick(Delay * 1.1f);
        match.Tick(11f, Loose);

        Assert.AreEqual(MatchPhase.Ended, match.State.Phase);
        Assert.AreEqual(TeamId.Home, match.State.Winner);
    }

    [Test]
    public void GameClock_ShotInTheAirAtTheBuzzer_StillCounts()
    {
        Create(fiba3x3: true);
        rules.periodLengthSeconds = 1f;
        match = new MatchManager(rules, Rim);
        match.OnPossessionRestart += (t, k) => restarts.Add((t, k));
        match.BeginMatch();

        match.Tick(1.5f, ShotInAir);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase, "waits for the shot");
        match.HandleScore(Shot(TeamId.Away, Beyond));
        match.Tick(Delay * 1.1f, Loose);
        match.Tick(0.1f, Loose);

        Assert.AreEqual(MatchPhase.Ended, match.State.Phase);
        Assert.AreEqual(TeamId.Away, match.State.Winner);
    }

    [Test]
    public void GameClock_TiedAtTheEnd_GoesToSuddenDeathOvertime()
    {
        Create(fiba3x3: true);
        rules.periodLengthSeconds = 1f;
        match = new MatchManager(rules, Rim);
        match.BeginMatch();
        match.Tick(1.5f, Loose);

        Assert.IsTrue(match.State.IsOvertime);
        Assert.AreNotEqual(MatchPhase.Ended, match.State.Phase);
        match.Tick(Delay * 1.1f, Loose);

        match.HandleScore(Shot(TeamId.Away, Inside)); // 1 point
        Assert.AreNotEqual(MatchPhase.Ended, match.State.Phase);
        match.Tick(Delay * 1.1f, Loose);
        match.HandleScore(Shot(TeamId.Away, Inside)); // 2 points in overtime
        Assert.AreEqual(MatchPhase.Ended, match.State.Phase);
        Assert.AreEqual(TeamId.Away, match.State.Winner);
    }

    [Test]
    public void GameClock_StopsWhenTheBallIsDead()
    {
        Create(fiba3x3: true);
        match.HandleScore(Shot(TeamId.Home, Inside)); // phase Scored
        float before = match.State.GameClock;
        match.Tick(0.5f, Loose);
        Assert.AreEqual(before, match.State.GameClock, 1e-4f);
    }

    // ---------- fouls and free throws ----------

    private void ShootingFoulOnHome(bool beyondArc)
    {
        match.NotifyShotReleased(0, beyondArc);
        match.HandleFoul(new FoulEvent(TeamId.Away, 0, TeamId.Home, shooting: true));
    }

    [Test]
    public void ShootingFoul_Missed_AwardsFreeThrowsByZone_ThenOpponentBallAfterMakes()
    {
        Create(fiba3x3: true);
        ShootingFoulOnHome(beyondArc: true);
        match.Tick(0.1f, ShotInAir);
        match.Tick(0.1f, Loose); // shot is over, no basket
        match.Tick(Delay * 1.1f, Loose);

        Assert.AreEqual(MatchPhase.FreeThrow, match.State.Phase);
        Assert.AreEqual(new List<int> { 0 }, freeThrowSetups);
        Assert.AreEqual(0, match.FreeThrowShooter);

        for (int ft = 0; ft < 2; ft++)
        {
            match.NotifyShotReleased(0, false);
            match.HandleScore(Shot(TeamId.Home, 8.9f, ShotType.FreeThrow));
            match.Tick(Delay * 1.1f, Loose);
        }

        Assert.AreEqual(2, match.State.ScoreHome, "two free throws, one point each");
        Assert.AreEqual(2, freeThrowSetups.Count);
        Assert.AreEqual((TeamId.Away, RestartKind.UnderBasket), restarts[restarts.Count - 1]);
    }

    [Test]
    public void ShootingFoul_Made_IsAndOne()
    {
        Create(fiba3x3: true);
        ShootingFoulOnHome(beyondArc: false);
        match.HandleScore(Shot(TeamId.Home, Inside));
        Assert.AreEqual(1, match.State.ScoreHome);
        match.Tick(Delay * 1.1f, Loose);

        Assert.AreEqual(MatchPhase.FreeThrow, match.State.Phase);
        match.NotifyShotReleased(0, false);
        match.HandleScore(Shot(TeamId.Home, 8.9f, ShotType.FreeThrow));
        Assert.AreEqual(2, match.State.ScoreHome);
    }

    [Test]
    public void FreeThrow_LastMissOffTheRim_IsALiveRebound()
    {
        Create(fiba3x3: true);
        ShootingFoulOnHome(beyondArc: false);
        match.Tick(0.1f, Loose);
        match.Tick(Delay * 1.1f, Loose);
        match.NotifyShotReleased(0, false);

        match.Tick(0.1f, BouncingOnRim);
        Assert.AreEqual(MatchPhase.Live, match.State.Phase);
    }

    [Test]
    public void FreeThrow_AirBall_GivesOpponentTheBall()
    {
        Create(fiba3x3: true);
        ShootingFoulOnHome(beyondArc: false);
        match.Tick(0.1f, Loose);
        match.Tick(Delay * 1.1f, Loose);
        match.NotifyShotReleased(0, false);

        match.Tick(0.1f, Loose); // release died without touching the rim
        match.Tick(Delay * 1.1f, Loose);
        Assert.AreEqual((TeamId.Away, RestartKind.CheckBall), restarts[restarts.Count - 1]);
    }

    [Test]
    public void NonShootingFoul_BelowPenalty_IsCheckBallForTheFouledTeam()
    {
        Create(fiba3x3: true);
        match.HandleFoul(new FoulEvent(TeamId.Home, 3, TeamId.Away, shooting: false));
        Assert.AreEqual(1, match.State.GetTeamFouls(TeamId.Home));
        match.Tick(Delay * 1.1f, Loose);
        Assert.AreEqual((TeamId.Away, RestartKind.CheckBall), restarts[1]);
    }

    [Test]
    public void NonShootingFoul_InPenalty_GoesToFreeThrows()
    {
        Create(fiba3x3: true);
        for (int i = 0; i < 6; i++)
        {
            match.HandleFoul(new FoulEvent(TeamId.Home, 3, TeamId.Away, shooting: false));
            match.Tick(Delay * 1.1f, Loose);
        }
        Assert.AreEqual(0, freeThrowSetups.Count);

        match.HandleFoul(new FoulEvent(TeamId.Home, 3, TeamId.Away, shooting: false)); // 7th
        match.Tick(Delay * 1.1f, Loose);
        Assert.AreEqual(MatchPhase.FreeThrow, match.State.Phase);
        Assert.AreEqual(3, match.FreeThrowShooter);
    }
}
