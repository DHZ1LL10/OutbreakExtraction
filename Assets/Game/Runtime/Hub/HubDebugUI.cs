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
        private ItemDefinition selectedItem;
        private QuestDefinition selectedAssignment;
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
                DrawStash(profile);
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
                    var mapRect = GUILayoutUtility.GetRect(280, 240);
                    Outbreak.Raids.RaidMinimap.Draw(new Rect(mapRect.x,mapRect.y,280,240));
                    GUILayout.Label("DIFFICULTY   /   Normal · 18 infected · 5 runners");
                    if (hub.Flow.MissionProgress?.state == QuestState.Active) GUILayout.Label("Active assignment: " + hub.Flow.Mission.DisplayName);
                    GUILayout.Space(20);
                    GUI.enabled = HubLoadoutAdapter.CanDeploy(hub.Flow, hub.Weapons, hub.Attachments);
                    if (GUILayout.Button("START RAID")) hub.Deploy();
                    GUI.enabled = true;
                    DrawEquipment(profile, false); break;
            }
            GUILayout.EndScrollView();
            GUILayout.Label(hub.Message);
            if (GUILayout.Button("CLOSE    /    ESC · TAB")) { if (inventoryOpen) SetInventory(false); else interactor.ClosePanel(); }
            TacticalUI.EndPanel();
            }
        }
        private void DrawMission()
        {
            if (selectedAssignment == null) selectedAssignment=Hub.Flow.Mission;
            GUILayout.BeginHorizontal(); GUILayout.BeginVertical(GUILayout.Width(460));
            TacticalUI.Heading("AVAILABLE ASSIGNMENTS");
            foreach (var assignment in Hub.Flow.Assignments)
            {
                var saved = Hub.Flow.Profile.Quests.FirstOrDefault(q=>q.questId==assignment.Id);
                TacticalUI.BeginCard(assignment.DisplayName, $"{assignment.Type} ×{assignment.RequiredCount} + EXTRACT  /  ${assignment.RewardMoney} · {assignment.RewardXP} XP  /  {saved?.state}",TacticalUI.Accent,selectedAssignment==assignment);
                if (TacticalUI.SelectButton("VIEW ASSIGNMENT",selectedAssignment==assignment)) selectedAssignment=assignment;
                TacticalUI.EndCard();
            }
            GUILayout.EndVertical(); GUILayout.BeginVertical(GUILayout.Width(475));
            var definition = selectedAssignment; var progress = Hub.Flow.Profile.Quests.FirstOrDefault(q=>q.questId==definition?.Id);
            if (definition == null || progress == null) { GUILayout.EndVertical(); GUILayout.EndHorizontal(); return; }
            TacticalUI.Heading(definition.DisplayName); GUILayout.Label(definition.Description);
            GUILayout.Label($"REWARD   ${definition.RewardMoney} / {definition.RewardXP} XP");
            switch (progress.state)
            {
                case QuestState.Available:
                    bool otherActive=Hub.Flow.Profile.Quests.Any(q=>q.questId!=definition.Id && (q.state==QuestState.Active || q.state==QuestState.ReadyToTurnIn));
                    GUI.enabled=!otherActive;
                    if (GUILayout.Button("ACCEPT") && Hub.Flow.EnableMission(definition)) Hub.AcceptMission();
                    GUI.enabled=true;
                    if(otherActive) GUILayout.Label("Finish and turn in your active assignment before accepting another.");
                    break;
                case QuestState.Active:
                    GUILayout.Label($"ACTIVE MISSION\n{definition.Description}\nProgress: {progress.count} / {definition.RequiredCount}\nExtract successfully");
                    break;
                case QuestState.ReadyToTurnIn:
                    GUILayout.Label("READY TO TURN IN");
                    GUILayout.Label($"Reward: +${definition.RewardMoney} / +{definition.RewardXP} XP");
                    if (GUILayout.Button("TURN IN")) Hub.TurnInMission();
                    break;
                case QuestState.Completed: case QuestState.Claimed: GUILayout.Label("Assignment complete."); break;
            }
            GUILayout.Label($"Money: ${Hub.Flow.Profile.Wallet.Balance} | XP: {Hub.Flow.Profile.XP}");
            GUILayout.EndVertical(); GUILayout.EndHorizontal();
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
                TacticalUI.ItemCard(stack.Item,stack.Quantity,selectedItem==stack.Item);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Inspect")) { selectedItem=stack.Item; inspected = TacticalUI.Name(stack.Item.DisplayName) + " | " + stack.Item.Description; }
                foreach (LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
                    if (Fits(slot, stack.Item) && GUILayout.Button("Equip " + slot)) { Hub.Equip(slot, stack.Item); break; }
                GUILayout.EndHorizontal();
                TacticalUI.EndCard();
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
                TacticalUI.BeginCard(slot.ToString().ToUpperInvariant(),stack == null ? "EMPTY SLOT" : TacticalUI.Name(stack.Item.DisplayName) + " ×" + stack.Quantity,stack == null ? Color.gray : RarityPalette.Color(stack.Item.Rarity));
                if (editable && stack != null && GUILayout.Button("UNEQUIP " + slot)) Hub.Change(f => f.Unequip(slot));
                TacticalUI.EndCard();
            }
        }
        private void DrawLoadout(PlayerProfile profile)
        {
            DrawStash(profile);
        }
        private void DrawTrader(PlayerProfile profile)
        {
            GUILayout.BeginHorizontal(); if (GUILayout.Button(sell ? "BUY" : "[ BUY ]")) sell = false; if (GUILayout.Button(sell ? "[ SELL ]" : "SELL")) sell = true; GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(tradeFeedback)) GUILayout.Label(tradeFeedback);
            GUILayout.BeginHorizontal(); GUILayout.BeginVertical(GUILayout.Width(620));
            if (!sell)
                foreach (var item in Hub.TraderStock)
                {
                    int quantity = item.Type == ItemType.Ammo ? 30 : 1;
                    TacticalUI.ItemCard(item,quantity,selectedItem==item);
                    if (GUILayout.Button("DETAILS")) selectedItem=item;
                    if (GUILayout.Button($"Buy {TacticalUI.Name(item.DisplayName)} x{quantity} | ${item.BuyValue * quantity}"))
                    {
                        if (!profile.Wallet.CanAfford(item.BuyValue * quantity)) tradeFeedback = "Insufficient funds.";
                        else if (!profile.Stash.Copy().AddItem(item, quantity)) tradeFeedback = "Stash full. Free storage space before buying.";
                        else tradeFeedback = Hub.Change(f => f.Buy(item.Id, quantity)) ? "Purchased " + TacticalUI.Name(item.DisplayName) + " ×" + quantity : Hub.Message;
                    }
                    TacticalUI.EndCard();
                }
            else foreach (var stack in profile.Stash.Slots.ToArray())
            {
                if (stack.Item.SellValue <= 0 || stack.Item.Type == ItemType.QuestItem) continue;
                TacticalUI.ItemCard(stack.Item,stack.Quantity,selectedItem==stack.Item);
                if (GUILayout.Button("DETAILS")) selectedItem=stack.Item;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"Sell 1 (${stack.Item.SellValue})")) tradeFeedback = Hub.Change(f => f.Sell(stack.Item.Id, 1)) ? "Sold " + TacticalUI.Name(stack.Item.DisplayName) + " ×1" : Hub.Message;
                if (GUILayout.Button($"Sell stack (${stack.Item.SellValue * stack.Quantity})")) tradeFeedback = Hub.Change(f => f.Sell(stack.Item.Id, stack.Quantity)) ? "Sold " + TacticalUI.Name(stack.Item.DisplayName) + " ×" + stack.Quantity : Hub.Message;
                GUILayout.EndHorizontal();
                TacticalUI.EndCard();
            }
            GUILayout.EndVertical(); GUILayout.BeginVertical(GUILayout.Width(315));
            TacticalUI.Heading("ITEM DETAILS");
            if (selectedItem != null) { TacticalUI.ItemCard(selectedItem,1,true); GUILayout.Label(selectedItem.Description); TacticalUI.EndCard(); }
            else GUILayout.Label("Select a card to inspect.");
            GUILayout.EndVertical(); GUILayout.EndHorizontal();
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
                TacticalUI.BeginCard(slot.ToString().ToUpperInvariant(),TacticalUI.Name(state.Attachments[slot]?.DisplayName ?? "EMPTY SLOT"),TacticalUI.Accent,state.Attachments[slot]!=null);
                GUILayout.BeginHorizontal();
                if (TacticalUI.SelectButton("None",state.Attachments[slot]==null)) Hub.Configure(selectedWeapon, slot, null);
                foreach (var attachment in Hub.Attachments)
                    if (attachment.slot == slot && attachment.IsCompatible(weapon) && TacticalUI.SelectButton(TacticalUI.Name(attachment.DisplayName),state.Attachments[slot]==attachment)) Hub.Configure(selectedWeapon, slot, attachment);
                GUILayout.EndHorizontal();
                TacticalUI.EndCard();
            }
        }
    }
}
