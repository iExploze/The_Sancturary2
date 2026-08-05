# The Sancturary Roadmap to v1.0

> **Planning status — August 4, 2026**
>
> The v0.1 scope in this document is a near-term production commitment. v0.2 through v0.4 are planned development targets. v0.5 through v0.8 are progressively less specific product direction, because their exact content should be informed by playtesting, art readiness, and what the earlier versions prove. v0.9 and v1.0 define release gates rather than promising a fixed amount of content.

## Purpose

This roadmap turns the current prototype, repository state, and established game-design discussions into one path toward a finished game. It is meant to prevent two opposite failures:

1. endlessly adding systems without completing a scary, playable escape game; and
2. finishing a technically functional prototype that never grows into the intended seven-night experience.

A version only advances when its exit conditions are met. Later ideas must not be pulled into an earlier version merely because their supporting code would be interesting to build.

The order of work is deliberate:

```text
Prove one complete escape
→ Make that night replayable and presentable
→ Make items create survival choices
→ Add meaningful route choice
→ Add monsters that change player behaviour
→ Connect the game into seven persistent nights
→ Complete campaign escalation and content
→ Deliver the Sentient One story and endings
→ Lock, balance, optimize, and ship
```

## Product north star

The Sancturary is a movement-heavy, first-person escape-horror game for one to four players. The players awaken inside a large abandoned institution filled with original monsters that were designed or confined there. They must investigate dangerous spaces, collect scarce items, understand environmental clues, prepare one viable escape route, survive pursuit, and get out.

The final game is not primarily a shooter, a puzzle anthology, a procedural roguelike, or a lobby-driven collection of disconnected matches. Its identity comes from combining:

- the vulnerable pursuit and hiding pressure of *Outlast*;
- the physical inventory pressure, route planning, and environmental progression of *Resident Evil*;
- movement-heavy first-person chases in which stopping to read, search, heal, organize items, or operate a mechanism is itself dangerous; and
- a seven-night campaign in which every successful escape permanently changes what remains possible.

### The intended v1.0 campaign loop

Every campaign lasts seven nights. The exact route order, monster schedule, and amount of run variation are not locked yet, but the campaign structure is.

```text
Wake in the Dead Room
→ Observe what changed and which routes are already sealed
→ Explore and scavenge the persistent institution
→ Decide which objective and escape route to pursue
→ Manage limited inventory space, health, tools, and teammates
→ Evade or temporarily counter the active monsters
→ Complete the selected route under increasing pressure
→ Escape the institution for that night
→ Receive a playback or confrontation from the Sentient One
→ Return for the next night
→ The used route seals permanently and spent resources do not respawn
```

Difficulty should come mainly from dwindling options, depleted resources, new threat combinations, altered spaces, and the player's own earlier decisions. It should not rely mainly on enemies receiving arbitrary health or damage increases.

The seven nights should reuse and transform one coherent institution rather than require seven unrelated maps. New rooms, blocked passages, changed lighting, monster territories, route dependencies, and environmental events can make the same place feel progressively less safe.

## Core design pillars

### Movement under pressure

Walking, sprinting, crouching, turning, jumping, vault-like traversal where eventually justified, hiding, and route choice must feel responsive. The player should often survive because they moved well and understood the space, not because a scripted sequence guaranteed safety.

### Dangerous investigation

Progress requires entering rooms, reading the environment, searching containers or surfaces, managing inventory space, and operating objectives. These actions cost attention and time while the world remains dangerous.

### Limited but meaningful information

The game should communicate enough that players can form a plan without reducing every objective to a waypoint. Environmental clues, sounds, landmarks, door states, notes, and teammate communication should matter.

### Scarcity without arbitrary inventory rules

The fixed grid is the carrying limit. Items do not stack. Players may carry duplicates when they physically fit. The full game should not rely on arbitrary category quotas such as “only one medicine” or “only one firearm”; unique items are limited by world scarcity and campaign state instead.

### Distinct threats, not cosmetic variants

Every monster must change how the player moves, listens, looks, hides, or chooses a route. A new monster is only worthwhile when it creates a different decision pattern.

### Costly counterplay, not power fantasy

Some later items may injure, stun, tranquilize, or kill certain monsters, but combat exists to buy survival or preserve a route. Ammunition and strong tools must be scarce enough that avoidance remains the normal answer.

### Cooperative uncertainty

Staying together feels safer, but searching efficiently, carrying large items, reviving a teammate, and preparing multiple route steps can force players apart. Solo play uses the same core systems and remains a supported way to complete the game.

### Persistent consequences

An escape is not a clean reset. Used routes close, consumed supplies remain consumed, unique items can be lost forever, and later nights reveal the cost of earlier choices.

## Scope and technical guardrails

The active repository and its closest `AGENTS.md` remain authoritative for implementation details. The current technical baseline is Unity 6.3 LTS `6000.3.15f1`, Universal Render Pipeline, C#, the Unity Input System, Unity AI Navigation, Photon Fusion 2, Blender/FBX assets, Git LFS, Windows, and Steam as the initial target.

The following guardrails apply across the roadmap:

- Reuse the existing Photon Fusion session and authority model rather than building a second multiplayer stack.
- Shared world state remains host/state-authoritative. Local cameras, input, first-person presentation, cursor state, and inventory presentation remain owner-local.
- Do not introduce Netcode for GameObjects, Photon PUN, Unity Multiplayer Services, a custom backend, dedicated servers, DOTS/ECS, procedural level generation, or another major framework without an explicit design decision.
- Security-camera gameplay and remotely controlled security doors are not part of the current roadmap.
- Do not build a complex generic framework before a concrete version needs it.
- Preserve Vlad's ownership of monster design, modelling, rigging, animation, and environment art. Roadmap milestones may schedule integration, but they do not authorize replacing or substantially redesigning his work.
- Preserve Ian's ownership of programming, technical implementation, gameplay systems, final level layouts, and overall design.
- Prefer one polished institution that changes across the campaign over several shallow maps.
- Prefer a smaller roster of complete, readable items and monsters over a large unfinished catalogue.

## Planning certainty

### Locked decisions

These are established product decisions unless Ian explicitly changes them:

