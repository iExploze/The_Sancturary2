# The Sancturary item and combat overhaul

Unity 6000.3.15f1, URP and the existing Photon Fusion host/client session flow. Use `Assets/Scenes/SandboxLaunch.unity` for multiplayer or `Assets/Scenes/SandboxPrototype.unity` for the one-player debug session. Production level layouts are unchanged.

## Items and assets

All definitions remain under `Assets/Inventory/Definitions`. Each usable item retains its existing `World<Name>`, `Held<Name>` and `Held<Name>Remote` prefab paths under `Assets/Inventory/Prefabs`. Ammunition has world prefabs and local reload props, and is not independently equippable.

| Item / prefab stem | Art | Handling |
| --- | --- | --- |
| FireAxe | New Blender axe, painted steel head and worn wood | Overhead wind-up, timed strike and recovery |
| Crowbar | New hooked steel crowbar | Distinct lateral strike |
| RevivalSyringe | New marked medical syringe with cap and plunger | Preparation, teammate-directed reach, injection and recovery |
| RevolverAmmo | New rimmed brass rounds; six-round world bundle | Six individual loading props |
| SawedOffShotgun | New short single-barrel, break-action shotgun | Opening breech, one shell, closure and strong recoil |
| ShotgunShell | New red crimped shell and brass base | Matching shotgun loading prop |
| TranqGun | New long pneumatic dart rifle, stock, sights and reservoir | Bolt opening, distinct dart insertion, closure and restrained recoil |
| TranqDart | New marked dart with needle and stabilizer | Matching rifle loading prop |
| OldRevolver | Existing core meshes/materials preserved | Cocking hammer, trigger, indexing fixed cylinder, six individual rounds and ADS |
| Flashlight | Existing core model/materials preserved | Deliberate switch interaction and equip handling; existing light retained |
| MedKit | Existing bottle and materials preserved; local cap separated | Cap manipulation and dressing treatment |
| Adrenaline | Existing vial/materials preserved | Vial transfer and separate self-injection applicator |

Editable, packed Blender files and export/roundtrip validation reports are in `SourceArt/Items`. Runtime FBXs are in `Assets/Inventory/Generated/Models/ItemOverhaul`; materials/textures and thumbnails are in adjacent Generated folders. Eight exported models contain 1,364-5,700 triangles each. Source files do not participate in Unity imports or require Blender/MCP at runtime.

Local presentation is item-only. The first-person arm prefab, derived arm meshes and IK script have been removed. All existing `Assets/Animations/Items` clips, held-item transforms, ADS, reload, use and recoil motions are unchanged. Invisible grip sockets remain to preserve clip bindings. Remote players retain the original body rig and remote item prefabs.

Ammo world prefabs use 18 x 10 x 18 cm solid box colliders, shared by local aiming and authority validation. Their physics bounds match the colliders, so drop placement remains consistent. Existing range, line-of-sight and single-owner pickup checks remain in force.

Normal monster contacts now produce a brief directional visual recoil and a narrow spray of small droplets. Lethal hits stop AI/collision immediately, then collapse the visual child during the existing 0.8-second removal window. The network root is not animated. Sleep presentation is preserved. Late joiners hide terminal bodies without replaying old reactions.

## Authority and actions

`NetworkPlayerInventory.EquippedInstanceId` is the single equipped gameplay item. Switching clears an active flashlight and cancels timed use/reload. Drops, grid movement, downing, lockers and vents also invalidate relevant actions. Reloads reserve no world object and commit matching ammunition consumption and weapon refill together only after the authoritative timer completes. Existing inventory, healing, revival and adrenaline rules remain in use.

Fusion input carries use, aim and reload requests to state authority. State authority checks the equipped instance, player restrictions, ammo, cooldown and active action before firing or beginning use. Shots resolve one authoritative ray through camera targeting and calibrated muzzle obstruction checks. The shooter's colliders and local presentation do not intercept that ray. Melee resolves during a configured strike window, checks reach/obstruction and records one accepted contact per swing. No local animation event grants damage, healing or ammo.

`MonsterCombatState` is shared by Geo and Henry. Health, death and a Fusion TickTimer for sleep replicate to clients. Incapacitation guards suspend both existing AI controllers; death cancels waking, disables colliders, collapses the visual child and despawns the network object. Local particles/audio have bounded lifetimes and deal no area damage. Small replicated event rings drive confirmed item audio and monster contacts once per peer; joining initializes the event cursor without replaying historical sounds or explosions.

