## Reference Playable refinement (current)

The current lighting reference is **Assets/Scenes/Reference Playable.unity**. The earlier Demo Level pass below is historical; this section supersedes its environment and post-processing values.

Completed through the running Unity MCP server in Unity 6000.3.15f1:

- Added **35 individual reflection probes** under `Sancturary_v0_1_BaseMap/Lighting/ReflectionProbes`, covering all 28 map areas. Seven long, narrow corridors have two probes each. No map-wide probe exists.
- Each probe has its own baked 512-pixel HDR cubemap in `Assets/Scenes/Sancturary_V01_Hospital/`. Probes use **Custom mode with an explicitly assigned baked texture**, so their assignments survive scene reloads without relying on an unsaved lighting-data mapping. These are offline captures, not continuously rendered probes.
- Probe bounds follow the room/corridor footprint and ceiling height. Box projection is enabled and blending is limited to 0.5 m, adapting Reference Playable's local placement pattern to this map's enclosed rooms. Reference Playable itself has 20 baked probes at 512 pixels. Architecture has the Reflection Probe Static flag; geometry, colliders, material assignments and transforms remain intact.
- Matched the reference's null skybox/black sky ambient, Skybox ambient mode, ambient intensity 1, linear gray fog (start -5 m, end 400 m). Suppressed the default environment reflection with scene reflection intensity 0 because it introduced a blue wash; local probes retain intensity 1.
- Reused the exact existing `Assets/FPS Horror Hospital Pack/Volume/FPS Horror Hospital  Volume.asset` profile without modifying it. Its Neutral tone mapping, bloom (1, threshold 0.8), white balance (temperature 6, tint -8), shadow grading, chromatic aberration, Panini projection and distance depth of field apply through the new global Volume. No separate LUT is needed: the reference's Color Lookup contribution is zero.
- The scene Volume uses the existing OwnerPostProcessing layer (8), priority 0, so the canonical player camera sees it through its existing mask. The owner's runtime damage/stamina vignette remains at priority 100 and overrides the profile vignette. The disabled inspection camera is also configured to preview the profile. No player or networking script changed.
- Retained 25 working lights and 10 dead fixtures. Lowered the two admissions sources from 6.5/4 to 4.5/2.6, Public Spine from 5.5 to 4.3, the central junction from 8 to 5.8 and Ward Main South from 6.2 to 4.8. Warm admissions, neutral ward and cooler service sources follow the reference's restrained palette. Existing fixture meshes, ranges and shadow settings remain.

Validation: saved and reopened the hospital; all 35 probes retained distinct, valid cubemaps. Entered Play directly using the real Fusion player: one Host runner, owner camera post-processing active, reference tone mapping/white balance/bloom confirmed in the evaluated Volume stack, and 17 valid state-authority door NetworkObjects. `Floor_WardMainHall` resolves its two corridor probes at 0.5 weight each. Captured and inspected actual player-camera views of admissions, ward, service and power room. Console returned zero error/warning entries. Scene diff review found no removed original objects, no architecture transform/mesh/collider changes and no door prefab instance changes.

Current screenshots are under `Library/CodexHospitalReference/runtime_*_final.png`; inspection/reference evidence and the scene diff are in the same folder. The reference scene, shared Volume profile, materials, player, networking code and project settings were not changed by this refinement.

Manual checks: open `Assets/Scenes/Sancturary_V01_Hospital.unity`, press Play, and walk admissions → ward → service → power room. Check room thresholds, reflected highlights, distant visibility and flashlight usefulness on your monitor. For multiplayer, host the hospital through the existing menu and join from a second build; both players should spawn together and see the same environment grade, with local damage effects remaining independent. A separate client instance was not tested in this refinement. Earlier door interaction and movement tests below were not repeated; door network initialization was rechecked.

Baked reflections intentionally represent the saved scene state; moving doors/players are not continuously recaptured. If fixtures or architecture change later, rebake the affected probe's existing EXR using Unity's Reflection Probe inspector and keep that file assigned as its Custom Cubemap.

---

## Earlier Demo Level pass (historical)

# Hospital lighting and direct Play validation

