# Save System

QuestCommandRTS now has a versioned JSON save envelope with a SHA-256 payload checksum. Saves are written through `RtsSaveService`, serialized by `RtsSaveSerializer`, and stored by `RtsSaveFileStore` under Unity's `Application.persistentDataPath`.

Each write stages the newest contents to a temporary file before replacing the primary slot. When a primary slot already exists, the prior valid file is copied to a `.bak` backup. Loads try the primary slot first and then fall back to the backup if the primary file is missing, corrupt, or fails checksum/schema validation.

Current schema version: `1`.

Captured state includes:
- match metadata, application version, slot id, skirmish config id, difficulty id, map id, map seed, match time, match state, and status text
- player resources and power
- stable IDs for units, structures, and resource nodes
- unit position, health, move/attack orders, and harvester state
- attack-move and stop-cleared order state
- production queue state, active production progress, and rally points
- resource node amounts
- fog explored cells
- active building placement
- enemy commander credits plus wave, income, rebuild, production, and idle-order timers

`RtsSaveSerializer.TryReadMetadata` and `RtsSaveService.TryGetSlotMetadata` read display-safe metadata only after validating the envelope checksum and schema. Metadata lookup uses the same primary-then-backup fallback policy as full loads, and `RtsSaveFileStore.ListSlots` includes backup-only slots so a valid `.bak` remains discoverable if the newest primary slot is removed or corrupt.

Restore is a rebuild pass. The current generated battlefield entities/resources are destroyed, saved resource nodes are recreated, structures are recreated first, units are recreated second, then orders and cross-references are restored by stable ID.

The manual slot is exposed in-game:
- Desktop: `F5` saves and `F9` loads.
- Desktop HUD: `System` buttons save, load, and pause/resume.
- Quest: command console `System` tab saves, loads, and pauses/resumes.

Profile settings are stored separately in `profile-settings.json` under `Application.persistentDataPath`. The profile uses schema version `1`, clamps unsafe values on load/save, rejects unsupported future schemas with a clear error, and applies Quest tabletop scale, tabletop height, pointer length, world-space UI scale, and the periodic autosave interval during runtime.

The file format is intentionally human-readable for this prototype. Future versions should add migrations in `RtsSaveMigration` or an equivalent DTO-to-DTO migration layer before changing version `1` fields.
