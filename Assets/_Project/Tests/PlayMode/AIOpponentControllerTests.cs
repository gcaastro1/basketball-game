using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.AI;

public class AIOpponentControllerTests
{
    [UnityTest]
    public IEnumerator Tick_LooseBall_MovesTowardBall()
    {
        var go = new GameObject("AI");
        var ai = go.AddComponent<AIOpponentController>();
        yield return null;

        var perception = new AIPerception(
            selfPosition: Vector3.zero,
            opponentPosition: new Vector3(-5f, 0f, 0f),
            ballPosition: new Vector3(3f, 0f, 0f),
            opponentHasBall: false,
            selfHasBall: false);

        ai.Tick(perception);

        Assert.AreEqual(AIState.Chase, ai.CurrentState);
        Assert.Greater(ai.GetMoveInput().x, 0f);
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Tick_SelfHasBall_WantsShootIsTrue()
    {
        var go = new GameObject("AI");
        var ai = go.AddComponent<AIOpponentController>();
        yield return null;

        var perception = new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: true);
        ai.Tick(perception);

        Assert.IsTrue(ai.WantsShoot());
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator WantsShoot_CalledAgainImmediately_ReturnsFalseDuringCooldown()
    {
        var go = new GameObject("AI");
        var ai = go.AddComponent<AIOpponentController>();
        yield return null;

        var perception = new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: true);
        ai.Tick(perception);

        Assert.IsTrue(ai.WantsShoot(), "first call should want to shoot");
        Assert.IsFalse(ai.WantsShoot(), "immediate second call should be suppressed by the shot cooldown");

        Object.Destroy(go);
    }
}
