# The Sancturary multiplayer sandbox

## Launch

- **Solo:** open `Assets/Scenes/SandboxPrototype.unity` and press Play. The existing DebugSessionBootstrap starts a one-player Fusion session.
- **Multiplayer:** open `Assets/Scenes/SandboxLaunch.unity` in the host and client Editors. Use Create Room / Join Room with the same room name, Ready in the existing lobby, then Start Game on the host. Up to four players use the existing session manager and canonical player prefab.
- **Windows build:** launch the game with `-sandbox` to open SandboxLaunch. Without that explicit argument, normal startup remains unchanged. Build all enabled scenes in their existing order.
- Death uses the existing scene-local respawn component with a **60-second delay**, leaving time to test revival. Respawns and joining players use four indexed safe spawn points.

## Structure and source

Blender owns the editable structure; Unity owns gameplay, collision, navigation and lighting.

| File | Location |
| --- | --- |
| Editable Blender source | `E:/BlenderWithMCPtest/assets/Sancturary_SandboxPrototype.blend` |
| Explicit staged export | `E:/BlenderWithMCPtest/exports/sancturary_sandbox/sancturary_sandbox.fbx` |
| Blender export manifest / roundtrip validation | `E:/BlenderWithMCPtest/exports/sancturary_sandbox/manifest.json`, `validation.json` |
| Unity structural asset | `Assets/FBX/Sancturary_SandboxPrototype.fbx` |
| Gameplay scene | `Assets/Scenes/SandboxPrototype.unity` |
| Separate launch scene | `Assets/Scenes/SandboxLaunch.unity` |
| Saved navigation | `Assets/Navigation/SandboxPrototype_NavMesh.asset` |

The 40 × 30 m envelope, central divider, west/east test regions and south/east sightlines evolve the inspected GrayboxPrototype. The southwest is protected staging; its southeast bay is an enclosed flashlight test area. A four-metre entrance leads east into danger. Two northern cross-connections form a loop around solid occluders; the northwest has two door/hiding rooms. Main ceilings are 4 m high, accommodating the actual Geo agent's 3.3 m height (Henry: 0.84 m; both agent radii: 0.38 m). Canonical player standing height/radius: 1.65/0.32 m. The two danger-room door instances are scaled to fit these test clearances; source prefabs are unchanged.

There is no scene asset named mapv01 in the checkout. The newly merged `Sancturary_V01_Hospital.unity` was inspected as the intended v0.1 reference. The sandbox uses its existing URP pipeline and read-only hospital Volume profile, black sky ambient, zero environment reflection, and linear grey fog. Seven overlapping neutral-white ceiling lights provide bright indoor equipment-room illumination in staging; weaker orientation pools leave dark danger space. The dark bay's existing switch controls a test light, initially off. No auto-exposure mechanism was added.

## Controls

At the north wall of staging, aim at a labelled button and press **F**, through the normal interaction system:

- **Select monster:** cycle Geo / Henry. The station display shows selection and active state.
- **Spawn selected:** create one monster at an available danger reset point. A second active spawn is rejected; reset points within 6 m of a player are skipped.
- **Despawn monster:** clear the active encounter through Fusion.
- **Reset encounter:** replace the active monster with the currently selected monster at a valid reset point.
- **Restock supplies:** recover dropped equipment, replenish the authored supply allowance, restore tray poses, reset tool obstacles and key locks. Held inventory is preserved and counted against supply. Authored supplies stay on their trays; dropped prefab copies retain normal physics. Close any open key-test door/drawer before retrying its lock.
- **Hurt SELF – 50 HP:** deliberately damage only the player pressing the button, for med-kit tests.
- **Down SELF – revive test:** deliberately down only the player pressing the button. Another player can use a revival syringe before the 60-second automatic respawn.

Controls require a living, non-hidden actor in staging and use the existing authoritative interaction validation for ownership, aim, distance and obstruction. Short cooldowns suppress duplicate commands. The host/state authority owns selection, active monster ID, restock revision, status, pickups, inventory, locks and damage. Owner UI and held-view presentation remain the existing local systems.

## Equipment and interaction audit

