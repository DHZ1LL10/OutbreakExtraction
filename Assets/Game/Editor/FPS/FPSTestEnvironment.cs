using Outbreak.Cameras;
using Outbreak.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Outbreak.Editor
{
    public static class FPSTestEnvironment
    {
        [MenuItem("Outbreak/Crear entorno de prueba FPS")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[OUTBREAK FPS] Sal de Play Mode antes de crear FPS_Test.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets/Game", "Scenes");
            EnsureFolder("Assets/Game", "DebugContent");
            EnsureFolder("Assets/Game/DebugContent", "FPS");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[OUTBREAK FPS] No se encontro el shader URP/Lit. Revisa la importacion de URP.");
                return;
            }
            var floorMaterial = Material("Floor", new Color(0.25f, 0.28f, 0.3f), shader);
            var wallMaterial = Material("Wall", new Color(0.55f, 0.57f, 0.58f), shader);
            var rampMaterial = Material("Ramp", new Color(0.32f, 0.45f, 0.38f), shader);
            var obstacleMaterial = Material("Obstacle", new Color(0.55f, 0.4f, 0.23f), shader);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = new GameObject("FPS Test Geometry").transform;
            Box("Ground 40m", new Vector3(0, -0.25f, 6), new Vector3(40, 0.5f, 40), floorMaterial, environment);
            Box("Back wall", new Vector3(0, 2, -13), new Vector3(40, 4, 0.4f), wallMaterial, environment);
            Box("Left wall", new Vector3(-19.5f, 2, 6), new Vector3(0.4f, 4, 38), wallMaterial, environment);
            Box("Right wall", new Vector3(19.5f, 2, 6), new Vector3(0.4f, 4, 38), wallMaterial, environment);
            Box("Front wall", new Vector3(0, 2, 25), new Vector3(40, 4, 0.4f), wallMaterial, environment);
            Box("Low obstacle 0.35m", new Vector3(-3, 0.175f, -3), new Vector3(2, 0.35f, 1), obstacleMaterial, environment);
            Box("Jump obstacle 0.65m", new Vector3(3, 0.325f, -3), new Vector3(2, 0.65f, 1), obstacleMaterial, environment);
            Box("Mantle box 0.8m", new Vector3(-6, 0.4f, -6), new Vector3(2, 0.8f, 2), obstacleMaterial, environment);
            Box("Mantle limit 1.0m", new Vector3(-10, 0.5f, -6), new Vector3(2, 1, 2), obstacleMaterial, environment);
            Box("Too high - 2.4m", new Vector3(-14, 1.2f, -6), new Vector3(2, 2.4f, 2), wallMaterial, environment);
            Box("Curb 0.18m", new Vector3(3, 0.09f, -7), new Vector3(2, 0.18f, 1.6f), obstacleMaterial, environment);
            Box("Low barrier 0.7m", new Vector3(3, 0.35f, -10), new Vector3(2, 0.7f, 0.25f), obstacleMaterial, environment);
            Box("Lean corridor left", new Vector3(10, 1.2f, -6), new Vector3(0.3f, 2.4f, 4), wallMaterial, environment);
            Box("Lean corridor right", new Vector3(11.2f, 1.2f, -6), new Vector3(0.3f, 2.4f, 4), wallMaterial, environment);
            Box("Mantle blocked box 0.8m", new Vector3(-6, 0.4f, -10), new Vector3(2, 0.8f, 2), obstacleMaterial, environment);
            Box("Mantle blocked ceiling", new Vector3(-6, 1.85f, -10), new Vector3(2, 0.3f, 2), wallMaterial, environment);
            Box("Crouch roof - clearance 1.32m", new Vector3(0, 1.47f, 3), new Vector3(3, 0.3f, 4), wallMaterial, environment);
            Box("Tunnel left", new Vector3(-1.65f, 0.66f, 3), new Vector3(0.3f, 1.32f, 4), wallMaterial, environment);
            Box("Tunnel right", new Vector3(1.65f, 0.66f, 3), new Vector3(0.3f, 1.32f, 4), wallMaterial, environment);
            float rampAngle = 20 * Mathf.Deg2Rad;
            var ramp = Box("Walkable ramp 20 degrees", new Vector3(-7, Mathf.Sin(rampAngle) * 4 + 0.15f, 5), new Vector3(4, 0.3f, 8), rampMaterial, environment);
            ramp.transform.rotation = Quaternion.Euler(-20, 0, 0);
            Box("Landing platform 3m", new Vector3(-7, 1.5f, 10.5f), new Vector3(4, 3, 4), rampMaterial, environment);
            var steep = Box("Unwalkable ramp 55 degrees", new Vector3(8, 2.2f, 8), new Vector3(3, 0.3f, 5), obstacleMaterial, environment);
            steep.transform.rotation = Quaternion.Euler(-55, 0, 0);
            for (int i = 0; i < 5; i++)
            {
                float height = (i + 1) * 0.2f;
                Box($"Step {i + 1} - {height:F1}m", new Vector3(6, height * 0.5f, -1 + i), new Vector3(2, height, 1), obstacleMaterial, environment);
            }

            var lighting = new GameObject("Directional Light");
            lighting.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lighting.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lighting.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.5f);
            RenderSettings.sun = light;

            var player = CreatePlayer(new Vector3(0, 0.08f, -7));
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Game/Scenes/FPS_Test.unity");
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log($"[OUTBREAK FPS] Escena lista: {path}. Pulsa Play. Frente: techo bajo; izquierda: rampa; derecha: escalones y rampa de 55 grados. Escape libera el cursor; clic lo captura.", player);
        }

        // Shared factory keeps both debug scenes on the validated FPS/bodycam configuration.
        public static GameObject CreatePlayer(Vector3 position)
        {
            var player = new GameObject("Player");
            player.transform.position = position;
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = Vector3.up * 0.9f;
            controller.skinWidth = 0.03f;
            controller.stepOffset = 0.28f;
            controller.slopeLimit = 45;
            controller.minMoveDistance = 0;
            var input = player.AddComponent<PlayerInput>();
            var look = player.AddComponent<PlayerLook>();
            var motor = player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerDebugOverlay>();
            var view = Child("ViewRoot", player.transform);
            view.localPosition = Vector3.up * 1.66f;
            var rig = Child("BodycamRig", view);
            var cameraTransform = Child("Main Camera", rig);
            cameraTransform.gameObject.tag = "MainCamera";
            var camera = cameraTransform.gameObject.AddComponent<UnityEngine.Camera>();
            camera.nearClipPlane = 0.04f;
            camera.farClipPlane = 150;
            camera.fieldOfView = 75;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.19f);
            cameraTransform.gameObject.AddComponent<AudioListener>();
            var cameraData = cameraTransform.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            var volume = Child("Bodycam Lens Volume", player.transform).gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            volume.weight = 1;
            // A runtime-only private profile is created by BodycamController.
            var bodycam = rig.gameObject.AddComponent<BodycamController>();
            Reference(look, "viewRoot", view);
            Reference(motor, "viewRoot", view);
            Reference(bodycam, "motor", motor);
            Reference(bodycam, "look", look);
            Reference(bodycam, "input", input);
            Reference(bodycam, "viewCamera", camera);
            Reference(bodycam, "lensVolume", volume);
            return player;
        }

        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        private static Material Material(string name, Color color, Shader shader)
        {
            string path = $"Assets/Game/DebugContent/FPS/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.05f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static void Reference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