Scene: `Assets/Scenes/Sancturary_V01_Hospital.unity`, Unity `6000.3.15f1`.

This pass preserves the v0.1 floorplan and replaces the inspection lighting with deliberately spaced horror lighting. No architecture, door prefab, vent marker, objective reference, monster, movement, inventory, networking code, package, or global rendering setting was changed.

## Lighting

The primary reference was `Assets/Scenes/Demo Level.unity`, inspected in the Editor and captured at player eye height. It has 20 active downward spot lights, typically intensity 7.5, range 7.4 m, cone 96 degrees, with approximately 8–16 m spacing on its principal routes. It uses dark Trilight ambient lighting, subdued reflections, and exponential-squared fog. Its directional light is inactive; there is no scene post-processing Volume. The visible ceiling sources are existing Lamp_A fixtures with the hospital pack's emissive material. No separate renderer spheres or reusable flicker behavior was found in that setup.

The hospital changes from **72 active lights to 25**, with **35 physical fixtures: 25 working and 10 unlit**. The new hierarchy is `Sancturary_v0_1_BaseMap/Lighting/{HallwayLights,RoomLights,ServiceLights}`. Each working fixture has its own related Light child. The 23 downward spots follow the reference's light-pool approach; two short-range point lights serve the enclosed bathroom and exit vestibule ceiling lamps. Working sources range from intensity 1.15–8 and range 3.7–7.4 m, selected for their particular room and mounting height. There is no active directional light.

Admissions and the central junction retain useful orientation light. Ward fixtures have deliberate gaps, including a dead fixture between working lamps. The service wing is cooler and dimmer. Examination East, Records, Patient 02, Patient 04, and Service Storage have unlit fixtures; their ambient visibility remains sufficient to make out nearby boundaries. These are static lighting states, without new gameplay logic.

Scene-specific environment settings match the Demo baseline:

| Setting | New value |
| --- | --- |
| Ambient mode | Trilight; formerly Flat |
| Sky / equator / ground colors | (0.075, 0.105, 0.088) / (0.040, 0.055, 0.048) / (0.018, 0.022, 0.021) |
| Ambient intensity | 0.72 |
| Reflection intensity | 0.55 |
| Fog | Exponential squared, density 0.012, color (0.025, 0.037, 0.032) |
| Exposure / post-processing | No new exposure override or Volume; existing player presentation retained |

Existing fixture assets reused:

- `Assets/Props/Lamp_A.prefab`
- `Assets/Props/Lamp_B_01.prefab`
- `Assets/Props/Lamp_B_02.prefab`

Working fixtures retain `Horror_Hospital_Props_Emissive_Mat.mat` and `Horror_Hospital_Props_Mat.mat`. Unlit fixture instances replace their emissive material slot with the existing non-emissive props material, preserving atlas UVs. No material asset was edited or created. No glow spheres, new models, downloads, flicker scripts, or decorative clutter were added.

All 25 working sources have shadows to respect the solid room boundaries, with restrained ranges and the existing URP **Low (256 px)** per-light shadow tier and low soft-shadow quality. This is configured on the new lights only. Initial inspection at the default high tier produced shadow-atlas reduction messages; the low tier resolves that pressure without changing the project pipeline asset.

## Existing multiplayer and debug startup

`Assets/Scenes/GrayboxPrototype.unity` was inspected as the prototype reference, together with `DebugSessionBootstrap`, `FusionSessionManager`, `FusionSpawnPoint`, and the canonical player.

The hospital already contained the appropriate configuration from the structural pass, so it was verified and retained rather than duplicated:

- One enabled `DebugSessionBootstrap`, referencing `Assets/Prefabs/FusionNetworkPlayer.prefab` and the existing `FusionLobbyPlayerState.prefab`.
- `FusionSessionManager.StartDirectDebugAsync` starts the current saved scene, using Fusion Host when configured or Fusion Single otherwise. The existing manager creates `NetworkRunner`, `NetworkSceneManagerDefault`, and `NetworkObjectProviderDefault`.
- The bootstrap skips startup when a runner is already active, allowing the normal menu/lobby multiplayer flow to coexist with direct Play.
- Four indexed `FusionSpawnPoint` objects at (-3, 0, -20), (-1, 0, -20), (1, 0, -20), and (3, 0, -20), facing +Z into admissions. Players start together.
- All 17 existing doors retain their NetworkObject, TransformOpenable, InteractionTarget, NetworkLockGroup, motion, audio, and collider configuration. Shared door state still uses the existing authority and interaction checks.

