# The Sancturary Roadmap

## Version target

The first complete playable release is **v0.1.0-alpha**.

Development builds before that release use **v0.0.x** versions. A version should only advance when its exit condition is met.

## v0.1.0-alpha goal

Deliver one small but complete two-player escape-horror experience:

> Enter one abandoned institution level, explore under pressure, solve a short objective chain, evade one roaming monster, unlock the final route, and escape.

The target playtime for a successful first run is roughly **20–40 minutes**.

## Core design pillars

- **Movement under pressure:** walking, sprinting, crouching, pursuit, and route choice must feel responsive.
- **Dangerous investigation:** players must slow down or stop to inspect clues and operate objectives while the monster remains a threat.
- **Cooperative uncertainty:** staying together feels safer, but some objectives should reward splitting up or covering different areas.
- **Readable escape structure:** players should understand what blocks the exit and gradually learn the level layout.
- **One strong monster:** one reliable and memorable threat is more valuable than several unfinished enemies.

## Milestones

### v0.0.1 — Multiplayer foundation

- Keep Netcode for GameObjects and Unity Transport.
- Support a two-player host/client session.
- Make local player movement immediately responsive.
- Synchronize remote players with interpolation.
- Keep monsters, puzzle state, pickups, doors, scene loading, and escape state host-authoritative.
- Provide a reliable disconnect and return-to-menu flow.
- Keep direct IP as a development fallback; add a simpler join-code or platform session flow only after the base connection is stable.

**Exit condition:** Two players can join over the internet, move through the same test scene, and disconnect without severe rubber-banding or session-breaking errors.

### v0.0.2 — Player gameplay foundation

- Walk, sprint, stamina, crouch, and flashlight.
- One reusable interaction system with a clear prompt.
- Open and close doors.
- Pick up, carry, use, and drop required objective objects.
- Basic player death and session failure handling.

**Exit condition:** Both players can interact with shared gameplay objects without duplication, ownership, or synchronization blockers.

### v0.0.3 — Monster prototype

- One host-authoritative monster prefab.
- Patrol, detection, chase, target switching, target loss, and recovery behaviour.
- Attack and kill players.
- Basic chase audio and visual feedback.
- Use a clearly temporary placeholder until Vlad's final model, rig, animations, and monster presentation are ready.

**Exit condition:** The monster can reliably detect and pursue either player during an online session without major navigation or synchronization failures.

### v0.0.4 — Complete escape loop

Build one compact level with:

- A safe but brief starting area.
- Several connected rooms and corridors.
- One clear final exit blocker.
- A short chain of clues, keys, switches, or carried objects.
- At least one shortcut or loop that rewards learning the map.
- A final escalation or chase after the exit route is unlocked.
- Escape, failure, and restart states.

Example structure:

```text
Explore
→ Discover what blocks the exit
→ Find clues and required objects
→ Complete the objective chain
→ Trigger the final escalation
→ Reach the exit
→ Escape
```

**Exit condition:** Two players can complete the experience from the main menu to an ending without developer intervention.

### v0.0.5 — MVP stabilization and atmosphere

- Fix major replication, interaction, AI, and progression bugs.
- Prevent duplicate pickups and conflicting objective actions.
- Test player death, both players dying, client disconnect, host disconnect, restart, and repeated sessions.
- Add enough lighting, sound, environmental dressing, and monster presentation for the level to feel intentionally scary.
- Add essential sensitivity, volume, connection-status, and failure messaging.
- Produce a clean Windows build.
- Remove task-specific temporary editor tools and their `.meta` files.

**Exit condition:** Complete at least three consecutive two-player runs without a blocker.

## v0.1.0-alpha release requirements

The release includes:

- Two-player online cooperative play.
- One compact asylum or Sancturary level.
- One monster.
- One complete objective and puzzle chain.
- One escape ending.
- Responsive movement, flashlight, and shared interactions.
- Functional monster AI, audio, lighting, failure, restart, and disconnect handling.
- A Windows build that can be sent to another player and tested without Unity installed.

## Explicit non-goals for v0.1.0-alpha

- More than two players.
- Multiple levels or monsters.
- Weapons or combat-focused gameplay.
- Reviving, checkpoints, or a complex spectator system.
- Inventory grids or large item collections.
- Procedural generation.
- Matchmaking browsers.
- Steam-specific integration.
- Save files or persistent progression.
- Final lore, cinematics, or extensive story delivery.
- Final character art or complete environment polish.

## Release discipline

- **v0.0.x:** incomplete development milestones.
- **v0.1.0-alpha:** the first complete tiny game.
- **v0.1.x-alpha:** fixes and small improvements that do not change the core scope.
- **v0.2.0-alpha:** the next meaningful gameplay or content expansion.

The project should not begin a second level, second monster, or major new system until the v0.1.0-alpha loop is playable, scary, and stable enough to finish repeatedly.