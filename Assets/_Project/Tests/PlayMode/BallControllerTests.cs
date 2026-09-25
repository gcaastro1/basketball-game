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
    public IEnumerator Release_FromHeld_StaysInFlightStateAndAppliesVelocity()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, new Vector3(0f, 5f, 3f));

        Assert.AreEqual(BallState.Shooting, ball.CurrentState);
        Assert.IsTrue(ball.HasLiveRelease);
        Assert.AreEqual(new Vector3(0f, 5f, 3f), ballGo.GetComponent<Rigidbody>().linearVelocity);
    }

    [UnityTest]
    public IEnumerator NotifyScored_AfterRelease_FiresOnce()
    {
        yield return null;
        int fired = 0;
        ball.OnScored += _ => fired++;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, Vector3.up);

        ball.NotifyScored();
        ball.NotifyScored();

        Assert.AreEqual(1, fired);
    }

    [UnityTest]
    public IEnumerator NotifyScored_WithoutRelease_DoesNotFire()
    {
        yield return null;
        bool fired = false;
        ball.OnScored += _ => fired = true;

        ball.NotifyScored();

        Assert.IsFalse(fired);
    }

    [UnityTest]
    public IEnumerator ShotInFlight_CannotBeCaughtNearby()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, Vector3.up);
        var other = new GameObject("Other");
        other.transform.position = ballGo.transform.position;

        Assert.IsFalse(ball.TryCatchNearby(other.transform));
        Object.Destroy(other);
    }

    [UnityTest]
    public IEnumerator PassInFlight_CanBeCaughtNearby()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Passing, Vector3.up);
        var receiver = new GameObject("Receiver");
        receiver.transform.position = ballGo.transform.position;

        Assert.IsTrue(ball.TryCatchNearby(receiver.transform));
        Assert.AreEqual(BallState.Held, ball.CurrentState);
        Assert.IsFalse(ball.HasLiveRelease);
        Object.Destroy(receiver);
    }

    [UnityTest]
    public IEnumerator ResetToHolder_FromAnyState_GivesBallToHolder()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, Vector3.up);
        var other = new GameObject("Other");

        ball.ResetToHolder(other.transform);

        Assert.AreEqual(BallState.Held, ball.CurrentState);
        Assert.AreEqual(other.transform, ball.CurrentHolder);
        Assert.IsTrue(ballGo.GetComponent<Rigidbody>().isKinematic);
        Object.Destroy(other);
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

    [UnityTest]
    public IEnumerator Player_CanOnlyCatchBallsWithinReach()
    {
        PlayerEntity player = PlaceholderPlayerFactory.Create("Rebounder", TeamId.Home,
            ScriptableObject.CreateInstance<PlayerMovementConfig>(), Color.gray, Color.yellow);
        yield return null;
        Vector3 feet = player.FeetPosition;
        ballGo.GetComponent<Rigidbody>().isKinematic = true; // hold the ball in place for the check

        ballGo.transform.position = feet + new Vector3(0.3f, 2.9f, 0f);
        Assert.IsFalse(ball.CanBeCaughtBy(player.transform), "above standing reach");

        ballGo.transform.position = feet + new Vector3(0.3f, 2.0f, 0f);
        Assert.IsTrue(ball.CanBeCaughtBy(player.transform));

        Object.Destroy(player.gameObject);
    }

    [UnityTest]
    public IEnumerator KnockLoose_FromHeld_FreesBallAndBlocksImmediateRegrab()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        ball.KnockLoose(Vector3.right, TeamId.Away);

        Assert.AreEqual(BallState.Free, ball.CurrentState);
        Assert.IsNull(ball.CurrentHolder);
        Assert.AreEqual(TeamId.Away, ball.LastTouchTeam);
        holderGo.transform.position = ballGo.transform.position;
        Assert.IsFalse(ball.TryCatchNearby(holderGo.transform), "previous holder is in the grace period");
    }
}
