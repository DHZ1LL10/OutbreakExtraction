using System;
using System.Collections.Generic;
using System.Linq;
using Outbreak.Items;
using Outbreak.Weapons;
using UnityEditor;
using UnityEngine;

namespace Outbreak.Editor
{
    public static class ImportedContentSetup
    {
        private const string Content="Assets/Game/ImportedContent";
        private const string Shotgun="Assets/Low Poly ShotGun Weapon Pack 1/Prefabs/";
        private static ItemCatalog catalog;
        private static readonly List<ItemDefinition> items=new List<ItemDefinition>();
        private static readonly List<ItemPreviewLibrary.Entry> previews=new List<ItemPreviewLibrary.Entry>();
        private static readonly List<ImportedContentRegistry.AttachmentModelReference> attachmentModels=new List<ImportedContentRegistry.AttachmentModelReference>();
        [MenuItem("Outbreak/Preparar contenido lógico Fase 10A")]
        public static void Apply()
        {
            if(!AssetDatabase.IsValidFolder(Content)) AssetDatabase.CreateFolder("Assets/Game","ImportedContent");
            items.Clear(); previews.Clear(); attachmentModels.Clear();
            catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/DebugContent/Hub/HubCatalog.asset");
            var pistolAmmo=catalog.Get("debug_pistol_ammo"); var rifleAmmo=catalog.Get("debug_rifle_ammo");
            var slug=Item("shotgun_slug","Shotgun Slugs",ItemType.Ammo,ItemRarity.Uncommon,8,4,40,"Single-projectile shotgun ammunition.");
            var weapons=new[]{
                Weapon("tec9","Tec9",WeaponClass.Pistol,pistolAmmo,"Assets/tec9/prefab/Tec9.prefab",16,24,96,900,FireMode.Automatic,55,2.2f,.28f,.6f,.3f,1.9f,800,AttachmentSlots.Muzzle|AttachmentSlots.Magazine),
                Weapon("light_smg","Light SMG",WeaponClass.Rifle,pistolAmmo,"Assets/WeaponsPack (LowPoly)/American Light SMG.prefab",18,30,150,840,FireMode.Automatic,75,1.9f,.2f,.65f,.28f,2,1150,AttachmentSlots.All),
                Weapon("light_carbine","Light Carbine",WeaponClass.Rifle,rifleAmmo,"Assets/WeaponsPack (LowPoly)/American Light AssaultRifle.prefab",26,25,125,480,FireMode.SemiAutomatic,125,1.2f,.05f,.85f,.18f,2.1f,1450,AttachmentSlots.All),
                Weapon("american_pistol","American Pistol",WeaponClass.Pistol,pistolAmmo,"Assets/WeaponsPack (LowPoly)/American Pistol.prefab",30,10,50,300,FireMode.SemiAutomatic,90,1,.08f,1.4f,.22f,1.5f,650,AttachmentSlots.Muzzle|AttachmentSlots.Magazine),
                Weapon("shotgun_a_slug","Shotgun A / Slug",WeaponClass.Rifle,slug,Shotgun+"Weapons/ShotGun_A.prefab",68,6,24,100,FireMode.SemiAutomatic,45,2.4f,.32f,3.8f,.55f,2.8f,1600,AttachmentSlots.All)
            };
            var shotgun=weapons.Last(); shotgun.headshotMultiplier=1.4f; shotgun.noiseMultiplier=1.8f; shotgun.adsSpeed=8; EditorUtility.SetDirty(shotgun);
            weapons[0].noiseMultiplier=.75f; weapons[0].headshotMultiplier=1.75f; weapons[1].noiseMultiplier=.9f; weapons[2].noiseMultiplier=1.1f;
            foreach(var weapon in weapons) { weapon.Validate(); EditorUtility.SetDirty(weapon); }
            string[] longGuns={"debug_rifle","light_smg","light_carbine","shotgun_a_slug"};
            var optic=Attachment("reflex_a","Reflex A",AttachmentSlot.Optic,longGuns,240,Shotgun+"Attachments/Optics/Optics/Optic_A.prefab","DebugRedDot"); optic.modifiers.adsSpread=new StatModifier(.85f);
            var brake=Attachment("shotgun_brake_a","Shotgun Brake A",AttachmentSlot.Muzzle,new[]{"shotgun_a_slug"},180,Shotgun+"Attachments/Barrels/Muzzles/Muzzle_ShotGun_Device_A.prefab","DebugCompensator");
            brake.modifiers.recoilVertical=new StatModifier(.72f); brake.modifiers.noise=new StatModifier(1.2f); brake.modifiers.muzzleFlash=new StatModifier(1.25f);
            var suppressor=Attachment("shotgun_suppressor_a","Shotgun Suppressor A",AttachmentSlot.Muzzle,new[]{"shotgun_a_slug"},450,Shotgun+"Attachments/Barrels/Muzzles/Muzzle_ShotGun_Suppresor_A.prefab","DebugSuppressor");
            suppressor.modifiers.noise=new StatModifier(.3f); suppressor.modifiers.muzzleFlash=new StatModifier(.1f); suppressor.modifiers.recoilVertical=new StatModifier(.92f); suppressor.suppressesFireSound=true;
            var grip=Attachment("forward_grip_a","Forward Grip A",AttachmentSlot.Grip,longGuns,220,Shotgun+"Attachments/UnderBarrels/ForwardGrips/ForwardGrip_A.prefab","DebugVerticalGrip");
            grip.modifiers.recoilVertical=new StatModifier(.88f); grip.modifiers.recoilHorizontal=new StatModifier(.82f); grip.modifiers.adsSpeed=new StatModifier(.94f);
            var magazineModel=ExtractMagazine();
            var magazine=Attachment("shotgun_box_mag","Shotgun Box Magazine",AttachmentSlot.Magazine,new[]{"shotgun_a_slug"},320,AssetDatabase.GetAssetPath(magazineModel),"DebugExtendedMagazine");
            magazine.modifiers.magazineCapacity=new StatModifier(1,3); magazine.modifiers.reloadDuration=new StatModifier(1.12f);
            var attachments=new[]{optic,brake,suppressor,grip,magazine};
            foreach(var attachment in attachments) { attachment.Validate(); EditorUtility.SetDirty(attachment); }
            AppendCatalog();
            var registry=Asset<ImportedContentRegistry>("Assets/Game/Resources/ImportedContent.asset");
            registry.weapons=weapons; registry.attachments=attachments; registry.traderItems=items.ToArray(); registry.futureAttachmentModels=attachmentModels.ToArray();
            registry.infectedVisuals=Variants(); registry.infectedAudio=Audio();
            const string effects="Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/";
            registry.futureImpacts=new[]{
                Impact("generic",effects+"2_Impact_1.1.0/VFX_ImpactClassic01_1.1.0.prefab"),
                Impact("blood-compatible; URP conversion required",effects+"0_Classic/VFX_Blood_01.prefab"),
                Impact("stylized hit/sparks; not a verified metal surface effect",effects+"0_Classic/VFX_Classic_01.prefab")
            };
            EditorUtility.SetDirty(registry);
            var previewLibrary=AssetDatabase.LoadAssetAtPath<ItemPreviewLibrary>("Assets/Game/Resources/ItemPreviews.asset");
            previewLibrary.entries=previewLibrary.entries.Where(e=>!previews.Any(p=>p.item==e.item)).Concat(previews).ToArray(); EditorUtility.SetDirty(previewLibrary);
            AssetDatabase.SaveAssets(); catalog.Validate();
            Debug.Log("[Phase10A] 5 new weapons + 5 attachments registered. Existing 2 weapons and scene presentation unchanged.");
        }
        private static T Asset<T>(string path) where T:ScriptableObject
        { var asset=AssetDatabase.LoadAssetAtPath<T>(path); if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset; }
        private static ItemDefinition Item(string id,string title,ItemType type,ItemRarity rarity,int buy,int sell,int stack,string description)
        {
            var item=Asset<ItemDefinition>(Content+"/"+id+"_item.asset"); var data=new SerializedObject(item);
            data.FindProperty("id").stringValue=id; data.FindProperty("displayName").stringValue=title; data.FindProperty("description").stringValue=description;
            data.FindProperty("type").enumValueIndex=(int)type; data.FindProperty("rarity").enumValueIndex=(int)rarity;
            data.FindProperty("buyValue").intValue=buy; data.FindProperty("sellValue").intValue=sell; data.FindProperty("maxStack").intValue=stack; data.ApplyModifiedPropertiesWithoutUndo();
            item.Validate(); items.Add(item); return item;
        }
        private static WeaponDefinition Weapon(string id,string title,WeaponClass weaponClass,ItemDefinition ammo,string modelPath,float damage,int magazine,int reserve,float rpm,FireMode mode,float range,float hip,float ads,float vertical,float horizontal,float reload,int price,AttachmentSlots slots)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath); if(model==null || model.GetComponentsInChildren<Renderer>(true).Length==0) throw new InvalidOperationException("Invalid selected model: "+modelPath);
            string description=id=="tec9"?"Compact automatic sidearm for close-range encounters.":
                id=="light_smg"?"Light automatic weapon with a fast cyclic rate, best at short range.":
                id=="light_carbine"?"Balanced carbine with precise fire for medium-range encounters.":
                id=="american_pistol"?"Reliable semi-automatic sidearm with a compact magazine.":
                "Heavy close-range weapon firing a single slug per shot.";
            var item=Item(id,title,ItemType.Weapon,ItemRarity.Uncommon,price,price/2,1,description);
            var weapon=Asset<WeaponDefinition>(Content+"/"+id+"_weapon.asset"); weapon.item=item; weapon.ammoType=ammo; weapon.weaponClass=weaponClass;
            weapon.damage=damage; weapon.magazineCapacity=magazine; weapon.initialReserveAmmo=reserve; weapon.fireRate=rpm; weapon.fireMode=mode;
            weapon.allowModeSwitch=id=="tec9" || id=="light_smg" || id=="light_carbine";
            weapon.range=range; weapon.hipSpread=hip; weapon.adsSpread=ads; weapon.recoilVertical=vertical; weapon.recoilHorizontal=horizontal; weapon.reloadDuration=reload;
            weapon.supportedAttachmentSlots=slots; weapon.visualKick=weaponClass==WeaponClass.Rifle?.025f:.035f; weapon.visualRotation=vertical*2;
            previews.Add(new ItemPreviewLibrary.Entry {item=item,model=model}); return weapon;
        }
        private static AttachmentDefinition Attachment(string id,string title,AttachmentSlot slot,string[] ids,int price,string modelPath,string placeholder)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath); if(model==null) throw new InvalidOperationException(modelPath);
            var item=Item(id,title,ItemType.Attachment,ItemRarity.Uncommon,price,price/2,1,"Weapon accessory for the "+slot.ToString().ToLowerInvariant()+" slot. Fit depends on the equipped weapon.");
            var attachment=Asset<AttachmentDefinition>(Content+"/"+id+"_attachment.asset"); attachment.item=item; attachment.slot=slot;
            attachment.compatibleClasses=AttachmentWeaponClasses.Rifle; attachment.compatibleWeaponIds=ids; attachment.modifiers=new AttachmentModifiers(); attachment.suppressesFireSound=false;
            // Logical customization now, imported socket alignment later. Preserve the tested placeholder visuals/anchors.
            attachment.visualPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/DebugContent/Gunplay/Attachments/"+placeholder+".prefab");
            previews.Add(new ItemPreviewLibrary.Entry {item=item,model=model}); attachmentModels.Add(new ImportedContentRegistry.AttachmentModelReference {attachment=attachment,model=model}); return attachment;
        }
        private static GameObject ExtractMagazine()
        {
            string path=Content+"/ShotgunMagazineModel.prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Shotgun+"Weapons/ShotGun_A.prefab").GetComponentsInChildren<Transform>(true).Single(t=>t.name=="ShotGun_A_Mag");
            var root=new GameObject("Shotgun A magazine visual");
            try { var copy=UnityEngine.Object.Instantiate(source.gameObject,root.transform); copy.transform.localPosition=Vector3.zero; copy.transform.localRotation=Quaternion.identity; return PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void AppendCatalog()
        {
            var data=new SerializedObject(catalog); var list=data.FindProperty("items");
            foreach(var item in items)
            {
                bool found=false; for(int i=0;i<list.arraySize;i++) if(list.GetArrayElementAtIndex(i).objectReferenceValue==item) found=true;
                if(found) continue; int index=list.arraySize; list.arraySize++; list.GetArrayElementAtIndex(index).objectReferenceValue=item;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static ImportedContentRegistry.InfectedVisualVariant[] Variants()
        {
            var paths=new[]{"Assets/Kevin Iglesias/Zombie Animations/Prefabs/Zombie.prefab","Assets/3D Characters Zombie Hospital Lowpoly Pack - Lite/Prefabs/(P) Characters_Zombie_Pacient_04.prefab"};
            return paths.Select((path,index)=> {
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path); var avatar=model.GetComponentInChildren<Animator>(true)?.avatar;
                if(avatar==null || !avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("Unusable humanoid: "+path);
                return new ImportedContentRegistry.InfectedVisualVariant {id=index==0?"kevin_zombie":"hospital_patient_04",model=model,avatar=avatar,
                    allowNormal=true,allowRunner=true,controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/DebugContent/Presentation/InfectedVisual.controller")};
            }).ToArray();
        }
        private static ImportedContentRegistry.InfectedAudioReferences Audio()
        {
            var clips=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/ZombieHorrorPackageFree"}).Select(g=>AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            return new ImportedContentRegistry.InfectedAudioReferences {idle=clips.Where(c=>c.name.Contains("Idle")).ToArray(),attack=clips.Where(c=>c.name.Contains("Attack_A")).ToArray(),damage=clips.Where(c=>c.name.Contains("Hurt")).ToArray(),death=clips.Where(c=>c.name.Contains("BodyFall")).ToArray()};
        }
        private static ImportedContentRegistry.ImpactReference Impact(string category,string path) => new ImportedContentRegistry.ImpactReference {category=category,prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path)};
    }
}
