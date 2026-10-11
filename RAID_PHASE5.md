# Phase 5.1 — Raid Interaction, Loot, Extraction and Wander Polish

## Runtime boundaries

- `PlayerInteractor` owns the camera raycast, interaction prompt and loot panel lifecycle. `PlayerInput.SetGameplaySuppressed(owner, value)` gives each modal an independent suppression owner. Loot/inventory panels leave WASD walking available, while mouse aim, fire, lean, sprint, jump, reload, weapon selection and debug attachment input are suppressed. Existing crouch is preserved. Closing clears sampled input, delays resumption and requires releasing mouse buttons before firing again.
- `LootContainer` delegates generation to the existing `LootTable` and current `RaidMapDefinition`. Its `LootContainerInventory` rolls once and keeps remaining contents in memory for the raid.
- `WorldItem` adds to `RaidSession.FoundLoot` before disabling its GameObject.
- `ExtractionZone` uses physical trigger occupancy, PlayerHealth and the active RaidSession. It starts automatically, cancels on exit/death/inactive session and requests `GameFlow.ExtractSuccessfully()` after five seconds. It has no PlayerInteractor or key dependency. Transfer, persistence and the exactly-once guard remain owned by GameFlow. A rejected transfer leaves the raid active and retries after a fresh countdown. On success player movement/input and infected targeting that player stop; debug result UI replaces the countdown.
- `RaidLoopDebugUI` is temporary IMGUI used only by the generated test scene. Discard removes loot and intentionally does not spawn a world drop.

## Generate the test raid

1. Exit Play Mode.
2. To preserve the existing scene and manual geometry/navigation changes, open RaidLoop_Test and run **Outbreak > Actualizar Raid Loop a Fase 5.1**, then save. Alternatively run **Outbreak > Crear entorno de prueba Raid Loop** and confirm regeneration only if you want to replace the scene.
3. Open the generated `Assets/Game/Scenes/RaidLoop_Test.unity` if Unity does not open it automatically.
4. Enter Play Mode.

The generator creates the debug catalog and rarity items, five loot tables, `RaidLoopMap`, a baked NavMesh, five containers, three world pickups, four normal infected, two runners and one extraction zone. The debug profile uses `raidloop_profile.json` so normal profile data is not mixed with this test.

## Controls and validation

- `E`: search/pick up when focused, otherwise lean right. Extraction requires no key: remain in the green zone for five seconds.
- `Tab`: open/close the raid inventory. `Discard` removes a loot stack; no world object is created in this debug implementation.
- `Escape`: close the active debug panel.
- `1` / `2`, mouse buttons, `R`, `B`, `F1`–`F4`: existing gunplay and attachment controls.
- `H`: return to the logical Hub state after success/failure.

## Exact manual validation (visual validation remains pending)

A. Enter Play, leave the safe spawn pocket through the opening toward +Z, and approach the Toolbox in the west building (approximately X=-13, Z=6). Look at it from within 2.4 metres and press E. Cursor must be visible/free; moving the mouse must not turn the camera and Q/E or mouse clicks must not shoot/lean. Click Take, then Take All. Tab inventory shows accepted items. Close with Close or Escape; cursor returns to FPS and that click must not fire. Fill the 16-slot backpack across containers; rejected whole stacks remain. Close/reopen a container: remaining items do not reroll. The existing design transfers whole stacks atomically, without partial Take.

B. Reopen a nearby container, hold S or A/D to walk away with its panel open. Beyond 3.3 metres the panel must close automatically, mouse control returns and held UI clicks do not fire. Between 2.4 and 3.3 metres an already-open panel stays open, but a closed panel cannot open there. In Play, disabling the container GameObject in the Inspector must also close its panel cleanly.

C. Reach the green extraction zone near X=0, Z=48. Enter without pressing E or any interaction key. Screen-space EXTRACTION / HOLD POSITION and countdown start near 5.0. Remain there five seconds: RAID SUCCESS appears once, showing extracted loot/value; stash receives loot once. No huge HOLD E label should appear. H returns to the logical Hub. Restart Play for another trial.

D. Enter extraction, leave before five seconds: EXTRACTION CANCELLED appears briefly. Reenter: countdown restarts at five, without retained progress. On a separate trial let an infected kill you during countdown: extraction cancels, bodycam death runs and RAID FAILED appears without successful loot transfer. Outside focused interactions/modal UI, verify Q/E lean, including inside extraction (E never controls the countdown).

E. Observe infected for 1–2 minutes without alerting them; use cover and Scene View manually if needed. Six spawn indices alternate Active/Passive, with the sixth Idle-prone: active pauses 0.6–1.8s/radius 6m, passive 1.8–4s/radius 4.5m, idle-prone 5–11s/radius 3m. Destination selection is delayed randomly per cycle; destinations stay around spawn and avoid trivial movement. Confirm varied walking/pauses without synchronization or jitter. Then alert an infected to check Chase/Investigate/Attack still work. Kill several: Scrap pickup drops are probabilistic, default 30%, rather than guaranteed. Pick up with E; full backpack leaves the pickup.

## Changed files and configuration

