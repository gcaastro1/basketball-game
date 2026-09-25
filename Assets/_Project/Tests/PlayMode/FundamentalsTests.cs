using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class FundamentalsTests
{
    private TestMatch match;

    [SetUp]
    public void SetUp() => match = new TestMatch();

    [TearDown]
    public void TearDown() => match.Dispose();

    [UnityTest]
    public IEnumerator JumpShot_ReleasedAtApex_IsPerfectAndScoresThree()
    {
        match.Start((TeamId.Home, TestMatch.ShootAtApex()));
        yield return match.RunUntil(() => match.Sim.Match.State.ScoreHome > 0, 6f);

        Assert.AreEqual(1, match.Shots.Count);
        Assert.AreEqual(ShotType.JumpShot, match.Shots[0].Type);
        Assert.Less(Mathf.Abs(match.Shots[0].TimingError), 0.1f, "released at the apex (within a frame)");
        Assert.AreEqual(3, match.Sim.Match.State.ScoreHome);
    }

    [UnityTest]
    public IEnumerator JumpShot_TappedShoot_IsReportedEarly()
    {
        float heldSince = -1f;
        match.Start((TeamId.Home, (s, self) =>
        {
            if (s.BallHolderIndex != self) return PlayerCommand.None;
            if (heldSince < 0f) heldSince = s.Time;
            // Press for a single tick after 0.3 s: releases right after takeoff.
            return new PlayerCommand(Vector2.zero, shootHeld: s.Time - heldSince > 0.3f && s.IsGrounded(self));
        }));
        yield return match.RunUntil(() => match.Shots.Count > 0, 3f);

        Assert.AreEqual(1, match.Shots.Count);
        Assert.Less(match.Shots[0].TimingError, -0.2f);
    }

    [UnityTest]
    public IEnumerator Layup_NearTheRim_ReleasesByItselfAndScoresTwo()
    {
        match.Court.checkBallSpot = new Vector3(0f, 0f, match.Court.rimCenter.z - 1.8f);
        match.Start((TeamId.Home, TestMatch.ShootAtApex()));
        yield return match.RunUntil(() => match.Sim.Match.State.ScoreHome > 0, 5f);

        Assert.AreEqual(ShotType.Layup, match.Shots[0].Type);
        Assert.AreEqual(2, match.Sim.Match.State.ScoreHome);
    }

    [UnityTest]
    public IEnumerator Dunk_SprintingNearTheRim_ScoresTwo()
    {
        match.Court.checkBallSpot = new Vector3(0f, 0f, match.Court.rimCenter.z - 1.8f);
        match.Start((TeamId.Home, TestMatch.ShootAtApex(sprint: true)));
        yield return match.RunUntil(() => match.Sim.Match.State.ScoreHome > 0, 5f);

        Assert.AreEqual(ShotType.Dunk, match.Shots[0].Type);
        Assert.AreEqual(2, match.Sim.Match.State.ScoreHome);
    }

    [UnityTest]
    public IEnumerator Block_DefenderJumpingInFront_DeflectsTheShot()
    {
        match.Court.defenderGap = 0.7f;
        match.Start(
            (TeamId.Home, TestMatch.ShootAtApex()),
            // Jumps as soon as the shooter leaves the ground.
            (TeamId.Away, (s, self) => new PlayerCommand(Vector2.zero, jump: s.BallHolderIndex == 0 && !s.IsGrounded(0))));
        yield return match.RunUntil(() => match.HasEvent("BLOCK") || match.Sim.Match.State.ScoreHome > 0, 4f);

        Assert.IsTrue(match.HasEvent("BLOCK"), string.Join("\n", match.Events));
        Assert.AreEqual(0, match.Sim.Match.State.ScoreHome);
        Assert.AreEqual(TeamId.Away, match.Sim.Ball.LastTouchTeam);
    }

    [UnityTest]
    public IEnumerator Block_DefenderFarAway_CannotBlock()
    {
        match.Court.defenderGap = 3f;
        match.Start(
            (TeamId.Home, TestMatch.ShootAtApex()),
            (TeamId.Away, (s, self) => new PlayerCommand(Vector2.zero, jump: s.BallHolderIndex == 0 && !s.IsGrounded(0))));
        yield return match.RunUntil(() => match.Sim.Match.State.ScoreHome > 0, 6f);

        Assert.IsFalse(match.HasEvent("BLOCK"));
        Assert.AreEqual(3, match.Sim.Match.State.ScoreHome);
    }

    [UnityTest]
    public IEnumerator Steal_FacingTheHandlerUpClose_KnocksTheBallLoose()
    {
        match.Court.defenderGap = 1f;
        match.DefenseConfig.stealBaseChance = 1f;
        match.Start(
            (TeamId.Home, TestMatch.Idle),
            (TeamId.Away, (s, self) => new PlayerCommand(Vector2.zero, steal: s.Time > 0.2f)));
        PlayerEntity handler = match.Players[0];
        yield return match.RunUntil(() => match.HasEvent("STEAL"), 2f);

        Assert.IsTrue(match.HasEvent("STEAL"));
        Assert.AreNotSame(handler.transform, match.Sim.Ball.CurrentHolder);
        Assert.AreEqual(TeamId.Away, match.Sim.Ball.LastTouchTeam);
    }

    [UnityTest]
    public IEnumerator Steal_OnFailure_WaitsForCooldownBeforeNextAttempt()
    {
        match.Court.defenderGap = 1f;
        match.DefenseConfig.stealBaseChance = 0f;
        match.DefenseConfig.stealMovingHandlerBonus = 0f;
        match.Start(
            (TeamId.Home, TestMatch.Idle),
            (TeamId.Away, (s, self) => new PlayerCommand(Vector2.zero, steal: true)));
        yield return match.RunUntil(() => false, 0.5f);

        Assert.AreSame(match.Players[0].transform, match.Sim.Ball.CurrentHolder, "a failed reach does not take the ball");
        Assert.IsFalse(match.HasEvent("STEAL"));
    }
}
