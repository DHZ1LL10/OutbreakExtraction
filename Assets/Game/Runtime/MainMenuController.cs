using Outbreak.Items;
using Outbreak.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Outbreak.Menu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private string saveFileName="profile.json";
        private bool hasSave,confirmNewGame,settings;
        private string message;
        private Canvas canvas;
        private Texture2D gradient;
        private const string SensitivityKey="outbreak.mouseSensitivity";
        private const string FullscreenKey="outbreak.fullscreen";
        public bool ContinueAvailable=>hasSave;
        private void Awake()
        {
            Application.targetFrameRate=120; RefreshSaveState();
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            canvas=GameUI.Canvas(transform,"Main menu UI");
            gradient=new Texture2D(64,1,TextureFormat.RGBA32,false) { wrapMode=TextureWrapMode.Clamp };
            for(int i=0;i<64;i++) gradient.SetPixel(i,0,new Color(.015f,.025f,.028f,Mathf.Lerp(.98f,.08f,Mathf.SmoothStep(0,1,i/63f))));
            gradient.Apply(); Build();
        }
        private void Start()=>ApplySettings();
        private void Update() { if(Input.GetKeyDown(KeyCode.Escape) && (settings || confirmNewGame)) { settings=confirmNewGame=false; Build(); } }
        private void RefreshSaveState()
        {
            hasSave=false; if(catalog==null) return;
            hasSave=new SaveService(fileName:saveFileName).Load(catalog,out _,out _)==LoadResult.Loaded;
        }
        private void LoadHub()
        {
            if(!Application.CanStreamedLevelBeLoaded("Hub_Test")) { message="Hub_Test is not enabled in Build Settings."; Build(); return; }
            SceneManager.LoadSceneAsync("Hub_Test",LoadSceneMode.Single);
        }
        private void Continue() { if(hasSave) LoadHub(); }
        private void StartNewGame()
        {
            if(catalog==null) { message="Game catalog is missing."; Build(); return; }
            var saves=new SaveService(fileName:saveFileName);
            if(!saves.TryDelete(out var error)) { message="Could not replace progress: "+error; Build(); return; }
            confirmNewGame=false; hasSave=false; LoadHub();
        }
        private void ApplySettings()
        {
            AudioListener.volume=Mathf.Clamp01(PlayerPrefs.GetFloat("outbreak.masterVolume",.8f));
            Screen.fullScreen=PlayerPrefs.GetInt(FullscreenKey,1)!=0;
            Screen.fullScreenMode=FullScreenMode.FullScreenWindow;
        }
        private void Build()
        {
            GameUI.Clear(canvas.transform);
            var background=GameUI.Rect(canvas.transform,"Readability gradient",new Rect(0,0,1920,1080));
            background.anchorMin=Vector2.zero; background.anchorMax=Vector2.one; background.offsetMin=background.offsetMax=Vector2.zero;
            var image=background.gameObject.AddComponent<RawImage>(); image.texture=gradient; image.raycastTarget=false;
            GameUI.Box(canvas.transform,new Rect(130,218,5,125),GameUI.Accent);
            GameUI.Label(canvas.transform,new Rect(158,199,720,100),"OUTBREAK",88,null,true);
            GameUI.Label(canvas.transform,new Rect(165,307,680,58),"E X T R A C T I O N",35,GameUI.Accent,true);
            GameUI.Label(canvas.transform,new Rect(164,383,730,35),"QUARANTINE ZONE / FIELD OPERATIONS",17,GameUI.Muted);
            GameUI.Label(canvas.transform,new Rect(1370,62,470,65),"OUTSKIRTS\nOPERATIONS TERMINAL",17,GameUI.Muted);
            if(settings) Settings(); else if(confirmNewGame) Confirm(); else Main();
            GameUI.Label(canvas.transform,new Rect(165,920,720,60),message??"SURVIVE · RECOVER · EXTRACT",18,GameUI.Muted);
            GameUI.Label(canvas.transform,new Rect(1370,978,470,35),"OUTBREAK EXTRACTION / PRESENTATION",15,GameUI.Muted);
        }
        private void Main()
        {
            GameUI.Action(canvas.transform,new Rect(165,474,550,70),hasSave?"CONTINUE →":"CONTINUE / NO PROFILE",Continue,hasSave);
            GameUI.Action(canvas.transform,new Rect(165,559,550,70),"NEW GAME",()=>{confirmNewGame=true;Build();});
            GameUI.Action(canvas.transform,new Rect(165,644,550,70),"SETTINGS",()=>{settings=true;Build();});
            GameUI.Action(canvas.transform,new Rect(165,729,550,70),"QUIT",Quit);
        }
        private void Confirm()
        {
            var root=GameUI.Box(canvas.transform,new Rect(165,474,670,330),GameUI.Panel).transform;
            GameUI.Label(root,new Rect(30,28,610,50),"START NEW GAME?",32,null,true);
            GameUI.Label(root,new Rect(30,106,610,75),"Existing progress will be replaced.",24,GameUI.Muted);
            GameUI.Action(root,new Rect(30,228,292,66),"CANCEL",()=>{confirmNewGame=false;Build();});
            GameUI.Action(root,new Rect(344,228,296,66),"START →",StartNewGame);
        }
        private void Settings()
        {
            var root=GameUI.Box(canvas.transform,new Rect(165,452,730,430),GameUI.Panel).transform;
            GameUI.Label(root,new Rect(30,22,670,45),"SETTINGS",28,null,true);
            SettingSlider(root,90,"MASTER VOLUME",PlayerPrefs.GetFloat("outbreak.masterVolume",.8f),0,1,v=> {
                PlayerPrefs.SetFloat("outbreak.masterVolume",v); PlayerPrefs.Save(); ApplySettings();
            },true);
            SettingSlider(root,184,"MOUSE SENSITIVITY",PlayerPrefs.GetFloat(SensitivityKey,1),.25f,2.5f,v=> {
                PlayerPrefs.SetFloat(SensitivityKey,v); PlayerPrefs.Save();
            },false);
            bool fullscreen=PlayerPrefs.GetInt(FullscreenKey,1)!=0;
            GameUI.Action(root,new Rect(30,287,670,48),"FULLSCREEN   "+(fullscreen?"ON":"OFF"),()=> {
                PlayerPrefs.SetInt(FullscreenKey,fullscreen?0:1); PlayerPrefs.Save(); ApplySettings(); Build();
            },true,fullscreen);
            GameUI.Action(root,new Rect(30,354,670,52),"BACK",()=>{settings=false;Build();});
        }
        private static void SettingSlider(Transform parent,float y,string title,float current,float min,float max,System.Action<float> changed,bool percent)
        {
            var value=GameUI.Label(parent,new Rect(30,y,670,30),title+"   "+(percent?Mathf.RoundToInt(current*100)+"%":current.ToString("0.00")),18,GameUI.Muted);
            var root=GameUI.Rect(parent,"Slider",new Rect(30,y+42,670,20));
            var slider=root.gameObject.AddComponent<Slider>(); slider.minValue=min; slider.maxValue=max;
            var track=GameUI.Box(root,new Rect(0,7,670,6),GameUI.CardColor); track.raycastTarget=true;
            var fillArea=GameUI.Rect(root,"Fill area",new Rect(0,7,670,6));
            var fill=GameUI.Box(fillArea,new Rect(0,0,0,6),GameUI.Accent); slider.fillRect=fill.rectTransform;
            fill.rectTransform.anchorMin=Vector2.zero; fill.rectTransform.anchorMax=Vector2.one; fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            var handleArea=GameUI.Rect(root,"Handle area",new Rect(8,0,654,20));
            var handle=GameUI.Box(handleArea,new Rect(0,0,16,20),GameUI.TextColor); slider.handleRect=handle.rectTransform; slider.targetGraphic=handle;
            handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=handle.rectTransform.pivot=new Vector2(0,.5f); handle.rectTransform.anchoredPosition=Vector2.zero;
            handle.raycastTarget=true; slider.SetValueWithoutNotify(current);
            slider.onValueChanged.AddListener(v=>{value.text=title+"   "+(percent?Mathf.RoundToInt(v*100)+"%":v.ToString("0.00"));changed(v);});
        }
        private static void Quit() { Debug.Log("[OUTBREAK] Quit requested from MainMenu."); Application.Quit(); }
        private void OnDestroy() { if(gradient!=null) Destroy(gradient); if(canvas!=null) Destroy(canvas.gameObject); }
    }
}
