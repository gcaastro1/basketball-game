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
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());
        yield return null;

        var holderGo = new GameObject("Holder");
        ball.Catch(holderGo.transform);

        var dribbleGo = new GameObject("Dribble");
        var dribble = dribbleGo.AddComponent<DribbleSystem>();
        dribble.Configure(ball);

        dribble.Tick(isMoving: true, dt: 0.2273f);
        yield return null;

        Assert.Less(ball.transform.position.y, holderGo.transform.position.y + 1.1f - 0.1f);

        Object.Destroy(ballGo);
        Object.Destroy(holderGo);
        Object.Destroy(dribbleGo);
    }
}
