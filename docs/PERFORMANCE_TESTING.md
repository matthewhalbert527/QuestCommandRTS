# Performance Testing

Runtime profiler markers were added for the systems most likely to matter as the board scales:
- `QuestCommandRTS.GameUpdate`
- `QuestCommandRTS.UnitOrders`
- `QuestCommandRTS.Production`
- `QuestCommandRTS.EnemyDirector`
- `QuestCommandRTS.FogUpdate`
- `QuestCommandRTS.SaveCapture`
- `QuestCommandRTS.SaveRestore`

Suggested manual profiling pass:
- Start a desktop match and capture a baseline with the Unity Profiler.
- Queue production, move a mixed army, and leave fog enabled.
- Trigger a manual save through `RtsSaveService.TryWriteSlot` from a debug hook or editor script.
- Confirm save capture/restore spikes are acceptable and do not add recurring per-frame cost.

Soak scenario still needed:
- many player units
- many enemy units
- several active production queues
- active fog updates
- repeated save/load attempts
- focus/pause/resume cycling
