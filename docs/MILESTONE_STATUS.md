# Quest XR Foundation Milestone Status

Last updated: 2026-06-25

This document records what has been implemented and what has actually been verified for the Quest XR Foundation and Core Command Vertical Slice. It is deliberately conservative: simulated/editor coverage is marked separately from physical Quest or Quest Link verification.

## Implemented Experience

- Desktop mode keeps the generated battlefield, command camera, mouse and keyboard RTS controls, desktop HUD, build hotkeys, production, control groups, save/load, fog of war, and enemy waves.
- Quest VR mode creates a scaled tabletop rig, uses an XR head camera, tracks left and right controller nodes through Unity XR input, and does not install the desktop camera/input/HUD path.
- Right controller ray feedback, reticle, single-target trigger selection, additive and terrain-centered area selection modifiers, context command button, cancel/clear button, attack-move/stop modifiers, building placement, and the Quest command console all route into shared game command services. Button-down transition tests cover held command/cancel inputs and the X/left-primary console toggle. Paused Quest placement input is covered so active placement previews are not moved, confirmed, or canceled while player input is paused. Pointer feedback colors distinguish attack, harvest, rally, move, UI, and invalid targets. Quest console build, produce, queued/active production cancel, repair, sell, pause, save/load, and new-match buttons are covered through panel-ray activation tests. Selected and damaged entities now show lightweight health bars for tabletop readability.
- Quest mode uses a compact world-space status panel for credits, power, selected count, status text, core command hints, and the X/left-primary command-console hint, with cached refreshes that update only when displayed state changes. The Quest tactical map shows a non-interactive battle-view panel with pooled resource and visible-force pips. The Quest command console uses a layered holographic-style panel frame with color-coded build/production row icon tiles and text labels.
- Quest tabletop scale now includes editor profile presets and screenshot export paths for the default approximately 1.78m board and a room-sized approximately 4.0m board with longer pointer reach.
- The generated battlefield includes original primitive set dressing for a tabletop war-room read: sandy terrain accents, projected water channels, ridges, rocks, scorches, rails, and corner pylons. The set dressing is visual-only so it does not steal command raycasts from the battlefield.

## Architecture

- `RtsRuntimeModeResolver` chooses `Desktop` or `QuestVr` from forced test settings, command-line/environment overrides, active XR device state, or an initialized Android OpenXR loader for Quest builds. EditMode tests cover override precedence and fallback to automatic XR state.
- `RtsGame` owns runtime bootstrap and installs only the components for the resolved mode. Runtime-created camera, light, and desktop UI event-system objects are parented under the generated root so editor exporters can cleanly remove the generated runtime.
- `RtsCommandDispatcher` centralizes ray/hit based RTS command semantics shared by desktop and Quest input. EditMode source checks guard it from directly reading mouse, keyboard, screen, or XR device APIs, and command tests cover context priority plus living-player-unit filtering.
- `RtsInputController` keeps desktop-only input, camera movement, drag selection, hotkeys, and control groups. Source checks guard it from duplicating context-command target resolution that belongs in `RtsCommandDispatcher`.
- `QuestTabletopRig`, `QuestTabletopSettings`, `QuestTrackedNodePose`, `QuestRtsInputController`, `QuestWorldHud`, `QuestTacticalMap`, and `QuestCommandConsole` contain Quest-specific rigging, tracked pose, controller input, pointer, and world-space UI behavior. Source checks guard the Quest path from adding artificial locomotion, board grabbing, or continuous tracked-pose overrides.
- `QuestXrProjectValidator` validates package and project settings, and separates automated checks from manual headset/setup checks.
- `QuestRuntimeSmokeReport` validates the generated Quest runtime object graph locally while keeping physical headset behavior marked manual.
- `DesktopRuntimeSmokeReport` validates the generated desktop runtime object graph locally while keeping hands-on desktop controls marked manual.
- Generated desktop, Quest, and scene-budget validators have EditMode cleanup coverage so their temporary runtime roots and generated camera/light/UI event-system objects do not pollute the editor scene.
- `RtsProfileSettings` stores versioned user preferences separately from match saves and applies safe Quest tabletop scale, height, pointer length, and UI scale during rig creation, including a room-sized tabletop profile preset.
- `RtsSaveFileStore` and `RtsSaveService` keep a prior `.bak` slot and fall back to it when the newest save is corrupt or unreadable.
- Save metadata is checksum-validated before display, includes app/config/map/time/state/count fields, and can fall back to backup metadata when the primary slot is corrupt.
- `RtsLifecycleCoordinator` now performs configurable periodic autosaves during active play while suppressing periodic writes during pause, focus loss, save, and load states.
- `RtsGame.TryRestartMatch` provides a New Match system command that clears dirty or restored state, resets match-ended/user pause reasons, and rebuilds the generated skirmish without changing runtime mode.
- `RtsRuntimeDiagnosticsSnapshot` exports generated-match JSON counts for entities, teams, production, resources, fog, save slots, and tabletop scale to support larger-board profiling.
- `RtsSceneBudgetSnapshot` exports generated Quest object, renderer, material, collider, light, camera, UI, tactical map, and fog overlay counts. `RtsFogOfWar` now uses one texture-backed overlay for the same 56 x 56 logical fog grid instead of per-cell renderers and materials, and the Quest budget validator explicitly fails if legacy `Fog Cell` objects return.
- `RtsSoakScenarioExporter` creates a repeatable populated desktop diagnostics baseline with more units, active production queues, attack-move orders, and an active placement preview for profiler/device comparisons.

