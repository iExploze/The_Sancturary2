# The Sancturary Roadmap

## Version target

The first complete playable release is **v0.1.0-alpha**.

Development builds before that release use **v0.0.x** versions. A version should only advance when its exit condition is met.

## v0.1.0-alpha goal

Deliver one small but complete cooperative escape-horror experience:

> You and your friends enter one abandoned institution level, search for keys, unlock the power room, restore power to the exit, evade the Geo Monster, revive fallen teammates when possible, and escape.

The target playtime for a successful first run is roughly **20–40 minutes**.

## Core design pillars

- **Movement under pressure:** walking, sprinting, crouching, pursuit, and route choice must feel responsive.
- **Dangerous investigation:** players must slow down or stop to search rooms, inspect objectives, and manage items while the monster remains a threat.
- **Cooperative uncertainty:** staying together feels safer, but searching for keys and helping downed teammates may force players to split up.
- **Readable escape structure:** players should understand that the exit has no power, discover the route to the power room, restore power, and escape.
- **One strong monster:** one reliable and memorable Geo Monster is more valuable than several unfinished enemies.

## Locked v0.1 gameplay scope

### Session flow

- Players meet in a lobby, host or join through Photon Fusion, and load into the playable level together.
- The first v0.1 release is designed around cooperative multiplayer while preserving solo testing through the same Fusion gameplay systems.
- When one living player reaches the powered exit, the run ends in victory for the team.

### Player gameplay

- Walk, sprint, crouch, jump, stamina, health, death, flashlight use, and character animation.
- A fixed **2-by-5 grid inventory**.
- Items may occupy multiple grid cells and can only be placed when their full footprint fits inside the grid without overlapping another item.
- Items do not stack.
- Players can pick up, organize, use, and drop supported items.
- The only planned usable support item for v0.1 is a **revival syringe**, which can revive a fallen teammate and is consumed when used.
- Keys are objective items used to unlock required doors.

### Geo Monster

- One host/state-authoritative Geo Monster with its character model and animations.
- Patrol hallways and valid navigation routes.
- Detect visible players, chase them, switch or lose targets, attack, and kill players.
- Players may escape pursuit by breaking line of sight, entering rooms, or hiding in lockers.
- Each locker allows only one player inside at a time.
- If the Geo Monster sees a pursued player enter a locker, it can approach that locker and kill the hidden player.

### Escape objective

The complete v0.1 objective chain is:

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

The level may use simple keyed doors and environmental routing to make finding the power room dangerous. Additional puzzle systems are not required for v0.1.

### Level and presentation

- One compact institution level built primarily with the purchased environment assets and Vlad's original art where applicable.
- Connected hallways and rooms that support searching, pursuit, route choice, and hiding.
- Player and Geo Monster character models and animations.
- Enough lighting, sound, environmental dressing, interaction feedback, and objective communication for the level to feel intentionally playable and scary.
- Escape, failure, restart, disconnect, and return-to-lobby handling sufficient for repeated multiplayer runs.

## Milestones

### v0.0.1 — Photon Fusion multiplayer foundation

- Use Photon Fusion 2 as the authoritative multiplayer framework.
- Host and join through Photon sessions rather than direct IP, Unity Relay, NGO lobby, or Unity Transport flows.
- Spawn exactly one Fusion player object per joined player with the correct input authority.
- Keep local player movement immediately responsive through Fusion input prediction.
- Synchronize remote players and presentation through Fusion replication and interpolation.
- Provide a reliable disconnect, shutdown, and return-to-menu flow that supports repeated sessions.
- Treat existing Netcode for GameObjects and Unity Transport code, prefabs, packages, and scene setup as legacy migration material. Do not expand them or leave a permanent NGO/Fusion hybrid.

**Exit condition:** At least two players can host and join the same Photon session, spawn correctly, move through the same test scene, disconnect, and start another session without duplicate players or session-breaking errors.

### v0.0.2 — Player gameplay and inventory foundation

