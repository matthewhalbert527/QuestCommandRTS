# RTS Foundation Phase 1

## Current System Map

- `RtsBootstrap` creates `RtsGame` after scene load when no game instance exists.
- `RtsGame` owns match state, resources, selection, entities, camera setup, fog, command dispatcher, build manager, save/load, runtime mode, Quest rig, enemy director, and generated world content.
- `RtsCommandDispatcher` is the shared command facade used by Desktop and Quest input.
- `RtsInputController` converts desktop mouse/keyboard state into dispatcher calls for selection, context commands, attack-move, guard, stop, build placement, and cancellation.
- `QuestRtsInputController` converts XR/controller ray frames into the same dispatcher calls and uses dispatcher context hints for pointer feedback.
- `QuestCommandConsole` and `RtsCommandConsoleModel` drive the world-space command panel, category tabs, production commands, repair/sell, and minimap-side controls.
- `RtsEntity`, `RtsUnit`, `HarvesterUnit`, `EngineerUnit`, `MediumTankUnit`, `RtsStructure`, and `ProductionStructure` provide current selectable entities, orders, repair, harvesting, transport boarding, rally points, and production queues.
- `BuildManager` handles current build placement with map bounds, build radius, pending constructions, and physics overlap checks.
- `RtsFogOfWar` owns the existing visual fog texture/grid. It remains the renderer-facing fog system.
- `ResourceNode` owns resource quantity and resource visuals.
- `RtsSaveService` and persistence types capture entities, resources, fog, build placement, production, enemy director state, and match state.
- Existing EditMode coverage lives mainly in `RtsQuestEditModeTests` and `RtsPersistenceEditModeTests`.

## Reference Boundary

The copied RTS reference source has been moved out of Unity `Assets` to:

`E:\UnityProjects\ExternalReference\CnC_Red_Alert-main`

It is reference-only. It must not be imported, compiled, packaged, committed, copied, translated, or ported into this Unity project. See `docs/EXTERNAL_REFERENCE_POLICY.md`.

## Phase 1 Architecture

Phase 1 keeps gameplay behavior stable and adds two original foundations:

1. A normalized command-resolution layer underneath `RtsCommandDispatcher`.
2. A non-rendered logical RTS grid service for future placement, resource, visibility, occupancy, and AI rules.

Desktop and Quest still call the dispatcher. The dispatcher now builds an `RtsCommandRequest` from the current selection and hit target, then asks `RtsContextCommandResolver` for the command. Quest pointer colors still use dispatcher context hints, preserving existing hover behavior.

The resolver is intentionally independent from mouse, keyboard, XR devices, UI, cameras, and raycasting. Input systems provide normalized selection, target, team, modifier, lifecycle, and visibility data.

## Code Added Or Changed

- Added `Assets/Scripts/Runtime/RtsCommandResolution.cs`
  - `RtsCommandKind`
  - `RtsCommandTargetKind`
  - `RtsCommandTarget`
  - `RtsCommandRequest`
  - `RtsCommandResolution`
  - `RtsContextCommandResolver`
- Updated `Assets/Scripts/Runtime/RtsCommandDispatcher.cs`
  - Builds normalized command targets.
  - Calls the shared resolver for context commands.
  - Removes duplicated dispatcher-local context priority helpers.
- Added `Assets/Scripts/Runtime/RtsGridService.cs`
  - `RtsCellFlags`
  - `RtsGridCell`
  - `RtsBuildFootprint`
  - `RtsGridQueryResult`
  - `RtsGridService`
- Updated `Assets/Scripts/Runtime/RtsGame.cs`
  - Adds `LogicalGrid`.
  - Initializes a centered array-backed grid for the current battlefield.
  - Registers resource nodes into the grid.
  - Clears dynamic grid resource/visibility/reservation/occupancy state when the dynamic world resets.
- Added `Assets/Scripts/Editor/RtsFoundationPhase1EditModeTests.cs`
  - Source-boundary test.
  - Command resolver tests.
  - Logical grid tests.

## Shared Command Semantics

The resolver covers:

- selected combat unit plus enemy target -> `Attack`
- selected mobile unit plus terrain -> `Move`
- selected mobile unit plus attack-move modifier plus terrain -> `AttackMove`
- selected harvester plus resource target -> `Harvest`
- selected engineer plus damaged friendly target -> `Repair`
- selected production structure plus terrain -> `SetRallyPoint`
- selected mobile unit plus stop request -> `Stop`
- no selection plus friendly selectable target -> `Select` or `AddToSelection` when selection is routed through the resolver
- fog-hidden entity/resource target -> invalid
- outside-map target -> invalid
- invalid state -> invalid with a concise reason

For current game compatibility, the dispatcher opts into broad context hints so Quest hover colors and existing right-click feedback still behave as before when no capable unit is selected. Direct resolver tests keep the stricter capability checks.

## Logical Grid

`RtsGridService` is non-rendered and array-backed. It does not create per-cell GameObjects or renderers.

Grid responsibilities now available:

- `WorldToCell`
- `CellToWorldCenter`
- `IsInsideMap`
- `IsWorldInsideMap`
- `TryGetCell`
- buildable, blocked, resource, explored, visible, reserved flags
- occupancy count
- resource node/amount marker
- `CanPlaceFootprint`
- `ReserveCells`
- `ReleaseReservation`
- `ClearDynamicState`

The existing visual fog renderer and build placement physics checks remain in place for Phase 1. The new grid is the shared rules foundation for Phase 2 integration.

## Tests

Added EditMode tests validate:

- no legacy native/reference source files remain under `Assets`
- attack, move, attack-move, stop command resolution
- harvest, repair, rally, selection, invalid no-selection, and fog-hidden command states
- world/cell conversion
- valid footprint placement
- outside, blocked, reserved, and occupied footprint failures
- resource marker and visible/explored flags

## Validation Notes

Static checks run during Phase 1:

```powershell
Get-ChildItem -Path .\Assets -Recurse -File -Include *.cpp,*.c,*.h,*.hpp,*.asm,*.mak,*.pas,*.rc
Select-String -Path .\Assets\**\*.cs -Pattern 'CnC','Red Alert','Command & Conquer','Soviet','Allied','Tesla','Tanya','Mammoth','Chrono' -SimpleMatch
```

Both should return no matches.

Actual Phase 1 validation on June 27, 2026:

- Legacy source extension check under `Assets`: passed, no files found.
- Protected/reference identifier check in `Assets/**/*.cs`: passed, no matches found.
- Unity script compilation in the already-open editor: passed. `Editor.log` reported `Tundra build success` for `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.
- Existing warning still present: `BuildManager.DestroyObject(Object)` hides `Object.DestroyObject(Object)`. This warning predates the Phase 1 foundation and was not changed.
- `dotnet build`: not run because `dotnet` was not available on PATH.
- Visual Studio `MSBuild.exe`: not usable because its Roslyn target file `Microsoft.CSharp.Core.targets` is missing from the local Visual Studio install.
- Unity batchmode EditMode tests: not run because Unity reported another instance already had `E:\UnityProjects\QuestCommandRTS` open. The existing `Logs/EditModeResults.xml` is stale from June 26, 2026 and should not be used as Phase 1 evidence.

After closing the open Unity editor, run EditMode tests with the project Unity version:

```powershell
& "E:\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "E:\UnityProjects\QuestCommandRTS" -runTests -testPlatform EditMode -testResults "E:\UnityProjects\QuestCommandRTS\Logs\EditModeResults.xml" -logFile "E:\UnityProjects\QuestCommandRTS\Logs\UnityTestRun.log"
```

## Future Simulation Order

Recommended Phase 2/3 simulation order:

1. lifecycle gates, pause, save/load blocking
2. triggers
3. AI/team orders
4. command queues
5. unit orders
6. movement
7. combat, damage, death
8. resource and economy
9. production
10. fog, visibility, minimap, tactical map
11. diagnostics

## Phase 2 Handoff

Good next targets:

- Route build placement footprint checks through `RtsGridService` while preserving existing physics overlap protection.
- Feed fog explored/visible state into `RtsGridService` without replacing the current fog renderer.
- Register static structure occupancy/reservation in the grid.
- Add stable cell-level resource and expansion planning APIs for AI.
- Add command queue objects after command resolution, instead of issuing all orders immediately from dispatcher methods.
