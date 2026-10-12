using System;
using Outbreak.Core;
using Outbreak.Items;
using Outbreak.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Outbreak.Menu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private string saveFileName = "profile.json";
        private bool hasSave, confirmNewGame, settings;
        private string message;
        private Vector2 menuScroll;
        private const string SensitivityKey = "outbreak.mouseSensitivity";
        private const string FullscreenKey = "outbreak.fullscreen";
        public bool ContinueAvailable => hasSave;

        private void Awake()
        {
            Application.targetFrameRate = 120;
            RefreshSaveState();
        }
        private void RefreshSaveState()
        {
            hasSave = false;
            if (catalog == null) return;
            var result = new SaveService(fileName: saveFileName).Load(catalog, out _, out _);
            hasSave = result == LoadResult.Loaded;
        }
        private void LoadHub()
        {
            if (!Application.CanStreamedLevelBeLoaded("Hub_Test")) { message = "Hub_Test is not enabled in Build Settings."; return; }
            SceneManager.LoadSceneAsync("Hub_Test", LoadSceneMode.Single);
        }
        private void Continue() { if (hasSave) LoadHub(); }
        private void StartNewGame()
        {
            if (catalog == null) { message = "Game catalog is missing."; return; }
            var saves = new SaveService(fileName: saveFileName);
            if (!saves.TryDelete(out var error)) { message = "Could not replace progress: " + error; return; }
            confirmNewGame = false; hasSave = false; LoadHub();
        }
        private void ApplySettings()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("outbreak.masterVolume", .8f));
            bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            Screen.fullScreen = fullscreen;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        private void Start() => ApplySettings();
        private void OnGUI()
        {
            using (new Outbreak.TacticalUI.Scope())
            {
                var background = new Color(.025f, .035f, .037f, .97f); GUI.color = background;
                GUI.DrawTexture(new Rect(0, 0, Outbreak.TacticalUI.Width, Outbreak.TacticalUI.Height), Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Box(new Rect(100, 48, 1080, 624), "");
                GUILayout.BeginArea(new Rect(170, 92, 940, 540));
                var title = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                title.normal.textColor = Outbreak.TacticalUI.Accent;
                GUILayout.Label("OUTBREAK", title); GUILayout.Label("EXTRACTION", new GUIStyle(title) { fontSize = 22, normal = { textColor = Color.white } });
                GUILayout.Space(36);
                if (settings) DrawSettings(); else if (confirmNewGame) DrawConfirm(); else DrawMain();
                GUILayout.EndArea();
            }
        }
        private void DrawMain()
        {
            GUILayout.BeginVertical(GUILayout.Width(430));
            GUI.enabled = hasSave;
            if (GUILayout.Button("CONTINUE", GUILayout.Height(48))) Continue();
            GUI.enabled = true;
            if (!hasSave) GUILayout.Label("No valid field profile found.", GUI.skin.label);
            if (GUILayout.Button("NEW GAME", GUILayout.Height(48))) confirmNewGame = true;
            if (GUILayout.Button("SETTINGS", GUILayout.Height(48))) settings = true;
            if (GUILayout.Button("QUIT", GUILayout.Height(48))) Quit();
            GUILayout.EndVertical();
            if (!string.IsNullOrEmpty(message)) GUILayout.Label(message);
        }
        private void DrawConfirm()
        {
            GUI.Box(new Rect(250, 245, 760, 150), "START NEW GAME?\nExisting progress will be replaced.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CANCEL", GUILayout.Height(48))) confirmNewGame = false;
            if (GUILayout.Button("START", GUILayout.Height(48))) StartNewGame();
            GUILayout.EndHorizontal();
        }
        private void DrawSettings()
        {
            GUILayout.BeginVertical(GUILayout.Width(650));
            GUILayout.Label("SETTINGS", new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold });
            float volume = PlayerPrefs.GetFloat("outbreak.masterVolume", .8f);
            GUILayout.Label("MASTER VOLUME  " + Mathf.RoundToInt(volume * 100) + "%");
            float nextVolume = GUILayout.HorizontalSlider(volume, 0, 1);
            if (!Mathf.Approximately(volume, nextVolume)) { PlayerPrefs.SetFloat("outbreak.masterVolume", nextVolume); PlayerPrefs.Save(); ApplySettings(); }
            float sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1);
            GUILayout.Label("MOUSE SENSITIVITY  " + sensitivity.ToString("0.00"));
            float nextSensitivity = GUILayout.HorizontalSlider(sensitivity, .25f, 2.5f);
            if (!Mathf.Approximately(sensitivity, nextSensitivity)) { PlayerPrefs.SetFloat(SensitivityKey, nextSensitivity); PlayerPrefs.Save(); }
            bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            bool nextFullscreen = GUILayout.Toggle(fullscreen, "FULLSCREEN");
            if (nextFullscreen != fullscreen) { PlayerPrefs.SetInt(FullscreenKey, nextFullscreen ? 1 : 0); PlayerPrefs.Save(); ApplySettings(); }
            GUILayout.Space(24);
            if (GUILayout.Button("BACK", GUILayout.Height(48))) settings = false;
            GUILayout.EndVertical();
        }
        private static void Quit()
        {
            Debug.Log("[OUTBREAK] Quit requested from MainMenu.");
            Application.Quit();
        }
    }
}
