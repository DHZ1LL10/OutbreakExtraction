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
    // Presentation screens consume the same raid/domain APIs as the original diagnostic adapter.
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
        private bool loadoutTab;
        private Vector2 inventoryScroll, containerScroll;
        private string inspected;

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
                ReturnToHub();
        }
        private void ReturnToHub()
        {
            if (Outbreak.Hub.HubSceneFlow.Instance != null)
            {
                if (!Outbreak.Hub.HubSceneFlow.Instance.ReturnToHub()) Message(Outbreak.Hub.HubSceneFlow.Instance.Message);
            }
            else context.Flow.ReturnToHub();
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
            using (new TacticalUI.Scope())
            {
            DrawStatus(flow);
            if (flow.State == GameState.Raid && flow.MissionProgress?.state == Outbreak.Quests.QuestState.Active)
            {
                var mission = flow.Mission; var progress = flow.MissionProgress;
                string instruction = progress.count >= mission.RequiredCount ? "EXTRACT" : "RECOVER";
                if (!inventoryOpen && interactor.ActivePanel == null)
                    GUI.Box(new Rect(920, 20, 340, 78), $"{mission.DisplayName}\nSupplies {progress.count}/{mission.RequiredCount} · {instruction}");
            }
            if (flow.State == GameState.Raid)
                foreach (var zone in FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None))
                {
                    if (zone.IsCountingDown) GUI.Box(new Rect(490, 120, 300, 96), $"EXTRACTION\nHOLD POSITION\n{zone.Remaining:0.0}");
                    else if (zone.CancelledRecently) TacticalUI.Prompt("EXTRACTION CANCELLED", 120);
                }
            if (interactor.ActivePanel is LootContainer container) DrawContainer(container, flow.Session);
            else if (inventoryOpen) DrawInventory(flow.Session);
            if (flow.State == GameState.RaidSuccess) DrawResult();
            else if (flow.State == GameState.RaidFailed) DrawFailure();
            }
        }
        private void DrawStatus(GameFlow flow)
        {
            if (PresentationMode.ShowDiagnostics)
            {
            GUI.Box(new Rect(12, 255, 355, 112), "RAID DIAGNOSTICS");
            string hp = health == null ? "--" : $"{health.CurrentHealth:0} / {health.MaxHealth:0}";
            int used = flow.Session?.FoundLoot.Count ?? 0, capacity = flow.Session?.LootCapacity ?? 0;
            GUI.Label(new Rect(24, 280, 330, 80), $"HP: {hp}\nLoot: {used} / {capacity} slots\nTab: inventory | E: interact / lean right\nExtraction: hold position");
            }
            if (interactor.ActivePanel == null && !inventoryOpen && interactor.Focused != null)
            {
                string label = "[E] " + interactor.Focused.Prompt;
                if (interactor.Focused is IHoldInteractable)
                    label += interactor.HoldProgress > 0 ? $"  {interactor.HoldProgress * 100:0}%" : " (hold)";
                TacticalUI.Prompt(label);
            }
            if (Time.unscaledTime < feedbackUntil) TacticalUI.Prompt(feedback, 580);
        }
        private void DrawContainer(LootContainer container, RaidSession session)
        {
            var items = container.Inventory.Contents;
            TacticalUI.BeginPanel(container.PanelTitle, $"FIELD STORAGE     /     BACKPACK {session.FoundLoot.Count} : {session.LootCapacity}");
            containerScroll = GUILayout.BeginScrollView(containerScroll);
            GUILayout.Label(items.Count == 0 ? "Container empty" : "Recover supplies. Only extracted loot reaches your stash.");
            for (int i = 0; i < items.Count; i++)
            {
                var stack = items[i];
                GUILayout.BeginHorizontal();
                Color previous = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                GUILayout.Label($"{stack.Item.Rarity}   /   {TacticalUI.Name(stack.Item.DisplayName)} ×{stack.Quantity}");
                GUI.color = previous;
                bool take = GUILayout.Button("TAKE", GUILayout.Width(130));
                GUILayout.EndHorizontal();
                if (take)
                {
                    if (!container.TryTake(i, session)) Message("Backpack full");
                    else Message(stack.Item.Rarity >= ItemRarity.Rare ? $"Secured {stack.Item.Rarity}: {TacticalUI.Name(stack.Item.DisplayName)}" : "Item added");
                    break;
                }
            }
            GUILayout.EndScrollView();
            if (Time.unscaledTime < feedbackUntil) GUILayout.Label(feedback);
            if (GUILayout.Button("TAKE ALL THAT FITS"))
            {
                int count = container.TryTakeAll(session); Message(count > 0 ? $"Took {count} stack(s)" : "Backpack full or container empty");
            }
            if (GUILayout.Button("CLOSE    /    ESC · TAB")) interactor.ClosePanel();
            TacticalUI.EndPanel();
        }
        private void DrawInventory(RaidSession session)
        {
            if (session == null) return;
            TacticalUI.BeginPanel("FIELD KIT", $"BACKPACK {session.FoundLoot.Count} : {session.LootCapacity}     /     EXTRACT TO SECURE YOUR LOOT");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("INVENTORY")) loadoutTab = false;
            if (GUILayout.Button("LOADOUT")) loadoutTab = true;
            GUILayout.EndHorizontal();
            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll);
            var equipment = session.Equipment;
            if (loadoutTab)
            foreach (LoadoutSlot slot in System.Enum.GetValues(typeof(LoadoutSlot)))
            {
                var stack = equipment.Get(slot);
                GUILayout.Label($"{slot.ToString().ToUpperInvariant()}   /   {(stack == null ? "Empty" : TacticalUI.Name(stack.Item.DisplayName))}");
            }
            else
            {
            if (session.FoundLoot.Count == 0) GUILayout.Label("Backpack empty. Search containers and recover supplies.");
            for (int i = 0; i < session.FoundLoot.Count; i++)
            {
                ItemStack stack = session.FoundLoot[i]; Color previous = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{stack.Item.Rarity}   /   {TacticalUI.Name(stack.Item.DisplayName)} ×{stack.Quantity}"); GUI.color = previous;
                if (GUILayout.Button("INSPECT", GUILayout.Width(130))) inspected = TacticalUI.Name(stack.Item.DisplayName) + " — " + stack.Item.Description;
                bool discard = GUILayout.Button("DISCARD", GUILayout.Width(130));
                GUILayout.EndHorizontal();
                if (discard)
                {
                    session.RemoveLoot(stack.Item.Id, stack.Quantity); Message("Item discarded"); break;
                }
            }
            }
            GUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(inspected)) GUILayout.Label(inspected);
            if (Time.unscaledTime < feedbackUntil) GUILayout.Label(feedback);
            if (GUILayout.Button("CLOSE    /    ESC · TAB")) SetInventory(false);
            TacticalUI.EndPanel();
        }
        private void DrawResult()
        {
            var zone = FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None).FirstOrDefault(x => x.Succeeded);
            Rect panel = new Rect(370, 120, 540, 480);
            GUI.Box(panel, "RAID SUCCESS");
            if (zone != null)
            {
                GUI.Label(new Rect(panel.x + 22, panel.y + 35, 410, 24), $"Loot extracted — estimated value ${zone.EstimatedValue}");
                float y = panel.y + 68;
                foreach (var stack in zone.Extracted)
                {
                    Color previous = GUI.color; GUI.color = RarityPalette.Color(stack.Item.Rarity);
                    GUI.Label(new Rect(panel.x + 28, y, 390, 23), $"{stack.Item.Rarity,-10} {TacticalUI.Name(stack.Item.DisplayName)} x{stack.Quantity}");
                    GUI.color = previous; y += 25;
                }
                GUI.Label(new Rect(panel.x + 22, panel.yMax - 96, 410, 24), $"Stash after save: {context.Flow.Profile.Stash.Slots.Count} / {context.Flow.Profile.Stash.Capacity} slots");
                GUI.Label(new Rect(panel.x + 22, panel.yMax - 72, 410, 24), $"Raid time: {zone.RaidDuration:0.0}s");
            }
            if (Outbreak.Hub.HubSceneFlow.Instance != null)
            { if (GUI.Button(new Rect(panel.x + 22, panel.yMax - 45, 410, 27), "Return To Hub (H)")) ReturnToHub(); }
            else GUI.Label(new Rect(panel.x + 22, panel.yMax - 45, 410, 25), "H — Return to Hub (logical/debug)");
        }
        private void DrawFailure()
        {
            // Result actions must remain readable above the completed bodycam death fade.
            int depth = GUI.depth; GUI.depth = -10001;
            GUI.Box(new Rect(390, 410, 500, 170), "RAID FAILED\nLoot and deployed equipment lost");
            if (Outbreak.Hub.HubSceneFlow.Instance != null && GUI.Button(new Rect(420, 505, 440, 48), "RETURN TO HUB    /    H")) ReturnToHub();
            GUI.depth = depth;
        }
    }
}
