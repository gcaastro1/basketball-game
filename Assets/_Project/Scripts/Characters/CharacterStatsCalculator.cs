using System.Collections.Generic;
using Basket.Core;

namespace Basket.Characters
{
    // Effective attributes and ability power of a character instance:
    // base + growth x (level - 1) + Limit Break bonuses + dupe bonuses, clamped to the cap.
    public static class CharacterStatsCalculator
    {
        public const float DefaultBaseAttribute = 50f;

        public static AttributeSet Compute(CharacterDefinition def, CharacterInstance inst, ProgressionConfig c)
        {
            var set = AttributeSet.Uniform(DefaultBaseAttribute);
            foreach (AttributeValue v in def.baseAttributes) set.Set(v.attribute, v.value);

            int levelsAboveOne = System.Math.Max(0, inst.level - 1);
            foreach (AttributeValue g in def.growthPerLevel) set.Add(g.attribute, g.value * levelsAboveOne);

            for (int i = 0; i < inst.limitBreak && i < c.limitBreaks.Count; i++)
            {
                foreach (AttributeValue b in c.limitBreaks[i].attributeBonus) set.Add(b.attribute, b.value);
            }
            for (int i = 0; i < inst.dupes && i < c.dupes.Count; i++)
            {
                foreach (AttributeValue b in c.dupes[i].attributeBonus) set.Add(b.attribute, b.value);
            }
            set.Clamp(0f, c.attributeCap);
            return set;
        }

        public static int AbilityLevel(CharacterInstance inst, ProgressionConfig c)
        {
            int level = 1;
            for (int i = 0; i < inst.limitBreak && i < c.limitBreaks.Count; i++) level += c.limitBreaks[i].abilityLevelBonus;
            for (int i = 0; i < inst.dupes && i < c.dupes.Count; i++) level += c.dupes[i].abilityLevelBonus;
            return level;
        }

        public static float CooldownMultiplier(CharacterInstance inst, ProgressionConfig c)
        {
            float m = 1f;
            for (int i = 0; i < inst.dupes && i < c.dupes.Count; i++) m *= c.dupes[i].cooldownMultiplier;
            return m;
        }

        public static List<AbilityDefinition> UnlockedAbilities(CharacterDefinition def, CharacterInstance inst)
        {
            var list = new List<AbilityDefinition>();
            foreach (AbilityUnlock u in def.abilities)
            {
                if (u.ability != null && u.unlockAtLimitBreak <= inst.limitBreak) list.Add(u.ability);
            }
            return list;
        }
    }
}
