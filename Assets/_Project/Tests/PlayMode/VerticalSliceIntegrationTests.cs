using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Basket.Bootstrap;
using Basket.Core;
using Basket.Gameplay;

public class VerticalSliceIntegrationTests
{
    private const string SliceScene = "01_VerticalSlice_HalfCourt";

    // The slice scene builds a full arena at the same coordinates the other tests use;
    // unload it so it cannot leak players/balls/hoops into later tests.
    [UnityTearDown]
    public IEnumerator UnloadSliceScene()
    {
        Scene slice = SceneManager.GetSceneByName(SliceScene);
        if (!slice.isLoaded) yield break;
        SceneManager.SetActiveScene(SceneManager.CreateScene("AfterSliceTest"));
        yield return SceneManager.UnloadSceneAsync(slice);
    }

    [UnityTest]
    public IEnumerator VerticalSliceScene_StartsWithCheckBallAndRunsFiveSeconds()
    {
        yield return SceneManager.LoadSceneAsync(SliceScene, LoadSceneMode.Single);
        yield return null;

        var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        Assert.IsNotNull(bootstrap, "GameBootstrap should exist in the scene.");
        MatchSimulation sim = bootstrap.Simulation;
        Assert.IsNotNull(sim);
        Assert.AreEqual(MatchPhase.Live, sim.Match.State.Phase);
        Assert.AreEqual(2, sim.Players.Count, "default setup is 1v1");
        Assert.AreEqual(BallState.Held, sim.Ball.CurrentState, "match starts with a check ball");

        float elapsed = 0f;
        while (elapsed < 5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    [UnityTest]
    public IEnumerator FullLoop_ShotScores_ThenPossessionRestartsWithCheckBall()
    {
        using var match = new TestMatch();
        match.Start((TeamId.Away, TestMatch.ShootAtApex()));
        Assert.AreSame(match.Players[0].transform, match.Sim.Ball.CurrentHolder, "solo team keeps the check ball");

        yield return match.RunUntil(() => match.Sim.Match.State.ScoreAway > 0, 6f);
        Assert.AreEqual(3, match.Sim.Match.State.ScoreAway, "check spot is beyond the arc");
        Assert.AreEqual(0, match.Sim.Match.State.ScoreHome);

        yield return match.RunUntil(() => match.Sim.Match.State.Phase == MatchPhase.Live, 2f);
        Assert.AreEqual(MatchPhase.Live, match.Sim.Match.State.Phase);
        Assert.AreEqual(BallState.Held, match.Sim.Ball.CurrentState, "possession restarted with a check ball");
    }
}