| Equipment | Supply | Implemented behavior / limits |
| --- | --- | --- |
| Flashlight | 4 | Pickup, equip, toggle, drop; existing owner/remote presentation |
| Med kit | 4 | Existing timed full heal |
| Revival syringe | 4 | Existing timed teammate revival |
| Adrenaline | 4 | Existing timed stamina/sprint effect |
| Crowbar / fire axe | 2 each | Existing tool actions against their matching resettable test obstacles; no invented monster melee damage |
| Old revolver | 2 | Existing firing/dry-fire presentation, ammunition consumption and reload |
| Tranq gun | 2 | Existing firing/dry-fire presentation, ammunition consumption and reload |
| Sawed-off shotgun | 2 | Existing firing/dry-fire presentation, ammunition consumption and reload |
| Revolver ammo / tranq darts / shotgun shells | 8 each | Matching existing ammunition IDs; normal inventory/reload rules |
| Small / medium / long test items | 2 each | Existing inventory footprint tests; no use action invented |
| Cabinet / maintenance / cage key | 1 each | Matched to file cabinet, safe Door C, and labelled bedside storage respectively |

**Firearms are incomplete combat implementations:** the inspected item-use controller consumes rounds and emits firing/reload presentation but does not implement monster bullet damage, tranquilization, or shotgun hit effects. The sandbox preserves that limitation.

Functional interaction instances include file-cabinet drawers, bedside drawers, aid-kit door, all four existing door variants in the safe bay, two danger-room doors, four hiding lockers (one safe, three in danger), the existing light switch, three keyed storage/door targets and two tool obstacles. No required test item is hidden behind progression; locks do not gate general movement.

**Monsters:** Geo and Henry use their actual registered network prefabs, controllers, animation and normal tuning. Geo provides visual detection/chase and witnessed-locker behavior. Henry provides movement-triggered acquisition and its existing chase/attack/locker rules. Ethan has imported model/animation assets but no functional gameplay prefab/controller in this checkout. The legacy SimpleMonsterChase is an offline prototype, not an additional supported Fusion monster.

## Safety and reset boundaries

The scene opts into `SandboxSession`; campaign scenes contain no such component. A navigation exclusion covers staging and the entrance buffer. Both existing monster controllers additionally reject movement steps entering the protected bounds and reject protected players at authoritative acquisition, chase retention and attack resolution. Pending monster damage is rechecked after player movement, so a player who returns to safety is protected even if damage was queued earlier. Intentional self-damage uses the normal damage path and is separate from monster immunity.

The entry has no closing barrier or progression lock. Returning to safety invalidates the target, allowing normal scan/patrol/calm behavior or another danger target. Spawn markers and patrol markers are outside protection. Only one controlled monster is active, and reset points are away from staging.

Restock despawns dynamic world pickups through Fusion, preserves all held inventories, and restores only the remaining authored quantity for each item ID. This bounds repeated replenishment and recovers disconnected players' missing supplies without changing campaign scarcity. Scene pickup availability and equipment pose, monster identity, locks and tool state replicate normally for late joiners.

## Future vent reservation

`Safe Staging - Equipment and Controls/Future Vent Reservation - Dark Bay South Wall (SEALED)` marks **(-4, 2, -14.8)**. The south wall remains solid. All inherited vent objects were removed from the new scene only; no entrance, exit, teleport, vent dimension or monster vent route is enabled.

## Validation

Actual validation used Unity **6000.3.15f1** and Blender **4.5.13 LTS**:

- BlenderMCP construction, overhead and player-scale inspection, explicit FBX export and fresh Blender FBX roundtrip passed. Unity mesh bounds agree with the exported metre-scale layout. The final export contains 32 structural meshes / 384 triangles.
- Scene saved and reopened with zero missing scripts; final script compilation had no errors. All four player spawn capsules were clear. All three monster reset points had complete paths. **574 route capsule samples** using Geo and Henry heights found no obstruction on the main test routes; safe staging had no navigable sample. Test doors were temporarily opened for clearance inspection and restored afterwards.
- **One Editor host plus three standalone Windows clients** connected through the existing lobby. The third and fourth players joined after equipment collection and monster spawning, received current replicated state, and spawned safely. The client selected/spawned/reset monsters through the normal queued F-interaction path. Geo and Henry both spawned on navigation; resetting to Henry removed Geo.
- Simultaneous host/client pickup requests yielded exactly one flashlight owner on both peers. Host equip/toggle/drop replicated. Client restocks removed the dynamic drop, preserved a teammate's held flashlight, and kept 56 scene item objects across repeated resets. After the equipment owner disconnected, restocking returned all 56 available supplies. A duplicate monster spawn was rejected.
- Geo chased a danger-zone client while the other players remained safe, then cleared its target and resumed patrol after the client returned to safety with health unchanged. A separate **host return during Geo's actual attack wind-up** preserved 100 health for both host and safe client. Henry acquired a moving host (movement sent through Input System/Fusion), then returned to patrol with no target and unchanged health after the host returned to safety.
- A client used the self-down button through normal interaction. The host's existing timed syringe action revived that client to 50 health; the other three players remained at 100. Weapon checks exercised the existing authoritative pickup/use logic and queued equip/reload/drop commands: revolver reload 6 → fire 5; tranq and shotgun reload 1 → fire 0. No unimplemented monster hit effects were claimed.
- The final one-player authoritative medical/tool check passed med-kit healing (50 to 100), adrenaline activation, and completion of both crowbar and fire-axe test targets. These used real item definitions, queued equip/drop commands and the existing timed-use controller.
- Safe Door A opened through the normal interaction request. Safe locker entry and exit succeeded with unchanged health. The final supply-physics check confirmed stationary authored supplies, normal dynamic physics on a dropped copy, and return to 56 scene items on restock.
- The multiplayer run preceded the final local signage, entrance-strip alignment and stationary-supply presentation refinement; those final refinements received a separate one-player runtime check. The multi-instance checks did not attempt every permutation of monster/locker/door/item behavior, full-map traversal by four simultaneous moving players, or every key/drawer combination.
- The existing selected Edit Mode suite completed **45 tests: 44 passed, one failed**. `RoadmapItemAssetTests.RequiredWorldPrefabsUseTheAuthoritativeWorldItemPattern` expects mesh-shaped collision on `WorldOldRevolver.prefab`, which currently has zero mesh colliders. That prefab and test are unchanged from main; the sandbox preserves the real current item. This asset-test discrepancy remains unresolved.
- The final Windows build after removing the temporary harness succeeded with **zero errors and 493 warnings** (the earlier multiplayer validation build had 494 warnings), including existing compute-shader variant warnings and duplicate WebSocket assembly warnings. During the controlled multiplayer run, the only Console warning retrieved was the expected rejection of the losing pickup contender. The final capture session logged five recursive-PlayerLoop errors from Unity MCP ScreenshotUtility.cs:196 while capturing images; these were tooling errors, not script compiler errors. They are retained as a validation limitation.
- GrayboxPrototype, Sancturary_V01_Hospital, normal FusionPrototypeMenu, shared Volume, player/item/monster prefabs, packages and global rendering settings are unchanged. No vent components were added. The inherited vent root was removed only in SandboxPrototype.

Evidence is local under `Library/CodexSandbox/`: `chase-return-test.txt`, `attack-boundary-test.txt`, `henry-safe-return-test.txt`, `client-henry-control-test.txt`, `revival-test.txt`, `firearms-test.txt`, `locker-test.txt`, `door-open-state.txt`, `final-supply-test.txt`, `clearance-validation.txt`, and saved host/client snapshots. Temporary test code, its `.meta`, and task-specific authoring scripts were removed. The final Editor was idle on the saved SandboxPrototype scene, compilation complete, with no temporary probe type loaded.

Actual screenshots:

- [Blender overhead](E:/BlenderWithMCPtest/exports/sancturary_sandbox/blender_overhead.png)
- [Blender player scale](E:/BlenderWithMCPtest/exports/sancturary_sandbox/blender_player_scale.png)
- [Unity equipment area](E:/Github/The_Sancturary2/Library/CodexSandbox/safe_equipment.png)
- [Unity flashlight off](E:/Github/The_Sancturary2/Library/CodexSandbox/flashlight_off.png) / [on](E:/Github/The_Sancturary2/Library/CodexSandbox/flashlight_on.png)

These screenshots are actual Blender/Unity captures. The Blender overhead hides ceilings for inspection only; the saved source and gameplay scene contain all ceilings. Subjective movement feel, monitor-dependent darkness, chase pacing, and the remaining manual combinations still need Ian's playtest.

## Changed files

New: SandboxPrototype and SandboxLaunch scenes; imported structural FBX; scene NavMesh; four URP materials and depth-tested label shader under `Assets/Materials/SandboxPrototype`; `SandboxSession.cs`, `SandboxControl.cs`; this guide and Unity-generated `.meta` files.

