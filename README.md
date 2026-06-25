# Quest Command RTS

Unity prototype for a classic RTS inspired by Command and Conquer. The current build runs in the Windows editor/standalone target; Android support is not required.

## What is included

- Runtime bootstrap that creates a complete playable battlefield when `Assets/Scenes/Battlefield.unity` enters Play Mode.
- Selectable infantry, harvesters, tanks, buildings, health, attacks, production queues, power, and credits.
- Resource harvesting loop with refineries and crystal fields.
- Base construction with placement ghosts, footprint validation, build radius checks, and credit costs.
- HUD buttons for production, construction, and army selection.
- Mouse and keyboard controls for editor testing.
- Basic enemy wave director that periodically sends units at the player base.
- RTS camera pan/zoom, drag selection, production rally points, control groups, minimap pips, and win/loss state.
- Construction tech prerequisites plus repair and sell commands for player structures.
- Editor menu item: `Command RTS > Open Battlefield Scene`.

## Open and run

1. Open this folder in Unity Hub as an existing project.
2. Use Unity 2022.3 LTS or newer.
3. Open `Assets/Scenes/Battlefield.unity`.
4. Press Play.

The scene is intentionally empty. `RtsBootstrap` creates the camera, map, HUD, player base, enemy base, units, and managers at runtime.

## Controls

- Left click or drag: select.
- Shift + left click: add to selection.
- Right click: command selected units, harvest resources, attack enemies, or set a selected production building's rally point.
- Middle click or Escape: cancel/clear.
- Arrow keys or screen-edge mouse: pan camera.
- Mouse wheel: zoom camera.
- Number keys 1-3: train rifleman, harvester, and tank.
- Q/W/E/R/T: place power plant, barracks, refinery, war factory, and turret.
- Z: repair the most damaged selected structure.
- X: sell selected structures for a partial refund.
- Ctrl + 5-9: assign control group; 5-9: recall control group.
- HUD buttons train units and place buildings.

## Next takeover steps

- Merge these scripts into the Antigravity project once its path or repo is available.
- Replace primitive visuals with production assets and tune camera/unit scale for desktop play.
- Add real pathfinding/avoidance, fog of war, save/load, audio, and campaign/skirmish rules.
- Move balance values from `RtsBalance` into ScriptableObjects when the design starts changing frequently.
