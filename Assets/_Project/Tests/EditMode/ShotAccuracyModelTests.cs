using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class ShotAccuracyModelTests
{
    private ShotConfig config;

    [SetUp]
    public void SetUp() => config = ScriptableObject.CreateInstance<ShotConfig>();

    private float Error(ShotType type, float distance = 5f, float timing = 0f, float contest = 0f, float speed = 0f, float rating = 0.75f) =>
        ShotAccuracyModel.ErrorRadius(new ShotAccuracyInput(type, distance, timing, contest, speed, rating), config);

    [Test]
    public void FartherShots_AreLessAccurate()
    {
        Assert.Greater(Error(ShotType.JumpShot, distance: 7f), Error(ShotType.JumpShot, distance: 3f));
    }

    [Test]
    public void ReleaseInsidePerfectWindow_HasNoTimingPenalty()
    {
        Assert.AreEqual(Error(ShotType.JumpShot, timing: 0f), Error(ShotType.JumpShot, timing: config.perfectReleaseWindow * 0.9f), 1e-6f);
    }

    [Test]
    public void EarlyAndLateReleases_ArePenalizedSymmetrically()
    {
        float early = Error(ShotType.JumpShot, timing: -0.2f);
        float late = Error(ShotType.JumpShot, timing: 0.2f);
        Assert.Greater(early, Error(ShotType.JumpShot));
        Assert.AreEqual(early, late, 1e-6f);
    }

    [Test]
    public void Contest_MovingAndLowRating_AllIncreaseError()
    {
        float open = Error(ShotType.JumpShot);
        Assert.Greater(Error(ShotType.JumpShot, contest: 1f), open);
        Assert.Greater(Error(ShotType.JumpShot, speed: 1f), open);
        Assert.Greater(Error(ShotType.JumpShot, rating: 0f), open);
        Assert.Less(Error(ShotType.JumpShot, rating: 1f), open);
    }

    [Test]
    public void Layup_IgnoresTimingAndIsMoreAccurateThanAJumper()
    {
        Assert.AreEqual(Error(ShotType.Layup, distance: 1.5f), Error(ShotType.Layup, distance: 1.5f, timing: 0.3f), 1e-6f);
        Assert.Less(Error(ShotType.Layup, distance: 1.5f), Error(ShotType.JumpShot, distance: 1.5f));
    }

    [Test]
    public void Dunk_HasNoAimError()
    {
        Assert.AreEqual(0f, Error(ShotType.Dunk, contest: 1f));
    }

    [Test]
    public void Classify_BySprintDistanceAndReach()
    {
        float rim = 3.05f;
        Assert.AreEqual(ShotType.Dunk, ShotAccuracyModel.Classify(1.5f, sprinting: true, reachAtApex: 3.25f, rim, config));
        Assert.AreEqual(ShotType.Layup, ShotAccuracyModel.Classify(1.5f, sprinting: false, reachAtApex: 3.25f, rim, config), "no sprint -> layup");
        Assert.AreEqual(ShotType.Layup, ShotAccuracyModel.Classify(1.5f, sprinting: true, reachAtApex: 3.0f, rim, config), "cannot reach the rim -> layup");
        Assert.AreEqual(ShotType.JumpShot, ShotAccuracyModel.Classify(5f, sprinting: true, reachAtApex: 3.25f, rim, config));
    }
}
