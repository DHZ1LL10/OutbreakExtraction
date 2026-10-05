using System;
using System.Collections.Generic;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Maps;
using UnityEngine;

namespace Outbreak.Loot
{
    [Serializable]
    public sealed class LootEntry
    {
        public ItemDefinition item;
        [Min(0)] public float weight = 1;
        [Min(1)] public int minQuantity = 1;
        [Min(1)] public int maxQuantity = 1;
    }

    [CreateAssetMenu(menuName = "Outbreak/Loot/Loot Table")]
    public sealed class LootTable : ScriptableObject
    {
        [SerializeField] private List<LootEntry> entries = new List<LootEntry>();
        [SerializeField, Range(0, 1)] private float dropChance = 1;
        [SerializeField] private bool restrictRarity;
        [SerializeField] private ItemRarity minimumRarity;
        [SerializeField] private ItemRarity maximumRarity = ItemRarity.Mythic;

        // Each roll first checks dropChance, then picks one eligible entry by weight.
        public List<ItemStack> Generate(System.Random random, int rolls = 1, RaidMapDefinition map = null)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (rolls < 0 || rolls > 10000) throw new ArgumentOutOfRangeException(nameof(rolls));
            if (map != null) map.Validate();
            Validate();
            var eligible = new List<LootEntry>();
            var weights = new List<double>();
            double total = 0;
            foreach (var entry in entries)
            {
                if (restrictRarity && (entry.item.Rarity < minimumRarity || entry.item.Rarity > maximumRarity)) continue;
                double weight = entry.weight * (entry.item.Rarity >= ItemRarity.Rare && map != null ? (double)map.RareLootMultiplier : 1);
                if (weight <= 0) continue;
                eligible.Add(entry);
                weights.Add(weight);
                total += weight;
            }
            var result = new List<ItemStack>();
            for (int roll = 0; roll < rolls && total > 0; roll++)
            {
                if (random.NextDouble() >= dropChance) continue;
                double pick = random.NextDouble() * total;
                int index = 0;
                while (index < weights.Count - 1 && pick >= weights[index]) pick -= weights[index++];
                var entry = eligible[index];
                int baseCount = (int)(entry.minQuantity + Math.Floor(random.NextDouble() * ((long)entry.maxQuantity - entry.minQuantity + 1)));
                double scaled = baseCount * (map != null ? (double)map.LootMultiplier : 1);
                if (scaled > 100000) throw new InvalidOperationException("Loot quantity exceeds safety limit (100000 per roll).");
                int quantity = (int)Math.Floor(scaled);
                if (random.NextDouble() < scaled - quantity) quantity++;
                while (quantity > 0)
                {
                    int count = Math.Min(quantity, entry.item.MaxStack);
                    result.Add(new ItemStack(entry.item, count));
                    quantity -= count;
                }
            }
            return result;
        }

        public void Validate()
        {
            if (float.IsNaN(dropChance) || dropChance < 0 || dropChance > 1 || minimumRarity > maximumRarity)
                throw new ArgumentException("Invalid loot table settings.");
            foreach (var entry in entries)
            {
                if (entry == null || entry.item == null || float.IsNaN(entry.weight) || float.IsInfinity(entry.weight) ||
                    entry.weight < 0 || entry.minQuantity < 1 || entry.maxQuantity < entry.minQuantity || entry.maxQuantity > 100000)
                    throw new ArgumentException("Invalid loot entry.");
                entry.item.Validate();
            }
        }
    }
}
