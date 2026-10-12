using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Outbreak.Core;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Maps;
using Outbreak.Persistence;
using Outbreak.Quests;
using UnityEditor;
using UnityEngine;

namespace Outbreak.Tests
{
    public sealed class ExpandedAssignmentTests
    {
        private string directory;
        private GameFlow flow;
        private ItemCatalog catalog;
        private RaidMapDefinition map;
        private QuestDefinition[] missions;
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"OutbreakAssignments",Guid.NewGuid().ToString("N"));
            catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/DebugContent/Hub/HubCatalog.asset");
            map=AssetDatabase.LoadAssetAtPath<RaidMapDefinition>("Assets/Game/DebugContent/RaidLoop/RaidLoopMap.asset");
            missions=new[]{Resources.Load<QuestDefinition>("RecoverSupplies")}.Concat(Resources.LoadAll<QuestDefinition>("Assignments")).ToArray();
            flow=new GameFlow(catalog,new SaveService(directory)); Assert.IsTrue(flow.InitializeAssignments(missions));
        }
        [TearDown] public void Cleanup() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
        private void Accept(string id) { Assert.IsTrue(flow.EnableMission(missions.Single(m=>m.Id==id))); Assert.IsTrue(flow.AcceptMission()); }
        [Test] public void FiveValidDistinctAssignmentsAndOneActiveSurviveLoad()
        {
            Assert.AreEqual(5,missions.Length); Assert.AreEqual(5,missions.Select(m=>m.Id).Distinct().Count());
            Accept("03_salvage"); Assert.IsFalse(flow.EnableMission(missions.Single(m=>m.Id=="02_clearance")));
            var loaded=new GameFlow(catalog,new SaveService(directory)); Assert.IsTrue(loaded.Load()); Assert.IsTrue(loaded.InitializeAssignments(missions));
            Assert.AreEqual("03_salvage",loaded.Mission.Id); Assert.AreEqual(QuestState.Active,loaded.MissionProgress.state);
        }
        [Test] public void KillObjectiveRequiresExtractionAndPaysOnlyOnce()
        {
            Accept("02_clearance"); flow.RecordInfectedKill(false); Assert.AreEqual(0,flow.MissionProgress.count);
            Assert.IsTrue(flow.StartRaid(map)); for(int i=0;i<12;i++) flow.RecordInfectedKill(i%2==0);
            Assert.AreEqual(10,flow.MissionProgress.count); Assert.AreEqual(QuestState.Active,flow.MissionProgress.state);
            // Loot events cannot reset kill progress.
            Assert.IsTrue(flow.Session.AddLoot(new[]{new ItemStack(catalog.Get("debug_scrap"),1)})); Assert.AreEqual(10,flow.MissionProgress.count);
            Assert.IsFalse(flow.TurnInMission()); Assert.IsTrue(flow.ExtractSuccessfully()); Assert.IsTrue(flow.ReturnToHubAndSave());
            Assert.IsTrue(flow.TurnInMission()); Assert.IsFalse(flow.TurnInMission()); Assert.AreEqual(800,flow.Profile.Wallet.Balance); Assert.AreEqual(200,flow.Profile.XP);
            Assert.IsTrue(flow.EnableMission(missions.Single(m=>m.Id=="04_runners")));
        }
        [Test] public void RunnerObjectiveIgnoresNormalsAndDeathResetsAttempt()
        {
            Accept("04_runners"); Assert.IsTrue(flow.StartRaid(map)); flow.RecordInfectedKill(false); Assert.AreEqual(0,flow.MissionProgress.count);
            flow.RecordInfectedKill(true); Assert.AreEqual(1,flow.MissionProgress.count);
            Assert.IsTrue(flow.FailRaid()); Assert.AreEqual(0,flow.MissionProgress.count); Assert.IsFalse(flow.TurnInMission());
        }
        [Test] public void ScrapObjectiveTracksOnlyLootCarriedAndConfirmsExtraction()
        {
            Accept("03_salvage"); Assert.IsTrue(flow.StartRaid(map)); var scrap=catalog.Get("debug_scrap");
            Assert.IsTrue(flow.Session.AddLoot(new[]{new ItemStack(scrap,6),new ItemStack(scrap,6)})); Assert.AreEqual(12,flow.MissionProgress.count);
            Assert.IsTrue(flow.Session.RemoveLoot(scrap.Id,1)); Assert.AreEqual(11,flow.MissionProgress.count);
            Assert.IsTrue(flow.Session.AddLoot(new[]{new ItemStack(scrap,1)})); Assert.IsTrue(flow.ExtractSuccessfully());
            Assert.AreEqual(QuestState.ReadyToTurnIn,flow.MissionProgress.state);
        }
    }
}
