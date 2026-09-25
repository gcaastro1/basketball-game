using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class BlockMathTests
{
    [Test]
    public void BallBetweenHeadAndFingertips_NearDefender_IsWithinArms()
    {
        Assert.IsTrue(BlockMath.IsWithinArms(new Vector3(0.3f, 2.2f, 0f), Vector3.zero, 1.8f, 2.45f, 0.45f));
    }

    [Test]
    public void BallAboveReach_IsNotWithinArms()
    {
        Assert.IsFalse(BlockMath.IsWithinArms(new Vector3(0f, 3.2f, 0f), Vector3.zero, 1.8f, 2.45f, 0.45f));
    }

    [Test]
    public void JumpingRaisesTheArms()
    {
        var ball = new Vector3(0f, 3.2f, 0f);
        Assert.IsTrue(BlockMath.IsWithinArms(ball, new Vector3(0f, 0.8f, 0f), 1.8f, 2.45f, 0.45f));
    }

    [Test]
    public void FarBall_IsNotWithinArms()
    {
        Assert.IsFalse(BlockMath.IsWithinArms(new Vector3(1f, 2.2f, 0f), Vector3.zero, 1.8f, 2.45f, 0.45f));
    }

    [Test]
    public void Deflect_PushesAwayAndDown()
    {
        Vector3 v = BlockMath.DeflectVelocity(new Vector3(0f, 2.5f, 1f), Vector3.zero, 4f);
        Assert.Greater(v.z, 0f);
        Assert.Less(v.y, 0f);
    }

    [Test]
    public void IsFacing_RequiresTargetInFront()
    {
        Assert.IsTrue(BlockMath.IsFacing(Vector3.forward, Vector3.zero, new Vector3(0f, 0f, 1f), 0.3f));
        Assert.IsFalse(BlockMath.IsFacing(Vector3.forward, Vector3.zero, new Vector3(0f, 0f, -1f), 0.3f));
    }
}
