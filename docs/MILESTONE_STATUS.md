# Quest XR Foundation Milestone Status

Last updated: 2026-06-25

This document records what has been implemented and what has actually been verified for the Quest XR Foundation and Core Command Vertical Slice. It is deliberately conservative: simulated/editor coverage is marked separately from physical Quest or Quest Link verification.

## Implemented Experience

- Desktop mode keeps the generated battlefield, command camera, mouse and keyboard RTS controls, desktop HUD, build hotkeys, production, control groups, save/load, fog of war, and enemy waves.
- Quest VR mode creates a scaled tabletop rig, uses an XR head camera, tracks left and right controller nodes through Unity XR input, and does not install the desktop camera/input/HUD path.
- Right controller ray feedback, reticle, single-target trigger selection, additive selection modifier, context command button, cancel/clear button, attack-move/stop modifiers, building placement, and the Quest command console all route into shared game command services.
- Quest mode uses a compact world-space status panel for credits, power, selected count, status text, and core control hints.

## Architecture

- `RtsRuntimeModeResolver` chooses `Desktop` or `QuestVr` from forced test settings, command-line/environment overrides, active XR device state, or an initialized Android OpenXR loader for Quest builds.
- `RtsGame` owns runtime bootstrap and installs only the components for the resolved mode.
- `RtsCommandDispatcher` centralizes ray/hit based RTS command semantics shared by desktop and Quest input.
- `RtsInputController` keeps desktop-only input, camera movement, drag selection, hotkeys, and control groups.
- `QuestTabletopRig`, `QuestTabletopSettings`, `QuestTrackedNodePose`, `QuestRtsInputController`, `QuestWorldHud`, and `QuestCommandConsole` contain Quest-specific rigging, tracked pose, controller input, pointer, and world-space UI behavior.
- `QuestXrProjectValidator` validates package and project settings, and separates automated checks from manual headset/setup checks.
- `QuestRuntimeSmokeReport` validates the generated Quest runtime object graph locally while keeping physical headset behavior marked manual.

## Verification Run

Automated verification last run locally:

- EditMode tests: `48` total, `48` passed, `0` failed.
- XR setup validator: automated package/project-setting checks passed; manual headset and Android OpenXR UI verification remain manual.
- Generated Quest runtime smoke report: automated object-graph checks pass in EditMode; physical headset behavior remains manual.
- Screenshot exporter: produced `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-sample.png`.
- Desktop build support validator: fails fast on this machine because the Unity 2022.3.62f3 install is missing `WindowsPlayer.exe` under the Windows standalone playback engine template. Repair Unity/Windows Build Support before treating desktop player builds as verified.

## Acceptance Checklist

| Item | Status | Evidence |
| --- | --- | --- |
| Project resolves packages and compiles in Unity 2022.3.62f3 with zero C# compilation errors | Pass | Full EditMode test run completed with no compiler failures. |
| Desktop version remains playable with prior controls | Partially verified | Desktop initialization, dispatcher behavior, production/building, save/load, and lifecycle are covered by EditMode tests. Manual desktop play remains recommended. |
| Quest runtime uses an OpenXR-tracked camera and controllers | Implemented, physical device unverified | Quest rig creates XR head/hand nodes and applies Unity XR device pose data. No physical headset run has been performed from this environment. |
| Right controller can select and command existing units | Simulated pass, physical device unverified | EditMode tests cover trigger selection, additive selection, A/right-primary context commands, cancel/clear, attack-move, stop, pointer feedback, and placement flow through Quest input frames. |
| Desktop and Quest input share the same RTS command dispatcher | Pass | Desktop and Quest controllers both delegate selection, placement, context commands, attack-move, stop, and clear/cancel through `RtsCommandDispatcher` and `RtsPlayerCommandService`. |
| Quest path does not create, reposition, or rotate the desktop command camera | Pass | Forced Quest initialization tests assert no `Command Camera`, no `RtsInputController`, and no `RtsHud`; the view camera is the Quest head camera. |
| Quest path does not run the desktop overlay HUD | Pass | Forced Quest initialization tests assert `RtsHud` is absent and `QuestWorldHud` is present. |
| Tabletop scale is configurable and defaults to approximately 125-128 simulation units per physical meter | Pass | `QuestTabletopSettings.SimulationUnitsPerMeter` defaults to `126`, yielding an approximately `1.78m` battlefield width in tests. |
| Android/OpenXR project settings are configured or explicitly validated and documented | Pass with manual remainder | Validator checks package pins, Android API/backend/architecture/package id/input/graphics, OpenXR loaders, SPI, and Oculus Touch. Android OpenXR package settings may require switching to Android with Android Build Support installed. |
| New per-frame Quest input code avoids obvious managed allocations | Pass | EditMode test scans Quest input, tracked pose, world HUD, and command console hot loops for obvious allocation-heavy patterns. |
| README and Quest XR setup docs are updated | Pass | `README.md`, `docs/QUEST_XR_SETUP.md`, `docs/PERFORMANCE_TESTING.md`, and this status document record controls, setup, validation, limitations, and manual checks. |
| Unverified physical-device behavior is clearly identified | Pass | Physical headset/Quest Link/device build verification remains marked manual and unverified here. |

## Manual Work Still Required

- Install or repair Unity Windows Build Support if desktop player builds are needed on this machine.
- Install Android Build Support, SDK/NDK Tools, and OpenJDK for Unity 2022.3.62f3 before Quest Android builds.
- Switch the Unity build target to Android, rerun `Tools > Quest RTS > Apply Recommended Quest Settings`, then inspect Android OpenXR package settings in Project Settings.
- Run the manual Quest Link or Android headset smoke test in `docs/QUEST_XR_SETUP.md`.
- Run a basic profiler pass against a populated match to confirm the 72 Hz target on actual hardware.

## Known Limitations

- No full VR construction/production radial UI, lasso/box selection, hand tracking, passthrough, spatial anchors, artificial locomotion, multiplayer, campaign, or final art/audio.
- The Quest command console is a functional placeholder for build/produce/selected/system commands.
- Physical Quest tracking, controller feel, comfort, and performance are not claimed as verified until run on headset hardware.
