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

Automated EditMode coverage also scans Quest input, pose, pointer, HUD, and console hot loops for obvious allocation-heavy patterns such as per-frame GameObject/material creation, scene-wide object searches, or LINQ.

Suggested manual profiling pass:
- Start a desktop match and capture a baseline with the Unity Profiler.
- Export the runtime diagnostics snapshot and keep it with the profiler capture so object counts are known.
- Queue production, move a mixed army, and leave fog enabled.
- Trigger a manual save with `F5`, the desktop HUD System button, or the Quest console System tab.
- Confirm save capture/restore spikes are acceptable and do not add recurring per-frame cost.

Soak scenario still needed:
- many player units
- many enemy units
- several active production queues
- active fog updates
- repeated save/load attempts
- focus/pause/resume cycling
