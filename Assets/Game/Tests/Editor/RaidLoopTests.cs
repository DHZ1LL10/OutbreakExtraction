using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Outbreak.Core;
using Outbreak.Combat;
using Outbreak.Interaction;
using Outbreak.Infected;
using Outbreak.Player;
using Outbreak.Weapons;
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

        private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(target, args);
        private GameObject Go(string name) { var go = new GameObject(name); objects.Add(go); return go; }
        private (PlayerInput input, PlayerInteractor interactor, LootContainer container) Panel()
        {
            var player = Go("player"); var input = player.AddComponent<PlayerInput>(); var interactor = player.AddComponent<PlayerInteractor>();
            Invoke(interactor, "Awake");
            var container = Go("container").AddComponent<LootContainer>(); container.transform.position = Vector3.forward * 2;
            return (input, interactor, container);
        }
        [Test] public void LootPanelSuppressesMouseAndRestoresGameplayOnClose()
        {
            var p = Panel(); p.interactor.OpenPanel(p.container);
            Assert.IsTrue(p.input.GameplaySuppressed); Assert.IsFalse(p.input.HasControl);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState); Assert.IsTrue(Cursor.visible);
            p.input.CaptureCursor(); Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            p.interactor.ClosePanel(); Assert.IsFalse(p.input.GameplaySuppressed); Assert.IsNull(p.interactor.ActivePanel);
            Assert.IsFalse(p.input.FireHeld); Assert.AreEqual(Vector2.zero, p.input.LookInput);
        }
        [Test] public void SuppressionOwnersDoNotReleaseEachOthersModal()
        {
            var p = Panel(); object inventory = new object(); p.input.SetGameplaySuppressed(inventory, true);
            p.interactor.OpenPanel(p.container); p.interactor.ClosePanel(); Assert.IsTrue(p.input.GameplaySuppressed);
            p.input.SetGameplaySuppressed(inventory, false); Assert.IsFalse(p.input.GameplaySuppressed);
        }
        [Test] public void LootDistanceUsesHysteresisAndClosesWithoutAiming()
        {
            var p = Panel(); p.container.transform.position = Vector3.forward * 2.8f;
            Assert.IsFalse(p.container.CanOpen(p.interactor)); Assert.IsTrue(p.container.CanStayOpen(p.interactor));
            p.container.transform.position = Vector3.forward * 2; p.interactor.OpenPanel(p.container);
            p.interactor.transform.position = Vector3.back * 2; p.interactor.ValidatePanel();
            Assert.IsNull(p.interactor.ActivePanel); Assert.IsFalse(p.input.GameplaySuppressed);
        }
        [Test] public void DisabledAndDestroyedContainersCloseCleanly()
        {
            var p = Panel(); p.interactor.OpenPanel(p.container); p.container.enabled = false; p.interactor.ValidatePanel();
            Assert.IsNull(p.interactor.ActivePanel); p.container.enabled = true; p.interactor.OpenPanel(p.container);
            UnityEngine.Object.DestroyImmediate(p.container.gameObject); Assert.DoesNotThrow(p.interactor.ValidatePanel);
            Assert.IsNull(p.interactor.ActivePanel); Assert.IsFalse(p.input.GameplaySuppressed);
        }
        [Test] public void ClosingPanelCannotReenableDeadPlayersControls()
        {
            var p = Panel(); p.interactor.OpenPanel(p.container); p.input.SetControlEnabled(false); p.interactor.ValidatePanel();
            Assert.IsFalse(p.input.ControlEnabled); Assert.IsFalse(p.input.HasControl); Assert.IsNull(p.interactor.ActivePanel);
        }
        [Test] public void LeanEOnlyConflictsWithFocusedInteractionOrModal()
        {
            Assert.AreEqual(1, PlayerInput.ResolveLean(false, true, false, false));
            Assert.AreEqual(0, PlayerInput.ResolveLean(false, true, true, false));
            Assert.AreEqual(0, PlayerInput.ResolveLean(false, true, false, true));
            Assert.AreEqual(-1, PlayerInput.ResolveLean(true, false, true, false));
        }
        [Test] public void ExtractionAutomaticallyStartsCompletesAndCannotRepeat()
        {
            var timer = new ExtractionCountdown(5);
            Assert.IsFalse(timer.Tick(10, true, true, true)); Assert.IsTrue(timer.Running);
            Assert.IsFalse(timer.Tick(14.9, true, true, true)); Assert.IsTrue(timer.Tick(15, true, true, true));
            Assert.IsTrue(timer.Completed); Assert.IsFalse(timer.Tick(20, true, true, true));
            Assert.IsFalse(typeof(IInteractable).IsAssignableFrom(typeof(ExtractionZone)));
        }
        [TestCase(false, true, true)] [TestCase(true, false, true)] [TestCase(true, true, false)]
        public void ExtractionExitDeathAndInactiveRaidResetCountdown(bool inside, bool alive, bool active)
        {
            var timer = new ExtractionCountdown(5); timer.Tick(0, true, true, true);
            Assert.IsFalse(timer.Tick(4, inside, alive, active)); Assert.IsFalse(timer.Running); Assert.AreEqual(5, timer.Remaining(4));
            timer.Tick(6, true, true, true); Assert.IsFalse(timer.Tick(10, true, true, true)); Assert.IsTrue(timer.Tick(11, true, true, true));
        }
        [Test] public void AutomaticZoneTransfersLootWithoutInteractorOrKey()
        {
            var item = Item("auto", 5); var setup = Raid(4, item); setup.flow.Session.AddLoot(new[] { new ItemStack(item, 2) });
            var manager = Go("manager").AddComponent<GameManager>();
            typeof(GameManager).GetField("<Flow>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(manager, setup.flow);
            var context = Go("context").AddComponent<RaidRuntimeContext>(); Set(context, "gameManager", manager);
            var player = Go("player"); var health = player.AddComponent<PlayerHealth>(); var collider = player.AddComponent<CapsuleCollider>();
            var zoneObject = Go("zone"); zoneObject.AddComponent<BoxCollider>(); var zone = zoneObject.AddComponent<ExtractionZone>(); Set(zone, "raidContext", context);
            Invoke(zone, "Awake"); Invoke(zone, "OnTriggerEnter", collider); zone.Tick(0);
            Assert.IsTrue(zone.IsCountingDown); Assert.AreEqual(5, zone.Remaining); Assert.AreEqual(0, zone.Progress);
            zone.Tick(2.5); Assert.AreEqual(2.5f, zone.Remaining); Assert.AreEqual(0.5f, zone.Progress);
            Invoke(zone, "OnTriggerExit", collider); Assert.IsFalse(zone.IsCountingDown); Assert.AreEqual(5, zone.Remaining);
            Invoke(zone, "OnTriggerStay", collider); zone.Tick(3); Assert.AreEqual(5, zone.Remaining);
            zone.Tick(8);
            Assert.IsTrue(zone.Succeeded); Assert.AreEqual(GameState.RaidSuccess, setup.flow.State); Assert.AreEqual(2, setup.flow.Profile.Stash.GetItemCount(item.Id));
            zone.Tick(10); Assert.AreEqual(2, setup.flow.Profile.Stash.GetItemCount(item.Id));
        }
        [Test] public void TakeAllRejectsWholeStackWithoutPartialLossAndReopenDoesNotReroll()
        {
            var item = Item("atomic", 5); var setup = Raid(1, item); setup.flow.Session.AddLoot(new[] { new ItemStack(item, 4) });
            var state = new LootContainerInventory(); state.EnsureGenerated(() => new[] { new ItemStack(item, 2) });
            Assert.AreEqual(0, state.TryTakeAll(setup.flow.Session)); Assert.AreEqual(2, state.Contents[0].Quantity);
            state.EnsureGenerated(() => throw new Exception("Must not reroll"));
            setup.flow.Session.RemoveLoot(item.Id, 1); Assert.IsTrue(state.TryTake(0, setup.flow.Session)); Assert.IsEmpty(state.Contents);
            Assert.AreEqual(5, setup.flow.Session.FoundLoot.Sum(x => x.Quantity));
        }
        [TestCase(WanderProfile.Active)] [TestCase(WanderProfile.Passive)] [TestCase(WanderProfile.IdleProne)]
        public void WanderProfilesHaveValidRangesAndPreserveCombat(WanderProfile profile)
        {
            var settings = new InfectedSettings(); float speed = settings.chaseSpeed, damage = settings.attackDamage;
            settings.ApplyWanderProfile(profile);
            Assert.Greater(settings.idleMinimum, 0); Assert.GreaterOrEqual(settings.idleMaximum, settings.idleMinimum);
            Assert.Greater(settings.wanderRadius, 0); Assert.GreaterOrEqual(settings.wanderDelayMaximum, settings.wanderDelayMinimum);
            Assert.AreEqual(speed, settings.chaseSpeed); Assert.AreEqual(damage, settings.attackDamage);
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
