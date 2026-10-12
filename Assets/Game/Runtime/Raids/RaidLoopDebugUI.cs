using System.Linq;
using Outbreak.Combat;
using Outbreak.Core;
using Outbreak.Interaction;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loot;
using Outbreak.Player;
using Outbreak.Quests;
using UnityEngine;
using UnityEngine.UI;

namespace Outbreak.Raids
{
    [DisallowMultipleComponent]
    public sealed class RaidLoopDebugUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private RaidRuntimeContext context;
        [SerializeField] private PlayerHealth health;
        private PlayerInput playerInput;
        private Canvas modal,hud;
        private Text prompt,mission,extraction,feedbackLabel;
        private Image extractionBar;
        private GameObject extractionPanel;
        private ItemPreviewRenderer preview;
        private ExtractionZone[] zones;
        private bool inventoryOpen,dirty=true;
        private object shownPanel;
        private ItemStack selected;
        private string feedback;
        private float feedbackUntil,raidTime;
        private int lostItems,lostEquipment;
        private GameState? lastState;
        public bool ResultVisible=>modal!=null && modal.gameObject.activeInHierarchy &&
            (lastState==GameState.RaidSuccess || lastState==GameState.RaidFailed);
        private void Awake()
        {
            if(interactor==null) interactor=GetComponent<PlayerInteractor>();
            if(context==null) context=FindFirstObjectByType<RaidRuntimeContext>();
            if(health==null) health=GetComponent<PlayerHealth>();
            playerInput=GetComponent<PlayerInput>(); preview=gameObject.AddComponent<ItemPreviewRenderer>();
            modal=GameUI.Canvas(transform,"Raid screens",200); modal.gameObject.SetActive(false);
            hud=GameUI.Canvas(transform,"Raid HUD",30); zones=FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None);
            prompt=GameUI.Label(hud.transform,new Rect(0,0,800,65),"",22,null,true); prompt.alignment=TextAnchor.MiddleCenter;
            var r=prompt.rectTransform; r.anchorMin=r.anchorMax=new Vector2(.5f,0); r.pivot=new Vector2(.5f,0); r.anchoredPosition=new Vector2(0,55);
            mission=GameUI.Label(hud.transform,new Rect(0,0,440,100),"",20,null,true);
            r=mission.rectTransform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,1); r.anchoredPosition=new Vector2(-38,-35);
            extractionPanel=GameUI.Box(hud.transform,new Rect(0,0,440,115),GameUI.Panel).gameObject;
            r=(RectTransform)extractionPanel.transform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1); r.anchoredPosition=new Vector2(0,-150);
            extraction=GameUI.Label(r,new Rect(25,15,390,52),"",24,null,true); extraction.alignment=TextAnchor.MiddleCenter;
            GameUI.Box(r,new Rect(25,85,390,7),GameUI.CardColor);
            extractionBar=GameUI.Box(r,new Rect(25,85,390,7),GameUI.Success);
            feedbackLabel=GameUI.Label(hud.transform,new Rect(0,0,800,45),"",20,GameUI.Accent,true); feedbackLabel.alignment=TextAnchor.MiddleCenter;
            r=feedbackLabel.rectTransform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,0); r.anchoredPosition=new Vector2(0,140);
        }
        private void Update()
        {
            var flow=context?.Flow; if(flow==null) return;
            bool alive=health==null || health.IsAlive;
            if((!alive || flow.State!=GameState.Raid) && (inventoryOpen || interactor.ActivePanel!=null)) Close();
            if(Input.GetKeyDown(KeyCode.Tab) && alive && flow.State==GameState.Raid)
            { if(interactor.ActivePanel!=null) interactor.ClosePanel(); else SetInventory(!inventoryOpen); }
            if(Input.GetKeyDown(KeyCode.Escape) && (inventoryOpen || interactor.ActivePanel!=null)) Close();
            if(Input.GetKeyDown(KeyCode.H) && (flow.State==GameState.RaidSuccess || flow.State==GameState.RaidFailed)) ReturnToHub();
            if(flow.State==GameState.Raid)
            {
                raidTime=context.ElapsedRaidTime; lostItems=flow.Session?.FoundLoot.Count??0;
                lostEquipment=flow.Session?.Equipment.Entries.Count()??0;
            }
            bool result=flow.State==GameState.RaidSuccess || flow.State==GameState.RaidFailed;
            object panel=result?(object)flow.State:inventoryOpen?(object)this:interactor.ActivePanel;
            bool open=panel!=null;
            hud.gameObject.SetActive(!open && flow.State==GameState.Raid && alive); modal.gameObject.SetActive(open);
            if(result && lastState!=flow.State) { dirty=true; preview.Hide(); }
            if(!result && !ReferenceEquals(panel,shownPanel)) { dirty=true; selected=null; }
            shownPanel=panel; lastState=flow.State;
            if(open && dirty) { dirty=false; Build(flow); }
            if(!open) preview.Hide();
            UpdateHUD(flow);
        }
        private void UpdateHUD(GameFlow flow)
        {
            prompt.text=interactor.Focused==null?"":"[ E ]  "+interactor.Focused.Prompt.ToUpperInvariant();
            if(interactor.Focused is IHoldInteractable) prompt.text+=interactor.HoldProgress>0?$"   {interactor.HoldProgress*100:0}%":"  · HOLD";
            var progress=flow.MissionProgress; var definition=flow.Mission;
            mission.text=progress?.state==QuestState.Active?$"{definition.DisplayName.ToUpperInvariant()}\n{progress.count} / {definition.RequiredCount}  {definition.Type.ToString().ToUpperInvariant()}\nEXTRACTION REQUIRED":"";
            var zone=zones.FirstOrDefault(z=>z!=null && z.IsCountingDown);
            bool cancelled=zones.Any(z=>z!=null && z.CancelledRecently);
            extractionPanel.SetActive(zone!=null || cancelled);
            extraction.text=zone!=null?$"EXTRACTING\n{zone.Remaining:0.0}s":"EXTRACTION CANCELLED";
            extractionBar.rectTransform.sizeDelta=new Vector2(zone!=null?390*zone.Progress:0,7);
            feedbackLabel.text=Time.unscaledTime<feedbackUntil?feedback:"";
        }
        private void SetInventory(bool value) { inventoryOpen=value; playerInput?.SetGameplaySuppressed(this,value); dirty=true; }
        private void Close() { if(interactor.ActivePanel!=null) interactor.ClosePanel(); if(inventoryOpen) SetInventory(false); preview.Hide(); dirty=true; }
        private void OnDisable() { if(inventoryOpen) SetInventory(false); if(modal!=null) modal.gameObject.SetActive(false); if(hud!=null) hud.gameObject.SetActive(false); preview?.Hide(); }
        private void OnDestroy() { if(modal!=null) Destroy(modal.gameObject); if(hud!=null) Destroy(hud.gameObject); }
        private void Message(string value) { feedback=value; feedbackUntil=Time.unscaledTime+3; dirty=true; }
        private void ReturnToHub()
        {
            if(Outbreak.Hub.HubSceneFlow.Instance!=null)
            { if(!Outbreak.Hub.HubSceneFlow.Instance.ReturnToHub()) Message(Outbreak.Hub.HubSceneFlow.Instance.Message); }
            else context.Flow.ReturnToHub();
        }
        private void Choose(ItemStack stack) { selected=stack; dirty=true; }
        private void Build(GameFlow flow)
        {
            GameUI.Clear(modal.transform); preview.Hide();
            if(flow.State==GameState.RaidSuccess || flow.State==GameState.RaidFailed) { Result(flow); return; }
            if(interactor.ActivePanel is LootContainer container) Container(container,flow.Session); else Inventory(flow.Session);
        }
        private void Inventory(RaidSession session)
        {
            if(session==null) return;
            var window=GameUI.Window(modal,"FIELD INVENTORY / LOADOUT",$"BACKPACK {session.FoundLoot.Count} / {session.LootCapacity}     /     EXTRACT TO SECURE YOUR LOOT",Close);
            selected=InventoryPresentation.Resolve(session.FoundLoot,selected);
            InventoryPresentation.Grid(window,new Rect(32,164,860,655),session.FoundLoot,selected,Choose);
            GameUI.Label(window,new Rect(918,140,340,30),"LOADOUT",20,null,true);
            InventoryPresentation.Loadout(window,new Rect(918,184,340,635),session.Equipment);
            var detail=InventoryPresentation.Detail(window,new Rect(1280,140,408,679),selected,preview);
            if(selected!=null) GameUI.Action(detail,new Rect(24,602,360,52),"DISCARD STACK",()=>{session.RemoveLoot(selected.Item.Id,selected.Quantity);selected=null;Message("ITEM DISCARDED");});
            GameUI.Label(window,new Rect(32,848,1500,30),Time.unscaledTime<feedbackUntil?feedback:"TAB / ESC · CLOSE",18,GameUI.Muted);
        }
        private void Container(LootContainer container,RaidSession session)
        {
            if(session==null) return;
            var window=GameUI.Window(modal,container.PanelTitle,$"FIELD STORAGE     /     BACKPACK {session.FoundLoot.Count} / {session.LootCapacity}",Close);
            var contents=container.Inventory.Contents;
            selected=InventoryPresentation.Resolve(contents,selected);
            InventoryPresentation.Grid(window,new Rect(32,150,1130,595),contents,selected,Choose,4);
            var detail=InventoryPresentation.Detail(window,new Rect(1190,140,498,679),selected,preview);
            if(selected!=null) GameUI.Action(detail,new Rect(24,602,450,52),"TAKE ITEM",()=> {
                int index=Enumerable.Range(0,contents.Count).First(i=>ReferenceEquals(contents[i],selected));
                Message(container.TryTake(index,session)?"ITEM SECURED":"BACKPACK FULL"); selected=null;
            });
            GameUI.Action(window,new Rect(32,768,1130,50),"TAKE ALL THAT FITS",()=>{int count=container.TryTakeAll(session); selected=null;Message(count>0?$"SECURED {count} STACKS":"BACKPACK FULL / CONTAINER EMPTY");});
            GameUI.Label(window,new Rect(32,848,1500,30),Time.unscaledTime<feedbackUntil?feedback:"ESC / TAB · CLOSE",18,GameUI.Accent);
        }
        private void Result(GameFlow flow)
        {
            bool success=flow.State==GameState.RaidSuccess;
            var window=GameUI.Window(modal,success?"RAID SUCCESS":"RAID FAILED",success?"EXTRACTION CONFIRMED":"OPERATOR LOST",ReturnToHub);
            var zone=zones.FirstOrDefault(z=>z!=null && z.Succeeded);
            var stats=GameUI.Rect(window,"Summary",new Rect(36,145,480,500));
            GameUI.Stat(stats,0,"RAID TIME",GameUI.Clock(success && zone!=null?zone.RaidDuration:raidTime),460);
            GameUI.Stat(stats,72,success?"LOOT VALUE":"ITEMS LOST",success && zone!=null?$"${zone.EstimatedValue:N0}":lostItems.ToString(),460,success?GameUI.Success:GameUI.Danger);
            GameUI.Stat(stats,144,success?"EXTRACTED STACKS":"DEPLOYED SLOTS LOST",success && zone!=null?zone.Extracted.Count.ToString():lostEquipment.ToString(),460);
            if(success)
            {
                GameUI.Stat(stats,230,"STASH",$"{flow.Profile.Stash.Slots.Count} / {flow.Profile.Stash.Capacity}",460);
                GameUI.Label(window,new Rect(565,145,1100,32),"EXTRACTED LOOT",24,null,true);
                if(zone!=null) InventoryPresentation.Grid(window,new Rect(565,200,1120,555),zone.Extracted,null,_=>{},4);
            }
            else
            {
                GameUI.Box(window,new Rect(565,145,1120,590),GameUI.CardColor);
                GameUI.Label(window,new Rect(615,250,1020,100),"DEPLOYED EQUIPMENT LOST",42,GameUI.Danger,true);
                GameUI.Label(window,new Rect(615,410,1020,100),"UNEXTRACTED LOOT LOST",30,GameUI.Muted,true);
            }
            GameUI.Action(window,new Rect(36,768,1649,65),"RETURN TO HUB  /  H",ReturnToHub);
        }
        private void OnGUI()
        {
            if(!PresentationMode.ShowDiagnostics || context?.Flow==null) return;
            using(new TacticalUI.Scope()) GUI.Box(new Rect(12,330,355,110),$"RAID DIAGNOSTICS\nHP: {health?.CurrentHealth:0}\n{context.Flow.State}");
        }
    }
}