- The spelling is **The Sancturary**.
- The game supports one to four players, with solo play using the same gameplay systems.
- v0.1 is one compact power-restoration escape against the Geo Monster.
- The inventory is a fixed 2-by-5 spatial grid and items do not stack.
- The v0.1 inventory supports keys, the flashlight, and a revival syringe; broad combat is excluded from v0.1.
- The long-term campaign contains seven nights.
- Each night ends with an escape and a Sentient One playback or confrontation.
- A route used to escape is permanently sealed for the remainder of that campaign.
- Spent or lost resources do not simply respawn on the next night.
- The campaign ends with a choice associated with the Sentient One and supports three ending paths: **Yes**, **No**, and **True Hidden / Good**.
- One unique old sawed-off shotgun exists in the entire campaign. It can kill a non-immortal monster with its critical shot at adequate range. Losing it or spending that shot early makes the true hidden ending unavailable for that campaign.

### Working design direction

These ideas are strong enough to guide architecture and content planning, but their exact tuning or release assignment may change:

- Escape-route families include the front entrance, sewer, basement, hidden passages, and other institution-specific exits.
- Later nights introduce new monsters, changed spaces, and companions or transformed people emerging from the Dead Room.
- The full item roster includes medicine, adrenaline, traversal tools, limited firearms, tranquilizer equipment, and the unique sawed-off shotgun.
- The institution contains recognizable functional zones such as reception/treatment, records/administration, patient housing, maintenance/utility spaces, and deeper restricted areas.
- The Geo Monster is the baseline visual hunter. Other planned threats use speed escalation, sound, room occupancy, false hiding places, gaze, or proximity.
- Narrative delivery should remain concise and environmental. Long cutscenes should not repeatedly interrupt the movement-heavy game.

### Decisions intentionally left open

These must be resolved at the stated roadmap gates rather than silently hard-coded now:

- whether multiplayer characters are separate transformed people, multiple manifestations of one victim, or another shared-lore explanation;
- the exact number of escape routes required to sustain seven nights and whether some route families have multiple variants;
- the exact night-by-night order of monsters, routes, map changes, and companion appearances;
- the campaign failure rule: restart the current night, rewind to a checkpoint, or lose the campaign;
- how a co-op campaign save is owned, resumed, and handled when the original host is absent;
- whether later companions are playable characters, narrative presences, scripted helpers, or autonomous NPCs;
- the exact staging of the Sentient One confrontation and the target of the unique shotgun shot;
- the final balance between lethal firearms, tranquilizers, melee tools, and pure avoidance;
- the final launch count of monsters, routes, rooms, items, and narrative collectibles.

## Current foundation on `main`

This is a repository snapshot, not a declaration that any milestone is complete. Every system still needs its milestone exit tests.

- The active gameplay scene is `Assets/Scenes/GrayboxPrototype.unity`, with recent work expanding placeholder rooms and adding an initial environment-dressing pass.
- Photon Fusion session, player spawning, input authority, shared scene flow, and return-to-lobby code are present.
- Player movement, stamina, crouching, health/death presentation, interaction targeting, flashlight handling, and character presentation exist in the project.
- A network-backed 2-by-5 inventory, item definitions/catalogue, drag reorganization, equipment presentation, authoritative pickup/drop, and world item prefabs exist.
- Current item definitions include the flashlight and several keys alongside temporary test items.
- Geo Monster controller, target-selection logic, prefab/Animator assets, and edit-mode target-selection tests exist.
- Locker logic and witnessed locker interactions exist in the project.
- Networked keyed doors, an exit door, an escape scene sequence, light switches, and escape-flow tests exist.
- The newest map commits establish a playable layout and a bare first pass for the intended rooms, but the v0.1 map still needs objective integration, gameplay validation, atmosphere, readability, and final release checks.

The roadmap therefore treats multiplayer, movement, inventory, Geo, lockers, doors, and escape flow as existing foundations to finish and integrate, not as greenfield systems to rebuild.

## Versioning policy

- `v0.0.x` — incomplete development milestones toward the first complete escape.
- `v0.1.0-alpha` — the first complete tiny game.
- `v0.1.x-alpha` — fixes and small improvements that do not expand the core scope.
- `v0.2.0-alpha` through `v0.5.0-alpha` — major playable expansions, each with one primary theme.
- `v0.6.0-beta` — the first end-to-end seven-night campaign skeleton.
- `v0.7.0-beta` and `v0.8.0-beta` — campaign content and narrative completion.
- `v0.9.0-rc` — feature- and content-locked release candidate work.
- `v1.0.0` — the launch build.

Patch releases fix or tune the current milestone. They do not quietly pull in the next milestone's headline system.

## Roadmap overview

| Version | Major theme | What becomes true for the player | Planning certainty |
|---|---|---|---|
| **v0.1** | The first complete escape | One compact multiplayer/solo night can be played from lobby to escape or failure against Geo. | Locked and detailed |
| **v0.2** | One night worth replaying | The vertical slice looks intentional, teaches itself, varies safely between runs, and is suitable for outside playtests. | Detailed plan |
| **v0.3** | Items become survival decisions | Held items, consumables, tools, scarcity, and physical handoffs make inventory choices meaningful. | Planned, moderate detail |
| **v0.4** | Choose how to escape | At least two genuinely different escape routes create route planning and replayability. | Planned, moderate detail |
| **v0.5** | Every monster changes how you move | Threat diversity and costly counterplay make sight, sound, hiding, and room use matter differently. | Directional plan |
| **v0.6** | A week in The Sancturary | Seven nights, route sealing, persistent resources, saving, and Sentient One transitions form one campaign. | Directional architecture |
| **v0.7** | Escalation, transformation, and campaign content | Nights gain distinct threat combinations, route pressure, companions, map changes, and the unique shotgun temptation. | Broad content target |
| **v0.8** | The Sentient One and three endings | The main story, final choice, negative endings, and true hidden/good ending are complete. | Broad narrative target |
| **v0.9** | Release candidate | Content is locked; the game is balanced, optimized, accessible, stable, and packaged for launch. | Release gate |
| **v1.0** | Complete escape-horror game | The full seven-night solo/co-op campaign ships on Windows/Steam. | Definition of done |

---

# v0.1.0-alpha — The First Complete Escape

## Player-facing promise

Players meet in a lobby, enter one compact institution level, discover that the exit has no power, search for required keys, unlock the route to the power room, restore electricity, evade the Geo Monster, revive a fallen teammate when possible, and reach the active exit. When one living player escapes, the team wins.

The successful first-run target remains approximately **20–40 minutes**.

