using UnityEngine;

namespace Basket.Characters
{
    // Materials/currency the player owns. Implemented by the inventory (Etapa 8).
    public interface IItemWallet
    {
        int Count(string itemId);
        void Spend(string itemId, int count);
    }

    public enum LimitBreakBlock { None, AllDone, LevelTooLow, MissingMaterials }

    public static class CharacterProgression
    {
        public static int LevelCap(ProgressionConfig c, int limitBreak)
        {
            int cap = c.baseLevelCap;
            for (int i = 0; i < limitBreak && i < c.limitBreaks.Count; i++) cap = c.limitBreaks[i].newLevelCap;
            return Mathf.Min(cap, c.maxLevel);
        }

        public static int XpToNextLevel(ProgressionConfig c, int level) =>
            Mathf.Max(1, Mathf.RoundToInt(c.xpBase * Mathf.Pow(level, c.xpExponent)));

        // Adds XP and levels up as far as the current cap allows. XP beyond the cap is
        // discarded (provisional; could be banked instead). Returns levels gained.
        public static int AddXp(CharacterInstance instance, ProgressionConfig c, int amount)
        {
            int cap = LevelCap(c, instance.limitBreak);
            int gained = 0;
            if (instance.level >= cap) return 0;
            instance.xp += Mathf.Max(0, amount);
            while (instance.level < cap)
            {
                int needed = XpToNextLevel(c, instance.level);
                if (instance.xp < needed) break;
                instance.xp -= needed;
                instance.level++;
                gained++;
            }
            if (instance.level >= cap) instance.xp = 0;
            return gained;
        }

        public static LimitBreakBlock CanLimitBreak(CharacterInstance instance, ProgressionConfig c, IItemWallet wallet)
        {
            if (instance.limitBreak >= c.limitBreaks.Count) return LimitBreakBlock.AllDone;
            LimitBreakStage stage = c.limitBreaks[instance.limitBreak];
            if (instance.level < stage.requiredLevel) return LimitBreakBlock.LevelTooLow;
            foreach (ItemCost cost in stage.costs)
            {
                if (cost.count > 0 && (wallet == null || wallet.Count(cost.itemId) < cost.count)) return LimitBreakBlock.MissingMaterials;
            }
            return LimitBreakBlock.None;
        }

        public static bool LimitBreak(CharacterInstance instance, ProgressionConfig c, IItemWallet wallet)
        {
            if (CanLimitBreak(instance, c, wallet) != LimitBreakBlock.None) return false;
            foreach (ItemCost cost in c.limitBreaks[instance.limitBreak].costs)
            {
                if (cost.count > 0) wallet.Spend(cost.itemId, cost.count);
            }
            instance.limitBreak++;
            return true;
        }

        public static bool CanAddDupe(CharacterInstance instance, ProgressionConfig c) => instance.dupes < c.dupes.Count;

        public static bool AddDupe(CharacterInstance instance, ProgressionConfig c)
        {
            if (!CanAddDupe(instance, c)) return false;
            instance.dupes++;
            return true;
        }
    }
}
