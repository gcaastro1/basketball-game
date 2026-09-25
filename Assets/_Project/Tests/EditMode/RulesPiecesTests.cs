using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class RulesPiecesTests
{
    [Test]
    public void GameClock_CountsDownToZeroAndStops()
    {
        var c = new GameClock();
        c.StartPeriod(1, 2f, overtime: false);
        c.Tick(1.5f);
        Assert.AreEqual(0.5f, c.Remaining, 1e-5f);
        c.Tick(3f);
        Assert.AreEqual(0f, c.Remaining);
        Assert.IsTrue(c.Expired);
    }

    [Test]
    public void ShotClock_PausesWhenNotPossessed()
    {
        var c = new ShotClock();
        c.Reset(12f);
        c.Tick(4f, possessed: false);
        Assert.AreEqual(12f, c.Remaining);
        c.Tick(4f, possessed: true);
        Assert.AreEqual(8f, c.Remaining);
    }

    private static MatchRules Fiba()
    {
        var r = ScriptableObject.CreateInstance<MatchRules>();
        r.teamFoulPenaltyThreshold = 7;
        r.teamFoulBonusPossessionThreshold = 10;
        r.penaltyFreeThrows = 2;
        r.shootingFoulFreeThrowsInsideArc = 1;
        r.shootingFoulFreeThrowsBeyondArc = 2;
        r.andOneFreeThrows = 1;
        return r;
    }

    [TestCase(1, 0)]
    [TestCase(6, 0)]
    [TestCase(7, 2)]
    [TestCase(9, 2)]
    [TestCase(10, 2)]
    public void NonShootingFoul_FreeThrowsByTeamFouls(int fouls, int expectedFreeThrows)
    {
        Assert.AreEqual(expectedFreeThrows, FoulRules.NonShooting(Fiba(), fouls).FreeThrows);
    }

    [Test]
    public void NonShootingFoul_TenthFoul_AlsoKeepsPossession()
    {
        Assert.AreEqual(AfterFreeThrows.LiveReboundOnMiss, FoulRules.NonShooting(Fiba(), 9).After);
        Assert.AreEqual(AfterFreeThrows.FouledTeamPossession, FoulRules.NonShooting(Fiba(), 10).After);
    }

    [Test]
    public void ShootingFoul_FreeThrowsByZoneAndOutcome()
    {
        Assert.AreEqual(1, FoulRules.Shooting(Fiba(), 1, shotScored: false, beyondArc: false).FreeThrows);
        Assert.AreEqual(2, FoulRules.Shooting(Fiba(), 1, shotScored: false, beyondArc: true).FreeThrows);
        Assert.AreEqual(1, FoulRules.Shooting(Fiba(), 1, shotScored: true, beyondArc: true).FreeThrows, "and-one");
    }

    private static MatchSnapshot ShooterAndDefender(Vector3 defenderPos, Vector3 defenderVelocity, bool defenderGrounded)
    {
        var s = new MatchSnapshot(2);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero, isGrounded: false);
        s.SetPlayer(1, TeamId.Away, defenderPos, defenderVelocity, defenderGrounded);
        return s;
    }

    [Test]
    public void ShootingContact_AirborneDefenderUpClose_IsContact()
    {
        var s = ShooterAndDefender(new Vector3(0f, 0.5f, 0.7f), Vector3.zero, defenderGrounded: false);
        Assert.AreEqual(1, FoulMath.ShootingContact(s, 0, 0.8f, 1.5f));
    }

    [Test]
    public void ShootingContact_StillGroundedDefender_IsNotContact()
    {
        var s = ShooterAndDefender(new Vector3(0f, 0f, 0.7f), Vector3.zero, defenderGrounded: true);
        Assert.AreEqual(-1, FoulMath.ShootingContact(s, 0, 0.8f, 1.5f));
    }

    [Test]
    public void ShootingContact_DefenderClosingFast_IsContact()
    {
        var s = ShooterAndDefender(new Vector3(0f, 0f, 0.7f), new Vector3(0f, 0f, -3f), defenderGrounded: true);
        Assert.AreEqual(1, FoulMath.ShootingContact(s, 0, 0.8f, 1.5f));
    }

    [Test]
    public void ShootingContact_FarDefender_IsNotContact()
    {
        var s = ShooterAndDefender(new Vector3(0f, 0.5f, 2f), Vector3.zero, defenderGrounded: false);
        Assert.AreEqual(-1, FoulMath.ShootingContact(s, 0, 0.8f, 1.5f));
    }

    [Test]
    public void MatchState_FreeThrowPoints_CanEndTheMatch()
    {
        var state = new MatchState(winningScore: 3);
        state.StartLivePlay();
        state.RegisterScore(TeamId.Home, 2);
        state.AddPoints(TeamId.Home, 1);
        Assert.AreEqual(MatchPhase.Ended, state.Phase);
        Assert.AreEqual(TeamId.Home, state.Winner);
    }
}