## Complete objective loop

```text
Enter from the lobby
→ Discover that the exit has no power
→ Search the institution for required keys
→ Unlock the route to the power room
→ Restore power
→ Reach the now-active exit
→ One living player escapes
→ Team victory
```

The objective must be understandable through environment, prompts, door states, and concise feedback. A developer should not need to explain the route during a normal playtest.

## Locked gameplay scope

### Session flow

- Host or join through the existing Photon Fusion session flow.
- Load all connected players into the same gameplay scene.
- Support one to four players, including one-player Fusion sessions for solo play.
- End in victory when one living player completes the powered exit.
- End in failure when no living player can continue and no valid revival remains.
- Support restart, disconnect, shutdown, and return to lobby sufficiently for repeated tests.

### Player gameplay

- Responsive walking, sprinting, jumping, crouching, stamina, health, death, flashlight use, and character animation.
- One reusable interaction flow with local prompts and state-authoritative validation.
- A fixed 2-by-5 inventory with variable item footprints.
- No item stacking.
- Pickup, drag reorganization, equipment selection, use where supported, and dropping.
- Keys that unlock the required objective doors.
- One consumed revival syringe that revives a fallen teammate.
- Owner-only first-person item presentation and replicated third-person presentation where required.

### Geo Monster

- Exactly one state-authoritative Geo Monster.
- Patrol valid routes and waypoints.
- Detect visible living players through field of view, distance, and line of sight.
- Chase, maintain or change targets according to its established target rules, lose targets, search or return, attack, and kill.
- Play its intended idle/walk/run/attack/death or kill presentation without duplicate proxy simulation.
- Allow players to escape by route knowledge, line-of-sight breaks, rooms, and lockers.
- Permit one player per locker.
- If Geo witnesses a player entering a locker, allow it to approach and kill that hidden player.

### Level and presentation

v0.1 is not allowed to ship as an undecorated gray box. It does not need final v1.0 art, but it must reach a **functional art and atmosphere pass**:

- one coherent institution wing with connected hallways and rooms;
- recognizable room identities and landmarks;
- objective doors, the power room, exit, keys, item placements, hiding spaces, and valid NavMesh routes;
- purchased environment assets used consistently with Vlad's original work where applicable;
- lighting that supports fear and readability rather than uniformly illuminating every room;
- basic ambient audio, interaction sounds, chase feedback, damage/death feedback, and escape/failure presentation;
- no obvious placeholder cubes or test labels in the normal player path unless they are deliberately retained for an internal build.

## Development milestones before v0.1

### v0.0.1 — Photon Fusion foundation

- Use Photon Fusion 2 as the sole active multiplayer framework.
- Spawn exactly one player object for each joined player with correct input authority.
- Keep local movement responsive through the existing prediction/input architecture.
- Synchronize remote players and necessary presentation.
- Shut down and restart sessions without duplicate players or broken runners.
- Treat existing NGO/Unity Transport remnants only as migration material; do not expand a hybrid architecture.

**Exit condition:** At least two players can host and join, spawn correctly, move in the same test scene, disconnect, return, and begin another session without a session-breaking error.

### v0.0.2 — Player, interaction, and inventory foundation

- Finish movement, stamina, crouching, health, death, flashlight, and animation behaviour.
- Finish the reusable interaction target and prompt flow.
- Finish the authoritative 2-by-5 inventory for pickup, placement, reorganization, equipment, and drop.
- Confirm that world items cannot be claimed twice.
- Confirm that dropped items behave physically on the ground without pushing the character upward, blocking the monster's navigation, or becoming invisible to other players.
- Confirm first-person and third-person equipment visibility rules.

**Exit condition:** Host and client can move, interact, pick up, reorganize, equip, use the flashlight, exchange/drop items, and die without duplication, ownership, or synchronization blockers.

### v0.0.3 — Geo and hiding loop

- Finish patrol, detection, target choice, chase, target loss, attack, kill, and return behaviour.
- Finish animation, sound, and local jumpscare/death presentation.
- Finish locker entry, occupancy, exit, and witnessed-locker kill behaviour.
- Validate NavMesh coverage and prevent ordinary dropped items from blocking navigation.

**Exit condition:** During an online session, Geo can reliably patrol, pursue either player, lose targets, attack, kill, and resolve witnessed locker hiding without proxy AI or state divergence.

### v0.0.4 — Escape, power, and revival loop

- Integrate the lobby-to-level flow into the active map.
- Establish the unpowered exit as the primary objective.
- Place required keys and keyed doors.
- Implement the power-room interaction and authoritative exit activation.
- Implement the revival syringe and authoritative teammate revival.
- Implement team victory, all-players-defeated failure, escape presentation, restart, and return-to-lobby handling.

**Exit condition:** Players can complete the entire experience from lobby to victory or failure without developer intervention.

### v0.0.5 — Stabilization and atmosphere

- Fix major replication, inventory, interaction, AI, locker, revival, objective, and session-reset defects.
- Test simultaneous pickups, simultaneous objective interactions, inventory-full rejection, invalid item use, player death during interaction, client disconnect, host disconnect, and repeated sessions.
- Complete the functional environment-art, lighting, audio, and readability pass.
- Add essential mouse sensitivity, volume, connection status, objective, victory, and failure messaging.
- Produce a clean Windows build.
- Remove temporary task-specific Editor tools and their matching `.meta` files.

**Exit condition:** Complete at least three consecutive multiplayer runs from lobby to ending without a blocker, including at least one run containing a death and revival and one run ending in failure.

## v0.1 release gate

v0.1 is complete only when all of the following are true:

- The full objective chain works in a Windows build without Unity installed.
- Solo, host, and client paths have been tested.
- The active map is visually intentional enough that a player does not describe it as a raw graybox.
- Geo, lockers, keys, power, exit, death, revival, restart, and disconnect handling all survive repeated runs.
- Players receive enough information to understand why the exit is locked and what restoring power accomplished.
- The diff contains no accidental scene, prefab, package, animation, material, imported-asset, network-prefab, or `.meta` changes.
- Known defects are recorded, and no known defect can regularly block completion.

## Explicit v0.1 non-goals

