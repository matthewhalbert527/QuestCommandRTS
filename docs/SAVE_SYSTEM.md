# Save System

QuestCommandRTS now has a versioned JSON save envelope with a SHA-256 payload checksum. Saves are written through `RtsSaveService`, serialized by `RtsSaveSerializer`, and stored by `RtsSaveFileStore` under Unity's `Application.persistentDataPath`.

Current schema version: `1`.

Captured state includes:
- match metadata, match time, match state, and status text
- player resources and power
- stable IDs for units, structures, and resource nodes
- unit position, health, move/attack orders, and harvester state
- production queue state, active production progress, and rally points
- resource node amounts
- fog explored cells
- active building placement
- current wave director timers

Restore is a rebuild pass. The current generated battlefield entities/resources are destroyed, saved resource nodes are recreated, structures are recreated first, units are recreated second, then orders and cross-references are restored by stable ID.

The file format is intentionally human-readable for this prototype. Future versions should add migrations in `RtsSaveMigration` or an equivalent DTO-to-DTO migration layer before changing version `1` fields.
