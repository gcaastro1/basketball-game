using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basket.Characters
{
    [Serializable]
    public struct ItemCost
    {
        public string itemId;
        public int count;

        public ItemCost(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }

    [Serializable]
    public class LimitBreakStage
    {
        // Level the character must have reached to perform this Limit Break.
        public int requiredLevel = 20;
        // Level cap after it (equal to requiredLevel for an "awakening" with no new levels).
        public int newLevelCap = 40;
        public List<ItemCost> costs = new List<ItemCost>();
        public List<AttributeValue> attributeBonus = new List<AttributeValue>();
        public int abilityLevelBonus = 1;
    }

    [Serializable]
    public class DupeStage
    {
        public List<AttributeValue> attributeBonus = new List<AttributeValue>();
        public int abilityLevelBonus;
        // Multiplies active ability cooldowns (0.9 = 10% faster).
        public float cooldownMultiplier = 1f;
    }

    // Progression rules shared by all characters (per-character overrides can come later).
    // Every number here is a placeholder to be balanced (brief section 7).
    [CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Basket/Progression Config")]
    public class ProgressionConfig : ScriptableObject
    {
        public int maxLevel = 60;
        public int startingLevel = 1;
        // Level cap before any Limit Break.
        public int baseLevelCap = 20;
        // XP to go from level L to L+1 = xpBase * L ^ xpExponent.
        public float xpBase = 100f;
        public float xpExponent = 1.5f;
        public float attributeCap = 99f;

        // Provisional reading of decision P-002: four Limit Breaks at 20, 40, 50 and 60;
        // the one at 60 is an "awakening" that grants bonuses without new levels.
        public List<LimitBreakStage> limitBreaks = new List<LimitBreakStage>
        {
            new LimitBreakStage { requiredLevel = 20, newLevelCap = 40 },
            new LimitBreakStage { requiredLevel = 40, newLevelCap = 50 },
            new LimitBreakStage { requiredLevel = 50, newLevelCap = 60 },
            new LimitBreakStage { requiredLevel = 60, newLevelCap = 60 },
        };

        // Base + up to this many copies (brief section 8: 6).
        public List<DupeStage> dupes = new List<DupeStage>
        {
            new DupeStage(), new DupeStage(), new DupeStage(), new DupeStage(), new DupeStage(), new DupeStage(),
        };
    }
}
