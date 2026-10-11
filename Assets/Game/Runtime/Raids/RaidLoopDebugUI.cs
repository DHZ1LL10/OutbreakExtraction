using System.Linq;
using Outbreak.Combat;
using Outbreak.Core;
using Outbreak.Interaction;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Loot;
using Outbreak.Player;
using UnityEngine;

namespace Outbreak.Raids
{
    // Functional IMGUI for RaidLoop_Test only. Production UI can replace it without changing raid/domain APIs.
    [DisallowMultipleComponent]
    public sealed class RaidLoopDebugUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private RaidRuntimeContext context;
        [SerializeField] private PlayerHealth health;
        private PlayerInput playerInput;
        private bool inventoryOpen;
        private string feedback;
        private float feedbackUntil;

        private void Awake()
        {
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (context == null) context = FindFirstObjectByType<RaidRuntimeContext>();
            if (health == null) health = GetComponent<PlayerHealth>();
            playerInput = GetComponent<PlayerInput>();
        }
        private void Update()
        {
            if (((health != null && !health.IsAlive) || context?.Flow?.State != GameState.Raid) && (inventoryOpen || interactor.ActivePanel != null))
            {
                if (interactor.ActivePanel != null) interactor.ClosePanel();
                if (inventoryOpen) SetInventory(false);
            }
            if (Input.GetKeyDown(KeyCode.Tab) && health != null && health.IsAlive && context?.Flow?.State == GameState.Raid)
            {
                if (interactor.ActivePanel != null) interactor.ClosePanel();
                else SetInventory(!inventoryOpen);
            }
            if (Input.GetKeyDown(KeyCode.Escape) && (inventoryOpen || interactor.ActivePanel != null))
            {
                if (interactor.ActivePanel != null) interactor.ClosePanel();
                if (inventoryOpen) SetInventory(false);
            }
            if (Input.GetKeyDown(KeyCode.H) && context?.Flow != null &&
                (context.Flow.State == GameState.RaidSuccess || context.Flow.State == GameState.RaidFailed))
                context.Flow.ReturnToHub();
        }
        private void SetInventory(bool value)
        {
            inventoryOpen = value;
            playerInput?.SetGameplaySuppressed(this, value);
        }
        private void OnDisable() { if (inventoryOpen) SetInventory(false); }
        private void Message(string value) { feedback = value; feedbackUntil = Time.unscaledTime + 2f; }
        private void OnGUI()
        {
            var flow = context?.Flow;
            if (flow == null) return;
            DrawStatus(flow);
            if (flow.State == GameState.Raid)
                foreach (var zone in FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None))
                {
                    if (zone.IsCountingDown) GUI.Box(new Rect(Screen.width / 2f - 125, 120, 250, 80), $"EXTRACTION\nHOLD POSITION\n{zone.Remaining:0.0}");
                    else if (zone.CancelledRecently) GUI.Box(new Rect(Screen.width / 2f - 125, 120, 250, 35), "EXTRACTION CANCELLED");
                }
            if (interactor.ActivePanel is LootContainer container) DrawContainer(container, flow.Session);
            else if (inventoryOpen) DrawInventory(flow.Session);
            if (flow.State == GameState.RaidSuccess) DrawResult();
            else if (flow.State == GameState.RaidFailed) DrawFailure();
        }
        private void DrawStatus(GameFlow flow)
        {
            GUI.Box(new Rect(12, 12, 355, 112), "RAID LOOP TEST");
            string hp = health == null ? "--" : $"{health.CurrentHealth:0} / {health.MaxHealth:0}";
            int used = flow.Session?.FoundLoot.Count ?? 0, capacity = flow.Session?.LootCapacity ?? 0;
            GUI.Label(new Rect(24, 38, 330, 80), $"HP: {hp}\nLoot: {used} / {capacity} slots\nTab: inventory | E: interact / lean right\nExtraction: hold position | F1-F4 attachments");
            if (interactor.ActivePanel == null && !inventoryOpen && interactor.Focused != null)
            {
                string label = "[E] " + interactor.Focused.Prompt;
                if (interactor.Focused is IHoldInteractable)
                    label += interactor.HoldProgress > 0 ? $"  {interactor.HoldProgress * 100:0}%" : " (hold)";
                GUI.Box(new Rect(Screen.width / 2f - 115, Screen.height - 115, 230, 34), label);
            }
            if (Time.unscaledTime < feedbackUntil) GUI.Box(new Rect(Screen.width / 2f - 170, 18, 340, 34), feedback);
        }
        private void DrawContainer(LootContainer container, RaidSession session)
        {
            var items = container.Inventory.Contents;
            float height = Mathf.Max(210, 125 + items.Count * 32);
            Rect panel = new Rect(Screen.width / 2f - 260, Screen.height / 2f - height / 2, 520, height);
            GUI.Box(panel, container.PanelTitle);
            GUI.Label(new Rect(panel.x + 18, panel.y + 28, 460, 24), items.Count == 0 ? "Empty" : "Select an item or take everything that fits.");
            for (int i = 0; i < items.Count; i++)
            {
                var stack = items[i]; float y = panel.y + 58 + i * 32;
                Color previous = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                GUI.Label(new Rect(panel.x + 20, y, 350, 28), $"{stack.Item.Rarity,-10} {stack.Item.DisplayName} x{stack.Quantity}");
                GUI.color = previous;
                if (GUI.Button(new Rect(panel.x + 395, y, 100, 25), "Take"))
                {
                    if (!container.TryTake(i, session)) Message("Backpack full");
                    else Message(stack.Item.Rarity >= ItemRarity.Rare ? $"Secured {stack.Item.Rarity}: {stack.Item.DisplayName}" : "Item added");
                    break;
                }
            }
            if (GUI.Button(new Rect(panel.x + 20, panel.yMax - 42, 150, 28), "Take All"))
            {
                int count = container.TryTakeAll(session); Message(count > 0 ? $"Took {count} stack(s)" : "Backpack full or container empty");
            }
            if (GUI.Button(new Rect(panel.xMax - 120, panel.yMax - 42, 100, 28), "Close")) interactor.ClosePanel();
        }
        private void DrawInventory(RaidSession session)
        {
            Rect panel = new Rect(Screen.width / 2f - 300, Screen.height / 2f - 270, 600, 540);
            GUI.Box(panel, "RAID INVENTORY");
            if (session == null) { GUI.Label(new Rect(panel.x + 20, panel.y + 35, 400, 25), "No active raid."); return; }
            GUI.Label(new Rect(panel.x + 20, panel.y + 35, 220, 24), "EQUIPMENT");
            float y = panel.y + 62;
            var equipment = session.Equipment;
            foreach (LoadoutSlot slot in new[] { LoadoutSlot.Primary, LoadoutSlot.Secondary, LoadoutSlot.Armor, LoadoutSlot.Rig, LoadoutSlot.Backpack })
            {
                var stack = equipment.Get(slot);
                GUI.Label(new Rect(panel.x + 25, y, 280, 22), $"{slot}: {(stack == null ? "Empty" : stack.Item.DisplayName)}"); y += 23;
            }
            y += 12; GUI.Label(new Rect(panel.x + 20, y, 300, 24), $"LOOT  {session.FoundLoot.Count} / {session.LootCapacity}"); y += 30;
            for (int i = 0; i < session.FoundLoot.Count; i++)
            {
                ItemStack stack = session.FoundLoot[i]; Color previous = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                GUI.Label(new Rect(panel.x + 25, y, 400, 24), $"{stack.Item.Rarity,-10} {stack.Item.DisplayName} x{stack.Quantity}"); GUI.color = previous;
                if (GUI.Button(new Rect(panel.xMax - 115, y, 90, 23), "Discard"))
                {
                    session.RemoveLoot(stack.Item.Id, stack.Quantity); Message("Discarded (debug: no world drop)"); break;
                }
                y += 27;
            }
            if (GUI.Button(new Rect(panel.xMax - 120, panel.yMax - 40, 100, 27), "Close")) SetInventory(false);
        }
        private void DrawResult()
        {
            var zone = FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None).FirstOrDefault(x => x.Succeeded);
            Rect panel = new Rect(Screen.width / 2f - 230, Screen.height / 2f - 190, 460, 380);
            GUI.Box(panel, "RAID SUCCESS");
            if (zone != null)
            {
                GUI.Label(new Rect(panel.x + 22, panel.y + 35, 410, 24), $"Loot extracted — estimated value ${zone.EstimatedValue}");
                float y = panel.y + 68;
                foreach (var stack in zone.Extracted)
                {
                    Color previous = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                    GUI.Label(new Rect(panel.x + 28, y, 390, 23), $"{stack.Item.Rarity,-10} {stack.Item.DisplayName} x{stack.Quantity}");
                    GUI.color = previous; y += 25;
                }
                GUI.Label(new Rect(panel.x + 22, panel.yMax - 96, 410, 24), $"Stash after save: {context.Flow.Profile.Stash.Slots.Count} / {context.Flow.Profile.Stash.Capacity} slots");
                GUI.Label(new Rect(panel.x + 22, panel.yMax - 72, 410, 24), $"Raid time: {zone.RaidDuration:0.0}s");
            }
            GUI.Label(new Rect(panel.x + 22, panel.yMax - 45, 410, 25), "H — Return to Hub (logical/debug)");
        }
        private void DrawFailure()
        {
            GUI.Box(new Rect(Screen.width / 2f - 180, Screen.height / 2f + 80, 360, 70), "RAID FAILED\nLoot and deployed equipment lost\nH — Return to Hub");
        }
    }
}
