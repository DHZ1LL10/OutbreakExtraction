using System;
using System.Collections.Generic;
using UnityEngine;

namespace Outbreak.Items
{
    [CreateAssetMenu(menuName = "Outbreak/Items/Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
        public IReadOnlyList<ItemDefinition> Items => items.AsReadOnly();

        public void Validate()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (item == null) throw new ArgumentException("Catalog contains a missing item.");
                item.Validate();
                if (!ids.Add(item.Id)) throw new ArgumentException($"Duplicate item ID: {item.Id}");
            }
        }

        public ItemDefinition Get(string id)
        {
            foreach (var item in items)
                if (item != null && item.Id == id) return item;
            throw new ArgumentException($"Unknown item ID: {id}");
        }
    }
}
