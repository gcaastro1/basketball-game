using UnityEngine;

namespace Basket.Gameplay
{
    public static class TrajectoryMath
    {
        public static Vector3 ComputeArcVelocity(Vector3 origin, Vector3 target, float apexHeight, float gravity)
        {
            float g = Mathf.Abs(gravity);
            Vector3 displacement = target - origin;
            Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
            float peakHeight = Mathf.Max(apexHeight, 0.01f);

            // Apex sits peakHeight above whichever of origin/target is higher, so the
            // trajectory is guaranteed to still be rising until it clears both endpoints.
            // (An earlier version measured peakHeight from origin only, which silently
            // produced a below-target apex — and a physically broken landing — whenever
            // the target sat more than apexHeight above the origin, e.g. a shot arc to a
            // rim well above the shooter's release height.)
            float apexAboveOrigin = peakHeight + Mathf.Max(0f, displacement.y);
            float apexAboveTarget = apexAboveOrigin - displacement.y;

            float timeUp = Mathf.Sqrt(2f * apexAboveOrigin / g);
            float timeDown = Mathf.Sqrt(2f * apexAboveTarget / g);
            float totalTime = timeUp + timeDown;

            Vector3 velocityXZ = displacementXZ / totalTime;
            float velocityY = g * timeUp;

            return velocityXZ + Vector3.up * velocityY;
        }

        // The analytic arc above assumes continuous, drag-free motion. The physics engine
        // integrates in fixed steps and applies linear damping, which on a ~2 s shot lands
        // the ball tens of centimetres short -- enough to hit the front rim every time.
        // This corrects the launch velocity against the same discrete integrator so the
        // ball's descending path crosses the target height at the target.
        public static Vector3 ComputeCompensatedArcVelocity(Vector3 origin, Vector3 target, float apexHeight, float gravity,
            float linearDamping, float fixedDeltaTime)
        {
            Vector3 velocity = ComputeArcVelocity(origin, target, apexHeight, gravity);
            if (fixedDeltaTime <= 0f) return velocity;

            Vector3 desiredXZ = new Vector3(target.x - origin.x, 0f, target.z - origin.z);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                if (!TrySimulateDescendingCrossing(origin, velocity, target.y, gravity, linearDamping, fixedDeltaTime, out Vector3 crossing, out _))
                {
                    // Damping cost the apex too much height: launch a bit higher and retry.
                    velocity.y *= 1.05f;
                    continue;
                }

                // Damping scales every velocity component equally and gravity only acts on
                // y, so horizontal travel at the (y-determined) crossing time is linear in
                // the horizontal launch speed: one proportional correction is exact.
                Vector3 achievedXZ = new Vector3(crossing.x - origin.x, 0f, crossing.z - origin.z);
                if (achievedXZ.sqrMagnitude > 1e-8f && desiredXZ.sqrMagnitude > 1e-8f)
                {
                    float scale = desiredXZ.magnitude / achievedXZ.magnitude;
                    velocity.x *= scale;
                    velocity.z *= scale;
                }
                return velocity;
            }
            return velocity;
        }

        // Mirrors the engine's per-step order: gravity, then linear damping, then position.
        public static bool TrySimulateDescendingCrossing(Vector3 origin, Vector3 velocity, float targetHeight, float gravity,
            float linearDamping, float dt, out Vector3 crossing, out float time)
        {
            Vector3 pos = origin;
            Vector3 vel = velocity;
            float dampFactor = Mathf.Max(0f, 1f - linearDamping * dt);
            int maxSteps = Mathf.CeilToInt(10f / dt);
            for (int step = 1; step <= maxSteps; step++)
            {
                vel.y += gravity * dt;
                vel *= dampFactor;
                Vector3 next = pos + vel * dt;
                if (vel.y < 0f && pos.y >= targetHeight && next.y < targetHeight)
                {
                    float f = (pos.y - targetHeight) / (pos.y - next.y);
                    crossing = Vector3.Lerp(pos, next, f);
                    time = (step - 1 + f) * dt;
                    return true;
                }
                pos = next;
            }
            crossing = pos;
            time = maxSteps * dt;
            return false;
        }
    }
}
