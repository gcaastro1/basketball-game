using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.AI;

public class AIAgentControllerTests
{
    private static readonly Vector3 Hoop = new Vector3(0f, 3.05f, 13.1f);

    private static AIConfig Config(float driveChance = 0f, float jitter = 0f)
    {
        var c = ScriptableObject.CreateInstance<AIConfig>();
        c.driveChance = driveChance;
        c.releaseTimingJitterSeconds = jitter;
        return c;
    }

    private static AIPerception WithBall(Vector3 self, float time, bool grounded = true, float vy = 0f, float vz = 0f) =>
        new AIPerception(self, new Vector3(20f, 0f, 0f), self, opponentHasBall: false, selfHasBall: true,
            attackHoop: Hoop, defendHoop: Hoop, time: time, selfVelocity: new Vector3(0f, vy, vz), selfGrounded: grounded);

    [Test]
    public void LooseBall_ChasesBall()
    {
        var ai = new AIAgentController(Config());
        var cmd = ai.Decide(new AIPerception(Vector3.zero, new Vector3(-5f, 0f, 0f), new Vector3(3f, 0f, 0f), false, false));

        Assert.AreEqual(AIState.Chase, ai.CurrentState);
        Assert.Greater(cmd.Move.x, 0f);
        Assert.IsTrue(cmd.Sprint);
    }

    [Test]
    public void WithBall_FarFromHoop_DrivesTowardHoopWithoutShooting()
    {
        var ai = new AIAgentController(Config());
        ai.Decide(WithBall(new Vector3(0f, 0f, 1f), time: 0f));
        var cmd = ai.Decide(WithBall(new Vector3(0f, 0f, 1f), time: 5f));

        Assert.AreEqual(AIState.Attack, ai.CurrentState);
        Assert.Greater(cmd.Move.y, 0f);
        Assert.IsFalse(cmd.ShootHeld);
    }

    [Test]
    public void JumpShot_HoldsUntilApexThenReleases()
    {
        var config = Config();
        var ai = new AIAgentController(config);
        Vector3 inRange = new Vector3(0f, 0f, Hoop.z - config.shootRange + 0.5f);

        Assert.IsFalse(ai.Decide(WithBall(inRange, 10f)).ShootHeld, "just caught: must hold first");
        Assert.IsTrue(ai.Decide(WithBall(inRange, 10.6f)).ShootHeld, "starts the shot");
        Assert.IsTrue(ai.Decide(WithBall(inRange, 10.7f, grounded: false, vy: 2f)).ShootHeld, "still rising");
        Assert.IsFalse(ai.Decide(WithBall(inRange, 10.9f, grounded: false, vy: -0.01f)).ShootHeld, "released at the apex");
    }

    [Test]
    public void JumpShot_OnTheRun_StopsAndSetsFeetFirst()
    {
        var config = Config();
        var ai = new AIAgentController(config);
        Vector3 inRange = new Vector3(0f, 0f, Hoop.z - config.shootRange + 0.5f);
        ai.Decide(WithBall(inRange, 10f));

        var running = ai.Decide(WithBall(inRange, 10.6f, vz: 5f));
        Assert.IsFalse(running.ShootHeld, "does not rise while running");
        Assert.AreEqual(Vector2.zero, running.Move, "stops instead");
        Assert.IsTrue(ai.Decide(WithBall(inRange, 10.8f, vz: config.setFeetSpeed * 0.5f)).ShootHeld, "shoots once set");
    }

    [Test]
    public void Drive_SprintsToTheRimBeforeShooting()
    {
        var config = Config(driveChance: 1f);
        var ai = new AIAgentController(config);
        Vector3 jumperRange = new Vector3(0f, 0f, Hoop.z - config.shootRange + 0.5f);

        ai.Decide(WithBall(jumperRange, 0f));
        var cmd = ai.Decide(WithBall(jumperRange, 1f));
        Assert.IsFalse(cmd.ShootHeld, "a drive does not settle for the jumper");
        Assert.IsTrue(cmd.Sprint);

        Vector3 atRim = new Vector3(0f, 0f, Hoop.z - config.driveFinishDistance + 0.2f);
        cmd = ai.Decide(WithBall(atRim, 1.5f));
        Assert.IsTrue(cmd.ShootHeld);
        Assert.IsTrue(cmd.Sprint, "sprinting into the finish (dunk attempt)");
    }

