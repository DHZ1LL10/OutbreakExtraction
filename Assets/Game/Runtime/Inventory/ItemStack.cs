using System;
using Outbreak.Items;

namespace Outbreak.Inventory
{
    public sealed class ItemStack
    {
        public ItemDefinition Item { get; }
        public int Quantity { get; }
        public ItemStack(ItemDefinition item, int quantity)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.Validate();
            if (quantity < 1 || quantity > item.MaxStack) throw new ArgumentOutOfRangeException(nameof(quantity));
            Item = item;
            Quantity = quantity;
        }
    }
}
