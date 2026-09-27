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
        // Different arcs have different rim curves: compare make chances, not radii.
        float layup = ShotAccuracyModel.EstimatedMakeChance(Error(ShotType.Layup, distance: 1.5f), config, ShotType.Layup);
        float jumper = ShotAccuracyModel.EstimatedMakeChance(Error(ShotType.JumpShot, distance: 1.5f), config, ShotType.JumpShot);
        Assert.Greater(layup, jumper);
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

    // Balancing targets for an average shooter (rating 0.75), measured against the
    // physical rim curve (ShotCalibrationTests -> ShotConfig.calibratedMakeRadius).
    private float Make(ShotType type, float distance, float contest = 0f, float speed = 0f, float rating = 0.75f) =>
        ShotAccuracyModel.EstimatedMakeChance(Error(type, distance, 0f, contest, speed, rating), config, type);

    [Test]
    public void Calibration_OpenShotsHitRealisticPercentages()
    {
        Assert.That(Make(ShotType.JumpShot, 6.75f), Is.InRange(0.35f, 0.45f), "open three");
        Assert.That(Make(ShotType.JumpShot, 4.5f), Is.InRange(0.45f, 0.58f), "open mid-range");
        Assert.That(Make(ShotType.FreeThrow, 4.2f), Is.InRange(0.68f, 0.82f), "free throw, perfect timing");
        Assert.That(Make(ShotType.Layup, 1.5f), Is.InRange(0.78f, 0.92f), "open layup");
    }

    [Test]
    public void Calibration_ContestAndMovementHurtWithoutZeroingTheShot()
    {
        float contested = Make(ShotType.JumpShot, 6.75f, contest: 1f);
        Assert.That(contested, Is.InRange(0.12f, 0.25f), "fully contested three");
        Assert.That(Make(ShotType.JumpShot, 6.75f, contest: 0.5f), Is.InRange(0.22f, 0.34f), "half contested three");
        Assert.That(Make(ShotType.JumpShot, 6.75f, speed: 1f), Is.InRange(0.2f, 0.35f), "three on the move");
        Assert.That(Make(ShotType.Layup, 1.5f, contest: 0.5f), Is.InRange(0.45f, 0.65f), "half contested layup");
        Assert.That(Make(ShotType.Layup, 1.5f, contest: 1f), Is.InRange(0.3f, 0.48f), "fully contested layup");
    }

    [Test]
    public void Calibration_ShooterRatingSpreadsTheOpenThree()
    {
        Assert.Less(Make(ShotType.JumpShot, 6.75f, rating: 0f), 0.2f, "non-shooter");
        Assert.That(Make(ShotType.JumpShot, 6.75f, rating: 1f), Is.InRange(0.55f, 0.8f), "best shooter");
    }

    [Test]
    public void EstimatedMakeChance_IsCertainInsideTheMakeRadiusAndFallsWithTheSquare()
    {
        float r = config.calibratedMakeRadius;
        Assert.AreEqual(1f, ShotAccuracyModel.EstimatedMakeChance(r * 0.5f, config));
        Assert.AreEqual(0.25f, ShotAccuracyModel.EstimatedMakeChance(r * 2f, config), 1e-5f);
    }

    [Test]
    public void RunningAtTheRim_FromCloseRange_IsALayup()
    {
        const float rim = 3.05f;
        Assert.AreEqual(ShotType.Layup, ShotAccuracyModel.Classify(3.8f, false, 3.0f, rim, config, approachSpeed: 4f), "driving in from 3.8 m");
        Assert.AreEqual(ShotType.JumpShot, ShotAccuracyModel.Classify(3.8f, false, 3.0f, rim, config, approachSpeed: 0.5f), "standing: jumper");
        Assert.AreEqual(ShotType.JumpShot, ShotAccuracyModel.Classify(3.8f, false, 3.0f, rim, config, approachSpeed: -4f), "running away: fadeaway jumper");
        Assert.AreEqual(ShotType.JumpShot, ShotAccuracyModel.Classify(6f, false, 3.0f, rim, config, approachSpeed: 4f), "too far for a layup");
        Assert.AreEqual(ShotType.Dunk, ShotAccuracyModel.Classify(1.5f, true, 3.25f, rim, config, approachSpeed: 4f), "a dunk stays a dunk");
    }
}
