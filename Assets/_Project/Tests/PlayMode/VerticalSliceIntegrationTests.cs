using System.Collections;
using System.Collections.Generic;
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

        var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
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

    // Shoots once whenever it holds the ball and has held it for a moment.
    private sealed class ShootWhenHolding : IAgentController
    {
        private float heldSince = -1f;

        public PlayerCommand Decide(MatchSnapshot s, int self)
        {
            if (s.BallHolderIndex != self)
            {
                heldSince = -1f;
                return PlayerCommand.None;
            }
            if (heldSince < 0f) heldSince = s.Time;
            return new PlayerCommand(Vector2.zero, shoot: s.Time - heldSince > 0.3f);
        }
    }

    [UnityTest]
    public IEnumerator FullLoop_ShotFromCheckSpot_ScoresThreeForShooterTeam_ThenPossessionRestarts()
    {
        var court = ScriptableObject.CreateInstance<CourtConfig>();
        var rules = ScriptableObject.CreateInstance<MatchRules>();
        var ballConfig = ScriptableObject.CreateInstance<BallConfig>();
        var shotConfig = ScriptableObject.CreateInstance<ShotConfig>();
        shotConfig.defaultShooterRating = 1f; // no random miss: this test checks the loop, not accuracy
        rules.restartDelaySeconds = 0.5f;

        Arena arena = PlaceholderArenaBuilder.Build(court, ballConfig, rules.threePointRadius);
        PlayerEntity shooter = PlaceholderPlayerFactory.Create("Shooter", TeamId.Away,
            ScriptableObject.CreateInstance<PlayerMovementConfig>(), Color.red, Color.yellow);
        var sim = new MatchSimulation(new List<PlayerEntity> { shooter }, new List<IAgentController> { new ShootWhenHolding() },
            arena.Ball, arena.Hoop, court, rules, ballConfig, shotConfig, new System.Random(1));
        sim.Begin();

        Assert.AreEqual(BallState.Held, arena.Ball.CurrentState);
        Assert.AreEqual(shooter.transform, arena.Ball.CurrentHolder, "solo team keeps the check ball");

        float elapsed = 0f;
        while (sim.Match.State.ScoreAway == 0 && elapsed < 6f)
        {
            yield return null;
            sim.Tick(Time.deltaTime);
            elapsed += Time.deltaTime;
        }

        Assert.AreEqual(3, sim.Match.State.ScoreAway, "check spot is beyond the arc");
        Assert.AreEqual(0, sim.Match.State.ScoreHome);

        elapsed = 0f;
        while (sim.Match.State.Phase != MatchPhase.Live && elapsed < 2f)
        {
            yield return null;
            sim.Tick(Time.deltaTime);
            elapsed += Time.deltaTime;
        }

        Assert.AreEqual(MatchPhase.Live, sim.Match.State.Phase);
        Assert.AreEqual(BallState.Held, arena.Ball.CurrentState, "possession restarted with a check ball");

        sim.Dispose();
        Object.Destroy(arena.Root);
        Object.Destroy(shooter.gameObject);
    }
}
