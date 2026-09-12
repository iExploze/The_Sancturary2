# The Sancturary v0.1 hospital base

This document records the original structural milestone. The current scene also includes the subsequent darker lighting and existing-fixture pass; see [HOSPITAL_LIGHTING.md](HOSPITAL_LIGHTING.md) for current lighting and direct Play validation.

This is the new, original structural base for v0.1. Its floorplan was authored from scratch in Blender through the portable Blender MCP environment. It does not reuse the floorplan of GrayboxPrototype, Sancturary_ExampleMap, or the hospital Demo Level. The prior Blender files and existing gameplay scenes remain available.

## Open and play

Open **Assets/Scenes/Sancturary_V01_Hospital.unity** in Unity **6000.3.15f1**. Direct Play uses the existing DebugSessionBootstrap, canonical FusionNetworkPlayer, and one-player Fusion session. Four indexed spawn points are in admissions. The existing FusionPrototypeMenu now selects this scene through its existing scene field; no session or networking code was rewritten. The scene is enabled in Build Settings.

This milestone establishes architecture, materials, working room doors, collision, navigation, and placement references. It does not implement keys, power restoration, escape logic, vent gameplay, lockers, monster patrol/AI, or prop dressing. The 20–40 minute first-success target depends on those later systems and playtesting; it is not a measured completion time for this empty base.

## Files

| Deliverable | Exact path |
| --- | --- |
| Blender source | E:\BlenderWithMCPtest\assets\Sancturary_v0_1_HospitalBase.blend |
| Staged FBX | E:\BlenderWithMCPtest\exports\sancturary_v01_hospitalbase\sancturary_v01_hospitalbase.fbx |
| Imported FBX | Assets/FBX/Sancturary_v0_1_HospitalBase.fbx |
| Unity scene | Assets/Scenes/Sancturary_V01_Hospital.unity |
| Baked navigation | Assets/Navigation/Sancturary_V01_Hospital_NavMesh.asset |
| Floorplan drawing | E:\BlenderWithMCPtest\exports\sancturary_v01_hospitalbase\floorplan.svg |
| Exact placements and dimensions | E:\BlenderWithMCPtest\exports\sancturary_v01_hospitalbase\integration.json |
| Validation evidence | E:\BlenderWithMCPtest\exports\sancturary_v01_hospitalbase\validation.json, blender_source_validation.json, unity_geometry_validation.json, unity_runtime_walk.json, unity_runtime_doors.json, unity_final_validation.json |
| Unity inspection images | E:\BlenderWithMCPtest\exports\sancturary_v01_hospitalbase\unity_lobby.png, unity_ward.png, unity_service.png |

The Blender exporter is the workspace's existing sancturary_export.py. Blender coordinates are **(-Unity X, -Unity Z, Unity Y)** for this verified export/import path. Both Blender and Unity use meters. Unity import was checked against the actual named object positions and dimensions, rather than assuming the previous staging-only axis convention was sufficient.

## Layout

The playable building envelope is approximately **64.3 × 56.3 m**, including wall thickness. There are **three zones**, **19 enterable rooms/landmark spaces**, and nine connecting hall sections. The building has an indented outline with separated patient and service wings; it is not a copy of the former four-area prototype.

| Zone | Spaces | Architectural role |
| --- | --- | --- |
| Admissions | Lobby, exit vestibule, examination west, examination east, public bathroom, administration, records | Understandable front area; optional exam-room dead ends; records is a useful future key/lock location. |
| Ward | Central nurses junction, treatment, nurses workroom, patients 01–04, isolation | Recognizable tall central landmark, searchable room cluster, open circulation loop, and a second connection through treatment/nurses spaces. |
| Service | Staff room, storage, maintenance, power room | Plainer concrete surfaces, a secondary circulation loop, a deep power destination, and useful service/vent access positions. |

The count includes the admissions lobby, central junction, and exit vestibule; it excludes the nine hallway sections. Room footprints range from approximately 6 × 4 m for the vestibule and 8 × 6 m for a patient room to 10 × 13 m for the power room and 16 × 10 m for admissions. Shared boundary walls reduce clear interior dimensions by their thickness.

**Ward loop:** central junction → ward south hall → long ward main hall → north hall → return hall → central junction. The return side enters the central junction at a different location from the outward side. Patient rooms are optional dead ends; the main loop stays open when their doors are closed.

**Service loop:** central junction → service approach → long service main hall → north service hall → return hall → central junction. The maintenance opening branches off the east side, while the power-room door is at the far northeast approach. Staff and storage connections offer additional room-scale options without gating either primary loop.

The lobby-to-power-door approach has a baked NavMesh route of approximately **55.5 m**. Restoring power later will require a meaningful return toward the front exit. The two loops remain traversable without a key; plausible future lock points are the records, isolation, storage, and power-room doors. Objective gating should be reviewed with all room connections in mind rather than assuming a single locked room door gates an entire wing.

Start points are at Unity (-3, 0, -20), (-1, 0, -20), (1, 0, -20), and (3, 0, -20). The intended exit is the front vestibule, marked at (0, 0, -26). The future power interaction is marked at (29, 0, 25). These markers have no objective logic.

