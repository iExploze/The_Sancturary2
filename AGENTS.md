# AGENTS.md

## Project

The Sancturary is a first-person 1–4 player co-op horror game made in Unity. Players explore an abandoned institution, complete objectives, use security cameras and remote doors, evade monsters, and escape.

The project is currently in pre-production. Prefer small, testable prototypes over production-scale systems.

## Technology

- Unity 6.3 LTS
- Universal Render Pipeline
- C#
- Unity Input System
- Unity AI Navigation
- Netcode for GameObjects
- Unity Multiplayer Services
- Blender and FBX for 3D assets
- Git and Git LFS

Do not introduce a new framework, package, online service, or architectural pattern without explaining the need and receiving approval.

## Development principles

- Keep implementations simple, modular, and readable.
- Build the smallest complete version of a feature before expanding it.
- Multiplayer is a core requirement. Avoid designs that would be difficult to make host-authoritative later.
- The host owns monster AI, damage, doors, objectives, random events, and match results.
- Clients own local input and presentation but request gameplay actions from the host.
- Separate gameplay rules from Unity presentation code when practical.
- Use ScriptableObjects for reusable design data such as monster statistics and item definitions.
- Prefer explicit state machines for monster behaviour until a more complex solution is demonstrably needed.
- Do not optimize without evidence from profiling.
- Do not add dedicated-server infrastructure, procedural generation, DOTS/ECS, or a custom backend during the prototype phase.

## Unity safety

- Do not manually rewrite Unity scene, prefab, animation, or `.meta` YAML files unless the task explicitly requires it and the change can be verified.
- Prefer C# components or editor scripts for repeatable setup work.
- Preserve asset GUIDs and always retain `.meta` files.
- Do not modify files under `ArtSource/` unless the task is specifically about the Blender source pipeline.
- Do not claim a scene or visual change works unless it was verified in Unity or the limitation is clearly stated.

## Code conventions

- Use clear descriptive names; avoid abbreviations that are not established in the project.
- Keep one primary public type per C# file and match the filename to the type name.
- Prefer composition over deep inheritance.
- Avoid global mutable state and unnecessary singletons.
- Keep network transport code separate from gameplay rules where possible.
- Comment design intent and non-obvious constraints, not straightforward syntax.
- Add EditMode tests for deterministic gameplay logic when practical.
- Add PlayMode tests for important Unity interactions when the test is stable and valuable.

## Working with the team

- Ian owns programming, gameplay systems, networking, and technical integration.
- Vlad owns Blender source files, monster modelling, animation, and environment art.
- Avoid editing the same Unity scene or prefab as another contributor at the same time.
- Treat exported FBX files as game inputs and `.blend` files as art source files.
- Keep changes focused so they are easy to review and revert.

## Agent workflow

Before changing code:

1. Inspect the relevant files and nearby conventions.
2. State any assumption that materially affects the implementation.
3. Make the smallest coherent change that solves the requested task.

After changing code:

1. Run the relevant tests or Unity batch-mode checks when available.
2. Review the diff for unrelated scene, prefab, package, and `.meta` changes.
3. Report what changed, what was verified, and any remaining manual Unity steps.

When requirements are unclear, preserve the current behaviour and ask before making a choice that would significantly expand scope.
