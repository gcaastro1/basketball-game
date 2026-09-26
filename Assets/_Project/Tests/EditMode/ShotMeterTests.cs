using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

// Shot meter (NBA 2K style): a green window around the jump apex whose size comes from the
// shot's attribute, the contest, the distance and moving; a green release has no aim error.
public class ShotMeterTests
{
    private ShotConfig c;

    [SetUp]
    public void SetUp() => c = ScriptableObject.CreateInstance<ShotConfig>();

    private float Green(ShotType type = ShotType.JumpShot, float distance = 4f, float contest = 0f, float speed = 0f, float rating = 0.75f) =>
        ShotAccuracyModel.GreenHalfWidth(type, distance, contest, speed, rating, c);

    [Test]
    public void Green_GrowsWithTheShotsAttribute()
    {
        Assert.Greater(Green(rating: 0.95f), Green(rating: 0.5f));
        Assert.AreEqual(c.greenHalfWidthAtRatingOne, Green(rating: 1f), 1e-6f);
    }

    [Test]
    public void Green_ShrinksWithContestDistanceAndMoving_NeverBelowTheFloor()
    {
        float open = Green();
        Assert.Less(Green(contest: 1f), open * 0.5f, "a contested shot has a small green");
        Assert.Less(Green(distance: 8f), open, "a long shot has a smaller green");
        Assert.Less(Green(speed: 1f), open, "shooting on the move shrinks it");
        float worst = Green(distance: 20f, contest: 1f, speed: 1f);
        Assert.AreEqual(open * c.greenMinScale, worst, 1e-6f);
    }

    [Test]
    public void LayupsAndDunks_HaveNoMeter()
    {
        Assert.AreEqual(0f, Green(ShotType.Layup));
        Assert.AreEqual(0f, Green(ShotType.Dunk));
        Assert.Greater(Green(ShotType.FreeThrow), 0f);
    }

    [Test]
    public void GreenRelease_IsPerfect_OutsideItTheErrorGrowsWithTheMiss()
    {
        float g = Green();
        var inGreen = new ShotAccuracyInput(ShotType.JumpShot, 4f, g * 0.9f, 0f, 0f, 0.75f);
        Assert.AreEqual(0f, ShotAccuracyModel.ErrorRadius(inGreen, g, c), "green = no aim error");
        float justOut = ShotAccuracyModel.ErrorRadius(new ShotAccuracyInput(ShotType.JumpShot, 4f, g + 0.01f, 0f, 0f, 0.75f), g, c);
        float farOut = ShotAccuracyModel.ErrorRadius(new ShotAccuracyInput(ShotType.JumpShot, 4f, -(g + 0.15f), 0f, 0f, 0.75f), g, c);
        Assert.Greater(justOut, 0f);
        Assert.Greater(farOut, justOut, "the farther from the green, the bigger the error");
        Assert.IsTrue(new ShotReport(0, ShotType.JumpShot, 4f, g * 0.5f, 0f, 0f, g).IsGreen);
        Assert.IsFalse(new ShotReport(0, ShotType.JumpShot, 4f, g * 1.5f, 0f, 0f, g).IsGreen);
    }

    [Test]
    public void MeterOff_KeepsTheOldFixedWindow()
    {
        c.useGreenWindow = false;
        Assert.AreEqual(0f, Green());
        var input = new ShotAccuracyInput(ShotType.JumpShot, 5f, 0f, 0f, 0f, 0.75f);
        Assert.AreEqual(ShotAccuracyModel.ErrorRadius(input, c), ShotAccuracyModel.ErrorRadius(input, 0f, c));
    }

    [Test]
    public void Grades_SevenSteps_FromVeryEarlyToVeryLate()
    {
        const float g = 0.015f;
        Assert.AreEqual(ShotTimingGrade.Perfect, ShotAccuracyModel.Grade(0.01f, g, c));
        Assert.AreEqual(ShotTimingGrade.Perfect, ShotAccuracyModel.Grade(-0.015f, g, c));
        Assert.AreEqual(ShotTimingGrade.SlightlyEarly, ShotAccuracyModel.Grade(-(g + 0.02f), g, c));
        Assert.AreEqual(ShotTimingGrade.SlightlyLate, ShotAccuracyModel.Grade(g + 0.02f, g, c));
        Assert.AreEqual(ShotTimingGrade.Early, ShotAccuracyModel.Grade(-(g + 0.06f), g, c));
        Assert.AreEqual(ShotTimingGrade.Late, ShotAccuracyModel.Grade(g + 0.06f, g, c));
        Assert.AreEqual(ShotTimingGrade.VeryEarly, ShotAccuracyModel.Grade(-(g + 0.2f), g, c));
        Assert.AreEqual(ShotTimingGrade.VeryLate, ShotAccuracyModel.Grade(g + 0.2f, g, c));
    }
}
