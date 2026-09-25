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

    // Regression (AI-vs-AI simulation): the on-ball defender standing next to the passer
    // used to "catch" the ball the instant it left the passer's hands.
    [UnityTest]
    public IEnumerator Pass_WithDefenderRightNextToThePasser_ReachesTheTeammate()
    {
        match.Court.defenderGap = 1.0f;
        bool passed = false;
        match.Start(
            (TeamId.Home, (s, self) =>
            {
                bool pass = !passed && s.BallHolderIndex == self && s.Phase == MatchPhase.Live && s.Time > 0.5f;
                if (pass) passed = true;
                return new PlayerCommand(Vector2.zero, pass: pass);
            }),
            (TeamId.Home, TestMatch.Idle),
            (TeamId.Away, TestMatch.Idle));
        yield return match.RunUntil(() => passed && match.Sim.Snapshot.BallHolderIndex >= 0 && match.Sim.Snapshot.BallHolderIndex != 0, 4f);

        Assert.IsTrue(passed, "the pass was thrown");
        Assert.AreEqual(1, match.Sim.Snapshot.BallHolderIndex, string.Join("\n", match.Events));
        Assert.AreEqual(1, match.Sim.Stats.Get(TeamId.Home).PassesCompleted);
    }

    // Regression (AI-vs-AI log): a defender hugging the passer and standing on the passing
    // line caught the pass just after the old 0.2 s protection ran out.
    [UnityTest]
    public IEnumerator Pass_WithDefenderOnThePassingLineNextToThePasser_ReachesTheTeammate()
    {
        bool passed = false;
        match.Start(
            (TeamId.Home, (s, self) =>
            {
                bool pass = !passed && s.BallHolderIndex == self && s.Phase == MatchPhase.Live && s.Time > 0.5f;
                if (pass) passed = true;
                return new PlayerCommand(Vector2.zero, pass: pass);
            }),
            (TeamId.Home, TestMatch.Idle),
            (TeamId.Away, TestMatch.Idle));
        Vector3 passer = match.Players[0].FeetPosition, receiver = match.Players[1].FeetPosition;
        Vector3 lane = receiver - passer;
        lane.y = 0f;
        match.Players[2].TeleportFeetTo(passer + lane.normalized * 0.8f, -lane);

        yield return match.RunUntil(() => passed && match.Sim.Snapshot.BallHolderIndex >= 0 && match.Sim.Snapshot.BallHolderIndex != 0, 4f);

        Assert.IsTrue(passed, "the pass was thrown");
        Assert.AreEqual(1, match.Sim.Snapshot.BallHolderIndex, string.Join("\n", match.Events));
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

    [UnityTest]
    public IEnumerator ShootingFoul_ContactAtRelease_SendsShooterToTheLine()
    {
        match.Court.defenderGap = 0.7f;
        match.DefenseConfig.shootingFoulChance = 1f;
        match.Start(
            (TeamId.Home, TestMatch.ShootAtApex()),
            (TeamId.Away, (s, self) => new PlayerCommand(Vector2.zero, jump: s.BallHolderIndex == 0 && !s.IsGrounded(0))));
        // Ends when the free throws are over and the other team has the ball.
        yield return match.RunUntil(() => match.Shots.Exists(r => r.Type == ShotType.FreeThrow)
                                           && match.Sim.Match.State.Phase == MatchPhase.Live
                                           && match.Sim.Match.State.PossessionTeam == TeamId.Away, 15f);

        Assert.IsTrue(match.HasEvent("FOUL on Away (shooting)"), string.Join("\n", match.Events));
        int freeThrows = match.Shots.FindAll(r => r.Type == ShotType.FreeThrow).Count;
        // Blocked (missed) three -> 3 free throws; or it went in anyway -> and-one.
        Assert.That(freeThrows == 3 || freeThrows == 1, $"free throws taken: {freeThrows}");
        int basket = freeThrows == 1 ? 3 : 0;
        Assert.AreEqual(basket + freeThrows, match.Sim.Match.State.ScoreHome, "each free throw made is one point");
    }
}
