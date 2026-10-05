using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Outbreak.Core;
using Outbreak.Economy;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Loot;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Quests;
using UnityEditor;
using UnityEngine;

namespace Outbreak.Tests
{
    public sealed class ArchitectureTests
    {
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private string directory;
        private ItemDefinition weapon, medical, valuable, attachment;
        private ItemCatalog catalog;
        private RaidMapDefinition map;
        private SaveService saves;
        private GameFlow game;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Application.temporaryCachePath, "OutbreakTests", Guid.NewGuid().ToString("N"));
            weapon = Item("weapon", ItemType.Weapon, 1);
            medical = Item("medical", ItemType.Medical, 5);
            valuable = Item("valuable", ItemType.Valuable, 10);
            attachment = Item("attachment", ItemType.Attachment, 1);
            catalog = Asset<ItemCatalog>(so =>
            {
                var list = so.FindProperty("items");
                var definitions = new[] { weapon, medical, valuable, attachment };
                list.arraySize = definitions.Length;
                for (int i = 0; i < definitions.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            });
            map = Asset<RaidMapDefinition>(so => so.FindProperty("id").stringValue = "map");
            saves = new SaveService(directory);
            game = new GameFlow(catalog, saves);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
            assets.Clear();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void InventorySplitsStacksAndRejectsPartialOperations()
        {
            var inventory = new BackpackInventory(2);
            Assert.IsTrue(inventory.AddItem(medical, 7));
            CollectionAssert.AreEqual(new[] { 5, 2 }, inventory.Slots.Select(x => x.Quantity));
            Assert.IsFalse(inventory.AddItem(medical, 4));
            Assert.IsFalse(inventory.RemoveItem(medical.Id, 8));
            Assert.AreEqual(7, inventory.GetItemCount(medical.Id));
            Assert.IsFalse(inventory.TryAddRange(new[] { new ItemStack(medical, 3), new ItemStack(weapon, 1) }));
            Assert.AreEqual(7, inventory.GetItemCount(medical.Id));
            Assert.IsTrue(inventory.RemoveItem(medical.Id, 6));
            Assert.AreEqual(1, inventory.GetItemCount(medical.Id));
            Assert.IsFalse(inventory.AddItem(medical, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemStack(medical, 6));
        }

        [Test]
        public void ExtractionReturnsSurvivingEquipmentAndLootExactlyOnce()
        {
            Deploy();
            Assert.IsTrue(game.Session.ConsumeEquipment(LoadoutSlot.Equipment1));
            Assert.IsTrue(game.Session.AddLoot(new[] { new ItemStack(valuable, 3) }));
            Assert.IsTrue(game.ExtractSuccessfully());
            Assert.AreEqual(GameState.RaidSuccess, game.State);
            Assert.AreEqual(1, game.Profile.Stash.GetItemCount(weapon.Id));
            Assert.AreEqual(2, game.Profile.Stash.GetItemCount(medical.Id));
            Assert.AreEqual(3, game.Profile.Stash.GetItemCount(valuable.Id));
            Assert.IsFalse(game.ExtractSuccessfully());
            Assert.IsTrue(game.ReturnToHub());
            Assert.IsTrue(game.Load());
            Assert.AreEqual(3, game.Profile.Stash.GetItemCount(valuable.Id));
        }

        [Test]
        public void DeathAndReloadLoseAllRaidItemsButKeepStash()
        {
            Deploy();
            var session = game.Session;
            Assert.IsTrue(session.AddLoot(new[] { new ItemStack(valuable, 2) }));
            Assert.IsTrue(game.FailRaid());
            Assert.IsFalse(session.IsActive);
            Assert.IsEmpty(session.FoundLoot);
            Assert.IsEmpty(session.Equipment.Entries);
            Assert.IsFalse(session.AddLoot(new[] { new ItemStack(valuable, 1) }));
            Assert.IsTrue(game.ReturnToHub());
            Assert.IsTrue(game.Load());
            Assert.AreEqual(0, game.Profile.Stash.GetItemCount(weapon.Id));
            Assert.AreEqual(0, game.Profile.Stash.GetItemCount(valuable.Id));
            Assert.AreEqual(1, game.Profile.Stash.GetItemCount(medical.Id));
        }

        [Test]
        public void ReloadAfterApplicationClosesDuringRaidCannotRestoreDeployedGear()
        {
            Deploy();
            Assert.IsFalse(game.Save());
            Assert.IsFalse(game.Load());
            var restarted = new GameFlow(catalog, saves);
            Assert.IsTrue(restarted.Load());
            Assert.IsFalse(restarted.Profile.Stash.HasItem(weapon.Id));
            Assert.IsEmpty(restarted.Profile.Loadout.Entries);
        }

        [Test]
        public void FullStashRejectsExtractionWithoutLosingSession()
        {
            var profile = new PlayerProfile(1);
            profile.Stash.AddItem(weapon);
            Assert.IsTrue(saves.TrySave(profile, out _));
            game.Load();
            Assert.IsTrue(game.Equip(LoadoutSlot.Primary, weapon.Id));
            Assert.IsTrue(game.StartRaid(map));
            game.Session.AddLoot(new[] { new ItemStack(valuable, 1) });
            Assert.IsFalse(game.ExtractSuccessfully());
            Assert.AreEqual(GameState.Raid, game.State);
            Assert.IsTrue(game.Session.IsActive);
            Assert.IsEmpty(game.Profile.Stash.Slots);
            Assert.IsTrue(game.Session.RemoveLoot(valuable.Id, 1));
            Assert.IsTrue(game.ExtractSuccessfully());
        }

        [Test]
        public void SaveFailureDoesNotDeployOrDiscardEquipment()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "blocked"), "file instead of directory");
            var broken = new GameFlow(catalog, new SaveService(Path.Combine(directory, "blocked")));
            broken.Profile.Stash.AddItem(weapon);
            broken.Equip(LoadoutSlot.Primary, weapon.Id);
            Assert.IsFalse(broken.StartRaid(map));
            Assert.AreEqual(GameState.Hub, broken.State);
            Assert.NotNull(broken.Profile.Loadout.Get(LoadoutSlot.Primary));
            Assert.IsNull(broken.Session);
        }

        [Test]
        public void ExtractionWriteFailureKeepsLootUntilRetry()
        {
            Deploy();
            game.Session.AddLoot(new[] { new ItemStack(valuable, 3) });
            Directory.CreateDirectory(saves.FilePath + ".tmp");
            Assert.IsFalse(game.ExtractSuccessfully());
            Assert.AreEqual(GameState.Raid, game.State);
            Assert.AreEqual(3, game.Session.FoundLoot.Sum(x => x.Quantity));
            Assert.IsFalse(game.Profile.Stash.HasItem(valuable.Id));
            Directory.Delete(saves.FilePath + ".tmp");
            Assert.IsTrue(game.ExtractSuccessfully());
            Assert.AreEqual(3, game.Profile.Stash.GetItemCount(valuable.Id));
        }

        [Test]
        public void UnequipIntoFullStashRetainsEquippedItem()
        {
            var profile = new PlayerProfile(1);
            profile.Stash.AddItem(weapon);
            saves.TrySave(profile, out _);
            game.Load();
            Assert.IsTrue(game.Equip(LoadoutSlot.Primary, weapon.Id));
            game.Profile.Stash.AddItem(valuable, 10);
            Assert.IsFalse(game.Unequip(LoadoutSlot.Primary));
            Assert.NotNull(game.Profile.Loadout.Get(LoadoutSlot.Primary));
            Assert.AreEqual(10, game.Profile.Stash.GetItemCount(valuable.Id));
        }

        [Test]
        public void ZeroRareMultiplierExcludesRareEntriesAndZeroWeightExcludesCommon()
        {
            Edit(valuable, so => so.FindProperty("rarity").enumValueIndex = (int)ItemRarity.Rare);
            var table = Asset<LootTable>(so =>
            {
                var entries = so.FindProperty("entries");
                entries.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("item").objectReferenceValue = i == 0 ? valuable : medical;
                    entry.FindPropertyRelative("weight").floatValue = 1;
                    entry.FindPropertyRelative("minQuantity").intValue = 1;
                    entry.FindPropertyRelative("maxQuantity").intValue = 1;
                }
            });
            Edit(map, so => so.FindProperty("rareLootMultiplier").floatValue = 0);
            var result = table.Generate(new System.Random(7), 20, map);
            Assert.AreEqual(20, result.Count);
            Assert.IsTrue(result.All(x => x.Item == medical));
            Edit(table, so => so.FindProperty("entries").GetArrayElementAtIndex(1).FindPropertyRelative("weight").floatValue = 0);
            Assert.IsEmpty(table.Generate(new System.Random(7), 20, map));
        }

        [Test]
        public void MissingCorruptAndInvalidSavesDoNotThrowOrOverwriteOriginal()
        {
            Assert.AreEqual(LoadResult.Missing, saves.Load(catalog, out _, out _));
            Directory.CreateDirectory(directory);
            foreach (string invalid in new[] { "{bad json", "{}", "null" })
            {
                File.WriteAllText(saves.FilePath, invalid);
                Assert.AreEqual(LoadResult.Invalid, saves.Load(catalog, out _, out _));
                Assert.AreEqual(invalid, File.ReadAllText(saves.FilePath));
                Assert.IsFalse(game.Load());
                Assert.AreEqual(1, game.Profile.Level);
            }
            var data = SaveMapper.Capture(new PlayerProfile());
            data.money = -1;
            File.WriteAllText(saves.FilePath, JsonUtility.ToJson(data));
            Assert.AreEqual(LoadResult.Invalid, saves.Load(catalog, out _, out _));
        }

        [Test]
        public void SaveRoundTripPreservesAllRequiredData()
        {
            game.Profile.Wallet.Credit(5000);
            game.Profile.AddXP(123);
            game.Profile.SetLevel(3);
            game.Profile.Stash.AddItem(weapon);
            game.Profile.Stash.AddItem(attachment);
            game.Equip(LoadoutSlot.Primary, weapon.Id);
            game.Profile.Quests.Add(new QuestProgress { questId = "quest", state = QuestState.Active, count = 2 });
            string before = JsonUtility.ToJson(SaveMapper.Capture(game.Profile));
            Assert.IsTrue(game.Save());
            Assert.IsTrue(game.NewGame());
            Assert.IsTrue(game.Load());
            Assert.AreEqual(before, JsonUtility.ToJson(SaveMapper.Capture(game.Profile)));
            Assert.AreEqual(1, SaveMapper.Capture(game.Profile).ownedWeapons.Single().quantity);
            Assert.AreEqual(1, SaveMapper.Capture(game.Profile).ownedAttachments.Single().quantity);
        }

        [Test]
        public void EconomyChecksFundsCapacityAndOverflowBeforeMutating()
        {
            var wallet = new Wallet(100);
            var inventory = new StashInventory(1);
            var economy = new EconomyService();
            Assert.IsFalse(economy.Buy(wallet, inventory, weapon, 2));
            Assert.AreEqual(100, wallet.Balance);
            Assert.IsTrue(economy.Buy(wallet, inventory, weapon, 1));
            Assert.AreEqual(0, wallet.Balance);
            wallet.Credit(100);
            Assert.IsFalse(economy.Buy(wallet, inventory, weapon, 1));
            Assert.AreEqual(100, wallet.Balance);
            Assert.IsTrue(economy.Sell(wallet, inventory, weapon, 1));
            Assert.AreEqual(150, wallet.Balance);
            inventory.AddItem(weapon);
            var fullWallet = new Wallet(int.MaxValue);
            Assert.IsFalse(economy.Sell(fullWallet, inventory, weapon, 1));
            Assert.IsTrue(inventory.HasItem(weapon.Id));
        }

        [Test]
        public void LoadoutRejectsWrongTypesAndDoesNotDuplicateItems()
        {
            game.Profile.Stash.AddItem(medical, 3);
            game.Profile.Stash.AddItem(weapon);
            Assert.IsFalse(game.Equip(LoadoutSlot.Primary, medical.Id));
            Assert.IsTrue(game.Equip(LoadoutSlot.Primary, weapon.Id));
            Assert.IsFalse(game.Equip(LoadoutSlot.Secondary, weapon.Id));
            Assert.IsTrue(game.Unequip(LoadoutSlot.Primary));
            Assert.AreEqual(1, game.Profile.Stash.GetItemCount(weapon.Id));
        }

        [Test]
        public void LootHonorsRarityQuantityMultiplierAndZeroChance()
        {
            var table = Asset<LootTable>(so =>
            {
                var entries = so.FindProperty("entries");
                entries.arraySize = 1;
                var entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("item").objectReferenceValue = valuable;
                entry.FindPropertyRelative("weight").floatValue = 1;
                entry.FindPropertyRelative("minQuantity").intValue = 7;
                entry.FindPropertyRelative("maxQuantity").intValue = 7;
            });
            Edit(map, so => so.FindProperty("lootMultiplier").floatValue = 2);
            var loot = table.Generate(new System.Random(1), 1, map);
            Assert.AreEqual(14, loot.Sum(x => x.Quantity));
            Assert.IsTrue(loot.All(x => x.Quantity <= valuable.MaxStack));
            Edit(table, so => { so.FindProperty("restrictRarity").boolValue = true; so.FindProperty("minimumRarity").enumValueIndex = (int)ItemRarity.Rare; });
            Assert.IsEmpty(table.Generate(new System.Random(1), 10, map));
            Edit(table, so => { so.FindProperty("restrictRarity").boolValue = false; so.FindProperty("dropChance").floatValue = 0; });
            Assert.IsEmpty(table.Generate(new System.Random(1), 10, map));
        }

        [Test]
        public void DuplicateCatalogIdsAndUnknownSavedItemsAreRejected()
        {
            Edit(attachment, so => so.FindProperty("id").stringValue = weapon.Id);
            Assert.Throws<ArgumentException>(() => catalog.Validate());
            var data = SaveMapper.Capture(new PlayerProfile());
            data.stash.Add(new StackData { itemId = "unknown", quantity = 1 });
            Assert.Throws<ArgumentException>(() => SaveMapper.Restore(data, catalog));
        }

        [Test]
        public void QuestProgressFollowsLifecycleAndClampsCount()
        {
            var definition = Asset<QuestDefinition>(so => { so.FindProperty("id").stringValue = "quest"; so.FindProperty("requiredCount").intValue = 3; });
            var progress = new QuestProgress { questId = definition.Id };
            Assert.IsFalse(progress.Activate());
            Assert.IsTrue(progress.Unlock());
            Assert.IsTrue(progress.Activate());
            Assert.IsTrue(progress.Advance(definition, 5));
            Assert.AreEqual(3, progress.count);
            Assert.AreEqual(QuestState.Completed, progress.state);
            Assert.IsTrue(progress.Claim());
            Assert.IsFalse(progress.Claim());
        }

        private void Deploy()
        {
            game.Profile.Stash.AddItem(weapon);
            game.Profile.Stash.AddItem(medical, 3);
            Assert.IsTrue(game.Equip(LoadoutSlot.Primary, weapon.Id));
            Assert.IsTrue(game.Equip(LoadoutSlot.Equipment1, medical.Id, 2));
            Assert.IsTrue(game.StartRaid(map));
        }
        private ItemDefinition Item(string id, ItemType type, int maxStack) => Asset<ItemDefinition>(so =>
        {
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("maxStack").intValue = maxStack;
            so.FindProperty("buyValue").intValue = 100;
            so.FindProperty("sellValue").intValue = 50;
        });
        private T Asset<T>(Action<SerializedObject> configure) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            assets.Add(asset);
            Edit(asset, configure);
            return asset;
        }
        private static void Edit(UnityEngine.Object asset, Action<SerializedObject> configure)
        {
            var so = new SerializedObject(asset);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
