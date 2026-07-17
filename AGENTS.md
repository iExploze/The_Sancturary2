# AGENTS.md

## Project

The Sancturary is a movement-heavy, single-player first-person escape-horror game made in Unity. The player explores an abandoned institution, finds clues and keys, solves a short chain of environmental puzzles, evades one monster, and opens the final escape route.

The current goal is a focused 10–15 minute prototype. Prefer a small, complete, scary gameplay loop over production-scale systems or speculative features.

Preserve the spelling **The Sancturary** unless Ian explicitly renames the project.

## Current gameplay priorities

Work in roughly this order unless a task says otherwise:

1. Responsive first-person movement and chase readability
2. Reliable interaction with clues, keys, doors, and puzzle objects
3. A clear, softlock-resistant puzzle and progression chain
4. One understandable monster with patrol, investigate, search, and chase states
5. A complete entry-to-escape loop with win and loss states
6. Atmosphere, lighting, audio, and visual polish that support the loop

Movement should initially support walking, looking, sprinting with stamina, and crouching at reduced speed. Communicate fatigue through presentation such as a vignette and breathing rather than a permanent stamina bar.

## Scope boundaries

The prototype does not currently require:

- Multiplayer or networking
- Security-camera monitoring
- Remotely operated security doors
- Multiple monsters
- Combat
- A large or procedurally generated map
- A custom backend
- DOTS/ECS
- Production-scale save, account, or matchmaking systems

Do not expand or reintroduce these systems without explaining how they improve the current escape loop and receiving approval. Existing out-of-scope code or packages may be preserved for now; do not delete them unless the task explicitly requires cleanup.

## Technology

- Unity 6.3 LTS, pinned to 6000.3.15f1
- Universal Render Pipeline
- C#
- Unity Input System
- Unity AI Navigation
- Blender and FBX for 3D assets
- Git and Git LFS
- Windows and Steam as the initial target

Do not introduce a new framework, package, online service, or major architectural pattern without explaining the need and receiving approval.

## Gameplay-system principles

- Build the smallest complete version of a feature before expanding it.
- Keep implementations simple, modular, readable, and easy to tune.
- Separate deterministic gameplay rules from Unity presentation code when practical.
- Model puzzle progression with explicit states and dependencies.
- Make keys, clues, and interactable objects readable without relying on excessive HUD markers.
- Keep the initial inventory small; avoid a general-purpose inventory system until the prototype needs one.
- Ensure important items and puzzle states cannot leave the run permanently softlocked.
- Use ScriptableObjects for reusable design data when they provide clear value, such as item definitions or monster tuning.
- Use an explicit finite state machine for the initial monster AI.
- Let the monster create urgency without making clue reading or puzzle reasoning constantly impossible.
- Prefer composition over deep inheritance.
- Do not optimize without evidence from profiling.

## Unity safety

- Do not manually rewrite Unity scene, prefab, animation, or `.meta` YAML files unless the task explicitly requires it and the change can be verified.
- Prefer C# components or editor scripts for repeatable setup work.
- Preserve asset GUIDs and always retain `.meta` files when moving or renaming assets.
- Do not modify files under `ArtSource/` unless the task is specifically about the Blender source pipeline.
- Do not claim a scene or visual change works unless it was opened and verified in Unity; otherwise state the limitation clearly.
- Use Unity 6000.3.15f1 for editor or batch-mode validation.
- Close Unity or obtain exclusive editor access before moving assets or making changes that require a reimport.

## Code conventions

- Use clear descriptive names; avoid abbreviations that are not established in the project.
- Keep one primary public type per C# file and match the filename to the type name.
- Avoid global mutable state and unnecessary singletons.
- Comment design intent and non-obvious constraints, not straightforward syntax.
- Add EditMode tests for deterministic movement, puzzle, inventory, and monster-state logic when practical.
- Add selective PlayMode tests for important Unity interactions when the tests are stable and valuable.

## Working with the team

- Ian owns programming, gameplay systems, technical implementation, and overall design.
- Vlad owns Blender source files, monster design, modelling, rigging, animation, and environment art.
- Do not replace or substantially redesign Vlad's work without discussing it first.
- Treat artwork from the original 2D game as useful concept and design reference.
- Avoid editing the same Unity scene or prefab as another contributor at the same time.
- Treat exported FBX files as game inputs and `.blend` files as art source files.
- Keep changes focused so they are easy to review and revert.

## Branch and pull request policy

Every repository change, including documentation-only and other low-risk changes, must use this workflow:

1. Start from the latest `main`.
2. Create a new, focused branch for the task before editing any file.
3. Keep only that task's changes on the branch.
4. Commit and push the completed work to the task branch.
5. Open a pull request targeting `main`.
6. Leave the pull request open and unmerged for Ian to review.

Never commit or push changes directly to `main`. Do not reuse an already merged or closed task branch for new work.

Ian personally reviews each pull request, may request revisions, and decides whether it is allowed to merge. Push requested revisions to the same open pull-request branch. Do not approve on Ian's behalf, merge a pull request, enable auto-merge, or otherwise update `main` unless Ian explicitly authorizes that merge after reviewing the PR.

Each pull request should clearly describe:

- What changed and why
- The important files or systems affected
- Tests and validation performed
- Known limitations and required manual Unity checks
- Any unexpected scene, prefab, package, animation, or `.meta` changes

After an approved pull request is merged, delete its task branch when safe and begin the next task from the updated `main`.

## Agent workflow

Before changing anything:

1. Read this file and the repository `README.md`.
2. Inspect `git status` and preserve unrelated work.
3. Inspect the relevant files and nearby conventions.
4. Confirm assumptions that materially affect gameplay or implementation.
5. Make the smallest coherent change that completes the request.

After changing anything:

1. Run the relevant tests, compilation checks, or Unity batch-mode validation when available.
2. Review the diff for accidental scene, prefab, package, and `.meta` changes.
3. Clearly report what changed, what was verified, and what still requires manual testing in Unity.

When requirements are unclear, preserve current behaviour and ask before making a choice that would significantly expand scope.
