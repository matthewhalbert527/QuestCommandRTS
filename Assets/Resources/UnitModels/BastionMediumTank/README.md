# Bastion MT-12 Rider — Unity / Meta Quest tank asset

This is an original low-poly RTS medium tank with a deliberately chunky, high-readability silhouette. It includes a boardable rear-right standing platform and an independently articulated six-barrel rotary gun. The visual language is inspired by classic base-building RTS vehicles, without reproducing a specific game unit, faction logo, or texture.

## Import

1. Copy the included `Assets/BastionMediumTank` folder into your Unity project's `Assets` folder.
2. Let Unity finish importing and compiling.
3. The editor script creates `Assets/BastionMediumTank/Prefabs/Bastion_MT12_Rider.prefab` automatically.
4. If needed, run **Tools > Bastion MT-12 Rider > Create or Rebuild Prefab**.
5. Drag the prefab into the scene.

The OBJ files are used directly by Unity. Optional GLB exports are in `Extras` and require a glTF importer if you choose to use them.

## Vehicle hierarchy

```text
Bastion_MT12_Rider
├── Hull / LeftTrack / RightTrack
├── TurretYaw
│   ├── TurretModel
│   ├── CannonPitch
│   │   ├── CannonModel
│   │   └── MainMuzzle
│   └── GunnerStation
│       ├── OccupantAnchor + hand/foot/head anchors
│       └── GunnerYaw
│           ├── MinigunBase
│           └── MinigunPitch
│               ├── MinigunBody
│               └── BarrelSpin
│                   ├── MinigunBarrels
│                   └── MinigunMuzzle
├── GunnerBoardingPoint
├── GunnerDismountPoint
└── GunnerBoardingZone (trigger)
```

## Main weapon API

```csharp
BastionMT12Controller tank = GetComponent<BastionMT12Controller>();
tank.SetAimPoint(target.position);
// Fire from tank.MainMuzzle using your own projectile or raycast system.
```

## Gunner station API

```csharp
BastionGunnerStation station = GetComponentInChildren<BastionGunnerStation>();

// Snap a soldier root onto the platform. Your animation/IK system can use the exposed anchors.
station.Mount(soldier.transform);
station.SetAimPoint(target.position);
station.SetFiring(true); // spins the barrel cluster; actual damage is project-specific

// Later:
station.SetFiring(false);
station.Dismount();
```

`Mount` parents the supplied actor root to `OccupantAnchor` and resets its local pose. For a networked or physics-driven character, replace that behavior with your own possession/mount system and use the same anchors.

The station exposes:

- `OccupantAnchor`
- `LeftHandGrip` / `RightHandGrip`
- `LeftFootAnchor` / `RightFootAnchor`
- `HeadLookAnchor`
- `BoardingPoint` / `DismountPoint`
- `Muzzle`

`Meshes/Bastion_MT12_GunnerReference.obj` is a non-rigged 1.80 m scale guide. Place it under `OccupantAnchor` at local position/rotation zero to inspect alignment; it is not a game-ready soldier.

## Coordinate and scale conventions

- 1 Unity unit = 1 meter
- +Y = up
- +Z = forward / weapon direction
- Root origin = ground center
- Main turret pivot = `TurretYaw`
- Main gun elevation pivot = `TurretYaw/CannonPitch`
- Minigun pivots = `TurretYaw/GunnerStation/GunnerYaw/MinigunPitch`

## Technical profile

- LOD0 triangles: 7,668
- LOD1 triangles: 376
- LOD0 articulated model roots: 8
- Shared materials: 1
- Palette textures: 2 × 224×32 RGBA PNG
- Approximate neutral-pose bounds: 4.28 m wide × 3.81 m high × 8.46 m long
- Main collision: one BoxCollider
- Boarding detection: one trigger BoxCollider
- LOD culling included
- GPU instancing enabled on the generated material

## Quest-oriented notes

The tank uses flat-shaded low-poly geometry, one palette material, no transparency, no normal map, compact textures, a static distant LOD, and inexpensive colliders. The soldier, firing logic, projectiles, sounds, VFX, locomotion, damage, and networking remain project-specific. Profile the final simultaneous unit count on the target Quest headset.
