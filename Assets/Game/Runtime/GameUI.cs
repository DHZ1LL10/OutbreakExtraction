using System;
using Outbreak.Items;
using Outbreak.Loot;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Outbreak
{
    // Shared, resolution-scaled Unity UI vocabulary. Presentation callbacks consume existing adapters.
    public static class GameUI
    {
        public static readonly Color Panel = new Color(.045f,.058f,.062f,.98f);
        public static readonly Color CardColor = new Color(.085f,.105f,.112f,1);
        public static readonly Color Accent = new Color(.73f,.78f,.58f);
        public static readonly Color Muted = new Color(.5f,.57f,.59f);
        public static readonly Color TextColor = new Color(.9f,.92f,.89f);
        public static readonly Color Success = new Color(.45f,.74f,.58f);
        public static readonly Color Danger = new Color(.8f,.43f,.36f);
        private static Font font;
        public static Canvas Canvas(Transform owner, string name, int order = 100)
        {
            var root = new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            // Overlay canvases are scene roots, independent of the FPS player's moving transform.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,owner.gameObject.scene);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=order;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current==null) new GameObject("UI EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            return canvas;
        }
        public static RectTransform Rect(Transform parent,string name,Rect bounds)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(bounds.x,-bounds.y); r.sizeDelta=bounds.size; return r;
        }
        public static Image Box(Transform parent,Rect bounds,Color color,string name="Panel")
        {
            var r=Rect(parent,name,bounds); var image=r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return image;
        }
        public static Text Label(Transform parent,Rect bounds,string text,int size=20,Color? color=null,bool bold=false)
        {
            var r=Rect(parent,"Label",bounds); var t=r.gameObject.AddComponent<Text>();
            if(font==null) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font=font; t.fontSize=size; t.color=color??TextColor; t.text=text; t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            t.verticalOverflow=VerticalWrapMode.Truncate; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.raycastTarget=false; return t;
        }
        public static Button Action(Transform parent,Rect bounds,string title,Action clicked,bool enabled=true,bool selected=false)
        {
            var background=Box(parent,bounds,selected?new Color(.22f,.28f,.22f):CardColor,title); background.raycastTarget=true;
            var b=background.gameObject.AddComponent<Button>(); b.targetGraphic=background; b.interactable=enabled;
            var colors=b.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.55f,1.65f,1.3f); colors.pressedColor=Accent;
            colors.selectedColor=new Color(1.35f,1.45f,1.2f); colors.disabledColor=new Color(.36f,.4f,.41f,.8f); colors.fadeDuration=.14f; b.colors=colors;
            b.onClick.AddListener(()=>clicked?.Invoke());
            Box(background.transform,new Rect(0,0,selected?4:2,bounds.height),selected?Accent:Muted);
            var label=Label(background.transform,new Rect(18,0,bounds.width-36,bounds.height),title,20,enabled?TextColor:Muted,true);
            label.alignment=TextAnchor.MiddleLeft; label.resizeTextForBestFit=true; label.resizeTextMinSize=12; label.resizeTextMaxSize=20; return b;
        }
        public static RectTransform Window(Canvas canvas,string title,string subtitle,Action close)
        {
            var dim=Box(canvas.transform,new Rect(0,0,1920,1080),new Color(.012f,.018f,.022f,.82f),"Modal shade");
            var dr=dim.rectTransform; dr.anchorMin=Vector2.zero; dr.anchorMax=Vector2.one; dr.offsetMin=dr.offsetMax=Vector2.zero; dim.raycastTarget=true;
            var window=Box(canvas.transform,new Rect(0,0,1720,900),Panel,"Window").rectTransform;
            window.anchorMin=window.anchorMax=window.pivot=new Vector2(.5f,.5f); window.anchoredPosition=Vector2.zero;
            Header(window,title,subtitle);
            Action(window,new Rect(1530,28,155,48),"CLOSE  ×",close);
            Box(window,new Rect(32,116,1656,1),new Color(.25f,.3f,.3f));
            return window;
        }
        public static void Header(Transform parent,string title,string subtitle)
        {
            Box(parent,new Rect(32,34,5,52),Accent);
            Label(parent,new Rect(54,26,1450,45),title,34,TextColor,true);
            Label(parent,new Rect(56,76,1420,24),subtitle,16,Muted);
        }
        public static RectTransform Scroll(Transform parent,Rect bounds,float contentHeight,out ScrollRect scroll)
        {
            var root=Rect(parent,"Scroll",bounds); scroll=root.gameObject.AddComponent<ScrollRect>();
            var viewport=Rect(root,"Viewport",new Rect(0,0,bounds.width,bounds.height)); viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>(); hit.color=new Color(0,0,0,.001f);
            var content=Rect(viewport,"Content",new Rect(0,0,bounds.width-14,Mathf.Max(contentHeight,bounds.height)));
            scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=35;
            var rail=Box(root,new Rect(bounds.width-9,0,8,bounds.height),CardColor,"Scrollbar"); rail.raycastTarget=true;
            var bar=rail.gameObject.AddComponent<Scrollbar>(); bar.direction=Scrollbar.Direction.BottomToTop;
            var handle=Box(rail.transform,new Rect(0,0,8,0),Muted,"Scroll handle");
            handle.rectTransform.anchorMin=Vector2.zero; handle.rectTransform.anchorMax=Vector2.one; handle.rectTransform.offsetMin=handle.rectTransform.offsetMax=Vector2.zero;
            bar.handleRect=handle.rectTransform; bar.targetGraphic=handle; handle.raycastTarget=true;
            scroll.verticalScrollbar=bar; scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }
        public static void Progress(Transform parent,Rect bounds,float value,string caption,Color? color=null)
        {
            Box(parent,bounds,new Color(.13f,.17f,.18f));
            Box(parent,new Rect(bounds.x,bounds.y,bounds.width*Mathf.Clamp01(value),bounds.height),color??Accent);
            if(!string.IsNullOrEmpty(caption)) Label(parent,new Rect(bounds.x,bounds.y-28,bounds.width,25),caption,16,Muted);
        }
        public static void Stat(Transform parent,float y,string name,string value,float width=360,Color? color=null)
        {
            Label(parent,new Rect(0,y,width*.55f,30),name,16,Muted);
            var t=Label(parent,new Rect(width*.5f,y,width*.5f,30),value,20,color??TextColor,true); t.alignment=TextAnchor.UpperRight;
            Box(parent,new Rect(0,y+35,width,1),new Color(.16f,.21f,.22f));
        }
        public static void Symbol(Transform parent,Rect bounds,ItemType type,Color color)
        {
            var r=Rect(parent,"Item silhouette",bounds);
            float w=bounds.width,h=bounds.height;
            if(type==ItemType.Weapon)
            {
                Box(r,new Rect(w*.17f,h*.38f,w*.58f,h*.15f),color);
                Box(r,new Rect(w*.04f,h*.37f,w*.2f,h*.25f),color);
                Box(r,new Rect(w*.7f,h*.4f,w*.26f,h*.05f),color);
                Box(r,new Rect(w*.4f,h*.5f,w*.09f,h*.24f),color);
            }
            else if(type==ItemType.Medical)
            { Box(r,new Rect(w*.43f,h*.15f,w*.14f,h*.7f),color); Box(r,new Rect(w*.2f,h*.4f,w*.6f,h*.2f),color); }
            else if(type==ItemType.Ammo)
            { for(int i=0;i<3;i++) Box(r,new Rect(w*(.25f+i*.18f),h*.2f,w*.1f,h*.65f),color); }
            else
            {
                Box(r,new Rect(w*.22f,h*.25f,w*.56f,h*.5f),color);
                Box(r,new Rect(w*.3f,h*.35f,w*.4f,h*.3f),CardColor);
                if(type==ItemType.Attachment) Box(r,new Rect(w*.42f,h*.42f,w*.16f,h*.16f),color);
            }
        }
        public static Button ItemSlot(Transform parent,Rect bounds,ItemDefinition item,int count,Action select,bool selected=false)
        {
            var b=Action(parent,bounds,"",select,true,selected); var r=b.transform;
            var rarity=RarityPalette.Color(item.Rarity);
            Box(r,new Rect(0,0,bounds.width,3),rarity);
            Label(r,new Rect(14,12,bounds.width-28,24),item.Type.ToString().ToUpperInvariant(),13,Muted);
            Symbol(r,new Rect(25,40,bounds.width-50,bounds.height-100),item.Type,new Color(rarity.r*.85f,rarity.g*.85f,rarity.b*.85f));
            Label(r,new Rect(14,bounds.height-61,bounds.width-28,27),TacticalUI.Name(item.DisplayName),19,TextColor,true);
            Label(r,new Rect(14,bounds.height-30,bounds.width-28,24),$"×{count}                 ${item.SellValue}",15,Muted);
            return b;
        }
        public static void Clear(Transform root)
        {
            for(int i=root.childCount-1;i>=0;i--) { var child=root.GetChild(i).gameObject; child.SetActive(false); UnityEngine.Object.Destroy(child); }
        }
        public static string Clock(float seconds) => $"{(int)seconds/60:00}:{(int)seconds%60:00}";
    }
}
