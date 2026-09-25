using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class ScoringMathTests
{
    private static readonly Vector3 Rim = new Vector3(0f, 3.05f, 13.1f);

    [Test]
    public void InsideArc_ReturnsInsidePoints()
    {
        Assert.AreEqual(2, ScoringMath.PointsForRelease(new Vector3(0f, 0f, 10f), Rim, 6.75f, 2, 3));
    }

    [Test]
    public void BeyondArc_ReturnsBeyondPoints_IgnoringHeight()
    {
        Assert.AreEqual(3, ScoringMath.PointsForRelease(new Vector3(0f, 0f, 5.5f), Rim, 6.75f, 2, 3));
        Assert.AreEqual(3, ScoringMath.PointsForRelease(new Vector3(0f, 10f, 5.5f), Rim, 6.75f, 2, 3));
    }

    [Test]
    public void ThreeByThreeValues_AreJustData()
    {
        Assert.AreEqual(1, ScoringMath.PointsForRelease(new Vector3(0f, 0f, 10f), Rim, 6.75f, 1, 2));
        Assert.AreEqual(2, ScoringMath.PointsForRelease(new Vector3(0f, 0f, 5.5f), Rim, 6.75f, 1, 2));
    }
}
