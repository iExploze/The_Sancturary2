# Reference map analysis

## Contents

- [Evidence and limits](#evidence-and-limits)
- [Reference hierarchy](#reference-hierarchy)
- [Visual language](#visual-language)
- [Composition and prop placement](#composition-and-prop-placement)
- [Materials and surface language](#materials-and-surface-language)
- [Lighting, reflections, and post-processing](#lighting-reflections-and-post-processing)
- [Prefab-use evidence](#prefab-use-evidence)
- [Current playable-map protection notes](#current-playable-map-protection-notes)
- [Transfer versus imitation](#transfer-versus-imitation)

## Evidence and limits

This analysis was made from:

- `Assets/FPS Horror Hospital Pack/Scenes/FPS Horror Hospital Environment Scene.unity`
- `Assets/FPS Horror Hospital Pack/Scenes/FPS Horror Models Scene.unity`
- all 140 prefabs under `Assets/FPS Horror Hospital Pack/Art/Prefabs/`
- all four FBX imports under `Assets/FPS Horror Hospital Pack/Art/Models/`
- all ten materials and their texture sets under `Assets/FPS Horror Hospital Pack/Art/Textures/`
- `Assets/FPS Horror Hospital Pack/Lighting/FPS Horror Hospital Lighting.lighting`
- `Assets/FPS Horror Hospital Pack/Volume/FPS Horror Hospital  Volume.asset`
- the pack URP assets under `Assets/FPS Horror Hospital Pack/URP Files/`
- a live, read-only Unity 6000.3.15f1 additive load of the reference scene, including hierarchy, component, Game view, Scene view, Volume, light, reflection-probe, pipeline, and Console inspection
- a live, read-only inspection of `Assets/Scenes/GrayboxPrototype.unity`

The reference scene and `GrayboxPrototype` hashes were unchanged after live inspection. No screenshot asset was saved into the project.

Limitations:

- The reference hierarchy has no semantic room groups. Room interpretations are based on spatial composition, visible context, and prop function rather than authoritative room labels.
- The scene references a baked `LightingDataAsset` with GUID `eaf1fcba6c16b4548a7250b88cc501e2`, but no matching `.meta` file exists in the repository. The baked lightmap payload could not be inspected.
- No pack-specific licence or documentation file was found. Use the purchased assets only in place; do not copy or redistribute source files.
- The live reference was loaded additively beside Ian's already-dirty `GrayboxPrototype`; per-scene hierarchy and serialized settings were checked separately to avoid confusing cross-scene objects.

## Reference hierarchy

The reference environment contains five scene roots:

| Root | Observed contents |
|---|---|
| `Volume` | One global URP Volume using `Assets/FPS Horror Hospital Pack/Volume/FPS Horror Hospital  Volume.asset`. |
| `Camera` | One presentation camera with post-processing enabled. |
| `Lights` | Fifteen spotlights. |
| `Reflections` | Twenty baked reflection probes sized to rooms, corridor runs, and stair volumes. |
| `Horror Hospital Models` | 1,421 direct child instances with a deliberately flat asset-demo hierarchy. |

Do not copy the flat hierarchy into production. Use the removable `ENV_Dressing_<ZoneName>` hierarchy defined in `SKILL.md`.

The environment reads as an L-shaped institutional corridor system with attached rooms and a multi-level stair segment. Architectural modules, long pipe systems, and repeating wall bays establish rhythm; local furniture clusters identify room function.

## Visual language

### Palette and contrast

- Use desaturated institutional green as a lower-wall band against dirty off-white or grey plaster.
- Use near-black recesses, dark metal trim, and stained cement to frame the playable route.
- Reserve saturated accents for semantic objects: red extinguishers, hazard labels, exit signs, indicator lights, and cool or warm light pools.
- Keep overall saturation low. Let material roughness, reflected highlights, and localized color temperature provide variation.

### Architectural rhythm

- Repeat arches, columns, wall bays, windows, radiators, lamps, and overhead pipe segments to create institutional regularity.
- Break the cadence at intersections, stairs, special rooms, and story anchors rather than at every bay.
- Keep long corridor silhouettes legible. Repetition should guide distance perception, not form a solid wall of visual noise.
- Treat the green/off-white wall split, metal edge trim, and exposed services as the primary identity. Furniture is secondary.

### Horror tone

- Build unease through abandonment and functional decay rather than indiscriminate gore.
- Use darkness to reveal fragments: a reflective chair edge, a lamp face, a red extinguisher, a sign, or a doorway plane.
- Let empty space and dark depth carry tension. Avoid filling the route with debris.
- Keep recognizable institutional function under the decay: patient care, administration, maintenance, sanitation, and storage.

## Composition and prop placement

### Large, medium, small order

1. Establish one large anchor such as a bed, desk, chair bank, shelf, folding curtain, or treatment table.
2. Add one to three medium supports such as a bedside cabinet, radiator, file cabinet, medical lamp, serum holder, wall fixture, or single chair.
3. Add a restrained small cluster such as medical glass, serum, a bowl, a cable, a socket, or a sign.

The reference does not scatter small medical objects evenly across floors. Glass, bowls, and serum objects cluster on plausible work surfaces or beside treatment equipment.

### Alignment and disorder

- Keep beds, desks, cabinets, shelves, chair banks, radiators, and wall fixtures aligned to walls and architectural axes.
- Use modest rotation offsets to break perfect repetition.
- Use stronger rotation variation on displaced single chairs, folding curtains, mobile medical tables, bowls, and small treatment props.
- Keep cables and pipes attached to a surface, supported by holders and connectors, and composed as continuous runs.
- Avoid unsupported floating fragments, implausible intersections, and purely random rotation.

### Density and negative space

- Concentrate detail at room edges, alcoves, intersections, and focal pockets.
- Preserve a low-detail navigation channel through pursuit spaces.
- Use dense pipe networks overhead or against walls, not through head clearance or the centre of a chase route.
- Leave approach space around doors, signs, switches, puzzle objects, and room-defining anchors.
- Keep puzzle-critical rooms sparser than purely exploratory rooms.

### Repetition avoidance

- Alternate `Lamp_A`, `Lamp_B_01`, and `Lamp_B_02` by function and mounting context.
- Mix single `Chair` instances with the `Chairs` bank; do not repeat identical chair banks at every corridor interval.
- Select signs by actual room meaning. Do not use signs as generic texture noise.
- Vary pipe diameter, line count, connectors, holders, and run length while keeping the system physically coherent.
- Use the medical-glass variants as a small family, not all variants in every room.

## Materials and surface language

The FBX imports map to shared URP/Lit materials. Keep these material assignments intact:

| Material path | Visual role | Restriction |
|---|---|---|
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Cement_Mat.mat` | Dark, rough structural cement. | Do not edit or duplicate for a one-room pass. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Plaster_Mat.mat` | Dirty painted/plaster wall fields. | Preserve the existing green/off-white language; do not recolor the shared material. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Tiles_Mat.mat` | Worn institutional floor/wall tile. | Keep UV scale and source assignment. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Trim_01_Mat.mat` | Primary architectural trim. | Use through existing prefab assignment only. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Trim_02_Mat.mat` | Secondary architectural/prop trim. | Use through existing prefab assignment only. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Trim_03_Mat.mat` | Dark metal, pipes, and service trim. | Use through existing prefab assignment only. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Props_Mat.mat` | Shared worn medical and utility prop atlas. | Do not create per-instance copies. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Props_Emissive_Mat.mat` | Fixture indicators and emissive prop details; serialized emission is approximately 4 per RGB channel. | Pair with motivated lighting; do not increase the shared emission. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Props_Glass_Mat.mat` | Transparent medical-prop glass. | Limit overlap and overdraw; keep off movement paths. |
| `Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_WindowGlass_Mat.mat` | Grey transparent window glazing. | Do not use as a generic decal or dirt overlay. |

The cement, plaster, tile, trim, and prop families have base-color, normal, mask, and ambient-occlusion textures where applicable. Damage and grime are primarily baked into those texture sets. Do not layer arbitrary decals on every surface.

No dedicated decal prefabs were found in the pack. Treat decals from elsewhere as unverified until their style, ownership, licence, material, projection volume, and gameplay readability are inspected.

## Lighting, reflections, and post-processing

### Spotlights

All fifteen reference lights are spotlights with approximately:

- intensity `8`
- range `10`
- outer spot angle `142.84°`
- inner spot angle `42.43°`
- soft shadows at strength `0.95`
- mixed bake type

Most are white. Five are warm cream at roughly RGB `(1.0, 0.87, 0.706)`, and one is cool blue at roughly `(0.665, 0.784, 1.0)`. Ten point down; five are angled laterally. The transferable pattern is mixed pools and motivated direction, not these exact values.

When the reference was viewed under the project's current `Assets/Settings/PC_RPAsset.asset`, Unity warned that additional punctual-light shadow resolution was reduced to fit fifteen shadow maps in the 2048 atlas. Do not reproduce the full light count. Budget shadow casters per zone.

### Reflection probes

The twenty observed probes are baked, resolution `512`, intensity `1`, and blend distance `5`. Their boxes are sized to individual rooms, corridor segments, and vertical stair volumes. Box projection is off in the reference.

Transfer the room-sized coverage idea. Do not copy probe counts, transforms, or resolution blindly. Check overlap, reflective need, memory, and the target map's existing probes.

### Lighting settings

`Assets/FPS Horror Hospital Pack/Lighting/FPS Horror Hospital Lighting.lighting` uses:

- baked GI on; realtime GI off
- Progressive GPU lightmapper
- lightmap resolution `20`, max size `1024`, padding `2`
- direct samples `32`, indirect samples `512`, environment samples `256`
- two bounces and minimum two bounces
- ambient occlusion on with max distance `1`
- indirect output scale `1.3`
- high-quality lightmap compression

Treat this as reference evidence only. Do not replace the target scene's LightingSettings or rebake the entire project during a local dressing pass without explicit approval.

### Environment settings

The reference scene serializes no skybox and no sun. Ambient sky is black with dark grey equator/ground values. Linear grey fog is enabled from `-5` to `400`; at interior distances it mainly reinforces the closed environment rather than creating a thick visible layer.

Do not copy these global settings into the playable map. Preserve the map's lighting design and add only zone-scoped improvements.

### Volume profile

The global profile at `Assets/FPS Horror Hospital Pack/Volume/FPS Horror Hospital  Volume.asset` demonstrates:

- tonemapping mode `1`
- bloom threshold `0.8`, intensity `1`, scatter `0.7`
- vignette intensity `0.3`, smoothness `0.3`
- chromatic aberration `0.2`
- white balance temperature `6`, tint `-8`
- Panini projection distance `0.1`
- Gaussian depth of field from `10` to `30`, radius `1`
- shadow lift of about `-0.228`

Film grain, motion blur, lens distortion, color lookup contribution, and lens-flare intensity are present but set to zero. Do not infer that every listed effect is visually active. In a movement-heavy first-person game, preserve comfort and interactable readability before matching the reference grade.

## Prefab-use evidence

The reference uses 129 of the pack's 140 prefab types. High-frequency structural/service motifs include:

- `Pipe_Med_Con_03` (91 instances)
- `Pipe_Med_Con_01` (79)
- `Pipe_A_Large_4m` (70)
- `Pipe_Large_Con_01` (63)
- `Pipe_B_Med_2m` (52)
- `Pipe_A_Med_4m` (51)
- `Roof_A_4x4m` (42)
- `Floor_A_4x4m` and `Pipe_Large_Con_03` (41 each)

Furniture and readable prop counts are much lower:

- heaters `34`
- wall lamp A `25`
- corridor arches `21`
- switches `19`
- chair banks and lamp B_02 `17` each
- beds `12`
- shelves `10`
- single chairs `9`
- folding curtains and medical bedside cabinets `8` each
- file cabinets and fire-extinguisher holders `7` each
- desks, electric boxes, ladders, and mirrors `5` each

Use these ratios qualitatively: continuous architectural/service systems, moderate wall fixtures, and sparse large story props. Do not copy instance counts into the smaller playable prototype.

Every inspected hospital-pack prefab contains at least one `MeshCollider`. Multi-part prefabs include two colliders for the aid-kit box, extinguisher holder, toilet, and toilet-door base; six for the medical bedside; eleven for the file cabinet. Collider cost and path obstruction must be checked on every new instance.

## Current playable-map protection notes

The live `Assets/Scenes/GrayboxPrototype.unity` hierarchy was already dirty before inspection. Re-inspect it every time; the following is only a protection snapshot, not a layout specification.

Observed layout shell:

- `GrayboxEnvironment` with `Floor`, exterior walls, centre walls, room walls, three large obstacles, and `AirborneColliderTestObstacle`

Observed protected gameplay/navigation roots include:

- `FusionSpawn_02`, `FusionSpawn_03`, `FusionSpawn_04`
- `MonsterSpawnPoint`, `MonsterSpawner`
- `NavigationSurface`
- `GeoMonsterPrototype`, `GeoMonsterPatrolWaypoints`
- `Door_A_Frame`, `Door_B_Frame`, `Door_C_Frame`, `Door_D_Frame`
- `Cabinet Key Pickup`
- `Inventory Prototype Items`
- five `Locker_Prefab_1` instances
- `Switch`
- `Medical Bedside`, `File Cabinet`, and `Aid Kit Box` gameplay variants

Several visible hospital props have been extended with Fusion, interaction, lock, or openable components. A matching-looking static pack prefab is not interchangeable with a gameplay instance. Never duplicate an interactable-looking asset near a puzzle or pickup without checking the target scene and obtaining direction.

## Transfer versus imitation

Transfer:

- green/off-white institutional banding
- stained PBR surfaces and dark service trim
- repeated arches and coherent exposed-service runs
- localized light pools, strong depth falloff, and restrained warm/cool accents
- furniture against architecture with controlled local disorder
- dense detail at edges and focal pockets, clear negative space in routes
- room-sized reflection coverage where reflective materials justify it

Do not imitate directly:

- the reference's structural layout, room dimensions, stair system, or route
- its flat 1,421-object hierarchy
- its exact pipe density or light count
- its global Volume, LightingSettings, fog, URP assets, or camera effects
- structural wall, floor, roof, door-frame, stair, column, or arch modules in a dressing-only pass
- static lookalikes of the playable map's doors, keys, switches, cabinets, aid kits, or other interactables
