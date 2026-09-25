using Basket.Core;
using UnityEngine;

namespace Basket.AI
{
    // Chooses the play for a possession. This is the seam for Mode B (team/tactical
    // control, decision P-001): a human "coach" becomes another implementation.
    public interface ITeamStrategy
    {
        PlayType ChoosePlay(MatchSnapshot snapshot, TeamId team, int handler);
    }

    public sealed class AutoStrategy : ITeamStrategy
    {
        private readonly AIConfig config;
        private readonly System.Random rng;

        public AutoStrategy(AIConfig config, System.Random rng)
        {
            this.config = config;
            this.rng = rng ?? new System.Random();
        }

        public PlayType ChoosePlay(MatchSnapshot snapshot, TeamId team, int handler)
        {
            int teammates = 0;
            for (int i = 0; i < snapshot.PlayerCount; i++) if (snapshot.GetTeam(i) == team) teammates++;
            if (teammates < 2) return PlayType.Isolation;

            float s = Mathf.Max(0f, config.spacingPlayWeight);
            float p = Mathf.Max(0f, config.pickAndRollPlayWeight);
            float iso = Mathf.Max(0f, config.isolationPlayWeight);
            float roll = (float)rng.NextDouble() * Mathf.Max(0.0001f, s + p + iso);
            if (roll < s) return PlayType.Spacing;
            if (roll < s + p) return PlayType.PickAndRoll;
            return PlayType.Isolation;
        }
    }
}
