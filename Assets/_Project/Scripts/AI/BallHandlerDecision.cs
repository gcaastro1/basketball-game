using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    public enum HandlerActionKind { Hold, Shoot, Drive, Pass }

    public readonly struct HandlerAction
    {
        public readonly HandlerActionKind Kind;
        public readonly Vector3 MoveTarget;
        public readonly int PassTarget;

        public HandlerAction(HandlerActionKind kind, Vector3 moveTarget = default, int passTarget = -1)
        {
            Kind = kind;
            MoveTarget = moveTarget;
            PassTarget = passTarget;
        }
    }

    // Utility decision for the player with the ball in team play: take the shot if it is
    // good, move the ball if a teammate has a clearly better one, attack an open lane (or
    // the screen), otherwise hold -- and never hold forever.
    public static class BallHandlerDecision
    {
        public static HandlerAction Decide(MatchSnapshot s, int self, TeamOrder order, float heldFor, AIConfig c)
            => Decide(s, self, order, heldFor, c, AITendencies.Neutral);

        // Tendencies (per character) and IQ attributes shape the decision: a sharpshooter
        // likes his threes, a playmaker passes more, a slasher attacks the rim, a high
        // Shot Selection player waits for a better look.
        public static HandlerAction Decide(MatchSnapshot s, int self, TeamOrder order, float heldFor, AIConfig c, AITendencies t)
        {
            Vector3 rim = s.GetAttackingHoop(s.GetTeam(self));
            Vector3 rimFloor = new Vector3(rim.x, 0f, rim.z);
            float distance = TeamMath.FlatDistance(s.GetPosition(self), rim);

            if (distance <= c.driveFinishDistance) return new HandlerAction(HandlerActionKind.Shoot);

            // Full court: far from the basket, bring the ball up (or push a fast break when
            // nobody is back); only pass ahead to an open teammate.
            if (distance > c.advanceDistance)
            {
                int ahead = BestPassTarget(s, self, c, out float aheadValue);
                if (ahead >= 0 && aheadValue >= c.shootQualityThreshold && heldFor >= c.minHoldSecondsBeforeShot)
                    return new HandlerAction(HandlerActionKind.Pass, s.GetPosition(ahead), ahead);
                if (TeamMath.DriveLaneOpen(s, self, c.driveLaneClearance)) return new HandlerAction(HandlerActionKind.Drive, rimFloor);
                Vector3 axis = TeamMath.Flat(s.CourtCenter - rimFloor);
                axis = axis.sqrMagnitude < 0.0001f ? Vector3.back : axis.normalized;
                return new HandlerAction(HandlerActionKind.Drive, rimFloor + axis * c.advanceSpotDistance);
            }

            float own = TeamMath.ShotValue(s, self, c);
            if (distance >= s.ThreePointRadius) own *= Pref(t.threePointPreference);
            int mate = BestPassTarget(s, self, c, out float mateValue);
            bool settled = heldFor >= c.minHoldSecondsBeforeShot;
            float threshold = c.shootQualityThreshold * Attributes.Centered(s.GetAttribute(self, AttributeId.ShotSelection), 0.8f, 1.15f);
            float passAdvantage = c.passAdvantage / Pref(t.passPreference)
                                  * Attributes.Centered(s.GetAttribute(self, AttributeId.PassingIQ), 1.5f, 0.6f);

            if (settled && own >= threshold) return new HandlerAction(HandlerActionKind.Shoot);
            if (settled && mate >= 0 && mateValue >= own + passAdvantage && mateValue >= c.passMinQuality)
                return new HandlerAction(HandlerActionKind.Pass, s.GetPosition(mate), mate);

            bool screenReady = order.Kind == TeamOrderKind.Handle && order.TargetIndex >= 0;
            if (screenReady) return new HandlerAction(HandlerActionKind.Drive, order.Target);
            if (TeamMath.DriveLaneOpen(s, self, c.driveLaneClearance / Pref(t.drivePreference))) return new HandlerAction(HandlerActionKind.Drive, rimFloor);

            if (heldFor >= c.forceDecisionSeconds)
            {
                if (own >= c.minForcedQuality || mate < 0) return new HandlerAction(HandlerActionKind.Shoot);
                return new HandlerAction(HandlerActionKind.Pass, s.GetPosition(mate), mate);
            }
            return new HandlerAction(HandlerActionKind.Hold, s.GetPosition(self));
        }

        private static float Pref(float preference) => preference > 0.01f ? preference : 1f;

        public static int BestPassTarget(MatchSnapshot s, int self, AIConfig c, out float value)
        {
            TeamId team = s.GetTeam(self);
            int best = -1;
            value = 0f;
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (i == self || s.GetTeam(i) != team) continue;
                float v = TeamMath.ShotValue(s, i, c) * TeamMath.PassSafety(s, self, i, c);
                if (v > value)
                {
                    value = v;
                    best = i;
                }
            }
            return best;
        }
    }
}
