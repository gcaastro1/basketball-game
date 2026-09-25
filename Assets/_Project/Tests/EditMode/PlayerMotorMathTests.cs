using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class PlayerMotorMathTests
{
    [Test]
    public void ComputeVelocity_FromStandstill_AcceleratesTowardDesiredDirection()
    {
        Vector3 result = PlayerMotorMath.ComputeVelocity(
            currentVelocity: Vector3.zero,
            desiredDirection: Vector3.forward,
            maxSpeed: 6f,
            acceleration: 30f,
            deceleration: 40f,
            dt: 0.1f);

        Assert.Greater(result.z, 0f);
        Assert.LessOrEqual(result.magnitude, 6f);
    }

    [Test]
    public void ComputeVelocity_NoInput_DeceleratesTowardZero()
    {
        Vector3 result = PlayerMotorMath.ComputeVelocity(
            currentVelocity: new Vector3(0f, 0f, 6f),
            desiredDirection: Vector3.zero,
            maxSpeed: 6f,
            acceleration: 30f,
            deceleration: 40f,
            dt: 0.1f);

        Assert.Less(result.z, 6f);
        Assert.GreaterOrEqual(result.z, 0f);
    }

    [Test]
    public void ComputeVelocity_NeverExceedsMaxSpeed()
    {
        Vector3 result = PlayerMotorMath.ComputeVelocity(
            currentVelocity: new Vector3(0f, 0f, 5.9f),
            desiredDirection: Vector3.forward,
            maxSpeed: 6f,
            acceleration: 30f,
            deceleration: 40f,
            dt: 1f);

        Assert.LessOrEqual(result.magnitude, 6f + 0.0001f);
    }
}
