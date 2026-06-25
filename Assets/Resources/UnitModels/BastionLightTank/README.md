# Bastion TX-9R — Unity / Meta Quest tank asset

This is an original, low-poly RTS tank revised around a classic Cold War silhouette: a rounded cast turret, five large road wheels, a low sloped hull, curved ribbed fenders, and a long plain gun tube. It uses that broad historical visual language without reproducing a specific game vehicle, logo, faction mark, or texture.

## Import

1. Close Unity or leave it open.
2. Copy the included `Assets/BastionTankR` folder into your Unity project’s `Assets` folder.
3. Let Unity import and compile. The editor script creates `Assets/BastionTankR/Prefabs/Bastion_TX9R.prefab` automatically.
4. If the prefab is not created, run **Tools > Bastion TX-9R > Create or Rebuild Prefab**.
5. Drag `Bastion_TX9R.prefab` into a scene.

The OBJ files are Unity-native import assets. The optional GLB files in `Extras` require a glTF importer package and are not used by the prefab builder.

## Coordinate and scale conventions

- 1 Unity unit = 1 meter
- +Y = up
- +Z = forward / gun direction
- Root origin = ground-center of the vehicle
- Turret yaw pivot = `TurretYaw`
- Cannon elevation pivot = `TurretYaw/CannonPitch`

## Runtime aiming

`BastionTankRController` exposes:

```csharp
controller.SetAimPoint(targetPosition);
controller.ClearAimPoint();
controller.SnapAimAt(targetPosition);
```

The controller only articulates the turret and cannon. Connect movement, firing, audio, damage, and networking to your own gameplay systems.

## Technical profile

- LOD0 triangles: 6,328
- LOD1 triangles: 284
- LOD0 articulated model roots: 5
- Shared materials: 1
- Palette textures: 2 × 160×32 RGBA PNG
- Approximate bounds, cannon forward: 3.87 m wide × 2.83 m high × 7.47 m long
- Collision: one inexpensive BoxCollider
- LOD culling included
- GPU instancing enabled on the generated material

## Files

- `Meshes/Bastion_Hull.obj`
- `Meshes/Bastion_LeftTrack.obj`
- `Meshes/Bastion_RightTrack.obj`
- `Meshes/Bastion_Turret.obj`
- `Meshes/Bastion_Cannon.obj`
- `Meshes/Bastion_Tank_Static.obj` — one-piece quick-import model
- `Meshes/Bastion_Tank_LOD1.obj`
- `Extras/Bastion_Tank_Articulated.glb`
- `Extras/Bastion_Tank_Static.glb`

## Quest-oriented notes

The package deliberately uses flat-shaded geometry, small uncompressed palette textures, one shared material, no transparency, no normal map, a simplified collider, and a low-detail distant mesh. Validate the final unit count and target frame rate on the specific Quest headset using Unity Profiler and Meta’s performance tools.
