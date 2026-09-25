using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class VerticalSliceIntegrationTests
{
    [UnityTest]
    public IEnumerator VerticalSliceScene_RunsForFiveSecondsWithoutExceptions()
    {
        yield return SceneManager.LoadSceneAsync("01_VerticalSlice_HalfCourt", LoadSceneMode.Single);
        yield return null;

        var matchManager = Object.FindFirstObjectByType<MatchManager>();
        Assert.IsNotNull(matchManager, "MatchManager should exist in the assembled scene.");
        Assert.AreEqual(MatchPhase.Live, matchManager.State.Phase);

        float elapsed = 0f;
        while (elapsed < 5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        Assert.Pass("Scene ran for 5 seconds without an unhandled exception.");
    }
}
