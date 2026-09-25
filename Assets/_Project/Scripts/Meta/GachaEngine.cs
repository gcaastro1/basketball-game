using System;
using System.Collections.Generic;

namespace Basket.Meta
{
    public readonly struct GachaRoll
    {
        public readonly int EntryIndex;
        public readonly int RarityIndex;
        public readonly bool Featured;
        public readonly int PityCount;

        public GachaRoll(int entryIndex, int rarityIndex, bool featured, int pityCount)
        {
            EntryIndex = entryIndex;
            RarityIndex = rarityIndex;
            Featured = featured;
            PityCount = pityCount;
        }
    }

    // The gacha's technical core: one pull from a banner's data and a pity state, with an
    // injected random source (seeded in tests). No economy, no inventory, no visuals.
    public static class GachaEngine
    {
        // Chance of the top rarity (index 0) on the `pullNumber`-th pull since the last one.
        public static float TopRate(BannerDefinition b, int pullNumber)
        {
            float baseRate = Normalized(b)[0];
            GachaPity p = b.pity;
            if (p.hardPity > 0 && pullNumber >= p.hardPity) return 1f;
            if (p.softPityStart > 0 && pullNumber >= p.softPityStart)
                return Math.Min(1f, baseRate + p.softPityStep * (pullNumber - p.softPityStart + 1));
            return baseRate;
        }

        // Rates per rarity for this pull: the top rarity's (pity-adjusted) chance, the rest
        // sharing what is left in their base proportions; rarities below `minRarityIndex`
        // (a multi-pull guarantee) are excluded.
        public static float[] Rates(BannerDefinition b, int pullNumber, int minRarityIndex = -1)
        {
            float[] base_ = Normalized(b);
            var rates = new float[base_.Length];
            float top = TopRate(b, pullNumber);
            rates[0] = top;
            float restBase = 0f;
            for (int i = 1; i < base_.Length; i++) restBase += base_[i];
            for (int i = 1; i < base_.Length; i++) rates[i] = restBase > 0f ? base_[i] / restBase * (1f - top) : 0f;
            if (minRarityIndex >= 0)
            {
                for (int i = minRarityIndex + 1; i < rates.Length; i++) rates[i] = 0f;
                float sum = 0f;
                foreach (float r in rates) sum += r;
                for (int i = 0; i < rates.Length; i++) rates[i] = sum > 0f ? rates[i] / sum : (i == 0 ? 1f : 0f);
            }
            return rates;
        }

        public static GachaRoll Roll(BannerDefinition b, BannerPityState state, Random rng, int minRarityIndex = -1)
        {
            int pullNumber = state.pullsSinceTop + 1;
            float[] rates = Rates(b, pullNumber, minRarityIndex);
            int rarity = Pick(rates, rng.NextDouble());

            bool featured;
            int entry = PickEntry(b, rarity, state, rng, out featured);

            state.totalPulls++;
            if (rarity == 0) state.pullsSinceTop = 0;
            else state.pullsSinceTop++;
            return new GachaRoll(entry, rarity, featured, pullNumber);
        }

        private static int PickEntry(BannerDefinition b, int rarity, BannerPityState state, Random rng, out bool featured)
        {
            string rarityId = b.rarities[rarity].rarityId;
            var candidates = new List<int>();
            bool hasFeatured = false, hasOthers = false;
            for (int i = 0; i < b.pool.Count; i++)
            {
                if (b.pool[i].rarityId != rarityId || b.pool[i].character == null) continue;
                candidates.Add(i);
                if (b.pool[i].featured) hasFeatured = true;
                else hasOthers = true;
            }
            if (candidates.Count == 0) throw new InvalidOperationException($"Banner {b.bannerId}: no characters of rarity {rarityId}");

            bool wantFeatured = false;
            if (rarity == 0 && b.pity.featuredGuarantee && hasFeatured)
            {
                wantFeatured = !hasOthers || state.featuredGuaranteed || rng.NextDouble() < b.pity.featuredChance;
                // Losing the rate-up guarantees the next top pull is featured.
                state.featuredGuaranteed = !wantFeatured;
            }
            else if (rarity == 0 && hasFeatured && !hasOthers)
            {
                wantFeatured = true;
            }

            var pool = new List<int>();
            foreach (int i in candidates)
            {
                if (rarity != 0 || !b.pity.featuredGuarantee || !hasFeatured || b.pool[i].featured == wantFeatured) pool.Add(i);
            }
            if (pool.Count == 0) pool = candidates;

            float total = 0f;
            foreach (int i in pool) total += Math.Max(0f, b.pool[i].weight);
            double pick = rng.NextDouble() * total;
            int chosen = pool[pool.Count - 1];
            foreach (int i in pool)
            {
                pick -= Math.Max(0f, b.pool[i].weight);
                if (pick < 0) { chosen = i; break; }
            }
            featured = b.pool[chosen].featured;
            return chosen;
        }

        private static int Pick(float[] rates, double roll)
        {
            double acc = 0;
            for (int i = 0; i < rates.Length; i++)
            {
                acc += rates[i];
                if (roll < acc) return i;
            }
            for (int i = rates.Length - 1; i >= 0; i--) if (rates[i] > 0f) return i;
            return rates.Length - 1;
        }

        private static float[] Normalized(BannerDefinition b)
        {
            var r = new float[b.rarities.Count];
            float sum = 0f;
            for (int i = 0; i < r.Length; i++) sum += Math.Max(0f, b.rarities[i].rate);
            for (int i = 0; i < r.Length; i++) r[i] = sum > 0f ? Math.Max(0f, b.rarities[i].rate) / sum : 0f;
            return r;
        }

        // Data problems that would break pulls (for tests and the editor).
        public static List<string> Validate(BannerDefinition b)
        {
            var errors = new List<string>();
            if (b.rarities.Count == 0) errors.Add("no rarities");
            float sum = 0f;
            foreach (GachaRarity r in b.rarities) sum += r.rate;
            if (Math.Abs(sum - 1f) > 0.001f) errors.Add($"rates sum to {sum:0.###}, not 1");
            foreach (GachaRarity r in b.rarities)
            {
                if (!b.pool.Exists(e => e.rarityId == r.rarityId && e.character != null)) errors.Add($"rarity {r.rarityId} has no characters");
            }
            foreach (GachaEntry e in b.pool)
            {
                if (b.RarityIndex(e.rarityId) < 0) errors.Add($"entry {(e.character != null ? e.character.characterId : "?")} has unknown rarity {e.rarityId}");
            }
            if (!string.IsNullOrEmpty(b.multiGuaranteeRarityId) && b.RarityIndex(b.multiGuaranteeRarityId) < 0)
                errors.Add($"multi-pull guarantee rarity {b.multiGuaranteeRarityId} does not exist");
            if (b.multiCount < 1) errors.Add("multiCount below 1");
            return errors;
        }
    }
}
