using System;
using System.IO;
using System.Linq;
using Outbreak.Core;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Maps;
using Outbreak.Player;
using Outbreak.Weapons;
using Outbreak.Quests;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Outbreak.Hub
{
    [DefaultExecutionOrder(200), RequireComponent(typeof(GameManager))]
    public sealed class HubSceneFlow : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition[] weapons;
        [SerializeField] private AttachmentDefinition[] attachments;
        [SerializeField] private ItemDefinition[] traderStock;
        [SerializeField] private RaidMapDefinition raidMap;
        [SerializeField] private ItemDefinition[] starterItems;
        [SerializeField, Min(0)] private int startingMoney = 1500;
        [SerializeField] private QuestDefinition mission;
        public static HubSceneFlow Instance { get; private set; }
        private GameManager manager;
        public GameFlow Flow => manager != null ? manager.Flow : null;
        public WeaponDefinition[] Weapons => weapons;
        public AttachmentDefinition[] Attachments => attachments;
        public ItemDefinition[] TraderStock => traderStock;
        public bool Ready { get; private set; }
        public bool Transitioning { get; private set; }
        public string Message { get; private set; }
        private void Awake()
        {
            manager = GetComponent<GameManager>();
            if (GameManager.Persistent != manager) return;
            Instance = this; SceneManager.sceneLoaded += SceneLoaded;
        }
        private void Start()
        {
            if (Instance != this || Flow == null) return;
            Ready = false;
            if (File.Exists(manager.SavePath) && !manager.LoadSucceeded)
            { Message = "Profile could not be loaded. Original save preserved: " + Flow.LastMessage; return; }
            if (!File.Exists(manager.SavePath))
            {
                if (!Flow.CommitHubChange(f => f.Profile.Wallet.Credit(startingMoney) && starterItems.All(i => f.Profile.Stash.AddItem(i, i.Type == ItemType.Ammo ? Mathf.Min(60, i.MaxStack) : 1))))
                { Message = "Could not save starter profile: " + Flow.LastMessage; return; }
            }
            if (mission == null) mission = Resources.Load<QuestDefinition>("RecoverSupplies");
            if (mission == null || !Flow.InitializeAssignments(new[] { mission }.Concat(Resources.LoadAll<QuestDefinition>("Assignments").OrderBy(q=>q.Id))))
            { Message = "Mission could not be initialized: " + Flow.LastMessage; return; }
            Ready = Flow.State == GameState.Hub && Flow.Session == null; Message = "Hub ready.";
        }
        private void OnDestroy()
        { if (Instance == this) { SceneManager.sceneLoaded -= SceneLoaded; Instance = null; } }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != this) return;
            Transitioning = false;
            if (scene.name == "RaidLoop_Test" && Flow?.State == GameState.Raid)
            {
                var firearm = FindFirstObjectByType<FirearmController>();
                if (firearm == null) { Message = "Raid firearm missing."; Debug.LogError(Message); return; }
                firearm.EquipFromRaid(Flow.Session, weapons, 180, 75);
                if (!HubLoadoutAdapter.ApplyPreset(firearm.PrimaryState, Flow.Profile, attachments) ||
                    !HubLoadoutAdapter.ApplyPreset(firearm.SecondaryState, Flow.Profile, attachments))
                    Debug.LogError("[Hub] Invalid persisted weapon preset.");
                // Delivery flow uses the saved workbench configuration. Standalone gunplay debug stays unchanged.
                var debug = firearm.GetComponent<DebugAttachmentController>(); if (debug != null) debug.enabled = false;
                Message = "Raid active.";
            }
            else if (scene.name == "Hub_Test") { Ready = Flow?.State == GameState.Hub && Flow.Session == null; Message = "Returned to Hub."; }
        }
        public bool AcceptMission()
        {
            if (!Ready || Transitioning) return false;
            bool success = Flow.AcceptMission(); Message = success ? "Mission accepted." : Flow.LastMessage; return success;
        }
        public bool TurnInMission()
        {
            if (!Ready || Transitioning) return false;
            bool success = Flow.TurnInMission(); Message = success ? $"Assignment complete. +${Flow.Mission.RewardMoney} / +{Flow.Mission.RewardXP} XP" : Flow.LastMessage; return success;
        }
        public bool Change(Func<GameFlow, bool> operation)
        {
            if (!Ready || Transitioning) return false;
            bool success = Flow.CommitHubChange(operation);
            Message = success ? "Saved." : "Rejected: check funds, stash capacity and item availability. " + Flow.LastMessage;
            return success;
        }
        public bool Equip(LoadoutSlot slot, ItemDefinition item)
        {
            var weapon = weapons.FirstOrDefault(w => w != null && w.WeaponId == item.Id);
            if ((slot == LoadoutSlot.Primary || slot == LoadoutSlot.Secondary) &&
                (weapon == null || weapon.weaponClass != (slot == LoadoutSlot.Primary ? WeaponClass.Rifle : WeaponClass.Pistol))) return false;
            return Change(f => f.Equip(slot, item.Id));
        }
        public bool Configure(LoadoutSlot weaponSlot, AttachmentSlot slot, AttachmentDefinition attachment)
        {
            if (!Ready || Transitioning) return false;
            var item = Flow.Profile.Loadout.Get(weaponSlot)?.Item;
            var weapon = weapons.FirstOrDefault(w => w != null && w.WeaponId == item?.Id);
            bool success = HubLoadoutAdapter.Configure(Flow, weapon, slot, attachment, attachments);
            Message = success ? "Weapon configuration saved." : "Incompatible attachment or save failed."; return success;
        }
        public bool Deploy()
        {
            if (!Ready || Transitioning) return false;
            if (!Application.CanStreamedLevelBeLoaded("RaidLoop_Test")) { Message = "RaidLoop_Test must be enabled in Build Settings."; return false; }
            if (!HubLoadoutAdapter.CanDeploy(Flow, weapons, attachments)) { Message = "Equip a rifle in Primary and/or a pistol in Secondary."; return false; }
            if (!Flow.StartRaid(raidMap)) { Message = Flow.LastMessage; return false; }
            Transitioning = true; Ready = false;
            FindFirstObjectByType<PlayerInput>()?.SetControlEnabled(false);
            SceneManager.LoadSceneAsync("RaidLoop_Test", LoadSceneMode.Single); return true;
        }
        public bool ReturnToHub()
        {
            if (Transitioning || Flow == null || (Flow.State != GameState.RaidSuccess && Flow.State != GameState.RaidFailed)) return false;
            if (!Application.CanStreamedLevelBeLoaded("Hub_Test")) { Message = "Hub_Test must be enabled in Build Settings."; return false; }
            if (!Flow.ReturnToHubAndSave()) { Message = Flow.LastMessage; return false; }
            Transitioning = true; SceneManager.LoadSceneAsync("Hub_Test", LoadSceneMode.Single); return true;
        }
    }
}
