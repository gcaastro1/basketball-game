using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class HoopMathTests
{
    private static readonly Vector3 Rim = new Vector3(0f, 3.05f, 13.1f);
    private const float Radius = 0.2286f;

    [Test]
    public void DescendingThroughCenter_Scores()
    {
        Assert.IsTrue(HoopMath.IsScoringCrossing(Rim + Vector3.up * 0.1f, Rim + Vector3.down * 0.1f, Rim, Radius));
    }

    [Test]
    public void AscendingThroughCenter_DoesNotScore()
    {
        Assert.IsFalse(HoopMath.IsScoringCrossing(Rim + Vector3.down * 0.1f, Rim + Vector3.up * 0.1f, Rim, Radius));
    }

    [Test]
    public void DescendingOutsideRing_DoesNotScore()
    {
        Vector3 outside = Rim + new Vector3(0.3f, 0f, 0f);
        Assert.IsFalse(HoopMath.IsScoringCrossing(outside + Vector3.up * 0.1f, outside + Vector3.down * 0.1f, Rim, Radius));
    }

    [Test]
    public void DiagonalStep_UsesPositionWhereItCrossesThePlane()
    {
        // Starts outside the ring but crosses the plane inside it.
        Vector3 prev = Rim + new Vector3(-0.4f, 0.2f, 0f);
        Vector3 curr = Rim + new Vector3(0.2f, -0.2f, 0f);
        Assert.IsTrue(HoopMath.IsScoringCrossing(prev, curr, Rim, Radius));
    }

    [Test]
    public void StayingAbove_DoesNotScore()
    {
        Assert.IsFalse(HoopMath.IsScoringCrossing(Rim + Vector3.up, Rim + Vector3.up * 0.5f, Rim, Radius));
    }
}
