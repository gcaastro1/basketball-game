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
    public void Evaluate_SelfHasBall_TransitionsToIdle()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: true);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.Idle, fsm.CurrentState);
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
}
