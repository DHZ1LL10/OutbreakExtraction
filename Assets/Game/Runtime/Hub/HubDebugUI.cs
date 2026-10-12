using System;
using System.Linq;
using Outbreak.Core;
using Outbreak.Interaction;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Loot;
using Outbreak.Weapons;
using Outbreak.Quests;
using UnityEngine;

namespace Outbreak.Hub
{
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class HubDebugUI : MonoBehaviour
    {
        private PlayerInteractor interactor;
        private Vector2 scroll;
        private bool sell;
        private LoadoutSlot selectedWeapon = LoadoutSlot.Primary;
        private string inspected;
        private string tradeFeedback;
        private bool inventoryOpen;
        private bool loadoutTab;
        private Outbreak.Player.PlayerInput input;
        private HubSceneFlow Hub => HubSceneFlow.Instance;
        private void Awake() { interactor = GetComponent<PlayerInteractor>(); input = GetComponent<Outbreak.Player.PlayerInput>(); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && Hub != null && Hub.Ready)
            {
                if (interactor.ActivePanel != null) interactor.ClosePanel();
                else SetInventory(!inventoryOpen);
            }
            if (inventoryOpen && (Input.GetKeyDown(KeyCode.Escape) || Hub == null || !Hub.Ready || Hub.Transitioning)) SetInventory(false);
        }
        private void SetInventory(bool value) { inventoryOpen = value; input?.SetGameplaySuppressed(this, value); }
        private void OnDisable() { if (inventoryOpen) SetInventory(false); }
        private void OnGUI()
        {
            var hub = Hub; if (hub == null || hub.Flow == null) return;
            var profile = hub.Flow.Profile;
            using (new TacticalUI.Scope())
            {
            if (PresentationMode.ShowDiagnostics)
                GUI.Box(new Rect(20, 260, 340, 85), $"HUB | ${profile.Wallet.Balance}\nStash {profile.Stash.Slots.Count}/{profile.Stash.Capacity}\n{hub.Message}");
            var station = interactor.ActivePanel as HubStation;
            if (station == null && !inventoryOpen)
            {
                if (interactor.Focused != null) TacticalUI.Prompt("[E] " + interactor.Focused.Prompt);
                if (!hub.Ready) TacticalUI.Prompt(hub.Message, 575);
                return;
            }
            TacticalUI.BeginPanel(inventoryOpen ? "PERSONAL EQUIPMENT" : station.PanelTitle,
                $"FIELD SAFEHOUSE     /     ${profile.Wallet.Balance}     /     STASH {profile.Stash.Slots.Count} : {profile.Stash.Capacity}");
            scroll = GUILayout.BeginScrollView(scroll);
            if (inventoryOpen)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("INVENTORY")) loadoutTab = false;
                if (GUILayout.Button("LOADOUT")) loadoutTab = true;
                GUILayout.EndHorizontal();
                if (loadoutTab) DrawEquipment(profile, false);
                else { GUILayout.Label("No raid loot in safehouse. Open STASH to manage stored items."); DrawEquipment(profile, false); }
            }
            else switch (station.Kind)
            {
                case HubStationKind.Stash: DrawStash(profile); break;
                case HubStationKind.Loadout: DrawLoadout(profile); break;
                case HubStationKind.Trader: DrawTrader(profile); break;
                case HubStationKind.Workbench: DrawWorkbench(profile); break;
                case HubStationKind.Missions: DrawMission(); break;
                case HubStationKind.Deploy:
                    TacticalUI.Heading("QUARANTINE OUTSKIRTS");
                    GUILayout.Label("DIFFICULTY   /   Normal\nOBJECTIVE   /   Recover supplies and extract");
                    if (hub.Flow.MissionProgress?.state == QuestState.Active) GUILayout.Label("Active assignment: " + hub.Flow.Mission.DisplayName);
                    GUILayout.Space(20);
                    DrawEquipment(profile, false);
                    GUI.enabled = HubLoadoutAdapter.CanDeploy(hub.Flow, hub.Weapons, hub.Attachments);
                    if (GUILayout.Button("START RAID")) hub.Deploy();
                    GUI.enabled = true; break;
            }
            GUILayout.EndScrollView();
            GUILayout.Label(hub.Message);
            if (GUILayout.Button("CLOSE    /    ESC · TAB")) { if (inventoryOpen) SetInventory(false); else interactor.ClosePanel(); }
            TacticalUI.EndPanel();
            }
        }
        private void DrawMission()
        {
            var definition = Hub.Flow.Mission; var progress = Hub.Flow.MissionProgress;
            if (definition == null || progress == null) { GUILayout.Label("No active assignment"); return; }
            GUILayout.Label(definition.DisplayName);
            switch (progress.state)
            {
                case QuestState.Available:
                    GUILayout.Label(definition.Description);
                    if (GUILayout.Button("ACCEPT")) Hub.AcceptMission();
                    break;
                case QuestState.Active:
                    GUILayout.Label($"ACTIVE MISSION\nMedical Supplies: {progress.count} / {definition.RequiredCount}\nExtract successfully");
                    break;
                case QuestState.ReadyToTurnIn:
                    GUILayout.Label("READY TO TURN IN");
                    GUILayout.Label($"Reward: +${definition.RewardMoney} / +{definition.RewardXP} XP");
                    if (GUILayout.Button("TURN IN")) Hub.TurnInMission();
                    break;
                case QuestState.Completed: case QuestState.Claimed: GUILayout.Label("Assignment complete."); break;
            }
            GUILayout.Label($"Money: ${Hub.Flow.Profile.Wallet.Balance} | XP: {Hub.Flow.Profile.XP}");
        }
        private bool Fits(LoadoutSlot slot, ItemDefinition item)
        {
            if (!Loadout.Accepts(slot, item)) return false;
            if (slot != LoadoutSlot.Primary && slot != LoadoutSlot.Secondary) return true;
            var weapon = Hub.Weapons.FirstOrDefault(w => w.WeaponId == item.Id);
            return weapon != null && weapon.weaponClass == (slot == LoadoutSlot.Primary ? WeaponClass.Rifle : WeaponClass.Pistol);
        }
        private void DrawStash(PlayerProfile profile)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(615));
            TacticalUI.Heading($"STORAGE    {profile.Stash.Slots.Count} / {profile.Stash.Capacity}");
            if (!string.IsNullOrEmpty(inspected)) GUILayout.Label(inspected);
            foreach (var stack in profile.Stash.Slots.ToArray())
            {
                Color color = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                GUILayout.Label($"{TacticalUI.Name(stack.Item.DisplayName)} x{stack.Quantity} | {stack.Item.Rarity} | sell ${stack.Item.SellValue}/unit"); GUI.color = color;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Inspect")) inspected = TacticalUI.Name(stack.Item.DisplayName) + " | " + stack.Item.Description;
                foreach (LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
                    if (Fits(slot, stack.Item) && GUILayout.Button("Equip " + slot)) { Hub.Equip(slot, stack.Item); break; }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(335));
            TacticalUI.Heading("LOADOUT"); DrawEquipment(profile, true);
            GUILayout.EndVertical(); GUILayout.EndHorizontal();
        }
        private void DrawEquipment(PlayerProfile profile, bool editable)
        {
            foreach (LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
            {
                var stack = profile.Loadout.Get(slot);
                GUILayout.Label(slot.ToString().ToUpperInvariant() + "   /   " + (stack == null ? "Empty" : TacticalUI.Name(stack.Item.DisplayName) + " x" + stack.Quantity));
                if (editable && stack != null && GUILayout.Button("UNEQUIP " + slot)) Hub.Change(f => f.Unequip(slot));
            }
        }
        private void DrawLoadout(PlayerProfile profile)
        {
            DrawEquipment(profile, true);
            foreach (LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
                foreach (var stack in profile.Stash.Slots.ToArray())
                    if (Fits(slot, stack.Item) && GUILayout.Button($"{slot} <- {TacticalUI.Name(stack.Item.DisplayName)}")) Hub.Equip(slot, stack.Item);
        }
        private void DrawTrader(PlayerProfile profile)
        {
            GUILayout.BeginHorizontal(); if (GUILayout.Button(sell ? "BUY" : "[ BUY ]")) sell = false; if (GUILayout.Button(sell ? "[ SELL ]" : "SELL")) sell = true; GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(tradeFeedback)) GUILayout.Label(tradeFeedback);
            if (!sell)
                foreach (var item in Hub.TraderStock)
                {
                    int quantity = item.Type == ItemType.Ammo ? 30 : 1;
                    if (GUILayout.Button($"Buy {TacticalUI.Name(item.DisplayName)} x{quantity} | ${item.BuyValue * quantity}"))
                    {
                        if (!profile.Wallet.CanAfford(item.BuyValue * quantity)) tradeFeedback = "Insufficient funds.";
                        else if (!profile.Stash.Copy().AddItem(item, quantity)) tradeFeedback = "Stash full. Free storage space before buying.";
                        else tradeFeedback = Hub.Change(f => f.Buy(item.Id, quantity)) ? "Purchased " + TacticalUI.Name(item.DisplayName) + " ×" + quantity : Hub.Message;
                    }
                }
            else foreach (var stack in profile.Stash.Slots.ToArray())
            {
                if (stack.Item.SellValue <= 0 || stack.Item.Type == ItemType.QuestItem) continue;
                GUILayout.BeginHorizontal(); GUILayout.Label(TacticalUI.Name(stack.Item.DisplayName) + " x" + stack.Quantity);
                if (GUILayout.Button($"Sell 1 (${stack.Item.SellValue})")) tradeFeedback = Hub.Change(f => f.Sell(stack.Item.Id, 1)) ? "Sold " + TacticalUI.Name(stack.Item.DisplayName) + " ×1" : Hub.Message;
                if (GUILayout.Button($"Sell stack (${stack.Item.SellValue * stack.Quantity})")) tradeFeedback = Hub.Change(f => f.Sell(stack.Item.Id, stack.Quantity)) ? "Sold " + TacticalUI.Name(stack.Item.DisplayName) + " ×" + stack.Quantity : Hub.Message;
                GUILayout.EndHorizontal();
            }
        }
        private void DrawWorkbench(PlayerProfile profile)
        {
            GUILayout.Label("Select a weapon and configure its available attachments.");
            GUILayout.BeginHorizontal(); if (GUILayout.Button("AKM / PRIMARY")) selectedWeapon = LoadoutSlot.Primary; if (GUILayout.Button("PISTOL / SECONDARY")) selectedWeapon = LoadoutSlot.Secondary; GUILayout.EndHorizontal();
            var item = profile.Loadout.Get(selectedWeapon)?.Item;
            var weapon = Hub.Weapons.FirstOrDefault(w => w.WeaponId == item?.Id);
            if (weapon == null) { GUILayout.Label("Equip a weapon first."); return; }
            var state = new WeaponRuntimeState(weapon, 0, 0);
            if (!HubLoadoutAdapter.ApplyPreset(state, profile, Hub.Attachments)) { GUILayout.Label("Invalid preset."); return; }
            GUILayout.Label(TacticalUI.Name(weapon.DisplayName));
            foreach (AttachmentSlot slot in Enum.GetValues(typeof(AttachmentSlot)))
            {
                GUILayout.Label(slot + ": " + TacticalUI.Name(state.Attachments[slot]?.DisplayName ?? "None"));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("None")) Hub.Configure(selectedWeapon, slot, null);
                foreach (var attachment in Hub.Attachments)
                    if (attachment.slot == slot && attachment.IsCompatible(weapon) && GUILayout.Button(TacticalUI.Name(attachment.DisplayName))) Hub.Configure(selectedWeapon, slot, attachment);
                GUILayout.EndHorizontal();
            }
        }
    }
}
