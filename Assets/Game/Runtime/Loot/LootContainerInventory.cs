using System;
using System.Collections.Generic;
using Outbreak.Inventory;
using Outbreak.Raids;

namespace Outbreak.Loot
{
    public sealed class LootContainerInventory
    {
        private readonly List<ItemStack> contents = new List<ItemStack>();
        public bool Generated { get; private set; }
        public IReadOnlyList<ItemStack> Contents => contents.AsReadOnly();

        public void EnsureGenerated(Func<IEnumerable<ItemStack>> generator)
        {
            if (Generated) return;
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var generated = generator();
            if (generated == null) throw new InvalidOperationException("Loot generator returned null.");
            foreach (var stack in generated)
                if (stack == null) throw new InvalidOperationException("Loot generator returned an empty stack.");
                else contents.Add(stack);
            Generated = true;
        }
        public bool TryTake(int index, RaidSession session)
        {
            if (!Generated || session == null || !session.IsActive || index < 0 || index >= contents.Count) return false;
            if (!session.AddLoot(new[] { contents[index] })) return false;
            contents.RemoveAt(index); return true;
        }
        public int TryTakeAll(RaidSession session)
        {
            int taken = 0;
            for (int i = contents.Count - 1; i >= 0; i--)
                if (TryTake(i, session)) taken++;
            return taken;
        }
    }
}