Focused extensions: GeoMonsterController and HenryMonsterController sandbox target/movement guards and patrol wiring; FusionNetworkPlayer's separate pending monster-damage path; sandbox reset methods on WorldInventoryItem, NetworkKeyPickup, NetworkToolObstacle and NetworkLockGroup; WorldItemPhysics tray-pose reset and scene-only stationary-supply option; two appended enabled build scenes. Normal startup uses its existing scene unless `-sandbox` is explicitly supplied.

The existing untracked Ethan monster `.meta` files are preserved and excluded from the sandbox commit. Blender source and staged export follow the existing external Blender workspace convention and are not automatically synced into Git.

## Manual acceptance

1. Open SandboxPrototype and Play. Expect one canonical Fusion player, full health, no active monster and four usable flashlights in staging. Walk around every equipment group and through the dark bay.
2. Pick up/equip/drop the existing items. Fire/reload each weapon with matching ammunition; expect presentation and round consumption, not unimplemented monster hit effects. Use the tools on the corresponding obstacles. Unlock the three matching test targets and exercise doors, drawers and the light switch.
3. Hurt yourself, then use a med kit. In multiplayer, down yourself and have a teammate revive you. Expect only the requesting player's health to change; wait 60 seconds separately to verify automatic safe respawn.
4. Spawn Geo, enter danger, draw a chase, turn behind solid occluders, complete both sides of the loop, and return through the marked threshold. Expect protection inside staging and eventual scan/patrol rather than a permanent entrance chase. Repeat with Henry, including moving versus standing still and the existing locker behavior.
5. With one host and one client, keep one player safe while the other enters danger. Exchange roles. The danger player remains a valid target; safe players receive no monster damage. Cross back during an attack wind-up and verify health remains protected.
6. Have both players request the same pickup and monster spawn nearly simultaneously. Expect one item owner and one active monster. Have the client select/reset/despawn/restock while the host observes. Drop items, consume medicine/ammo, restock repeatedly and confirm no accumulating dynamic pickups or duplicated held equipment.
7. Join another player after items are collected, a door is opened and a monster is active. Expect current replicated state and a safe spawn. Disconnect an equipment owner and restock; repeat with four players if available.
8. In the dark bay with its switch off, compare flashlight off/on at nearby corners and down the longer view. Judge navigation readability and flashlight response on your monitor. Verify no vent interaction is presented and normal hospital/menu behavior is unchanged.

## Equipment-room lighting revision

The safe equipment room now uses seven broad neutral-white ceiling spots (intensity 28, range 10 m, 145-degree outer cone), redistributed across the weapons, utilities, medical, tools and control stations. This replaces the five narrow, dim pools. Soft shadows preserve item shape and table contrast. The dark-bay light, danger lights, shared Volume and global rendering settings are unchanged.

Validated in Unity 6000.3.15f1 with one-player Play Mode and actual screenshots: all 60 runtime world/key pickups had an unobstructed staging-light path within the useful light cone/range, ignoring their own colliders. Equipment overview and weapon-table views were visually inspected; Console check returned no errors or warnings. This lighting-only revision did not rerun the Windows build or host/client test. Screenshot: `Library/CodexSandbox/staging-brighter-play.png`.

Manual check: open SandboxPrototype, Play with the flashlight off, and walk each equipment aisle. Expect readable items and surfaces without blown-out white tables. For multiplayer, launch SandboxLaunch on host/client and compare the same stations; lights are scene-local presentation and add no replicated state. Check the enclosed dark bay separately with its switch off.
## Locker-death respawn correction

All death paths now use the same authoritative death transition, including the instant kill used by monster locker ejection. It starts the existing scene respawn timer and releases locker occupancy before clearing the player's locker reference. The sandbox still automatically respawns after 60 seconds, preserving its revival window; scenes without LevelRespawnSettings retain their existing death policy.

Unity 6000.3.15f1 Play Mode regression suite: 2/2 passed. The existing normal-damage test verifies the five-second Reference Playable delay. The added sandbox regression repeats instant death while hidden, monster-ejection instant death, and lethal normal damage while hidden, checking scheduled respawn, freed occupancy, restored health/input and return to safe staging. Only the test instance uses a one-second delay. The run emitted existing convex-mesh hull-limit diagnostics; no collider assets were changed. No new multi-instance test or Windows build was run for this correction.

Manual host/client check: in SandboxLaunch, have the client enter a danger-zone locker and trigger Geo's witnessed-entry attack. After death, wait 60 seconds without using a revival syringe. Expect automatic safe respawn with movement and inventory input restored, and the locker available again. Repeat as host, then repeat a normal death outside a locker.