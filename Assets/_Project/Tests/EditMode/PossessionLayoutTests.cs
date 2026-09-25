using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class PossessionLayoutTests
{
    private static readonly Vector3 HoopFloor = new Vector3(0f, 0f, 13.1f);
    private static readonly Vector3 Check = new Vector3(0f, 0f, 5.5f);

    [Test]
    public void Handler_StandsAtCheckSpot()
    {
        Assert.AreEqual(Check, PossessionLayout.OffenseSpot(HoopFloor, Check, 0, 45f));
    }

    [Test]
    public void SupportPlayers_AlternateSidesAtSameDistanceFromHoop()
    {
        Vector3 left = PossessionLayout.OffenseSpot(HoopFloor, Check, 1, 45f);
        Vector3 right = PossessionLayout.OffenseSpot(HoopFloor, Check, 2, 45f);

        Assert.AreEqual(Vector3.Distance(Check, HoopFloor), Vector3.Distance(left, HoopFloor), 0.001f);
        Assert.AreEqual(-left.x, right.x, 0.001f);
        Assert.AreNotEqual(0f, left.x);
    }

    [Test]
    public void Defender_StandsBetweenAttackerAndHoop()
    {
        Vector3 d = PossessionLayout.DefenseSpot(HoopFloor, Check, 1.5f);
        Assert.AreEqual(Check.z + 1.5f, d.z, 0.001f);
        Assert.AreEqual(0f, d.x, 0.001f);
    }

    [Test]
    public void ClampToCourt_KeepsSpotInsideMargins()
    {
        Vector3 c = PossessionLayout.ClampToCourt(new Vector3(20f, 0f, -3f), 15f, 14f, 0.5f);
        Assert.AreEqual(7f, c.x, 0.001f);
        Assert.AreEqual(0.5f, c.z, 0.001f);
    }
}