## Structure and editing

The scene contains **189 individually selectable structural objects**: 28 floor slabs, 28 ceiling slabs, and 133 wall/header pieces. They are ordinary GameObjects with MeshFilters, MeshRenderers, and BoxColliders, grouped under:

```
Sancturary_v0_1_BaseMap
  Architecture
    Walls
    Floors
    Ceilings
    StructuralOpenings
  Doors
  VentMarkers
  GameplayReferences
  BasicLighting
```

The root is not a model-prefab instance. Every structural child can be moved, rotated, scaled, duplicated, or removed directly in Unity. Each child references its corresponding separated FBX mesh, preserving the authored UVs. Mesh geometry is cuboid construction with closed manifold surfaces; no destructive doorway booleans or one-sided walls are used. The FBX remains the source of the vertex/UV data, while the scene owns placement, materials, and collision. There is no runtime generator or permanent custom importer.

Walls are **0.3 m thick**. Main corridors are 4 m between wall centerlines, approximately **3.7 m clear**. Open room transitions are 3.4 m wide and 3.65 m high. Main chase-network ceilings are **3.8 m clear**; ordinary small rooms are 3.1 m; admissions is 4.2 m; the central junction is 4.5 m. Floors are **0.3 m solid slabs** with their tops at y=0. Ceilings are **0.25 m solid slabs**. Both are segmented by named spaces, without gaps between adjacent floor regions.

Door openings have segmented side walls and headers. The A/C frame openings are 1.42 × 2.37 m; B openings are 2.28 × 3.065 m to accommodate the original complete frames. The actual leaf passages are lower, about 2.2 m. No existing door or monster was rescaled.

For larger dimensional edits, note that Transform scaling changes both texture size and collision with the mesh. The authored map has consistent metric UVs; maintaining that density after large nonuniform resizes may require a UV adjustment in Blender. Ordinary Unity wall placement and resizing remain immediately practical without an importer or framework.

## Materials and lighting

Only existing project materials are assigned to the architecture:

- Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Plaster_Mat.mat — aged green/white institutional walls in admissions and the ward.
- Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Tiles_Mat.mat — existing hospital floor tiles in the public and medical spaces.
- Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Cement_Mat.mat — service surfaces and simple ceilings.

The Demo Level was inspected in Unity and its actual material assignments and dimensions sampled. Its typical structural wall is about 4.5 m high, with 4 m modules. Its tile floor uses the lower half of the existing tile atlas. The new floor UVs sample that same atlas region in metric patches instead of stretching one copy over a whole room. Plaster UVs keep the painted lower band consistent across adjacent wall segments. Cement repeats at a consistent metric scale.

Blender contains translated preview material slots and workspace copies of three existing base-color images. Unity remaps the imported slots to the original URP materials, including their existing normal/occlusion/mask setup. No Unity materials, texture artwork, downloads, custom shaders, or material library were created. Door materials remain those on the original prefabs.

Basic Unity lights provide inspection coverage and distinguish the warmer public/ward area from the cooler service wing. There are no light-fixture models, new post-processing volumes, baked-lighting pass, or final horror-lighting claims.

The reused hospital material and door sources come from the existing FPS Horror Hospital Pack, product ID 335086, version 1.0, credited in its metadata to Skyden_Games. They remain in the team's existing licensed project workflow. Vlad's source models, rigs, animations, and environment art were not changed.

## Doors and authority

Seventeen door instances reuse the **actual current-game prefabs**:

- Assets/Structures/Door_A_Frame.prefab
- Assets/Structures/Door_B_Frame.prefab
- Assets/Structures/Door_C_Frame.prefab

Their existing TransformOpenable, InteractionTarget, NetworkObject, NetworkLockGroup, AudioSource, motion references, sounds, and MeshColliders are retained. Doors are not baked into the Blender FBX. The new scene's door instances override only `needsKey=false` so all rooms are available for architectural playtesting without adding key items or objectives. The original lock components/key identifiers and prefab assets remain intact for later progression wiring.

Door open/lock state continues through the existing Fusion authority and interaction flow. Structural meshes are static shared scene content. Local input, prompts, camera, inventory presentation, and pause UI use the existing systems. No gameplay network payload or AI code changed.

## Geo and navigation

The current Geo prefab has a NavMeshAgent height of **3.3 m**, radius **0.38 m**, and base offset **-0.35 m**. Its capsule collider is approximately **3.068 m** tall, centered near y=1.78. The project's existing agent-type-0 bake settings instead specify height **2 m** and radius **0.5 m**. This is a real configuration mismatch; the default bake alone is not proof of Geo clearance.

The map keeps the main chase circulation independent of the small room doors. A scene NavMeshSurface uses the existing type-0 workflow, physics-collider sources, and a 0.1 m voxel size. Standard NavMeshModifier volumes mark the low door thresholds Not Walkable for AI. Low-ceiling small-room floors are also marked Not Walkable, and structural wall/ceiling tops are excluded from walkable areas. These are navigation modifiers only; they do not block the player. They remain in the scene so an ordinary rebake preserves the restriction without a custom importer.

