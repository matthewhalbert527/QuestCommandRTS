# Bastion Base Structures — Unity / Meta Quest RTS asset pack

This package contains 13 original low-poly assets:

- Barracks
- War Factory
- Refinery and Harvester
- Power Plant and Large Power Plant
- Communications Center
- Repair Bay
- MASH field hospital
- Tech Center
- Turret, Gun Tower, and Advanced Gun Tower

The pack uses one shared palette material, compact emission texture, direct Unity OBJ imports, automatic prefab construction, simplified LOD1 meshes, inexpensive box colliders, and gameplay sockets. The visual language is classic base-building RTS, but the geometry, proportions, details, and code are original and contain no copied franchise logos or meshes.

## Import

1. Copy `Assets/BastionStructures` into the root of your Unity project.
2. Let Unity finish importing and compiling.
3. The editor script creates all prefabs in `Assets/BastionStructures/Prefabs` automatically.
4. If needed, run **Tools > Bastion Base Structures > Create or Rebuild All Prefabs**.

## Generated prefabs

| Asset | Prefab | Role | LOD0 tris | LOD1 tris | Footprint |
|---|---|---:|---:|---:|---:|
| Bastion Barracks | `Bastion_Barracks.prefab` | InfantryProduction | 352 | 44 | 6.2 × 5.2 m |
| Bastion War Factory | `Bastion_WarFactory.prefab` | VehicleProduction | 668 | 60 | 10.2 × 9.0 m |
| Bastion Refinery | `Bastion_Refinery.prefab` | Economy | 1,220 | 100 | 9.2 × 8.0 m |
| Bastion Harvester | `Bastion_Harvester.prefab` | Vehicle | 1,912 | 80 | 4.1 × 6.2 m |
| Bastion Power Plant | `Bastion_PowerPlant.prefab` | Power | 832 | 88 | 5.6 × 5.0 m |
| Bastion Large Power Plant | `Bastion_LargePowerPlant.prefab` | Power | 1,748 | 184 | 8.2 × 7.2 m |
| Bastion Communications Center | `Bastion_CommunicationsCenter.prefab` | Support | 972 | 176 | 6.8 × 6.6 m |
| Bastion Repair Bay | `Bastion_RepairBay.prefab` | Support | 512 | 60 | 8.2 × 7.2 m |
| Bastion MASH | `Bastion_MASH.prefab` | Medical | 332 | 56 | 7.2 × 6.2 m |
| Bastion Tech Center | `Bastion_TechCenter.prefab` | Technology | 1,632 | 152 | 8.2 × 8.0 m |
| Bastion Turret | `Bastion_Turret.prefab` | Defense | 536 | 76 | 3.4 × 3.4 m |
| Bastion Gun Tower | `Bastion_GunTower.prefab` | Defense | 972 | 92 | 4.0 × 4.0 m |
| Bastion Advanced Gun Tower | `Bastion_AdvancedGunTower.prefab` | Defense | 1,488 | 68 | 4.8 × 4.8 m |

## Runtime components

- `BastionStructure` stores role, footprint, hit points, power production/consumption, and production/service sockets.
- `BastionDefenseController` aims the turret, gun tower, and advanced gun tower at a world-space point.
- `BastionDoorController` opens and closes the War Factory bay door.
- `BastionSpin` drives radar, turbine, beacon, refinery drum, harvester collector, and tech-ring motion.
- `BastionRepairBayController` deploys the two service arms.

Example defense use:

```csharp
BastionDefenseController defense = GetComponent<BastionDefenseController>();
defense.SetAimPoint(target.position);
Transform muzzle = defense.Muzzles[0];
```

Example War Factory door use:

```csharp
BastionDoorController door = GetComponent<BastionDoorController>();
door.SetOpen(true);
```

The package does not impose a resource system, build queue, pathfinding, projectile implementation, repair rules, or networking model. Connect the provided metadata, sockets, and events to your game systems.

## Coordinate conventions

- 1 Unity unit = 1 meter
- +Y = up
- +Z = front / primary entrance / weapon direction
- Building origins are centered at ground level
- Harvester origin is centered between its tracks

## Quest-oriented profile

- Assets: 13
- Shared materials: 1
- Shared texture size: 352 × 32 pixels
- Total LOD0 triangles across the full pack: 13,176
- Total LOD1 triangles across the full pack: 1,236
- No transparency, normal maps, or high-cost shader features
- GPU instancing enabled on the generated material

Profile the complete battlefield on the target Quest headset. Shadows, particles, AI, projectiles, overdraw, and simultaneous animated units can dominate performance before the building meshes do.
