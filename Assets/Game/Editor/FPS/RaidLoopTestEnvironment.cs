using System;
using System.Collections.Generic;
using System.Linq;
using Outbreak.Cameras;
using Outbreak.Combat;
using Outbreak.Core;
using Outbreak.Infected;
using Outbreak.Interaction;
using Outbreak.Items;
using Outbreak.Loot;
using Outbreak.Maps;
using Outbreak.Raids;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Outbreak.Editor
{
    public static class RaidLoopTestEnvironment
    {
        private const string Content = "Assets/Game/DebugContent/RaidLoop";
        [MenuItem("Outbreak/Crear entorno de prueba Raid Loop")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play antes de crear RaidLoop_Test."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Game/Scenes/RaidLoop_Test.unity") != null &&
                !EditorUtility.DisplayDialog("RaidLoop_Test", "Regenerar reemplaza la escena existente. Para conservar tus cambios, usa Outbreak > Actualizar Raid Loop a Fase 5.1.", "Regenerar", "Cancelar")) return;
            Folder("Assets/Game", "Scenes"); Folder("Assets/Game", "DebugContent"); Folder("Assets/Game/DebugContent", "RaidLoop");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene unloads unused native assets, so create/load persistent debug content after it.
            var content = EnsureContent();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Raid Loop test requires URP/Lit.");
            var floor = Material("Floor", new Color(0.18f, 0.2f, 0.19f), shader);
            var wall = Material("Wall", new Color(0.33f, 0.35f, 0.32f), shader);
            var crate = Material("Container", new Color(0.34f, 0.29f, 0.18f), shader);
            var pickup = Material("Pickup", new Color(0.25f, 0.55f, 0.62f), shader);
            var extraction = Material("Extraction", new Color(0.16f, 0.72f, 0.36f), shader);

            var geometry = new GameObject("Raid geometry").transform;
            Box("Ground", geometry, new Vector3(0, -0.25f, 15), new Vector3(46, 0.5f, 76), floor);
            Box("North boundary", geometry, new Vector3(0, 2, 53), new Vector3(46, 4, 0.5f), wall);
            Box("South boundary", geometry, new Vector3(0, 2, -23), new Vector3(46, 4, 0.5f), wall);
            Box("West boundary", geometry, new Vector3(-23, 2, 15), new Vector3(0.5f, 4, 76), wall);
            Box("East boundary", geometry, new Vector3(23, 2, 15), new Vector3(0.5f, 4, 76), wall);
            // Safe spawn pocket, two loot buildings and LOS-breaking lanes.
            Box("Safe north left", geometry, new Vector3(-3.75f, 1.7f, -12), new Vector3(4.5f, 3.4f, 0.5f), wall);
            Box("Safe north right", geometry, new Vector3(3.75f, 1.7f, -12), new Vector3(4.5f, 3.4f, 0.5f), wall);
            Box("Safe west", geometry, new Vector3(-6, 1.7f, -17), new Vector3(0.5f, 3.4f, 10), wall);
            Box("Safe east", geometry, new Vector3(6, 1.7f, -17), new Vector3(0.5f, 3.4f, 10), wall);
            Building(geometry, new Vector3(-11, 0, 8), wall);
            Building(geometry, new Vector3(11, 0, 24), wall);
            Box("Central LOS wall", geometry, new Vector3(0, 1.7f, 17), new Vector3(10, 3.4f, 0.5f), wall);
            Box("West lane", geometry, new Vector3(-6, 1.7f, 32), new Vector3(0.5f, 3.4f, 14), wall);
            Box("East lane", geometry, new Vector3(7, 1.7f, 38), new Vector3(0.5f, 3.4f, 14), wall);
            Label("SAFE SPAWN", new Vector3(0, 2.4f, -20));
            Label("LOOT BUILDING", new Vector3(-11, 3, 8));


            var surface = geometry.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
            if (surface.navMeshData == null || NavMesh.CalculateTriangulation().vertices.Length == 0)
                throw new InvalidOperationException("Raid Loop NavMesh bake failed.");
            string navigationPath = AssetDatabase.GenerateUniqueAssetPath(Content + "/RaidLoopNavigation.asset");
            AssetDatabase.CreateAsset(surface.navMeshData, navigationPath);

            var manager = new GameObject("Game Manager").AddComponent<GameManager>();
            Reference(manager, "catalog", content.catalog); String(manager, "saveFileName", "raidloop_profile.json");
            var context = new GameObject("Raid Runtime").AddComponent<RaidRuntimeContext>();
            Reference(context, "gameManager", manager); Reference(context, "map", content.map); Int(context, "debugBackpackCapacity", 16);

            var player = GunplayTestEnvironment.CreateArmedPlayer(new Vector3(0, 0.08f, -19));
            var health = player.AddComponent<PlayerHealth>(); player.AddComponent<PlayerNoiseEmitter>(); player.AddComponent<PlayerDeathSequence>();
            var interactor = player.AddComponent<PlayerInteractor>(); Reference(interactor, "raidContext", context);
            var ui = player.AddComponent<RaidLoopDebugUI>(); Reference(ui, "interactor", interactor); Reference(ui, "context", context); Reference(ui, "health", health);

            Container("Toolbox", LootContainerType.Toolbox, new Vector3(-13, 0.55f, 6), content.tables[0], crate, 3, 1101);
            Container("Medical Bag", LootContainerType.MedicalBag, new Vector3(-9, 0.45f, 10), content.tables[1], crate, 3, 2202);
            Container("Civilian Container", LootContainerType.CivilianContainer, new Vector3(9, 0.6f, 21), content.tables[2], crate, 4, 3303);
            Container("Locker", LootContainerType.Locker, new Vector3(13, 1.0f, 26), content.tables[3], crate, 4, 4404);
            Container("Military Crate", LootContainerType.MilitaryCrate, new Vector3(2, 0.55f, 34), content.tables[4], crate, 6, 5505);
            Pickup("World Scrap", content.items[0], 2, new Vector3(-3, 0.3f, 2), pickup);
            Pickup("World Ammo", content.items[1], 12, new Vector3(4, 0.3f, 13), pickup);
            Pickup("Rare Radio", content.items[3], 1, new Vector3(10, 0.3f, 30), pickup);

            var extract = Box("Extraction Zone", null, new Vector3(0, 0.08f, 48), new Vector3(8, 0.16f, 7), extraction);
            extract.AddComponent<ExtractionZone>().ConfigureTrigger(context);

            var spawner = new GameObject("Infected spawns").AddComponent<InfectedSpawner>();
            Reference(spawner, "normalPrefab", InfectedTestEnvironment.EnsureDebugPrefab(false));
            Reference(spawner, "runnerPrefab", InfectedTestEnvironment.EnsureDebugPrefab(true)); Reference(spawner, "player", health);
            Vector3[] positions = { new Vector3(-2, 0, 5), new Vector3(-16, 0, 17), new Vector3(4, 0, 25), new Vector3(16, 0, 36), new Vector3(-12, 0, 43), new Vector3(12, 0, 47) };
            var spawnerSo = new SerializedObject(spawner); var points = spawnerSo.FindProperty("spawnPoints"); points.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                if (!NavMesh.SamplePosition(positions[i], out var hit, 2, NavMesh.AllAreas)) throw new InvalidOperationException("No NavMesh at raid spawn " + i);
                var point = new GameObject("Spawn " + (i + 1)).transform; point.SetParent(spawner.transform, false); point.position = hit.position;
                points.GetArrayElementAtIndex(i).objectReferenceValue = point;
            }
            spawnerSo.FindProperty("variedWander").boolValue = true;
            spawnerSo.FindProperty("deathLootTable").objectReferenceValue = Table("InfectedScrapLoot", new[] { content.items[0] }, new[] { 1f });
            spawnerSo.FindProperty("dropChance").floatValue = 0.3f;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            var light = new GameObject("Directional Light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.05f; light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(52, -28, 0);
            light.gameObject.AddComponent<UniversalAdditionalLightData>(); RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.37f, 0.39f, 0.4f);

            const string path = "Assets/Game/Scenes/RaidLoop_Test.unity";
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, path);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(x => x.path == path)) { scenes.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            Selection.activeGameObject = player; AssetDatabase.SaveAssets();
            Debug.Log("[Raid Loop] RaidLoop_Test ready. E interact/lean, automatic 5s extraction, Tab backpack, 1/2 weapons, F1-F4 attachments. Loot persists per container; success saves to raidloop_profile.json.", player);
        }

        [MenuItem("Outbreak/Actualizar Raid Loop a Fase 5.1")]
        public static void UpgradeCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "RaidLoop_Test")
            { Debug.LogWarning("Abre RaidLoop_Test fuera de Play antes de actualizar."); return; }
            var context = Object.FindObjectsByType<RaidRuntimeContext>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            foreach (var zone in Object.FindObjectsByType<ExtractionZone>(FindObjectsSortMode.None))
            {
                if (zone.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) continue;
                Undo.RecordObject(zone, "Automatic extraction");
                Undo.RecordObject(zone.GetComponent<Collider>(), "Extraction trigger volume");
                zone.ConfigureTrigger(context);
                EditorUtility.SetDirty(zone); EditorUtility.SetDirty(zone.GetComponent<Collider>());
                var so = new SerializedObject(zone); so.FindProperty("extractionDuration").floatValue = 5; so.ApplyModifiedProperties();
            }
            foreach (var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if (label.text == "EXTRACTION — HOLD E") { Undo.RecordObject(label.gameObject, "Hide old extraction label"); label.gameObject.SetActive(false); }
            var scrap = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Content + "/Scrap.asset");
            foreach (var spawner in Object.FindObjectsByType<InfectedSpawner>(FindObjectsSortMode.None))
            {
                Undo.RecordObject(spawner, "Varied wander");
                var so = new SerializedObject(spawner); so.FindProperty("variedWander").boolValue = true;
                if (scrap != null && so.FindProperty("deathLootTable").objectReferenceValue == null)
                    so.FindProperty("deathLootTable").objectReferenceValue = Table("InfectedScrapLoot", new[] { scrap }, new[] { 1f });
                so.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Raid Loop] Fase 5.1 applied. Geometry, navigation and manual scene changes preserved. Save the scene.");
        }

        private static (ItemCatalog catalog, RaidMapDefinition map, ItemDefinition[] items, LootTable[] tables) EnsureContent()
        {
            var items = new[]
            {
                Item("Scrap", "debug_scrap", ItemRarity.Common, ItemType.Valuable, 12, 10),
                Item("Ammo", "debug_loot_ammo", ItemRarity.Common, ItemType.Ammo, 8, 60),
                Item("Medical Supplies", "debug_medical_supplies", ItemRarity.Uncommon, ItemType.Medical, 55, 5),
                Item("Military Radio", "debug_military_radio", ItemRarity.Rare, ItemType.Valuable, 240, 2),
                Item("Encrypted Drive", "debug_encrypted_drive", ItemRarity.Epic, ItemType.Valuable, 650, 1),
                Item("Prototype Weapon Part", "debug_prototype_part", ItemRarity.Legendary, ItemType.Valuable, 1600, 1),
                Item("Experimental Core", "debug_experimental_core", ItemRarity.Mythic, ItemType.QuestItem, 4200, 1)
            };
            var catalog = Asset<ItemCatalog>("RaidLoopCatalog");
            var catalogSo = new SerializedObject(catalog); var array = catalogSo.FindProperty("items"); array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            catalogSo.ApplyModifiedPropertiesWithoutUndo(); catalog.Validate();
            var map = Asset<RaidMapDefinition>("RaidLoopMap"); var mapSo = new SerializedObject(map);
            mapSo.FindProperty("id").stringValue = "debug_raid_loop"; mapSo.FindProperty("displayName").stringValue = "Debug Raid Loop";
            mapSo.FindProperty("difficulty").intValue = 2; mapSo.FindProperty("lootMultiplier").floatValue = 1f; mapSo.FindProperty("rareLootMultiplier").floatValue = 1.35f;
            var extractions = mapSo.FindProperty("extractions"); extractions.arraySize = 1;
            extractions.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "north_gate";
            extractions.GetArrayElementAtIndex(0).FindPropertyRelative("displayName").stringValue = "North Gate";
            extractions.GetArrayElementAtIndex(0).FindPropertyRelative("durationSeconds").floatValue = 4;
            mapSo.ApplyModifiedPropertiesWithoutUndo(); map.Validate();
            var tables = new[]
            {
                Table("ToolboxLoot", new[] { items[0], items[1], items[3] }, new[] { 7f, 4f, 0.8f }),
                Table("MedicalLoot", new[] { items[2], items[0], items[4] }, new[] { 7f, 2f, 0.45f }),
                Table("CivilianLoot", new[] { items[0], items[1], items[2], items[3] }, new[] { 6f, 5f, 2f, 0.7f }),
                Table("LockerLoot", new[] { items[1], items[2], items[3], items[4] }, new[] { 5f, 3f, 1.2f, 0.45f }),
                Table("MilitaryLoot", new[] { items[1], items[3], items[4], items[5], items[6] }, new[] { 5f, 2.5f, 1.2f, 0.35f, 0.12f })
            };
            return (catalog, map, items, tables);
        }
        private static ItemDefinition Item(string name, string id, ItemRarity rarity, ItemType type, int value, int maxStack)
        {
            var item = Asset<ItemDefinition>(name.Replace(" ", "")); var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id; so.FindProperty("displayName").stringValue = name;
            so.FindProperty("description").stringValue = "Raid Loop debug item."; so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("rarity").enumValueIndex = (int)rarity; so.FindProperty("buyValue").intValue = value * 2;
            so.FindProperty("sellValue").intValue = value; so.FindProperty("maxStack").intValue = maxStack;
            so.ApplyModifiedPropertiesWithoutUndo(); item.Validate(); return item;
        }
        private static LootTable Table(string name, ItemDefinition[] items, float[] weights)
        {
            var table = Asset<LootTable>(name); var so = new SerializedObject(table); var entries = so.FindProperty("entries"); entries.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i); entry.FindPropertyRelative("item").objectReferenceValue = items[i];
                entry.FindPropertyRelative("weight").floatValue = weights[i]; entry.FindPropertyRelative("minQuantity").intValue = 1;
                entry.FindPropertyRelative("maxQuantity").intValue = items[i].Type == ItemType.Ammo ? 12 : items[i].Rarity == ItemRarity.Common ? 3 : 1;
            }
            so.FindProperty("dropChance").floatValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); table.Validate(); return table;
        }
        private static void Container(string name, LootContainerType type, Vector3 position, LootTable table, Material material, int rolls, int seed)
        {
            var go = Box(name, null, position, type == LootContainerType.Locker ? new Vector3(1.1f, 2, 0.65f) : new Vector3(1.5f, 0.8f, 0.9f), material);
            var container = go.AddComponent<LootContainer>(); var so = new SerializedObject(container);
            so.FindProperty("containerType").enumValueIndex = (int)type; so.FindProperty("lootTable").objectReferenceValue = table;
            so.FindProperty("rolls").intValue = rolls; so.FindProperty("seed").intValue = seed; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Pickup(string name, ItemDefinition item, int quantity, Vector3 position, Material material)
        {
            var go = Box(name, null, position, new Vector3(0.35f, 0.35f, 0.35f), material); var pickup = go.AddComponent<WorldItem>();
            Reference(pickup, "item", item); Int(pickup, "quantity", quantity);
        }
        private static void Building(Transform parent, Vector3 origin, Material wall)
        {
            Box("Building back", parent, origin + new Vector3(0, 1.7f, 4), new Vector3(10, 3.4f, 0.5f), wall);
            Box("Building left", parent, origin + new Vector3(-5, 1.7f, 0), new Vector3(0.5f, 3.4f, 8), wall);
            Box("Building right", parent, origin + new Vector3(5, 1.7f, 0), new Vector3(0.5f, 3.4f, 8), wall);
            Box("Building front L", parent, origin + new Vector3(-3.5f, 1.7f, -4), new Vector3(3, 3.4f, 0.5f), wall);
            Box("Building front R", parent, origin + new Vector3(3.5f, 1.7f, -4), new Vector3(3, 3.4f, 0.5f), wall);
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        private static void Label(string text, Vector3 position)
        { var label = new GameObject(text).AddComponent<TextMesh>(); label.text = text; label.transform.position = position; label.characterSize = 0.055f; label.fontSize = 32; label.anchor = TextAnchor.MiddleCenter; }
        private static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = Content + "/" + name + ".asset"; var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset; asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static Material Material(string name, Color color, Shader shader)
        {
            string path = Content + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material; material = new Material(shader); material.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Reference(Object target, string field, Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void String(Object target, string field, string value)
        { var so = new SerializedObject(target); so.FindProperty(field).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Int(Object target, string field, int value)
        { var so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Folder(string parent, string name)
        { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    }
}