- A second monster or second complete level.
- Firearms, ammunition, melee combat, tranquilizers, or the sawed-off shotgun.
- Additional medicines, adrenaline, crowbars, axes, crafting, combining, durability, weight, containers, or item stacking.
- Campaign saves, checkpoints, persistent world state, route sealing, or the seven-night loop.
- Multiple complete escape routes.
- Procedural generation.
- Steam-specific integration.
- Final lore, cinematics, the Sentient One ending choice, or the hidden ending.
- Final environment polish beyond what is required for one scary and readable run.

---

# v0.2.0-alpha — One Night Worth Replaying

## Major theme

Turn the technically complete v0.1 scenario into an intentional, repeatable horror game that can be shown to outside players without a developer standing beside them.

v0.2 should deepen the existing night rather than immediately adding a new monster, campaign, or large item roster.

## Player-facing additions

- A coherent first production-quality institution wing with strong room identities, visual landmarks, believable transitions, and fewer repeated asset arrangements.
- Improved onboarding through environmental staging, short prompts, readable locks, power-state feedback, and sound rather than a long tutorial.
- Curated run variation through validated spawn sets for keys, the syringe, and selected objective components. The layout itself remains authored rather than procedurally generated.
- More than one sensible search order, so players can split up or reroute after encountering Geo.
- Better chase readability: clear state-change audio, spatially useful footsteps, door and locker feedback, and fewer deaths that feel unexplained.
- A clean lobby, pause flow, rematch/restart flow, settings basics, and end-of-run presentation.
- First-pass balancing for solo, two-player, and larger groups so co-op does not trivialize searching or make required items impossible to locate.

## Design work

- Establish a room-language guide: what kinds of objects, lighting, clutter, and clues communicate patient housing, treatment, records, maintenance, and restricted areas.
- Establish a small set of validated item and objective spawn points. Every possible configuration must be completable.
- Decide which information is shown at the start, which must be discovered, and which is communicated only through teammate observation.
- Tune the balance between required searching and empty-room fatigue.
- Tune Geo's patrol routes and response timing against the dressed map rather than against a pure blockout.

## Technical work

- Add a small authoritative run-configuration layer only for concrete v0.2 variation needs.
- Reset every randomized placement, lock, power state, inventory, death state, locker reservation, and escape flag correctly between runs.
- Add validation that detects impossible spawn combinations before a build is released.
- Improve automated coverage around objective reset, repeated sessions, and escape/failure transitions.
- Profile the active map for obvious lighting, physics, NavMesh, and replication costs before expanding it further.

## Art and audio work

- Bring the first wing through functional art, atmosphere, and readability passes.
- Replace remaining prominent test geometry in the normal route.
- Add a consistent lighting hierarchy: safe-enough navigation light, dangerous darkness, objective emphasis, and monster silhouettes.
- Add room-tone layers, distant institutional sounds, power-state changes, and Geo cues without making the monster perfectly trackable at all times.
- Review environmental work with Vlad rather than overwriting his layouts or assets.

## Exit conditions

- A first-time player can discover and complete the objective without verbal developer instructions.
- At least several blind playtests complete or fail for understandable gameplay reasons rather than confusion or technical defects.
- Curated variation produces different search decisions without producing a soft lock.
- Solo, two-player, and four-player runs remain viable.
- The level sustains tension after players already know the basic power-room solution.
- A public itch.io alpha becomes reasonable at this point, although release timing remains Ian's decision.

## v0.2 non-goals

- Seven-night persistence.
- Multiple finished escape-route families.
- A broad monster roster.
- A full firearm/combat system.
- Large map expansion merely to increase square footage.

---

# v0.3.0-alpha — Items Become Survival Decisions

## Major theme

Expand the inventory from a working grid into the game's main survival-decision system. Items must occupy physical space, be understandable when held, create trade-offs, and remain reliable in solo and multiplayer.

## Player-facing additions

- A reusable held-item presentation system for one-handed and two-handed objects, with owner first-person and replicated third-person representations.
- Item-specific use behaviour rather than treating every inventory object as a key or flashlight.
- A small survival roster selected from the planned med kit, adrenaline, additional objective tools, and traversal tools.
- Physical dropping and handoff between players.
- Curated scarcity: finding two useful objects should create an inventory decision rather than automatically increasing power.
- Clear feedback when an item cannot be used, has been consumed, does not fit, or requires a world target.

## Design rules

- The 2-by-5 grid remains fixed.
- Items do not stack.
- Duplicate items are allowed when their footprints fit.
- Carrying capacity is determined by footprints, not arbitrary category limits.
- A unique campaign item is controlled by world uniqueness and persistent state, not by a general “one firearm” inventory rule.
- Inventory management remains dangerous in real time; opening the inventory must not pause the shared world.
- Non-equippable objects should not pretend to be equippable.
- Tools should solve authored problems and create route choices; they should not become generic keys for every obstacle.

## Technical work

- Separate item definition, inventory instance state, authoritative gameplay effect, first-person presentation, and third-person presentation cleanly enough to support the planned roster without creating one script per trivial variation or one over-generic manager.
- Support consumable use, targeted use on a player or world object, toggled equipment, reload-like actions where later required, and safe interruption by death, drop, disconnect, or scene transition.
- Preserve stable item identifiers across networking and later campaign saves.
- Validate pickup distance, line of sight, ownership, availability, placement, use target, and item consumption on state authority.
- Ensure dropped objects collide with the environment while avoiding player-height exploits and monster path blockage.
- Add tests for duplicate pickup denial, full inventory, item use consumption, drop/repickup, disconnect while holding an item, and repeated scene flow.

## Initial v0.3 content target

The exact subset should be chosen after v0.2 playtests. The likely first additions are:

- med kit;
- adrenaline;
- one route or puzzle tool, such as a crowbar or fire axe, used for a clearly marked authored obstacle;
- finalized revival syringe behaviour;
- finalized flashlight handling; and
- real objective components replacing remaining test items.

Firearms and tranquilizers may use the same item foundation later, but they are not required for v0.3.

## Exit conditions

- Every included item has a complete world, inventory, use, consumption, drop, first-person, third-person, audio, and authority path.
- No supported item requires undocumented prefab or scene setup.
- A player can give another player an item without duplication or lost ownership.
- Inventory choices meaningfully affect at least one complete run.
- The item system is ready to add later firearms and the unique sawed-off without redesigning the entire inventory.

---

# v0.4.0-alpha — Choose How to Escape

## Major theme

Make escape-route choice a real part of the game. The existing powered front-exit scenario becomes one route within a reusable but not over-generalized route structure.

## Player-facing additions

