# The Sancturary

The Sancturary is a cooperative first-person escape-horror game set inside an abandoned institution containing strange original monsters that were designed and confined there.

You and your friends are trapped inside. The goal is simple: survive, restore power, and escape.

## v0.1.0-alpha

The first playable release is one compact multiplayer escape scenario.

Players meet in a lobby, enter the institution together, discover that the exit has no power, search rooms for keys, unlock the route to the power room, restore electricity, and reach the active exit while the Geo Monster patrols and hunts them. When one living player escapes, the team wins.

### Player gameplay

- Responsive first-person walking, sprinting, jumping, crouching, stamina, health, death, flashlight use, and character animation.
- A fixed 2-by-5 grid inventory.
- Items can only be placed when their complete grid footprint fits without overlapping another item.
- Items do not stack.
- Players can pick up, organize, use, and drop supported items.
- Keys unlock the doors required to reach the power room.
- A revival syringe can be used to revive a fallen teammate.

### Geo Monster

The v0.1 threat is the Geo Monster, with its own character model and animations. It patrols the hallways, detects players, chases them, loses targets when they escape its awareness, attacks, and kills.

Players can break line of sight, hide in rooms, or enter lockers. Each locker holds only one player. A player is not safe if the Geo Monster sees them enter: it can approach the witnessed locker and kill the hidden player.

### Level and objective

The v0.1 level is built primarily with purchased institution environment assets, alongside original work from the team. It contains connected hallways and rooms designed for searching, hiding, pursuit, and route choice.

The complete objective loop is:

```text
Enter from the lobby
→ Discover that the exit has no power
→ Search for the required keys
→ Unlock the route to the power room
→ Restore power
→ Reach the active exit
→ Escape
```

The first release focuses on completing this one loop reliably and making it scary. It does not include weapons, crafting, item stacking, multiple levels, multiple monsters, large item collections, checkpoints, or persistent progression.

## Inspiration

The game is inspired by the pursuit and hiding of *Outlast* and the exploration, inventory pressure, and environmental progression of *Resident Evil*.

## Development

The Sancturary is being developed in Unity 6.3 LTS using editor version `6000.3.15f1`, Universal Render Pipeline, C#, the Unity Input System, Unity AI Navigation, and Photon Fusion 2.

It is a 3D reimagining of an unfinished 2D prototype created by Ian Zhang and Vlad Ign. Ian owns programming, gameplay systems, level design, and overall design decisions. Vlad owns Blender work, original monster design, modelling, rigging, animation, and environment art.

See [ROADMAP.md](ROADMAP.md) for the locked v0.1 scope and milestone exit conditions.
