# Quest XR Foundation Milestone Status

Last updated: 2026-06-25

This document records what has been implemented and what has actually been verified for the Quest XR Foundation and Core Command Vertical Slice. It is deliberately conservative: simulated/editor coverage is marked separately from physical Quest or Quest Link verification.

## Implemented Experience

- Desktop mode keeps the generated battlefield, command camera, mouse and keyboard RTS controls, desktop HUD, build hotkeys, production, control groups, save/load, fog of war, and enemy waves.
- Quest VR mode creates a scaled tabletop rig, uses an XR head camera, tracks left and right controller nodes through Unity XR input, and does not install the desktop camera/input/HUD path.
- Right controller ray feedback, reticle, single-target trigger selection, additive selection modifier, context command button, cancel/clear button, attack-move/stop modifiers, building placement, and the Quest command console all route into shared game command services. Quest console build, produce, cancel-queue, repair, sell, pause, save/load, and new-match buttons are covered through panel-ray activation tests. Selected and damaged entities now show lightweight health bars for tabletop readability.
- Quest mode uses a compact world-space status panel for credits, power, selected count, status text, and core control hints.

## Architecture

- `RtsRuntimeModeResolver` chooses `Desktop` or `QuestVr` from forced test settings, command-line/environment overrides, active XR device state, or an initialized Android OpenXR loader for Quest builds.
- `RtsGame` owns runtime bootstrap and installs only the components for the resolved mode.
- `RtsCommandDispatcher` centralizes ray/hit based RTS command semantics shared by desktop and Quest input.
- `RtsInputController` keeps desktop-only input, camera movement, drag selection, hotkeys, and control groups.
- `QuestTabletopRig`, `QuestTabletopSettings`, `QuestTrackedNodePose`, `QuestRtsInputController`, `QuestWorldHud`, and `QuestCommandConsole` contain Quest-specific rigging, tracked pose, controller input, pointer, and world-space UI behavior.
- `QuestXrProjectValidator` validates package and project settings, and separates automated checks from manual headset/setup checks.
- `QuestRuntimeSmokeReport` validates the generated Quest runtime object graph locally while keeping physical headset behavior marked manual.
- `DesktopRuntimeSmokeReport` validates the generated desktop runtime object graph locally while keeping hands-on desktop controls marked manual.
- `RtsProfileSettings` stores versioned user preferences separately from match saves and applies safe Quest tabletop scale, height, pointer length, and UI scale during rig creation.
- `RtsSaveFileStore` and `RtsSaveService` keep a prior `.bak` slot and fall back to it when the newest save is corrupt or unreadable.
- Save metadata is checksum-validated before display, includes app/config/map/time/state/count fields, and can fall back to backup metadata when the primary slot is corrupt.
- `RtsLifecycleCoordinator` now performs configurable periodic autosaves during active play while suppressing periodic writes during pause, focus loss, save, and load states.
- `RtsGame.TryRestartMatch` provides a New Match system command that clears dirty or restored state, resets match-ended/user pause reasons, and rebuilds the generated skirmish without changing runtime mode.
- `RtsRuntimeDiagnosticsSnapshot` exports generated-match JSON counts for entities, teams, production, resources, fog, save slots, and tabletop scale to support larger-board profiling.
- `RtsSoakScenarioExporter` creates a repeatable populated desktop diagnostics baseline with more units, active production queues, attack-move orders, and an active placement preview for profiler/device comparisons.

## Verification Run

Automated verification last run locally:

- EditMode tests: `69` total, `69` passed, `0` failed.
- XR setup validator: automated package/project-setting checks pass except for local Android Build Support, which is missing from this Unity install; manual headset and Android OpenXR UI verification remain manual.
- Generated Quest runtime smoke report: automated object-graph checks pass in EditMode; physical headset behavior remains manual.
- Generated desktop runtime smoke report: automated object-graph checks pass in EditMode; hands-on desktop control regression remains manual.
- Screenshot exporter: produces a populated desktop-board showcase at `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-sample.png` and a Quest-view world-space UI showcase at `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-quest-sample.png`.
- Runtime diagnostics exporter: produces `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-diagnostics.json`.
- Soak diagnostics exporter: produces `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-soak-diagnostics.json`.
- Desktop build support validator: fails fast on this machine because the Unity 2022.3.62f3 install is missing `WindowsPlayer.exe` under the Windows standalone playback engine template. Repair Unity/Windows Build Support before treating desktop player builds as verified.

