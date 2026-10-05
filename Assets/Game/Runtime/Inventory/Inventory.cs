using System;
using System.Collections.Generic;
using Outbreak.Items;

namespace Outbreak.Inventory
{
    public class Inventory
    {
        private List<ItemStack> slots = new List<ItemStack>();
        public int Capacity { get; }
        public IReadOnlyList<ItemStack> Slots => slots.AsReadOnly();

        public Inventory(int capacity)
        {
            if (capacity < 1 || capacity > 10000) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        public int GetItemCount(string id)
        {
            int count = 0;
            foreach (var stack in slots)
                if (stack.Item.Id == id) count = checked(count + stack.Quantity);
            return count;
        }

        public bool HasItem(string id, int quantity = 1) => quantity > 0 && GetItemCount(id) >= quantity;

        public bool AddItem(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            item.Validate();
            long free = (long)(Capacity - slots.Count) * item.MaxStack;
            foreach (var stack in slots)
                if (stack.Item.Id == item.Id) free += stack.Item.MaxStack - stack.Quantity;
            if (free < quantity || (long)GetItemCount(item.Id) + quantity > int.MaxValue) return false;
            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                var stack = slots[i];
                if (stack.Item.Id != item.Id) continue;
                int added = Math.Min(quantity, stack.Item.MaxStack - stack.Quantity);
                slots[i] = new ItemStack(stack.Item, stack.Quantity + added);
                quantity -= added;
            }
            while (quantity > 0)
            {
                int added = Math.Min(quantity, item.MaxStack);
                slots.Add(new ItemStack(item, added));
                quantity -= added;
            }
            return true;
        }

        public bool RemoveItem(string id, int quantity = 1)
        {
            if (!HasItem(id, quantity)) return false;
            for (int i = slots.Count - 1; i >= 0 && quantity > 0; i--)
            {
                var stack = slots[i];
                if (stack.Item.Id != id) continue;
                int removed = Math.Min(quantity, stack.Quantity);
                quantity -= removed;
                if (removed == stack.Quantity) slots.RemoveAt(i);
                else slots[i] = new ItemStack(stack.Item, stack.Quantity - removed);
            }
            return true;
        }

        // All-or-nothing: a full stash must never discard part of an extraction.
        public bool TryAddRange(IEnumerable<ItemStack> stacks)
        {
            var candidate = Copy();
            foreach (var stack in stacks)
                if (stack == null || !candidate.AddItem(stack.Item, stack.Quantity)) return false;
            slots = candidate.slots;
            return true;
        }

        public Inventory Copy()
        {
            var copy = new Inventory(Capacity);
            copy.slots.AddRange(slots);
            return copy;
        }

        internal void Clear() => slots.Clear();
    }
}
