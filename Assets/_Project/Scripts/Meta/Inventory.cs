using System;
using System.Collections.Generic;
using Basket.Characters;

namespace Basket.Meta
{
    // What the player owns: item stacks (currencies, materials, evolution and Limit Break
    // items) and characters (level, XP, Limit Break, dupes). Plain data + rules, no Unity:
    // the save system copies it in and out.
    public sealed class Inventory : IItemWallet
    {
        private readonly Dictionary<string, long> items = new Dictionary<string, long>();
        private readonly Dictionary<string, CharacterInstance> characters = new Dictionary<string, CharacterInstance>();
        private readonly Func<string, long> maxStack;

        public event Action<string> OnItemChanged;
        public event Action<string> OnCharacterChanged;

        // maxStack: per item cap (0 or less = none); null = no caps.
        public Inventory(Func<string, long> maxStack = null)
        {
            this.maxStack = maxStack;
        }

        public IEnumerable<KeyValuePair<string, long>> Items => items;
        public IEnumerable<CharacterInstance> Characters => characters.Values;

        public long Amount(string itemId) => itemId != null && items.TryGetValue(itemId, out long n) ? n : 0;

        // Adds (or removes, with a negative amount, never below 0). Returns the new amount.
        public long Add(string itemId, long amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount == 0) return Amount(itemId);
            long value = Math.Max(0, Amount(itemId) + amount);
            long cap = maxStack != null ? maxStack(itemId) : 0;
            if (cap > 0) value = Math.Min(value, cap);
            if (value == 0) items.Remove(itemId);
            else items[itemId] = value;
            OnItemChanged?.Invoke(itemId);
            return value;
        }

        public bool Has(string itemId, long amount) => Amount(itemId) >= amount;

        public bool Owns(string characterId) => characterId != null && characters.ContainsKey(characterId);

        public CharacterInstance GetCharacter(string characterId) =>
            characterId != null && characters.TryGetValue(characterId, out CharacterInstance c) ? c : null;

        public CharacterInstance AddCharacter(CharacterInstance instance)
        {
            characters[instance.characterId] = instance;
            OnCharacterChanged?.Invoke(instance.characterId);
            return instance;
        }

        public void NotifyCharacterChanged(string characterId) => OnCharacterChanged?.Invoke(characterId);

        public void Clear()
        {
            items.Clear();
            characters.Clear();
        }

        // IItemWallet (Limit Break costs).
        int IItemWallet.Count(string itemId) => (int)Math.Min(int.MaxValue, Amount(itemId));
        void IItemWallet.Spend(string itemId, int count) => Add(itemId, -count);
    }
}
