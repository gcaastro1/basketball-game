using System.Collections.Generic;
using UnityEngine;

namespace Basket.AI
{
    public static class OffensePlanner
    {
        // Perimeter spots around the rim: wings first, then corners, then the top.
        // `clearOut` (isolation) sends everyone to the corners.
        public static List<Vector3> SpacingSlots(Vector3 rimFloor, Vector3 courtAxis, float radius, int count, bool clearOut)
        {
            float[] angles = clearOut ? new[] { 80f, -80f, 55f, -55f, 0f } : new[] { 50f, -50f, 80f, -80f, 0f };
            Vector3 axis = TeamMath.Flat(courtAxis);
            axis = axis.sqrMagnitude < 0.0001f ? Vector3.back : axis.normalized;
            var slots = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                float angle = angles[i % angles.Length];
                slots.Add(rimFloor + Quaternion.AngleAxis(angle, Vector3.up) * axis * radius);
            }
            return slots;
        }

        // Greedy nearest assignment: returns, for each player, the index of their slot.
        public static int[] AssignSlots(IReadOnlyList<Vector3> players, IReadOnlyList<Vector3> slots)
        {
            var result = new int[players.Count];
            var taken = new bool[slots.Count];
            var assigned = new bool[players.Count];
            for (int round = 0; round < players.Count; round++)
            {
                int bestPlayer = -1, bestSlot = -1;
                float bestDistance = float.MaxValue;
                for (int p = 0; p < players.Count; p++)
                {
                    if (assigned[p]) continue;
                    for (int s = 0; s < slots.Count; s++)
                    {
                        if (taken[s]) continue;
                        float d = TeamMath.FlatDistance(players[p], slots[s]);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            bestPlayer = p;
                            bestSlot = s;
                        }
                    }
                }
                if (bestPlayer < 0) break;
                assigned[bestPlayer] = true;
                taken[bestSlot] = true;
                result[bestPlayer] = bestSlot;
            }
            return result;
        }

        // Screen spot: beside the handler's defender, on the side the handler will use.
        // side = +1 / -1 picks the side perpendicular to the handler -> rim line.
        public static Vector3 ScreenSpot(Vector3 handler, Vector3 defender, Vector3 rimFloor, int side, float offset)
        {
            Vector3 toRim = TeamMath.Flat(rimFloor - handler);
            toRim = toRim.sqrMagnitude < 0.0001f ? Vector3.forward : toRim.normalized;
            Vector3 perp = new Vector3(toRim.z, 0f, -toRim.x) * side;
            return TeamMath.Flat(defender) + perp * offset;
        }

        // Where the handler goes to use the screen: past the screener, toward the rim.
        public static Vector3 DriveOffScreen(Vector3 handler, Vector3 screener, Vector3 rimFloor)
        {
            Vector3 toRim = TeamMath.Flat(rimFloor - handler);
            toRim = toRim.sqrMagnitude < 0.0001f ? Vector3.forward : toRim.normalized;
            Vector3 besideScreener = TeamMath.Flat(screener) + (TeamMath.Flat(screener) - TeamMath.Flat(handler)).normalized * 0.9f;
            return besideScreener + toRim * 1.5f;
        }

        public static Vector3 CutTarget(Vector3 player, Vector3 rimFloor)
        {
            Vector3 fromRim = TeamMath.Flat(player - rimFloor);
            fromRim = fromRim.sqrMagnitude < 0.0001f ? Vector3.back : fromRim.normalized;
            return rimFloor + fromRim * 1.0f;
        }
    }

    public static class DefensePlanner
    {
        // Driver near the rim with their defender trailing (farther from the rim).
        public static bool IsBeaten(Vector3 handler, Vector3 handlerDefender, Vector3 rimFloor, float helpRadius, float margin)
        {
            float handlerToRim = TeamMath.FlatDistance(handler, rimFloor);
            if (handlerToRim > helpRadius) return false;
            return TeamMath.FlatDistance(handlerDefender, rimFloor) > handlerToRim + margin;
        }

        public static Vector3 HelpSpot(Vector3 handler, Vector3 rimFloor, float depth)
        {
            Vector3 toRim = TeamMath.Flat(rimFloor - handler);
            float d = toRim.magnitude;
            if (d < 0.001f) return TeamMath.Flat(handler);
            return TeamMath.Flat(handler) + toRim / d * Mathf.Min(depth, d);
        }

        // Between the man and the rim, `distance` from the man: sealing him from the rebound.
        public static Vector3 BoxOutSpot(Vector3 man, Vector3 rimFloor, float distance) => HelpSpot(man, rimFloor, distance);
    }
}