## Tuning and controls

Shared asset: `Assets/Inventory/Definitions/ItemCombatSettings.asset`.

| Setting | Default |
| --- | --- |
| Normal accepted bullet / axe / crowbar damage | 25 |
| Revolver capacity | 6, from the actual OldRevolver definition |
| Geo full health | 2 x capacity x damage = 300; 12 accepted normal hits |
| Henry full health | 3 x damage = 75; 3 accepted normal hits |
| Shotgun | One confirmed hit kills either current monster; configurable instant-kill flag and fallback damage |
| Tranquilizer sleep | 60 seconds; another dart refreshes to 60, ordinary damage does not wake |
| ADS walk limit | 1.1 m/s, applied after ordinary movement/sprint selection, including during adrenaline |
| ADS transition | 0.2 seconds |
| Equip restriction | 0.25 seconds |
| Melee | 2 m reach, 0.16 m sweep radius, normalized strike window 0.42-0.62 |
| Death removal delay | 0.8 seconds |

RMB holds ADS; R reloads the equipped firearm using matching inventory ammunition; LMB uses/fires. Existing inventory drag-to-reload remains supported. ADS cancels sprint and prevents starting it. Per-item definitions expose spread, range, reload duration and separate hip/ADS muzzle offsets. Hip spread is 12 degrees (shotgun 18); ADS spread is 0.3 degrees (tranq 0.2). Shotgun uses one uncertain shot direction rather than a fan of lethal pellets. Revolver reload is 6.2 s, shotgun 2.4 s, rifle 2.7 s. One revolver ammunition bundle refills the six-round cylinder; shells/darts refill their one-round weapon. Treatment takes 3.2 s, self-injection 2.8 s and teammate injection 2.7 s, with the existing healing/revival/buff outcomes committed at completion.

## Audio and effects

Every listed item has replacement handling/use/pickup audio as applicable. Reload insertion and closure are timed separately. Confirmed contacts use impact variations and a separate death burst. All imported recordings are mono 48 kHz PCM16 with trimmed silence, DC removal, fades and balanced levels. They use the existing audio mixer and spatial presentation.

Source, creator, CC0 license and edits for every recording are documented in `Assets/Inventory/Generated/Audio/AUDIO_SOURCES.md`; its JSON companion includes original hashes, selected regions, durations and levels. Particle texture provenance is in `Assets/Inventory/Generated/Textures/ItemOverhaul/SOURCES.md`.

## Validation

Performed in the isolated `codex/item-combat-overhaul` checkout with Unity **6000.3.15f1**. No production scene, package or original character/monster source was replaced.

| Check | Actual result |
| --- | --- |
| Blender exports | All eight passed fresh-process FBX roundtrip checks: hashes, textures, membership, bounds/orientation, units, triangles, UVs and materials. |
| Edit Mode | **74/74 passed**, 2.25 s, after removing temporary authoring/test drivers and checking the final prefab layers. |
| Play Mode | **8/8 passed**, 129.00 s: scene bootstrap/movement, equipment, escape/lobby, receiver balance, complete sleep/refresh/wake/death, interruption/commit, respawn and locker cleanup. |
| Windows development build | **Succeeded**, 0 errors, 490 warnings, 555.93 MB, 44.63 s; temporary drivers removed. |
| Online Fusion | Separate Windows host and non-host builds; third/fourth players joined during testing, including the Editor owner-camera review. Actions were sent through existing Fusion input/interaction paths. |
| Live visual review | All nine usable items were inspected in Play Mode. Medical treatment, injection/revival, ADS, reload props, flashlight, sleep and death frames were captured. Final changes corrected crowded medical rest poses, clipped melee silhouettes and detached sight pieces. |

Authoritative fixture setup supplied inventory, ammunition, encounter resets and player positions for repeatable tests. The temporary driver and Editor generators were removed, including their `.meta` files. Runtime builds require neither those tools nor Blender/MCP.

Measured online results:

- Host revolver against fresh Geo: accepted hit health values **275, 250, 225, 200, 175, 150**, one complete six-round reload, then **125, 100, 75, 50, 25, 0**. Client revolver against Henry: **50, 25, 0**. Both peers observed matching health and one death per kill.
- Separate client axe and crowbar swings each removed **25 HP once**, with one contact event per accepted swing. Client shotgun hits killed both Henry and Geo. Further damage against dead receivers was rejected; dead objects despawned.
- Both monsters were darted in the online session. Sleep stopped movement/attacks for the full interval; another dart refreshed the clock, normal damage preserved sleep, and attacks resumed after expiry. A late join received Henry's remaining **41.4 s**, rather than a fresh timer. Death while asleep canceled waking. A four-player late-join check after deaths contained no monsters and replayed no old death effects.
- Host/client pickup contention awarded a syringe to one inventory and rejected the second request. Every listed item was picked up; usable items were equipped and switched, ammunition remained inventory/reload data, and dropping was exercised. That earlier version still had owner arms; this revision removes them. Proxies used remote held items. Switching from flashlight to revolver cleared the beam on both peers.
- Healing, revival and adrenaline completed once; switching canceled treatment/revival without consuming the item or granting its outcome. Full-health healing was rejected. Reload interruption preserved ammunition and completion consumed one matching source. Aiming with adrenaline and sprint requested had **maxSpeed 1.1 m/s**; releasing aim restored **2.4 m/s** walking.
- At 6 m, rifle hip/ADS contacts were **2/6 versus 6/6**; at 12 m, **1/6 versus 6/6**. At 2 m both were **6/6**. Revolver samples at 6/12 m were **1/3 and 0/3 hip**, versus **3/3 ADS** at each distance. Shotgun samples were **0/3 hip versus 3/3 ADS** at both 6 and 12 m. These small samples demonstrate the configured difference, not a statistical balance study.
- Shots against the existing danger-room wall consumed ammunition without producing monster contacts behind it. Confirmed contact/death counts and item audio event cursors agreed between peers; the owner did not replay its confirmed event twice.

Earlier failures were corrected before the passing runs: a client lobby query accessed an unspawned network property; new sight supports inherited the wrong pickup layer; and older movement tests referenced obsolete Animator state names and ran into Door_A. Deterministic tests now use the existing local Fusion mode to avoid Photon authentication/session collisions, a temporary clear movement floor, and the authoritative clock for the full sleep interval. Online behavior was tested separately in actual multi-instance sessions.

### Remaining limits and manual verification

Recoil comfort, transition feel, the new monster reactions and the final subjective sound mix still need Ian's manual review. Sleep uses a tipped animation pose and simple colliders, not a ragdoll. Detailed use animation remains owner-only as requested. Severe packet loss, disconnect during every action variant, and melee against a monster immediately behind a thin moving door were not exhaustively exercised.

The multi-instance runs preceded the final medical duration, shoulder placement and sight/particle presentation adjustments. The final timed logic passed local Play Mode tests; repeat the host/client checklist on the final build when reviewing feel and timing. Audio source downloads, waveform levels, spatial setup and replicated event delivery were checked; a subjective listening pass was not performed.

Existing GrayboxPrototype missing `InsideTrigger` references and inference-package shader/assembly warnings appear in the all-scenes development build. They are outside this overhaul; the SandboxPrototype tests used the current dedicated test map. No changes to those production/legacy scene references were included.

Previous overhaul build: `Builds/ItemOverhaul/TheSancturary.exe` with its adjacent data folders, produced at 21:56 UTC on September 12, 2026. Build output and detailed local test logs under `Library/ItemOverhaul` are intentionally excluded from Git.

### September 12 feedback revision

All 47 item/contact WAV files were replaced with different premade effects or recorded foley. Natural pitch/speed are preserved; edits are excerpt selection, quiet-edge trimming, mono conversion, level balancing and short anti-click fades. Source licenses, creators and exact selections are in `Assets/Inventory/Generated/Audio/AUDIO_SOURCES.md` and `.json`. No generated audio is used.

The detailed multiplayer results above describe the preceding overhaul, not a new multi-instance run of this revision. The old screenshots under `SourceArt/Items/PlayModeReview` are historical and show the removed arms/old effects. Revision validation: 77/77 Edit Mode tests and 9/9 Play Mode tests pass in Unity 6000.3.15f1. The new tests cover forgiving ammo colliders and visible hit recoil/death collapse on both monsters while their network roots stay still. All 47 WAVs differ from the preceding version, have finite mono 48 kHz samples and no output clipping. Unity importer normalization is disabled to preserve the authored level differences. Item animation clips and held-item prefabs are byte-unchanged. Live Sandbox inspection confirmed zero arm objects or skinned renderers beneath the owner camera, a held revolver and its separate remote representation, and correct enlarged colliders on all 24 spawned ammo supplies. A fresh two-instance test and subjective listening pass were not run for this revision; use the checklist below.