- At least two complete escape routes with meaningfully different preparation and final pressure.
- Environmental evidence near the beginning that establishes that more than one escape may exist, without revealing every step as a checklist.
- Route-specific clues, locks, tools, risks, and spaces.
- The ability to abandon one plan and commit to another when resources, monster pressure, or teammate deaths make the original route unattractive.
- Distinct final sequences rather than every route ending at the same door with a different key.

The first additional route should be chosen based on the active map and available art. Sewer, basement, and hidden-passage concepts are candidates, not promises for this exact version.

## Route design requirements

Every route must define:

- how players first learn it exists;
- what condition prevents immediate use;
- the short chain of clues, keys, tools, or environmental operations required;
- which inventory space and resources it pressures;
- which rooms and monster territories it forces players to enter;
- what can be done in parallel by a team;
- what can fail or become temporarily dangerous without permanently soft-locking the run;
- the final escape interaction and chase pressure; and
- how the route reports success to the shared match state.

## Technical work

- Introduce only the route-state concepts required by the implemented routes: availability, discovered state if needed, objective steps, completion readiness, and escape result.
- Keep route state authoritative and late-join readable.
- Validate all authored route configurations for required objects and impossible states.
- Reset routes correctly between ordinary runs.
- Record which route was used in a form that can later feed the campaign-persistence layer, without implementing the seven-night campaign yet.

## Map work

- Expand or open only the spaces needed to support the selected route.
- Preserve a connected institution with shortcuts and cross-links rather than building isolated objective corridors.
- Ensure each route changes movement and risk, not merely item names.
- Bring new spaces through functional art and atmosphere before calling the route complete.

## Exit conditions

- At least two routes can each be discovered, prepared, and completed in solo and multiplayer.
- Players can identify why one route is currently blocked and what kind of lead they need next.
- No route can consume a required one-off object and leave the run unknowingly impossible.
- Route choice produces different search plans, inventory pressure, and chase locations.
- The existing front-exit route remains stable after the route framework is introduced.

---

# v0.5.0-alpha — Every Monster Changes How You Move

## Major theme

Add threat diversity only after the baseline game, items, and route choice work. New monsters should force different behaviour rather than behave like faster or tougher Geo reskins.

## Planned threat direction

Geo remains the baseline balanced visual hunter. v0.5 should introduce a deliberately small selection from the established roster, likely including:

- one additional mobile hunter that changes movement through speed or sound; and
- one environmental or trap-like threat that changes how players use rooms, lockers, sightlines, or attention.

The exact selection depends on Vlad's art readiness and which behaviours create the clearest contrast in playtests. The full roster is documented later in this roadmap as design reference, not as a promise that all threats ship in v0.5.

## Player-facing additions

- Distinct warnings that allow a knowledgeable player to identify the active threat before dying to it.
- Monster-specific counterplay: slow movement, noise discipline, rapid room decisions, avoiding false lockers, controlling gaze, or another readable response.
- Threat combinations that create tension without producing unavoidable deaths.
- First limited combat or incapacitation options where they improve horror decisions, potentially including a tranquilizer, scarce revolver use, or a tool that works only on selected mortal monsters.
- Consequences for loud or powerful counterplay so that firing a weapon is not a free solution.

## Technical direction

- Reuse shared patrol, target, chase, damage, and presentation code only among monsters that genuinely share those concepts.
- Keep Ethan-, Arjun-, or Adam-like environmental threats as focused mechanics rather than forcing them into a locomotion-AI inheritance hierarchy.
- Run monster decisions on state authority and replicate only the state and presentation required by clients.
- Define a common language for mortal, temporarily vulnerable, resistant, and effectively immortal threats without building an unused combat framework.
- Ensure monster audio, warning, attack, and kill presentation remain local where appropriate and synchronized where other players need to understand the event.

## Balance rules

- Avoidance remains the default answer.
- A weapon should solve an immediate survival problem at the cost of ammunition, noise, inventory space, or future options.
- A killable monster such as Henry must not turn the game into routine target clearing.
- Immune or resistant monsters must communicate that fact without requiring players to waste every scarce item to discover it.
- Co-op players should not be able to permanently stunlock a threat through alternating items.

## Exit conditions

- Each added threat changes player behaviour in blind testing.
- Players can learn at least one reliable warning and one reliable counter for each threat.
- New monsters work in solo and multiplayer without authority divergence.
- Combat or tranquilizer items, if included, are complete from inventory through effects and do not trivialize Geo or route objectives.
- The combined threat set remains readable enough that a death can usually be explained afterward.

---

# v0.6.0-beta — A Week in The Sancturary

## Major theme

Connect the standalone escape game into the first complete seven-night campaign skeleton. This is the largest structural milestone in the roadmap and should not begin until the earlier run-level systems are stable.

## Campaign state

The campaign must persist at least:

- current night;
- routes already used and permanently sealed;
- route-specific world changes;
- consumed, dropped, lost, and surviving important items;
- unique-item existence and ammunition state;
- relevant door, obstacle, power, and puzzle consequences;
- monster or event unlock flags;
- narrative playback flags;
- ending eligibility flags; and
- any companion or transformed-character state required by the final design.

Not every movable prop needs perfect persistence. The persistent state should cover decisions that matter to later nights and avoid saving arbitrary physics noise.

## Campaign flow

- Begin each night in the Dead Room.
- Present enough changed information for players to understand that this is the same institution and the previous escape mattered.
- Allow the team to select a viable plan through exploration rather than a level-select menu.
- End a night only when a valid route is completed or the campaign's failure rule is reached.
- Play a short Sentient One sequence or playback after escape.
- Seal the used route and carry the persistent state into the next night.
- Support save, quit, resume, and campaign restart without an online backend.

## Required design gates before implementation

Before v0.6 campaign code is committed, lock written answers for:

1. the campaign failure/retry rule;
2. campaign save ownership in co-op;
3. expected behaviour when the original host is absent;
4. join-in-progress and reconnect boundaries;
5. the required number of route families or route variants;
6. which item classes persist physically and which persist only as campaign flags;
7. what happens to a dead player between nights; and
8. how companions relate to player slots and multiplayer lore.

A reasonable implementation candidate is a host-owned local campaign save shared through the existing session when resumed, but that remains a decision rather than a locked requirement.

## Content target

v0.6 needs a full seven-night sequence that can be completed end to end, but it does not need final content density, final monster order, or final endings. Temporary route assignments or reduced night variation are acceptable only inside development builds.

