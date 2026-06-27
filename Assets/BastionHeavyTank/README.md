# Bastion HT-77 Mammoth — Unity / Meta Quest heavy tank asset

This package contains an original low-poly RTS super-heavy tank with twin main cannons and an articulated eight-tube missile attachment. It follows the same Bastion asset conventions as the earlier light and medium tanks: OBJ source meshes imported directly by Unity, a generated prefab, one shared palette material, a static distant LOD, simple collision, and runtime articulation scripts.

The design uses broad classic base-building RTS conventions. It is not an exact copy of a specific Command & Conquer vehicle and contains no copied meshes, textures, logos, or faction marks.

## Import

1. Copy the included `Assets/BastionHeavyTank` folder into the root of your Unity project.
2. Let Unity finish importing and compiling.
3. The editor script creates `Assets/BastionHeavyTank/Prefabs/Bastion_HT77_Mammoth.prefab` automatically.
4. If needed, run **Tools > Bastion HT-77 Mammoth > Create or Rebuild Prefab**.
5. Drag the prefab into your scene.

The optional GLB files in `Extras` require a glTF importer. Unity uses the included OBJ files directly.

## Prefab hierarchy

```text
Bastion_HT77_Mammoth
├── Hull
├── LeftTrack
├── RightTrack
├── TurretYaw
│   ├── TurretModel
│   ├── CannonPitch
│   │   ├── TwinCannons
│   │   ├── LeftCannonMuzzle
│   │   └── RightCannonMuzzle
│   └── MissileYaw
│       ├── MissileBase
│       └── MissilePitch
│           ├── MissilePod
│           └── MissileMuzzle_01 ... MissileMuzzle_08
└── LOD1
```

## Runtime aiming API

```csharp
BastionHT77Controller tank = GetComponent<BastionHT77Controller>();

// Aim both weapon systems at the same target.
tank.SetAimPoint(target.position);

// Or assign separate targets.
tank.SetMainAimPoint(armorTarget.position);
tank.SetMissileAimPoint(airOrVehicleTarget.position);

// Spawn projectiles from the exposed muzzle transforms.
Transform leftGun = tank.LeftCannonMuzzle;
Transform rightGun = tank.RightCannonMuzzle;
Transform firstMissile = tank.MissileMuzzles[0];
```

The component handles only articulation. Projectile spawning, salvos, recoil, muzzle flashes, damage, sound, locomotion, networking, and target selection remain project-specific.

## Coordinate and scale conventions

- 1 Unity unit = 1 meter
- +Y = up
- +Z = forward / weapon direction
- Root origin = ground center
- Twin cannons share one elevation pivot
- Missile attachment has independent yaw and pitch pivots

## Technical profile

- LOD0 triangles: 9,060
- LOD1 triangles: 600
- Articulated LOD0 renderer roots: 7
- Shared materials: 1
- Palette textures: 2 × 224×32 RGBA PNG
- Approximate neutral-pose bounds: 5.27 m wide × 4.36 m high × 10.45 m long
- Main collision: one BoxCollider
- LOD culling included
- GPU instancing enabled on the generated material

## Quest-oriented notes

The asset uses flat-shaded low-poly geometry, one compact palette material, no transparency, no normal map, a simplified LOD1, and an inexpensive collider. Profile the final scene and simultaneous vehicle count on the target Quest headset.
