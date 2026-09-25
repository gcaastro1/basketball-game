using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public static class FoulMath
    {
        // Defender whose body is in contact with the shooter at release: within
        // contactDistance and either in the air or closing in fast. -1 if none.
        public static int ShootingContact(MatchSnapshot s, int shooter, float contactDistance, float closingSpeed)
        {
            TeamId team = s.GetTeam(shooter);
            Vector3 shooterPos = s.GetPosition(shooter);
            int best = -1;
            float bestDistance = contactDistance;
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (s.GetTeam(i) == team) continue;
                Vector3 d = shooterPos - s.GetPosition(i);
                d.y = 0f;
                float distance = d.magnitude;
                if (distance > bestDistance) continue;

                Vector3 v = s.GetVelocity(i);
                float closing = distance > 0.001f ? Vector3.Dot(new Vector3(v.x, 0f, v.z), d / distance) : 0f;
                if (!s.IsGrounded(i) || closing >= closingSpeed)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
