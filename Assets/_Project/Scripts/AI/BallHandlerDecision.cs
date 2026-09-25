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
        {
            Vector3 rim = s.GetAttackingHoop(s.GetTeam(self));
            Vector3 rimFloor = new Vector3(rim.x, 0f, rim.z);
            float distance = TeamMath.FlatDistance(s.GetPosition(self), rim);

            if (distance <= c.driveFinishDistance) return new HandlerAction(HandlerActionKind.Shoot);

            float own = TeamMath.ShotValue(s, self, c);
            int mate = BestPassTarget(s, self, c, out float mateValue);
            bool settled = heldFor >= c.minHoldSecondsBeforeShot;

            if (settled && own >= c.shootQualityThreshold) return new HandlerAction(HandlerActionKind.Shoot);
            if (settled && mate >= 0 && mateValue >= own + c.passAdvantage && mateValue >= c.passMinQuality)
                return new HandlerAction(HandlerActionKind.Pass, s.GetPosition(mate), mate);

            bool screenReady = order.Kind == TeamOrderKind.Handle && order.TargetIndex >= 0;
            if (screenReady) return new HandlerAction(HandlerActionKind.Drive, order.Target);
            if (TeamMath.DriveLaneOpen(s, self, c.driveLaneClearance)) return new HandlerAction(HandlerActionKind.Drive, rimFloor);

            if (heldFor >= c.forceDecisionSeconds)
            {
                if (own >= c.minForcedQuality || mate < 0) return new HandlerAction(HandlerActionKind.Shoot);
                return new HandlerAction(HandlerActionKind.Pass, s.GetPosition(mate), mate);
            }
            return new HandlerAction(HandlerActionKind.Hold, s.GetPosition(self));
        }

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
