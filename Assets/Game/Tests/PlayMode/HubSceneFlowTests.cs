#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Outbreak.Tests
{
    // Non-rendering scene lifecycle regression with a unique save; never touches profile.json.
    public sealed class HubSceneFlowTests
    {
        private GameObject managerObject;
        private string savePath;
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (managerObject != null) UnityEngine.Object.Destroy(managerObject);
            yield return null;
            if (savePath != null && File.Exists(savePath)) File.Delete(savePath);
        }
        [UnityTest] public IEnumerator HubRaidSuccessAndDeathReturnUseSameProfileAndConfiguredEquipment()
        {
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugRifle.asset");
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugPistol.asset");
            var attachments = AssetDatabase.FindAssets("t:AttachmentDefinition", new[] { "Assets/Game/DebugContent/Gunplay/Attachments" }).Select(g => AssetDatabase.LoadAssetAtPath<AttachmentDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/DebugContent/Hub/HubCatalog.asset");
            var map = AssetDatabase.LoadAssetAtPath<RaidMapDefinition>("Assets/Game/DebugContent/RaidLoop/RaidLoopMap.asset");
            var scrap = catalog.Get("debug_scrap");
            string saveName = "hub_scene_test_" + Guid.NewGuid().ToString("N") + ".json";
            savePath = new SaveService(fileName: saveName).FilePath;
            managerObject = new GameObject("Test persistent manager"); managerObject.SetActive(false);
            var manager = managerObject.AddComponent<GameManager>(); Set(manager, "catalog", catalog); Set(manager, "saveFileName", saveName); Set(manager, "persistAcrossScenes", true);
            var driver = managerObject.AddComponent<HubSceneFlow>(); Set(driver, "weapons", new[] { rifle, pistol }); Set(driver, "attachments", attachments);
            Set(driver, "raidMap", map); Set(driver, "starterItems", new[] { rifle.item, pistol.item });
            managerObject.SetActive(true); yield return null;
            yield return SceneManager.LoadSceneAsync("Hub_Test"); yield return null;
            Assert.AreSame(manager, GameManager.Persistent); Assert.AreSame(driver, HubSceneFlow.Instance); Assert.IsTrue(driver.Ready);
            Assert.IsTrue(driver.Equip(LoadoutSlot.Primary, rifle.item));
            var optic = attachments.First(a => a.slot == AttachmentSlot.Optic);
            Assert.IsTrue(driver.Configure(LoadoutSlot.Primary, optic.slot, optic));
            Assert.IsTrue(driver.Deploy());
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().name != "RaidLoop_Test"; i++) yield return null;
            yield return null;
            Assert.AreEqual("RaidLoop_Test", SceneManager.GetActiveScene().name);
            var firearm = UnityEngine.Object.FindFirstObjectByType<FirearmController>();
            Assert.AreEqual(rifle.WeaponId, firearm.PrimaryState.Definition.WeaponId); Assert.IsNull(firearm.SecondaryState);
            Assert.AreSame(optic, firearm.PrimaryState.Attachments[AttachmentSlot.Optic]); Assert.AreSame(manager.Flow, UnityEngine.Object.FindFirstObjectByType<Outbreak.Raids.RaidRuntimeContext>().Flow);
            Assert.IsTrue(manager.Flow.Session.AddLoot(new[] { new ItemStack(scrap, 2) })); Assert.IsTrue(manager.Flow.ExtractSuccessfully()); Assert.IsTrue(driver.ReturnToHub());
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().name != "Hub_Test"; i++) yield return null;
            yield return null;
            Assert.AreSame(manager, GameManager.Persistent); Assert.AreEqual(2, manager.Flow.Profile.Stash.GetItemCount(scrap.Id)); Assert.IsNull(manager.Flow.Session);
            Assert.IsTrue(driver.Equip(LoadoutSlot.Primary, rifle.item)); Assert.IsTrue(driver.Deploy());
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().name != "RaidLoop_Test"; i++) yield return null;
            yield return null;
            Assert.IsTrue(manager.Flow.Session.AddLoot(new[] { new ItemStack(scrap, 3) })); Assert.IsTrue(manager.Flow.FailRaid()); Assert.IsTrue(driver.ReturnToHub());
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().name != "Hub_Test"; i++) yield return null;
            yield return null;
            Assert.AreEqual(2, manager.Flow.Profile.Stash.GetItemCount(scrap.Id)); Assert.AreEqual(0, manager.Flow.Profile.Stash.GetItemCount(rifle.WeaponId));
            var loaded = new GameFlow(catalog, new SaveService(fileName: saveName)); Assert.IsTrue(loaded.Load()); Assert.AreEqual(2, loaded.Profile.Stash.GetItemCount(scrap.Id)); Assert.AreEqual(1500, loaded.Profile.Wallet.Balance);
        }
    }
}
#endif
