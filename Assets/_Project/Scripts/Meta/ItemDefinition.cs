using UnityEngine;

namespace Basket.Meta
{
    public enum ItemKind
    {
        Currency,
        Material,
        // Consumed to give a character XP.
        EvolutionItem,
        // Consumed by Limit Breaks (costs in ProgressionConfig reference these ids).
        LimitBreakItem,
    }

    // One kind of thing the player can hold in quantity (brief section 27). Names, values
    // and the set of currencies are placeholders: monetization is undecided (section 3).
    [CreateAssetMenu(fileName = "Item", menuName = "Basket/Meta/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string itemId = "item";
        public string displayName = "Placeholder";
        public ItemKind kind = ItemKind.Material;
        [TextArea] public string description;
        [Tooltip("XP given to a character per item (evolution items only).")]
        public int xpValue;
        [Tooltip("0 = no limit.")]
        public long maxStack;
    }
}
