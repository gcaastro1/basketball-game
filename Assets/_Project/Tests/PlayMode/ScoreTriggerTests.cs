using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class ScoreTriggerTests
{
    [UnityTest]
    public IEnumerator BallEnteringTrigger_CallsNotifyScoredOnBall()
    {
        var ballGo = new GameObject("Ball");
        var ballCollider = ballGo.AddComponent<SphereCollider>();
        ballGo.AddComponent<Rigidbody>();
        var ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());
        yield return null;

        bool scored = false;
        ball.OnScored += _ => scored = true;

        var rimGo = new GameObject("Rim");
        var rimCollider = rimGo.AddComponent<SphereCollider>();
        rimCollider.isTrigger = true;
        rimCollider.radius = 5f;
        var trigger = rimGo.AddComponent<ScoreTrigger>();
        trigger.Configure(ball);

        ballGo.transform.position = rimGo.transform.position;
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.IsTrue(scored);

        Object.Destroy(ballGo);
        Object.Destroy(rimGo);
    }
}
