# The Sancturary

> Working title for a movement-heavy, single-player first-person escape-horror game set inside an abandoned institution.

## Overview

The player is trapped inside an abandoned institution and must find a way out while a monster hunts them. Progress comes from searching rooms, interpreting environmental clues, collecting keys and useful items, and solving a short chain of puzzles that opens the escape route.

The game should feel tense because the player must keep moving, choose routes quickly, and decide when it is safe to investigate. The monster creates pressure, but exploration and puzzle solving—not combat—drive the game forward.

The project is a 3D reimagining of an earlier 2D prototype created by Ian Zhang and Vlad Ign.

## Core experience

- Responsive first-person movement built for exploration and chases
- Sprinting with limited stamina and crouching with reduced speed
- Fatigue communicated through effects such as a vignette and breathing, without a permanent stamina bar
- A compact, interconnected environment with alternate paths and recognizable landmarks
- Environmental clues, keys, and simple inventory items
- A small sequence of readable puzzles that gradually unlocks the escape route
- One monster that patrols, investigates, searches, and chases
- Limited visibility, environmental storytelling, and reactive audio
- A complete 10–15 minute escape run

## Core loop

1. Explore the building and identify blocked routes.
2. Find clues, keys, and puzzle items.
3. Solve a puzzle or unlock a new area.
4. Evade or hide from the monster when it approaches.
5. Use movement and map knowledge to reach the next objective.
6. Open the final escape route and get out.

## Initial prototype scope

The first complete playable version should contain:

- One compact graybox building wing with a few connected routes
- One player with polished walking, looking, sprinting, stamina, and crouching
- A simple interaction and small-item inventory system
- Two or three linked clues, keys, or environmental puzzles
- One monster with patrol, investigate, search, and chase states
- One final locked escape route
- Clear win and loss states
- A complete 10–15 minute gameplay loop

The current prototype does not need multiplayer, security-camera gameplay, remotely operated doors, multiple monsters, a large map, procedural generation, combat, or a custom backend. These ideas can be reconsidered only after the smaller escape loop is fun.

## Technology

- Unity 6.3 LTS, pinned to 6000.3.15f1
- Universal Render Pipeline
- C#
- Unity Input System
- Unity AI Navigation
- Blender and FBX for characters and environments
- GitHub and Git LFS
- Windows and Steam as the initial target

## Team

- Ian Zhang — programming, gameplay systems, technical implementation, and overall design
- Vlad Ign — monster design, 3D modelling, rigging, animation, and environment art

## Status

Early pre-production. The immediate goal is to prove a small, scary escape loop in graybox: movement feels good, clues and puzzles are understandable, the monster creates pressure, and the player can complete one satisfying run from entry to escape.