- Complete movement, stamina, crouching, health, death, flashlight, and character animation.
- Use one reusable interaction system with a clear local prompt.
- Open and close doors.
- Complete the fixed 2-by-5 non-stacking inventory.
- Pick up, organize, use, and drop supported objective and support items.
- Keep shared item ownership and world state authoritative.

**Exit condition:** Players can move, interact, and use the inventory from both host and client perspectives without duplication, ownership, or synchronization blockers.

### v0.0.3 — Geo Monster and hiding loop

- Complete the host-authoritative Geo Monster patrol, detection, chase, target loss, attack, and kill behaviour.
- Complete basic chase audio, animation, and visual feedback.
- Complete one-player locker occupancy.
- Complete witnessed locker entry and locker kill behaviour.

**Exit condition:** The Geo Monster can reliably patrol, pursue either player, lose targets correctly, attack players, and handle witnessed locker hiding during an online session.

### v0.0.4 — Complete escape and revival loop

- Build the lobby-to-level flow.
- Establish the unpowered exit as the main objective.
- Place the required keys and keyed doors leading to the power room.
- Implement the power interaction and activate the exit after power is restored.
- Implement the revival syringe and teammate revival.
- Implement team victory when one living player escapes.
- Implement failure and restart handling when recovery is no longer possible.

**Exit condition:** Players can complete the experience from lobby to victory or failure without developer intervention.

### v0.0.5 — MVP stabilization and atmosphere

- Fix major replication, interaction, inventory, AI, revival, and progression bugs.
- Prevent duplicate pickups, conflicting objective actions, and invalid locker occupancy.
- Test player death, revival, all players being defeated, client disconnect, host disconnect, restart, and repeated sessions.
- Add enough lighting, sound, environmental dressing, and monster presentation for the level to feel intentionally scary.
- Add essential sensitivity, volume, connection-status, objective, victory, and failure messaging.
- Produce a clean Windows build.
- Remove task-specific temporary editor tools and their `.meta` files.

**Exit condition:** Complete at least three consecutive multiplayer runs from lobby to ending without a blocker.

## v0.1.0-alpha release requirements

The release includes:

- Cooperative online play through Photon Fusion 2 sessions.
- A lobby-to-game session flow.
- One compact asylum or Sancturary level.
- One fully functional Geo Monster.
- One fixed 2-by-5 non-stacking inventory.
- Keys and keyed doors leading to the power room.
- One revival syringe support item.
- Lockers and rooms used as hiding options.
- One complete power-restoration objective chain.
- One escape ending triggered when one living player reaches the powered exit.
- Responsive movement, character animation, flashlight, shared interactions, death, and revival.
- Functional monster AI, audio, lighting, failure, restart, and disconnect handling.
- A Windows build that can be sent to another player and tested without Unity installed.

## Explicit non-goals for v0.1.0-alpha

- A second level or second monster.
- Weapons or combat-focused gameplay.
- Additional medicines, ammunition, crafting, item combining, durability, weight, containers, or large item collections.
- Inventory expansion beyond the fixed 2-by-5 grid.
- Item stacking.
- Checkpoints, persistent progression, save files, or a complex spectator system.
- Procedural generation.
- Matchmaking browsers.
- A permanent NGO/Fusion hybrid or new direct-IP, Unity Relay, or Unity Transport session path.
- Steam-specific integration.
- Final lore, cinematics, or extensive story delivery.
- Final environment polish or content beyond what is necessary for one complete, scary escape run.

## Release discipline

- **v0.0.x:** incomplete development milestones.
- **v0.1.0-alpha:** the first complete tiny game.
- **v0.1.x-alpha:** fixes and small improvements that do not change the core scope.
- **v0.2.0-alpha:** the next meaningful gameplay or content expansion.

The project should not begin a second level, second monster, major inventory expansion, combat system, or other major new feature until the v0.1.0-alpha loop is playable, scary, and stable enough to finish repeatedly.
