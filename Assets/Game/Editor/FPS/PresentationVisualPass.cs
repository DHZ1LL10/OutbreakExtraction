using System;
using System.Linq;
using Outbreak.Combat;
using Outbreak.Infected;
using Outbreak.Interaction;
using Outbreak.Loot;
using Outbreak.Raids;
using Outbreak.Weapons;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Outbreak.Editor
{
    // Explicit, saved-scene presentation authoring. Never runs in Play and never regenerates gameplay.
    public static class PresentationVisualPass
    {
        private const string Content = "Assets/Game/DebugContent/Presentation";
        private const string Buildings = "Assets/Abandoned buildings/Prefab/abandoned_buildings/";
        private const string Props = "Assets/ithappy/Apocalypse_Free/Prefabs/Props/";
        private const string Zombie = "Assets/Kevin Iglesias/Zombie Animations/";
        [MenuItem("Outbreak/Actualizar escena a presentación Fase 8")]
        public static void UpdateCurrent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Hub_Test" && scene.name != "RaidLoop_Test")
            { Debug.LogWarning("Abre Hub_Test o RaidLoop_Test fuera de Play."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply(scene); EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        }
        // Used in batch validation against a copy. Existing scenes are opened, never regenerated.
        public static void ApplySavedScenes()
        {
            foreach (var name in new[] { "RaidLoop_Test", "Hub_Test" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/" + name + ".unity", OpenSceneMode.Single);
                Apply(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            }
        }
        public static void Apply(Scene scene)
        {
            if (!AssetDatabase.IsValidFolder(Content)) AssetDatabase.CreateFolder("Assets/Game/DebugContent", "Presentation");
            if (scene.name == "RaidLoop_Test") Raid(scene);
            else if (scene.name == "Hub_Test") Hub(scene);
            else throw new InvalidOperationException("Presentation is restricted to Hub_Test/RaidLoop_Test.");
            ConfigurePresentation(scene);
            Validate(scene);
        }
        private static T[] InScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static GameObject Find(Scene scene, string name) => InScene<Transform>(scene).FirstOrDefault(t => t.name == name)?.gameObject;
        private static Transform ResetVisualRoot(Scene scene, string name)
        {
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (old != null) Object.DestroyImmediate(old);
            return new GameObject(name).transform;
        }
        private static void Raid(Scene scene)
        {
            var geometry = Find(scene, "Gameplay_Geometry") ?? Find(scene, "Raid geometry");
            if (geometry == null) throw new InvalidOperationException("Existing raid geometry missing.");
            geometry.name = "Gameplay_Geometry";
            // Collision proxies belong to this pass; the original geometry remains in place.
            var oldProxy = geometry.transform.Find("Presentation collision proxies");
            if (oldProxy != null) Object.DestroyImmediate(oldProxy.gameObject);
            var proxies = Child("Presentation collision proxies", geometry.transform);
            var environment = ResetVisualRoot(scene, "Environment_Visuals");
            var props = ResetVisualRoot(scene, "Props_Visuals");
            var ground = Mat("Asphalt", new Color(.16f,.18f,.19f));
            var concrete = Mat("Weathered concrete", new Color(.30f,.31f,.29f));
            var timber = Mat("Shelter timber", new Color(.22f,.20f,.16f));
            var paint = Mat("Lane paint", new Color(.58f,.56f,.42f));
            foreach (var r in geometry.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.name.StartsWith("Building")) r.enabled = false;
                else if (r.name.EndsWith("boundary")) r.enabled = false;
                else r.sharedMaterial = r.name == "Ground" ? ground : concrete;
            }
            // The imported ruin's accessible opening is on its +Z side at x ~= +2.5m.
            // Rotate it to the approach side and retain the original wall objects/colliders.
            DressBuilding(scene, environment, proxies, new Vector3(-11,0,8), true);
            DressBuilding(scene, environment, proxies, new Vector3(11,0,24), false);
            Model(Buildings + "house_aband.prefab", environment, "Distant abandoned house", new Vector3(-29,0,34), Quaternion.Euler(0,90,0), Vector3.one);
            Model(Buildings + "house_roof.prefab", environment, "Distant roof", new Vector3(-29,0,34), Quaternion.Euler(0,90,0), Vector3.one);
            for (int z=-19; z<48; z+=5)
                Box("Worn centre line", environment, new Vector3(0,.012f,z), new Vector3(.12f,.018f,2.3f), paint);
            Box("West sidewalk", environment, new Vector3(-19,.05f,15), new Vector3(4,.1f,70), concrete);
            Box("East sidewalk", environment, new Vector3(19,.05f,15), new Vector3(4,.1f,70), concrete);
            // Keep entry/loot/combat routes readable; large props have simple collision proxies.
            Car(props, proxies, new Vector3(5,0,8), 24, "Abandoned car");
            Car(props, proxies, new Vector3(-14,0,29), -15, "Stripped car");
            for (int i=0;i<14;i++)
            {
                Model(Props+"Concrete_Fence.prefab", props, "West perimeter fence", new Vector3(-22,0,-18+i*5), Quaternion.Euler(0,90,0), new Vector3(1.5f,1.4f,1));
                Model(Props+"Concrete_Fence.prefab", props, "East perimeter fence", new Vector3(22,0,-18+i*5), Quaternion.Euler(0,90,0), new Vector3(1.5f,1.4f,1));
            }
            // Continuous street edges connect the scattered assets into one small settlement.
            foreach(float x in new[]{-5.5f,5.5f})
                Box("Street shoulder",environment,new Vector3(x,.008f,14),new Vector3(1.2f,.012f,64),concrete);
            var patch=Mat("Road repairs",new Color(.115f,.13f,.14f));
            for(int i=0;i<9;i++)
            {
                var repair=Box("Asphalt repair",environment,new Vector3(i%2==0?-2.1f:2.8f,.025f,-12+i*6),new Vector3(1.5f,.01f,2.1f),patch);
                repair.transform.rotation=Quaternion.Euler(0,i*23,0);
            }
            Box("Medical sign backing",props,new Vector3(-13.5f,2.3f,3.94f),new Vector3(1.55f,.34f,.04f),timber);
            Label("MEDICAL / SUPPLIES",props,new Vector3(-13.5f,2.3f,3.91f),.025f);
            Label("QUARANTINE / NORTH",props,new Vector3(0,1.12f,16.65f),.045f);
            var salvage=Model(Buildings+"stove_burned.prefab",props,"Discarded household scrap",new Vector3(-17,0,12),Quaternion.Euler(0,22,0),Vector3.one);
            Ground(salvage);
            var fuel = Model(Props+"Fuel_Canister.prefab", props, "Discarded fuel", new Vector3(-4,0,19), Quaternion.Euler(0,25,0), Vector3.one*1.2f);
            Ground(fuel);
            Model(Buildings+"stove_burned.prefab", props, "Abandoned stove", new Vector3(18,0,31), Quaternion.Euler(0,-90,0), Vector3.one);
            Box("Barricade base", props, new Vector3(1, .6f,17), new Vector3(9,1.2f,.65f), timber);
            // Original LOS collider remains the authoritative central wall.
            foreach (var r in geometry.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Central LOS wall")) r.sharedMaterial = timber;
            foreach (var container in InScene<LootContainer>(scene)) LootVisual(container.gameObject);
            foreach (var pickup in InScene<WorldItem>(scene)) PickupVisual(pickup.gameObject);
            foreach (var zone in InScene<ExtractionZone>(scene))
            {
                var renderer = zone.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = false;
                Checkpoint(environment, props, zone.transform.position, concrete, paint);
            }
            foreach (var firearm in InScene<FirearmController>(scene)) Weapons(firearm);
            foreach (var spawner in InScene<InfectedSpawner>(scene))
            {
                var characters = scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Characters_Visuals") ?? new GameObject("Characters_Visuals");
                spawner.transform.SetParent(characters.transform,true);
                Reference(spawner,"normalPrefab",InfectedPrefab(false)); Reference(spawner,"runnerPrefab",InfectedPrefab(true));
            }
            var surface = geometry.GetComponent<NavMeshSurface>();
            if (surface == null) throw new InvalidOperationException("Raid NavMeshSurface missing.");
            surface.RemoveData(); surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("Raid NavMesh bake failed.");
            string path = Content + "/RaidPresentationNavigation.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing != null)
            {
                surface.RemoveData(); EditorUtility.CopySerialized(surface.navMeshData, existing);
                surface.navMeshData = existing; surface.AddData(); EditorUtility.SetDirty(existing);
            }
            else AssetDatabase.CreateAsset(surface.navMeshData,path);
            VerifyPaths(scene);
            Lighting(scene,false);
        }
        private static void DressBuilding(Scene scene, Transform parent, Transform proxies, Vector3 origin, bool roof)
        {
            var scale = new Vector3(10f/10.22f,1.02f,8f/6.56f);
            var house = Model(Buildings+"house_ruined.prefab",parent,roof ? "West medical building" : "East looted ruin",origin,Quaternion.Euler(0,180,0),scale);
            if (roof) Model(Buildings+"house_roof.prefab",parent,"West medical roof",origin,Quaternion.Euler(0,180,0),scale);
            foreach (var mf in house.GetComponentsInChildren<MeshFilter>().Where(m=>m.GetComponent<Renderer>() != null && m.GetComponent<Renderer>().enabled))
            {
                var collision = Child("Building mesh collision",proxies);
                collision.position=mf.transform.position; collision.rotation=mf.transform.rotation; collision.localScale=mf.transform.lossyScale;
                collision.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
            }
            foreach (var collider in InScene<BoxCollider>(scene).Where(c=>c.name.StartsWith("Building front") && Mathf.Abs(c.transform.position.z-(origin.z-4))<.1f))
            {
                bool left = collider.name.EndsWith("L");
                collider.transform.position=origin+new Vector3(left ? -4.25f : 1.75f,1.7f,-4);
                collider.transform.localScale=new Vector3(left ? 1.5f : 6.5f,3.4f,.5f);
            }
        }
        private static void Car(Transform visuals, Transform proxies, Vector3 position, float yaw, string name)
        {
            Model(Props+"Apocalypse_Car_01.prefab",visuals,name,position,Quaternion.Euler(0,yaw,0),Vector3.one);
            var proxy=Child(name+" collision",proxies); proxy.position=position+Vector3.up*.8f;
            proxy.rotation=Quaternion.Euler(0,yaw,0); var box=proxy.gameObject.AddComponent<BoxCollider>(); box.size=new Vector3(2.5f,1.6f,5.2f);
        }
        private static void Checkpoint(Transform environment, Transform props, Vector3 position, Material concrete, Material paint)
        {
            position.y=0;
            foreach (float x in new[]{-6f,6f})
            {
                Model(Props+"Concrete_Fence.prefab",props,"Extraction barricade",position+new Vector3(x,0,0),Quaternion.Euler(0,90,0),new Vector3(1.5f,1.3f,1));
                Box("Checkpoint post",environment,position+new Vector3(x,1.8f,2),new Vector3(.18f,3.6f,.18f),concrete);
                Lamp(environment,position+new Vector3(x,3.3f,2),new Color(.6f,.78f,.88f),3,9);
            }
            Box("Checkpoint lintel",environment,position+new Vector3(0,3.55f,2),new Vector3(12.2f,.3f,.25f),concrete);
            for(int z=-2;z<=2;z+=2)
                Box("Extraction ground marking",environment,position+new Vector3(0,.018f,z),new Vector3(7,.02f,.10f),paint);
            Label("NORTH CHECKPOINT / EXTRACTION",environment,position+new Vector3(0,3.15f,1.82f),.06f);
            // No new collider/trigger: the original ExtractionZone volume is untouched.
        }
        private static void LootVisual(GameObject root)
        {
            var renderer=root.GetComponent<Renderer>(); if(renderer!=null)renderer.enabled=false;
            var old=root.transform.Find("VisualModel"); if(old!=null)Object.DestroyImmediate(old.gameObject);
            var visual=Child("VisualModel",root.transform); NeutralWorldScale(visual);
            var bounds=root.GetComponent<Collider>().bounds;
            if(root.name.Contains("Medical"))
            {
                Box("Medical supply case",visual,bounds.center-Vector3.up*.2f,new Vector3(1.15f,.36f,.75f),Mat("Medical case",new Color(.34f,.40f,.35f)));
                Model(Props+"First_Aid.prefab",visual,"First aid kit",bounds.center+Vector3.up*.165f,Quaternion.Euler(0,180,0),Vector3.one*1.35f,true);
            }
            else
            {
                bool locker=root.name.Contains("Locker");
                Box("Storage case",visual,bounds.center,bounds.size*.94f,Mat(locker ? "Locker metal" : "Supply timber",locker ? new Color(.22f,.27f,.25f):new Color(.32f,.25f,.16f)));
                foreach(float side in new[]{-1f,1f})
                    Box("Case metal band",visual,bounds.center+new Vector3(side*bounds.size.x*.3f,0,0),new Vector3(.055f,bounds.size.y*.97f,bounds.size.z*.97f),Mat("Fittings",new Color(.12f,.14f,.15f)));
                if(root.name=="Toolbox")Box("Toolbox handle",visual,bounds.center+Vector3.up*(bounds.size.y*.44f),new Vector3(.36f,.055f,.09f),Mat("Fittings",new Color(.12f,.14f,.15f)));
            }
        }
        private static void PickupVisual(GameObject root)
        {
            var renderer=root.GetComponent<Renderer>(); if(renderer!=null)renderer.enabled=false;
            var old=root.transform.Find("VisualModel");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var visual=Child("VisualModel",root.transform);NeutralWorldScale(visual);
            if(root.name.Contains("Radio"))Model(Props+"Walkie_talkie.prefab",visual,"Radio",root.transform.position,Quaternion.identity,Vector3.one*2,true);
            else Box(root.name.Contains("Ammo")?"Ammunition carton":"Salvage case",visual,root.transform.position,new Vector3(.28f,.18f,.22f),Mat("Supply timber",new Color(.32f,.25f,.16f)));
        }
        private static void Hub(Scene scene)
        {
            var environment=ResetVisualRoot(scene,"Environment_Visuals");var props=ResetVisualRoot(scene,"Props_Visuals");
            var structure=Find(scene,"Gameplay_Geometry");if(structure==null)structure=new GameObject("Gameplay_Geometry");
            foreach(string name in new[]{"Floor","North wall","South wall","West wall","East wall"})
            {
                var go=Find(scene,name);if(go==null)continue;
                go.transform.SetParent(structure.transform,true);
                go.GetComponent<Renderer>().sharedMaterial=Mat(name=="Floor"?"Safehouse floor":"Safehouse plaster",name=="Floor"?new Color(.24f,.23f,.20f):new Color(.40f,.38f,.32f));
            }
            var wood=Mat("Shelter timber",new Color(.22f,.20f,.16f));
            for(int z=-3;z<=14;z+=4)
            {
                Box("Ceiling beam",environment,new Vector3(0,3.4f,z),new Vector3(16,.22f,.22f),wood);
                foreach(float x in new[]{-7.7f,7.7f})Box("Wall timber",environment,new Vector3(x,1.7f,z),new Vector3(.15f,3.4f,.2f),wood);
            }
            Box("Safehouse ceiling",environment,new Vector3(0,3.65f,5),new Vector3(16,.2f,18),Mat("Safehouse plaster",new Color(.40f,.38f,.32f)));
            foreach(var station in InScene<Outbreak.Hub.HubStation>(scene))
            {
                var renderer=station.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;
                var old=station.transform.Find("VisualModel");if(old!=null)Object.DestroyImmediate(old.gameObject);
                var visual=Child("VisualModel",station.transform);NeutralWorldScale(visual);var p=station.transform.position;
                Box("Station worktop",visual,p+Vector3.up*.56f,new Vector3(2.1f,.12f,1.25f),wood);
                foreach(float x in new[]{-.83f,.83f})Box("Station support",visual,p+new Vector3(x,-.02f,0),new Vector3(.16f,1.1f,1.05f),wood);
                string item=station is Outbreak.Hub.MissionStation || station is Outbreak.Hub.DeployStation ? "Map.prefab" : station is Outbreak.Hub.TraderStation ? "First_Aid.prefab" : "Fuel_Canister.prefab";
                Model(Props+item,visual,"Station supplies",p+new Vector3(.35f,.92f,0),Quaternion.Euler(0,90,0),Vector3.one*(item=="First_Aid.prefab"?2:.7f),true);
                if(station is Outbreak.Hub.StashStation)Box("Stash case",visual,p+new Vector3(-.4f,.77f,0),new Vector3(.8f,.3f,.55f),Mat("Locker metal",new Color(.22f,.27f,.25f)));
            }
            var missionLabel=InScene<TextMesh>(scene).FirstOrDefault(t=>t.text=="MISSIONS");if(missionLabel!=null)missionLabel.text="COMMANDER";
            foreach(var label in InScene<TextMesh>(scene))
            {
                if(label.text.Contains("SAFEHOUSE"))continue;
                label.characterSize=.035f; label.fontSize=64;
                label.color=new Color(.72f,.76f,.65f);
            }
            foreach(float z in new[]{2f,8f,12f})Lamp(environment,new Vector3(0,3.15f,z),new Color(1,.78f,.49f),3.5f,9);
            Label("OUTBREAK / FIELD SAFEHOUSE",environment,new Vector3(0,2.5f,13.75f),.075f);
            Model(Props+"First_Aid.prefab",props,"Emergency supplies",new Vector3(-6.7f,1.2f,12.5f),Quaternion.identity,Vector3.one*3);
            Model(Props+"Concrete_Fence.prefab",props,"External safehouse barrier",new Vector3(9,0,5),Quaternion.Euler(0,90,0),Vector3.one*1.5f);
            Lighting(scene,true);
        }
        private static void Weapons(FirearmController firearm)
        {
            var so=new SerializedObject(firearm);
            Weapon((WeaponViewModel)so.FindProperty("primaryView").objectReferenceValue,true);
            Weapon((WeaponViewModel)so.FindProperty("secondaryView").objectReferenceValue,false);
        }
        private static void Weapon(WeaponViewModel view,bool rifle)
        {
            if(view==null)throw new InvalidOperationException("Weapon view missing.");
            var old=view.transform.Find("VisualModel");if(old!=null)Object.DestroyImmediate(old.gameObject);
            foreach(var renderer in view.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            var model=Model(rifle ? "Assets/DelthorGames/AKM/Prefabs/AKM.prefab" : Props+"Pistol1.prefab",view.transform,"VisualModel",Vector3.zero,Quaternion.identity,Vector3.one);
            const float scale=.82f;
            model.transform.localPosition=(rifle ? new Vector3(0,-.064f,.1f):new Vector3(0,-.11f,0))*scale;
            model.transform.localRotation=Quaternion.Euler(0,rifle ? 180:90,0);model.transform.localScale=Vector3.one*(rifle ? .042f:1)*scale;
            foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
            foreach(var renderer in model.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
            var so=new SerializedObject(view);
            so.FindProperty("hipPosition").vector3Value=rifle?new Vector3(.20f,-.18f,.52f):new Vector3(.15f,-.17f,.52f);
            so.FindProperty("adsPosition").vector3Value=rifle?new Vector3(0,-.046f*scale,.52f):new Vector3(0,-.044f*scale,.52f);
            string magName=rifle?"Cube.003_low":"Pistol1_Magazine";
            var magazine=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==magName);
            if(magazine==null)throw new InvalidOperationException("Model magazine missing: "+magName);
            so.FindProperty("baseMagazine").objectReferenceValue=magazine.gameObject;
            SetAnchor(so,"muzzle",new Vector3(0,0,rifle?.522f:.264f));
            SetAnchor(so,"muzzleSocket",new Vector3(0,0,rifle?.522f:.264f));
            SetAnchor(so,"opticSocket",new Vector3(0,.033f,rifle?.14f:.04f));
            SetAnchor(so,"magazineSocket",rifle?new Vector3(0,-.10f,.115f):new Vector3(0,-.11f,-.008f));
            SetAnchor(so,"gripSocket",new Vector3(0,-.035f,rifle?.27f:.18f));
            SetAnchor(so,"ejectionPoint",new Vector3(.035f,.007f,rifle?.16f:.06f));
            foreach(string field in new[]{"muzzle","muzzleSocket","opticSocket","magazineSocket","gripSocket","ejectionPoint"})
            {
                var anchor=(Transform)so.FindProperty(field).objectReferenceValue;
                anchor.localPosition*=scale; anchor.localScale=Vector3.one*scale;
            }
            so.ApplyModifiedPropertiesWithoutUndo();view.UpdatePose(0,false,false,0);
        }
        private static void SetAnchor(SerializedObject so,string field,Vector3 position)
        {var t=so.FindProperty(field).objectReferenceValue as Transform;if(t==null)throw new InvalidOperationException(field+" missing");t.localPosition=position;t.localRotation=Quaternion.identity;}
        private static InfectedController InfectedPrefab(bool runner)
        {
            string path=Content+(runner?"/PresentationRunner.prefab":"/PresentationInfected.prefab");
            var existing=AssetDatabase.LoadAssetAtPath<InfectedController>(path);
            if(existing!=null)
            {
                var contents=PrefabUtility.LoadPrefabContents(path);
                try { InfectedMaterials(contents); PrefabUtility.SaveAsPrefabAsset(contents,path); }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                return AssetDatabase.LoadAssetAtPath<InfectedController>(path);
            }
            var source="Assets/Game/DebugContent/Infected/"+(runner?"DebugRunner":"DebugInfected")+".prefab";
            var root=PrefabUtility.LoadPrefabContents(source);
            try
            {
                root.name=runner?"Runner":"Infected";
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                var visual=root.transform.Find("Visual");if(visual==null)throw new InvalidOperationException("Infected visual root missing.");
                var model=Model(Zombie+"Prefabs/Zombie.prefab",visual,"Zombie model",Vector3.zero,Quaternion.identity,Vector3.one);
                model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
                var animator=model.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=Controller();animator.applyRootMotion=false;
                var adapter=root.AddComponent<InfectedVisualAnimator>();Reference(adapter,"animator",animator);
                Reference(adapter,"headHitbox",root.GetComponentsInChildren<DamageHitbox>().First(h=>h.zone==HitZone.Head).transform);
                var aso=new SerializedObject(adapter);aso.FindProperty("runner").boolValue=runner;aso.ApplyModifiedPropertiesWithoutUndo();
                var presentation=root.GetComponent<InfectedPresentation>();var pso=new SerializedObject(presentation);pso.FindProperty("useAnimatedPose").boolValue=true;pso.ApplyModifiedPropertiesWithoutUndo();
                InfectedMaterials(root);
                return PrefabUtility.SaveAsPrefabAsset(root,path).GetComponent<InfectedController>();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void InfectedMaterials(GameObject root)
        {
            // Source vendor materials remain unchanged. The presentation variant receives world lighting.
            var material=Mat("Infected lit",Color.white);
            var source=AssetDatabase.LoadAssetAtPath<Material>(Zombie+"Materials/ZombieMaterial.mat");
            material.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));EditorUtility.SetDirty(material);
            foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>material).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            }
        }
        private static AnimatorController Controller()
        {
            string path=Content+"/InfectedVisual.controller";var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(existing!=null)return existing;
            var controller=AnimatorController.CreateAnimatorControllerAtPath(path);var machine=controller.layers[0].stateMachine;
            string[] states={"Idle","Walk","Attack","Damage","Death"};string[] clips={"Zombie@Idle01","Zombie@Walk01","Zombie@Attack01","Zombie@Damage01","Zombie@Death01_A"};
            for(int i=0;i<states.Length;i++)
            {
                var state=machine.AddState(states[i],new Vector3(250,i*65));state.motion=AssetDatabase.LoadAllAssetsAtPath(Zombie+"Animations/"+clips[i]+".fbx").OfType<AnimationClip>().First(c=>c.name==clips[i]);
                state.writeDefaultValues=false;state.speed=states[i]=="Attack"?1.8f:states[i]=="Damage"?1.5f:1;
                if(i==0)machine.defaultState=state;
            }
            return controller;
        }
        private static void Lighting(Scene scene,bool hub)
        {
            var sun=InScene<Light>(scene).FirstOrDefault(l=>l.type==LightType.Directional);
            if(sun!=null){sun.color=hub?new Color(1,.9f,.74f):new Color(.75f,.83f,.95f);sun.intensity=hub?.45f:.85f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-32,0);RenderSettings.sun=sun;}
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=hub?new Color(.38f,.34f,.29f):new Color(.42f,.47f,.54f);
            RenderSettings.ambientEquatorColor=hub?new Color(.26f,.23f,.19f):new Color(.27f,.30f,.32f);
            RenderSettings.ambientGroundColor=hub?new Color(.14f,.12f,.10f):new Color(.15f,.16f,.17f);
            RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>("Assets/ithappy/Apocalypse_Free/Skyboxes/Skybox_3.mat");
            RenderSettings.fog=!hub;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.45f,.49f,.53f);RenderSettings.fogStartDistance=35;RenderSettings.fogEndDistance=110;
            foreach(var camera in InScene<Camera>(scene)){camera.clearFlags=CameraClearFlags.Skybox;camera.backgroundColor=new Color(.45f,.49f,.53f);}
        }
        private static void Ground(GameObject model)
        { model.transform.position -= Vector3.up * Bounds(model).min.y; }

        private static void ConfigurePresentation(Scene scene)
        {
            var player=InScene<Outbreak.Player.PlayerInput>(scene).Single();
            if(player.GetComponent<PresentationMode>()==null)player.gameObject.AddComponent<PresentationMode>();
            var input=new SerializedObject(player); input.FindProperty("blockMovementWhileSuppressed").boolValue=true;input.ApplyModifiedPropertiesWithoutUndo();
            foreach(var body in InScene<Outbreak.Cameras.BodycamController>(scene))
            {
                var so=new SerializedObject(body);so.FindProperty("normalFov").floatValue=79;so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var camera in InScene<Camera>(scene))
            {
                camera.fieldOfView=79;camera.allowDynamicResolution=false;camera.allowHDR=true;camera.allowMSAA=true;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
                data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality=AntialiasingQuality.High;
            }
            // The active PC pipeline already renders at native resolution with 4x MSAA.
            // Keep project-wide quality profiles untouched; use scene SMAA for remaining edge shimmer.
            var existing=Find(scene,"Presentation grading");if(existing!=null)Object.DestroyImmediate(existing);
            string path=Content+(scene.name=="Hub_Test"?"/SafehouseGrading.asset":"/RaidGrading.asset");
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);
                var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.Neutral);AssetDatabase.AddObjectToAsset(tone,profile);
                var color=profile.Add<ColorAdjustments>();color.contrast.Override(8);color.saturation.Override(-7);
                color.postExposure.Override(.12f);AssetDatabase.AddObjectToAsset(color,profile);EditorUtility.SetDirty(profile);
            }
            var volume=new GameObject("Presentation grading").AddComponent<Volume>();volume.isGlobal=true;volume.priority=5;volume.sharedProfile=profile;
        }
        private static GameObject Model(string path,Transform parent,string name,Vector3 position,Quaternion rotation,Vector3 scale,bool centerOnPosition=false)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new InvalidOperationException("Missing model: "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            go.name=name;go.transform.SetParent(parent,true);go.transform.position=position;go.transform.rotation=rotation;go.transform.localScale=scale;
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
            if(centerOnPosition){var bounds=Bounds(go);go.transform.position+=position-bounds.center;}
            return go;
        }
        private static Bounds Bounds(GameObject go)
        {var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new InvalidOperationException("Model has no renderer.");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        private static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static void NeutralWorldScale(Transform transform)
        {var s=transform.parent.lossyScale;transform.localScale=new Vector3(1/s.x,1/s.y,1/s.z);}
        private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,true);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
        private static Material Mat(string name,Color color)
        {string path=Content+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(mat,path);return mat;}
        private static void Label(string text,Transform parent,Vector3 position,float size)
        {var label=Child(text,parent).gameObject.AddComponent<TextMesh>();label.text=text;label.transform.position=position;label.characterSize=size;label.fontSize=40;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.82f,.86f,.76f);}
        private static void Lamp(Transform parent,Vector3 position,Color color,float intensity,float range)
        {
            var light=Child("Practical light",parent).gameObject.AddComponent<Light>();light.transform.position=position;
            light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;light.gameObject.AddComponent<UniversalAdditionalLightData>();
            var glass=Mat(color.r>.9f?"Warm lamp glass":"Checkpoint lamp glass",color);
            glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",color*1.5f);EditorUtility.SetDirty(glass);
            Box("Lamp housing",parent,position,new Vector3(.46f,.12f,.28f),Mat("Fittings",new Color(.12f,.14f,.15f)));
            Box("Lamp diffuser",parent,position-Vector3.up*.063f,new Vector3(.35f,.02f,.20f),glass);
        }
        private static void Reference(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void VerifyPaths(Scene scene)
        {
            var start=InScene<FirearmController>(scene).Single().transform.position;
            if(!NavMesh.SamplePosition(start,out var origin,2,NavMesh.AllAreas))throw new InvalidOperationException("Player spawn lost navigation.");
            foreach(var point in InScene<Transform>(scene).Where(t=>t.name.StartsWith("Spawn ")))
            {
                if(!NavMesh.SamplePosition(point.position,out var hit,1.5f,NavMesh.AllAreas))throw new InvalidOperationException("Spawn lost navigation: "+point.name);
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(origin.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Disconnected spawn: "+point.name);
            }
            foreach(var container in InScene<LootContainer>(scene))
            {
                if(!NavMesh.SamplePosition(container.transform.position,out var loot,1.75f,NavMesh.AllAreas))throw new InvalidOperationException("Loot has no accessible floor: "+container.name);
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(origin.position,loot.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Loot building inaccessible: "+container.name);
            }
            var zone=InScene<ExtractionZone>(scene).Single();var route=new NavMeshPath();
            if(!NavMesh.SamplePosition(zone.transform.position,out var extraction,2,NavMesh.AllAreas)||!NavMesh.CalculatePath(origin.position,extraction.position,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Extraction unreachable.");
        }
        private static void Validate(Scene scene)
        {
            foreach(var t in InScene<Transform>(scene))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script: "+t.name);
            foreach(var r in InScene<Renderer>(scene).Where(r=>r.enabled && r.GetComponent<TextMesh>() == null))
                foreach(var mat in r.sharedMaterials)if(mat==null || mat.shader==null)throw new InvalidOperationException("Missing material/shader: "+r.name);
            Debug.Log("[Presentation] "+scene.name+" updated; existing gameplay objects retained.");
        }
    }
}
