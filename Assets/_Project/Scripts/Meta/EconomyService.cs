using System;
using System.Collections.Generic;
using Basket.Characters;

namespace Basket.Meta
{
    // Every change to what the player owns goes through here (brief: economy separate from
    // the gacha and from rewards). A spend is all-or-nothing.
    public interface IEconomyService
    {
        bool CanAfford(IReadOnlyList<ItemCost> costs);
        // Spends every cost, or nothing if any is short.
        bool TrySpend(IReadOnlyList<ItemCost> costs, string reason);
        void Grant(IReadOnlyList<ItemCost> items, string reason);
        long Balance(string itemId);
        event Action<EconomyTransaction> OnTransaction;
    }

    public readonly struct EconomyTransaction
    {
        public readonly string ItemId;
        public readonly long Delta;
        public readonly string Reason;

        public EconomyTransaction(string itemId, long delta, string reason)
        {
            ItemId = itemId;
            Delta = delta;
            Reason = reason;
        }
    }

    public sealed class EconomyService : IEconomyService
    {
        private readonly Inventory inventory;

        public event Action<EconomyTransaction> OnTransaction;

        public EconomyService(Inventory inventory)
        {
            this.inventory = inventory;
        }

        public long Balance(string itemId) => inventory.Amount(itemId);

        public bool CanAfford(IReadOnlyList<ItemCost> costs)
        {
            // Sum per item: two costs of the same item must both be covered.
            var needed = new Dictionary<string, long>();
            foreach (ItemCost c in costs)
            {
                if (c.count <= 0) continue;
                needed.TryGetValue(c.itemId, out long n);
                needed[c.itemId] = n + c.count;
            }
            foreach (var pair in needed)
            {
                if (!inventory.Has(pair.Key, pair.Value)) return false;
            }
            return true;
        }

        public bool TrySpend(IReadOnlyList<ItemCost> costs, string reason)
        {
            if (!CanAfford(costs)) return false;
            foreach (ItemCost c in costs)
            {
                if (c.count <= 0) continue;
                inventory.Add(c.itemId, -c.count);
                OnTransaction?.Invoke(new EconomyTransaction(c.itemId, -c.count, reason));
            }
            return true;
        }

        public void Grant(IReadOnlyList<ItemCost> items, string reason)
        {
            foreach (ItemCost c in items)
            {
                if (c.count <= 0) continue;
                inventory.Add(c.itemId, c.count);
                OnTransaction?.Invoke(new EconomyTransaction(c.itemId, c.count, reason));
            }
        }
    }
}