Both major loops and the route from admissions to the **power-door approach** have complete baked paths. A separate 3.35 m tall, 0.38 m radius capsule clearance sweep passed along the main routes. This does not claim that Geo can enter through the smaller patient/power doors, or that its full animated pursuit behavior has been tested. No Geo model or AI was placed in the final scene; only a named reference spawn marker is provided. Actual patrol, chase pressure, room safety, and target behavior remain a later integration/playtest responsibility.

## Future vents

Five empty markers reserve spatially separated access positions: Records, Treatment, Ward/Isolation, Storage, and Maintenance. The locations favor service-side walls and the upper-room perimeter. Future trunk connections can run from maintenance/storage toward treatment and the ward, with a separate administrative branch. No visible vent assets or gameplay components were added.

The existing NetworkVentEntrance/NetworkVentExit convention uses paired entrances/exits and multiple arrival anchors for shared players. These markers leave room for that approach; they do not promise a walkable duct network or replace its authority checks. Route the future interior space with sufficient crouched clearance, using the individually editable ceilings and the service space above/alongside the corridors. The current ceiling slabs fully enclose the playable building.

## Validation and remaining playtests

Blender source saving and reopening, closed manifold solid checks, axis/scale checking, and fresh FBX reimport passed. Unity 6000.3.15f1 imported the map and original materials successfully. Scene validation reported no missing scripts or broken prefabs; material validation found no missing or pink/error-shader references. Requested project compilation completed with no C# compiler errors. The initial Console had stale missing-script messages before this map; those were not introduced by this scene. After the runtime tests, the MCP transport logged one WebSocket reconnect warning; this was tooling output, not a game compilation or runtime failure.

Editor physics checks passed at **651 standing-player doorway samples**, **750 conservative Geo route samples**, and **1,401 floor-support samples**. These used actual scene colliders, with original door motion targets temporarily placed in their open poses and then restored. Both circulation loops returned complete NavMesh paths on every tested segment. Player-eye-height views were inspected in admissions, the ward, and service area.

The existing Fusion player completed a **300.2 m runtime walk**, reaching all **21 route waypoints** in approximately **125 seconds**, through the ordinary Input System/Fusion movement path. The test covered both loops, the power approach, and return to admissions. A first automation attempt stopped when input auto-switched to keyboard/mouse; rerunning with the synthetic gamepad held as the temporary runtime control scheme completed successfully. This changed no player prefab or input settings on disk. Door-interaction results are recorded in the evidence files above.

All **17 doors** passed opening, normal player traversal, and closing in the one-player Fusion host session. Requests went through the existing queued interaction path; closing was tested while looking at an actual open leaf, rather than through the empty doorway. No warnings or errors were reported during the controlled runtime checks. After Play Mode, the final manifold mesh export was imported, all mesh/BoxCollider dimensions rechecked, and the scene saved and reopened.

A two-instance host/client test and subjective movement/chase review are still required. No objective completion, first-run duration, AI pursuit, or final atmosphere claim is made for this structural milestone.

Manual acceptance:

1. Open Sancturary_V01_Hospital, wait for imports, and enter Play. Expect the existing Fusion player in admissions, correct floor height, and readable existing materials. Walk both complete loops in both directions; visit each room; jump/crouch at corners and headers. Expect no floor gaps or structural blockers on the main routes.
2. Approach every room door, press F to open, cross, turn around, and close it. All are initially unlocked for this pass. Verify both leaves of the B doors, collider alignment, sounds, prompts, and clear movement through openings. Enable later locks only together with the corresponding key/objective placement.
3. Start from FusionPrototypeMenu in host and client instances with identical new content. Both ready in the existing lobby, then start. Expect both players to load this hospital and use distinct spawn points. Have each player open/close the same doors while the other observes. Join later with a door open and verify its current replicated state. Check disconnect/return-to-menu through the existing pause flow.
4. For subsequent Geo integration, retain its prefab size/settings, place it only on the open chase network, wire reviewed patrol markers, and rebake using the existing NavMeshSurface. Verify the 3.8 m corridors, both loops, maintenance opening, and power-door approach from host and client perspectives. Low-door rooms are intentionally outside that circulation until a separate design decision addresses the mismatch.
5. Select, move, resize, duplicate, and undo one wall in the Unity scene. Its BoxCollider should track the transform. The other walls and original FBX source should not be changed. Keep the new scene saved after intended edits and rebake navigation after structural edits.

No prop dressing or new decorative environment assets were created. The only placed non-structural 3D game assets are the original functional door prefabs. Existing materials/textures are reused throughout. This map is intended to be developed further as **The Sancturary v0.1 structural base**.

The current `map-modifications` checkout was retained. No branch or pull request was created. The only existing tracked files intentionally modified are FusionPrototypeMenu's two destination references and EditorBuildSettings' new enabled scene entry. The new map files, navigation data, and this report are left in the working tree. Temporary authoring code was removed; no custom Editor tool or runtime generator remains.
