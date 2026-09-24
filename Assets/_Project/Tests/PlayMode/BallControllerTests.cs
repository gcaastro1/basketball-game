using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class BallControllerTests
{
    private GameObject ballGo;
    private BallController ball;
    private GameObject holderGo;

    [SetUp]
    public void SetUp()
    {
        ballGo = new GameObject("TestBall");
        ballGo.AddComponent<Rigidbody>();
        ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());

        holderGo = new GameObject("TestHolder");
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(ballGo);
        Object.Destroy(holderGo);
    }

    [UnityTest]
    public IEnumerator Catch_FromFree_TransitionsToHeldAndFollowsHolder()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        Assert.AreEqual(BallState.Held, ball.CurrentState);
        Assert.AreEqual(holderGo.transform, ball.CurrentHolder);
    }

    [UnityTest]
    public IEnumerator Release_FromHeld_TransitionsThroughToFreeAndAppliesVelocity()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, new Vector3(0f, 5f, 3f));

        Assert.AreEqual(BallState.Free, ball.CurrentState);
        Assert.AreEqual(new Vector3(0f, 5f, 3f), ballGo.GetComponent<Rigidbody>().linearVelocity);
    }

    [UnityTest]
    public IEnumerator NotifyScored_FiresOnScoredEvent()
    {
        yield return null;
        bool fired = false;
        ball.OnScored += _ => fired = true;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, Vector3.up);

        ball.NotifyScored();

        Assert.IsTrue(fired);
    }

    [UnityTest]
    public IEnumerator TryCatchNearby_PlayerWithinRadius_CatchesBall()
    {
        yield return null;
        holderGo.transform.position = ballGo.transform.position; // well within default catchRadius (1.0)

        bool caught = ball.TryCatchNearby(holderGo.transform);

        Assert.IsTrue(caught);
        Assert.AreEqual(BallState.Held, ball.CurrentState);
        Assert.AreEqual(holderGo.transform, ball.CurrentHolder);
    }

    [UnityTest]
    public IEnumerator TryCatchNearby_PlayerOutsideRadius_DoesNotCatch()
    {
        yield return null;
        holderGo.transform.position = ballGo.transform.position + Vector3.forward * 10f; // far outside default catchRadius (1.0)

        bool caught = ball.TryCatchNearby(holderGo.transform);

        Assert.IsFalse(caught);
        Assert.AreEqual(BallState.Free, ball.CurrentState);
    }
}