    [Test]
    public void Defender_JumpsToBlockAShooterNearTheApex()
    {
        var ai = new AIAgentController(Config());
        var p = new AIPerception(Vector3.zero, new Vector3(0f, 0f, 1f), Vector3.zero, opponentHasBall: true, selfHasBall: false,
            attackHoop: Hoop, defendHoop: Hoop, opponentVelocity: new Vector3(0f, 1f, 0f), opponentGrounded: false);

        Assert.IsTrue(ai.Decide(p).Jump);
    }

    [Test]
    public void Defender_DoesNotJumpAtAGroundedHandler_ButReachesForTheBall()
    {
        var ai = new AIAgentController(Config());
        var p = new AIPerception(Vector3.zero, new Vector3(0f, 0f, 1f), Vector3.zero, opponentHasBall: true, selfHasBall: false,
            attackHoop: Hoop, defendHoop: Hoop, time: 5f);

        var cmd = ai.Decide(p);
        Assert.IsFalse(cmd.Jump);
        Assert.IsTrue(cmd.Steal);
        Assert.IsFalse(ai.Decide(p).Steal, "waits before reaching again");
    }

    [Test]
    public void Rebound_JumpsForAHighDescendingBallOverhead()
    {
        var ai = new AIAgentController(Config());
        var p = new AIPerception(Vector3.zero, new Vector3(5f, 0f, 0f), new Vector3(0.3f, 2.8f, 0f), false, false,
            ballVelocity: new Vector3(0f, -2f, 0f), ballState: BallState.Free);

        Assert.IsTrue(ai.Decide(p).Jump);
    }

    [Test]
    public void Guard_StandsBetweenManAndHoop()
    {
        var ai = new AIAgentController(Config());
        Vector3 man = new Vector3(0f, 0f, 5f);
        var p = new AIPerception(new Vector3(6f, 0f, 5f), man, man, opponentHasBall: true, selfHasBall: false,
            attackHoop: Hoop, defendHoop: Hoop);

        var cmd = ai.Decide(p);

        Assert.AreEqual(AIState.Guard, ai.CurrentState);
        Assert.Less(cmd.Move.x, 0f, "moves back toward the man's line");
        Assert.Greater(cmd.Move.y, 0f, "on the hoop side of the man");
    }

    [Test]
    public void Decide_FromSnapshot_UsesPerceptionOfThatPlayer()
    {
        var s = new MatchSnapshot(2);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Away, new Vector3(0f, 0f, 2f), Vector3.zero);
        s.SetAttackingHoop(TeamId.Home, Hoop);
        s.SetAttackingHoop(TeamId.Away, Hoop);
        s.SetBall(new Vector3(0f, 1f, 2f), BallState.Held, 1);

        var ai = new AIAgentController(Config());
        ai.Decide(s, 0);

