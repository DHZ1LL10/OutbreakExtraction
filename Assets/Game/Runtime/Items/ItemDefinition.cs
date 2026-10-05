using System;
using UnityEngine;

namespace Outbreak.Items
{
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
    public enum ItemType { Weapon, Ammo, Attachment, Medical, Valuable, QuestItem, Armor, Backpack, Rig }

    [CreateAssetMenu(menuName = "Outbreak/Items/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ItemType type;
        [SerializeField] private ItemRarity rarity;
        [SerializeField, Min(0)] private int buyValue;
        [SerializeField, Min(0)] private int sellValue;
        [SerializeField, Min(1)] private int maxStack = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemType Type => type;
        public ItemRarity Rarity => rarity;
        public int BuyValue => buyValue;
        public int SellValue => sellValue;
        public int MaxStack => maxStack;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName) ||
                maxStack < 1 || buyValue < 0 || sellValue < 0 ||
                !Enum.IsDefined(typeof(ItemType), type) || !Enum.IsDefined(typeof(ItemRarity), rarity))
                throw new ArgumentException($"Invalid item definition: {name}");
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");
        }
    }
}
