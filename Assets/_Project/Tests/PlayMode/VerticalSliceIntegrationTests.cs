using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Basket.Bootstrap;
using Basket.Core;
using Basket.Gameplay;
using Basket.Presentation;

public class VerticalSliceIntegrationTests
{
    private const string SliceScene = "01_VerticalSlice_HalfCourt";
    private const string FullCourtScene = "02_FullCourt_5v5";
    private string saveDirectory;

    // I5 (etapa 8.5): estas cenas carregam GameBootstrap de verdade, que agora (I1) monta o
    // elenco/save do perfil em Awake(). Sem isto, os testes leriam/escreveriam no save real da
    // máquina do desenvolvedor (Application.persistentDataPath). Seta ANTES de qualquer
    // LoadSceneAsync e limpa depois de cada teste.
    [SetUp]
    public void RedirectSaveDirectoryToTemp()
    {
        saveDirectory = Path.Combine(Path.GetTempPath(), "vslice_save_" + System.Guid.NewGuid());
        GameBootstrap.SaveDirectoryOverride = saveDirectory;
    }

    [TearDown]
    public void RestoreSaveDirectory()
    {
        GameBootstrap.SaveDirectoryOverride = null;
        if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, recursive: true);
    }

    // The slice scene builds a full arena at the same coordinates the other tests use;
    // unload it so it cannot leak players/balls/hoops into later tests.
    [UnityTearDown]
    public IEnumerator UnloadSliceScene()
    {
        foreach (string name in new[] { SliceScene, FullCourtScene })
        {
            Scene slice = SceneManager.GetSceneByName(name);
            if (!slice.isLoaded) continue;
            SceneManager.SetActiveScene(SceneManager.CreateScene("After_" + name));
            yield return SceneManager.UnloadSceneAsync(slice);
        }
    }

    [UnityTest]
    public IEnumerator Scene3v3_StartsWithCheckBallAndRunsTenSeconds()
    {
        yield return SceneManager.LoadSceneAsync(SliceScene, LoadSceneMode.Single);
        yield return null;

        var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        Assert.IsNotNull(bootstrap, "GameBootstrap should exist in the scene.");
        MatchSimulation sim = bootstrap.Simulation;
        Assert.IsNotNull(sim);
        Assert.AreEqual(MatchPhase.Live, sim.Match.State.Phase);
        Assert.AreEqual(6, sim.Players.Count, "the scene plays 3v3");
        Assert.AreEqual(BallState.Held, sim.Ball.CurrentState, "match starts with a check ball");
        Assert.AreEqual(6, Object.FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None).Length, "every player has a character model");

        float elapsed = 0f;
        while (elapsed < 10f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    [UnityTest]
    public IEnumerator Scene5v5_FullCourt_StartsWithJumpBallAndRunsTenSeconds()
    {
        yield return SceneManager.LoadSceneAsync(FullCourtScene, LoadSceneMode.Single);
        yield return null;

        var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        Assert.IsNotNull(bootstrap, "GameBootstrap should exist in the scene.");
        MatchSimulation sim = bootstrap.Simulation;
        Assert.IsNotNull(sim);
        Assert.AreEqual(10, sim.Players.Count, "the scene plays 5v5");
        Assert.AreEqual(2, Object.FindObjectsByType<HoopController>(FindObjectsSortMode.None).Length, "full court: two baskets");
        Assert.AreEqual(BallState.Free, sim.Ball.CurrentState, "the game starts with a jump ball");

        float elapsed = 0f;
        while (elapsed < 10f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        Assert.AreNotEqual(MatchPhase.Ended, sim.Match.State.Phase, "the game is still being played");
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
