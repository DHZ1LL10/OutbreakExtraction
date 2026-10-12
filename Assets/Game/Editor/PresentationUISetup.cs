using System.Collections.Generic;
using System.Linq;
using Outbreak.Items;
using Outbreak.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Outbreak.Editor
{
    public static class PresentationUISetup
    {
        private const string Props="Assets/ithappy/Apocalypse_Free/Prefabs/Props/";
        [MenuItem("Outbreak/Actualizar presentación UI Fase 9C")]
        public static void Apply()
        {
            PreviewLibrary();
            var scene=EditorSceneManager.OpenScene("Assets/Game/Scenes/MainMenu.unity",OpenSceneMode.Single);
            var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Main menu environment"); if(old!=null) Object.DestroyImmediate(old);
            var root=new GameObject("Main menu environment").transform;
            Model("Assets/Abandoned buildings/Prefab/abandoned_buildings/house_ruined.prefab",root,new Vector3(4,0,11),180);
            Model(Props+"Apocalypse_Car_01.prefab",root,new Vector3(4,0,3),25);
            Model(Props+"Concrete_Fence.prefab",root,new Vector3(-1,0,5),0);
            Model(Props+"Concrete_Fence.prefab",root,new Vector3(-4,0,7),-20);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name="Menu road"; ground.transform.SetParent(root);
            ground.transform.position=new Vector3(0,-.15f,7); ground.transform.localScale=new Vector3(40,.3f,40); Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/DebugContent/Presentation/Asphalt.mat");
            var camera=new GameObject("Menu background camera",typeof(Camera)).GetComponent<Camera>(); camera.transform.SetParent(root); camera.transform.position=new Vector3(-8,2.5f,-9); camera.transform.LookAt(new Vector3(3,1.1f,7));
            camera.fieldOfView=55; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.25f,.3f,.34f);
            var sun=new GameObject("Cold overcast light",typeof(Light)).GetComponent<Light>(); sun.transform.SetParent(root); sun.type=LightType.Directional; sun.color=new Color(.7f,.8f,.95f); sun.intensity=.95f; sun.transform.rotation=Quaternion.Euler(45,-30,0); sun.shadows=LightShadows.Soft;
            RenderSettings.skybox=null; RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.35f,.4f,.45f); RenderSettings.ambientEquatorColor=new Color(.2f,.25f,.27f); RenderSettings.ambientGroundColor=new Color(.12f,.13f,.14f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogColor=camera.backgroundColor; RenderSettings.fogStartDistance=15; RenderSettings.fogEndDistance=65;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[UI 9C] MainMenu background and shared preview library authored. Hub/Raid/NavMesh untouched.");
        }
        private static void Model(string path,Transform parent,Vector3 position,float yaw)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(prefab==null) throw new System.InvalidOperationException("Missing menu asset: "+path);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent); PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            model.transform.position=position; model.transform.rotation=Quaternion.Euler(0,yaw,0);
            foreach(var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
        }
        private static void PreviewLibrary()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/DebugContent/Hub/HubCatalog.asset");
            const string path="Assets/Game/Resources/ItemPreviews.asset";
            var library=AssetDatabase.LoadAssetAtPath<ItemPreviewLibrary>(path); if(library==null) { library=ScriptableObject.CreateInstance<ItemPreviewLibrary>(); AssetDatabase.CreateAsset(library,path); }
            var entries=new List<ItemPreviewLibrary.Entry>();
            Add(entries,catalog.Get("debug_rifle"),"Assets/DelthorGames/AKM/Prefabs/AKM.prefab");
            Add(entries,catalog.Get("debug_pistol"),Props+"Pistol1.prefab");
            Add(entries,catalog.Get("debug_medical_supplies"),Props+"First_Aid.prefab");
            foreach(var guid in AssetDatabase.FindAssets("t:AttachmentDefinition",new[]{"Assets/Game/DebugContent/Gunplay/Attachments"}))
            {
                var attachment=AssetDatabase.LoadAssetAtPath<AttachmentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if(attachment.item!=null && attachment.visualPrefab!=null) entries.Add(new ItemPreviewLibrary.Entry {item=attachment.item,model=attachment.visualPrefab});
            }
            library.entries=library.entries.Where(e=>!entries.Any(entry=>entry.item==e.item)).Concat(entries).ToArray(); EditorUtility.SetDirty(library);
        }
        private static void Add(List<ItemPreviewLibrary.Entry> entries,ItemDefinition item,string path)
        { var model=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(model==null) throw new System.InvalidOperationException("Missing preview model: "+path); entries.Add(new ItemPreviewLibrary.Entry {item=item,model=model}); }
    }
}
