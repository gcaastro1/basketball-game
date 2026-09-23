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

    [SetUp]
    public void SetUp()
    {
        ballGo = new GameObject("TestBall");
        ballGo.AddComponent<Rigidbody>();
        ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());

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

        var passSystemGo = new GameObject("PassSystem");
        var passSystem = passSystemGo.AddComponent<PassSystem>();
        passSystem.Configure(ball, ScriptableObject.CreateInstance<BallConfig>());

        bool result = passSystem.TryPass(shooterGo.transform, targetGo.transform);

        Assert.IsTrue(result);
        Assert.AreEqual(BallState.Free, ball.CurrentState);
        Assert.Greater(ballGo.GetComponent<Rigidbody>().linearVelocity.magnitude, 0f);
        Object.Destroy(passSystemGo);
    }

    [UnityTest]
    public IEnumerator TryShoot_WhileHolding_ReleasesBallTowardRim()
    {
        yield return null;
        ball.Catch(shooterGo.transform);

        var shootGo = new GameObject("ShootingSystem");
        var shootSystem = shootGo.AddComponent<ShootingSystem>();
        shootSystem.Configure(ball, ScriptableObject.CreateInstance<ShotConfig>(), targetGo.transform);

        bool result = shootSystem.TryShoot(shooterGo.transform);

        Assert.IsTrue(result);
        Assert.AreEqual(BallState.Free, ball.CurrentState);
        Object.Destroy(shootGo);
    }

    [UnityTest]
    public IEnumerator TryPass_WhileNotHolding_ReturnsFalse()
    {
        yield return null;
        var passSystemGo = new GameObject("PassSystem");
        var passSystem = passSystemGo.AddComponent<PassSystem>();
        passSystem.Configure(ball, ScriptableObject.CreateInstance<BallConfig>());

        bool result = passSystem.TryPass(shooterGo.transform, targetGo.transform);

        Assert.IsFalse(result);
        Object.Destroy(passSystemGo);
    }

    // Regression test for a bug in TrajectoryMath.ComputeArcVelocity where the
    // trajectory apex could end up below an elevated target (fixed in Task 9's
    // review). ShootingSystem is the real-world consumer of this exact scenario:
    // a rim sits above the shooter's release height. No existing test anywhere
    // in the suite exercised an elevated target, so this closes that gap.
    [UnityTest]
    public IEnumerator TryShoot_TargetAboveOrigin_ReleasesBallWithPositiveUpwardVelocity()
    {
        yield return null;
        ball.Catch(shooterGo.transform);
        targetGo.transform.position = new Vector3(4f, 2f, 0f);

        var shootGo = new GameObject("ShootingSystem");
        var shootSystem = shootGo.AddComponent<ShootingSystem>();
        shootSystem.Configure(ball, ScriptableObject.CreateInstance<ShotConfig>(), targetGo.transform);

        bool result = shootSystem.TryShoot(shooterGo.transform);

        Assert.IsTrue(result);
        Assert.Greater(ballGo.GetComponent<Rigidbody>().linearVelocity.y, 0f);
        Object.Destroy(shootGo);
    }
}