## Verification Run

Automated verification last run locally:

- EditMode tests: `96` total, `96` passed, `0` failed.
- XR setup validator: automated package/project-setting checks pass, including forbidden Meta XR package absence, except for local Android Build Support, which is missing from this Unity install; manual headset and Android OpenXR UI verification remain manual.
- Generated Quest runtime smoke report: automated object-graph, desktop overlay/event-system absence, XR node binding, pointer-geometry, world-UI rig anchoring, command-console panel-ray, non-interactive world-HUD/tactical-map, and world-HUD hint checks pass in EditMode; physical headset behavior remains manual.
- Generated desktop runtime smoke report: automated object-graph checks pass in EditMode; hands-on desktop control regression remains manual.
- Screenshot exporter: produces a populated desktop-board showcase at `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-sample.png`, a Quest-view world-space UI showcase at `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-quest-sample.png`, and a room-sized Quest profile showcase at `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-quest-room-sample.png`.
- Runtime diagnostics exporter: produces `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-diagnostics.json`.
- Quest scene budget exporter: produces `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-quest-scene-budget.json`; latest generated Quest baseline is 1177 GameObjects, 694 renderers, 79 unique shared materials, 76 enabled colliders, 1 camera, 1 light, 3 world-space canvases, 0 overlay canvases, 1 tactical map canvas, 1 fog overlay renderer, 0 legacy fog-cell objects, and 0 set-dressing colliders.
- Soak diagnostics exporter: produces `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-soak-diagnostics.json`.
- Desktop build support validator: fails fast on this machine because the Unity 2022.3.62f3 install is missing `WindowsPlayer.exe` under the Windows standalone playback engine template. Repair Unity/Windows Build Support before treating desktop player builds as verified.

## Acceptance Checklist

