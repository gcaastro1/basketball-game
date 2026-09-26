using System.Collections.Generic;
using UnityEngine;

namespace Basket.AI
{
    public enum DefenseScheme { ManToMan, Zone }

    // Where defenders stand (pure functions of positions + AIConfig).
    //   Man-to-man off the ball: "ball - you - man". One pass away, deny: between the man and
    //   the rim, a step into the passing lane. Farther from the ball, sag toward the help line
    //   (a point between the rim and the ball), leashed to the man so he can still close out.
    //   Zone: fixed spots around the rim (2-3, 2-2, 1-2...) sliding toward the ball.
    public static class DefenseFormation
    {
        public static Vector3 OffBallSpot(Vector3 man, Vector3 ball, Vector3 rimFloor, AIConfig c)
        {
            man = TeamMath.Flat(man);
            ball = TeamMath.Flat(ball);
            rimFloor = TeamMath.Flat(rimFloor);

            Vector3 toRim = rimFloor - man;
            float toRimDistance = toRim.magnitude;
            Vector3 between = toRimDistance > 0.001f ? man + toRim / toRimDistance * Mathf.Min(c.guardDistance, toRimDistance) : man;

            Vector3 toBall = ball - man;
            float manToBall = toBall.magnitude;
            Vector3 deny = manToBall > 0.001f ? between + toBall / manToBall * c.denyLaneStep : between;

            float span = Mathf.Max(0.01f, c.helpFullDistance - c.denyDistance);
            float sag = Mathf.Clamp01((manToBall - c.denyDistance) / span) * c.maxSag;
            Vector3 helpPoint = rimFloor + (ball - rimFloor) * c.helpLineFraction;
            Vector3 spot = Vector3.Lerp(deny, helpPoint, sag);

            Vector3 fromMan = spot - man;
            if (fromMan.magnitude > c.maxSagFromMan) spot = man + fromMan.normalized * c.maxSagFromMan;
            return spot;
        }

        // Base zone shape by number of defenders: (side, depth) in meters from the rim,
        // depth measured toward the court's center. 5: 2-3, 4: 2-2, 3: 1-2, 2: 1-1.
        public static Vector2[] Formation(int count)
        {
            switch (count)
            {
                case 1: return new[] { new Vector2(0f, 2.5f) };
                case 2: return new[] { new Vector2(0f, 5.5f), new Vector2(0f, 1.6f) };
                case 3: return new[] { new Vector2(0f, 6f), new Vector2(-2.8f, 2f), new Vector2(2.8f, 2f) };
                case 4: return new[] { new Vector2(-2.4f, 5.6f), new Vector2(2.4f, 5.6f), new Vector2(-2.8f, 1.8f), new Vector2(2.8f, 1.8f) };
                default:
                    var five = new List<Vector2>
                    {
                        new Vector2(-2.4f, 5.8f), new Vector2(2.4f, 5.8f),
                        new Vector2(-3.8f, 1.8f), new Vector2(0f, 1.2f), new Vector2(3.8f, 1.8f),
                    };
                    // More than five (not a real case): extra defenders fill the middle.
                    for (int i = 5; i < count; i++) five.Add(new Vector2(0f, 3.5f));
                    return five.ToArray();
            }
        }

        public static List<Vector3> ZoneSpots(Vector3 rimFloor, Vector3 courtAxis, Vector3 ball, int count, AIConfig c)
        {
            rimFloor = TeamMath.Flat(rimFloor);
            Vector3 axis = TeamMath.Flat(courtAxis);
            axis = axis.sqrMagnitude < 0.0001f ? Vector3.back : axis.normalized;
            Vector3 side = new Vector3(axis.z, 0f, -axis.x);
            Vector2[] shape = Formation(count);
            var spots = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                Vector3 home = rimFloor + side * shape[i].x + axis * shape[i].y;
                Vector3 shift = (TeamMath.Flat(ball) - home) * c.zoneShift;
                if (shift.magnitude > c.zoneShiftMax) shift = shift.normalized * c.zoneShiftMax;
                Vector3 spot = home + shift;
                // Never behind the rim toward the baseline.
                float depth = Vector3.Dot(spot - rimFloor, axis);
                if (depth < 0.8f) spot += axis * (0.8f - depth);
                spots.Add(spot);
            }
            return spots;
        }

        // Marking an attacker inside one's zone: between him and the rim.
        public static Vector3 MarkSpot(Vector3 attacker, Vector3 rimFloor, float distance) =>
            DefensePlanner.HelpSpot(attacker, rimFloor, distance);
    }
}
