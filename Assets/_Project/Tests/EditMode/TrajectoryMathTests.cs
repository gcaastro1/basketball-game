using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class TrajectoryMathTests
{
    [Test]
    public void ComputeArcVelocity_LevelTargets_LandsAtTargetXZ()
    {
        Vector3 origin = Vector3.zero;
        Vector3 target = new Vector3(5f, 0f, 0f);
        float gravity = -9.81f;

        Vector3 velocity = TrajectoryMath.ComputeArcVelocity(origin, target, apexHeight: 2f, gravity: gravity);

        // Simulate the resulting parabola and confirm it lands near the target.
        Vector3 pos = origin;
        Vector3 vel = velocity;
        float dt = 0.001f;
        float landingX = 0f;
        for (float t = 0f; t < 5f; t += dt)
        {
            vel += Vector3.up * gravity * dt;
            pos += vel * dt;
            if (pos.y <= 0f && t > 0.01f)
            {
                landingX = pos.x;
                break;
            }
        }

        Assert.AreEqual(target.x, landingX, 0.1f);
    }

    [Test]
    public void ComputeArcVelocity_HasPositiveUpwardComponent()
    {
        Vector3 velocity = TrajectoryMath.ComputeArcVelocity(Vector3.zero, new Vector3(3f, 0f, 0f), apexHeight: 1.5f, gravity: -9.81f);
        Assert.Greater(velocity.y, 0f);
    }
}
