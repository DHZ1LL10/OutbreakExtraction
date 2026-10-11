using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Outbreak.Core;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loot;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Raids;
using UnityEditor;
using UnityEngine;

namespace Outbreak.Tests
{
    public sealed class RaidLoopTests
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        private readonly List<string> directories = new List<string>();
        [TearDown] public void TearDown()
        {
            foreach (var value in objects) if (value != null) UnityEngine.Object.DestroyImmediate(value);
            foreach (var directory in directories) if (Directory.Exists(directory)) Directory.Delete(directory, true);
            objects.Clear(); directories.Clear();
        }

        [Test] public void ContainerGeneratesOnlyOnceAndTakeItemPersists()
        {
            var item = Item("scrap", 1); var state = new LootContainerInventory(); int calls = 0;
            state.EnsureGenerated(() => { calls++; return new[] { new ItemStack(item, 1) }; });
            state.EnsureGenerated(() => { calls++; return new[] { new ItemStack(item, 1) }; });
            var setup = Raid(4, item);
            Assert.AreEqual(1, calls); Assert.AreEqual(1, state.Contents.Count);
            Assert.IsTrue(state.TryTake(0, setup.flow.Session)); Assert.IsEmpty(state.Contents);
            Assert.AreEqual(1, setup.flow.Session.FoundLoot.Sum(x => x.Quantity));
        }

        [Test] public void TakeAllTakesWhatFitsAndLeavesRejectedLoot()
        {
            var one = Item("one", 1); var two = Item("two", 1); var state = new LootContainerInventory();
            state.EnsureGenerated(() => new[] { new ItemStack(one, 1), new ItemStack(two, 1) });
            var setup = Raid(1, one, two);
            Assert.AreEqual(1, state.TryTakeAll(setup.flow.Session));
            Assert.AreEqual(1, setup.flow.Session.FoundLoot.Count); Assert.AreEqual(1, state.Contents.Count);
        }

        [Test] public void FullBackpackRejectsPickupAndSuccessfulPickupDisappears()
        {
            var full = Item("full", 1); var pickupItem = Item("pickup", 1); var setup = Raid(1, full, pickupItem);
            Assert.IsTrue(setup.flow.Session.AddLoot(new[] { new ItemStack(full, 1) }));
            var go = new GameObject("pickup"); objects.Add(go); var pickup = go.AddComponent<WorldItem>(); Set(pickup, "item", pickupItem); Set(pickup, "quantity", 1);
            Assert.IsFalse(pickup.TryPickup(setup.flow.Session)); Assert.IsTrue(go.activeSelf);
            Assert.IsTrue(setup.flow.Session.RemoveLoot(full.Id, 1));
            Assert.IsTrue(pickup.TryPickup(setup.flow.Session)); Assert.IsFalse(go.activeSelf);
        }

        [Test] public void LootTableHonorsQuantityAndRareMultipliers()
        {
            var common = Item("common", 10, ItemRarity.Common); var rare = Item("rare", 10, ItemRarity.Rare);
            var quantityTable = Table((common, 1, 2, 2)); var doubled = Map(2, 1);
            Assert.AreEqual(4, quantityTable.Generate(new System.Random(7), 1, doubled).Sum(x => x.Quantity));
            var rarityTable = Table((common, 1, 1, 1), (rare, 100, 1, 1)); var noRare = Map(1, 0);
            var generated = rarityTable.Generate(new System.Random(9), 30, noRare);
            Assert.IsTrue(generated.All(x => x.Item == common));
        }

        [Test] public void DiscardRemovesLootFromActiveRaid()
        {
            var item = Item("discard", 5); var setup = Raid(3, item);
            setup.flow.Session.AddLoot(new[] { new ItemStack(item, 3) });
            Assert.IsTrue(setup.flow.Session.RemoveLoot(item.Id, 2));
            Assert.AreEqual(1, setup.flow.Session.FoundLoot.Sum(x => x.Quantity));
        }

        [Test] public void ExtractionRequiresActiveSessionAndTransfersExactlyOnce()
        {
            var item = Item("extract", 5); var setup = Flow(item);
            Assert.IsFalse(setup.flow.ExtractSuccessfully());
            Assert.IsTrue(setup.flow.StartRaid(setup.map, 4)); setup.flow.Session.AddLoot(new[] { new ItemStack(item, 3) });
            Assert.IsTrue(setup.flow.ExtractSuccessfully()); Assert.AreEqual(3, setup.flow.Profile.Stash.GetItemCount(item.Id));
            Assert.IsFalse(setup.flow.ExtractSuccessfully()); Assert.AreEqual(3, setup.flow.Profile.Stash.GetItemCount(item.Id));
        }

