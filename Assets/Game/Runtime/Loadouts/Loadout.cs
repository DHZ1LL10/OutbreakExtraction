using System;
using System.Collections.Generic;
using Outbreak.Inventory;
using Outbreak.Items;

namespace Outbreak.Loadouts
{
    public enum LoadoutSlot { Primary, Secondary, Armor, Backpack, Rig, Equipment1, Equipment2, Equipment3, Equipment4 }

    public sealed class Loadout
    {
        private readonly Dictionary<LoadoutSlot, ItemStack> equipment = new Dictionary<LoadoutSlot, ItemStack>();
        public ItemStack Get(LoadoutSlot slot) => equipment.TryGetValue(slot, out var stack) ? stack : null;
        public IEnumerable<KeyValuePair<LoadoutSlot, ItemStack>> Entries
        {
            get { foreach (var pair in equipment) yield return pair; }
        }

        public static bool Accepts(LoadoutSlot slot, ItemDefinition item)
        {
            if (item == null) return false;
            switch (slot)
            {
                case LoadoutSlot.Primary: case LoadoutSlot.Secondary: return item.Type == ItemType.Weapon;
                case LoadoutSlot.Armor: return item.Type == ItemType.Armor;
                case LoadoutSlot.Backpack: return item.Type == ItemType.Backpack;
                case LoadoutSlot.Rig: return item.Type == ItemType.Rig;
                case LoadoutSlot.Equipment1: case LoadoutSlot.Equipment2:
                case LoadoutSlot.Equipment3: case LoadoutSlot.Equipment4:
                    return item.Type == ItemType.Medical || item.Type == ItemType.Ammo || item.Type == ItemType.Attachment;
                default: return false;
            }
        }

        internal void Set(LoadoutSlot slot, ItemStack stack)
        {
            if (stack == null) { equipment.Remove(slot); return; }
            if (!Accepts(slot, stack.Item) || ((int)slot < (int)LoadoutSlot.Equipment1 && stack.Quantity != 1))
                throw new ArgumentException("Item or quantity incompatible with loadout slot.");
            equipment[slot] = stack;
        }

        public Loadout Copy()
        {
            var copy = new Loadout();
            foreach (var pair in equipment) copy.Set(pair.Key, pair.Value);
            return copy;
        }

        internal void Clear() => equipment.Clear();
    }
}
