using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.AI;

public class OpponentAIStateMachineTests
{
    [Test]
    public void Evaluate_OpponentHasBallFar_TransitionsToGuard()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, new Vector3(10f, 0f, 0f), Vector3.zero, opponentHasBall: true, selfHasBall: false);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.Guard, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_OpponentHasBallClose_TransitionsToContestShot()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, new Vector3(1f, 0f, 0f), Vector3.zero, opponentHasBall: true, selfHasBall: false);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.ContestShot, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_NoOneHasBall_TransitionsToChase()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, Vector3.zero, new Vector3(3f, 0f, 3f), opponentHasBall: false, selfHasBall: false);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.Chase, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_SelfHasBall_TransitionsToAttack()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: true);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.Attack, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_StateChange_FiresOnStateChanged()
    {
        var fsm = new OpponentAIStateMachine();
        bool fired = false;
        fsm.OnStateChanged += (from, to) => fired = true;

        fsm.Evaluate(new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, false, false));

        Assert.IsTrue(fired);
    }

    [Test]
    public void Evaluate_TeammateHasBall_TransitionsToIdle()
    {
        var fsm = new OpponentAIStateMachine();
        fsm.Evaluate(new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: false, teammateHasBall: true));
        // Initial state is already Idle; force a change first so the assertion is meaningful.
        fsm.Evaluate(new AIPerception(Vector3.zero, Vector3.zero, Vector3.one, false, false));
        Assert.AreEqual(AIState.Chase, fsm.CurrentState);

        fsm.Evaluate(new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: false, teammateHasBall: true));
        Assert.AreEqual(AIState.Idle, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_UsesConfiguredContestDistance()
    {
        var config = ScriptableObject.CreateInstance<AIConfig>();
        config.contestDistance = 5f;
        var fsm = new OpponentAIStateMachine(config);

        fsm.Evaluate(new AIPerception(Vector3.zero, new Vector3(4f, 0f, 0f), Vector3.zero, opponentHasBall: true, selfHasBall: false));

        Assert.AreEqual(AIState.ContestShot, fsm.CurrentState);
    }
}
