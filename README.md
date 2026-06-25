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

Android modules are not needed for desktop/editor play, but they are required for Quest device development builds. `Tools > Quest RTS > Validate XR Setup` reports whether this Unity install currently has Android Build Support available.

## Runtime Modes

`RtsRuntimeModeResolver` chooses a mode at startup:

- `Desktop`: creates the existing command camera, mouse/keyboard input, Screen Space Overlay HUD, OnGUI minimap, and desktop build/production controls.
- `QuestVr`: activates a scaled tabletop XR rig, uses the tracked HMD camera, installs controller input, and shows Quest world-space status, tactical map, and command panels instead of the desktop HUD.

Quest mode is selected from active XR device state, or from an initialized Android OpenXR loader for Quest builds. Desktop/editor runs without an active XR device remain in `Desktop` by default. For testing, use the force-mode override in code, the `-questRtsMode QuestVr` command-line argument, or the `QUEST_RTS_FORCE_MODE=QuestVr` environment variable.

## Open And Run

1. Open this folder in Unity Hub as an existing project.
2. Use Unity 2022.3.62f3.
3. Open `Assets/Scenes/Battlefield.unity`.
4. Press Play.

The scene is intentionally empty. `RtsBootstrap` creates the map, units, buildings, resources, managers, and runtime-specific input/HUD objects. Desktop/player builds open on a main menu over the generated skirmish; start or load from there.

## Desktop Controls

- Left click or drag: select.
- Shift + left click: add to selection.
- Right click: move, attack, harvest, or set a selected production building rally point.
- Middle click: cancel/clear.
- Escape or P: pause/resume and show the pause menu.
- Arrow keys or screen-edge mouse: pan camera.
- Mouse wheel: zoom camera.
- A: attack-move selected units toward the cursor.
- S: stop selected units.
- Number keys 1-5: train gunner, harvester, light tank, medium tank, and heavy tank.
- Q/W/E/R/T: place power plant, barracks, refinery, war factory, and turret.
- Z: repair the most damaged selected structure.
- X: sell selected structures for a partial refund.
- F5: save the manual slot.
- F9: load the manual slot.
- Main/Pause menu: start, resume, restart skirmish, save, load, or quit.
- New Match HUD button: reset the current skirmish without reloading the scene.
- Ctrl + 6-0: assign control group; 6-0: recall control group.

## Quest Controls

- Right controller ray: hover terrain, units, buildings, and resources.
- X/left primary: open or close the command console.
- Right trigger: select one friendly entity, or activate a command-console control when the console captures the ray.
- Left trigger + right trigger: add the friendly entity to the current selection, or area-select nearby friendly units when aimed at terrain.
- A/right primary: issue the shared context command: attack enemy, harvest resource, set rally point, or move.
- Left trigger + A/right primary: attack-move selected units.
- A/right primary while placing: confirm the current valid structure placement.
- B/right secondary while placing: cancel placement.
- B/right secondary while not placing: clear selection.
- Left trigger + B/right secondary: stop selected units.
- Command console `System` tab: pause/resume, save, load, and start a new match.

## Quest Command Console

The Quest command console has four tabs:

- `Build`: browse player structures, credit costs, power effects, prerequisites, affordability, and disabled reasons. Pick a structure to start controller-ray placement.
- `Produce`: select a production building, browse infantry, harvester, and tank options, queue units, see the active item/progress, inspect queued items, and cancel queued or active production for a full unit-cost refund.
- `Selected`: inspect selected health/counts, production/rally status, repair eligible player structures, sell selected player structures, stop selected units, and view the rally-point hint.
- `System`: pause/resume the simulation, save the manual slot, load the manual slot when one exists, and start a new match.

Building placement follows the existing desktop build rules. The preview snaps to the map, turns green when valid, turns red when invalid, and reports concise invalid reasons such as outside map, outside build radius, blocked footprint, missing prerequisite, or insufficient credits.

The Quest tactical map is a non-interactive world-space battle-view panel. It mirrors the desktop minimap data with pooled pips for resources and visible friendly/enemy forces, keeps fogged enemies hidden, and has pointer raycasting disabled so it does not block battlefield or console commands.

The full battlefield remains approximately 224 simulation units wide. The Quest rig defaults to 126 simulation units per physical meter, so the board appears roughly 1.78 meters wide while gameplay coordinates and movement logic stay unchanged. `Command RTS > Profile > Use Room-Sized Quest Tabletop Scale` writes a local profile preset that expands the board to roughly 4.0 meters wide and increases pointer reach; `Use Default Quest Tabletop Scale` restores the default. `QuestTabletopSettings.BoardHeightMeters` defaults to 0.82m, which shifts the generated battlefield to tabletop height above the physical tracking origin instead of leaving the board on the floor.

The generated battlefield uses original primitive placeholder art with sandy terrain accents, projected water channels, ridges, rocks, scorches, rails, and corner pylons. Imported Bastion infantry, vehicle, structure, fabrication hub, and defense meshes now replace several gameplay placeholders, with added team-color roof/side plates and textured armor/detail panels for skirmish readability. The set-dressing pieces remain visual-only and have their colliders removed so pointer raycasts and build placement still use the underlying battlefield.

## Persistence And Lifecycle

