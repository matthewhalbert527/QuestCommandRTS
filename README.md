# Quest Command RTS

Unity RTS prototype with original content and classic command-and-control mechanics inspired by the RTS genre. Command & Conquer is only a mechanical reference point; this project does not use protected names, logos, factions, characters, story, maps, audio, or recognizable assets.

## Supported Unity Version

- Unity 2022.3.62f3.
- Desktop/editor play works in the Windows Standalone target.
- Quest VR uses Unity XR Management and OpenXR.

## Package Prerequisites

The project manifest pins:

- `com.unity.xr.management` 4.5.4
- `com.unity.xr.openxr` 1.15.1
- `com.unity.xr.interaction.toolkit` 3.2.2
- `com.unity.inputsystem` 1.18.0
- `com.unity.test-framework` 1.1.33

Android support is not required for the current desktop-focused prototype workflow.

## Runtime Modes

`RtsRuntimeModeResolver` chooses a mode at startup:

- `Desktop`: creates the existing command camera, mouse/keyboard input, Screen Space Overlay HUD, OnGUI minimap, and desktop build/production controls.
- `QuestVr`: activates a scaled tabletop XR rig, uses the tracked HMD camera, installs controller input, and shows Quest world-space status and command panels instead of the desktop HUD.

Quest mode is selected from an active XR loader/runtime. For testing, use the force-mode override in code, the `-questRtsMode QuestVr` command-line argument, or the `QUEST_RTS_FORCE_MODE=QuestVr` environment variable.

## Open And Run

1. Open this folder in Unity Hub as an existing project.
2. Use Unity 2022.3.62f3.
3. Open `Assets/Scenes/Battlefield.unity`.
4. Press Play.

The scene is intentionally empty. `RtsBootstrap` creates the map, units, buildings, resources, managers, and runtime-specific input/HUD objects.

## Desktop Controls

- Left click or drag: select.
- Shift + left click: add to selection.
- Right click: move, attack, harvest, or set a selected production building rally point.
- Middle click or Escape: cancel/clear.
- Arrow keys or screen-edge mouse: pan camera.
- Mouse wheel: zoom camera.
- A: attack-move selected units toward the cursor.
- S: stop selected units.
- Number keys 1-3: train rifleman, harvester, and tank.
- Q/W/E/R/T: place power plant, barracks, refinery, war factory, and turret.
- Z: repair the most damaged selected structure.
- X: sell selected structures for a partial refund.
- P: pause or resume the match.
- F5: save the manual slot.
- F9: load the manual slot.
- Ctrl + 5-9: assign control group; 5-9: recall control group.

## Quest Controls

- Right controller ray: hover terrain, units, buildings, and resources.
- X/left primary: open or close the command console.
- Right trigger: select one friendly entity, or activate a command-console control when the console captures the ray.
- Left trigger + right trigger: add the friendly entity to the current selection.
- A/right primary: issue the shared context command: attack enemy, harvest resource, set rally point, or move.
- Left trigger + A/right primary: attack-move selected units.
- A/right primary while placing: confirm the current valid structure placement.
- B/right secondary while placing: cancel placement.
- B/right secondary while not placing: clear selection.
- Left trigger + B/right secondary: stop selected units.
- Command console `System` tab: pause/resume, save, and load.

## Quest Command Console

The Quest command console has three tabs:

- `Build`: browse player structures, credit costs, power effects, prerequisites, affordability, and disabled reasons. Pick a structure to start controller-ray placement.
- `Produce`: select a production building, browse trainable units, queue units, see the active item/progress, inspect queued items, and cancel the last queued item for a full queued-cost refund.
- `Selected`: inspect selected health/counts, production/rally status, repair eligible player structures, sell selected player structures, stop selected units, and view the rally-point hint.
- `System`: pause/resume the simulation, save the manual slot, and load the manual slot when one exists.

Building placement follows the existing desktop build rules. The preview snaps to the map, turns green when valid, turns red when invalid, and reports concise invalid reasons such as outside map, outside build radius, blocked footprint, missing prerequisite, or insufficient credits.

The full battlefield remains approximately 224 simulation units wide. The Quest rig defaults to 126 simulation units per physical meter, so the board appears roughly 1.78 meters wide while gameplay coordinates and movement logic stay unchanged.

## Persistence And Lifecycle

- Runtime entities and resource nodes now receive stable save IDs.
- `RtsSaveService` captures and restores the generated skirmish state through a versioned JSON envelope with checksum validation.
- Saved state includes resources, health, orders, production queues, harvest state, fog exploration, active placement, and enemy commander economy/timers.
- `RtsLifecycleCoordinator` pauses simulation on app pause/focus loss, blocks commands while suspended or saving/loading, hides Quest pointer visuals when input focus is gone, and attempts autosave on pause/focus loss.
- Desktop HUD and Quest command console controls expose manual save/load and user pause.

## Skirmish AI

- The enemy commander now has its own credit bank, income ticks, paid unit production, and periodic attack orders.
- Enemy structures can be rebuilt from fixed base slots while at least one enemy structure remains alive.
- Timed enemy attacks still exist, but spawned attackers now spend commander credits instead of appearing for free.

See `docs/SAVE_SYSTEM.md`, `docs/LIFECYCLE_TEST_MATRIX.md`, and `docs/PERFORMANCE_TESTING.md`.

## Current VR Limitations

- No radial menus, lasso selection, hand tracking, passthrough, spatial anchors, locomotion, or board grabbing.
- Production queue cancellation applies to the last queued item, not the active item already in progress.
- The VR console uses simple placeholder styling and text-only controls.
- Quest Link and device behavior still require manual headset verification.
- Primitive placeholder art remains intentionally lightweight.

## Editor Tools

- `Command RTS > Open Battlefield Scene`
- `Command RTS > Export Sample Screenshot`
- `Command RTS > Build > Desktop Development Build`
- `Tools > Quest RTS > Apply Recommended Quest Settings`
- `Tools > Quest RTS > Validate XR Setup`

## Tests

Batch edit-mode test command:

```powershell
& "C:\Users\matth\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "E:\UnityProjects\QuestCommandRTS" -runTests -testPlatform EditMode -testResults "E:\UnityProjects\QuestCommandRTS\Logs\EditModeResults.xml" -logFile "E:\UnityProjects\QuestCommandRTS\Logs\UnityTestRun.log"
```

See `docs/QUEST_XR_SETUP.md` for Quest setup, manual OpenXR checks, and troubleshooting.