The goal is to prove that the campaign cannot become impossible through route sealing, item depletion, disconnects, or save/load errors.

## Exit conditions

- A campaign can progress from Night 1 through Night 7 without manually editing state.
- Used routes remain sealed and meaningful resources remain depleted after save/load.
- A quit and resume returns players to the correct night and campaign state.
- No permitted earlier decision can unknowingly make every future night impossible unless that consequence is an intentional campaign-loss rule.
- Solo and at least one multiplayer campaign can complete the full skeleton.
- A temporary conclusion can stand in for final endings, but all required ending eligibility state is representable.

---

# v0.7.0-beta — Escalation, Transformation, and Campaign Content

## Major theme

Turn the functioning seven-night skeleton into seven nights that feel meaningfully different. This version is content-driven and therefore intentionally less prescriptive than the earlier milestones.

## Broad content goals

- Give each night a distinct combination of open spaces, sealed routes, active objectives, threat behaviour, resource pressure, and environmental change.
- Introduce additional members of the monster roster as their gameplay and Vlad's art are ready.
- Develop companions or transformed people emerging from the Dead Room without assuming autonomous companion AI until that design is chosen.
- Escalate the institution through changed lighting, damaged routes, new sounds, inaccessible safe assumptions, and evidence of the Sentient One's control.
- Place scarce weapons and tools so that using them to simplify one night can damage later campaign options.
- Introduce the unique sawed-off shotgun as a real persistent campaign object with exactly one campaign existence and one critical shot.
- Make the shotgun genuinely tempting to spend on a dangerous late-night escape while preserving it remains necessary for the hidden good ending.

## Night-design principles

- Do not build seven unrelated levels.
- Do not make every night longer than the previous one.
- Do not rely on simply adding every monster at once.
- Reuse learned spaces with changed constraints so player knowledge becomes valuable and then partially unreliable.
- Alternate pressure types: exploration, pursuit, sound discipline, room pressure, deceptive safety, inventory scarcity, and route commitment.
- Preserve at least one understandable plan after setbacks, but allow costly earlier choices to narrow the plan.

## Monster and item integration

The exact night schedule remains open. Content planning should prefer combinations with readable interactions. For example, a hearing-focused hunter can make normally safe sprint routes dangerous, while a fake locker threat can undermine a previously learned hiding habit. Such combinations should be tested before being assigned permanently to a night.

Weapons and tools should be distributed across the campaign as authored strategic resources, not as randomly abundant loot. The strongest items may be entirely absent from many ordinary paths.

## Narrative and companion work

- Establish why later people or transformed companions appear from the Dead Room.
- Decide the multiplayer narrative framing before recording final dialogue or creating ending cinematics.
- Use environmental storytelling and short inter-night sequences to reveal transformation, experimentation, and the Sentient One without explaining every mystery.
- Ensure narrative delivery works for solo and co-op pacing.

## Exit conditions

- All seven nights contain authored, playable content rather than temporary objective assignments.
- Each night has a distinct player-facing identity in playtests.
- Route sealing and resource depletion produce tension without routinely producing unwinnable campaigns.
- The unique shotgun can be found, carried across nights, lost, fired, and correctly reflected in campaign state.
- The planned launch threat set is substantially integrated, although final tuning continues.
- The campaign can be completed repeatedly through more than one route history.

---

# v0.8.0-beta — The Sentient One and Three Endings

## Major theme

Complete the story structure, final choice, and hidden good-ending path without turning the game into a cutscene-heavy experience.

## Story structure

The player has repeatedly escaped, but the Sentient One has continued pulling them back. By the end of the seventh night, the campaign confronts the player with whether they truly want to escape.

The exact writing and staging remain subject to revision, but the established ending meanings are:

### Yes

The player accepts escape. They reach the outside world but are no longer fully human. Human society rejects, captures, experiments on, or destroys them. The apparent escape is not a clean victory.

### No

The player chooses to remain. The institution's monsters no longer attack them, but the player gives up the remaining struggle and gradually loses consciousness, identity, or independent will inside The Sancturary.

### True Hidden / Good

The player preserves the one unique sawed-off shotgun and its critical shot through the entire campaign, discovers the hidden requirements, and uses it during the final confrontation to destroy the Sentient One or the vulnerable core through which it can be killed. This breaks the cycle and creates the genuine good ending.

The exact physical target and final sequence must be finalized with the monster/lore design before implementation. The non-negotiable rule is that using or losing the unique shotgun earlier forfeits this ending for that campaign.

## Hidden-ending fairness rules

- The path should be secret, but clues must exist in the game.
- The player should understand that the shotgun is unusually important without being directly told “save this for the good ending.”
- The game must never duplicate or silently restore the unique weapon.
- Losing it through a bug, scene transition, disconnect, or save corruption is unacceptable.
- Firing it early is an intentional consequence and should persist.
- The final requirement must not depend on an undocumented input, pixel hunt, or arbitrary timing window.
- Co-op players must receive one consistent ending based on shared campaign state.

## Narrative presentation

- Prioritize short playbacks, changed spaces, monster behaviour, environmental evidence, and player-controlled movement.
- Use longer non-interactive sequences only where they materially improve the ending.
- Make the three endings emotionally and visually distinct.
- Ensure the negative endings feel like meaningful consequences rather than joke failures.
- Ensure the true ending resolves the central campaign conflict while leaving room for mystery.

## Exit conditions

- All three endings are reachable through intended play.
- Ending eligibility survives save/load, disconnect, and seven-night progression.
- The unique shotgun path is fair, deterministic, and impossible to duplicate.
- The final choice and hidden ending work in solo and co-op.
- Final narrative text, voice, animation, and environment needs are identified and scheduled with Vlad where they depend on his work.
- The campaign can be played start to finish with no temporary story screens.

---

# v0.9.0-rc — Content Lock and Release Candidate

## Major theme

Stop expanding. Finish, balance, optimize, test, and package the game that already exists.

No new monster, route family, major item system, campaign mechanic, or ending should enter v0.9 unless it replaces a cut feature that is required to make the existing game completable.

## Content lock

- Finalize the launch set of rooms, routes, monsters, items, notes, playbacks, companions, and endings.
- Remove or hide unused test content from release builds.
- Confirm all third-party asset and audio licences.
- Freeze save-data schema except for critical fixes.
- Freeze major scene and prefab restructuring.

