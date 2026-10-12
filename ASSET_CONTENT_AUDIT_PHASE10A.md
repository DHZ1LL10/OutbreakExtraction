# OUTBREAK: EXTRACTION — Phase 10A content audit

Scope: import cleanup and data/backend integration only. Hub_Test, RaidLoop_Test, MainMenu, lighting, NavMesh and infected population remain untouched. No downloads, browser, scene placement or visual pass.

## Import fixes and exclusions

- `Assets/AssetsStore/Garage_props/Demo/demo.unity` references absent GUID **87d6ece282c12ef43b920eb59f46aa98**, at `m_SourcePrefab` (original line 1417). A physical index of every Assets .meta confirms there is no source asset for it. Only this supplier scene is quarantined as `demo.unity.disabled`; its bytes and GUID are retained. All 37 usable garage prefabs, meshes and textures remain. Rename back through AssetDatabase only if the missing source is supplied. Do not include it in builds.
- `Assets/Nebula - Free low poly car pack/Bonus content/Meshes/Diorama.fbx`: the importer discards a self-intersecting polygon in **Tree2**. This is bonus scenery, not a car dependency. Diorama/Tree2 are excluded from approved future integration. The rest of the cars/signs/cones/barriers remain. No mesh reconstruction or unsafe FBX/YAML patching. The harmless importer warning may remain.
- Standard materials in the two newly imported weapon packs are converted through Unity APIs to URP Lit, retaining base texture/color, UV transform, normal map, metal/smoothness. Existing game/imported-old materials are not modified.
- Hospital's vendor rating popup now skips batch mode; it is editor-only and irrelevant to gameplay.
- Vendor trailing whitespace was normalized only in explicitly known text/YAML files. FBX, textures, WAV, unitypackages and binary .asset payloads were not rewritten.

## Complete physical inventory

`ASSET_PHYSICAL_INVENTORY_PHASE10A.md` lists every physical Assets root, extension counts, all prefab names, scene paths and shader counts. `ASSET_CONTENT_INDEX_PHASE10A.csv` indexes every non-meta file, including prefab/model collider counts, mesh/skinned renderer counts, transforms/bounds, attached scripts, Avatar validity, embedded clips, material shader and audio metadata. These are actual Unity inspections rather than assumed store names; counts below are the import-audit snapshot.

