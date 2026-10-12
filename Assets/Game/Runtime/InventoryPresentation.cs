using System;
using System.Collections.Generic;
using System.Linq;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace Outbreak
{
    public static class InventoryPresentation
    {
        public static ItemStack Resolve(IReadOnlyList<ItemStack> items,ItemStack selected) => selected==null?null:
            items.FirstOrDefault(s=>ReferenceEquals(s,selected)) ?? items.FirstOrDefault(s=>s.Item==selected.Item && s.Quantity==selected.Quantity) ?? items.FirstOrDefault(s=>s.Item==selected.Item);
        public static void Grid(Transform parent,Rect area,IReadOnlyList<ItemStack> items,ItemStack selected,Action<ItemStack> choose,int columns=3)
        {
            const float height=190,gap=12;
            float width=(area.width-16-(columns-1)*gap)/columns;
            var content=GameUI.Scroll(parent,area,Mathf.CeilToInt(items.Count/(float)columns)*(height+gap),out _);
            for(int i=0;i<items.Count;i++)
            {
                var stack=items[i]; GameUI.ItemSlot(content,new Rect((i%columns)*(width+gap),(i/columns)*(height+gap),width,height),stack.Item,stack.Quantity,()=>choose(stack),ReferenceEquals(selected,stack));
            }
            if(items.Count==0) GameUI.Label(content,new Rect(20,30,area.width-40,80),"NO ITEMS STORED\nSearch containers in the field.",22,GameUI.Muted);
        }
        public static void Loadout(Transform parent,Rect area,Loadout equipment,Action<LoadoutSlot> unequip=null)
        {
            var content=GameUI.Scroll(parent,area,740,out _); float y=0;
            foreach(LoadoutSlot slot in Enum.GetValues(typeof(LoadoutSlot)))
            {
                var stack=equipment.Get(slot); bool weapon=slot==LoadoutSlot.Primary || slot==LoadoutSlot.Secondary;
                float height=weapon?130:58;
                var box=GameUI.Box(content,new Rect(0,y,area.width-16,height),GameUI.CardColor).transform;
                GameUI.Box(box,new Rect(0,0,3,height),stack==null?GameUI.Muted:RarityPalette.Color(stack.Item.Rarity));
                GameUI.Label(box,new Rect(15,10,area.width-40,22),slot.ToString().ToUpperInvariant(),13,GameUI.Muted);
                if(weapon && stack!=null) GameUI.Symbol(box,new Rect(16,39,140,70),ItemType.Weapon,GameUI.Accent);
                GameUI.Label(box,new Rect(weapon?157:15,weapon?45:31,area.width-(weapon?180:40),weapon?65:23),stack==null?"EMPTY":TacticalUI.Name(stack.Item.DisplayName),weapon?20:15,stack==null?GameUI.Muted:GameUI.TextColor,true);
                if(unequip!=null && stack!=null) GameUI.Action(box,new Rect(area.width-115,5,90,28),"REMOVE",()=>unequip(slot));
                y+=height+9;
            }
        }
        public static RectTransform Detail(Transform parent,Rect area,ItemStack selected,ItemPreviewRenderer preview)
        {
            var root=GameUI.Box(parent,area,GameUI.CardColor,"Detail panel").rectTransform;
            GameUI.Label(root,new Rect(24,20,area.width-48,25),"SELECTED ITEM",14,GameUI.Muted);
            if(selected==null) { GameUI.Label(root,new Rect(24,75,area.width-48,100),"SELECT A CARD\nTO INSPECT",26,GameUI.Muted,true); preview.Hide(); return root; }
            var item=selected.Item;
            GameUI.Symbol(root,new Rect(40,68,area.width-80,185),item.Type,RarityPalette.Color(item.Rarity));
            var image=GameUI.Rect(root,"3D preview",new Rect(14,55,area.width-28,225)).gameObject.AddComponent<RawImage>(); image.raycastTarget=false; preview.Show(item,image);
            GameUI.Label(root,new Rect(24,300,area.width-48,65),TacticalUI.Name(item.DisplayName),28,null,true);
            GameUI.Label(root,new Rect(24,370,area.width-48,28),item.Rarity.ToString().ToUpperInvariant()+"  /  "+item.Type,16,RarityPalette.Color(item.Rarity));
            var rows=GameUI.Rect(root,"Stats",new Rect(24,412,area.width-48,90));
            GameUI.Stat(rows,0,"QUANTITY",selected.Quantity.ToString(),area.width-48);
            GameUI.Stat(rows,44,"UNIT VALUE","$"+item.SellValue,area.width-48);
            GameUI.Label(root,new Rect(24,515,area.width-48,78),item.Description.Replace("Raid Loop debug item.","Field supplies recovered in the quarantine zone."),17,GameUI.Muted);
            return root;
        }
        public static void Map(Transform parent,Rect area)
        {
            var root=GameUI.Box(parent,area,new Color(.075f,.105f,.115f)).transform;
            GameUI.Label(root,new Rect(24,15,area.width-48,30),"QUARANTINE OUTSKIRTS     /     N ↑",18,GameUI.Muted);
            float width=area.width-80,height=area.height-110;
            GameUI.Box(root,new Rect(40+22/54f*width,65,10/54f*width,height),new Color(.17f,.24f,.25f));
            foreach(var p in new[]{new Vector3(-11,0,8),new Vector3(11,0,24),new Vector3(-13,0,53),new Vector3(13,0,70)})
                GameUI.Box(root,new Rect(40+(p.x+27)/54*width-10/54f*width*.5f,65+(97-p.z)/120*height-8/120f*height*.5f,10/54f*width,8/120f*height),new Color(.35f,.4f,.37f));
            GameUI.Label(root,new Rect(40,area.height-45,width,30),"SOUTH · SAFE SPAWN",16,GameUI.Muted);
            GameUI.Box(root,new Rect(area.width*.5f-35,68,70,10),GameUI.Success);
            GameUI.Label(root,new Rect(area.width*.5f+45,60,150,30),"EXTRACTION",16,GameUI.Success,true);
        }
    }
}
