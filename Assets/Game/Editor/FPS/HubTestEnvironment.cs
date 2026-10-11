using System;
using System.Linq;
using Outbreak.Core;
using Outbreak.Hub;
using Outbreak.Interaction;
using Outbreak.Items;
using Outbreak.Maps;
using Outbreak.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Outbreak.Editor
{
    public static class HubTestEnvironment
    {
        private const string Content = "Assets/Game/DebugContent/Hub";
        [MenuItem("Outbreak/Crear HUB de prueba")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play antes de crear Hub_Test."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string scenePath = "Assets/Game/Scenes/Hub_Test.unity";
            if (!Application.isBatchMode && AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null &&
                !EditorUtility.DisplayDialog("Hub_Test", "Regenerar reemplaza solo Hub_Test. La partida guardada y las escenas existentes se conservan.", "Regenerar", "Cancelar")) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Game/Scenes/RaidLoop_Test.unity") == null)
                throw new InvalidOperationException("Create RaidLoop_Test first.");
            if (!AssetDatabase.IsValidFolder(Content)) AssetDatabase.CreateFolder("Assets/Game/DebugContent", "Hub");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugRifle.asset");
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/DebugContent/Gunplay/DebugPistol.asset");
            if (rifle == null || pistol == null) throw new InvalidOperationException("Gunplay definitions missing.");
            PriceIfMissing(rifle.item, 320, 160); PriceIfMissing(pistol.item, 180, 90);
            PriceIfMissing(rifle.ammoType, 2, 1); PriceIfMissing(pistol.ammoType, 2, 1);
            var attachments = AssetDatabase.FindAssets("t:AttachmentDefinition", new[] { "Assets/Game/DebugContent/Gunplay/Attachments" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AttachmentDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            if (attachments.Length != 5) throw new InvalidOperationException("Expected the five Phase 3B debug attachments.");
            var items = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/Game/DebugContent" })
                .Select(g => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var duplicates = items.GroupBy(i => i.Id).Where(g => g.Count() > 1).ToArray();
            if (duplicates.Length > 0) throw new InvalidOperationException("Duplicate ItemDefinition IDs: " + string.Join(",", duplicates.Select(g => g.Key)));
            string catalogPath = Content + "/HubCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(catalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<ItemCatalog>(); AssetDatabase.CreateAsset(catalog, catalogPath); }
            var so = new SerializedObject(catalog); var list = so.FindProperty("items"); list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo(); catalog.Validate();
            var medical = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/DebugContent/RaidLoop/Medical Supplies.asset");
            if (medical == null) medical = items.First(i => i.Type == ItemType.Medical);
            var map = AssetDatabase.LoadAssetAtPath<RaidMapDefinition>("Assets/Game/DebugContent/RaidLoop/RaidLoopMap.asset");
            var manager = new GameObject("Persistent Game Manager").AddComponent<GameManager>();
            Reference(manager, "catalog", catalog); Boolean(manager, "persistAcrossScenes", true);
            var driver = manager.gameObject.AddComponent<HubSceneFlow>();
            Array(driver, "weapons", new Object[] { rifle, pistol }); Array(driver, "attachments", attachments);
            Array(driver, "traderStock", new Object[] { rifle.item, pistol.item, rifle.ammoType, pistol.ammoType, medical });
            Array(driver, "starterItems", new Object[] { rifle.item, pistol.item, rifle.ammoType, pistol.ammoType, medical }); Reference(driver, "raidMap", map);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Hub requires URP.");
            var floor = Material("Floor", new Color(0.2f, 0.23f, 0.23f), shader);
            var wall = Material("Wall", new Color(0.3f, 0.32f, 0.33f), shader);
            var table = Material("Stations", new Color(0.22f, 0.4f, 0.45f), shader);
            Box("Floor", new Vector3(0, -.25f, 5), new Vector3(16, .5f, 18), floor);
            Box("North wall", new Vector3(0, 1.75f, 14), new Vector3(16, 3.5f, .3f), wall);
            Box("South wall", new Vector3(0, 1.75f, -4), new Vector3(16, 3.5f, .3f), wall);
            Box("West wall", new Vector3(-8, 1.75f, 5), new Vector3(.3f, 3.5f, 18), wall);
            Box("East wall", new Vector3(8, 1.75f, 5), new Vector3(.3f, 3.5f, 18), wall);
            Station<StashStation>("STASH", new Vector3(-5, .6f, 2), table);
            Station<LoadoutStation>("LOADOUT", new Vector3(5, .6f, 2), table);
            Station<TraderStation>("QUARTERMASTER", new Vector3(-5, .6f, 6), table);
            Station<WorkbenchStation>("WORKBENCH", new Vector3(5, .6f, 6), table);
            Station<MissionStation>("MISSIONS", new Vector3(-5, .6f, 10), table);
            Station<DeployStation>("DEPLOY", new Vector3(5, .6f, 10), table);
            var player = FPSTestEnvironment.CreatePlayer(new Vector3(0, .08f, -1));
            player.AddComponent<PlayerInteractor>(); player.AddComponent<HubDebugUI>();
            var light = new GameObject("Directional Light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1;
            light.transform.rotation = Quaternion.Euler(50, -30, 0); light.gameObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = light; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.4f, .42f, .44f);
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, scenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == scenePath); scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            int raidIndex = scenes.FindIndex(s => s.path == "Assets/Game/Scenes/RaidLoop_Test.unity");
            if (raidIndex < 0) scenes.Add(new EditorBuildSettingsScene("Assets/Game/Scenes/RaidLoop_Test.unity", true));
            else scenes[raidIndex] = new EditorBuildSettingsScene("Assets/Game/Scenes/RaidLoop_Test.unity", true);
            EditorBuildSettings.scenes = scenes.ToArray(); Selection.activeGameObject = player;
            Debug.Log("[Hub] Hub_Test created. Uses profile.json; starter content is granted only when no save exists. E stations, Escape close, deploy/return use the existing GameFlow.");
        }
        private static void Station<T>(string name, Vector3 position, Material material) where T : HubStation
        {
            var go = Box(name, position, new Vector3(2.2f, 1.2f, 1.3f), material); go.AddComponent<T>();
            var label = new GameObject(name + " label").AddComponent<TextMesh>(); label.text = name; label.fontSize = 36; label.characterSize = .07f; label.anchor = TextAnchor.MiddleCenter;
            label.transform.position = position + Vector3.up * 1.3f;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.position = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go; }
        private static Material Material(string name, Color color, Shader shader)
        { string path = Content + "/" + name + ".mat"; var value = AssetDatabase.LoadAssetAtPath<Material>(path); if (value != null) return value; value = new Material(shader); value.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(value, path); return value; }
        private static void PriceIfMissing(ItemDefinition item, int buy, int sell)
        { var so = new SerializedObject(item); if (item.BuyValue == 0) so.FindProperty("buyValue").intValue = buy; if (item.SellValue == 0) so.FindProperty("sellValue").intValue = sell; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Reference(Object target, string field, Object value) { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Boolean(Object target, string field, bool value) { var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Array(Object target, string field, Object[] values)
        { var so = new SerializedObject(target); var array = so.FindProperty(field); array.arraySize = values.Length; for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
