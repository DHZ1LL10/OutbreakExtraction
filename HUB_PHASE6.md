# Phase 6 — Minimal playable Hub

## Start and manual demo

1. Exit Play Mode. Run **Outbreak > Crear HUB de prueba**, then enter Play in Hub_Test. The existing FPS/bodycam walks normally. No firearm component exists in the Hub, so firing is unavailable. Stash/Loadout are near Z=2, Quartermaster/Workbench near Z=6, Missions/Deploy near Z=10, on the left/right sides of the room.
2. Approach STASH within 2.4m, look at it and press E. The existing modal suppression frees the cursor and blocks aim/fire/lean. Inspect items, equip DebugRifle into Primary and DebugPistol into Secondary. Those items leave Stash. Return to Stash/unequip must put them back exactly once. Loadout station shows all nine existing slots, including Armor/Backpack/Rig/Equipment1–4.
3. At WORKBENCH select Primary or Secondary and choose the existing Red Dot, Suppressor/Compensator, Extended Magazine and compatible Grip. None removes a slot. Rifle/Pistol configurations remain independent and are saved by weapon ItemDefinition ID. This debug workbench provides free presets, not physical attachment ownership or a second inventory. Grip is filtered out for the pistol by its existing compatibility rule.
4. At QUARTERMASTER, BUY rifle/pistol/ammo/medical supplies. Ammo is sold in batches of 30; other stock in units of one. Watch actual wallet and Stash change. SELL removes units/stacks from Stash and credits the wallet. Insufficient money or full Stash rejects the transaction. Existing debug gunplay ammo reserves remain 180/75; purchased ammo is a real inventory stack and may be prepared in existing Equipment slots, but this phase does not add a round-by-round inventory ammo adapter.
5. At DEPLOY inspect the prepared loadout and click START RAID. At least one supported weapon is required (rifle Primary and/or pistol Secondary, matching the current debug viewmodels). RaidLoop_Test loads with only that prepared equipment and its saved attachments. In this connected delivery flow, F1–F4 debug changes are disabled; use Workbench. Opening gunplay/raid scenes directly still uses the original debug setup.
6. Loot, reach the green extraction zone and remain five seconds. At RAID SUCCESS click Return To Hub, or H. The physical Hub loads with extracted loot and surviving equipment in Stash, preserving money/progress. Equipment returns to Stash by the existing domain rule; prepare the loadout again for another deploy. Sell extracted Scrap at Quartermaster and confirm money increases.
7. Deploy again, collect loot and die. Wait for bodycam death to finish. Return To Hub button is visible above the completed fade; H also works. Deployed equipment/new loot must be lost while earlier Stash loot and wallet remain.
8. Exit Play and start Hub_Test again: wallet, Stash, loadout and weapon configuration load from the same saved profile. Close station panels with Close/Escape or walk beyond 3.3m. Mouse control restores without firing from the UI click. E remains right lean when no station is focused.

## Profile and scene boundaries

Uses the existing GameManager/GameFlow/PlayerProfile/SaveService and **profile.json** at Application.persistentDataPath. The console logs the actual path. No separate Hub wallet/inventory/profile is created. Starter money $1500, rifle, pistol, 60 rounds of each ammo type and a medical item are granted and saved only if that file does not exist. A valid existing profile is preserved; an unreadable existing save is preserved and Hub operations remain blocked with an error rather than overwriting it. Existing valid saves are not topped up automatically.

The Hub catalog contains existing debug ItemDefinition assets, including identities from earlier phases. The four gunplay item assets had zero prices; generator fills missing debug prices only: rifle $320/$160, pistol $180/$90, ammo $2/$1 per round. Other existing prices remain unchanged.

Only the Hub-created manager opts into DontDestroyOnLoad. Scene-local raid managers defer to it instead of loading a second save/session. RaidRuntimeContext reads that persistent flow, and never starts a second raid if one is active. HubSceneFlow applies the deployed loadout through FirearmController.EquipFromRaid and saved presets through WeaponRuntimeState.TryApplyAttachments. The original standalone raid context behavior remains available without a Hub manager.