| Package / physical path | Content | Rig / animations / audio / VFX | URP / usefulness / integration decision |
|---|---|---|---|
| `Assets/DelthorGames` — AKM | 1 prefab, 1 FBX, 1 material, 7 PNG, demo | Static weapon; no animation/audio dependency | Existing URP AutodeskInteractive material; keep validated AKM unchanged. |
| `Assets/tec9` | 1 prefab + FBX, 1 material, 3 PNG, demo | Static weapon; no framework scripts | URP Lit; Tec9 backend/preview candidate now, FPS placement 10B. Bounds ~0.14×0.80×0.88 m; normalize/alignment needed for FPS. |
| `Assets/WeaponsPack (LowPoly)` | 40 prefabs/FBX/materials/textures, demo, SpinWeapon.cs | Static rifles/SMGs/pistols, some separable mechanical pieces | Standard → URP Lit safely converted. Select Light SMG, Light AssaultRifle and American Pistol only; spin script is demo-only. Others reserve. |
| `Assets/Low Poly ShotGun Weapon Pack 1` | 55 prefabs/FBX, 3 materials, texture atlas, demo, optional unitypackages | 5 shotgun assemblies, separate optics/muzzles/grips/rails/lights/lasers and bullets; no weapon framework | Standard → URP Lit converted. ShotGun_A data with single slug hitscan; five compatible attachment definitions. No lasers/flashlights/scopes enabled. |
| `Assets/Abandoned buildings` | 12 prefabs, 11 FBX, 9 materials, 13 TGA, 2 scenes | Static ruins/houses/roofs/stoves; no rigs/audio | Existing URP Lit; already used. Preserve current scene/material state. Useful 10B infill. |
| `Assets/UrbanBuilding` | 1 building prefab/FBX, 7 materials, 21 PNG, UrbunBuilding.unity | Static building; no rig/audio | Standard materials; 10B needs URP copies/conversion and collision authoring. Prefab ~8.18×4.50×12.83 m, 7 mesh filters, **0 colliders**. Not a whole city or proven enterable interior. |
| `Assets/ModularLowpolyStreetsFree` | 33 prefabs, 7 FBX, 23 materials, 63 PNG, 3 scenes | Roads/crossroads/pavement, traffic lights/cones, cafe furniture, bench/hydrant/sewer parts | Standard; reserve safe URP conversion for 10B. `Scenes/Demo.unity` is a streets fallback, not Polygon City. Walkability/collision/NavMesh must be authored in our future scene. |
| `Assets/AssetsStore/Garage_props` | 37 prefabs, 13 FBX, 6 materials, 15 TGA, quarantined demo | Shelves/racks/bench/table/chair/stairs; colored barrels/tanks/cylinders | 5 Standard + 1 AutodeskInteractive; useful industrial loot yards/interiors. Reserve URP adaptation; do not import missing demo dependency. |
| `Assets/LowPolyBarriersPackFree` | 17 prefabs/FBX, 2 materials, texture, demo/PDF | Concrete, wooden, sandbag barriers and parts | Standard; conversion reserved. Example ConcreteBarrier_05_v1 ~2×1×2 m, collider present. Good future cover/checkpoints. |
| `Assets/Nebula - Free low poly car pack` | 17 prefabs, 18 FBX, 55 materials, 53 PNG, demo/PDF | Cars and bonus signs/cones/barriers, no gameplay framework | Standard; reserve URP conversion and collision checks. Exclude only Diorama/Tree2; individual car/road props are usable. |
| `Assets/ithappy/Apocalypse_Free` | 69 prefabs, 67 FBX, 18 materials, 10 PNG, 41 .anim, demo for 3 pipelines | Modular humanoid/dog characters, props, pistol/car/fences/medical; third-person demo/face scripts | 10 URP Lit + 7 URP blood-overlay + skybox. Keep current content. Demo scripts not our player system. Adult_Survivor FBX has 9 skinned renderers, no Avatar on the source; full-body parts, not FPS arms. |
| `Assets/ADR_Textures` — concrete sample | 6 materials, 15 large PNG, 2 prefab samples, 2 demos | Static surface textures, no rig/animation/audio | 3 URP materials + 3 Standard. Use existing URP versions in 10B; memory budget matters. Do not place sample demo in raid. |
| `Assets/Kevin Iglesias/Zombie Animations` | 1 prefab, 7 FBX, 2 materials, 2 TGA, 5 controllers, demo | Valid Humanoid; embedded idle/walk/attack/damage/death clips | URP Unlit; current source unchanged. Prepared normal/runner visual registry entry, existing combined controller retained. |
| `Assets/3D Characters Zombie Hospital Lowpoly Pack - Lite` | 1 prefab/FBX, 2 materials, 4 PNG, sample scene, editor popup | Patient_04: valid Humanoid Avatar, 1 skinned renderer, no included motion clips; ~1.87×2.30×1.36 m bounds, no colliders | Body URP Lit + procedural sky. Retarget existing Humanoid controller; eligible for normal or runner visual roles. Needs 10B scale/pose/hitbox alignment; no current AI replacement. |
| `Assets/ZombieHorrorPackageFree` | 44 WAV | 6 idle VO, 6 attack VO, 6 hurt VO; bite, footsteps, flesh impacts, body fall, blood/eating/clothing/crawl Foley | Pipeline-independent. Data references only; no playback/mix added. No specifically-labelled alert/chase/death VO. |
| `Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion` | 18 prefabs, 1 FBX, 82 materials, 77 PNG, demo, 2 demo scripts | Generic/stylized hits, critical/arcade/toon, blood, poison | **71 Mobile/Particles/Additive + 11 Mobile/Particles/Alpha Blended**: built-in particle materials need URP adaptation. Reserve references only; no active replacement, no verified material-specific concrete/metal classification. |
| `Assets/Game`, `Assets/Settings`, `Assets/Scenes`, `Assets/TutorialInfo` | Project code/data/scenes, URP assets and template/readme | Own stable runtime + sample scene/readme | Not newly imported gameplay packs. Audit includes them; preserve stable systems and previous local edits. |

