using System;
using System.Collections.Generic;
using Outbreak.Core;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Quests;

namespace Outbreak.Persistence
{
    public static class SaveMapper
    {
        public static SaveData Capture(PlayerProfile profile)
        {
            var data = new SaveData
            {
                format = "OUTBREAK_EXTRACTION", version = 1,
                money = profile.Wallet.Balance, level = profile.Level, xp = profile.XP,
                stashCapacity = profile.Stash.Capacity,
                stash = new List<StackData>(), equipment = new List<EquipmentData>(),
                ownedWeapons = new List<StackData>(), ownedAttachments = new List<StackData>(),
                quests = new List<QuestProgress>()
            };
            foreach (var stack in profile.Stash.Slots)
            {
                data.stash.Add(ToData(stack));
                AddOwnership(data, stack);
            }
            foreach (var pair in profile.Loadout.Entries)
            {
                data.equipment.Add(new EquipmentData { slot = pair.Key, stack = ToData(pair.Value) });
                AddOwnership(data, pair.Value);
            }
            foreach (var quest in profile.Quests) data.quests.Add(quest.Copy());
            return data;
        }

        public static PlayerProfile Restore(SaveData data, ItemCatalog catalog)
        {
            if (data == null || data.format != "OUTBREAK_EXTRACTION" || data.version != 1 ||
                data.stash == null || data.equipment == null || data.quests == null ||
                data.ownedWeapons == null || data.ownedAttachments == null)
                throw new ArgumentException("Missing or unsupported save structure/version.");
            var profile = new PlayerProfile(data.stashCapacity, data.money, data.level, data.xp);
            if (data.stash.Count > data.stashCapacity) throw new ArgumentException("Too many saved slots.");
            foreach (var stack in data.stash)
            {
                var restored = RestoreStack(stack, catalog);
                if (!profile.Stash.AddItem(restored.Item, restored.Quantity)) throw new ArgumentException("Saved stash exceeds capacity.");
            }
            var slots = new HashSet<LoadoutSlot>();
            foreach (var entry in data.equipment)
            {
                if (entry == null || !slots.Add(entry.slot)) throw new ArgumentException("Invalid/duplicate equipment slot.");
                profile.Loadout.Set(entry.slot, RestoreStack(entry.stack, catalog));
            }
            var questIds = new HashSet<string>();
            foreach (var quest in data.quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.questId) || !questIds.Add(quest.questId) ||
                    quest.count < 0 || !Enum.IsDefined(typeof(QuestState), quest.state))
                    throw new ArgumentException("Invalid quest progress.");
                profile.Quests.Add(quest.Copy());
            }
            var expected = Capture(profile);
            ValidateOwnership(data.ownedWeapons, expected.ownedWeapons);
            ValidateOwnership(data.ownedAttachments, expected.ownedAttachments);
            return profile;
        }

        private static StackData ToData(ItemStack stack) => new StackData { itemId = stack.Item.Id, quantity = stack.Quantity };
        private static ItemStack RestoreStack(StackData stack, ItemCatalog catalog)
        {
            if (stack == null) throw new ArgumentException("Missing saved stack.");
            return new ItemStack(catalog.Get(stack.itemId), stack.quantity);
        }
        private static void AddOwnership(SaveData data, ItemStack stack)
        {
            var list = stack.Item.Type == ItemType.Weapon ? data.ownedWeapons :
                stack.Item.Type == ItemType.Attachment ? data.ownedAttachments : null;
            if (list == null) return;
            var existing = list.Find(x => x.itemId == stack.Item.Id);
            if (existing == null) list.Add(ToData(stack));
            else existing.quantity = checked(existing.quantity + stack.Quantity);
        }
        private static void ValidateOwnership(List<StackData> actual, List<StackData> expected)
        {
            if (actual.Count != expected.Count) throw new ArgumentException("Invalid ownership summary.");
            var ids = new HashSet<string>();
            foreach (var entry in actual)
                if (entry == null || !ids.Add(entry.itemId) || !expected.Exists(x => x.itemId == entry.itemId && x.quantity == entry.quantity))
                    throw new ArgumentException("Ownership does not match stored equipment.");
        }
    }
}
