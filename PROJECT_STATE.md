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
- Gameplay currently includes unit selection, mouse/XR controller command input, construction placement, production queues, resource harvesting, health/combat, HUD controls, Skyraider/Orca Lifter air units, the Dual Helipad producer, and basic enemy waves.
- Units now use lightweight local avoidance while moving, so direct-line orders steer around nearby units, structures, and resource fields without requiring a baked NavMesh.
- HUD production/build controls now use square cards; supplied air/helipad artwork fills the card, unit queue count appears as a bottom-left badge, and cost is shown on hover.
- Selected combat units show animated command previews under the pointer: blue for movement, red for enemy targeting, and amber for guard mode.
- Holding G/Alt on desktop or the secondary Quest controller button while commanding assigns selected units to guard a friendly unit, building, or ground area.
- Combat feedback includes muzzle flashes, impact sparks, fading tracers, smoke-trailed heavy projectiles, expanding explosion effects, Kenney CC0 weapon/impact SFX, generated explosion layers, and generated unit voice responses.
- Editor helper menu: `Quest RTS > Apply Quest Android Settings`

## Known decisions

- Preserve the recovered source folder as historical output.
- Use this copied workspace as the active development project for this thread.
- Keep development Quest-friendly: readable scale, simple input paths, and editor/PC controls for fast iteration.
- Prefer incremental improvements that can be verified without depending on headset-only behavior.

## Open question

- The old README mentions an external "Antigravity" project. If that repo exists, merge this prototype into that repo instead of treating this Unity folder as the final home.

## Next likely development steps

- Upgrade local avoidance into full pathfinding if maps gain hard chokepoints or complex base layouts.
- Add fog of war and a minimap.
- Add save/load and skirmish setup.
- Add audio and continue replacing primitive placeholder visuals with production assets.
- Move balance values from code into ScriptableObjects when tuning becomes frequent.
