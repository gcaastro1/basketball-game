using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

// Live-play shooting against the model (own class: ShotCalibrationTests builds a bare arena
// in SetUp, which would overlap a TestMatch's).
public class LiveShotTests
{
    private float previousScale;

    [SetUp]
    public void SetUp() => previousScale = Time.timeScale;

    [TearDown]
    public void TearDown() => Time.timeScale = previousScale;

    // Rebounds a miss, walks back to the spot, sets the feet, shoots at the apex.
    private static System.Func<MatchSnapshot, int, PlayerCommand> ShootFromSpotAndRebound(Vector3 spot)
    {
        float settledSince = -1f;
        return (s, self) =>
        {
            Vector3 me = s.GetPosition(self);
            if (s.BallHolderIndex != self)
            {
                settledSince = -1f;
                if (s.BallState != BallState.Free) return PlayerCommand.None;
                Vector3 toBall = s.BallPosition - me;
                return new PlayerCommand(new Vector2(toBall.x, toBall.z).normalized, sprint: true);
            }
            Vector3 toSpot = spot - me;
            toSpot.y = 0f;
            if (toSpot.magnitude > 0.3f && s.IsGrounded(self))
            {
                settledSince = -1f;
                return new PlayerCommand(new Vector2(toSpot.x, toSpot.z).normalized);
            }
            if (settledSince < 0f) settledSince = s.Time;
            if (s.Time - settledSince < 0.5f) return PlayerCommand.None; // stop and set the feet
            bool atApex = !s.IsGrounded(self) && s.GetVelocity(self).y <= 0f;
            return new PlayerCommand(Vector2.zero, shootHeld: !atApex);
        };
    }

    // Live play, no defense: one shooter keeps the check ball and shoots from the check
    // spot (7.6 m) with real accuracy. The makes must match the model's own expectation
    // (sum of each shot's make chance); the AI-vs-AI runs made ~1 of ~7 expected long
    // jumpers, so this separates "long shots break in live play" from defense effects.
    [UnityTest]
    public IEnumerator LiveJumpShots_Unguarded_MakeTheirExpectedShare()
    {
        Time.timeScale = 4f;
        var match = new TestMatch();
        try
        {
            var defaults = ScriptableObject.CreateInstance<ShotConfig>();
            match.ShotConfig.jumpShotBaseError = defaults.jumpShotBaseError;
            match.ShotConfig.jumpShotErrorPerMeter = defaults.jumpShotErrorPerMeter;
            match.Rules.winningScore = 0;
            match.Start((TeamId.Home, ShootFromSpotAndRebound(match.Court.checkBallSpot)));
            const int shots = 25;
            yield return match.RunUntil(() => match.Shots.Count >= shots && match.Sim.Ball.CurrentState == BallState.Held, 150f);

            float expected = 0f;
            foreach (ShotReport r in match.Shots) expected += ShotAccuracyModel.EstimatedMakeChance(r.ErrorRadius, match.ShotConfig, r.Type);
            int made = match.MadeShots.Count;
            string traces = string.Join("\n", match.Events.FindAll(e => e.StartsWith("SHOT TRACE")));
            Debug.Log($"Live unguarded jumpers: {made}/{match.Shots.Count} made, model expected {expected:0.0}\n{traces}");
            Assert.GreaterOrEqual(match.Shots.Count, 10, "enough shots to judge");
            Assert.GreaterOrEqual(made, expected * 0.4f, "live makes far below the model's expectation\n" + traces);
        }
        finally
        {
            match.Dispose();
        }
    }

    // Shot meter: a release in the green is a perfect shot -- no aim error, and it goes in
    // (the green is widened here so every apex release lands in it).
    [UnityTest]
    public IEnumerator GreenReleases_AlwaysGoIn()
    {
        Time.timeScale = 4f;
        var match = new TestMatch();
        try
        {
            var defaults = ScriptableObject.CreateInstance<ShotConfig>();
            match.ShotConfig.jumpShotBaseError = defaults.jumpShotBaseError;
            match.ShotConfig.jumpShotErrorPerMeter = defaults.jumpShotErrorPerMeter;
            match.ShotConfig.greenHalfWidthAtRatingZero = 0.2f;
            match.ShotConfig.greenHalfWidthAtRatingOne = 0.2f;
            match.ShotConfig.greenMinScale = 1f;
            match.Rules.winningScore = 0;
            match.Start((TeamId.Home, ShootFromSpotAndRebound(match.Court.checkBallSpot)));
            const int shots = 6;
            yield return match.RunUntil(() => match.Shots.Count >= shots && match.Sim.Ball.CurrentState == BallState.Held, 90f);

            int green = 0;
            foreach (ShotReport r in match.Shots) if (r.IsGreen) green++;
            string traces = string.Join("\n", match.Events.FindAll(e => e.StartsWith("SHOT TRACE")));
            Debug.Log($"Green releases: {green}/{match.Shots.Count}, made {match.MadeShots.Count}\n{traces}");
            Assert.GreaterOrEqual(match.Shots.Count, shots);
            Assert.AreEqual(match.Shots.Count, green, "every apex release is in the (wide) green");
            Assert.AreEqual(match.Shots.Count, match.MadeShots.Count, "green = perfect shot\n" + traces);
        }
        finally
        {
            match.Dispose();
        }
    }
}
