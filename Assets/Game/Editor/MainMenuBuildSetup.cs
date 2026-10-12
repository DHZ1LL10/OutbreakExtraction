#if UNITY_EDITOR
using System.Linq;
using Outbreak.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Outbreak.Editor
{
    public static class MainMenuBuildSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/MainMenu.unity";
        [MenuItem("Outbreak/Crear Main Menu")]
        public static void CreateMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";
            var root = new GameObject("Main Menu");
            var menu = root.AddComponent<Outbreak.Menu.MainMenuController>();
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/DebugContent/Hub/HubCatalog.asset");
            if (catalog == null) throw new System.InvalidOperationException("HubCatalog.asset missing.");
            var so = new SerializedObject(menu); so.FindProperty("catalog").objectReferenceValue = catalog; so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            ConfigureBuildSettings();
            Debug.Log("[Outbreak] MainMenu created and presentation scenes configured.");
        }

        [MenuItem("Outbreak/Preparar Build de Presentación")]
        public static void PreparePresentationBuild()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            { Debug.LogError("MainMenu.unity is missing. Use Outbreak/Create Main Menu first."); return; }
            ConfigureBuildSettings();
            PlayerSettings.productName = "Outbreak Extraction";
            PlayerSettings.companyName = "Outbreak Extraction";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultIsNativeResolution = true;
            EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            AssetDatabase.SaveAssets();
            Debug.Log("[Outbreak] Presentation build prepared for Windows x86_64. No build was started.");
        }

        private static void ConfigureBuildSettings()
        {
            var desired = new[] { ScenePath, "Assets/Game/Scenes/Hub_Test.unity", "Assets/Game/Scenes/RaidLoop_Test.unity" };
            EditorBuildSettings.scenes = desired.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }
    }
}
#endif
