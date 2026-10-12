# Phase 9B presentation

## Scope

- Raid footprint: 46 × 76 m → 54 × 120 m (1.85×). Southern safe spawn retained; checkpoint moved north to z=91.
- Four ruined buildings, parking/road barriers, northern service supplies, three additional loot caches and western/eastern bypasses. Existing extraction trigger/countdown unchanged.
- 18 infected: 13 normal + 5 runners. Existing spawner, AI and damage contracts; no waves or respawn changes.
- Authored schematic minimap: north up, player heading and green extraction marker. M toggles it. No enemies, extra camera, RenderTexture or interaction. Same schematic appears in Deploy.
- Inventory/stash left, loadout right, rarity-bordered cards, item details, attachment selection highlights, assignment cards and map preview. Existing buy/sell/equip/configure operations retained.
- AKM visual scale .82 → .92 (~12% larger); hip depth .52 → .49. Socket scale follows the mesh. FOV remains 79; recoil, sway and ADS logic unchanged.
- Grey overcast background, softer directional lighting and moderate linear fog (45–145 m).

## Assignments

One active assignment at a time, saved through the existing profile quest list. Extraction confirms progress; Commander grants money/XP once. Death or an interrupted raid resets unextracted progress. Discarding collect items reduces progress. Kills are credited by the existing health death event only when the source is the player's firearm.

| Assignment | Objective in one raid, then extract | Reward |
|---|---|---|
| Recuperar suministros | Original 2 medical supplies, retained for existing profiles | $500 / 100 XP |
| Limpieza | 10 infected, either variant | $800 / 200 XP |
| Salvamento | 12 scrap | $700 / 200 XP |
| Control de corredores | 4 runners | $1100 / 300 XP |
| Recuperación crítica | 8 medical supplies | $1000 / 250 XP |

The northern buildings contain authored supplies (12 scrap and 8 medical supplies total) in addition to ordinary random container loot. Mixed-objective examples were simplified to existing single-objective quests, avoiding a quest/save format redesign.

## Explicit fallbacks

- **No suitable FPS arms found.** Imported humanoids use full-body skinned meshes, without a clean standalone FPS arms rig. Arms are deferred rather than masking full characters or introducing risky viewmodel animation changes.
- Item previews use the requested fallback: clean cards with name, category, rarity, quantity/value and selected-item description. No additional 3D preview rendering system.
- The minimap is a schematic, not an exact collision/survey map; small props are intentionally omitted.
- Appearance, readability, ADS framing and performance on the presentation PC require manual validation. Headless checks do not establish visual quality.

## Files

- `PresentationExpansion.cs`: expanded layout, population, supplies, authored assignment assets and overcast settings.
- `PresentationVisualPass.cs`: expansion integration, weapon presentation, mission credit prefab hookup and route verification. NavMeshData remains binary and is generated through Unity APIs.
- `RaidMinimap.cs`: lightweight schematic shared with Deploy.
- `TacticalUI.cs`, `HubDebugUI.cs`, `RaidLoopDebugUI.cs`: cards, columns, selection and assignment UI.
- `GameFlow.cs`, `HubSceneFlow.cs`, `InfectedMissionCredit.cs`: minimal assignment selection/initialization and kill event integration.
- `Resources/Assignments/*`: four additional definitions; existing RecoverSupplies asset unchanged.
- RaidLoop_Test, presentation infected/runner prefabs and regenerated navigation data: authored scene output.
- `ExpandedAssignmentTests.cs`: four focused tests; existing Hub flow test now expects 18 actors.

## Manual presentation check

1. Open MainMenu, Continue/New Game → Hub. F10 remains off by default.
2. TAB/Stash: inspect item cards, rarity/value, equip/unequip; inspect primary, secondary, armor, backpack, rig and equipment slots.
3. Quartermaster: select details, buy and sell; funds and storage update. Workbench: primary/secondary, four slot cards, equipped highlight; configure suppressor/optic.
4. Commander: browse all five assignments. Accept one; accepting another is disabled until completion/turn-in. Inspect objective and reward.
5. Deploy: schematic and active assignment; start raid. Check rifle hip/ADS/optic/suppressor and pistol switching.
6. M hides/shows minimap. Player arrow rotates with heading; green EXIT remains at the north checkpoint.
7. Explore both original and northern ruins, west/east bypasses and service yards. Check infected wandering/chasing, runners, collisions and loot access.
8. For salvage/medical assignments, collect supplies from the two northern buildings; discard one and observe count decrease. For kill assignments, kill the required targets (normal kills do not advance the runner assignment).
9. Reach checkpoint and hold position for the existing countdown. Return to Hub; Commander turn-in grants the shown reward once. Reopen/reload and verify it cannot be claimed again; select a different assignment.
10. On another raid, die: bodycam death → return to Hub. Unextracted mission progress resets and deployed losses retain existing behavior.

## Git preservation

Validation completed in an isolated project copy using Unity 2023.2.20f1: runtime/editor/test assemblies compiled; 129 EditMode tests passed (125 existing + 4 focused additions), and the Hub→Raid→extraction/death→Hub PlayMode integration test passed with 18 spawned actors. Editor route checks passed for spawns, loot, supply pickups, bypass waypoints and extraction. Windows x86_64 build reported `Build Finished, Result: Success` and produced `Builds/Phase9BValidation/Outbreak Extraction.exe` plus `Outbreak Extraction_Data`. The incremental Bee build first retried an outdated script graph, then regenerated it and compiled successfully; no source workaround was needed.

The staged Phase 9B diff passes whitespace checks. Full working-tree checks still report existing whitespace in excluded local material/MainMenu edits.

Navigation repair was committed/pushed separately as `4838772` before Phase 9B.
Pre-existing local edits to imported building/zombie materials, checkpoint/warm lamp materials, MainMenu.unity and the three URP quality assets are preserved and excluded from the Phase 9B commit. They were not reset, discarded or silently included.
Never apply text normalization to arbitrary `.asset` files: the navigation asset is binary.
