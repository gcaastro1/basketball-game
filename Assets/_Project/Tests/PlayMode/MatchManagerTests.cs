using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class MatchManagerTests
{
    [UnityTest]
    public IEnumerator BallScored_IncreasesScoreOnMatchManager()
    {
        var ballGo = new GameObject("Ball");
        ballGo.AddComponent<Rigidbody>();
        var ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());
        yield return null;

        var matchGo = new GameObject("Match");
        var match = matchGo.AddComponent<MatchManager>();
        match.Configure(ball);
        match.State.StartLivePlayForTest();

        var holderGo = new GameObject("Holder");
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, Vector3.up);
        ball.NotifyScored();

        Assert.AreEqual(2, match.State.ScoreHome + match.State.ScoreAway);

        Object.Destroy(ballGo);
        Object.Destroy(matchGo);
        Object.Destroy(holderGo);
    }
}