        [Test] public void DeathAndExtractionAreMutuallyExclusiveAndDeathLosesLoot()
        {
            var item = Item("lost", 5); var first = Raid(4, item); first.flow.Session.AddLoot(new[] { new ItemStack(item, 2) });
            Assert.IsTrue(first.flow.FailRaid()); Assert.IsFalse(first.flow.ExtractSuccessfully()); Assert.AreEqual(0, first.flow.Profile.Stash.GetItemCount(item.Id));
            var second = Raid(4, item); second.flow.Session.AddLoot(new[] { new ItemStack(item, 2) });
            Assert.IsTrue(second.flow.ExtractSuccessfully()); Assert.IsFalse(second.flow.FailRaid()); Assert.AreEqual(2, second.flow.Profile.Stash.GetItemCount(item.Id));
        }

        [Test] public void SaveLoadPreservesExtractedStash()
        {
            var item = Item("persistent", 5); var setup = Raid(4, item); setup.flow.Session.AddLoot(new[] { new ItemStack(item, 4) });
            Assert.IsTrue(setup.flow.ExtractSuccessfully()); Assert.IsTrue(setup.flow.ReturnToHub());
            var loaded = new GameFlow(setup.catalog, setup.saves); Assert.IsTrue(loaded.Load());
            Assert.AreEqual(4, loaded.Profile.Stash.GetItemCount(item.Id));
        }

        private (GameFlow flow, RaidMapDefinition map, ItemCatalog catalog, SaveService saves) Raid(int capacity, params ItemDefinition[] items)
        {
            var setup = Flow(items); Assert.IsTrue(setup.flow.StartRaid(setup.map, capacity)); return setup;
        }
        private (GameFlow flow, RaidMapDefinition map, ItemCatalog catalog, SaveService saves) Flow(params ItemDefinition[] items)
        {
            var catalog = Asset<ItemCatalog>(); var so = new SerializedObject(catalog); var list = so.FindProperty("items"); list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i]; so.ApplyModifiedPropertiesWithoutUndo();
            string directory = Path.Combine(Path.GetTempPath(), "OutbreakRaidTests", Guid.NewGuid().ToString("N")); directories.Add(directory);
            var saves = new SaveService(directory, "profile.json"); return (new GameFlow(catalog, saves), Map(1, 1), catalog, saves);
        }
        private ItemDefinition Item(string id, int maxStack, ItemRarity rarity = ItemRarity.Common)
        {
            var item = Asset<ItemDefinition>(); var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id; so.FindProperty("displayName").stringValue = id;
            so.FindProperty("type").enumValueIndex = (int)ItemType.Valuable; so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("sellValue").intValue = 10; so.FindProperty("maxStack").intValue = maxStack; so.ApplyModifiedPropertiesWithoutUndo(); return item;
        }
        private RaidMapDefinition Map(float loot, float rare)
        {
            var map = Asset<RaidMapDefinition>(); var so = new SerializedObject(map);
            so.FindProperty("id").stringValue = Guid.NewGuid().ToString("N"); so.FindProperty("displayName").stringValue = "Test";
            so.FindProperty("lootMultiplier").floatValue = loot; so.FindProperty("rareLootMultiplier").floatValue = rare;
            so.FindProperty("enemyDensityMultiplier").floatValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); return map;
        }
        private LootTable Table(params (ItemDefinition item, float weight, int min, int max)[] values)
        {
            var table = Asset<LootTable>(); var so = new SerializedObject(table); var entries = so.FindProperty("entries"); entries.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i); entry.FindPropertyRelative("item").objectReferenceValue = values[i].item;
                entry.FindPropertyRelative("weight").floatValue = values[i].weight; entry.FindPropertyRelative("minQuantity").intValue = values[i].min;
                entry.FindPropertyRelative("maxQuantity").intValue = values[i].max;
            }
            so.FindProperty("dropChance").floatValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); return table;
        }
        private T Asset<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(UnityEngine.Object target, string field, int value)
        { var so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
