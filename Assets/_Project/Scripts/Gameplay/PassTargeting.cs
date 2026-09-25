using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public static class PassTargeting
    {
        // Teammate most aligned with the aim direction; nearest teammate when not aiming.
        // Returns -1 when the passer has no teammate (1v1): passing to an opponent is
        // never chosen automatically.
        public static int SelectTarget(MatchSnapshot snapshot, int passerIndex, Vector2 aim)
        {
            TeamId team = snapshot.GetTeam(passerIndex);
            Vector3 from = snapshot.GetPosition(passerIndex);
            Vector3 aimDir = new Vector3(aim.x, 0f, aim.y);
            bool hasAim = aimDir.sqrMagnitude > 0.01f;
            if (hasAim) aimDir.Normalize();

            int best = -1;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < snapshot.PlayerCount; i++)
            {
                if (i == passerIndex || snapshot.GetTeam(i) != team) continue;
                Vector3 d = snapshot.GetPosition(i) - from;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist < 0.01f) continue;

                float score = hasAim ? Vector3.Dot(aimDir, d / dist) : -dist;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }
    }
}
