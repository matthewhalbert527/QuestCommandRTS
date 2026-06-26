# Quest Command RTS

Unity prototype for a Quest-friendly classic RTS inspired by Command and Conquer.

## What is included

- Runtime bootstrap that creates a complete playable battlefield when `Assets/Scenes/Battlefield.unity` enters Play Mode.
- Selectable infantry, harvesters, tanks, buildings, health, attacks, production queues, power, and credits.
- Resource harvesting loop with refineries and crystal fields.
- Base construction with placement ghosts, footprint validation, build radius checks, and credit costs.
- HUD buttons for production, construction, and army selection.
- Mouse controls for editor testing and XR controller ray support through `UnityEngine.XR`.
- Basic enemy wave director that periodically sends units at the player base.
- Editor menu item: `Quest RTS > Apply Quest Android Settings`.

## Open and run

1. Open this folder in Unity Hub as an existing project.
2. Use Unity 2022.3 LTS or newer.
3. Open `Assets/Scenes/Battlefield.unity`.
4. Press Play.

The scene is intentionally empty. `RtsBootstrap` creates the camera, map, HUD, player base, enemy base, units, and managers at runtime.

## Controls

- Left click or Quest trigger: select.
- Shift + left click: add to selection.
- Right click, grip, or primary controller button: command selected units.
- Middle click, secondary controller button, or Escape: cancel/clear.
- HUD buttons train units and place buildings.

## Quest build notes

Use `Quest RTS > Apply Quest Android Settings` first. Then open Project Settings and enable XR Plug-in Management for Android with OpenXR/Meta Quest support. The project includes Unity package dependencies for XR Management and OpenXR, but Unity still needs to generate its local XR settings assets inside the editor.

## Next takeover steps

- Merge these scripts into the Antigravity project once its path or repo is available.
- Replace primitive visuals with production assets and tune scale for Quest comfort.
- Add real pathfinding/avoidance, fog of war, minimap, save/load, audio, and campaign/skirmish rules.
- Move balance values from `RtsBalance` into ScriptableObjects when the design starts changing frequently.
