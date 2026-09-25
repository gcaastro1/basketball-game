using System.Collections.Generic;
using UnityEngine;
using Basket.Characters;

namespace Basket.Meta
{
    // What getting a character does -- the "rules of obtainment" the brief keeps separate
    // from the gacha: new -> owned at level 1; owned -> one dupe (up to ProgressionConfig's
    // 6); owned with every dupe -> converted into items. Values are placeholders.
    [CreateAssetMenu(fileName = "CharacterObtainRules", menuName = "Basket/Meta/Character Obtain Rules")]
    public class CharacterObtainRules : ScriptableObject
    {
        [Tooltip("Given instead of a dupe the character can no longer take.")]
        public List<ItemCost> maxedDupeConversion = new List<ItemCost> { new ItemCost("dupe_overflow_token", 1) };
    }

    public enum ObtainOutcome { New, Dupe, Converted }

    public readonly struct ObtainResult
    {
        public readonly ObtainOutcome Outcome;
        public readonly CharacterInstance Instance;

        public ObtainResult(ObtainOutcome outcome, CharacterInstance instance)
        {
            Outcome = outcome;
            Instance = instance;
        }
    }

    public static class CharacterObtainer
    {
        public static ObtainResult Obtain(string characterId, Inventory inventory, IEconomyService economy,
            ProgressionConfig progression, CharacterObtainRules rules, string reason)
        {
            CharacterInstance owned = inventory.GetCharacter(characterId);
            if (owned == null)
            {
                int start = progression != null ? Mathf.Max(1, progression.startingLevel) : 1;
                return new ObtainResult(ObtainOutcome.New, inventory.AddCharacter(new CharacterInstance(characterId, start)));
            }
            if (progression != null && CharacterProgression.AddDupe(owned, progression))
            {
                inventory.NotifyCharacterChanged(characterId);
                return new ObtainResult(ObtainOutcome.Dupe, owned);
            }
            if (rules != null) economy.Grant(rules.maxedDupeConversion, reason + ":maxed_dupe");
            return new ObtainResult(ObtainOutcome.Converted, owned);
        }
    }
}
