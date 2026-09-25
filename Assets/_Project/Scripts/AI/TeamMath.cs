using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    // The AI's own reads of the floor (pure functions over the snapshot).
    public static class TeamMath
    {
        public static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        public static float FlatDistance(Vector3 a, Vector3 b) => Flat(b - a).magnitude;

        public static float NearestOpponentDistance(MatchSnapshot s, int index)
        {
            TeamId team = s.GetTeam(index);
            float best = float.MaxValue;
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (s.GetTeam(i) == team) continue;
                best = Mathf.Min(best, FlatDistance(s.GetPosition(i), s.GetPosition(index)));
            }
            return best;
        }

        // Which shooting attribute a shot from this distance uses (the AI's read).
        public static AttributeId ShotSkill(float distance, float threePointRadius)
        {
            if (distance <= 2.6f) return AttributeId.Layup;
            if (distance <= 4.5f) return AttributeId.CloseShot;
            return distance >= threePointRadius ? AttributeId.ThreePoint : AttributeId.MidRange;
        }

        // Estimated value of a shot by `index` from where they stand: make chance from
        // distance, the shooter's skill there and the nearest defender, times the arc bonus.
        // The AI knows its teammates' strengths: a shooter's open three reads better than a
        // center's.
        // How contested a shot from here would be, by the AI's own read (0 open .. 1 draped).
        public static float ContestRead(MatchSnapshot s, int index, AIConfig c) =>
            Mathf.Clamp01(1f - NearestOpponentDistance(s, index) / c.contestReadRadius);

        public static float ShotValue(MatchSnapshot s, int index, AIConfig c)
        {
            Vector3 rim = s.GetAttackingHoop(s.GetTeam(index));
            float distance = FlatDistance(s.GetPosition(index), rim);
            float skill = Attributes.Centered(s.GetAttribute(index, ShotSkill(distance, s.ThreePointRadius)), c.shotSkillAtZero, c.shotSkillAtMax);
            float make = Mathf.Clamp01((c.qualityAtRim - c.qualityFalloffPerMeter * distance) * skill);
            float value = make * (1f - c.contestWeight * ContestRead(s, index, c));
            float arcBonus = s.ArcValueRatio > 0f ? s.ArcValueRatio : c.threePointValueMultiplier;
            return distance >= s.ThreePointRadius ? value * arcBonus : value;
        }

        // 1 = clean lane; drops toward 0 as a defender gets close to the passing line.
        // The passer's own defender is ignored: he pressures the shot, the pass goes past him.
        public static float PassSafety(MatchSnapshot s, int passer, int receiver, AIConfig c)
        {
            Vector3 a = Flat(s.GetPosition(passer));
            Vector3 b = Flat(s.GetPosition(receiver));
            TeamId team = s.GetTeam(passer);
            int onBallDefender = s.GetMatchup(passer);
            float safety = 1f;
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (s.GetTeam(i) == team || i == onBallDefender) continue;
                float d = DistanceToSegment(Flat(s.GetPosition(i)), a, b);
                if (d < c.passLaneDanger) safety *= d / c.passLaneDanger;
            }
            return safety;
        }

        // No opponent within `clearance` of the path from the player to the rim.
        public static bool DriveLaneOpen(MatchSnapshot s, int index, float clearance)
        {
            Vector3 a = Flat(s.GetPosition(index));
            Vector3 b = Flat(s.GetAttackingHoop(s.GetTeam(index)));
            TeamId team = s.GetTeam(index);
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (s.GetTeam(i) == team) continue;
                if (DistanceToSegment(Flat(s.GetPosition(i)), a, b) < clearance) return false;
            }
            return true;
        }

        public static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return (p - a).magnitude;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            return (p - (a + ab * t)).magnitude;
        }

        // Desired direction bent around nearby players in the way (except `ignore`).
        public static Vector2 Avoid(MatchSnapshot s, int self, Vector2 direction, int ignore, float radius)
        {
            if (s == null || direction.sqrMagnitude < 0.0001f) return direction;
            Vector3 dir = new Vector3(direction.x, 0f, direction.y).normalized;
            Vector3 me = Flat(s.GetPosition(self));
            Vector3 steer = dir;
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (i == self || i == ignore) continue;
                Vector3 rel = Flat(s.GetPosition(i)) - me;
                float d = rel.magnitude;
                if (d < 0.001f || d > radius) continue;
                if (Vector3.Dot(dir, rel / d) < 0.5f) continue;
                // Push sideways, away from the obstacle.
                Vector3 side = new Vector3(dir.z, 0f, -dir.x);
                float sign = Vector3.Dot(side, rel) > 0f ? -1f : 1f;
                steer += side * sign * (1f - d / radius) * 1.5f;
            }
            steer.Normalize();
            return new Vector2(steer.x, steer.z);
        }
    }
}
