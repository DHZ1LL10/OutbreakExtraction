using System;
using Outbreak.Core;
using Outbreak.Debugging;
using Outbreak.Items;
using Outbreak.Loot;
using Outbreak.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Outbreak.Editor
{
    public static class ArchitectureSetup
    {
        private const string Folder = "Assets/Game/DebugContent";

        [MenuItem("Outbreak/Crear entorno de prueba logico")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[OUTBREAK] Stop Play Mode before creating debug content.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Game", "DebugContent");
            var weapon = Item("DebugWeapon", ItemType.Weapon, 1);
            var medical = Item("DebugMedical", ItemType.Medical, 5);
            var valuable = Item("DebugValuable", ItemType.Valuable, 10);
            var catalog = Asset<ItemCatalog>("DebugCatalog", so =>
            {
                var list = so.FindProperty("items");
                list.arraySize = 3;
                list.GetArrayElementAtIndex(0).objectReferenceValue = weapon;
                list.GetArrayElementAtIndex(1).objectReferenceValue = medical;
                list.GetArrayElementAtIndex(2).objectReferenceValue = valuable;
            });
            var map = Asset<RaidMapDefinition>("DebugMap", so =>
            {
                so.FindProperty("id").stringValue = "debug.map";
                so.FindProperty("displayName").stringValue = "Mapa de prueba logica";
                var exits = so.FindProperty("extractions");
                exits.arraySize = 1;
                exits.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "debug.exit";
                exits.GetArrayElementAtIndex(0).FindPropertyRelative("displayName").stringValue = "Extraccion debug";
            });
            var table = Asset<LootTable>("DebugLoot", so =>
            {
                var entries = so.FindProperty("entries");
                entries.arraySize = 1;
                var entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("item").objectReferenceValue = valuable;
                entry.FindPropertyRelative("weight").floatValue = 1;
                entry.FindPropertyRelative("minQuantity").intValue = 1;
                entry.FindPropertyRelative("maxQuantity").intValue = 3;
            });
            var existing = UnityEngine.Object.FindFirstObjectByType<ArchitectureDebug>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[OUTBREAK] Existing debug component selected; content was not overwritten.");
                return;
            }
            var go = new GameObject("Outbreak Architecture Debug");
            Undo.RegisterCreatedObjectUndo(go, "Create Outbreak debug setup");
            var manager = Undo.AddComponent<GameManager>(go);
            var managerData = new SerializedObject(manager);
            managerData.FindProperty("catalog").objectReferenceValue = catalog;
            managerData.FindProperty("saveFileName").stringValue = "architecture-debug.json";
            managerData.ApplyModifiedPropertiesWithoutUndo();
            var debug = Undo.AddComponent<ArchitectureDebug>(go);
            var debugData = new SerializedObject(debug);
            debugData.FindProperty("weapon").objectReferenceValue = weapon;
            debugData.FindProperty("medical").objectReferenceValue = medical;
            debugData.FindProperty("valuable").objectReferenceValue = valuable;
            debugData.FindProperty("map").objectReferenceValue = map;
            debugData.FindProperty("lootTable").objectReferenceValue = table;
            debugData.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            Debug.Log("[OUTBREAK] Ready. Enter Play Mode and use ArchitectureDebug context menus. Save: architecture-debug.json.");
        }

        private static ItemDefinition Item(string name, ItemType type, int maxStack) => Asset<ItemDefinition>(name, so =>
        {
            so.FindProperty("id").stringValue = "debug." + type.ToString().ToLowerInvariant();
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("maxStack").intValue = maxStack;
            so.FindProperty("buyValue").intValue = 100;
            so.FindProperty("sellValue").intValue = 50;
        });

        private static T Asset<T>(string name, Action<SerializedObject> configure) where T : ScriptableObject
        {
            string path = $"{Folder}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            var serialized = new SerializedObject(asset);
            configure(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
