using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Gameplay;

public class DribbleSystemTests
{
    [UnityTest]
    public IEnumerator Tick_WhileHeldAndMoving_OffsetsBallDownward()
    {
        var ballGo = new GameObject("Ball");
        ballGo.AddComponent<Rigidbody>();
        var ball = ballGo.AddComponent<BallController>();
        var config = ScriptableObject.CreateInstance<BallConfig>();
        ball.SetConfigForTest(config);
        yield return null;

        var holderGo = new GameObject("Holder");
        ball.Catch(holderGo.transform);

        var dribble = new DribbleSystem(ball, config);
        dribble.Tick(isMoving: true, dt: 0.2273f);
        yield return null;

        Assert.Less(ball.transform.position.y, holderGo.transform.position.y + config.handHeightOffset - 0.1f);

        Object.Destroy(ballGo);
        Object.Destroy(holderGo);
    }

    // Once dribbling, stopping does not pick the ball up: it keeps bouncing until a shot
    // gathers it (PickUp) or it changes hands.
    [UnityTest]
    public IEnumerator Stopping_KeepsTheDribble_UntilPickUpOrNewHolder()
    {
        var ballGo = new GameObject("Ball");
        ballGo.AddComponent<Rigidbody>();
        var ball = ballGo.AddComponent<BallController>();
        var config = ScriptableObject.CreateInstance<BallConfig>();
        ball.SetConfigForTest(config);
        yield return null;
        var holderGo = new GameObject("Holder");
        ball.Catch(holderGo.transform);
        var dribble = new DribbleSystem(ball, config);

        dribble.Tick(isMoving: false, dt: 0.1f);
        Assert.IsFalse(dribble.IsDribbling, "holding before the first dribble");
        dribble.Tick(isMoving: true, dt: 0.1f);
        Assert.IsTrue(dribble.IsDribbling);
        float before = dribble.Bounces;
        dribble.Tick(isMoving: false, dt: 0.2f);
        Assert.IsTrue(dribble.IsDribbling, "still dribbling standing still");
        Assert.Greater(dribble.Bounces, before, "the ball keeps bouncing");

        dribble.PickUp();
        Assert.IsFalse(dribble.IsDribbling, "gathered for a shot");
        dribble.Tick(isMoving: true, dt: 0.1f);
        var other = new GameObject("Other");
        ball.Catch(other.transform);
        dribble.Tick(isMoving: false, dt: 0.1f);
        Assert.IsFalse(dribble.IsDribbling, "a new holder starts holding the ball");

        Object.Destroy(ballGo);
        Object.Destroy(holderGo);
        Object.Destroy(other);
    }
}
