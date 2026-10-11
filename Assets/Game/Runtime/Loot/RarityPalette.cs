using Outbreak.Items;
using UnityEngine;

namespace Outbreak.Loot
{
    public static class RarityPalette
    {
        public static Color Color(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return new Color(0.48f, 0.78f, 0.48f);
                case ItemRarity.Rare: return new Color(0.42f, 0.65f, 0.92f);
                case ItemRarity.Epic: return new Color(0.72f, 0.48f, 0.9f);
                case ItemRarity.Legendary: return new Color(0.95f, 0.68f, 0.28f);
                case ItemRarity.Mythic: return new Color(0.94f, 0.36f, 0.44f);
                default: return new Color(0.82f, 0.82f, 0.82f);
            }
        }
    }
}
