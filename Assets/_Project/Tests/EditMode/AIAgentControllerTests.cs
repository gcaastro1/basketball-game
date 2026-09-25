using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.AI;

public class AIAgentControllerTests
{
    private static readonly Vector3 Hoop = new Vector3(0f, 3.05f, 13.1f);

    private static AIPerception WithBall(Vector3 self, float time) =>
        new AIPerception(self, Vector3.zero, self, opponentHasBall: false, selfHasBall: true,
            teammateHasBall: false, attackHoop: Hoop, defendHoop: Hoop, time: time);

    [Test]
    public void LooseBall_ChasesBall()
    {
        var ai = new AIAgentController();
        var cmd = ai.Decide(new AIPerception(Vector3.zero, new Vector3(-5f, 0f, 0f), new Vector3(3f, 0f, 0f), false, false));

        Assert.AreEqual(AIState.Chase, ai.CurrentState);
        Assert.Greater(cmd.Move.x, 0f);
        Assert.IsTrue(cmd.Sprint);
    }

    [Test]
    public void WithBall_FarFromHoop_DrivesTowardHoopWithoutShooting()
    {
        var ai = new AIAgentController();
        var cmd = ai.Decide(WithBall(new Vector3(0f, 0f, 1f), time: 0f));
        cmd = ai.Decide(WithBall(new Vector3(0f, 0f, 1f), time: 5f));

        Assert.AreEqual(AIState.Attack, ai.CurrentState);
        Assert.Greater(cmd.Move.y, 0f);
        Assert.IsFalse(cmd.Shoot);
    }

    [Test]
    public void WithBall_InRange_ShootsOnlyAfterMinimumHold()
    {
        var config = ScriptableObject.CreateInstance<AIConfig>();
        var ai = new AIAgentController(config);
        Vector3 inRange = new Vector3(0f, 0f, Hoop.z - config.shootRange + 0.5f);

        Assert.IsFalse(ai.Decide(WithBall(inRange, time: 10f)).Shoot, "just caught: must hold first");
        Assert.IsTrue(ai.Decide(WithBall(inRange, time: 10f + config.minHoldSecondsBeforeShot)).Shoot);
    }

    [Test]
    public void Guard_StandsBetweenManAndHoop()
    {
        var ai = new AIAgentController();
        Vector3 man = new Vector3(0f, 0f, 5f);
        // Self is beside the man, far enough not to contest.
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

        var ai = new AIAgentController();
        ai.Decide(s, 0);

        Assert.AreEqual(AIState.ContestShot, ai.CurrentState);
    }
}
