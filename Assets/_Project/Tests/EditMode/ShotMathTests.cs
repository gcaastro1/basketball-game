using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class ShotMathTests
{
    [Test]
    public void SampleDiscOffset_ZeroRadius_ReturnsZero()
    {
        Assert.AreEqual(Vector3.zero, ShotMath.SampleDiscOffset(0f, new System.Random(42)));
    }

    [Test]
    public void SampleDiscOffset_StaysInsideRadiusAndHorizontal()
    {
        var rng = new System.Random(7);
        for (int i = 0; i < 500; i++)
        {
            Vector3 o = ShotMath.SampleDiscOffset(0.2f, rng);
            Assert.AreEqual(0f, o.y);
            Assert.LessOrEqual(o.magnitude, 0.2f + 1e-5f);
        }
    }

    [Test]
    public void SampleDiscOffset_IsUniformOverArea_AboutQuarterInsideHalfRadius()
    {
        var rng = new System.Random(3);
        int inside = 0;
        const int n = 4000;
        for (int i = 0; i < n; i++)
        {
            if (ShotMath.SampleDiscOffset(1f, rng).magnitude < 0.5f) inside++;
        }
        Assert.AreEqual(0.25, inside / (double)n, 0.03);
    }
}
