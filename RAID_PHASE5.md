# Phase 5 — Raid Interaction, Loot and Extraction

## Runtime boundaries

- `PlayerInteractor` owns the single camera raycast, interaction prompt, panel focus and hold progress.
- `LootContainer` delegates generation to the existing `LootTable` and current `RaidMapDefinition`. Its `LootContainerInventory` rolls once and keeps remaining contents in memory for the raid.
- `WorldItem` adds to `RaidSession.FoundLoot` before disabling its GameObject.
- `ExtractionZone` only requests `GameFlow.ExtractSuccessfully()`. Transfer, persistence and the exactly-once guard remain owned by `GameFlow`.
- `RaidLoopDebugUI` is temporary IMGUI used only by the generated test scene. Discard removes loot and intentionally does not spawn a world drop.

## Generate the test raid

1. Exit Play Mode.
2. Run **Outbreak > Crear entorno de prueba Raid Loop**.
3. Open the generated `Assets/Game/Scenes/RaidLoop_Test.unity` if Unity does not open it automatically.
4. Enter Play Mode.

The generator creates the debug catalog and rarity items, five loot tables, `RaidLoopMap`, a baked NavMesh, five containers, three world pickups, four normal infected, two runners and one extraction zone. The debug profile uses `raidloop_profile.json` so normal profile data is not mixed with this test.

## Controls and validation

- `E`: search/pick up. Hold inside the green extraction zone for four seconds.
- `Tab`: open/close the raid inventory. `Discard` removes a loot stack; no world object is created in this debug implementation.
- `Escape`: close the active debug panel.
- `1` / `2`, mouse buttons, `R`, `B`, `F1`–`F4`: existing gunplay and attachment controls.
- `H`: return to the logical Hub state after success/failure.

Validate that reopening a container does not reroll it; a full backpack leaves container loot and pickups in place; leaving extraction or releasing `E` cancels progress; success moves loot to the stash once and persists it; death still runs the bodycam sequence and then loses raid loot.
