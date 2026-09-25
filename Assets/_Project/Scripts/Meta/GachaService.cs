using System;
using System.Collections.Generic;
using Basket.Characters;

namespace Basket.Meta
{
    public readonly struct PullResult
    {
        public readonly string CharacterId;
        public readonly string RarityId;
        public readonly bool Featured;
        public readonly ObtainOutcome Outcome;
        public readonly int PityCount;

        public PullResult(string characterId, string rarityId, bool featured, ObtainOutcome outcome, int pityCount)
        {
            CharacterId = characterId;
            RarityId = rarityId;
            Featured = featured;
            Outcome = outcome;
            PityCount = pityCount;
        }
    }

    public enum PullFailure { None, InvalidCount, CannotAfford, InvalidBanner }

    public sealed class PullOutcome
    {
        public PullFailure Failure;
        public readonly List<PullResult> Results = new List<PullResult>();
        public bool Success => Failure == PullFailure.None;
    }

    // Pulls = economy (pay) + engine (roll) + obtain rules (grant) + history. Presentation
    // (the pull animation/screen) only reads PullOutcome.
    public sealed class GachaService
    {
        public const int HistoryLimit = 1000;

        private readonly IEconomyService economy;
        private readonly Inventory inventory;
        private readonly ProgressionConfig progression;
        private readonly CharacterObtainRules rules;
        private readonly GachaSaveData data;
        private readonly Random rng;
        private readonly Func<long> clock;

        public GachaService(IEconomyService economy, Inventory inventory, ProgressionConfig progression,
            CharacterObtainRules rules, GachaSaveData data, Random rng, Func<long> utcTicks = null)
        {
            this.economy = economy;
            this.inventory = inventory;
            this.progression = progression;
            this.rules = rules;
            this.data = data;
            this.rng = rng ?? new Random();
            clock = utcTicks ?? (() => DateTime.UtcNow.Ticks);
        }

        public GachaSaveData Data => data;

        // The cost of `count` pulls: the multi price for a full multi-pull, single price otherwise.
        public static ItemCost CostOf(BannerDefinition b, int count) =>
            count == b.multiCount && b.multiCount > 1
                ? b.multiCost
                : new ItemCost(b.singleCost.itemId, b.singleCost.count * count);

        public PullOutcome Pull(BannerDefinition banner, int count)
        {
            var outcome = new PullOutcome();
            if (banner == null || GachaEngine.Validate(banner).Count > 0) { outcome.Failure = PullFailure.InvalidBanner; return outcome; }
            if (count < 1 || count > Math.Max(1, banner.multiCount)) { outcome.Failure = PullFailure.InvalidCount; return outcome; }
            if (!economy.TrySpend(new[] { CostOf(banner, count) }, "gacha:" + banner.bannerId))
            {
                outcome.Failure = PullFailure.CannotAfford;
                return outcome;
            }

            BannerPityState state = data.PityFor(banner.PityGroup);
            int guarantee = count > 1 ? banner.RarityIndex(banner.multiGuaranteeRarityId) : -1;
            bool metGuarantee = false;
            for (int i = 0; i < count; i++)
            {
                bool last = i == count - 1;
                int min = guarantee >= 0 && last && !metGuarantee ? guarantee : -1;
                GachaRoll roll = GachaEngine.Roll(banner, state, rng, min);
                if (guarantee >= 0 && roll.RarityIndex <= guarantee) metGuarantee = true;

                GachaEntry entry = banner.pool[roll.EntryIndex];
                string id = entry.character.characterId;
                ObtainResult obtained = CharacterObtainer.Obtain(id, inventory, economy, progression, rules, "gacha:" + banner.bannerId);
                var result = new PullResult(id, banner.rarities[roll.RarityIndex].rarityId, roll.Featured, obtained.Outcome, roll.PityCount);
                outcome.Results.Add(result);
                Record(banner, result);
            }
            return outcome;
        }

        private void Record(BannerDefinition banner, in PullResult r)
        {
            data.history.Add(new GachaHistoryEntry
            {
                bannerId = banner.bannerId,
                characterId = r.CharacterId,
                rarityId = r.RarityId,
                featured = r.Featured,
                outcome = r.Outcome,
                pityCount = r.PityCount,
                utcTicks = clock(),
            });
            if (data.history.Count > HistoryLimit) data.history.RemoveRange(0, data.history.Count - HistoryLimit);
        }
    }
}
