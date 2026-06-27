# Bastion Fabrication Hub — Unity / Meta Quest RTS structure

The **Bastion Fabrication Hub** is an original construction-yard replacement for the Bastion building family. It is the player's primary base-construction and deployment structure, with a heavy command block, assembly bay, animated beacon, and articulated fabrication crane.

## Import

1. Copy `Assets/BastionFabricationHub` into the root of your Unity project.
2. Let Unity finish importing and compiling.
3. Unity creates `Assets/BastionFabricationHub/Prefabs/Bastion_FabricationHub.prefab` automatically.
4. If needed, run **Tools > Bastion Fabrication Hub > Create or Rebuild Prefab**.

The prefab builder reuses `Assets/BastionStructures/Materials/Bastion_Structures_Palette.mat` when the earlier structures pack is installed. Otherwise it creates a compatible local URP/Built-in material.

## Runtime components

- `BastionFabricationHub` exposes health, power consumption, build radius, construction state, and gameplay sockets.
- `BastionFabricationCrane` aims the crane at a world-space work point and moves the trolley.
- `BastionFabricationDoor` controls the assembly-bay door.
- `BastionFabricationSpin` rotates the command beacon.

Example:

```csharp
BastionFabricationHub hub = GetComponent<BastionFabricationHub>();
hub.SetConstructionActive(true);

BastionFabricationCrane crane = GetComponent<BastionFabricationCrane>();
crane.SetWorkPoint(buildSite.position);
```

## Sockets

- `BuildOrigin`
- `RallyPoint`
- `VehicleExit`
- `BlueprintEmitter`
- `ConstructionFXSocket` — parented to the crane trolley
- `CraneTip`

## Asset profile

- LOD0 triangles: 3,852
- LOD1 triangles: 140
- Footprint: 12.0 × 10.4 meters
- Nominal height: 7.25 meters
- Materials: 1
- Palette texture: 352 × 32 pixels
- Collider: one inexpensive box collider
- Coordinate system: meters, +Y up, +Z front

The package supplies geometry, presentation, sockets, and generic runtime controls. Connect build queues, placement validation, economy rules, construction VFX, networking, and destruction behavior to your game's systems.
