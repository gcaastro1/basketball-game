using System;
using Basket.Characters;

namespace Basket.Meta
{
    public enum UpgradeResult { Done, UnknownCharacter, NotAnXpItem, NotEnoughItems, AtLevelCap, LevelTooLow, AllLimitBreaksDone, MissingMaterials }

    // Spending evolution and Limit Break items on characters (the progression half of the
    // meta; the rules themselves are CharacterProgression + ProgressionConfig).
    public static class CharacterUpgrades
    {
        // Uses up to `count` XP items, never more than the level cap can absorb.
        public static UpgradeResult UseXpItems(Inventory inventory, IEconomyService economy, ProgressionConfig config,
            string characterId, ItemDefinition item, int count, out int used)
        {
            used = 0;
            CharacterInstance c = inventory.GetCharacter(characterId);
            if (c == null) return UpgradeResult.UnknownCharacter;
            if (item == null || item.kind != ItemKind.EvolutionItem || item.xpValue <= 0) return UpgradeResult.NotAnXpItem;
            long toCap = XpToCap(c, config);
            if (toCap <= 0) return UpgradeResult.AtLevelCap;
            long useful = (toCap + item.xpValue - 1) / item.xpValue;
            used = (int)Math.Min(count, useful);
            if (used <= 0 || !economy.TrySpend(new[] { new ItemCost(item.itemId, used) }, "xp:" + characterId))
            {
                used = 0;
                return UpgradeResult.NotEnoughItems;
            }
            CharacterProgression.AddXp(c, config, (int)Math.Min(int.MaxValue, (long)used * item.xpValue));
            inventory.NotifyCharacterChanged(characterId);
            return UpgradeResult.Done;
        }

        // XP still needed to reach the current level cap.
        public static long XpToCap(CharacterInstance c, ProgressionConfig config)
        {
            int cap = CharacterProgression.LevelCap(config, c.limitBreak);
            long total = 0;
            for (int level = c.level; level < cap; level++) total += CharacterProgression.XpToNextLevel(config, level);
            return Math.Max(0, total - c.xp);
        }

        public static UpgradeResult LimitBreak(Inventory inventory, ProgressionConfig config, string characterId)
        {
            CharacterInstance c = inventory.GetCharacter(characterId);
            if (c == null) return UpgradeResult.UnknownCharacter;
            switch (CharacterProgression.CanLimitBreak(c, config, inventory))
            {
                case LimitBreakBlock.AllDone: return UpgradeResult.AllLimitBreaksDone;
                case LimitBreakBlock.LevelTooLow: return UpgradeResult.LevelTooLow;
                case LimitBreakBlock.MissingMaterials: return UpgradeResult.MissingMaterials;
            }
            CharacterProgression.LimitBreak(c, config, inventory);
            inventory.NotifyCharacterChanged(characterId);
            return UpgradeResult.Done;
        }
    }
}
