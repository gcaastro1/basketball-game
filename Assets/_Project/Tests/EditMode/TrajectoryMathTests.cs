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

    [Test]
    public void ComputeArcVelocity_ElevatedTarget_ReachesTargetHeightAtArrival()
    {
        Vector3 origin = Vector3.zero;
        Vector3 target = new Vector3(5f, 4f, 0f);
        float gravity = -9.81f;
        float apexHeight = 1f;

        Vector3 velocity = TrajectoryMath.ComputeArcVelocity(origin, target, apexHeight, gravity);

        // Recover the flight time implied by the returned horizontal velocity — exact by
        // construction (horizontalSpeed == horizontalDisplacement / totalTime internally),
        // so this doesn't depend on any implementation detail beyond the public contract.
        float horizontalDisplacement = new Vector3(target.x - origin.x, 0f, target.z - origin.z).magnitude;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        float totalTime = horizontalDisplacement / horizontalSpeed;

        float heightAtArrival = origin.y + velocity.y * totalTime + 0.5f * gravity * totalTime * totalTime;

        Assert.AreEqual(target.y, heightAtArrival, 0.05f);
    }

    [Test]
    public void CompensatedArc_WithDampingAndFixedStep_CrossesRimHeightAtRim()
    {
        // A 3PT-range shot: the uncompensated arc falls short by tens of centimetres
        // once the engine's fixed step and linear damping are applied.
        Vector3 origin = new Vector3(0.2f, 1.0f, 5.9f);
        Vector3 rim = new Vector3(0f, 3.05f, 13.1f);
        const float gravity = -9.81f, damping = 0.05f, dt = 0.02f;

        Vector3 naive = TrajectoryMath.ComputeArcVelocity(origin, rim, 3.5f, gravity);
        Assert.IsTrue(TrajectoryMath.TrySimulateDescendingCrossing(origin, naive, rim.y, gravity, damping, dt, out Vector3 naiveHit, out _));
        Assert.Greater(FlatDistance(naiveHit, rim), 0.1f, "precondition: the naive arc really does miss");

        Vector3 compensated = TrajectoryMath.ComputeCompensatedArcVelocity(origin, rim, 3.5f, gravity, damping, dt);
        Assert.IsTrue(TrajectoryMath.TrySimulateDescendingCrossing(origin, compensated, rim.y, gravity, damping, dt, out Vector3 hit, out _));
        Assert.Less(FlatDistance(hit, rim), 0.02f);
    }

    [Test]
    public void CompensatedArc_ZeroFixedStep_EqualsAnalyticArc()
    {
        Vector3 a = TrajectoryMath.ComputeArcVelocity(Vector3.zero, new Vector3(4f, 1f, 0f), 1f, -9.81f);
        Vector3 b = TrajectoryMath.ComputeCompensatedArcVelocity(Vector3.zero, new Vector3(4f, 1f, 0f), 1f, -9.81f, 0.05f, 0f);
        Assert.AreEqual(a, b);
    }

    [Test]
    public void EstimateFlightTime_MatchesTheArcsHorizontalSpeed()
    {
        Vector3 origin = new Vector3(0f, 1f, 0f), target = new Vector3(6f, 1.2f, 2f);
        Vector3 v = TrajectoryMath.ComputeArcVelocity(origin, target, 0.6f, -9.81f);
        float horizontal = new Vector2(target.x - origin.x, target.z - origin.z).magnitude;
        float expected = horizontal / new Vector2(v.x, v.z).magnitude;
        Assert.AreEqual(expected, TrajectoryMath.EstimateFlightTime(origin, target, 0.6f, -9.81f), 1e-3f);
    }

    private static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
}
