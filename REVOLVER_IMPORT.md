# Sancturary revolver art import

Imports the existing original Blender MCP revolver and reusable cartridge as additional visual assets. Existing OldRevolver inventory models, gameplay prefabs, definitions, scenes, and Fusion registration are unchanged.

## Assets

- `Assets/Inventory/Generated/Models/Sancturary_OldRevolver.fbx`: 46 meshes, 32,196 triangles including six loaded cartridges; bounds 0.284965 x 0.185257 x 0.068200 metres in Unity.
- `Assets/Inventory/Generated/Models/Sancturary_RevolverRound.fbx`: three meshes, 2,240 triangles; length 0.050650 metres.
- `Assets/Inventory/Prefabs/Sancturary_OldRevolver_Visual.prefab` and `Sancturary_RevolverRound_Visual.prefab`: unit-scale visual wrappers with muzzle/projectile facing local +Z. The source FBXs face -X; the child rotation converts this without changing source pivots.
- `Assets/Inventory/Generated/Materials/SancturaryRevolver`: five URP/Lit materials for Metal, Wood, Brass, Bullet, and Recess.
- `Assets/Inventory/Generated/Textures/SancturaryRevolver`: source base-color maps and converted metallic/smoothness maps. Metallic is in red; alpha is one minus source roughness. Packed maps use linear sampling.

These are art-only prefabs: no colliders, pickup logic, shooting, animation, or network components. No new authoritative or replicated state is introduced. Future movable/pickup integration must use the existing Fusion inventory flow.

## Source and limitations

Source: `E:\BlenderWithMCPtest\assets\Sancturary_OldRevolver.blend`.
Exports: `E:\BlenderWithMCPtest\exports\sancturary_oldrevolver\Sancturary_OldRevolver.fbx` and `E:\BlenderWithMCPtest\exports\sancturary_revolverround\Sancturary_RevolverRound.fbx`.
The existing locally authored Blender MCP asset was reused; no third-party model was downloaded. Source and inspection renders remain outside Unity Assets.

UVs and image textures are included. This import does not add normal maps or rebake the source. Separate frame, barrel, cylinder, crane, ejector, hammer, trigger, and grip meshes survive import. Cylinder, crane, hammer, and trigger origins match the source after axis conversion. Pivots are retained, but mechanical parenting/animation still needs to be authored; this is not a working reload rig.

## Validation

- Connected to the existing Blender scene through the supplied MCP bridge and inspected the existing source/export reports and render.
- Existing Blender scene and FBX roundtrip reports pass. Weapon FBX and referenced texture hashes match the manifest.
- Imported and serialized through Unity MCP in Unity 6000.3.15f1; no temporary Editor script was required.
- Verified both model counts, unit scale, weapon bounds, key pivot positions, URP shaders, and base/packed texture references in the Editor.
- Inspected a Unity preview render. Preview lighting is limited; final material appearance requires inspection under game lighting.
- No Console errors observed; an unrelated HenryJumpscare video color-primaries warning was present. Active scene remained clean.
- Play Mode, builds, and host/client tests were not run. No multiplayer functionality is claimed by this art import.

## Manual checks

1. Open each Visual prefab in Prefab Mode. Confirm materials render without pink/missing textures and the revolver has six loaded rounds.
2. In a disposable scene with a camera and directional light, place each prefab at scale 1. Confirm the revolver is about 28.5 cm long, the round is about 5.1 cm, and blue +Z points toward the muzzle/projectile. Inspect both sides, grip, muzzle, and cylinder at close range under representative lighting. Do not save changes to gameplay scenes.
3. For a solo smoke check, run the existing Fusion menu and start a one-player session; existing inventory/gameplay should remain unchanged.
4. For host/client regression checks, launch one host and one client using the existing session flow, join the same session, and check current item pickup/equipment from both perspectives. These new visual prefabs are intentionally not registered as usable items, so no new revolver pickup is expected.