### Requested packages absent from the actual filesystem

No independent **Polygon City Pack**, **Demo City**, **Zombie City Streets Lite**, **Survivalist Character**, **Weapons Pack – Realistic LowPoly**, **Low Poly Weapon Series** or **Low Poly Weapon Series WWII** folder/content was found. `WeaponsPack (LowPoly)` does include modern and WWII-named firearms, but must not be represented as separately imported packs. No package is downloaded or unpacked speculatively.

## Polygon City: priority for 10B

**Primary intended map base remains Polygon City Pack, but it is not imported and no scene/prefab can be selected or measured.** Therefore city dimensions, interiors, walkability, colliders, scripts and URP compatibility cannot honestly be certified. Best Polygon City scene: **unavailable**. Import the actual package before 10B; then duplicate its best city scene into an owned Outbreak scene, strip vendor demo scripts, adapt materials/collision and rebake through the existing safe NavMesh APIs. Never turn the original supplier demo into our raid.

Current fallback for planning only: `Assets/ModularLowpolyStreetsFree/Scenes/Demo.unity` + `Assets/UrbanBuilding/Prefabs/urban_building_Prefab.prefab`. This is not substituted into RaidLoop_Test.

## Selected weapon data

Keep AKM and Pistol1 unchanged. Five new definitions use ItemDefinition + WeaponDefinition and the current FirearmController. Prices are purchase/sale; reserves retain the existing runtime initialization design and do not implement inventory-based ammo consumption.

| Weapon | Exact source | Slot | Damage | Mag/reserve | RPM/mode | Range | Hip/ADS spread | Recoil V/H | Reload | Noise | Price |
|---|---|---|---:|---|---|---:|---|---|---:|---:|---|
| Tec9 | `Assets/tec9/prefab/Tec9.prefab` | Secondary | 16 | 24/96 | 900 auto, switchable | 55 | 2.2/.28 | .60/.30 | 1.9 | .75 | $800/$400 |
| Light SMG | `Assets/WeaponsPack (LowPoly)/American Light SMG.prefab` | Primary | 18 | 30/150 | 840 auto, switchable | 75 | 1.9/.20 | .65/.28 | 2.0 | .90 | $1150/$575 |
| Light Carbine | `Assets/WeaponsPack (LowPoly)/American Light AssaultRifle.prefab` | Primary | 26 | 25/125 | 480 semi, switchable | 125 | 1.2/.05 | .85/.18 | 2.1 | 1.10 | $1450/$725 |
| American Pistol | `Assets/WeaponsPack (LowPoly)/American Pistol.prefab` | Secondary | 30 | 10/50 | 300 semi | 90 | 1.0/.08 | 1.40/.22 | 1.5 | 1.00 | $650/$325 |
| Shotgun A / Slug | `Assets/Low Poly ShotGun Weapon Pack 1/Prefabs/Weapons/ShotGun_A.prefab` | Primary | 68 | 6/24 | 100 semi | 45 | 2.4/.32 | 3.80/.55 | 2.8 | 1.80 | $1600/$800 |

Shotgun is explicitly **one hitscan slug**, not buckshot/pellets/pump simulation. No new weapon system. The existing `WeaponClass.Rifle` currently identifies primary long guns; it includes SMG/carbine/slug shotgun in this data phase. Existing recoil/spread/ADS/reload/wall avoidance code is reused.

**Visual integration deferred:** new selected guns use the already-tested primary/secondary class viewmodels in Raid until 10B. Their actual imported models appear only in the permitted detail previews and data references. This avoids silently claiming a complete FPS model/socket alignment pass. New weapons are bought normally, not added to starter gear.

## Attachment compatibility

Five existing debug attachments remain, governed by their existing class/slot rules. New imported pieces are separate source objects; no fused gun surface is claimed to be dynamically removable.

