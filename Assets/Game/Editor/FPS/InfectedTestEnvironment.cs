using System;
using System.Linq;
using Outbreak.Cameras;
using Outbreak.Combat;
using Outbreak.Infected;
using Outbreak.Weapons;
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
    public static class InfectedTestEnvironment
    {
        private const string Content = "Assets/Game/DebugContent/Infected";
        [MenuItem("Outbreak/Crear entorno de prueba Infected")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play antes de crear Infected_Test."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Folder("Assets/Game", "Scenes"); Folder("Assets/Game", "DebugContent"); Folder("Assets/Game/DebugContent", "Infected");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Infected test requires URP/Lit.");
            var floor = Material("Floor", new Color(0.22f, 0.25f, 0.27f), shader);
            var wall = Material("Wall", new Color(0.42f, 0.44f, 0.4f), shader);
            var normal = Material("Normal", new Color(0.35f, 0.5f, 0.18f), shader);
            var runner = Material("Runner", new Color(0.7f, 0.25f, 0.08f), shader);
            var head = Material("Head", new Color(0.6f, 0.68f, 0.38f), shader);
            var geometry = new GameObject("Navigation geometry").transform;
            Box("Ground", geometry, new Vector3(0, -0.25f, 12), new Vector3(36, 0.5f, 52), floor);
            Box("North", geometry, new Vector3(0, 2, 38), new Vector3(36, 4, 0.5f), wall);
            Box("South", geometry, new Vector3(0, 2, -14), new Vector3(36, 4, 0.5f), wall);
            Box("West", geometry, new Vector3(-18, 2, 12), new Vector3(0.5f, 4, 52), wall);
            Box("East", geometry, new Vector3(18, 2, 12), new Vector3(0.5f, 4, 52), wall);
            // Start behind a full-height wall. Exit left/right to enter sight; the area is not invulnerable.
            Box("Initial LOS screen", geometry, new Vector3(0, 1.7f, 0), new Vector3(12, 3.4f, 0.5f), wall);
            Box("West corridor divider", geometry, new Vector3(-6, 1.7f, 10), new Vector3(0.5f, 3.4f, 12), wall);
            Box("East corridor divider", geometry, new Vector3(6, 1.7f, 16), new Vector3(0.5f, 3.4f, 16), wall);
            Box("Search corner", geometry, new Vector3(-11, 1.7f, 22), new Vector3(8, 3.4f, 0.5f), wall);
            Box("Rear LOS break", geometry, new Vector3(0, 1.7f, 28), new Vector3(7, 3.4f, 0.5f), wall);
            Box("Crouch cover", geometry, new Vector3(2, 0.6f, 6), new Vector3(3, 1.2f, 0.6f), wall);
            Label("START / F9 restart / F2 suppressor", new Vector3(0, 2.3f, -0.4f));
            Label("LOS + HEARING / exit around wall", new Vector3(-7, 2.4f, 4));
            var surface = geometry.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (surface.navMeshData == null || NavMesh.CalculateTriangulation().vertices.Length == 0)
                throw new InvalidOperationException("NavMesh bake failed; no valid test scene saved.");
            string navigationPath = AssetDatabase.GenerateUniqueAssetPath(Content + "/InfectedNavigation.asset");
            AssetDatabase.CreateAsset(surface.navMeshData, navigationPath);
            var player = GunplayTestEnvironment.CreateArmedPlayer(new Vector3(0, 0.08f, -7));
            var health = player.AddComponent<PlayerHealth>(); player.AddComponent<PlayerNoiseEmitter>(); player.AddComponent<PlayerDeathSequence>();
            var debug = new GameObject("Infected test debug").AddComponent<InfectedTestDebug>(); Reference(debug, "player", health);
            var normalPrefab = Prefab("DebugInfected", false, normal, head);
            var runnerPrefab = Prefab("DebugRunner", true, runner, head);
            var spawner = new GameObject("Infected spawns").AddComponent<InfectedSpawner>();
            Reference(spawner, "normalPrefab", normalPrefab); Reference(spawner, "runnerPrefab", runnerPrefab); Reference(spawner, "player", health);
            Vector3[] positions = { new Vector3(-3, 0, 9), new Vector3(3, 0, 13), new Vector3(-11, 0, 16), new Vector3(11, 0, 20), new Vector3(-3, 0, 24), new Vector3(11, 0, 30) };
            var so = new SerializedObject(spawner); var points = so.FindProperty("spawnPoints"); points.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                if (!NavMesh.SamplePosition(positions[i], out var hit, 1, NavMesh.AllAreas)) throw new InvalidOperationException("No NavMesh at spawn " + i);
                var point = new GameObject("Spawn " + (i + 1)).transform; point.SetParent(spawner.transform, false);
                point.position = hit.position; point.rotation = Quaternion.Euler(0, 180, 0);
                points.GetArrayElementAtIndex(i).objectReferenceValue = point;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var light = new GameObject("Directional Light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.1f; light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.gameObject.AddComponent<UniversalAdditionalLightData>(); RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.46f);
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Game/Scenes/Infected_Test.unity");
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, path);
            // SceneManager.LoadScene requires a build-settings entry for the runtime F9 debug restart.
            var scenes = EditorBuildSettings.scenes.ToList(); scenes.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = scenes.ToArray();
            Selection.activeGameObject = player;
            Debug.Log("[Infected] " + path + " ready: 4 normal + 2 runners, baked NavMesh. F9 restart; F2 suppressor; HP/debug labels. Initial wall blocks vision, not sound.", player);
        }
        private static InfectedController Prefab(string name, bool runner, Material body, Material head)
        {
            string path = Content + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<InfectedController>();
            var root = new GameObject(name);
            try
            {
                var health = root.AddComponent<InfectedHealth>(); Float(health, "maxHealth", runner ? 36 : 48);
                var agent = root.AddComponent<NavMeshAgent>(); agent.radius = 0.32f; agent.height = 1.8f; agent.angularSpeed = 240; agent.acceleration = runner ? 16 : 10;
                var visual = new GameObject("Visual").transform; visual.SetParent(root.transform, false);
                var torso = Box("Body", visual, new Vector3(0, 0.95f, 0), new Vector3(runner ? 0.44f : 0.58f, 1.05f, 0.36f), body);
                torso.AddComponent<DamageHitbox>().zone = HitZone.Body;
                var skull = Box("Head", visual, new Vector3(0, 1.65f, 0), new Vector3(0.3f, 0.32f, 0.3f), head);
                skull.AddComponent<DamageHitbox>().zone = HitZone.Head;
                foreach (float side in new[] { -1f, 1f })
                {
                    var arm = Box("Arm", visual, new Vector3(side * 0.37f, 1.05f, 0.1f), new Vector3(0.14f, 0.65f, 0.18f), body);
                    arm.AddComponent<DamageHitbox>().zone = HitZone.Limb;
                    var leg = Box("Leg", visual, new Vector3(side * 0.16f, 0.3f, 0), new Vector3(0.18f, 0.6f, 0.24f), body);
                    leg.AddComponent<DamageHitbox>().zone = HitZone.Limb;
                }
                var eye = new GameObject("Eyes").transform; eye.SetParent(root.transform, false); eye.localPosition = new Vector3(0, 1.6f, 0.17f);
                var ai = root.AddComponent<InfectedController>(); Reference(ai, "eyes", eye);
                if (runner) { ai.settings.chaseSpeed = 4.3f; ai.settings.wanderSpeed = 1.1f; ai.settings.reactionTime = 0.1f; ai.settings.attackDamage = 12; ai.settings.attackWindup = 0.32f; }
                var presentation = root.AddComponent<InfectedPresentation>(); Reference(presentation, "visualRoot", visual);
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<InfectedController>();
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        private static void Label(string text, Vector3 position)
        { var label = new GameObject(text).AddComponent<TextMesh>(); label.text = text; label.transform.position = position; label.characterSize = 0.06f; label.fontSize = 32; label.anchor = TextAnchor.MiddleCenter; }
        private static Material Material(string name, Color color, Shader shader)
        {
            string path = Content + "/" + name + ".mat"; var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(shader); material.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Reference(Object target, string field, Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Float(Object target, string field, float value)
        { var so = new SerializedObject(target); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    }
}
