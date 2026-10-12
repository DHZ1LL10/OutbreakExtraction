using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Outbreak.Core;
using Outbreak.Hub;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Weapons;
using UnityEditor;
using UnityEngine;

namespace Outbreak.Tests
{
    public sealed class ImportedContentTests
    {
        private ImportedContentRegistry registry;
        private ItemCatalog catalog;
        private string directory;
        [SetUp] public void Setup()
        {
            registry=Resources.Load<ImportedContentRegistry>("ImportedContent");
            catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/DebugContent/Hub/HubCatalog.asset");
            directory=Path.Combine(Path.GetTempPath(),"OutbreakImportedTests",Guid.NewGuid().ToString("N"));
        }
        [TearDown] public void Cleanup() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
        [Test] public void SevenWeaponsHaveValidDistinctRolesPricesAmmoAndPreviewModels()
        {
            Assert.IsNotNull(registry); Assert.AreEqual(5,registry.weapons.Length); catalog.Validate();
            var previews=Resources.Load<ItemPreviewLibrary>("ItemPreviews");
            var originals=new[]{AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugRifle.asset"),AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugPistol.asset")};
            Assert.AreEqual(7,ImportedContentRegistry.Merge(originals,registry.weapons).Length);
            Assert.AreEqual(5,registry.weapons.Select(w=>w.WeaponId).Distinct().Count());
            Assert.AreEqual(5,registry.weapons.Select(w=>w.damage).Distinct().Count());
            foreach(var weapon in registry.weapons)
            {
                Assert.DoesNotThrow(weapon.Validate); Assert.AreSame(weapon.item,catalog.Get(weapon.WeaponId));
                Assert.Greater(weapon.magazineCapacity,0); Assert.Greater(weapon.initialReserveAmmo,0);
                Assert.IsFalse(float.IsNaN(weapon.fireRate) || float.IsInfinity(weapon.fireRate)); Assert.Greater(weapon.fireRate,0);
                Assert.Greater(weapon.item.BuyValue,weapon.item.SellValue); Assert.Greater(weapon.item.SellValue,0);
                Assert.IsNotNull(previews.Find(weapon.item)); Assert.AreEqual(-1,originals[0].initialReserveAmmo);
            }
        }
        [Test] public void EveryNewWeaponBuysEquipsSavesLoadsAndUsesItsReserveInRaid()
        {
            foreach(var weapon in registry.weapons)
            {
                string path=Path.Combine(directory,weapon.WeaponId); var saves=new SaveService(path); var flow=new GameFlow(catalog,saves);
                Assert.IsTrue(flow.CommitHubChange(f=>f.Profile.Wallet.Credit(10000)));
                Assert.IsTrue(flow.CommitHubChange(f=>f.Buy(weapon.WeaponId,1)));
                Assert.AreEqual(10000-weapon.item.BuyValue,flow.Profile.Wallet.Balance);
                var slot=weapon.weaponClass==WeaponClass.Pistol?LoadoutSlot.Secondary:LoadoutSlot.Primary;
                Assert.IsTrue(flow.CommitHubChange(f=>f.Equip(slot,weapon.WeaponId)));
                Assert.AreEqual(0,flow.Profile.Stash.GetItemCount(weapon.WeaponId));
                var loaded=new GameFlow(catalog,saves); Assert.IsTrue(loaded.Load()); Assert.AreSame(weapon.item,loaded.Profile.Loadout.Get(slot).Item);
                Assert.IsTrue(HubLoadoutAdapter.CanDeploy(loaded,registry.weapons,registry.attachments));
                Assert.IsTrue(loaded.StartRaid(AssetDatabase.LoadAssetAtPath<RaidMapDefinition>("Assets/Game/DebugContent/RaidLoop/RaidLoopMap.asset")));
                var root=new GameObject("Non-rendering firearm content test"); root.SetActive(false);
                try
                {
                    var controller=root.AddComponent<FirearmController>(); controller.EquipFromRaid(loaded.Session,registry.weapons,1,2);
                    Assert.AreSame(weapon,controller.Current.Definition); Assert.AreEqual(weapon.initialReserveAmmo,controller.Current.ReserveAmmo);
                    Assert.AreEqual(weapon.magazineCapacity,controller.Current.AmmoInMagazine);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
        [Test] public void AttachmentCompatibilityCapacityAndSavedPresetUseExistingRules()
        {
            var shotgun=registry.weapons.Single(w=>w.WeaponId=="shotgun_a_slug");
            var pistol=registry.weapons.Single(w=>w.WeaponId=="american_pistol");
            foreach(var attachment in registry.attachments) { Assert.DoesNotThrow(attachment.Validate); Assert.IsTrue(attachment.IsCompatible(shotgun)); Assert.IsFalse(attachment.IsCompatible(pistol)); }
            var magazine=registry.attachments.Single(a=>a.ItemId=="shotgun_box_mag");
            var state=new WeaponRuntimeState(shotgun,10,6); Assert.IsTrue(state.TryEquipAttachment(magazine));
            Assert.AreEqual(9,state.Stats.MagazineCapacity); Assert.AreEqual(6,state.AmmoInMagazine); Assert.AreEqual(10,state.ReserveAmmo);
            Assert.IsTrue(state.TryRemoveAttachment(AttachmentSlot.Magazine)); Assert.AreEqual(6,state.Stats.MagazineCapacity);
            var flow=new GameFlow(catalog,new SaveService(directory));
            Assert.IsTrue(HubLoadoutAdapter.Configure(flow,shotgun,AttachmentSlot.Magazine,magazine,registry.attachments));
            var loaded=new GameFlow(catalog,new SaveService(directory)); Assert.IsTrue(loaded.Load());
            var restored=new WeaponRuntimeState(shotgun,0); Assert.IsTrue(HubLoadoutAdapter.ApplyPreset(restored,loaded.Profile,registry.attachments));
            Assert.AreSame(magazine,restored.Attachments[AttachmentSlot.Magazine]);
        }
        [Test] public void PreparedHumanoidsAudioAndVfxAreReferencesOnly()
        {
            Assert.AreEqual(2,registry.infectedVisuals.Length);
            foreach(var variant in registry.infectedVisuals) { Assert.IsTrue(variant.avatar.isValid && variant.avatar.isHuman); Assert.IsNotNull(variant.controller); Assert.IsTrue(variant.allowNormal && variant.allowRunner); }
            Assert.AreEqual(6,registry.infectedAudio.idle.Length); Assert.AreEqual(6,registry.infectedAudio.attack.Length); Assert.AreEqual(6,registry.infectedAudio.damage.Length);
            Assert.AreEqual(3,registry.infectedAudio.death.Length); Assert.IsNull(registry.infectedAudio.alert); Assert.IsNull(registry.infectedAudio.chase);
            foreach(var effect in registry.futureImpacts) Assert.IsNotNull(effect.prefab);
            Assert.AreEqual(5,registry.futureAttachmentModels.Length);
        }
    }
}
