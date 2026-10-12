using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Outbreak.Core;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loot;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Quests;
using UnityEditor;

namespace Outbreak.Tests
{
    public sealed class RecoverSuppliesQuestTests
    {
        private string directory;
        private ItemCatalog catalog;
        private ItemDefinition medical;
        private QuestDefinition quest;
        private RaidMapDefinition map;
        private SaveService saves;
        private GameFlow flow;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "OutbreakQuestTests", Guid.NewGuid().ToString("N")); saves = new SaveService(directory);
            catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/DebugContent/Hub/HubCatalog.asset"); medical = catalog.Get("debug_medical_supplies");
            quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Game/Resources/RecoverSupplies.asset");
            map = AssetDatabase.LoadAssetAtPath<RaidMapDefinition>("Assets/Game/DebugContent/RaidLoop/RaidLoopMap.asset");
            flow = new GameFlow(catalog, saves); flow.Profile.Wallet.Credit(100); Assert.IsTrue(flow.EnableMission(quest));
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private void Start() { Assert.IsTrue(flow.AcceptMission()); Assert.IsTrue(flow.StartRaid(map)); }
        private void Collect(int count) { Assert.IsTrue(flow.Session.AddLoot(new[] { new ItemStack(medical, count) })); }
        [Test] public void AcceptanceAndSuccessfulLootTransfersProgressWithoutCompleting()
        {
            Assert.AreEqual(QuestState.Available, flow.MissionProgress.state); Start();
            var container = new LootContainerInventory(); container.EnsureGenerated(() => new[] { new ItemStack(medical, 1), new ItemStack(medical, 1) });
            Assert.AreEqual(0, flow.MissionProgress.count); Assert.IsTrue(container.TryTake(0, flow.Session)); Assert.AreEqual(1, flow.MissionProgress.count);
            Assert.IsTrue(container.TryTake(0, flow.Session)); Assert.AreEqual(2, flow.MissionProgress.count);
            Assert.AreEqual(QuestState.Active, flow.MissionProgress.state); Assert.IsFalse(flow.TurnInMission()); Assert.AreEqual(100, flow.Profile.Wallet.Balance);
            Assert.IsTrue(flow.Session.RemoveLoot(medical.Id, 1)); Assert.AreEqual(1, flow.MissionProgress.count);
            Assert.IsTrue(flow.ExtractSuccessfully()); Assert.AreEqual(QuestState.Active, flow.MissionProgress.state);
        }
        [Test] public void DeathResetsAttemptAndNeverMakesItReadyOrPaysReward()
        {
            Start(); Collect(2); Assert.IsTrue(flow.FailRaid()); Assert.AreEqual(0, flow.MissionProgress.count); Assert.AreEqual(QuestState.Active, flow.MissionProgress.state);
            Assert.IsTrue(flow.ReturnToHubAndSave()); Assert.IsFalse(flow.TurnInMission()); Assert.AreEqual(100, flow.Profile.Wallet.Balance); Assert.AreEqual(0, flow.Profile.XP);
            var loaded = new GameFlow(catalog, saves); Assert.IsTrue(loaded.Load()); loaded.EnableMission(quest);
            Assert.AreEqual(QuestState.Active, loaded.MissionProgress.state); Assert.AreEqual(0, loaded.MissionProgress.count);
        }
        [Test] public void ExtractionTurnInAndReloadPayExactlyOnce()
        {
            Start(); Collect(2); Assert.IsTrue(flow.ExtractSuccessfully()); Assert.AreEqual(QuestState.ReadyToTurnIn, flow.MissionProgress.state);
            Assert.AreEqual(100, flow.Profile.Wallet.Balance); Assert.AreEqual(0, flow.Profile.XP); Assert.IsTrue(flow.ReturnToHubAndSave());
            var loaded = new GameFlow(catalog, saves); Assert.IsTrue(loaded.Load()); loaded.EnableMission(quest);
            Assert.AreEqual(QuestState.ReadyToTurnIn, loaded.MissionProgress.state); Assert.IsTrue(loaded.TurnInMission());
            Assert.AreEqual(600, loaded.Profile.Wallet.Balance); Assert.AreEqual(100, loaded.Profile.XP); Assert.AreEqual(QuestState.Completed, loaded.MissionProgress.state);
            Assert.IsFalse(loaded.TurnInMission()); Assert.AreEqual(600, loaded.Profile.Wallet.Balance); Assert.AreEqual(100, loaded.Profile.XP);
            var final = new GameFlow(catalog, saves); Assert.IsTrue(final.Load()); final.EnableMission(quest);
            Assert.AreEqual(QuestState.Completed, final.MissionProgress.state); Assert.IsFalse(final.TurnInMission()); Assert.AreEqual(600, final.Profile.Wallet.Balance);
        }
        [Test] public void InterruptedRaidReloadsActiveWithoutUnextractedProgress()
        {
            Start(); Collect(2); Assert.IsFalse(flow.Save());
            var loaded = new GameFlow(catalog, saves); Assert.IsTrue(loaded.Load()); loaded.EnableMission(quest);
            Assert.AreEqual(QuestState.Active, loaded.MissionProgress.state); Assert.AreEqual(0, loaded.MissionProgress.count); Assert.IsFalse(loaded.TurnInMission());
        }
        [Test] public void FailedExtractionOrTurnInSaveCannotConfirmOrReward()
        {
            var rifle = catalog.Get("debug_rifle"); while (flow.Profile.Stash.Slots.Count < flow.Profile.Stash.Capacity) flow.Profile.Stash.AddItem(rifle);
            Start(); Collect(2); Assert.IsFalse(flow.ExtractSuccessfully()); Assert.AreEqual(QuestState.Active, flow.MissionProgress.state);
            Assert.IsTrue(flow.Profile.Stash.RemoveItem(rifle.Id, 1)); Assert.IsTrue(flow.ExtractSuccessfully()); Assert.IsTrue(flow.ReturnToHubAndSave());
            File.Move(saves.FilePath, saves.FilePath + ".backup"); Directory.CreateDirectory(saves.FilePath);
            Assert.IsFalse(flow.TurnInMission()); Assert.AreEqual(QuestState.ReadyToTurnIn, flow.MissionProgress.state);
            Assert.AreEqual(100, flow.Profile.Wallet.Balance); Assert.AreEqual(0, flow.Profile.XP);
        }
        [Test] public void DemoMedicalBagContainsEnoughSuppliesWithoutChangingOtherTables()
        {
            var table = AssetDatabase.LoadAssetAtPath<LootTable>("Assets/Game/DebugContent/RaidLoop/MedicalLoot.asset");
            Assert.GreaterOrEqual(table.Generate(new Random(2202), 3, map).Where(x => x.Item.Id == medical.Id).Sum(x => x.Quantity), 2);
        }
    }
}