        Assert.AreEqual(AIState.ContestShot, ai.CurrentState);
    }

    [Test]
    public void OffBallDefender_GuardsHisMan_WithoutJumpingOrReaching()
    {
        var ai = new AIAgentController(Config());
        Vector3 man = new Vector3(3f, 0f, 8f);
        var p = new AIPerception(new Vector3(3f, 0f, 8.8f), man, Vector3.zero, opponentHasBall: true, selfHasBall: false,
            attackHoop: Hoop, defendHoop: Hoop, time: 5f, opponentVelocity: new Vector3(0f, 1f, 0f), opponentGrounded: false,
            focusHasBall: false);

        var cmd = ai.Decide(p);
        Assert.AreEqual(AIState.Guard, ai.CurrentState);
        Assert.IsFalse(cmd.Jump);
        Assert.IsFalse(cmd.Steal);
    }

    [Test]
    public void LooseBall_OnlyTheClosestTeammateChases()
    {
        var ai = new AIAgentController(Config());
        ai.Decide(new AIPerception(Vector3.zero, new Vector3(-5f, 0f, 0f), new Vector3(3f, 0f, 0f), false, false, closestToBall: false));
        Assert.AreEqual(AIState.Guard, ai.CurrentState);
    }

    [Test]
    public void MustClear_TakesTheBallBeyondTheArcInsteadOfShooting()
    {
        var ai = new AIAgentController(Config());
        Vector3 nearRim = new Vector3(0f, 0f, Hoop.z - 3f);
        var p = new AIPerception(nearRim, new Vector3(20f, 0f, 0f), nearRim, opponentHasBall: false, selfHasBall: true,
            attackHoop: Hoop, defendHoop: Hoop, time: 5f, mustClear: true);
        ai.Decide(p);
        var cmd = ai.Decide(p);

        Assert.IsFalse(cmd.ShootHeld);
        Assert.Less(cmd.Move.y, 0f, "moves away from the rim, toward the arc");
    }

    [Test]
    public void MustClear_GoesAroundTheDefenderInTheWay()
    {
        var s = new MatchSnapshot(2);
        s.SetPlayer(0, TeamId.Home, new Vector3(0f, 0f, 11.6f), Vector3.zero);
        s.SetPlayer(1, TeamId.Away, new Vector3(0.1f, 0f, 10.8f), Vector3.zero);   // right on the clear line
        s.SetMatchup(0, 1);
        s.SetMatchup(1, 0);
        s.SetAttackingHoop(TeamId.Home, Hoop);
        s.SetAttackingHoop(TeamId.Away, Hoop);
        s.SetBall(new Vector3(0f, 1f, 11.6f), BallState.Held, 0);
        s.SetMatch(MatchPhase.Live, 5f, shotClock: 10f, ballMustBeCleared: true);

        var ai = new AIAgentController(Config());
        ai.Decide(s, 0);
        var cmd = ai.Decide(s, 0);

        Assert.IsFalse(cmd.ShootHeld);
        Assert.Less(cmd.Move.y, 0f, "still heads out toward the arc");
        Assert.Less(cmd.Move.x, -0.3f, "steps around the defender instead of into him");
    }

    [Test]
    public void FreeThrow_ShootsEvenWhenItWouldOtherwiseDrive()
    {
        var ai = new AIAgentController(Config(driveChance: 1f));
        Vector3 line = new Vector3(0f, 0f, Hoop.z - 4.2f);
        var p1 = new AIPerception(line, new Vector3(20f, 0f, 0f), line, false, true, attackHoop: Hoop, defendHoop: Hoop, time: 1f, phase: MatchPhase.FreeThrow);
        var p2 = new AIPerception(line, new Vector3(20f, 0f, 0f), line, false, true, attackHoop: Hoop, defendHoop: Hoop, time: 2f, phase: MatchPhase.FreeThrow);
        ai.Decide(p1);
        Assert.IsTrue(ai.Decide(p2).ShootHeld);
    }

    [Test]
    public void ShotClockAboutToExpire_ShootsFromAnywhere()
    {
        var ai = new AIAgentController(Config());
        Vector3 far = new Vector3(0f, 0f, 1f);
        var p = new AIPerception(far, new Vector3(20f, 0f, 0f), far, false, true, attackHoop: Hoop, defendHoop: Hoop, time: 0f, shotClock: 1f);
        Assert.IsTrue(ai.Decide(p).ShootHeld);
    }

    [Test]
    public void ShotClockAboutToExpire_ShootsEvenWhileMoving()
    {
        var ai = new AIAgentController(Config());
        Vector3 far = new Vector3(0f, 0f, 1f);
        var moving = new Vector3(3f, 0f, 2f);
        var p = new AIPerception(far, new Vector3(20f, 0f, 0f), far, false, true, attackHoop: Hoop, defendHoop: Hoop, time: 0f,
            selfVelocity: moving, shotClock: 1f);
        Assert.IsTrue(ai.Decide(p).ShootHeld);
    }
}
