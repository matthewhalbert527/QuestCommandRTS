# Performance Testing

Runtime profiler markers were added for the systems most likely to matter as the board scales:
- `QuestCommandRTS.GameUpdate`
- `QuestCommandRTS.UnitOrders`
- `QuestCommandRTS.Production`
- `QuestCommandRTS.EnemyDirector`
- `QuestCommandRTS.FogUpdate`
- `QuestCommandRTS.SaveCapture`
- `QuestCommandRTS.SaveRestore`

`Command RTS > Export Runtime Diagnostics Snapshot` writes a JSON report to `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-diagnostics.json`. The report captures generated-match counts for entities, teams, unit kinds, structure kinds, production queues, resource fields, fog coverage, save slots, and tabletop scale. Use it as a lightweight baseline before and after scaling the board or adding more content.

`Command RTS > Export Quest Scene Budget Snapshot` writes `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-quest-scene-budget.json`. The report captures generated Quest scene-footprint counts for GameObjects, renderers, line renderers, shared materials, colliders, cameras, lights, canvases, tactical map canvases, fog overlay, and visual-only set dressing. The latest local Quest baseline is 1509 generated GameObjects, 909 renderers, 81 unique shared materials, 76 enabled colliders, 1 camera, 1 light, 3 world-space canvases, 0 overlay canvases, 1 tactical map canvas, 1 fog overlay renderer, 0 legacy fog-cell objects, and 0 set-dressing colliders.

`Tools > Quest RTS > Validate Quest Scene Budget` runs automated local gates for the generated Quest footprint. This does not prove 72 Hz on hardware, but it catches accidental growth such as restoring per-cell fog renderers, adding overlay UI to Quest mode, losing the tactical map panel, adding extra lights/cameras, or letting visual set dressing block raycasts. The budget report also includes a named fog overlay gate that expects one `Fog Overlay` renderer and zero legacy `Fog Cell` objects.

`Command RTS > Export Soak Diagnostics Snapshot` writes `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\quest-command-rts-soak-diagnostics.json`. This generated desktop scenario adds a player War Factory, a larger mixed player/enemy army, attack-move orders, active production queues, and a valid active placement preview. It is not a replacement for headset profiling, but it gives the Unity Profiler and later Quest device runs a repeatable populated baseline.

Automated EditMode coverage also scans Quest input, pointer feedback helpers, pose, HUD, tactical map refresh paths, and console hot loops for obvious allocation-heavy patterns such as per-frame GameObject/material creation, scene-wide object searches, or LINQ. Fog of war uses one texture-backed overlay with the same 56 x 56 logical visibility grid instead of thousands of per-cell renderers and materials.

Suggested manual profiling pass:
- Start a desktop match and capture a baseline with the Unity Profiler.
- Export the runtime diagnostics snapshot and keep it with the profiler capture so object counts are known.
- Export the Quest scene budget snapshot and keep it with Quest Link or device profiler captures so rendering/object footprint is known.
- Export the soak diagnostics snapshot, reproduce the same populated state in Play Mode or on device, and profile the heavier object/queue/order mix.
- Queue production, move a mixed army, and leave fog enabled.
- Trigger a manual save with `F5`, the desktop HUD System button, or the Quest console System tab.
- Confirm save capture/restore spikes are acceptable and do not add recurring per-frame cost.

Soak coverage still requiring physical or Play Mode verification:
- active fog updates under sustained runtime ticks
- repeated save/load attempts during a populated match
- focus/pause/resume cycling during a populated match
- Quest headset frame timing and 72 Hz comfort
