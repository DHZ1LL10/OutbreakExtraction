using System.Linq;
using Outbreak.Infected;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Loot;
using Outbreak.Raids;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Outbreak.Editor
{
    public static partial class PresentationVisualPass
    {
        private static void CreateAssignments()
        {
            const string folder="Assets/Game/Resources/Assignments";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Game/Resources","Assignments");
            Assignment(folder,"02_clearance","Limpieza",Outbreak.Quests.QuestType.Kill,"infected",10,800,200,"Eliminate 10 infected during one raid, then extract successfully.");
            Assignment(folder,"03_salvage","Salvamento",Outbreak.Quests.QuestType.Collect,"debug_scrap",12,700,200,"Recover 12 scrap from the residential and northern service areas. Extract with the supplies.");
            Assignment(folder,"04_runners","Control de corredores",Outbreak.Quests.QuestType.Kill,"runner",4,1100,300,"Eliminate 4 runners during one raid, then extract successfully.");
            Assignment(folder,"05_critical","Recuperación crítica",Outbreak.Quests.QuestType.Collect,"debug_medical_supplies",8,1000,250,"Recover 8 medical supplies from both northern buildings and extract successfully.");
            AssetDatabase.SaveAssets();
        }
        private static void Assignment(string folder,string id,string title,Outbreak.Quests.QuestType type,string target,int count,int money,int xp,string description)
        {
            string path=folder+"/"+id+".asset";
            var quest=AssetDatabase.LoadAssetAtPath<Outbreak.Quests.QuestDefinition>(path);
            if(quest==null) { quest=ScriptableObject.CreateInstance<Outbreak.Quests.QuestDefinition>(); AssetDatabase.CreateAsset(quest,path); }
            var data=new SerializedObject(quest);
            data.FindProperty("id").stringValue=id; data.FindProperty("displayName").stringValue=title; data.FindProperty("description").stringValue=description;
            data.FindProperty("type").enumValueIndex=(int)type; data.FindProperty("targetId").stringValue=target; data.FindProperty("requiredCount").intValue=count;
            data.FindProperty("requiresExtraction").boolValue=true; data.FindProperty("rewardMoney").intValue=money; data.FindProperty("rewardXP").intValue=xp;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void MissionCredit(GameObject root,bool runner)
        {
            var credit=root.GetComponent<Outbreak.Quests.InfectedMissionCredit>() ?? root.AddComponent<Outbreak.Quests.InfectedMissionCredit>();
            var data=new SerializedObject(credit); data.FindProperty("runner").boolValue=runner; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void ExpandRaid(Scene scene, Transform geometry, Transform environment, Transform props, Transform proxies, Material concrete, Material paint)
        {
            SetBox(scene,"Ground",new Vector3(0,-.25f,37),new Vector3(54,.5f,120));
            SetBox(scene,"North boundary",new Vector3(0,2,97),new Vector3(54,4,.5f));
            SetBox(scene,"South boundary",new Vector3(0,2,-23),new Vector3(54,4,.5f));
            SetBox(scene,"West boundary",new Vector3(-27,2,37),new Vector3(.5f,4,120));
            SetBox(scene,"East boundary",new Vector3(27,2,37),new Vector3(.5f,4,120));
            foreach (var zone in InScene<ExtractionZone>(scene)) zone.transform.position = new Vector3(0,.08f,91);
            foreach (var fence in props.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("perimeter fence")).ToArray())
                fence.position += Vector3.right * (fence.position.x < 0 ? -4 : 4);
            for (int z=52;z<97;z+=5)
            {
                Box("Worn centre line",environment,new Vector3(0,.012f,z),new Vector3(.12f,.018f,2.3f),paint);
                foreach(float x in new[]{-26f,26f}) Model(Props+"Concrete_Fence.prefab",props,"Northern perimeter fence",new Vector3(x,0,z),Quaternion.Euler(0,90,0),new Vector3(1.5f,1.4f,1));
            }
            foreach(float x in new[]{-19f,19f}) Box("Northern sidewalk",environment,new Vector3(x,.05f,72),new Vector3(4,.1f,47),concrete);
            DressBuilding(scene,environment,proxies,new Vector3(-13,0,53),false);
            DressBuilding(scene,environment,proxies,new Vector3(13,0,70),false);
            Car(props,proxies,new Vector3(3,0,47),65,"Abandoned street blockade");
            Car(props,proxies,new Vector3(-10,0,76),-25,"Checkpoint queue");
            Car(props,proxies,new Vector3(12,0,42),8,"Residential parking");
            Car(props,proxies,new Vector3(8,0,84),75,"Checkpoint inspection car");
            foreach (var p in new[]{new Vector3(-7,0,39),new Vector3(7,0,58),new Vector3(-7,0,65)})
            {
                var wall=Box("Road barricade",proxies,p+Vector3.up*.7f,new Vector3(5,1.4f,.65f),concrete);
                wall.AddComponent<BoxCollider>();
            }
            for(int i=0;i<8;i++)
            {
                var p = new Vector3(i%2==0 ? -19 : 20,0,40+i*6);
                var fuel=Model(Props+"Fuel_Canister.prefab",props,"Service yard supplies",p,Quaternion.Euler(0,i*27,0),Vector3.one*1.2f); Ground(fuel);
                Box("Stacked supply crate",props,p+new Vector3(1,.4f,1),new Vector3(.8f,.8f,.8f),concrete);
            }
            var lootRoot=ResetVisualRoot(scene,"Expansion loot");
            var template=InScene<LootContainer>(scene).First(c=>!c.transform.IsChildOf(lootRoot));
            foreach(var p in new[]{new Vector3(-13,.55f,53),new Vector3(13,.55f,70),new Vector3(-20,.55f,80)})
            {
                var clone=Object.Instantiate(template.gameObject,lootRoot); clone.name="Salvage cache"; clone.transform.position=p;
                var data=new SerializedObject(clone.GetComponent<LootContainer>()); data.FindProperty("seed").intValue=901+(int)p.z; data.ApplyModifiedPropertiesWithoutUndo();
            }
            var items=AssetDatabase.FindAssets("t:ItemDefinition",new[]{"Assets/Game"}).Select(g=>AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            // Authored supplies make the longer assignments possible without relying on random loot rolls.
            AddSupplies(lootRoot,items.First(i=>i.Id=="debug_scrap"),new Vector3(-15,.3f,52),6);
            AddSupplies(lootRoot,items.First(i=>i.Id=="debug_scrap"),new Vector3(15,.3f,69),6);
            AddSupplies(lootRoot,items.First(i=>i.Id=="debug_medical_supplies"),new Vector3(-10,.3f,51),4);
            AddSupplies(lootRoot,items.First(i=>i.Id=="debug_medical_supplies"),new Vector3(10,.3f,68),4);
            var spawnRoot=ResetVisualRoot(scene,"Expansion spawn points");
            var positions=new[]{new Vector3(-3,0,10),new Vector3(-17,0,19),new Vector3(3,0,27),new Vector3(17,0,35),
                new Vector3(-12,0,39),new Vector3(-21,0,48),new Vector3(-7,0,55),new Vector3(6,0,63),new Vector3(21,0,68),
                new Vector3(-17,0,73),new Vector3(-3,0,80),new Vector3(3,0,86),new Vector3(-15,0,89),
                new Vector3(12,0,46),new Vector3(3,0,56),new Vector3(-20,0,64),new Vector3(19,0,79),new Vector3(12,0,91)};
            foreach(var spawner in InScene<InfectedSpawner>(scene))
            {
                var data=new SerializedObject(spawner); var array=data.FindProperty("spawnPoints"); array.arraySize=positions.Length;
                for(int i=0;i<positions.Length;i++) { var point=Child("Spawn expanded "+i,spawnRoot); point.position=positions[i]; array.GetArrayElementAtIndex(i).objectReferenceValue=point; }
                data.FindProperty("normalCount").intValue=13; data.FindProperty("runnerCount").intValue=5; data.FindProperty("maximum").intValue=18;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var player=InScene<Outbreak.Player.PlayerInput>(scene).First();
            if(player.GetComponent<RaidMinimap>()==null) player.gameObject.AddComponent<RaidMinimap>();
        }
        private static void SetBox(Scene scene,string name,Vector3 position,Vector3 size)
        { var go=Find(scene,name); if(go==null) throw new System.InvalidOperationException("Missing map boundary: "+name); go.transform.position=position; go.transform.localScale=size; }
        private static void AddSupplies(Transform parent,ItemDefinition item,Vector3 p,int count)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name="Field supplies "+item.DisplayName; go.transform.SetParent(parent); go.transform.position=p; go.transform.localScale=Vector3.one*.5f;
            go.AddComponent<WorldItem>().Initialize(new ItemStack(item,count));
        }
        private static void Overcast(Scene scene)
        {
            RenderSettings.skybox=null; RenderSettings.fogColor=new Color(.49f,.53f,.56f); RenderSettings.fogStartDistance=45; RenderSettings.fogEndDistance=145;
            foreach(var camera in InScene<Camera>(scene)) { camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=RenderSettings.fogColor; }
            foreach(var light in InScene<Light>(scene).Where(l=>l.type==LightType.Directional)) { light.intensity=.7f; light.shadowStrength=.65f; }
        }
    }
}
