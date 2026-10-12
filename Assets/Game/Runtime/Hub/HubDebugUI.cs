using System;
using System.Linq;
using Outbreak.Core;
using Outbreak.Interaction;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Quests;
using Outbreak.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Outbreak.Hub
{
    // Presentation adapter. Domain transactions and persistence remain in HubSceneFlow.
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class HubDebugUI : MonoBehaviour
    {
        private PlayerInteractor interactor;
        private Outbreak.Player.PlayerInput input;
        private Canvas modal,hud;
        private Text prompt;
        private ItemPreviewRenderer preview;
        private bool inventoryOpen,sell,dirty=true;
        private object shownPanel;
        private ItemStack selected;
        private QuestDefinition assignment;
        private LoadoutSlot selectedWeapon=LoadoutSlot.Primary;
        private AttachmentSlot selectedSocket=AttachmentSlot.Optic;
        private string feedback;
        private HubSceneFlow Hub=>HubSceneFlow.Instance;
        private void Awake()
        {
            interactor=GetComponent<PlayerInteractor>(); input=GetComponent<Outbreak.Player.PlayerInput>();
            modal=GameUI.Canvas(transform,"Hub screens"); modal.gameObject.SetActive(false);
            hud=GameUI.Canvas(transform,"Hub interaction",20);
            prompt=GameUI.Label(hud.transform,new Rect(0,0,600,70),"",22,null,true);
            var r=prompt.rectTransform; r.anchorMin=r.anchorMax=new Vector2(.5f,0); r.pivot=new Vector2(.5f,0); r.anchoredPosition=new Vector2(0,65); prompt.alignment=TextAnchor.MiddleCenter;
            preview=gameObject.AddComponent<ItemPreviewRenderer>();
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Tab) && Hub!=null && Hub.Ready)
            { if(interactor.ActivePanel!=null) interactor.ClosePanel(); else SetInventory(!inventoryOpen); }
            if(inventoryOpen && (Input.GetKeyDown(KeyCode.Escape) || Hub==null || !Hub.Ready || Hub.Transitioning)) SetInventory(false);
            object panel=inventoryOpen ? (object)this : interactor.ActivePanel;
            bool open=panel!=null && Hub?.Ready==true;
            modal.gameObject.SetActive(open); hud.gameObject.SetActive(!open);
            prompt.text=interactor.Focused==null?"":"[ E ]\n"+interactor.Focused.Prompt.Replace("Open ","").ToUpperInvariant();
            if(Hub!=null && !Hub.Ready) prompt.text=Hub.Message;
            if(!open) { if(shownPanel!=null) preview.Hide(); shownPanel=null; return; }
            if(!ReferenceEquals(panel,shownPanel)) { selected=null; feedback=null; dirty=true; shownPanel=panel; }
            if(dirty) { dirty=false; Build(); }
        }
        private void SetInventory(bool value) { inventoryOpen=value; input?.SetGameplaySuppressed(this,value); dirty=true; }
        private void Close() { if(inventoryOpen) SetInventory(false); else interactor.ClosePanel(); preview.Hide(); }
        private void OnDisable() { if(inventoryOpen) SetInventory(false); if(modal!=null) modal.gameObject.SetActive(false); if(hud!=null) hud.gameObject.SetActive(false); preview?.Hide(); }
        private void OnEnable() { if(hud!=null) hud.gameObject.SetActive(true); dirty=true; }
        private void OnDestroy() { if(modal!=null) Destroy(modal.gameObject); if(hud!=null) Destroy(hud.gameObject); }
        private void Change(Func<bool> action) { bool success=action(); feedback=success?"SAVED":Hub.Message; dirty=true; }
        private void Choose(ItemStack stack) { selected=stack; dirty=true; }
        private void Build()
        {
            GameUI.Clear(modal.transform); preview.Hide();
            var profile=Hub.Flow.Profile; var station=interactor.ActivePanel as HubStation;
            var window=GameUI.Window(modal,inventoryOpen?"INVENTORY / LOADOUT":station?.PanelTitle??"FIELD KIT",$"FIELD SAFEHOUSE     /     ${profile.Wallet.Balance:N0}     /     {profile.XP} XP",Close);
            GameUI.Label(window,new Rect(32,848,1550,30),feedback??"TAB / ESC · CLOSE     |     SELECT A CARD TO INSPECT",18,feedback==null?GameUI.Muted:GameUI.Accent);
            if(inventoryOpen || station?.Kind==HubStationKind.Stash || station?.Kind==HubStationKind.Loadout) Stash(window,profile);
            else if(station?.Kind==HubStationKind.Trader) Trader(window,profile);
            else if(station?.Kind==HubStationKind.Workbench) Workbench(window,profile);
            else if(station?.Kind==HubStationKind.Missions) Missions(window);
            else if(station?.Kind==HubStationKind.Deploy) Deploy(window,profile);
        }
        private bool Fits(LoadoutSlot slot,ItemDefinition item)
        {
            if(!Loadout.Accepts(slot,item)) return false;
            if(slot!=LoadoutSlot.Primary && slot!=LoadoutSlot.Secondary) return true;
            var weapon=Hub.Weapons.FirstOrDefault(w=>w.WeaponId==item.Id);
            return weapon!=null && weapon.weaponClass==(slot==LoadoutSlot.Primary?WeaponClass.Rifle:WeaponClass.Pistol);
        }
        private void Stash(Transform window,PlayerProfile profile)
        {
            GameUI.Label(window,new Rect(32,136,850,30),"STASH",20,null,true);
            GameUI.Progress(window,new Rect(32,193,850,5),profile.Stash.Slots.Count/(float)profile.Stash.Capacity,$"USED {profile.Stash.Slots.Count} / {profile.Stash.Capacity}");
            selected=InventoryPresentation.Resolve(profile.Stash.Slots,selected);
            InventoryPresentation.Grid(window,new Rect(32,214,860,605),profile.Stash.Slots,selected,Choose);
            GameUI.Label(window,new Rect(918,140,340,30),"LOADOUT",20,null,true);
            InventoryPresentation.Loadout(window,new Rect(918,184,340,635),profile.Loadout,slot=>Change(()=>Hub.Change(f=>f.Unequip(slot))));
            var detail=InventoryPresentation.Detail(window,new Rect(1280,140,408,679),selected,preview);
            if(selected==null) return;
            var options=GameUI.Scroll(detail,new Rect(24,598,360,68),180,out _); float y=0;
            foreach(LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
                if(Fits(slot,selected.Item)) { var target=slot; GameUI.Action(options,new Rect(0,y,342,42),"EQUIP · "+slot.ToString().ToUpperInvariant(),()=>Change(()=>Hub.Equip(target,selected.Item))); y+=46; }
            options.sizeDelta=new Vector2(options.sizeDelta.x,Mathf.Max(68,y));
        }
        private void Trader(Transform window,PlayerProfile profile)
        {
            GameUI.Action(window,new Rect(32,136,170,45),"BUY",()=>{sell=false;selected=null;dirty=true;},true,!sell);
            GameUI.Action(window,new Rect(214,136,170,45),"SELL",()=>{sell=true;selected=null;dirty=true;},true,sell);
            var stock=sell?profile.Stash.Slots.Where(s=>s.Item.SellValue>0 && s.Item.Type!=ItemType.QuestItem).ToArray():(Hub.TraderStock??Array.Empty<ItemDefinition>()).Where(i=>i!=null).Select(i=>new ItemStack(i,i.Type==ItemType.Ammo?30:1)).ToArray();
            selected=InventoryPresentation.Resolve(stock,selected);
            InventoryPresentation.Grid(window,new Rect(32,200,1130,619),stock,selected,Choose,4);
            var detail=InventoryPresentation.Detail(window,new Rect(1190,140,498,679),selected,preview);
            if(selected==null) return;
            var item=selected.Item; int count=selected.Quantity;
            if(!sell) GameUI.Action(detail,new Rect(24,598,450,54),$"BUY ×{count}   /   ${item.BuyValue*count:N0}",()=> {
                if(!profile.Wallet.CanAfford(item.BuyValue*count)) feedback="INSUFFICIENT FUNDS";
                else if(!profile.Stash.Copy().AddItem(item,count)) feedback="STASH FULL";
                else feedback=Hub.Change(f=>f.Buy(item.Id,count))?"PURCHASED":Hub.Message;
                dirty=true;
            });
            else
            {
                GameUI.Action(detail,new Rect(24,590,215,58),$"SELL 1 · ${item.SellValue}",()=>Change(()=>Hub.Change(f=>f.Sell(item.Id,1))));
                GameUI.Action(detail,new Rect(249,590,225,58),$"SELL STACK · ${item.SellValue*count}",()=>Change(()=>Hub.Change(f=>f.Sell(item.Id,count))));
            }
        }
        private void Workbench(Transform window,PlayerProfile profile)
        {
            GameUI.Action(window,new Rect(32,140,370,45),"PRIMARY",()=>{selectedWeapon=LoadoutSlot.Primary;dirty=true;},true,selectedWeapon==LoadoutSlot.Primary);
            GameUI.Action(window,new Rect(414,140,370,45),"SECONDARY",()=>{selectedWeapon=LoadoutSlot.Secondary;dirty=true;},true,selectedWeapon==LoadoutSlot.Secondary);
            var item=profile.Loadout.Get(selectedWeapon)?.Item; var weapon=Hub.Weapons.FirstOrDefault(w=>w.WeaponId==item?.Id);
            if(weapon==null) { GameUI.Label(window,new Rect(32,240,900,100),"EQUIP A WEAPON IN LOADOUT FIRST",28,GameUI.Muted); return; }
            InventoryPresentation.Detail(window,new Rect(32,204,752,615),new ItemStack(item,1),preview);
            var state=new WeaponRuntimeState(weapon,0,0); if(!HubLoadoutAdapter.ApplyPreset(state,profile,Hub.Attachments)) return;
            int index=0;
            foreach(AttachmentSlot slot in Enum.GetValues(typeof(AttachmentSlot)))
            {
                var chosen=slot; var equipped=state.Attachments[slot];
                var card=GameUI.Action(window,new Rect(820+(index%2)*430,140+(index/2)*130,410,112),"",()=>{selectedSocket=chosen;dirty=true;},true,selectedSocket==slot).transform;
                GameUI.Label(card,new Rect(20,14,370,25),slot.ToString().ToUpperInvariant(),16,GameUI.Muted);
                GameUI.Label(card,new Rect(20,50,370,40),TacticalUI.Name(equipped?.DisplayName??"EMPTY SLOT"),22,equipped==null?GameUI.Muted:GameUI.Accent,true);
                if(equipped!=null) GameUI.Box(card,new Rect(0,108,410,4),GameUI.Accent);
                index++;
            }
            GameUI.Label(window,new Rect(820,427,830,32),selectedSocket.ToString().ToUpperInvariant()+" / AVAILABLE OPTIONS",20,null,true);
            GameUI.Action(window,new Rect(820,477,830,50),"REMOVE ATTACHMENT",()=>Change(()=>Hub.Configure(selectedWeapon,selectedSocket,null)),true,state.Attachments[selectedSocket]==null);
            float y=540;
            foreach(var attachment in Hub.Attachments.Where(a=>a.slot==selectedSocket && a.IsCompatible(weapon)))
            {
                var option=attachment; GameUI.Action(window,new Rect(820,y,830,58),TacticalUI.Name(option.DisplayName),()=>Change(()=>Hub.Configure(selectedWeapon,selectedSocket,option)),true,state.Attachments[selectedSocket]==option); y+=72;
            }
        }
        private void Missions(Transform window)
        {
            if(assignment==null) assignment=Hub.Flow.Mission;
            var list=GameUI.Scroll(window,new Rect(32,140,830,679),Hub.Flow.Assignments.Count*132,out _); int index=0;
            foreach(var definition in Hub.Flow.Assignments)
            {
                var option=definition; var progress=Hub.Flow.Profile.Quests.First(q=>q.questId==option.Id);
                var card=GameUI.Action(list,new Rect(0,index++*132,800,120),"",()=>{assignment=option;dirty=true;},true,assignment==option).transform;
                bool completed=progress.state==QuestState.Completed || progress.state==QuestState.Claimed;
                GameUI.Label(card,new Rect(20,12,550,32),option.DisplayName,24,null,true);
                GameUI.Label(card,new Rect(20,50,530,25),$"{option.Type.ToString().ToUpperInvariant()}  /  {progress.state.ToString().ToUpperInvariant()}",15,completed?GameUI.Success:GameUI.Muted);
                GameUI.Progress(card,new Rect(20,90,530,6),progress.count/(float)option.RequiredCount,"");
                GameUI.Label(card,new Rect(590,14,185,80),$"${option.RewardMoney}\n{option.RewardXP} XP\n{progress.count} / {option.RequiredCount}",20,GameUI.Accent,true);
            }
            var detail=GameUI.Box(window,new Rect(900,140,788,679),GameUI.CardColor).transform;
            if(assignment==null) return;
            var current=Hub.Flow.Profile.Quests.First(q=>q.questId==assignment.Id);
            GameUI.Label(detail,new Rect(30,32,728,80),assignment.DisplayName,36,null,true);
            GameUI.Label(detail,new Rect(30,145,728,120),assignment.Description,24,GameUI.Muted);
            GameUI.Progress(detail,new Rect(30,330,728,12),current.count/(float)assignment.RequiredCount,$"PROGRESS   {current.count} / {assignment.RequiredCount}");
            GameUI.Label(detail,new Rect(30,390,728,60),$"${assignment.RewardMoney}     /     {assignment.RewardXP} XP",32,GameUI.Accent,true);
            bool blocked=Hub.Flow.Profile.Quests.Any(q=>q.questId!=assignment.Id && (q.state==QuestState.Active || q.state==QuestState.ReadyToTurnIn));
            if(current.state==QuestState.Available) GameUI.Action(detail,new Rect(30,555,728,64),blocked?"FINISH CURRENT ASSIGNMENT":"ACCEPT ASSIGNMENT",()=>Change(()=>Hub.Flow.EnableMission(assignment) && Hub.AcceptMission()),!blocked);
            else if(current.state==QuestState.ReadyToTurnIn) GameUI.Action(detail,new Rect(30,555,728,64),"TURN IN · CLAIM REWARD",()=>Change(()=>Hub.TurnInMission()));
            else GameUI.Label(detail,new Rect(30,555,728,64),current.state==QuestState.Active?"ACTIVE · EXTRACT TO CONFIRM":"ASSIGNMENT COMPLETED",24,GameUI.Success,true);
        }
        private void Deploy(Transform window,PlayerProfile profile)
        {
            InventoryPresentation.Map(window,new Rect(32,140,790,679));
            var root=GameUI.Rect(window,"Deployment briefing",new Rect(868,140,820,679));
            GameUI.Label(root,new Rect(0,0,810,55),"QUARANTINE OUTSKIRTS",32,null,true);
            GameUI.Label(root,new Rect(0,75,810,30),"ACTIVE ASSIGNMENT",16,GameUI.Muted);
            GameUI.Label(root,new Rect(0,115,810,75),Hub.Flow.MissionProgress?.state==QuestState.Active?Hub.Flow.Mission.DisplayName:"FREE RAID · RECOVER & EXTRACT",28,GameUI.Accent,true);
            GameUI.Label(root,new Rect(0,216,810,30),"LOADOUT CHECK",16,GameUI.Muted);
            int i=0; foreach(var slot in new[]{LoadoutSlot.Primary,LoadoutSlot.Secondary,LoadoutSlot.Backpack})
            {
                var stack=profile.Loadout.Get(slot); var box=GameUI.Box(root,new Rect(i*273,260,260,154),GameUI.CardColor).transform;
                GameUI.Label(box,new Rect(16,10,230,25),slot.ToString().ToUpperInvariant(),14,GameUI.Muted);
                if(stack!=null) GameUI.Symbol(box,new Rect(32,38,190,67),stack.Item.Type,GameUI.Accent);
                GameUI.Label(box,new Rect(16,112,230,34),stack==null?"EMPTY":TacticalUI.Name(stack.Item.DisplayName),18,null,true); i++;
            }
            GameUI.Stat(root,458,"INFECTED THREAT","HIGH",810,GameUI.Danger);
            GameUI.Action(root,new Rect(0,585,810,74),"DEPLOY →",()=>Change(()=>Hub.Deploy()),HubLoadoutAdapter.CanDeploy(Hub.Flow,Hub.Weapons,Hub.Attachments));
        }
        private void OnGUI()
        {
            if(!PresentationMode.ShowDiagnostics || Hub?.Flow==null) return;
            using(new TacticalUI.Scope()) GUI.Box(new Rect(20,330,340,90),$"HUB | ${Hub.Flow.Profile.Wallet.Balance}\n{Hub.Message}");
        }
    }
}
