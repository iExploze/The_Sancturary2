# Reference-inspired revolver replacement

The existing Old Revolver now uses an original Blender model inspired by Ian's reference: a long tapered barrel, curved walnut grip, rounded recoil shield, fluted cylinder, slim guard, hammer spur, and slotted screws. The ammunition pickup shows six matching brass cartridges with lead projectiles. A revolver uses cartridges rather than a detachable magazine; one existing ammo item still refills the six-shot cylinder.

## Integration

- New models: `Assets/Inventory/Generated/Models/FrontierRevolver.fbx` and `FrontierRound.fbx`.
- New URP materials/textures: `Assets/Inventory/Generated/Materials/FrontierRevolver` and `Assets/Inventory/Generated/Textures/FrontierRevolver`. Five materials, with base color and linear metallic/smoothness maps.
- Updated existing `HeldOldRevolver`, `HeldOldRevolverRemote`, `WorldOldRevolver`, and `WorldRevolverAmmo` prefabs under `Assets/Inventory/Prefabs`.
- Held meshes face +Z at unit scale, aligned to the existing hand anchor. Both muzzle effect origins sit 2 mm ahead of the new barrel tip. Existing recoil/equip presentation remains in place.
- World weapon lies on its side with three compound box colliders around barrel, frame, and grip. Updated physics bounds and both physical/pickup collider arrays. The six-cartridge pickup retains its box collider with matching bounds.
- Removed the previous Sancturary visual imports and their standalone visual prefabs, plus the older OldRevolver/RevolverAmmo FBXs, simplified OBJ/MTL files, materials, and textures. No serialized references to deleted GUIDs remain in Assets.

Gameplay prefab GUIDs, item definition references, item IDs, audio, capacity, and Fusion registration are preserved. Existing state authority still owns pickups, drops, ownership, firing, and ammunition; this revision changes presentation and collision geometry, not networking code. Owner and remote presentations both use the new model.

## Source

- Blender: `E:\BlenderWithMCPtest\assets\Frontier_Revolver.blend`.
- Weapon export: `E:\BlenderWithMCPtest\exports\frontier_revolver\frontier_revolver.fbx`.
- Cartridge export: `E:\BlenderWithMCPtest\exports\frontier_round\frontier_round.fbx`.
- Blender inspection renders/reports: `E:\BlenderWithMCPtest\exports\frontier_revolver`.
- Unity preview renders/report: `E:\BlenderWithMCPtest\logs\HeldOldRevolverRemote-frontier.png`, `WorldRevolverAmmo-frontier.png`, and `frontier-unity-validation.txt`.

Geometry was authored through the supplied Blender MCP installation, without downloading a weapon model. Weapon: 25,948 triangles across 24 meshes, 328 x 48 x 157 mm in Blender. Single cartridge: 2,140 triangles, 41.2 mm long. UVs and image textures are included; no normal maps or reload animation were added. Cylinder, hammer, and trigger retain separate meshes and pivots. The reference has a fixed-cylinder silhouette; a swing-out crane is not included. The gameplay export has no permanently visible loaded rounds, so it does not display six cartridges when authoritative ammo is empty.

## Verification and manual playtest

Blender inspection found no non-manifold edges in the exported meshes. Both exports passed the supplied fresh-process FBX validator (hashes, names, UVs, material slots, dimensions, and orientation). Inspected side, three-quarter, and rear Blender renders and Unity prefab previews.

Unity 6000.3.15f1 MCP validation confirms all four prefabs have valid meshes/materials/textures, no missing scripts, valid pickup/physics references, and retained required gameplay/Fusion components. Both muzzle positions were checked numerically. Final Console query returned no errors or warnings. No gameplay scenes were saved. Unrelated dirty editor state and ProBuilder settings were left outside this change.

Play Mode, build, and two-instance networking tests were not run. Required manual checks:

1. Open `HeldOldRevolver`, `HeldOldRevolverRemote`, `WorldOldRevolver`, and `WorldRevolverAmmo` in Prefab Mode. Check silhouette, material appearance under game lighting, hand fit, and six-round pickup layout.
2. Start a one-player Fusion session using the existing menu/graybox flow. Pick up the Old Revolver and ammo. Equip it, reload through the inventory, and fire. Expect the new first-person model, existing six-round reload behaviour, and muzzle flash at the new barrel tip. Check camera clipping and aim with the longer barrel.
3. Drop the weapon and ammo on a flat floor and beside a wall. Expect the new world models to settle without disappearing or passing through the surface. Pick them up again; ammunition state should follow the existing rules.
4. Run one host and one client in the same session. Have each player equip and fire while the other observes: both should see the new remote model and muzzle effect. Drop and exchange the weapon/ammo in both directions; each world object should disappear once on pickup and never be claimed twice.
5. Join a client after a weapon has been dropped. Verify the current world model and pickup state are correct. This remains a manual network check, not a claim of runtime verification.