- `Assets/Game/Runtime/Player/PlayerInput.cs`: modal suppression, mouse release guard and lean arbitration.
- `Assets/Game/Runtime/Interaction/PlayerInteractor.cs`, `IInteractable.cs`: panel ownership, close/liveness checks and optional spatial panel contract.
- `Assets/Game/Runtime/Loot/LootContainer.cs`: open 2.4m / forced-close 3.3m, measured root-to-container centre. `WorldItem.cs`: initialization API reused by drops. LootContainerInventory and RaidSession transfer logic are unchanged.
- `Assets/Game/Runtime/Raids/ExtractionZone.cs`: automatic occupancy countdown and existing GameFlow integration. `RaidLoopDebugUI.cs`: input suppression for inventory, screen-space extraction status, removal of hold-E instructions.
- `Assets/Game/Runtime/Infected/InfectedBrain.cs`, `InfectedController.cs`, `InfectedSpawner.cs`: configurable wander profiles, delayed destination selection, spawn mix and raid-end stop. Existing Chase/Investigate/Attack decisions remain unchanged.
- New `Assets/Game/Runtime/Infected/InfectedLootDrop.cs` and `.meta`: configurable probabilistic pickup using existing LootTable/WorldItem. Inspection found no infected death drop component in the starting source; Scrap previously existed as a placed world pickup. The requested occasional death drop is now explicit.
- New `Assets/Game/DebugContent/RaidLoop/InfectedScrapLoot.asset` and `.meta`: single Scrap entry, 1–2 units, table roll chance 100%; the spawner's separate dropChance gates whether a corpse rolls it (default 30%).
- `Assets/Game/Editor/FPS/RaidLoopTestEnvironment.cs`: generator and non-destructive scene upgrade menu.
- `Assets/Game/Scenes/RaidLoop_Test.unity`: five-second duration, old label disabled, variedWander and drop table assigned. Existing manual scene edits and the user's additional NavMesh asset were retained. FPS_Test, Gunplay_Test, Infected_Test and ProjectSettings were not changed.
- `Assets/Game/Tests/Editor/RaidLoopTests.cs`: modal ownership, distance/liveness, lean, atomic loot, automatic extraction/cancellation/once-only transfer and wander parameter regressions.
- `RAID_PHASE5.md`: updated instructions and validation record.

## Code validation

Unity 2023.2.20f1 bundled Roslyn compiled Outbreak.Game, Outbreak.Bodycam, both Editor assemblies and both test assemblies without C# warnings/errors. Unity EditMode suite: **109 passed, 0 failed** (94 previous + 15 new cases). Reports: `Logs/Phase51Tests.xml` and `Logs/Phase51Tests.log` (ignored by Git). Tests ran headlessly in the existing temporary validation copy; no visual automation was performed. Startup logged transient licensing signature/token errors, then successfully resolved entitlements and exited with code 0. First import of that copy also emitted immutable package orphan-meta warnings; the final test run had no C# warnings or failed tests. PlayMode assembly was compiled, not visually executed. This was the validation record before the final extraction/ADS fixes below; no Phase 6 work was added.

## Final extraction and ADS bug fixes

The physical extraction failure was reproduced headlessly with the actual CharacterController: traversing the legacy 8 x 0.16 x 7 metre marker left the occupant count at zero. Raising only the collider to a 2.4 metre volume registered the player, both with and without a Rigidbody. This rules out HUD visibility, player identification and the layer collision matrix as the cause in this reproduction. The previous test manually invoked OnTriggerEnter and therefore missed this defect.

ExtractionZone.ConfigureTrigger repairs legacy thin BoxColliders while keeping their horizontal footprint, bottom height, renderer and transform. The existing marker remains visually unchanged. The generator and repeatable updater both apply the repair and assign RaidRuntimeContext when missing; the saved RaidLoop_Test collider is now a trigger, size (1,15,1), centre (0,7,0), under its existing scale (8,0.16,7), producing a volume from ground level to 2.4m. Existing taller authored volumes are preserved. OnTriggerStay can recover occupancy if an enter notification was missed. Transition logging is optional via logTransitions and produces only entered/started/cancelled/completed messages, never frame spam.

WeaponDebugOverlay now derives CrosshairVisible from the equipped WeaponRuntimeState.IsAiming. Hip draws the reticle; ADS hides it immediately for both weapons/any optic. HitmarkerVisible retains its independent hit timer and option. No aiming, attachment, recoil or hit damage logic changed.

Focused code changes for these two defects: ExtractionZone.cs, WeaponDebugOverlay.cs, RaidLoopTestEnvironment.cs, RaidLoop_Test.unity, RaidLoopTests.cs, GunplayTests.cs and FPSControllerTests.cs. The prior pending Phase 5.1 files listed above, manual RaidLoop scene changes and additional RaidLoopNavigation 1/2 assets are retained for the requested checkpoint. Generated YAML/text trailing whitespace was removed without changing serialized values. No FPS_Test, Gunplay_Test, ProjectSettings, validated loot/wander/death implementation or Git history was reset or replaced.

Short manual retest (exit Play first, open RaidLoop_Test, run Outbreak > Actualizar Raid Loop a Fase 5.1 and save):

1. Enter green extraction, press nothing, remain five seconds: countdown 5 to 0, then RAID SUCCESS once.
2. Restart Play. Enter, leave halfway: cancellation. Reenter: a fresh five-second countdown. Death during countdown must still cancel and fail the raid.
3. Hip: crosshair visible. RMB ADS: crosshair hidden. Shoot an infected/target while aiming: hitmarker still appears. Repeat rifle/pistol, base sights/Red Dot as desired.

No phase-5 tag is created until the user manually validates these two fixes. No Phase 6 work.

Final code validation: all six runtime/editor/test assemblies compile without C# warnings/errors; 111/111 EditMode tests pass, plus 1/1 focused non-rendering PlayMode physics regression. Reports: Logs/Phase5FinalTests.xml and Logs/ExtractionPhysicsTests.xml. The diagnostic reproduction is recorded separately in Logs/ExtractionProbe.log. No screenshots, visual automation or browser were used. These fixes and the legitimate pending Phase 5 changes are included in the requested commit `fix: finalize raid extraction and ADS crosshair`.
