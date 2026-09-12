# Hospital sparse prop pass

Blender planning file: `E:/BlenderWithMCPtest/assets/Sancturary_v0_1_Hospital_PropLayout.blend`.
Unity scene: `Assets/Scenes/Sancturary_V01_Hospital.unity` (Unity 6000.3.15f1).
Placement manifest: `E:/BlenderWithMCPtest/exports/sancturary_v01_hospitalbase/prop_layout.json`.

## Delivered scope

61 existing prefab instances, organized under `Sancturary_v0_1_BaseMap/Props`. Original architecture, all original component settings, lighting, Volume, reflection probes, materials, doors and bootstrap are preserved. The only change to an original scene object is the new Props child on the existing map root. Blender architecture mesh/transform fingerprints match before/after; its source remains intact, with a separate planning copy and PropLayout collection. Planning boxes and arrows were not exported into Unity. No new art assets or gameplay scripts were created.

The existing hospital pack assets under `Assets/Props` supply beds, chairs, three-seat waiting benches, tables, shelves, sanitary fixtures and electrical boxes. 14 individual chairs and two three-seat bench units were added; only two individual chairs sit in chase hallways. Rooms contain roughly 2–4 pieces, with empty circulation areas retained. Desks in administration, nurses and maintenance use a scene instance height scale of 1.5 to make the existing low table serve as a work surface; shared prefabs remain unchanged.

## Gameplay placements

Four `Assets/Prefabs/Locker_Prefab_1.prefab` instances:

| Area | Position (x, y, z) | Facing |
| --- | --- | --- |
| Ward Main Hall | (-23.5, 0, 13.3) | East |
| Ward South Hall | (-10.5, 0, -3.5) | North |
| Service Main Hall | (21.5, 0, 17) | West |
| Service Return Hall | (2.5, 0, 9.5) | East |

The saved locker doors are closed. Their existing view, hidden-storage, exit and monster interaction anchors remain wired. They sit on walls, away from doorway arcs, without clusters at intersections.

| Room | One gameplay unit | Existing prefab |
| --- | --- | --- |
| Patient 01 | Openable bedside | Assets/Prefabs/Medical Bedside.prefab |
| Patient 03 | Openable bedside | Assets/Prefabs/Medical Bedside.prefab |
| Administration | Openable file cabinet | Assets/Props/File Cabinet.prefab |
| Records | Openable file cabinet | Assets/Props/File Cabinet.prefab |
| Treatment | MedKit on medical cart | Assets/Inventory/Prefabs/WorldMedKit.prefab |
| Nurses Workroom | MedKit on medical cart | Assets/Inventory/Prefabs/WorldMedKit.prefab |

All other normal rooms have zero such gameplay units. Each multi-drawer furniture unit counts as one searchable furniture object. No room combines storage with a MedKit or a second storage unit. Existing NetworkLockGroup components are retained but explicitly configured with needsKey=false and empty key requirements; no locks, keys or drawer contents were added. The Props File Cabinet is already Fusion-enabled and has no embedded key pickup, unlike the separate Prefabs variant inspected during the audit. Two MedKits total use the real current item (its existing model is a medicine bottle).

## Starting equipment

One existing `Assets/Props/Shelf.prefab` at (-4.5, 0, -14.65), facing the spawn area, holds four separate `Assets/Inventory/Prefabs/WorldFlashlight.prefab` instances: two columns 0.8 m apart on two shelf levels. Flashlights lie horizontally on physical shelves and settle under the existing item physics. The rack is decorative. The four pickups are the explicit starting-equipment exception to the normal-room distribution rule; no extra drawer, cabinet or MedKit is in admissions. Spawn transforms remain untouched and clear.

World pickups follow the scene NetworkObject placement pattern inspected in GrayboxPrototype. Each has its own authoritative collection flag and NetworkTransform/WorldItemPhysics. Furniture uses existing TransformOpenable replication; lockers use existing occupant and door-state replication. No alternate networking or local-only shared object was introduced.

## Room and hallway review

All 28 mapped areas were captured from the real player camera and visually reviewed with a physically collected flashlight. Dark areas retain their existing light levels. Gameplay readability and empty chase space take priority over dressing density.