| New definition | Slot | Logical compatible weapons | Source / effect |
|---|---|---|---|
| Reflex A | Optic | AKM, Light SMG, Light Carbine, Shotgun A | `.../Attachments/Optics/Optics/Optic_A.prefab`; ADS spread ×.85 |
| Forward Grip A | Grip | AKM, Light SMG, Light Carbine, Shotgun A | `.../Attachments/UnderBarrels/ForwardGrips/ForwardGrip_A.prefab`; recoil V×.88/H×.82, ADS speed×.94 |
| Shotgun Brake A | Muzzle | Shotgun A only | `.../Attachments/Barrels/Muzzles/Muzzle_ShotGun_Device_A.prefab`; vertical recoil×.72, noise×1.2, flash×1.25 |
| Shotgun Suppressor A | Muzzle | Shotgun A only | `.../Attachments/Barrels/Muzzles/Muzzle_ShotGun_Suppresor_A.prefab`; noise×.30, flash×.10, recoil×.92 |
| Shotgun Box Magazine | Magazine | Shotgun A only | Actually separate `ShotGun_A_Mag` mesh, extracted into a presentation prefab; capacity 6→9, reload×1.12 |

Tec9/American Pistol expose Muzzle/Magazine only. Other new long guns expose all four slots. Original attachments therefore stay compatible only where a slot/class allows them. Physical fit of new source pieces still needs 10B socket authoring; runtime uses the tested corresponding debug visual/anchor prefabs while imported pieces are used for preview/future references. Capacity changes retain existing ammo reconciliation without creating/discarding rounds.

## Zombies, arms, audio and VFX

- Prepared two model entries for **both normal and runner roles**: Kevin Zombie and Hospital Patient_04. This means two appearances usable by either role, not four distinct models. Both have valid Human Avatars and can use the existing Idle/Walk/Attack/Damage/Death controller through Humanoid retargeting. Infected AI, movement, population, hitboxes and current visuals are unchanged; bone/hitbox placement is a 10B task.
- **Survivalist FPS arms: NOT SUITABLE with the assets currently available.** The specifically named Survivalist Character package is absent. The existing ithappy Adult_Survivor is full-body modular/skinned content, with no standalone FPS arm setup or source Avatar; reusable gloves do not provide a complete FPS arms solution. No arms are integrated.
- Audio reference groups: Idle 6, Attack 6, Damage/Hurt 6, Death body-fall Foley 3. Alert/Chase references intentionally null: no specifically named source clips. Additional bite 2, concrete footsteps 4, flesh/gory impacts 5 and other Foley are indexed. These are data hooks, with no runtime playback or mixing pass.
- VFX future references: ImpactClassic01 generic hit, Blood_01 blood-compatible, Classic_01 stylized sparks/hit. No verified concrete/metal-specific effect is fabricated. All remain inactive until URP particle conversion/visual review in a later pass. Poison/arcade/toon effects are not activated.

## Approved future environment subset

- Garage furniture: `Assets/AssetsStore/Garage_props/Assets/Prefabs/{table,bench,rack,rack2,rack3,stairs,stul}.prefab`; colored barrel/tank/cylinder folders.
- Road layout: ModularLowpolyStreetsFree `Prefabs/Roads` and `Prefabs/Complex`; street clutter under `Prefabs/Other` (cones, hydrant, bench, cafe furniture, sewer parts, traffic lights).
- Cover: LowPolyBarriersPackFree `BarriersAssets/Prefabs/{ConcreteBarriers,SandBagBarriers,WoodenBarriers}`.
- Cars/bonus road props: Nebula individual prefabs indexed in CSV, **excluding Bonus content/Prefabs/Diorama.prefab and its Tree2 mesh**.
- Existing apocalypse car/concrete fence/first aid/fuel and abandoned ruins are retained, not newly placed.

## Integration / validation / Git boundaries

`ImportedContentRegistry` is a Resources data registry referencing existing ItemDefinitions/WeaponDefinitions/AttachmentDefinitions, avatars, audio and future VFX; loading it does not spawn or play anything. HubSceneFlow merges its weapon, attachment and trader entries into the current arrays. HubCatalog includes new identities before SaveService restoration; save format is unchanged. PreviewLibrary recognizes source models. WeaponDefinition's optional reserve override preserves legacy definitions with -1 and uses explicit imported reserves during EquipFromRaid.

Prior material/lamp/URP edits are preserved locally and excluded from these two commits; MainMenu has no pending diff in the current state. No destructive Git operation. All imports/metas are versioned; Library/Temp/logs/build caches remain ignored. Validation results and exact commits are reported on completion.
