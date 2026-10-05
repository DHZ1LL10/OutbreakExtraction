using Outbreak.Items;
using Outbreak.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Outbreak.Editor
{
    // Editor-only placeholder authoring. Generated assets remain editable and are not overwritten.
    public static class GunplayAttachmentContent
    {
        private const string Path = "Assets/Game/DebugContent/Gunplay/Attachments";
        public static void Configure(GameObject player)
        {
            if (!AssetDatabase.IsValidFolder(Path)) AssetDatabase.CreateFolder("Assets/Game/DebugContent/Gunplay", "Attachments");
            var metal = Material("AttachmentMetal", new Color(0.12f, 0.15f, 0.18f));
            var flash = Material("Flash", new Color(1, 0.65f, 0.15f), true);
            var red = Material("RedDot", new Color(1, 0.03f, 0.01f), true);
            var shell = Material("Brass", new Color(0.75f, 0.5f, 0.15f));
            var worldImpact = Material("WorldImpact", new Color(1, 0.8f, 0.4f), true);
            var targetImpact = Material("TargetImpact", new Color(0.1f, 0.85f, 1), true);
            var debug = player.AddComponent<DebugAttachmentController>();
            debug.redDot = Definition("DebugRedDot", "Debug Red Dot", AttachmentSlot.Optic, metal, red);
            debug.suppressor = Definition("DebugSuppressor", "Debug Suppressor", AttachmentSlot.Muzzle, metal, red);
            debug.compensator = Definition("DebugCompensator", "Debug Compensator", AttachmentSlot.Muzzle, metal, red);
            debug.extendedMagazine = Definition("DebugExtendedMagazine", "Debug Extended Magazine", AttachmentSlot.Magazine, metal, red);
            debug.verticalGrip = Definition("DebugVerticalGrip", "Debug Vertical Grip", AttachmentSlot.Grip, metal, red);
            var effects = player.AddComponent<WeaponEffects>();
            Reference(effects, "flashMaterial", flash); Reference(effects, "shellMaterial", shell);
            Reference(effects, "worldImpactMaterial", worldImpact); Reference(effects, "targetImpactMaterial", targetImpact);
            var audio = player.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0; audio.volume = 0.6f;
            Reference(player.GetComponent<FirearmController>(), "audioSource", audio);
        }
        private static AttachmentDefinition Definition(string name, string displayName, AttachmentSlot slot, Material metal, Material red)
        {
            string path = Path + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttachmentDefinition>(path);
            if (existing != null) { existing.Validate(); return existing; }
            var definition = ScriptableObject.CreateInstance<AttachmentDefinition>();
            definition.item = Item(name, displayName); definition.slot = slot;
            var m = definition.modifiers;
            switch (name)
            {
                case "DebugRedDot": m.adsSpread = new StatModifier(0.85f); break;
                case "DebugSuppressor":
                    m.noise = new StatModifier(0.25f); m.muzzleFlash = new StatModifier(0.12f);
                    m.recoilVertical = new StatModifier(0.95f); definition.suppressesFireSound = true; break;
                case "DebugCompensator":
                    m.recoilVertical = new StatModifier(0.72f); m.noise = new StatModifier(1.2f); m.muzzleFlash = new StatModifier(1.3f); break;
                case "DebugExtendedMagazine":
                    m.magazineCapacity = new StatModifier(1.5f); m.reloadDuration = new StatModifier(1.1f); break;
                case "DebugVerticalGrip":
                    definition.compatibleClasses = AttachmentWeaponClasses.Rifle;
                    m.recoilVertical = new StatModifier(0.85f); m.recoilHorizontal = new StatModifier(0.8f);
                    m.hipSpread = new StatModifier(0.9f); m.adsSpeed = new StatModifier(0.92f); break;
            }
            definition.visualPrefab = Visual(name, slot, metal, red);
            definition.Validate(); AssetDatabase.CreateAsset(definition, path); return definition;
        }
        private static ItemDefinition Item(string name, string displayName)
        {
            string path = Path + "/" + name + "Item.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null) return existing;
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = name;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("type").enumValueIndex = (int)ItemType.Attachment;
            so.FindProperty("maxStack").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo(); item.Validate(); AssetDatabase.CreateAsset(item, path); return item;
        }
        private static GameObject Visual(string name, AttachmentSlot slot, Material metal, Material red)
        {
            string path = Path + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject(name); root.layer = 2;
            try
            {
                var anchors = root.AddComponent<AttachmentVisual>();
                if (slot == AttachmentSlot.Optic)
                {
                    Box(root.transform, "Rail", new Vector3(0, 0, 0), new Vector3(0.052f, 0.012f, 0.065f), metal);
                    Box(root.transform, "Frame L", new Vector3(-0.024f, 0.035f, 0.01f), new Vector3(0.006f, 0.06f, 0.025f), metal);
                    Box(root.transform, "Frame R", new Vector3(0.024f, 0.035f, 0.01f), new Vector3(0.006f, 0.06f, 0.025f), metal);
                    Box(root.transform, "Frame top", new Vector3(0, 0.066f, 0.01f), new Vector3(0.054f, 0.006f, 0.025f), metal);
                    Box(root.transform, "Reticle", new Vector3(0, 0.039f, 0.028f), Vector3.one * 0.0025f, red);
                    anchors.adsAnchor = Child(root.transform, "ADSAnchor", new Vector3(0, 0.039f, 0.028f));
                }
                else if (slot == AttachmentSlot.Muzzle)
                {
                    float length = name == "DebugSuppressor" ? 0.18f : 0.045f;
                    var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder); tube.name = "Muzzle device";
                    tube.transform.SetParent(root.transform, false);
                    tube.transform.localPosition = Vector3.forward * length * 0.5f;
                    tube.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    tube.transform.localScale = new Vector3(0.038f, length * 0.5f, 0.038f);
                    Finish(tube, metal);
                    anchors.muzzleTip = Child(root.transform, "MuzzleTip", Vector3.forward * (length + 0.005f));
                }
                else if (slot == AttachmentSlot.Magazine)
                    Box(root.transform, "Extended magazine", new Vector3(0, -0.035f, 0), new Vector3(0.05f, 0.18f, 0.068f), metal);
                else Box(root.transform, "Vertical grip", new Vector3(0, -0.045f, 0), new Vector3(0.033f, 0.09f, 0.045f), metal);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static Transform Child(Transform parent, string name, Vector3 position)
        { var child = new GameObject(name).transform; child.SetParent(parent, false); child.localPosition = position; return child; }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale; Finish(go, material);
        }
        private static void Finish(GameObject go, Material material)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>()); go.layer = 2;
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Path + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("Missing URP placeholder shader.");
            var material = new Material(shader); material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Reference(Object target, string field, Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
