using Outbreak.Hub;
using Outbreak.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Outbreak.Editor
{
    public static class RecoverSuppliesMissionContent
    {
        public static QuestDefinition Ensure()
        {
            const string path = "Assets/Game/Resources/RecoverSupplies.asset";
            var quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(path);
            if (quest != null) return quest;
            if (!AssetDatabase.IsValidFolder("Assets/Game/Resources")) AssetDatabase.CreateFolder("Assets/Game", "Resources");
            quest = ScriptableObject.CreateInstance<QuestDefinition>(); var so = new SerializedObject(quest);
            so.FindProperty("id").stringValue = "recover_supplies"; so.FindProperty("displayName").stringValue = "Recuperar suministros";
            so.FindProperty("description").stringValue = "Recupera 2 suministros médicos durante una incursión y extrae con vida.";
            so.FindProperty("type").enumValueIndex = (int)QuestType.Collect;
            so.FindProperty("targetId").stringValue = "debug_medical_supplies"; so.FindProperty("requiredCount").intValue = 2;
            so.FindProperty("requiresExtraction").boolValue = true; so.FindProperty("rewardMoney").intValue = 500; so.FindProperty("rewardXP").intValue = 100;
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(quest, path); return quest;
        }
        [MenuItem("Outbreak/Actualizar misión Recuperar suministros")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Hub_Test")
            { Debug.LogWarning("Abre Hub_Test fuera de Play para actualizar su misión."); return; }
            var quest = Ensure();
            foreach (var driver in Object.FindObjectsByType<HubSceneFlow>(FindObjectsSortMode.None))
            {
                if (driver.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) continue;
                Undo.RecordObject(driver, "Recover supplies mission");
                var so = new SerializedObject(driver); so.FindProperty("mission").objectReferenceValue = quest; so.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Debug.Log("[Mission] Commander conectado. Geometría y configuración de Hub conservadas; guarda la escena.");
        }
    }
}
