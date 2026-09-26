using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

// Measures the physical side of shooting on the real rim: where a shot aimed at the rim
// center actually crosses the rim plane (trajectory bias), and the make rate as a function
// of the aim offset. ShotConfig's error radii are tuned against this curve.
public class ShotCalibrationTests
{
    private const float ReleaseHeight = 2.6f;
    private const int Angles = 8;
    private static readonly float[] Offsets = { 0.03f, 0.06f, 0.09f, 0.12f, 0.15f, 0.2f, 0.25f };

    private CourtConfig court;
    private ShotConfig shots;
    private Arena arena;
    private GameObject holder;
    private float previousScale;

    [SetUp]
    public void SetUp()
    {
        court = ScriptableObject.CreateInstance<CourtConfig>();
        shots = ScriptableObject.CreateInstance<ShotConfig>();
        arena = PlaceholderArenaBuilder.Build(court, ScriptableObject.CreateInstance<BallConfig>(), 6.75f);
        holder = new GameObject("Holder");
        previousScale = Time.timeScale;
        Time.timeScale = 4f;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = previousScale;
        Object.Destroy(arena.Root);
        Object.Destroy(holder);
    }

    private sealed class Outcome
    {
        public bool Made;
        public Vector3? Crossing;
    }

    // One jump shot from `distance` straight out from the rim (angle in degrees around it),
    // aimed at rim center + `aimOffset`.
    private IEnumerator Shoot(float distance, float angleDeg, Vector3 aimOffset, float arcHeight, Outcome outcome)
    {
        Vector3 rim = court.rimCenter;
        Vector3 dir = Quaternion.AngleAxis(angleDeg, Vector3.up) * Vector3.back;
        Vector3 start = new Vector3(rim.x, ReleaseHeight, rim.z) + dir * distance;
        BallController ball = arena.Ball;

        bool made = false;
        System.Action<ScoreEvent> onScored = _ => made = true;
        ball.OnScored += onScored;
        ball.ResetToHolder(holder.transform);
        Vector3 velocity = TrajectoryMath.ComputeCompensatedArcVelocity(start, rim + aimOffset, arcHeight,
            Physics.gravity.y, ball.LinearDamping, Time.fixedDeltaTime);
        ball.ReleaseAt(BallState.Shooting, start, velocity, ShotType.JumpShot);

        Vector3 previous = start;
        for (int i = 0; i < 400; i++)
        {
            yield return new WaitForFixedUpdate();
            Vector3 current = ball.PhysicsPosition;
            if (outcome.Crossing == null && previous.y >= rim.y && current.y < rim.y)
            {
                float t = (previous.y - rim.y) / Mathf.Max(1e-5f, previous.y - current.y);
                outcome.Crossing = Vector3.Lerp(previous, current, t) - rim;
            }
            previous = current;
            if (made || current.y < 0.6f) break;
        }
        ball.OnScored -= onScored;
        outcome.Made = made;
    }

    [UnityTest]
    public IEnumerator AimedAtRimCenter_CrossesTheRimPlaneNearTheCenter_AndScores()
    {
        yield return new WaitForFixedUpdate();
        var log = new StringBuilder("Trajectory bias (crossing offset from rim center, aimed at center):\n");
        var misses = new List<string>();
        foreach (float distance in new[] { 4.2f, 5.5f, 6.75f, 7.5f })
        {
            foreach (float angle in new[] { 0f, 40f, 70f })
            {
                var o = new Outcome();
                yield return Shoot(distance, angle, Vector3.zero, ShotArc.ApexAboveRim(distance, shots), o);
                Vector3 c = o.Crossing ?? new Vector3(float.NaN, 0f, float.NaN);
                log.AppendLine($"  d={distance:0.00} a={angle:0} crossing=({c.x:+0.000;-0.000}, {c.z:+0.000;-0.000}) |{new Vector2(c.x, c.z).magnitude:0.000}| {(o.Made ? "MADE" : "MISS")}");
                if (!o.Made) misses.Add($"d={distance} a={angle}");
            }
        }
        Debug.Log(log.ToString());
        Assert.IsEmpty(misses, "a perfectly aimed jump shot goes in\n" + log);
    }

    [UnityTest]
    public IEnumerator MakeRate_FallsAsTheAimOffsetGrows()
    {
        yield return new WaitForFixedUpdate();
        var log = new StringBuilder($"Make rate vs aim offset (entry {shots.entryAngleDegrees} deg: arc {ShotArc.ApexAboveRim(4.5f, shots):0.00} m at 4.5 m, {ShotArc.ApexAboveRim(6.75f, shots):0.00} m at 6.75 m; {Angles} directions each):\n");
        var rates = new List<float>();
        foreach (float distance in new[] { 4.5f, 6.75f })
        {
            foreach (float r in Offsets)
            {
                int made = 0;
                for (int k = 0; k < Angles; k++)
                {
                    float a = k * Mathf.PI * 2f / Angles;
                    var o = new Outcome();
                    yield return Shoot(distance, 0f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r, ShotArc.ApexAboveRim(distance, shots), o);
                    if (o.Made) made++;
                }
                float rate = made / (float)Angles;
                if (distance > 6f) rates.Add(rate);
                log.AppendLine($"  d={distance:0.00} offset={r:0.00} m: {made}/{Angles} ({rate:P0})");
            }
        }
        Debug.Log(log.ToString());
        Assert.GreaterOrEqual(rates[0], rates[rates.Count - 1], "a bigger miss never goes in more often\n" + log);
    }

    // Layups use a low arc, so their make curve is its own: logged to tune layupBaseError.
    [UnityTest]
    public IEnumerator LayupArc_MakeRateByAimOffset_IsLogged()
    {
        yield return new WaitForFixedUpdate();
        var log = new StringBuilder($"Layup make rate vs aim offset (arc {shots.layupArcHeight} m, 1.5 m, {Angles} directions each):\n");
        int madeAtSmallest = 0;
        foreach (float r in Offsets)
        {
            int made = 0;
            for (int k = 0; k < Angles; k++)
            {
                float a = k * Mathf.PI * 2f / Angles;
                var o = new Outcome();
                yield return Shoot(1.5f, 0f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r, shots.layupArcHeight, o);
                if (o.Made) made++;
            }
            if (r == Offsets[0]) madeAtSmallest = made;
            log.AppendLine($"  offset={r:0.00} m: {made}/{Angles}");
        }
        Debug.Log(log.ToString());
        Assert.AreEqual(Angles, madeAtSmallest, "a near-perfect layup goes in\n" + log);
    }
}