Hub transactions wrap the existing Equip/Unequip/Buy/Sell domain APIs in GameFlow.CommitHubChange. A failed operation or save restores the previous profile; no money/item duplication and no silent successful operation that was not saved. ReturnToHubAndSave likewise restores the terminal raid state if saving fails, allowing a retry. Invalid deploy/loadout/scene checks occur before StartRaid removes deployed equipment from the profile.

Attachment save data is an optional additional version-1 field. Older saves with no field restore with empty presets. IDs are validated against the same ItemCatalog. Presets are configuration only, survive replacing/losing an item of that weapon type, and are validated again against the existing AttachmentSet/compatibility when configured or deployed. There is no per-instance weapon identity in the existing ItemStack architecture.

## Files created

- Assets/Game/Runtime/Hub/HubSceneFlow.cs: scene coordination and starter initialization.
- Assets/Game/Runtime/Hub/HubLoadoutAdapter.cs: existing attachment APIs and delivery loadout validation.
- Assets/Game/Runtime/Hub/HubStation.cs plus StashStation.cs, LoadoutStation.cs, TraderStation.cs, WorkbenchStation.cs, MissionStation.cs, DeployStation.cs: six spatial interactables sharing the existing panel contract.
- Assets/Game/Runtime/Hub/HubDebugUI.cs: temporary list/button UI; Mission panel only says COMMANDER / No active assignment.
- Assets/Game/Editor/FPS/HubTestEnvironment.cs: Outbreak > Crear HUB de prueba generator. Regeneration replaces only Hub_Test after confirmation; saves and other scenes remain intact.
- Assets/Game/Scenes/Hub_Test.unity and Assets/Game/DebugContent/Hub/: scene, combined catalog and three primitive materials, with Unity metas.
- Assets/Game/Tests/Editor/HubLoopTests.cs and Assets/Game/Tests/PlayMode/HubSceneFlowTests.cs: eight focused domain tests and one non-rendering scene lifecycle test, each using unique test saves.
- HUB_PHASE6.md: instructions and validation record.

## Files modified

- Runtime/Core/GameFlow.cs: atomic saved Hub operations and saved return.
- Runtime/Core/GameManager.cs: optional scene persistence and loaded-profile status.
- Runtime/Core/PlayerProfile.cs, Runtime/Persistence/SaveData.cs and SaveMapper.cs: small optional attachment configuration persistence boundary.
- Runtime/Raids/RaidRuntimeContext.cs: reads an existing persistent raid flow when available.
- Runtime/Raids/RaidLoopDebugUI.cs: physical Return To Hub button/H path when connected, and readable failure actions above death fade; standalone logical return remains.
- DebugContent/Gunplay/DebugRifleItem.asset, DebugPistolItem.asset, DebugRifleAmmoItem.asset, DebugPistolAmmoItem.asset: missing debug prices.
- ProjectSettings/EditorBuildSettings.asset: Hub_Test first and RaidLoop_Test enabled, preserving other entries.

FPS_Test, Gunplay_Test, Infected_Test and RaidLoop_Test scene files are unchanged. PlayerMotor, PlayerInput, PlayerLook, BodycamController, inventory implementations, damage/AI and existing attachments are unchanged. Git started clean at 70fe0c4; no user changes needed preservation.

## Validation

Unity 2023.2.20f1 built-in Roslyn compiles both runtime, both Editor and both test assemblies without C# warnings/errors. EditMode suite: **119/119 passed**. Focused non-rendering PlayMode scene integration: **1/1 passed**, testing both success and failed raids, actual scene changes, singleton continuity, correct equipped firearm/attachments and saved loot/money. Tests did not interact visually with UI, inspect rendering or modify profile.json. Logs/HubGenerator.log, Logs/HubTests.xml and Logs/HubSceneTests.xml are ignored generated reports. Unity emitted transient licensing signature/token messages, then resolved entitlements and completed each run with exit 0; no game C# errors or failed tests remain.

Manual Unity validation remains pending. No external assets, browser, visual automation, Phase 7 mission, final UI or phase-6 tag were added.