## Balance and usability

- Tune search time, objective clarity, route difficulty, inventory pressure, item scarcity, revive availability, monster warnings, chase duration, and campaign attrition.
- Balance solo, two-player, and four-player play without requiring separate game rules whenever avoidable.
- Add or finish key rebinding, sensitivity, audio controls, brightness/gamma guidance, subtitle support where dialogue requires it, readable UI scaling, and motion/camera comfort options appropriate to the game.
- Test colour and audio-dependent clues so essential progress has more than one readable signal.
- Ensure players can recover from common mistakes without eliminating meaningful consequences.

## Technical hardening

- Validate every shared gameplay action from host and client perspectives.
- Test repeated session creation, reconnect boundaries, campaign load/resume, client departure, host departure, and return to menu.
- Profile CPU, GPU, memory, garbage collection, lighting, animation, NavMesh, physics, and network traffic in the final map.
- Set and verify minimum/recommended PC targets before final optimization claims.
- Remove development-only logs, cheats, test objects, temporary tools, and duplicate legacy paths.
- Audit packages and keep only justified dependencies.
- Verify Git LFS assets and a clean clone/build workflow.
- Produce reproducible Windows builds using Unity `6000.3.15f1`.

## QA matrix

At minimum, release-candidate testing must cover:

- solo, two-player, and four-player campaigns;
- every escape route and important route combination;
- every monster alone and in intended combinations;
- every item pickup, use, drop, loss, and persistence path;
- death, revival, all-player failure, restart, and campaign reset;
- save and load at each allowed boundary;
- the Yes, No, and True Hidden / Good endings;
- the shotgun preserved, lost, dropped, transferred, and fired early;
- lobby creation, join, failed join, disconnect, and repeated sessions;
- low and high graphics settings on representative hardware;
- keyboard/mouse settings and supported display resolutions;
- a build installed and played on a machine without the Unity project.

## Release preparation

- Final title/menu flow, credits, licences, version display, and legal text.
- Steam build configuration and store/release assets when Ian decides to proceed.
- Crash and bug-report instructions that do not require adding an unnecessary backend.
- A launch trailer, screenshots, and store description only after final visuals are representative.
- A final itch.io or Steam playtest build before launch.

## Exit conditions

- Multiple full campaigns complete across solo and multiplayer without progression blockers.
- No known common-path crash, save corruption, item duplication, impossible route, or ending-state defect remains.
- Performance meets the written target on the chosen minimum-spec machine.
- Blind players can understand the core objective and campaign flow without developer coaching.
- The final art, animation, sound, UI, and narrative are present.
- The release branch contains no accidental assets or unrelated changes.
- Remaining known issues are documented and acceptable for launch.

---

# v1.0.0 — Launch Definition

v1.0 is complete when The Sancturary is a coherent game rather than a collection of promising systems.

A launch player can:

- play alone or host/join a one-to-four-player session;
- begin a seven-night campaign in the Dead Room;
- explore one coherent, changing abandoned institution;
- understand environmental clues and choose among viable escape plans;
- manage a fixed 2-by-5 non-stacking inventory;
- use medicine, tools, equipment, and scarce combat counterplay included in the final roster;
- evade a set of distinct original monsters with learnable warnings and counters;
- survive chases through movement, map knowledge, hiding, teamwork, and costly item use;
- escape once per night while permanently sealing the used route;
- experience persistent depletion and consequences across all seven nights;
- encounter the Sentient One's escalating control and story;
- reach the Yes or No ending through the final choice; and
- discover the True Hidden / Good ending by preserving and correctly using the unique sawed-off shotgun.

The launch build must be stable, performant, readable, licensed, packaged for Windows, and suitable for Steam release. The exact count of routes, monsters, items, and rooms remains subordinate to this quality bar. Cutting a weak or unfinished piece of content is better than shipping it merely to satisfy a number in an old plan.

Post-launch content, additional campaigns, dedicated-server work, console ports, workshop support, procedural generation, security-camera roles, or new networking architecture are outside this v1.0 roadmap unless explicitly approved later.

---

# Design Reference: Planned Item Roster

This table records the current drafted full-game roster. It is not a promise that every item ships, and dimensions may change after inventory playtests. No item stacks.

| Item | Draft footprint | Intended role | Current roadmap position |
|---|---:|---|---|
| Generic or route key | 1x1 | One-use objective access for its matching lock. | v0.1 onward |
| Flashlight | 1x1 | Equippable exploration light; left-click use while held. | v0.1 onward |
| Revival syringe | 2x1 | Consumed to revive a fallen teammate. | v0.1 onward |
| Med kit | 1x1 | Full or substantial heal; exact final healing value is tunable. | Candidate v0.3 |
| Adrenaline | 1x1 | Roughly 30 seconds of unrestricted or greatly extended sprint. | Candidate v0.3 |
| Fire axe | 2x3 | Large traversal/tool item; breaks marked barriers and may provide costly melee counterplay. | v0.3–v0.5 candidate |
| Crowbar | 2x3 | Opens selected vents, boards, or authored obstacles. | v0.3–v0.4 candidate |
| Old revolver | 2x2 | Scarce lethal emergency weapon against mortal threats. | v0.5+ direction |
| Revolver reload / speedloader | 1x1 | Separate non-stacking reload resource. | v0.5+ direction |
| Tranquilizer gun | 2x4 | Large non-lethal weapon that temporarily disables eligible monsters. | v0.5+ direction |
| Tranquilizer darts | 1x1 | Separate non-stacking tranquilizer ammunition. | v0.5+ direction |
| Old sawed-off shotgun | 2x4 | One unique campaign weapon; one-shots non-immortal monsters at adequate range and is required for the true ending. | v0.7–v0.8 |
| Unique shotgun shell / critical shot | 1x1 or weapon-contained | One campaign-critical shot; exact inventory representation remains open. | v0.7–v0.8 |
| Puzzle components | Variable | Fuses, overrides, mechanical parts, or route-specific tools. | Added only with concrete routes |

Item rules that should remain true unless explicitly redesigned:

- The grid footprint is the main carrying constraint.
- Items do not stack.
- Ammunition and reload objects occupy space.
- Powerful items are scarce in the world rather than blocked by arbitrary inventory categories.
- Large tools compete directly with medicine, ammunition, and objective items.
- A dropped item remains a real world object and may be lost.
- The unique shotgun can exist only once per campaign.