| Area | Added pieces | Normal-room interaction units |
| --- | --- | --- |
| AdmissionsLobby | 2 bench, 1 desk | 0 |
| ExitVestibule | 1 chair | 0 |
| PublicSpine | Open circulation; no new props | 0 |
| ExaminationWest | 1 bed, 1 chair, 1 table | 0 |
| PublicBathroom | 1 toilet, 1 basin | 0 |
| ExaminationEast | 1 bed, 1 chair | 0 |
| Administration | 1 desk, 1 chair, 1 cabinet | 1 |
| Records | 2 shelf, 1 cabinet | 1 |
| CentralNursesJunction | Open circulation; no new props | 0 |
| WardSouthHall | 1 locker | 0 |
| WardMainHall | 1 locker | 0 |
| WardNorthHall | 1 chair | 0 |
| WardReturnHall | Open circulation; no new props | 0 |
| Treatment | 1 bed, 1 table, 1 chair, 1 medkit | 1 |
| NursesWorkroom | 1 desk, 1 chair, 1 table, 1 medkit | 1 |
| Patient01 | 1 bed, 1 chair, 1 bedside | 1 |
| Patient02 | 1 bed, 1 chair | 0 |
| Patient03 | 1 bed, 1 chair, 1 bedside | 1 |
| Patient04 | 1 bed, 1 chair | 0 |
| Isolation | 1 bed, 1 table | 0 |
| ServiceApproach | 1 chair | 0 |
| ServiceMainHall | 1 locker | 0 |
| ServiceNorthHall | Open circulation; no new props | 0 |
| ServiceReturnHall | 1 locker | 0 |
| StaffRoom | 1 desk, 2 chair, 1 shelf | 0 |
| ServiceStorage | 3 shelf | 0 |
| Maintenance | 1 shelf, 1 desk, 1 electric | 0 |
| PowerRoom | 2 electric, 1 shelf | 0 |

The equipment shelf and its four flashlights are listed separately above.

## Validation

- All 61 prop bounds fit their assigned area, with no missing materials. No prop bounding box is within 1.5 m of a portal center. Existing colliders were reused; no global collision settings changed.
- Updated the existing NavMesh data in place, preserving its asset GUID. All ten segment checks across both chase loops returned complete paths; all four monster locker approach points sample onto the NavMesh. The existing agent type is shared with Geo. No Geo AI instance was added for this pass.
- Direct Play starts the existing Host session and real player. All 14 added NetworkObjects initialize with state authority.
- All four rack flashlights were individually targeted and collected through the player's normal interaction command flow. Availability decreased one at a time, and all four separate inventory entries appeared. One collected flashlight was equipped and switched on for room inspection.
- All four lockers were entered and exited through that interaction flow, reaching Occupied then ClosedFree with the correct player hidden state.
- A drawer in each of the four storage units was opened through player interaction and verified unlocked. Both MedKits were collected through the same authoritative pipeline.
- The real player controller completed a 175.7 m, 12-waypoint walk through both chase loops using input, in 73.5 seconds. An initial straight waypoint trial was followed by a successful NavMesh-guided route. No traversal teleporting was used after the initial test position. Existing patient-room door opening and closing both passed; closing was retested from inside the room to get clear line of sight to the swung leaf. Both bedside units also closed after retargeting their opened drawers.
- Scene validation reported zero missing scripts or broken prefabs; Console checks returned zero errors/warnings. No new C# scripts required compilation. The final scene is saved in Edit Mode.
- Two-instance and four-player runtime tests were not performed. Single-host checks do not establish client replication correctness.

Evidence: `Library/CodexHospitalProps/` contains room screenshots, the rack view, per-instance bounds, clearance checks, NavMesh checks, interaction results and preservation diff. Temporary authoring scripts are removed before handoff.

## Ian's manual checks

1. Open the hospital directly and press Play. Confirm all four spawns remain clear, the shelf is visible ahead/left, and each of its four flashlights is individually targetable. Pick one up with F, equip it and toggle it using the existing item controls.
2. Walk both loops, pass all four lockers, enter/leave a locker with F, and open the existing room doors. Check movement feel and shoulder clearance around chairs and open locker doors.
3. Search Patient 01/03, Administration and Records: storage should open without a key. Treatment and Nurses Workroom each have one real MedKit on a cart. Confirm all other rooms remain sparse and free of unintended gameplay items.
4. Host through the existing menu and join from another build; ideally use four players. Each player takes a different flashlight. Only that pickup should disappear for everyone. Verify storage states replicate, a locker admits only one occupant, and only one player can claim each MedKit. Verify local camera effects remain independent.
5. Judge room function, darkness, shelf discoverability and chase readability on your monitor. Existing baked reflection cubemaps are retained unchanged in this controlled pass.

