# Phase 9C — Complete game UI / UX loop

## Implemented

Presentation menus now use Unity UI Canvas rather than IMGUI lists. The reusable vocabulary includes headers, tabs/action buttons, rarity-accented item slots, weapon/equipment slots, selected/disabled/completed states, stat rows, progress bars, scrollbars and detail panels. CanvasScaler uses a 1920×1080 reference and Expand scaling; canvases are scene roots, independent of FPS transforms. Hover/pressed/selected color transitions use Unity UI's 0.14 s fade. No tween package.

- **TAB / Stash / Loadout:** three-column inventory cards, loadout alongside, selected-item details and contextual equip actions; storage usage bar. All nine existing loadout slots remain accessible, including four equipment slots.
- **Quartermaster:** Buy/Sell tabs, four-column products/stash, selected-item preview, prices, buy/sell actions and explicit funds/capacity feedback.
- **Workbench:** primary/secondary tabs, one weapon preview, four attachment cards, equipped accents and options for the selected slot. Attachment configuration still delegates to HubSceneFlow.
- **Commander:** all five mission cards, progress, state, differentiated rewards and a lateral detail panel. Existing one-active rule, accept and turn-in APIs unchanged.
- **Deploy:** schematic map, active assignment, three quick equipment checks, threat status and Deploy action. Map is presentation-only.
- **Raid:** card inventory/loadout, container cards with Take/Take All, compact assignment HUD, existing minimap, compact interaction prompt and extraction progress/countdown/cancellation.
- **Results:** consistent success/failure panels, time, value/extracted stacks or lost loot/equipment slots, stash counter and Return To Hub. Loss counters are UI snapshots of the existing session, not inventory mutations.
- **MainMenu:** left title/action composition, a small independent ruin/car/fence scene with cold lighting and fog; readability gradient. Continue retains valid-save gating. New Game retains SaveService.TryDelete and confirmation. Settings retain existing volume/sensitivity/fullscreen preferences and now use visual sliders/buttons. Escape returns from submenus.

## Preview and icon audit

The imported Apocalypse_Free textures are material atlases, signs, fabric, dashboard/map and sky textures; no ready-to-use inventory sprite library was found. Cards use restrained procedural category silhouettes (weapon, medicine, ammo, equipment) rather than inappropriate atlas images or external assets.

`ItemPreviews.asset` is a presentation lookup keyed by the existing ItemDefinition references. It includes the imported AKM, Pistol1 and First_Aid model, plus the five existing debug attachment visual prefabs. Other items retain the category fallback. It creates no parallel item identity or inventory.

One `ItemPreviewRenderer` per active Hub/Raid UI reuses one 512×384 RenderTexture and one camera. At selection it replaces one isolated visual copy, removes scripts/colliders/rigidbodies before activation, disables animator/audio/particles/imported cameras/lights, puts it on layer 31 and normalizes only that copy's bounds. A distant point light cannot replace the world's URP main directional light. Camera/output aspect matching avoids stretching. Rendering pauses when the detail closes; objects/texture are cleaned on scene destruction. Headless runs skip GPU preview rendering and retain fallback/UI construction.

Workbench preview shows the selected base weapon; the slot cards communicate equipped attachments. No interactive Gunsmith or attachment animation was added.

## Preserved behavior and local changes

No changes to RaidLoop_Test, Hub_Test, NavMeshData, map geometry, population, damage/HP, AI, weapon feel, quest rules, economy, loot, extraction logic or save format. Menus continue using PlayerInput's existing suppression ownership; opening frees the cursor and blocks movement/look/fire/lean, closing releases the owner and preserves the existing mouse-release guard.

The only change in PlayerDeathSequence is in OnGUI: once its sequence is complete and the new result Canvas is visible, it yields rendering so the legacy black IMGUI overlay does not obscure the result buttons. Death timing, camera motion, events and FailRaid are untouched.

