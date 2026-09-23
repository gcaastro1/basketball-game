using System;
using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class ShotMathTests
{
    [Test]
    public void ComputeMissOffset_PerfectRating_ReturnsZero()
    {
        var rng = new System.Random(42);
        Vector3 offset = ShotMath.ComputeMissOffset(baseRadius: 0.5f, rating: 1f, rng: rng);
        Assert.AreEqual(Vector3.zero, offset);
    }

    [Test]
    public void ComputeMissOffset_ZeroRating_StaysWithinBaseRadius()
    {
        var rng = new System.Random(42);
        Vector3 offset = ShotMath.ComputeMissOffset(baseRadius: 0.5f, rating: 0f, rng: rng);
        Assert.LessOrEqual(new Vector3(offset.x, 0f, offset.z).magnitude, 0.5f + 0.0001f);
    }
}
