# Quest Command RTS Project State

Last updated: 2026-06-26

## Active workspace

- Active project path: `C:\Users\matth\Documents\Codex\2026-06-26\quest-pc-rts-game\work\QuestCommandRTS`
- Source recovered from: `C:\Users\matth\Documents\Codex\2026-06-24\i-s\outputs\QuestCommandRTS`
- Engine: Unity `2022.3.62f3`
- Target platforms: Meta Quest Android and PC/editor play mode

## Current state

- Unity project is a runtime-generated RTS prototype.
- Main scene: `Assets\Scenes\Battlefield.unity`
- Runtime bootstrap: `Assets\Scripts\Runtime\RtsGame.cs`
- Gameplay currently includes unit selection, mouse/XR controller command input, construction placement, production queues, resource harvesting, health/combat, HUD controls, and basic enemy waves.
- Editor helper menu: `Quest RTS > Apply Quest Android Settings`

## Known decisions

- Preserve the recovered source folder as historical output.
- Use this copied workspace as the active development project for this thread.
- Keep development Quest-friendly: readable scale, simple input paths, and editor/PC controls for fast iteration.
- Prefer incremental improvements that can be verified without depending on headset-only behavior.

## Open question

- The old README mentions an external "Antigravity" project. If that repo exists, merge this prototype into that repo instead of treating this Unity folder as the final home.

## Next likely development steps

- Add stronger pathfinding/avoidance for units.
- Add fog of war and a minimap.
- Add save/load and skirmish setup.
- Add audio and replace primitive placeholder visuals.
- Move balance values from code into ScriptableObjects when tuning becomes frequent.