| Item | Status | Evidence |
| --- | --- | --- |
| Project resolves packages and compiles in Unity 2022.3.62f3 with zero C# compilation errors | Pass | Full EditMode test run completed with no compiler failures. |
| Desktop version remains playable with prior controls | Strong simulated pass, manual playthrough still recommended | Desktop generated-runtime smoke checks, dispatcher behavior, production/building, save/load, and lifecycle are covered by EditMode tests. Manual camera/control regression remains recommended. |
| Quest runtime uses an OpenXR-tracked camera and controllers | Implemented, physical device unverified | Quest rig creates XR head/hand nodes and applies Unity XR device pose data. Source checks assert Quest input does not add artificial locomotion or board manipulation and that the tabletop rig does not continuously overwrite tracked head/hand poses. No physical headset run has been performed from this environment. |
| Right controller can select and command existing units | Simulated pass, physical device unverified | EditMode tests cover trigger selection, additive selection, terrain-centered area selection, A/right-primary context commands, button-down transitions, cancel/clear, attack-move, stop, pointer feedback geometry and context colors, build placement through the Quest console, paused-placement input blocking, selected production queuing plus queued/active cancellation through the Quest console, color-coded console row icon tiles, Selected tab repair/sell actions, System tab pause/save/load/New Match activation, selected/damaged health-bar readability, and panel-ray capture. |
| Desktop and Quest input share the same RTS command dispatcher | Pass | Desktop and Quest controllers both delegate selection, placement, context commands, attack-move, stop, and clear/cancel through `RtsCommandDispatcher` and `RtsPlayerCommandService`; source checks assert the dispatcher does not read device input APIs directly and desktop input does not duplicate context-target rules. Dispatcher tests also verify command priority and that gathered command units are living player units. |
| Quest path does not create, reposition, or rotate the desktop command camera | Pass | Forced Quest initialization tests assert no `Command Camera`, no `RtsInputController`, and no `RtsHud`; the view camera is the Quest head camera. |
| Quest path does not run the desktop overlay HUD | Pass | Forced Quest initialization tests assert `RtsHud` is absent and `QuestWorldHud` plus `QuestTacticalMap` are present. Desktop smoke checks also assert Quest-only world UI is absent from desktop mode. |
| Tabletop scale is configurable and defaults to approximately 125-128 simulation units per physical meter | Pass | `QuestTabletopSettings.SimulationUnitsPerMeter` defaults to `126`, yielding an approximately `1.78m` battlefield width in tests. Profile presets also cover an optional approximately `4.0m` room-sized board. |
| Android/OpenXR project settings are configured or explicitly validated and documented | Partial local environment gap | Validator checks package pins, absence of forbidden Meta XR SDK packages, Android Build Support, Android API/backend/architecture/package id/input/graphics, OpenXR loaders, SPI, and Oculus Touch. This Unity install is missing Android Build Support, so Quest device builds are not locally verified. |
| New per-frame Quest input code avoids obvious managed allocations | Pass | EditMode test scans Quest input, pointer feedback helpers, tracked pose, world HUD, tactical map refresh paths, and command console hot loops for obvious allocation-heavy patterns. Quest scene budget tests also guard generated object, renderer, material, collider, camera, light, UI, tactical map, and visual set-dressing counts. |
| README and Quest XR setup docs are updated | Pass | `README.md`, `docs/QUEST_XR_SETUP.md`, `docs/SAVE_SYSTEM.md`, `docs/LIFECYCLE_TEST_MATRIX.md`, `docs/PERFORMANCE_TESTING.md`, and this status document record controls, persistence/settings, setup, validation, limitations, and manual checks. |
| Unverified physical-device behavior is clearly identified | Pass | Physical headset/Quest Link/device build verification remains marked manual and unverified here. |

## Manual Work Still Required

- Install or repair Unity Windows Build Support if desktop player builds are needed on this machine.
- Install Android Build Support, SDK/NDK Tools, and OpenJDK for Unity 2022.3.62f3 before Quest Android builds.
- Switch the Unity build target to Android, rerun `Tools > Quest RTS > Apply Recommended Quest Settings`, then inspect Android OpenXR package settings in Project Settings.
- Run the manual Quest Link or Android headset smoke test in `docs/QUEST_XR_SETUP.md`.
- Run a basic profiler pass against the populated soak scenario to confirm the 72 Hz target on actual hardware.

## Known Limitations

- No full VR construction/production radial UI, box/lasso selection volume, hand tracking, passthrough, spatial anchors, artificial locomotion, multiplayer, campaign, or final art/audio.
- The Quest command console and tactical map are functional placeholder world panels; they now have holographic framing, color-coded row icon tiles, and pooled map pips, but not a final radial/spatial command UI.
- Physical Quest tracking, controller feel, comfort, and performance are not claimed as verified until run on headset hardware.
