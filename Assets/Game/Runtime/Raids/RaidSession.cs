using System.Collections.Generic;
using Outbreak.Inventory;
using Outbreak.Loadouts;
using Outbreak.Maps;

namespace Outbreak.Raids
{
    public sealed class RaidSession
    {
        private readonly Loadout equipment;
        private readonly BackpackInventory foundLoot;
        public RaidMapDefinition Map { get; }
        public bool IsActive { get; private set; } = true;
        public Loadout Equipment => equipment.Copy();
        public IReadOnlyList<ItemStack> FoundLoot => foundLoot.Slots;
        public int LootCapacity => foundLoot.Capacity;

        internal RaidSession(Loadout hubLoadout, RaidMapDefinition map, int backpackCapacity)
        {
            Map = map;
            equipment = hubLoadout.Copy();
            foundLoot = new BackpackInventory(backpackCapacity);
        }
        public bool AddLoot(IEnumerable<ItemStack> loot) => IsActive && foundLoot.TryAddRange(loot);
        public bool RemoveLoot(string id, int quantity) => IsActive && foundLoot.RemoveItem(id, quantity);
        public bool ConsumeEquipment(LoadoutSlot slot, int quantity = 1)
        {
            var stack = equipment.Get(slot);
            if (!IsActive || stack == null || quantity <= 0 || quantity > stack.Quantity) return false;
            equipment.Set(slot, quantity == stack.Quantity ? null : new ItemStack(stack.Item, stack.Quantity - quantity));
            return true;
        }
        internal IEnumerable<ItemStack> Survivors()
        {
            foreach (var entry in equipment.Entries) yield return entry.Value;
            foreach (var stack in foundLoot.Slots) yield return stack;
        }
        internal void Close()
        {
            IsActive = false;
            equipment.Clear();
            foundLoot.Clear();
        }
    }
}
