# Lifecycle Test Matrix

Implemented lifecycle behavior:
- `RtsLifecycleCoordinator` listens to Unity application pause/focus events.
- Pause reasons are tracked by `RtsSimulationClock`.
- Simulation, production, fog, enemy waves, unit orders, turrets, and effects stop while paused.
- Player commands are blocked while the app is paused, focus is lost, or save/load is active.
- User pause blocks gameplay commands but keeps system UI available for resume/save/load.
- Quest pointer visuals are hidden when input is blocked.
- Active placement previews are suspended during pause/focus loss and restored when focus returns.
- Autosave is attempted on application pause and focus loss.

Manual Quest checks still needed on headset:
- Open the Universal Menu during placement, return, and confirm the placement preview resumes cleanly.
- Remove the headset mid-match, wait, then resume and confirm no units advanced during suspension.
- Lose/regain input focus and confirm controller rays are hidden while focus is gone.
- Save during active production and reload to verify queue progress.
- Save with explored fog and reload to verify explored areas remain known.

Automated coverage currently verifies clock pause behavior, focus-loss command blocking, Quest pointer hiding when system input is blocked, user-pause system input, checksum rejection, future-version migration rejection, profile settings clamping/future-version handling, stable IDs, file-backed manual save/load, and a core save/restore round trip.
Command coverage also verifies stop and attack-move order persistence.
