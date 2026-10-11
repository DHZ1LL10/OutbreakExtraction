using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Outbreak.Core;
using Outbreak.Hub;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Weapons;
using UnityEditor;
using UnityEngine;

namespace Outbreak.Tests
{
    public sealed class HubLoopTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private string directory;
        private ItemCatalog catalog;
        private GameFlow flow;
        private SaveService saves;
        private ItemDefinition trade;
        private WeaponDefinition rifle, pistol;
        private AttachmentDefinition[] attachments;
        private RaidMapDefinition map;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "OutbreakHubTests", Guid.NewGuid().ToString("N"));
            saves = new SaveService(directory);
            rifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugRifle.asset");
            pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugPistol.asset");
            attachments = AssetDatabase.FindAssets("t:AttachmentDefinition", new[] { "Assets/Game/DebugContent/Gunplay/Attachments" }).Select(g => AssetDatabase.LoadAssetAtPath<AttachmentDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            map = AssetDatabase.LoadAssetAtPath<RaidMapDefinition>("Assets/Game/DebugContent/RaidLoop/RaidLoopMap.asset");
            trade = ScriptableObject.CreateInstance<ItemDefinition>(); created.Add(trade); var item = new SerializedObject(trade);
            item.FindProperty("id").stringValue = "hub_test_trade"; item.FindProperty("displayName").stringValue = "trade";
            item.FindProperty("type").enumValueIndex = (int)ItemType.Medical; item.FindProperty("buyValue").intValue = 10; item.FindProperty("sellValue").intValue = 4;
            item.FindProperty("maxStack").intValue = 5; item.ApplyModifiedPropertiesWithoutUndo();
            catalog = ScriptableObject.CreateInstance<ItemCatalog>(); created.Add(catalog);
            var definitions = new[] { rifle.item, pistol.item, rifle.ammoType, pistol.ammoType, trade }.Concat(attachments.Select(a => a.item)).ToArray();
            var so = new SerializedObject(catalog); var array = so.FindProperty("items"); array.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i]; so.ApplyModifiedPropertiesWithoutUndo();
            flow = new GameFlow(catalog, saves); flow.Profile.Wallet.Credit(100);
            flow.Profile.Stash.AddItem(rifle.item); flow.Profile.Stash.AddItem(pistol.item);
        }
        [TearDown] public void Cleanup()
        {
            foreach (var asset in created) UnityEngine.Object.DestroyImmediate(asset); created.Clear();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        [Test] public void EquipUnequipArePhysicalTransfersAndPersist()
        {
            Assert.IsTrue(flow.CommitHubChange(f => f.Equip(LoadoutSlot.Primary, rifle.WeaponId)));
            Assert.AreEqual(0, flow.Profile.Stash.GetItemCount(rifle.WeaponId)); Assert.AreEqual(rifle.WeaponId, flow.Profile.Loadout.Get(LoadoutSlot.Primary).Item.Id);
            Assert.IsTrue(flow.CommitHubChange(f => f.Unequip(LoadoutSlot.Primary)));
            Assert.IsNull(flow.Profile.Loadout.Get(LoadoutSlot.Primary)); Assert.AreEqual(1, flow.Profile.Stash.GetItemCount(rifle.WeaponId));
            var loaded = new GameFlow(catalog, saves); Assert.IsTrue(loaded.Load()); Assert.AreEqual(1, loaded.Profile.Stash.GetItemCount(rifle.WeaponId));
        }
        [Test] public void BuySellPersistExactAmountsAndInsufficientFundsReject()
        {
            Assert.IsTrue(flow.CommitHubChange(f => f.Buy(trade.Id, 2))); Assert.AreEqual(80, flow.Profile.Wallet.Balance);
            Assert.IsTrue(flow.CommitHubChange(f => f.Sell(trade.Id, 1))); Assert.AreEqual(84, flow.Profile.Wallet.Balance); Assert.AreEqual(1, flow.Profile.Stash.GetItemCount(trade.Id));
            Assert.IsFalse(flow.CommitHubChange(f => f.Buy(trade.Id, 9))); Assert.AreEqual(84, flow.Profile.Wallet.Balance); Assert.AreEqual(1, flow.Profile.Stash.GetItemCount(trade.Id));
            var loaded = new GameFlow(catalog, saves); Assert.IsTrue(loaded.Load()); Assert.AreEqual(84, loaded.Profile.Wallet.Balance);
        }
        [Test] public void FullStashBuyCannotChargeWallet()
        {
            while (flow.Profile.Stash.Slots.Count < flow.Profile.Stash.Capacity) Assert.IsTrue(flow.Profile.Stash.AddItem(rifle.item));
            Assert.IsFalse(flow.CommitHubChange(f => f.Buy(trade.Id, 1))); Assert.AreEqual(100, flow.Profile.Wallet.Balance); Assert.AreEqual(0, flow.Profile.Stash.GetItemCount(trade.Id));
        }
        [Test] public void FailedSaveRollsBackHubTransaction()
        {
            Directory.CreateDirectory(directory); string blocked = Path.Combine(directory, "not_a_directory"); File.WriteAllText(blocked, "block");
            var failed = new GameFlow(catalog, new SaveService(blocked)); failed.Profile.Wallet.Credit(100);
            Assert.IsFalse(failed.CommitHubChange(f => f.Buy(trade.Id, 1))); Assert.AreEqual(100, failed.Profile.Wallet.Balance); Assert.AreEqual(0, failed.Profile.Stash.GetItemCount(trade.Id));
        }
        [Test] public void DeployUsesOnlyPreparedEquipmentAndRejectsEmptyLoadout()
        {
            Assert.IsFalse(HubLoadoutAdapter.CanDeploy(flow, new[] { rifle, pistol }, attachments));
            Assert.IsTrue(flow.CommitHubChange(f => f.Equip(LoadoutSlot.Primary, rifle.WeaponId)));
            Assert.IsTrue(HubLoadoutAdapter.CanDeploy(flow, new[] { rifle, pistol }, attachments)); Assert.IsTrue(flow.StartRaid(map));
            Assert.AreEqual(rifle.WeaponId, flow.Session.Equipment.Get(LoadoutSlot.Primary).Item.Id);
            Assert.IsNull(flow.Session.Equipment.Get(LoadoutSlot.Secondary)); Assert.IsNull(flow.Profile.Loadout.Get(LoadoutSlot.Primary));
        }
        [TestCase(true)] [TestCase(false)]
        public void ReturnPreservesStashAndMoneyButOnlySuccessKeepsDeployedItemsAndLoot(bool success)
        {
            flow.Profile.Stash.AddItem(trade); flow.Equip(LoadoutSlot.Primary, rifle.WeaponId); flow.StartRaid(map);
            flow.Session.AddLoot(new[] { new ItemStack(trade, 2) });
            Assert.IsTrue(success ? flow.ExtractSuccessfully() : flow.FailRaid()); Assert.IsTrue(flow.ReturnToHubAndSave());
            var loaded = new GameFlow(catalog, saves); Assert.IsTrue(loaded.Load()); Assert.AreEqual(GameState.Hub, loaded.State); Assert.IsNull(loaded.Session);
            Assert.AreEqual(success ? 3 : 1, loaded.Profile.Stash.GetItemCount(trade.Id)); Assert.AreEqual(success ? 1 : 0, loaded.Profile.Stash.GetItemCount(rifle.WeaponId));
            Assert.AreEqual(1, loaded.Profile.Stash.GetItemCount(pistol.WeaponId)); Assert.AreEqual(100, loaded.Profile.Wallet.Balance);
        }
        [Test] public void WorkbenchPresetsPersistIndependentlyAndUseExistingAttachmentSet()
        {
            var optic = attachments.First(a => a.slot == AttachmentSlot.Optic); var suppressor = attachments.First(a => a.suppressesFireSound);
            Assert.IsTrue(HubLoadoutAdapter.Configure(flow, rifle, optic.slot, optic, attachments));
            Assert.IsTrue(HubLoadoutAdapter.Configure(flow, rifle, suppressor.slot, suppressor, attachments));
            Assert.IsTrue(HubLoadoutAdapter.Configure(flow, pistol, optic.slot, optic, attachments));
            var loaded = new GameFlow(catalog, saves); Assert.IsTrue(loaded.Load());
            var r = new WeaponRuntimeState(rifle, 0); var p = new WeaponRuntimeState(pistol, 0);
            Assert.IsTrue(HubLoadoutAdapter.ApplyPreset(r, loaded.Profile, attachments)); Assert.IsTrue(HubLoadoutAdapter.ApplyPreset(p, loaded.Profile, attachments));
            Assert.AreSame(suppressor, r.Attachments[AttachmentSlot.Muzzle]); Assert.IsNull(p.Attachments[AttachmentSlot.Muzzle]); Assert.AreSame(optic, p.Attachments[AttachmentSlot.Optic]);
            Assert.Less(r.EffectiveNoiseMultiplier, p.EffectiveNoiseMultiplier);
            var grip = attachments.First(a => a.slot == AttachmentSlot.Grip); Assert.IsFalse(HubLoadoutAdapter.Configure(loaded, pistol, grip.slot, grip, attachments));
            var old = SaveMapper.Capture(loaded.Profile); old.weaponAttachments = null; Assert.IsEmpty(SaveMapper.Restore(old, catalog).WeaponAttachments);
        }
    }
}
