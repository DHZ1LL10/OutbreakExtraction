# Phase 7 — Recuperar suministros

One existing QuestDefinition asset: Assets/Game/Resources/RecoverSupplies.asset. Collect 2 of the existing debug_medical_supplies during a single raid, extract alive, then turn in to Commander. Reward: $500 and 100 XP, once. No second quest system, additional currency, narrative, visual redesign or later phase.

## Manual validation

A. Open your existing Hub_Test and enter Play. At MISSIONS / COMMANDER accept Recuperar suministros. Check Active / 0 of 2.
B. Prepare equipment and Deploy. Search the Medical Bag in the west loot building, approximately X=-9, Z=10. Its existing deterministic loot seed provides enough supplies. Take at least two; HUD reaches 2/2 but asks for extraction. Stay in the green extraction volume for five seconds, then Return To Hub.
C. Commander shows MISSION COMPLETE and TURN IN. Click it: money increases by exactly $500 and XP by 100, state Completed.
D. Reopen Commander/restart Play: Assignment complete. No second claim or added reward.
E. Test death while the mission is still Active, before completing C, or in another profile: collect two, die and return. Mission stays Active / 0 of 2; no reward and no Turn In. Retry a new raid. Do not delete/reset the real profile to test this.

The current Hub scene works without regeneration: HubSceneFlow loads the mission from Resources when the new optional serialized reference is absent. Outbreak > Actualizar misión Recuperar suministros can explicitly connect the asset to the current Hub_Test without replacing geometry/configuration. Creating a new Hub through the existing generator also connects it. User modifications to Hub_Test present at the start of this task are left untouched and outside this commit.

## Ownership, events and persistence

QuestState values 0–4 are unchanged for old saves. ReadyToTurnIn is appended as 5. QuestProgress retains the previous Advance/Claim behavior for definitions that do not require extraction. This mission remains Active even at count 2; ConfirmExtraction moves it to ReadyToTurnIn. TurnIn changes only ReadyToTurnIn to Completed.

GameFlow owns the single enabled mission definition and existing Profile.Quests progress. EnableMission adds/persists Available once, including old profiles without the mission. Acceptance and turn-in use CommitHubChange and the existing SaveService. Reward uses Profile.Wallet.Credit and AddXP; state/money/XP roll back together if reward overflow or saving fails. Repeated turn-in fails without reward.

RaidSession.LootChanged fires only after a successful AddLoot/RemoveLoot. GameFlow subscribes when starting a raid and computes progress from matching items currently in FoundLoot. No per-frame progress polling. Stash items, trader purchases, deployed medical equipment, failed transfers and discarded loot do not falsely satisfy the objective. Counts clamp at two. Only items retained for the successful extraction qualify. No turn-in item consumption was specified; the extracted medical items remain in Stash even after the reward.

Raid progress is provisional. Starting a raid resets Active progress and writes zero in the existing deployment checkpoint. Taking items updates in-memory progress without a save every frame; closing Play mid-raid reloads Active/zero. Failed raids reset the attempt to zero. Successful extraction computes ReadyToTurnIn on the same candidate profile as the stash transfer, then persists both atomically. A full stash/save failure leaves the mission Active and the raid retryable. Partial successful extraction leaves Active progress persisted, but a new attempt starts at zero because the objective requires two in one raid. Completed/ReadyToTurnIn missions are not reset by later raids.

## Files

- Runtime/Quests/QuestDefinition.cs: explicit enum values, extraction requirement and reward data.
- Runtime/Quests/QuestProgress.cs: extraction confirmation and turn-in; legacy lifecycle preserved.
- Runtime/Core/GameFlow.cs: mission binding/acceptance/turn-in, loot event integration, atomic extraction confirmation and death reset.
- Runtime/Raids/RaidSession.cs: successful loot transfer/removal event.
- Runtime/Hub/HubSceneFlow.cs and HubDebugUI.cs: automatic mission binding and Commander states/actions.
- Runtime/Raids/RaidLoopDebugUI.cs: minimal active-mission HUD, hidden after terminal raid state.
- Editor/FPS/HubTestEnvironment.cs: connects the mission in newly generated Hub scenes.
- New Editor/FPS/RecoverSuppliesMissionContent.cs (+ meta): idempotent asset authoring and non-destructive Commander updater.
- Resources/RecoverSupplies.asset (+ asset/folder metas): one mission, targeting the existing Medical Supplies ItemDefinition ID.
- DebugContent/RaidLoop/MedicalLoot.asset and Editor/FPS/RaidLoopTestEnvironment.cs: medical table quantity raised to 2–3 for its medical roll. Other entries/weights/drop chance, tables and scenes remain unchanged. This does not guarantee supplies in every container.
- New Tests/Editor/RecoverSuppliesQuestTests.cs (+ meta): six focused regressions.
- Tests/PlayMode/HubSceneFlowTests.cs: existing non-rendering scene lifecycle test extended through accept/extract/turn-in and completed-state persistence.
- QUEST_PHASE7.md: instructions and validation record.

Existing SaveMapper/SaveData already persist QuestProgress, including the appended valid enum state; no parallel save schema was added. Player controller, gunplay, attachments, inventory transfer implementations, AI, death sequence and scene geometry are unchanged. No Git tags are created or modified.

## Code validation

All six runtime/editor/test assemblies compile with Unity 2023.2.20f1 Roslyn without C# warnings/errors. EditMode: 125/125 passed (six new quest tests). Non-rendering scene integration: 1/1 passed, including acceptance, successful extraction, saved turn-in, duplicate reward rejection and Completed persistence across the next failed raid. Reports: Logs/QuestTests.xml and Logs/QuestSceneTests.xml, ignored by Git. Unity resolved licensing and exited 0. No visual automation or browser was used; manual validation remains pending.

The full git diff --check reports Unity-generated trailing whitespace in the pre-existing user changes to Hub_Test.unity. Those changes were neither edited nor staged for this task. The Phase 7 commit's staged diff is checked separately and passes. The working tree intentionally retains that previously modified scene.
