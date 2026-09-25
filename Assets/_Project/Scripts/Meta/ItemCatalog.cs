using System.Collections.Generic;
using UnityEngine;

namespace Basket.Meta
{
    // Every item the game knows. Save data stores item ids; the catalog gives them meaning.
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Basket/Meta/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();

        public ItemDefinition Find(string itemId)
        {
            foreach (ItemDefinition item in items)
            {
                if (item != null && item.itemId == itemId) return item;
            }
            return null;
        }
    }
}
