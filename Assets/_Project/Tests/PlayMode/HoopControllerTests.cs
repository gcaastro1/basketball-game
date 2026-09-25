using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class HoopControllerTests
{
    private CourtConfig court;
    private Arena arena;
    private GameObject holderGo;

    [SetUp]
    public void SetUp()
    {
        court = ScriptableObject.CreateInstance<CourtConfig>();
        arena = PlaceholderArenaBuilder.Build(court, ScriptableObject.CreateInstance<BallConfig>(), 6.75f);
        holderGo = new GameObject("Holder");
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(arena.Root);
        Object.Destroy(holderGo);
    }

    // Holds the ball at `start`, then releases it with `velocity`.
    private void Launch(Vector3 start, Vector3 velocity)
    {
        arena.Ball.Catch(holderGo.transform);
        arena.Ball.Release(BallState.Shooting, velocity);
        arena.Ball.transform.position = start;
        arena.Ball.GetComponent<Rigidbody>().position = start;
    }

    [UnityTest]
    public IEnumerator BallDroppedThroughRim_Scores()
    {
        yield return new WaitForFixedUpdate();
        bool scored = false;
        arena.Ball.OnScored += _ => scored = true;

        Launch(court.rimCenter + Vector3.up * 1f, Vector3.zero);
        for (int i = 0; i < 60 && !scored; i++) yield return new WaitForFixedUpdate();

        Assert.IsTrue(scored);
    }

    [UnityTest]
    public IEnumerator BallThrownUpThroughRim_DoesNotScoreOnTheWayUp()
    {
        yield return new WaitForFixedUpdate();
        bool scored = false;
        arena.Ball.OnScored += _ => scored = true;

        // Straight up through the ring from below; stop watching before it falls back.
        Launch(court.rimCenter + Vector3.down * 0.5f, Vector3.up * 5f);
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();

        Assert.IsFalse(scored);
    }

    [UnityTest]
    public IEnumerator BallDroppedOnFrontRim_TouchesRimAndDoesNotScore()
    {
        yield return new WaitForFixedUpdate();
        bool scored = false;
        arena.Ball.OnScored += _ => scored = true;
        Vector3 frontRim = court.rimCenter + Vector3.back * court.rimRadius;
        Launch(frontRim + Vector3.up * 1f, Vector3.zero);

        // 0.6 s: long enough to fall 1 m onto the rim, too short to reach the floor
        // (which would also end the in-flight state).
        for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

        Assert.AreEqual(BallState.Free, arena.Ball.CurrentState, "a physical rim contact ends the shot's in-flight state");
        Assert.IsFalse(scored);
    }
}