# Design Reference: Planned Monster Roster

Monster names and concepts come from the original project and team discussions. Vlad retains ownership of their visual design, modelling, rigging, and animation. Exact launch inclusion and night assignment remain subject to gameplay and art readiness.

| Monster | Core behaviour | Behaviour it demands from the player |
|---|---|---|
| **Geo** | Balanced visual patrol/chase monster and baseline hunter. | Break sight, use routes and doors, hide intelligently, and never enter a locker while witnessed. |
| **Henry** | Mortal, killable fodder-like pursuer that may appear in greater numbers. | Decide whether scarce combat resources are worth spending instead of simply running. |
| **Ian** | Mobile hunter whose speed increases over time or during pursuit. | End chases quickly, break tracking early, and avoid assuming a long straight sprint remains safe. |
| **Yossef** | Hearing-focused or hearing-only hunter with exceptional sound detection. | Control sprinting, doors, dropped objects, gunfire, and teammate noise. |
| **Ethan** | Room-pressure threat that appears or attacks when players remain in one space too long. | Investigate efficiently and avoid treating a cleared room as permanent safety. |
| **Arjun** | Fake locker or hiding-place mimic/trap. | Read subtle warnings and stop treating every locker as trustworthy. |
| **Adam** | Gaze- or proximity-driven threat that punishes prolonged looking or unsafe closeness. | Control attention, camera direction, and spacing rather than relying only on sprint speed. |
| **The Sentient One** | Campaign controller and final antagonist behind the repeated return. | Understand the cycle, preserve the unique weapon, and make the final campaign choice. |

Roster rules:

- Geo is completed first and remains the reference quality bar.
- A monster is not added only because a model exists.
- Every threat needs a trigger, warning, pursuit or attack rule, counterplay, failure result, animation/audio language, multiplayer authority model, and testing plan.
- Mortal, resistant, and effectively immortal monsters must be communicated consistently.
- Environmental threats should not be forced into the same architecture as navigation-based hunters.
- Exact stats and night assignments remain tunable until campaign balance is proven.

# Map Production Roadmap

The map should advance through deliberate passes. v0.1 must move beyond a raw blockout, but final polish is reserved for later versions.

## Pass 1 — Flow blockout

- Establish major zones, loops, dead ends, shortcuts, hiding spaces, chase turns, and objective distances.
- Validate player scale, movement clearance, sightlines, and basic NavMesh.
- Use simple geometry only as long as it remains useful for changing layout quickly.

## Pass 2 — Gameplay integration

- Place doors, locks, objective anchors, route steps, item spawn sets, lockers, monster patrol points, chase recoveries, and escape triggers.
- Validate solo and co-op flow.
- Remove layout soft locks and monster navigation traps.

## Pass 3 — Functional art pass

- Replace prominent blockout with purchased institution assets and Vlad's work where applicable.
- Give rooms recognizable identities and believable functions.
- Add essential collision, lighting, and prop placement.
- Reach this pass for the full v0.1 player route.

## Pass 4 — Atmosphere and readability

- Shape darkness, contrast, landmarks, environmental storytelling, sound zones, decals, clutter, and monster silhouettes.
- Emphasize interactable or important states without making them look like arcade pickups.
- Reach an initial version of this pass in v0.1 and a stronger production version in v0.2.

## Pass 5 — Campaign variation

- Add route-specific spaces, sealed-night changes, destroyed or opened passages, altered room use, threat territories, and later-night storytelling.
- Develop mainly across v0.4 through v0.7.

## Pass 6 — Final polish and optimization

- Final lighting, prop density, collision cleanup, occlusion where appropriate, LODs, light baking strategy, reflection/probe work, NavMesh validation, audio polish, and performance cleanup.
- Complete during v0.8 and v0.9.

A larger map is not automatically a better map. New space is justified when it supports a route, monster behaviour, campaign consequence, narrative reveal, or important pacing need.

# Cross-Version Production Requirements

## Multiplayer and authority

Every shared feature must define who owns the state, what clients may request, what state is replicated, how late join or reconnect sees it, and what happens when a player disconnects while using it. Host behaviour alone never proves multiplayer correctness.

## Save and campaign persistence

Campaign persistence begins only when v0.6 requires it. Stable item IDs, route IDs, and narrative flags should be preserved before then, but no speculative universal save framework should be built early.

## Art integration

Do not replace Vlad's monsters or environment work to satisfy a code milestone. When an art dependency is not ready, use a clearly temporary integration stand-in on a development branch, document it, and remove it before the milestone release.

## Audio

Audio is gameplay information, especially for Geo, Yossef-like hearing mechanics, doors, power state, pursuit, hiding, and route machinery. It must be tested for readability without making threats perfectly trackable.

## Accessibility and comfort

The final game should support adjustable sensitivity, volume, brightness guidance, readable UI, subtitles where needed, and reasonable camera/motion options. Essential objective information should not depend only on colour or one subtle sound.

## Testing discipline

For every meaningful implementation:

1. inspect the current code, scene, prefab, call sites, and authority rules;
2. make the smallest coherent change that completes the requested v0.x behaviour;
3. run relevant static tests, edit-mode/play-mode tests, compilation, or Unity `6000.3.15f1` validation when available;
4. manually test the player-facing result in Unity;
5. test both host and client when shared state changes;
6. review the diff for accidental scene, prefab, package, animation, material, imported-asset, and `.meta` changes; and
7. record exact manual checks that remain.

## Milestone release checklist

Every `v0.x.0` milestone should include:

- a written scope and exit condition;
- a clean build from the pinned Unity version;
- a changelog or release summary;
- automated tests where they provide useful confidence;
- documented solo and multiplayer manual test coverage;
- a pass for progression blockers and soft locks;
- a pass for accidental asset and package changes; and
- a clear list of known issues and cuts.

# Scope-Cut Rules

When a version is slipping, cut in this order:

1. optional content variants;
2. duplicate rooms or cosmetic props;
3. secondary item types;
4. a weak or redundant monster;
5. a route that does not create distinct decisions;
6. nonessential narrative collectibles; and
7. polish that can safely move to the next patch.

Do not cut the version's headline theme, the complete escape loop, basic readability, state authority, save integrity once introduced, or the ability to finish the game.

The roadmap succeeds when each version leaves behind a more complete game, not merely more code.