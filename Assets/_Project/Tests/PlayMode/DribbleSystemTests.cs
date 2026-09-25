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
}
