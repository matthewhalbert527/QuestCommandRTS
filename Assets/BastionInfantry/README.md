# Bastion Infantry Smooth Pack — Unity / Meta Quest RTS infantry

This package contains five original rounded low-poly soldier prefabs designed for a readable command-and-conquer-style RTS camera while remaining suitable for standalone Meta Quest budgets. The soldiers share one palette material and use articulated transform hierarchies rather than a skinned skeletal rig.

## Import

1. Copy `Assets/BastionInfantry` into your Unity project's `Assets` folder.
2. Let Unity finish importing and compiling.
3. The editor script automatically creates five prefabs in `Assets/BastionInfantry/Prefabs`.
4. If automatic creation does not run, use **Tools > Bastion Infantry > Create or Rebuild All Soldier Prefabs**.

Generated prefabs:

- `Bastion_Gunner.prefab`
- `Bastion_Grenadier.prefab`
- `Bastion_RocketSoldier.prefab`
- `Bastion_FlameTrooper.prefab`
- `Bastion_Engineer.prefab`

## Role defaults

| Role | Delivery | Health | Move speed | Range | Damage / tick |
|---|---:|---:|---:|---:|---:|
| Gunner | Hitscan | 100 | 3.6 | 19 | 8 |
| Grenadier | LobbedProjectile | 105 | 3.25 | 15 | 52 |
| Rocket Soldier | Rocket | 115 | 2.95 | 27 | 120 |
| Flame Trooper | FlameStream | 145 | 3 | 8.5 | 6 |
| Engineer | RepairTool | 90 | 3.45 | 5.5 | 5 |

The values are serialized defaults intended as integration scaffolding, not final balance.

## Runtime API

Each prefab includes `BastionInfantryUnit` and `BastionInfantryProceduralAnimator`.

- `SetAimPoint(worldPosition)` aims the upper body and weapon.
- `SetMovementAmount(0..1)` drives lightweight procedural leg swing and pelvis bob.
- `TryPrimaryAction()` enforces the prefab cooldown, triggers recoil, and invokes the primary action UnityEvent.
- `ApplyDamage`, `RestoreHealth`, and `ResetHealth` provide a minimal health hook.
- The engineer exposes `IsEngineer` and `UtilityPower` for repair-system integration.

Projectile spawning, hitscan resolution, flame volumes, repair targeting, audio, VFX, navigation, and network authority remain intentionally project-owned. Connect those systems to the UnityEvents or the public properties.

## Hierarchy

```text
Bastion_<Role>
├── LOD0_Rig
│   └── PelvisPivot
│       ├── PelvisModel
│       ├── LeftLegPivot / LeftLegModel
│       ├── RightLegPivot / RightLegModel
│       └── UpperBodyYaw
│           ├── TorsoModel
│           ├── GearModel
│           ├── HeadYaw / HeadModel
│           └── WeaponPitch
│               ├── ArmsModel
│               └── WeaponRecoil
│                   ├── WeaponModel
│                   └── Muzzle or ToolTip
├── LOD1
├── HealthBarAnchor
├── SelectionAnchor
└── AimCenter
```

## Technical summary

- Coordinates: meters, Y-up, Z-forward
- Shared palette material: 1
- LOD0: articulated transform parts
- LOD1: rounded medium-distance silhouette
- LOD2: aggressively simplified distant silhouette
- Capsule collider on each prefab
- Optional static GLB files in `Extras`
- Static OBJ versions included for quick import
- Combined LOD0 triangle count: 24896
- Combined LOD1 triangle count: 3980
- Combined LOD2 triangle count: 1588

## Quest notes

The shared material supports GPU instancing. Actual Quest performance depends on unit count, shadows, overdraw, VFX, AI, navigation, and the rest of the scene. Profile the complete encounter on the target headset before shipping.

## Smooth revision

The revised models use tapered limbs, rounded body masses, smoother imported normals, and three LOD stages while retaining one shared palette material. The texture footprint remains deliberately tiny; the smoother appearance comes primarily from mesh topology and normals rather than additional materials or large textures.