The existing local MainMenu edit only changed serialized object IDs/whitespace; its controller/catalog/save filename configuration was retained while adding the menu environment, and is included/documented in this UI commit. Prior imported building/zombie material changes, lamp material changes and three URP prefilter/settings edits remain preserved locally and excluded. No reset/clean/revert.

## Files

- `GameUI.cs`: shared Canvas, typography, buttons, cards, progress/stats and scrolling.
- `InventoryPresentation.cs`: grid/loadout/detail/map composition and selected-stack resolution.
- `ItemPreviewLibrary.cs`, `ItemPreviewRenderer.cs`, `Resources/ItemPreviews.asset`: isolated reusable preview.
- `HubDebugUI.cs`, `RaidLoopDebugUI.cs`: presentation adapters using unchanged domain operations; IMGUI retained only for F10 diagnostics.
- `MainMenuController.cs`: Canvas presentation around the existing save/settings actions.
- `PlayerDeathSequence.cs`: terminal fade drawing handoff only.
- `PresentationUISetup.cs`, `MainMenu.unity`: lightweight menu background and preview lookup authoring. Editor menu: Outbreak → Actualizar presentación UI Fase 9C. This tooling does not open or regenerate Hub/Raid.
- `HubSceneFlowTests.cs`: existing lifecycle/input regression extended to each station and raid inventory; one focused MainMenu settings/confirmation construction test.

## Validation

Unity 2023.2.20f1 / URP 16.0.6 compiled runtime/editor/test assemblies. EditMode: 129 passed. PlayMode UI/Hub integration: 2 passed, including modal ownership release and success/death return using the same saved profile. No new compiler warnings were reported in the successful validation logs.

Windows x86_64 build: `Builds/Phase9CValidation/Outbreak Extraction.exe` and `Outbreak Extraction_Data`. Build settings remain MainMenu, Hub_Test, RaidLoop_Test. Build/cache/log output stays ignored. Automated checks are structural/lifecycle checks; visual quality and actual GPU previews require manual review.

## Manual verification (1080p, then 720p/windowed)

1. Start the build. Check the independent 3D menu background, gradient/title and hover/disabled Continue. Continue loads existing profile; New Game opens confirmation, Cancel preserves progress, Start uses the existing reset flow and loads Hub.
2. Settings: drag/click volume and sensitivity, check values, toggle fullscreen, Back/ESC. Reopen to verify preferences persist.
3. Hub: F10 off by default, only nearby interaction prompt visible. TAB opens grid/loadout; mouse is free and look/fire/lean are blocked. ESC/TAB/X closes and gameplay resumes; clicking Close does not fire immediately.
4. Stash: select AKM, pistol, supplies and an attachment. Check slow 3D rotation, correct proportions, rarity/quantity/value and category fallback for other items. Scroll inventory and equipment slots; equip/remove via contextual actions.
5. Quartermaster: Buy/Sell tabs, select details, purchase/sell; verify feedback for success, insufficient funds and full stash. Selecting one of two same-item stacks should highlight only that card.
6. Workbench: switch primary/secondary; click Optic/Muzzle/Magazine/Grip; inspect options, equip/remove and highlight. Reopen to confirm existing presets persist.
7. Commander: all five cards, progress/state/rewards. Inspect details, accept one, check other accepts disabled. Turn in a completed raid assignment once using the existing flow.
8. Deploy: map, active mission and loadout visible without paragraphs. Deploy, check unchanged gunplay/AI and M minimap toggle. TAB/container screens share the visual language; Take/Take All/Discard retain existing behavior.
9. Extract: progress bar/countdown updates, leaving cancels briefly. Success shows raid time, value, loot cards and stash; Return/H goes to Hub.
10. Die: existing bodycam sequence/fade finishes, then failure screen remains readable/clickable with loss counters. Return/H goes to Hub; controls are not stuck. Toggle F10 to confirm diagnostic tools still work.

No visual automation, browser, downloads or new gameplay systems were used.
