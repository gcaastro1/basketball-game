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

    // NBA line (stadium court): arc 7.24 m, straight corners 6.71 m to the side of the rim.
    [Test]
    public void CornerThree_CountsFromTheStraightLine_NotTheArc()
    {
        Vector3 rim = new Vector3(0f, 3.05f, 27.05f);
        // Corner, 6.8 m to the side and level with the rim: only 6.8 m away (< 7.24) but a three.
        Assert.IsTrue(ScoringMath.IsBeyondArc(new Vector3(6.8f, 0f, 27.05f), rim, 7.24f, 6.71f));
        Assert.AreEqual(3, ScoringMath.PointsForRelease(new Vector3(-6.8f, 0f, 28.2f), rim, 7.24f, 2, 3, 6.71f));
        // Just inside the corner line.
        Assert.IsFalse(ScoringMath.IsBeyondArc(new Vector3(6.6f, 0f, 27.05f), rim, 7.24f, 6.71f));
        // Past where the corner line meets the arc (2.72 m up the court), the arc decides.
        Assert.IsFalse(ScoringMath.IsBeyondArc(new Vector3(6.0f, 0f, 27.05f - 3f), rim, 7.24f, 6.71f));
        Assert.IsTrue(ScoringMath.IsBeyondArc(new Vector3(6.8f, 0f, 27.05f - 3f), rim, 7.24f, 6.71f));
        Assert.IsTrue(ScoringMath.IsBeyondArc(new Vector3(0f, 0f, 27.05f - 7.3f), rim, 7.24f, 6.71f));
        Assert.IsFalse(ScoringMath.IsBeyondArc(new Vector3(0f, 0f, 27.05f - 7.1f), rim, 7.24f, 6.71f));
    }

    [Test]
    public void NoCornerDistance_IsTheArcOnly()
    {
        Assert.IsFalse(ScoringMath.IsBeyondArc(new Vector3(6.8f, 0f, 13.1f), Rim, 7.24f));
        Assert.IsTrue(ScoringMath.IsBeyondArc(new Vector3(6.8f, 0f, 13.1f), Rim, 6.75f));
    }
}
