using System;
using System.Linq;
using Outbreak.Core;
using Outbreak.Interaction;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Loot;
using Outbreak.Weapons;
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
        private HubSceneFlow Hub => HubSceneFlow.Instance;
        private void Awake() { interactor = GetComponent<PlayerInteractor>(); }
        private void OnGUI()
        {
            var hub = Hub; if (hub == null || hub.Flow == null) return;
            var profile = hub.Flow.Profile;
            GUI.Box(new Rect(12, 12, 400, 82), $"HUB | ${profile.Wallet.Balance}\nStash {profile.Stash.Slots.Count}/{profile.Stash.Capacity}\n{hub.Message}");
            if (!(interactor.ActivePanel is HubStation station))
            {
                if (interactor.Focused != null) GUI.Box(new Rect(Screen.width / 2f - 160, Screen.height - 70, 320, 32), "[E] " + interactor.Focused.Prompt);
                return;
            }
            float width = Mathf.Min(720, Screen.width - 24), height = Mathf.Min(560, Screen.height - 110);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2 + 35, width, height), station.PanelTitle, GUI.skin.window);
            scroll = GUILayout.BeginScrollView(scroll);
            switch (station.Kind)
            {
                case HubStationKind.Stash: DrawStash(profile); break;
                case HubStationKind.Loadout: DrawLoadout(profile); break;
                case HubStationKind.Trader: DrawTrader(profile); break;
                case HubStationKind.Workbench: DrawWorkbench(profile); break;
                case HubStationKind.Missions: GUILayout.Label("No active assignment"); break;
                case HubStationKind.Deploy:
                    GUILayout.Label("Quarantine Outskirts | Difficulty: Debug/Normal");
                    DrawEquipment(profile, false);
                    GUI.enabled = HubLoadoutAdapter.CanDeploy(hub.Flow, hub.Weapons, hub.Attachments);
                    if (GUILayout.Button("START RAID")) hub.Deploy();
                    GUI.enabled = true; break;
            }
            GUILayout.EndScrollView();
            GUILayout.Label(hub.Message);
            if (GUILayout.Button("Close (Escape)")) interactor.ClosePanel();
            GUILayout.EndArea();
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
            GUILayout.Label($"STASH {profile.Stash.Slots.Count} / {profile.Stash.Capacity}");
            if (!string.IsNullOrEmpty(inspected)) GUILayout.Label(inspected);
            foreach (var stack in profile.Stash.Slots.ToArray())
            {
                Color color = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                GUILayout.Label($"{stack.Item.DisplayName} x{stack.Quantity} | {stack.Item.Rarity} | sell ${stack.Item.SellValue}/unit"); GUI.color = color;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Inspect")) inspected = stack.Item.DisplayName + " | " + stack.Item.Description;
                foreach (LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
                    if (Fits(slot, stack.Item) && GUILayout.Button("Equip " + slot)) { Hub.Equip(slot, stack.Item); break; }
                GUILayout.EndHorizontal();
            }
            DrawEquipment(profile, true);
        }
        private void DrawEquipment(PlayerProfile profile, bool editable)
        {
            foreach (LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
            {
                var stack = profile.Loadout.Get(slot);
                GUILayout.BeginHorizontal(); GUILayout.Label(slot + ": " + (stack == null ? "Empty" : stack.Item.DisplayName + " x" + stack.Quantity));
                if (editable && stack != null && GUILayout.Button("Return to Stash", GUILayout.Width(145))) Hub.Change(f => f.Unequip(slot));
                GUILayout.EndHorizontal();
            }
        }
        private void DrawLoadout(PlayerProfile profile)
        {
            DrawEquipment(profile, true);
            foreach (LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
                foreach (var stack in profile.Stash.Slots.ToArray())
                    if (Fits(slot, stack.Item) && GUILayout.Button($"{slot} <- {stack.Item.DisplayName}")) Hub.Equip(slot, stack.Item);
        }
        private void DrawTrader(PlayerProfile profile)
        {
            GUILayout.BeginHorizontal(); if (GUILayout.Button("BUY")) sell = false; if (GUILayout.Button("SELL")) sell = true; GUILayout.EndHorizontal();
            if (!sell)
                foreach (var item in Hub.TraderStock)
                {
                    int quantity = item.Type == ItemType.Ammo ? 30 : 1;
                    if (GUILayout.Button($"Buy {item.DisplayName} x{quantity} | ${item.BuyValue * quantity}")) Hub.Change(f => f.Buy(item.Id, quantity));
                }
            else foreach (var stack in profile.Stash.Slots.ToArray())
            {
                if (stack.Item.SellValue <= 0 || stack.Item.Type == ItemType.QuestItem) continue;
                GUILayout.BeginHorizontal(); GUILayout.Label(stack.Item.DisplayName + " x" + stack.Quantity);
                if (GUILayout.Button($"Sell 1 (${stack.Item.SellValue})")) Hub.Change(f => f.Sell(stack.Item.Id, 1));
                if (GUILayout.Button($"Sell stack (${stack.Item.SellValue * stack.Quantity})")) Hub.Change(f => f.Sell(stack.Item.Id, stack.Quantity));
                GUILayout.EndHorizontal();
            }
        }
        private void DrawWorkbench(PlayerProfile profile)
        {
            GUILayout.Label("Debug attachments are available here without purchase.");
            GUILayout.BeginHorizontal(); if (GUILayout.Button("Primary")) selectedWeapon = LoadoutSlot.Primary; if (GUILayout.Button("Secondary")) selectedWeapon = LoadoutSlot.Secondary; GUILayout.EndHorizontal();
            var item = profile.Loadout.Get(selectedWeapon)?.Item;
            var weapon = Hub.Weapons.FirstOrDefault(w => w.WeaponId == item?.Id);
            if (weapon == null) { GUILayout.Label("Equip a weapon first."); return; }
            var state = new WeaponRuntimeState(weapon, 0, 0);
            if (!HubLoadoutAdapter.ApplyPreset(state, profile, Hub.Attachments)) { GUILayout.Label("Invalid preset."); return; }
            GUILayout.Label(weapon.DisplayName);
            foreach (AttachmentSlot slot in Enum.GetValues(typeof(AttachmentSlot)))
            {
                GUILayout.Label(slot + ": " + (state.Attachments[slot]?.DisplayName ?? "None"));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("None")) Hub.Configure(selectedWeapon, slot, null);
                foreach (var attachment in Hub.Attachments)
                    if (attachment.slot == slot && attachment.IsCompatible(weapon) && GUILayout.Button(attachment.DisplayName)) Hub.Configure(selectedWeapon, slot, attachment);
                GUILayout.EndHorizontal();
            }
        }
    }
}