- Runtime entities and resource nodes now receive stable save IDs.
- `RtsSaveService` captures and restores the generated skirmish state through a versioned JSON envelope with checksum validation.
- Save files are written through a temporary file plus `.bak` backup, and load falls back to the previous valid backup when the newest primary save is corrupt or missing.
- Save metadata is validated before display and includes slot, app version, skirmish config, map, match time, match state, entity counts, resource-node counts, and backup/primary source.
- Saved state includes resources, health, orders, production queues, harvest state, fog exploration, active placement, and enemy commander economy/timers.
- `RtsLifecycleCoordinator` pauses simulation on app pause/focus loss, blocks commands while suspended or saving/loading, hides Quest pointer visuals when input focus is gone, and attempts autosave on pause/focus loss plus configurable periodic autosaves during active play.
- `RtsGame.TryRestartMatch` resets dirty or restored skirmish state, clears match-ended/user pause reasons, rebuilds the generated match, resets fog and enemy AI, and keeps the runtime mode active.
- `RtsProfileSettings` stores versioned player preferences separately from match saves and safely clamps Quest tabletop scale, tabletop height, pointer length, UI scale, volume, quality preset, and periodic autosave interval values before use.
- Desktop HUD and Quest command console controls expose manual save/load and user pause.
- Selected or damaged entities show lightweight world-space health bars; fogged enemy health bars remain hidden.

`RtsRuntimeDiagnosticsSnapshot` can export a JSON snapshot of a generated match with entity, team, unit, structure, production, resource, fog, save-slot, and tabletop scale counts. Use it before and after large-board or content changes to catch accidental world-generation drift.

`RtsSceneBudgetSnapshot` can export the generated Quest scene footprint with object, renderer, material, collider, light, camera, world-space UI, tactical map, and fog-overlay counts. Fog of war uses one texture-backed overlay instead of thousands of per-cell renderers so the tabletop scene stays inside the local Quest budget gates before hardware profiling.

`Command RTS > Export Soak Diagnostics Snapshot` creates a populated desktop baseline with additional units, active production queues, mixed attack-move orders, and an active placement preview, then exports the same diagnostics shape for profiler/device comparisons.

## Skirmish AI

- The enemy commander now has its own credit bank, income ticks, paid unit production, and periodic attack orders.
- Enemy structures can be rebuilt from fixed base slots while at least one enemy structure remains alive, including upgraded defensive towers.
- Timed enemy attacks still exist, but spawned attackers now spend commander credits instead of appearing for free and can include mixed gunner, grenadier, rocket, and flame infantry.
- Player and enemy units/buildings use multiple colored recognition plates so teams are readable from the skirmish camera and Quest tabletop view.
- Armed combat units automatically acquire nearby enemies while idle and retaliate against enemies that damage them.

See `docs/SAVE_SYSTEM.md`, `docs/LIFECYCLE_TEST_MATRIX.md`, `docs/PERFORMANCE_TESTING.md`, and `docs/MILESTONE_STATUS.md`.

## Current VR Limitations

- No radial menus, box/lasso selection volume, hand tracking, passthrough, spatial anchors, locomotion, or board grabbing.
- The VR console and tactical map are still placeholder UI, but now use layered holographic-style world panels with color-coded row icon tiles, pooled map pips, and text labels.
- Quest Link and device behavior still require manual headset verification.
- Infantry currently includes gunner, grenadier, rocket soldier, flame trooper, and engineer roles using imported low-poly assets. Grenadiers/flamers apply splash, rockets hit armored targets harder, and engineers can repair damaged friendly units or structures through context commands.
- Base structures now use imported low-poly meshes for the fabrication hub command center, power plant, barracks, refinery, war factory, harvester, turret, gun tower, and advanced gun tower, with added textured detail panels to reduce the flat single-color read. Gun Tower and Advanced Gun Tower are buildable defensive structures in the Quest Build tab.

## Editor Tools

- `Command RTS > Open Battlefield Scene`
- `Command RTS > Export Sample Screenshot`
- `Command RTS > Export Quest Sample Screenshot`
- `Command RTS > Export Room-Sized Quest Sample Screenshot`
- `Command RTS > Export Runtime Diagnostics Snapshot`
- `Command RTS > Export Quest Scene Budget Snapshot`
- `Command RTS > Export Soak Diagnostics Snapshot`
- `Command RTS > Validate Generated Desktop Runtime`
- `Command RTS > Build > Validate Desktop Build Support`
- `Command RTS > Build > Desktop Development Build`
- `Command RTS > Profile > Use Default Quest Tabletop Scale`
- `Command RTS > Profile > Use Room-Sized Quest Tabletop Scale`
- `Tools > Quest RTS > Apply Recommended Quest Settings`
- `Tools > Quest RTS > Validate XR Setup`
- `Tools > Quest RTS > Validate Generated Quest Runtime`
- `Tools > Quest RTS > Validate Quest Scene Budget`

The desktop build command targets `StandaloneWindows64` and requires Windows Build Support for Unity 2022.3.62f3. If the command reports that `StandaloneWindows64` is not supported or that the `WindowsPlayer.exe` template is missing, repair the editor install in Unity Hub and add the Windows build module before treating desktop player builds as verified.

## Tests

Batch edit-mode test command:

```powershell
& "C:\Users\matth\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "E:\UnityProjects\QuestCommandRTS" -runTests -testPlatform EditMode -testResults "E:\UnityProjects\QuestCommandRTS\Logs\EditModeResults.xml" -logFile "E:\UnityProjects\QuestCommandRTS\Logs\UnityTestRun.log"
```

See `docs/QUEST_XR_SETUP.md` for Quest setup, manual OpenXR checks, and troubleshooting.
