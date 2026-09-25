using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Characters;

namespace Basket.Meta
{
    // One rarity tier of a banner. Rarities are undecided (brief section 3): tiers are
    // data, ordered from the highest (index 0) down.
    [Serializable]
    public class GachaRarity
    {
        public string rarityId = "rarity";
        public string displayName = "Placeholder";
        [Tooltip("Base chance per pull (rates are normalized if they do not sum to 1).")]
        public float rate;
    }

    [Serializable]
    public class GachaEntry
    {
        public CharacterDefinition character;
        public string rarityId;
        [Tooltip("Rate-up character of this banner.")]
        public bool featured;
        public float weight = 1f;
    }

    [Serializable]
    public class GachaPity
    {
        [Tooltip("Pity counter group: banners sharing it share their counters (e.g. all standard banners).")]
        public string group = "";
        [Tooltip("Highest rarity at this many pulls without one (0 = off).")]
        public int hardPity = 80;
        [Tooltip("From this pull on, the top rarity's chance grows by softPityStep per pull (0 = off).")]
        public int softPityStart = 65;
        public float softPityStep = 0.06f;
        [Tooltip("Top-rarity pulls are featured with featuredChance; after losing it, the next one is featured.")]
        public bool featuredGuarantee = true;
        [Range(0f, 1f)] public float featuredChance = 0.5f;
    }

    // Banner data only (brief section 26 separates it from the gacha's logic, the economy,
    // presentation and obtain rules). Every number here is a placeholder.
    [CreateAssetMenu(fileName = "Banner", menuName = "Basket/Meta/Gacha Banner")]
    public class BannerDefinition : ScriptableObject
    {
        public string bannerId = "banner";
        public string displayName = "Placeholder";
        [TextArea] public string description;

        public ItemCost singleCost = new ItemCost("currency_gems", 160);
        public int multiCount = 10;
        public ItemCost multiCost = new ItemCost("currency_gems", 1600);
        [Tooltip("A multi-pull always holds at least one of this rarity or higher (empty = no guarantee).")]
        public string multiGuaranteeRarityId = "";

        public List<GachaRarity> rarities = new List<GachaRarity>();
        public List<GachaEntry> pool = new List<GachaEntry>();
        public GachaPity pity = new GachaPity();

        public string PityGroup => string.IsNullOrEmpty(pity.group) ? bannerId : pity.group;

        public int RarityIndex(string rarityId) => rarities.FindIndex(r => r.rarityId == rarityId);
    }
}