## Acceptance Checklist

| Item | Status | Evidence |
| --- | --- | --- |
| Project resolves packages and compiles in Unity 2022.3.62f3 with zero C# compilation errors | Pass | Full EditMode test run completed with no compiler failures. |
| Desktop version remains playable with prior controls | Strong simulated pass, manual playthrough still recommended | Desktop generated-runtime smoke checks, dispatcher behavior, production/building, save/load, and lifecycle are covered by EditMode tests. Manual camera/control regression remains recommended. |
| Quest runtime uses an OpenXR-tracked camera and controllers | Implemented, physical device unverified | Quest rig creates XR head/hand nodes and applies Unity XR device pose data. No physical headset run has been performed from this environment. |
| Right controller can select and command existing units | Simulated pass, physical device unverified | EditMode tests cover trigger selection, additive selection, A/right-primary context commands, cancel/clear, attack-move, stop, pointer feedback, build placement through the Quest console, selected production queuing/canceling through the Quest console, Selected tab repair/sell actions, System tab pause/save/load/New Match activation, selected/damaged health-bar readability, and panel-ray capture. |
| Desktop and Quest input share the same RTS command dispatcher | Pass | Desktop and Quest controllers both delegate selection, placement, context commands, attack-move, stop, and clear/cancel through `RtsCommandDispatcher` and `RtsPlayerCommandService`. |
| Quest path does not create, reposition, or rotate the desktop command camera | Pass | Forced Quest initialization tests assert no `Command Camera`, no `RtsInputController`, and no `RtsHud`; the view camera is the Quest head camera. |
| Quest path does not run the desktop overlay HUD | Pass | Forced Quest initialization tests assert `RtsHud` is absent and `QuestWorldHud` is present. |
| Tabletop scale is configurable and defaults to approximately 125-128 simulation units per physical meter | Pass | `QuestTabletopSettings.SimulationUnitsPerMeter` defaults to `126`, yielding an approximately `1.78m` battlefield width in tests. |
| Android/OpenXR project settings are configured or explicitly validated and documented | Partial local environment gap | Validator checks package pins, Android Build Support, Android API/backend/architecture/package id/input/graphics, OpenXR loaders, SPI, and Oculus Touch. This Unity install is missing Android Build Support, so Quest device builds are not locally verified. |
| New per-frame Quest input code avoids obvious managed allocations | Pass | EditMode test scans Quest input, tracked pose, world HUD, and command console hot loops for obvious allocation-heavy patterns. |
| README and Quest XR setup docs are updated | Pass | `README.md`, `docs/QUEST_XR_SETUP.md`, `docs/SAVE_SYSTEM.md`, `docs/LIFECYCLE_TEST_MATRIX.md`, `docs/PERFORMANCE_TESTING.md`, and this status document record controls, persistence/settings, setup, validation, limitations, and manual checks. |
| Unverified physical-device behavior is clearly identified | Pass | Physical headset/Quest Link/device build verification remains marked manual and unverified here. |

## Manual Work Still Required

- Install or repair Unity Windows Build Support if desktop player builds are needed on this machine.
- Install Android Build Support, SDK/NDK Tools, and OpenJDK for Unity 2022.3.62f3 before Quest Android builds.
- Switch the Unity build target to Android, rerun `Tools > Quest RTS > Apply Recommended Quest Settings`, then inspect Android OpenXR package settings in Project Settings.
- Run the manual Quest Link or Android headset smoke test in `docs/QUEST_XR_SETUP.md`.
- Run a basic profiler pass against the populated soak scenario to confirm the 72 Hz target on actual hardware.

## Known Limitations

- No full VR construction/production radial UI, lasso/box selection, hand tracking, passthrough, spatial anchors, artificial locomotion, multiplayer, campaign, or final art/audio.
- The Quest command console is a functional placeholder for build/produce/selected/system commands.
- Physical Quest tracking, controller feel, comfort, and performance are not claimed as verified until run on headset hardware.
