using Outbreak.Items;
using Outbreak.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Outbreak.Editor
{
    public static class GunplayTestEnvironment
    {
        private const string Content = "Assets/Game/DebugContent/Gunplay";
        // Shared gunplay factory for new combat tests; does not open/save or alter existing scenes.
        public static GameObject CreateArmedPlayer(Vector3 position)
        {
            Folder("Assets/Game", "DebugContent"); Folder("Assets/Game/DebugContent", "Gunplay");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("Gunplay needs URP/Lit.");
            var player = FPSTestEnvironment.CreatePlayer(position);
            var camera = player.GetComponentInChildren<Camera>();
            SetBool(player.GetComponentInChildren<Outbreak.Cameras.BodycamController>(), "enableLensDistortion", false);
            var root = Child("WeaponRoot", camera.transform);
            var sight = Material("Sight", new Color(0.8f, 0.85f, 0.65f), shader);
            var rifleView = View("DebugRifle", root, true, Material("Rifle", new Color(0.3f, 0.35f, 0.22f), shader), sight);
            var pistolView = View("DebugPistol", root, false, Material("Pistol", new Color(0.22f, 0.3f, 0.35f), shader), sight);
            var firearm = player.AddComponent<FirearmController>();
            Reference(firearm, "viewCamera", camera); Reference(firearm, "primary", Definition(true)); Reference(firearm, "secondary", Definition(false));
            Reference(firearm, "primaryView", rifleView); Reference(firearm, "secondaryView", pistolView);
            pistolView.gameObject.SetActive(false);
            GunplayAttachmentContent.Configure(player); player.AddComponent<WeaponDebugOverlay>(); ValidateLoadout(firearm);
            return player;
        }
        [MenuItem("Outbreak/Crear entorno de prueba Gunplay")]
        public static void Create() => CreateEnvironment(false);
        [MenuItem("Outbreak/Crear entorno de prueba Gunplay 3B")]
        public static void Create3B() => CreateEnvironment(true);
        private static void CreateEnvironment(bool phase3B)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play antes de crear Gunplay_Test."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Folder("Assets/Game", "Scenes"); Folder("Assets/Game", "DebugContent"); Folder("Assets/Game/DebugContent", "Gunplay");
            // NewScene unloads unused assets. Load definitions/materials only AFTER that boundary;
            // local managed references do not keep their native Unity objects alive.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("Gunplay necesita URP/Lit."); return; }
            var floor = Material("Floor", new Color(0.22f, 0.25f, 0.28f), shader);
            var wall = Material("Wall", new Color(0.4f, 0.43f, 0.46f), shader);
            var body = Material("Body", new Color(0.28f, 0.5f, 0.58f), shader);
            var head = Material("Head", new Color(0.9f, 0.55f, 0.15f), shader);
            var pistolMaterial = Material("Pistol", new Color(0.22f, 0.3f, 0.35f), shader);
            var rifleMaterial = Material("Rifle", new Color(0.3f, 0.35f, 0.22f), shader);
            var sight = Material("Sight", new Color(0.8f, 0.85f, 0.65f), shader);
            var pistol = Definition(false); var rifle = Definition(true);
            var geometry = new GameObject("Gunplay Test Geometry").transform;
            Box("Ground", geometry, new Vector3(0, -0.25f, 23), new Vector3(40, 0.5f, 70), floor);
            Box("Backstop", geometry, new Vector3(0, 3, 57), new Vector3(40, 6, 0.5f), wall);
            Box("Left wall", geometry, new Vector3(-20, 2, 23), new Vector3(0.5f, 4, 70), wall);
            Box("Right wall", geometry, new Vector3(20, 2, 23), new Vector3(0.5f, 4, 70), wall);
            Box("Rear wall", geometry, new Vector3(0, 2, -12), new Vector3(40, 4, 0.5f), wall);
            Box("Lean cover", geometry, new Vector3(-3, 1.2f, 3), new Vector3(1.8f, 2.4f, 0.4f), wall);
            Box("Crouch cover", geometry, new Vector3(3, 0.55f, 3), new Vector3(2.5f, 1.1f, 0.4f), wall);
            Box("Mantle 0.8m - Space facing edge", geometry, new Vector3(-6, 0.4f, 0), new Vector3(2, 0.8f, 2), head);
            Box("Crouch roof 1.32m clearance", geometry, new Vector3(7, 1.47f, 3), new Vector3(3, 0.3f, 3), wall);
            Box("Tunnel left", geometry, new Vector3(5.35f, 0.66f, 3), new Vector3(0.3f, 1.32f, 3), wall);
            Box("Tunnel right", geometry, new Vector3(8.65f, 0.66f, 3), new Vector3(0.3f, 1.32f, 3), wall);
            Target("Cercano 6m", new Vector3(0, 0, 4), body, head);
            Target("Medio 18m", new Vector3(-4, 0, 16), body, head);
            Target("Lejano 42m", new Vector3(4, 0, 40), body, head);
            Target("Movimiento 12m", new Vector3(8, 0, 10), body, head);
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f; light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.gameObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.5f); RenderSettings.sun = light;
            var player = FPSTestEnvironment.CreatePlayer(new Vector3(0, 0.08f, -2));
            var camera = player.GetComponentInChildren<Camera>();
            // Keep debug crosshair/sight alignment exact; all movement/bodycam feedback stays active.
            SetBool(player.GetComponentInChildren<Outbreak.Cameras.BodycamController>(), "enableLensDistortion", false);
            var root = Child("WeaponRoot", camera.transform);
            var rifleView = View("DebugRifle", root, true, rifleMaterial, sight);
            var pistolView = View("DebugPistol", root, false, pistolMaterial, sight);
            var firearm = player.AddComponent<FirearmController>();
            Reference(firearm, "viewCamera", camera); Reference(firearm, "primary", rifle); Reference(firearm, "secondary", pistol);
            Reference(firearm, "primaryView", rifleView); Reference(firearm, "secondaryView", pistolView);
            pistolView.gameObject.SetActive(false);
            if (phase3B) GunplayAttachmentContent.Configure(player);
            player.AddComponent<WeaponDebugOverlay>();
            ValidateLoadout(firearm);
            string path = AssetDatabase.GenerateUniqueAssetPath(phase3B ? "Assets/Game/Scenes/Gunplay_3B_Test.unity" : "Assets/Game/Scenes/Gunplay_Test.unity");
            EditorSceneManager.SaveScene(scene, path); AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log($"[Gunplay] {path} lista. Play | 1 rifle | 2 pistola | LMB fuego | RMB ADS | R recarga | B modo. Targets azules: cuerpo; naranja: cabeza. Izquierda: mantle/lean; derecha: crouch. Armas equipadas para prueba; no se requiere pickup.", player);
        }
        private static WeaponDefinition Definition(bool rifle)
        {
            string name = rifle ? "DebugRifle" : "DebugPistol";
            string path = Content + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (existing != null) return existing; // Preserve Inspector tuning on regeneration.
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.item = Item(name, rifle ? "debug_rifle" : "debug_pistol", ItemType.Weapon);
            definition.ammoType = Item(rifle ? "DebugRifleAmmo" : "DebugPistolAmmo", rifle ? "debug_rifle_ammo" : "debug_pistol_ammo", ItemType.Ammo);
            if (rifle)
            {
                definition.weaponClass = WeaponClass.Rifle; definition.damage = 24; definition.fireRate = 600;
                definition.fireMode = FireMode.Automatic; definition.allowModeSwitch = true;
                definition.magazineCapacity = 30; definition.reloadDuration = 2.2f; definition.range = 150;
                definition.hipSpread = 1.6f; definition.adsSpread = 0.07f; definition.adsFov = 55;
                definition.recoilVertical = 0.7f; definition.recoilHorizontal = 0.16f; definition.recoilRecovery = 3;
                definition.cameraKick = 0.24f; definition.visualKick = 0.025f; definition.visualRotation = 2.4f;
            }
            AssetDatabase.CreateAsset(definition, path); definition.Validate(); return definition;
        }
        private static ItemDefinition Item(string name, string id, ItemType type)
        {
            string path = Content + "/" + name + "Item.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null) return item;
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id; so.FindProperty("displayName").stringValue = name;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("maxStack").intValue = type == ItemType.Ammo ? 120 : 1;
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(item, path); return item;
        }
        private static WeaponViewModel View(string name, Transform parent, bool rifle, Material material, Material sight)
        {
            var root = Child(name, parent);
            var view = root.gameObject.AddComponent<WeaponViewModel>();
            view.UpdatePose(0, false, false, 0);
            Box("Receiver", root, new Vector3(0, -0.016f, 0.02f), new Vector3(0.065f, 0.06f, rifle ? 0.32f : 0.17f), material, false);
            Box("Grip", root, new Vector3(0, -0.09f, -0.025f), new Vector3(0.055f, 0.12f, 0.065f), material, false);
            if (rifle)
            {
                Reference(view, "baseMagazine", Box("Magazine", root, new Vector3(0, -0.09f, 0.12f), new Vector3(0.05f, 0.13f, 0.08f), material, false));
                Box("Barrel", root, new Vector3(0, 0, 0.26f), new Vector3(0.025f, 0.025f, 0.22f), material, false);
            }
            float front = rifle ? 0.27f : 0.085f;
            Box("Front sight", root, new Vector3(0, 0.044f, front), new Vector3(0.007f, 0.022f, 0.01f), sight, false);
            Box("Rear sight L", root, new Vector3(-0.012f, 0.044f, -0.055f), new Vector3(0.008f, 0.022f, 0.01f), sight, false);
            Box("Rear sight R", root, new Vector3(0.012f, 0.044f, -0.055f), new Vector3(0.008f, 0.022f, 0.01f), sight, false);
            var muzzle = Child("Muzzle", root); muzzle.localPosition = new Vector3(0, 0, rifle ? 0.38f : 0.115f);
            var optic = Child("OpticSocket", root); optic.localPosition = new Vector3(0, 0.028f, -0.005f);
            var muzzleMount = Child("MuzzleSocket", root); muzzleMount.localPosition = muzzle.localPosition;
            var magazine = Child("MagazineSocket", root); magazine.localPosition = new Vector3(0, -0.09f, rifle ? 0.12f : -0.025f);
            var grip = Child("GripSocket", root); grip.localPosition = new Vector3(0, -0.045f, rifle ? 0.23f : 0.07f);
            var ejection = Child("EjectionPoint", root); ejection.localPosition = new Vector3(0.045f, 0.005f, rifle ? 0.06f : 0.025f);
            Reference(view, "opticSocket", optic); Reference(view, "muzzleSocket", muzzleMount);
            Reference(view, "magazineSocket", magazine); Reference(view, "gripSocket", grip); Reference(view, "ejectionPoint", ejection);
            Reference(view, "muzzle", muzzle); return view;
        }
        private static void Target(string name, Vector3 position, Material body, Material head)
        {
            var root = new GameObject(name); root.transform.position = position; root.AddComponent<TargetDummy>();
            Box("Body", root.transform, new Vector3(0, 0.92f, 0), new Vector3(0.62f, 1.15f, 0.28f), body).AddComponent<DamageHitbox>().zone = HitZone.Body;
            Box("Head", root.transform, new Vector3(0, 1.66f, 0), new Vector3(0.3f, 0.32f, 0.3f), head).AddComponent<DamageHitbox>().zone = HitZone.Head;
            var label = Child("Damage feedback", root.transform).gameObject.AddComponent<TextMesh>();
            label.transform.localPosition = new Vector3(0, 2.2f, 0); label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.035f; label.fontSize = 36; label.text = name; label.color = Color.white;
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) { Object.DestroyImmediate(go.GetComponent<Collider>()); go.layer = 2; go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; }
            return go;
        }
        private static Transform Child(string name, Transform parent)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        private static Material Material(string name, Color color, Shader shader)
        {
            string path = Content + "/" + name + ".mat";
            var value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value != null) return value;
            value = new Material(shader); value.SetColor("_BaseColor", color); value.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(value, path); return value;
        }
        private static void Reference(Object target, string field, Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void ValidateLoadout(FirearmController firearm)
        {
            var so = new SerializedObject(firearm);
            foreach (string field in new[] { "primary", "secondary" })
            {
                var definition = so.FindProperty(field).objectReferenceValue as WeaponDefinition;
                if (definition == null || !AssetDatabase.Contains(definition))
                    throw new System.InvalidOperationException("Gunplay: missing persistent " + field + " definition; scene not saved.");
                definition.Validate();
            }
            foreach (string field in new[] { "viewCamera", "primaryView", "secondaryView" })
                if (so.FindProperty(field).objectReferenceValue == null)
                    throw new System.InvalidOperationException("Gunplay: missing " + field + "; scene not saved.");
        }
        private static void SetBool(Object target, string field, bool value)
        { var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Folder(string parent, string name)
        { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    }
}