Revised Windows development build: `Builds/ItemRevision/TheSancturary.exe` and adjacent data folders, built September 12 at 23:03 UTC: zero errors, 490 existing project/build warnings (Graybox trigger references and inference shaders/assembly warnings). No temporary Editor tool or test driver is included.

![Current item-only owner view](SourceArt/Items/PlayModeReview/Revolver-no-arms-revision.png)

## Manual acceptance pass

1. Open SandboxLaunch in host/client instances, create/join the same room, Ready, then Start Game. For solo, open SandboxPrototype and press Play. Allow compilation/import first.
2. Use the safe staging supply trays. Pick up each item and matching ammunition with F, including aiming slightly beside each ammo model and trying the same action with a wall in the way (must reject). Equip, switch rapidly, move its inventory cell and drop it. Only one local item should remain; the other player should see one correctly attached remote item and no floating local arms. Toggle the flashlight, switch to a gun, and verify its held beam shuts off.
3. Use staging's Select Monster / Reset Encounter controls with F. Start Geo at full health. Reload the six-round revolver and count accepted contacts, not trigger pulls: six hits, reload, six hits kills. Reset with Henry: three hits kills. Fresh Geo should lose 25 HP for one axe swing and 25 for one crowbar swing, including when holding the crosshair on it throughout the strike window. Misses and strikes through walls must not produce monster contact effects.
4. Test both monsters with one confirmed shotgun hit. Verify one directional death spray, a visible collapse, immediate cessation of attacks, and network removal. Dry fire, reload, switch during reload, down during reload and retry; ammo must not duplicate or complete early.
5. Fire all three ranged weapons without ADS and with ADS at short, medium and long unobstructed sightlines. ADS should be much more reliable, align real sights, prevent sprint and enforce a slow walk. Repeat while adrenaline is active. Put the muzzle against a wall: it must not damage a monster behind it.
6. Dart each monster, approach it in danger, and remain nearby for the full 60 seconds. It must not move or attack until expiry. Dart again partway through: the timer refreshes to 60 rather than adding 60. Normal damage must not wake it. Kill it while asleep and wait beyond expiry: it must remain removed. Join a third player during sleep and after death to verify current state, unchanged remaining time and no replayed death.
7. Use Hurt SELF, then medkit; repeat with switching halfway through and at full health. Test adrenaline, repeat-use rejection and cancellation. Use Down SELF on one player and revive with the syringe before the existing 60-second respawn. Test wrong target, range/LOS loss and switching during revival. Confirm no item loss on invalid/canceled use, one item consumed on completion, and matching health/buff state on both peers.
8. Run the above with actions initiated by both host and client. Check repeated encounter resets/restocks and player disconnect/rejoin. Inspect both Consoles for missing references or runtime errors. Confirm no owner hands/arms appear; judge medical prop visibility, reload timing, recoil comfort and sound balance in motion; automated state checks do not establish animation quality.

## Clear monster-hit feedback

Confirmed contacts now play a separate species-specific hurt voice: Geo uses a low pitched grunt/growl, Henry a high pitched scream. Both are premade CC0 recordings; provenance and processing are in `Assets/Inventory/Generated/Audio/AUDIO_SOURCES.md`. Voices are spatial, clearly audible nearby, and independent of the monster object so a lethal hit or sleep transition does not cut them off. Wet contact sounds remain underneath. Pitch and volume can be adjusted on `ItemCombatSettings`.

The existing MonsterContact particle prefab now emits 36 initial blood droplets plus a short 0.65-second flow, with larger unlit red droplets falling under gravity. Confirmed dart hits also have contact blood; lethal hits retain their larger death burst. All effects use the existing replicated contact sequence. No damage, hit detection, AI, models, animations, or replicated fields changed.

Manual check: open SandboxPrototype, spawn Geo, then shoot or hit with a crowbar/axe. Expect one low hurt growl and a clear red burst at each accepted hit. Reset with Henry and expect a distinct shrill scream. Try a tranq hit and a lethal shotgun hit, then a miss: misses must produce no monster feedback. Repeat in SandboxLaunch as host and client, with the other player observing nearby. Confirm both hear the matching voice and see blood once per hit. Audio character and loudness still need Ian's listening pass.

Validation for this revision: Unity 6000.3.15f1 batch import/compilation and native prefab serialization completed. Two focused PlayMode tests passed: distinct hurt voices/visible blood/no replay, and existing exact damage/death rules. Temporary setup script and meta removed. No fresh host/client run or subjective listening/visual-quality pass was performed.
