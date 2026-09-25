using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class PassAndShootSystemTests
{
    private GameObject ballGo, shooterGo, targetGo;
    private BallController ball;
    private BallConfig ballConfig;

    [SetUp]
    public void SetUp()
    {
        ballGo = new GameObject("TestBall");
        ballGo.AddComponent<Rigidbody>();
        ball = ballGo.AddComponent<BallController>();
        ballConfig = ScriptableObject.CreateInstance<BallConfig>();
        ball.SetConfigForTest(ballConfig);

        shooterGo = new GameObject("Shooter");
        targetGo = new GameObject("Target");
        targetGo.transform.position = new Vector3(4f, 0f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(ballGo);
        Object.Destroy(shooterGo);
        Object.Destroy(targetGo);
    }

    [UnityTest]
    public IEnumerator TryPass_WhileHolding_ReleasesBallTowardTarget()
    {
        yield return null;
        ball.Catch(shooterGo.transform);
        var passSystem = new PassSystem(ball, ballConfig);

        bool result = passSystem.TryPass(shooterGo.transform, targetGo.transform);

        Assert.IsTrue(result);
        Assert.AreEqual(BallState.Passing, ball.CurrentState);
        Vector3 v = ballGo.GetComponent<Rigidbody>().linearVelocity;
        Assert.Greater(v.x, 0f, "heads toward the target");
    }

    [UnityTest]
    public IEnumerator TryShoot_WhileHolding_ReleasesBallTowardRim()
    {
        yield return null;
        ball.Catch(shooterGo.transform);
        var shootSystem = new ShootingSystem(ball, ScriptableObject.CreateInstance<ShotConfig>(), targetGo.transform.position + Vector3.up * 3f);

        bool result = shootSystem.TryShoot(shooterGo.transform);

        Assert.IsTrue(result);
        Assert.AreEqual(BallState.Shooting, ball.CurrentState);
        Assert.Greater(ballGo.GetComponent<Rigidbody>().linearVelocity.y, 0f);
    }

    [UnityTest]
    public IEnumerator TryPass_WhileNotHolding_ReturnsFalse()
    {
        yield return null;
        var passSystem = new PassSystem(ball, ballConfig);

        bool result = passSystem.TryPass(shooterGo.transform, targetGo.transform);

        Assert.IsFalse(result);
    }
}
