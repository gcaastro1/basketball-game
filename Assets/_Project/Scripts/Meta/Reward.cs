using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Characters;

namespace Basket.Meta
{
    // A bundle given to the player (match end, story chapter, event...). Data only.
    [Serializable]
    public class Reward
    {
        public List<ItemCost> items = new List<ItemCost>();
        public List<CharacterDefinition> characters = new List<CharacterDefinition>();
        [Tooltip("XP for each character that took part (match rewards).")]
        public int characterXp;
    }

    public static class RewardGranter
    {
        // Items through the economy, characters through the obtain rules, XP to the given
        // characters. Returns what each character grant turned into.
        public static List<ObtainResult> Grant(Reward reward, Inventory inventory, IEconomyService economy,
            ProgressionConfig progression, CharacterObtainRules rules, string reason, IEnumerable<string> xpTo = null)
        {
            var results = new List<ObtainResult>();
            if (reward == null) return results;
            economy.Grant(reward.items, reason);
            foreach (CharacterDefinition c in reward.characters)
            {
                if (c != null) results.Add(CharacterObtainer.Obtain(c.characterId, inventory, economy, progression, rules, reason));
            }
            if (reward.characterXp > 0 && xpTo != null && progression != null)
            {
                foreach (string id in xpTo)
                {
                    CharacterInstance instance = inventory.GetCharacter(id);
                    if (instance == null) continue;
                    CharacterProgression.AddXp(instance, progression, reward.characterXp);
                    inventory.NotifyCharacterChanged(id);
                }
            }
            return results;
        }
    }
}
