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

    // Real shots come into the rim at ~45 degrees: the apex grows with the distance.
    [Test]
    public void ShotArc_EntryAngle_SetsTheApex_WithinLimits()
    {
        var c = UnityEngine.ScriptableObject.CreateInstance<ShotConfig>();
        c.entryAngleDegrees = 45f;
        c.minArcHeight = 0.9f;
        c.maxArcHeight = 2.6f;
        Assert.AreEqual(1.8f, ShotArc.ApexAboveRim(7.2f, c), 1e-3f, "three: D * tan45 / 4");
        Assert.AreEqual(1.05f, ShotArc.ApexAboveRim(4.2f, c), 1e-3f, "free throw");
        Assert.AreEqual(0.9f, ShotArc.ApexAboveRim(1f, c), 1e-4f, "short shots keep a minimum arc");
        Assert.AreEqual(2.6f, ShotArc.ApexAboveRim(20f, c), 1e-4f, "long heaves are capped");
        c.entryAngleDegrees = 55f;
        Assert.Greater(ShotArc.ApexAboveRim(7.2f, c), 1.8f, "steeper entry, higher arc");
    }
}
