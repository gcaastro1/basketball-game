using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.Characters
{
    [Serializable]
    public struct AttributeValue
    {
        public AttributeId attribute;
        public float value;

        public AttributeValue(AttributeId attribute, float value)
        {
            this.attribute = attribute;
            this.value = value;
        }
    }

    [Serializable]
    public struct AbilityUnlock
    {
        public AbilityDefinition ability;
        // Limit Break stage that unlocks it (0 = from the start).
        public int unlockAtLimitBreak;
    }

    // Static data of one character. Names, lore and art are undecided (brief section 3):
    // these fields exist, the content is placeholder.
    [CreateAssetMenu(fileName = "Character", menuName = "Basket/Character Definition")]
    public class CharacterDefinition : ScriptableObject
    {
        public string characterId = "character";
        public string displayName = "Placeholder";
        // Free-form play-style tag (e.g. "Sharpshooter"); not a closed list on purpose.
        public string archetype = "Balanced";

        [Tooltip("Attributes at level 1. Missing attributes start at 50.")]
        public List<AttributeValue> baseAttributes = new List<AttributeValue>();
        [Tooltip("Points gained per level above 1.")]
        public List<AttributeValue> growthPerLevel = new List<AttributeValue>();

        public List<AbilityUnlock> abilities = new List<AbilityUnlock>();
        public AITendencies aiTendencies = AITendencies.Neutral;
    }

    // Per-player save data (brief section 28): what the player owns of a character.
    [Serializable]
    public class CharacterInstance
    {
        public string characterId;
        public int level = 1;
        public int xp;
        public int limitBreak;
        public int dupes;

        public CharacterInstance() { }

        public CharacterInstance(string characterId, int level = 1, int limitBreak = 0, int dupes = 0)
        {
            this.characterId = characterId;
            this.level = level;
            this.limitBreak = limitBreak;
            this.dupes = dupes;
        }
    }
}