No hospital-only player, additional bootstrap, preplaced runner, networking framework, or Geo instance was introduced. The existing menu destination and Build Settings entry already select this hospital and were preserved.

## Validation

The 324 objects outside the lighting hierarchy matched the pre-edit snapshot in hierarchy, active state, transform, component types, structural meshes/materials, and BoxCollider settings. All 189 architectural meshes remain intact. The FBX, navigation data, reference scenes, menu scene, and Build Settings match their hashes from the start of this pass. Pre-existing ProBuilder settings changes were preserved.

All 25 working fixtures passed checks for ceiling mounting, light origin alignment, and emissive source presence. All 10 inactive fixtures have no enabled Light and no emissive material. Player-height views were inspected in admissions, the ward, service, the central junction, treatment, a dark patient room, and the power room, alongside the Demo reference.

Direct Play started **one running Fusion Host and one canonical player** with input/state authority and one active player camera. The player completed **214.8 m through 21 waypoints in 89.6 seconds**, covering both principal loops and returning to admissions, through the existing Input System/Fusion movement flow with no teleporting during the walk. Temporary synthetic input was removed after the check.

All **17 door NetworkObjects** initialized with valid host authority. Representative A (`Door_ExamWest`), B (`Door_Exit`), and C (`Door_Power`) doors passed queued opening, ordinary player movement through the doorway, and queued closing. The B close was retested from admissions while approaching its visible open leaf; the initial request from farther inside the vestibule did not close it. Door test setup repositioned the player between the three locations; traversal itself used normal input. No door code or prefab was modified.

The existing flashlight was temporarily added through the authoritative inventory during Play, equipped through the existing request path, and switched on to compare the same dark patient-room view. The real held model and light worked and substantially improved wall visibility. That temporary item was discarded when Play stopped; it is not a saved loadout or scene pickup.

Requested script compilation completed with **no C# errors**. The controlled direct-host, movement, door, and flashlight checks produced **zero Console warnings/errors** after the light shadow-tier adjustment. The saved scene reopened successfully, with 25 active lights, zero missing scripts, zero broken prefabs, and no unsaved changes. Unity is left out of Play Mode.

Evidence is saved in `Library/CodexHospitalLighting/`, including reference/player-height images, the before/after hierarchy snapshots, `fixture_validation.json`, `runtime_walk.json`, and `runtime_doors.json`.

## Ian's acceptance checks

1. Open `Sancturary_V01_Hospital` directly and press Play. Expect one player in admissions, facing into the building, with working movement and camera. Walk both loops and compare nearby readability with the dark distant corners.
2. Inspect the dark patient/records/storage rooms and the power approach on your usual monitor settings. Check flashlight usefulness with the existing flashlight item, doorway prompts, and the transition between lit and unlit spaces. This task does not add a permanent flashlight pickup or starting loadout.
3. Open/close the original doors, cross their thresholds, and check sounds, prompts, and shadows with a moving player. The architectural playtest doors remain initially unlocked.
4. In two matching game instances, start from `FusionPrototypeMenu`, host/join the same session, ready both players, and launch. Expect both to reach this hospital at nearby distinct spawns. Open and close doors from each peer, observe replication, then test late join and disconnect. A separate client/two-instance test was not run in this pass; the successful direct host test alone does not prove client behavior.

The original inspection bridge encountered a serialization stack overflow before any map edits. Unity was recovered from the clean saved scene and that bridge and its meta file were removed. Subsequent inspection, scene editing, captures, and Play Mode work used the running Unity MCP server. No temporary Editor script remains in Assets.

Changes remain in the existing `map-modifications` working tree. No branch, commit, push, or pull request was created. This pass changes the hospital scene and its reports only; unrelated changes present at the start were preserved.

