# AGENTS.md

## Project goal

The Sancturary is a small, movement-heavy, cooperative first-person escape-horror prototype for one to four players. Players explore an abandoned institution, find environmental clues and keys, solve a short chain of puzzles, evade a roaming monster, unlock the final route, and escape together. Solo play remains supported through the same core gameplay systems.

Prioritize completing one playable and scary multiplayer vertical slice over building impressive architecture or speculative systems. Preserve the spelling **The Sancturary** unless Ian explicitly renames the project.

## Ownership

Ian owns programming, technical implementation, gameplay systems, final level layouts, and overall design decisions.

Vlad owns Blender work, original monster design, modelling, rigging, animation, and environment art. Do not replace, substantially redesign, or discard Vlad's work without discussing it first.

## Current technical boundary

- Unity 6.3 LTS, pinned to editor version `6000.3.15f1`.
- Universal Render Pipeline.
- C#.
- Unity Input System.
- Unity AI Navigation.
- Photon Fusion is the active multiplayer framework.
- Blender assets exported as FBX.
- Windows and Steam are the initial targets.

Multiplayer is part of the full prototype scope. The prototype must support one to four players, with hosting and joining through the existing Photon Fusion session architecture. Solo play is still supported, but gameplay systems must not be designed as single-player-only when they affect shared world state.

Build shared gameplay with clear network authority:

- State authority validates consequential actions such as item pickup, item dropping, inventory ownership, damage, death, monster decisions, doors, locks, keys, puzzle state, and escape progression.
- Shared world objects must be spawned, despawned, and synchronized through Fusion where applicable.
- Owner-only presentation such as the local camera, input, inventory UI, first-person held-object view, screen effects, and local prompts should remain local.
- Other players must receive the replicated state and any required third-person visual representation of equipped items and actions.
- Do not trust a client request without validating ownership, distance, line of sight, availability, and relevant gameplay restrictions on state authority.

Reuse and extend the existing Photon Fusion code rather than creating parallel networking systems. Do not introduce Netcode for GameObjects, Photon PUN, Unity Multiplayer Services, or another networking framework unless Ian explicitly changes the networking direction. Security-camera gameplay and remotely controlled security doors remain outside the current prototype scope unless Ian explicitly brings them in.

## How Codex should work

- Read this file, `README.md`, the closest applicable `AGENTS.md`, and the relevant existing code before changing anything.
- Inspect the current branch and working tree before making edits. Preserve unrelated user work.
- Inspect the repository and available Unity project state before making claims about scenes, prefabs, packages, compiler errors, networking behaviour, or runtime behaviour.
- Make the smallest coherent change that fully completes the request.
- Reuse existing code, folders, prefabs, imported assets, Fusion behaviours, and session flows before creating parallel replacements.
- Avoid unnecessary folders, scripts, managers, abstractions, and support files.
- Do not introduce packages, frameworks, online services, or major architectural patterns without explaining their value and trade-offs.
- Do not create or substantially redesign final level layouts unless Ian explicitly requests it.
- Use imported assets when they fit the task. When Ian names a specific imported asset or pack, use it unless there is a concrete technical blocker.
- Check licences before copying third-party art, audio, models, animations, effects, or sample code.
- Ask for clarification only when the answer would materially change the design, risk existing work, or create significant extra work. Otherwise, state a reasonable assumption briefly and continue.
- Stop after the requested coherent task. Do not begin unrelated follow-up features.

A delivered gameplay prefab should include its required components, references, colliders, layers, tags, network components, authority assumptions, and sensible defaults. It should not depend on undocumented setup steps.

## Multiplayer implementation rules

- Treat the host/state authority as authoritative for shared gameplay state.
- Use Fusion input and RPC or authoritative interaction flows consistently with the existing project architecture.
- Keep network payloads compact and use stable identifiers for item or gameplay definitions rather than attempting to synchronize Unity object references directly.
- Prevent two players from simultaneously claiming the same world item or completing the same one-time interaction twice.
- Ensure late joiners receive the current authoritative state of persistent shared objects and puzzle progression where relevant.
- Handle player disconnects without leaving invalid ownership references or permanently reserved gameplay objects.
- Do not replicate purely local UI layout details, cursor state, inventory drag previews, camera effects, or first-person-only presentation.
- When an equipped item must be visible to other players, separate the owner's first-person representation from the replicated third-person representation.
- Preserve solo play by allowing a one-player Fusion session or the existing local Fusion debug mode; do not maintain a separate single-player gameplay implementation unless technically necessary.

## Development workflow

Use this loop by default:

1. Inspect the current implementation and identify the smallest coherent change.
2. Determine which state is local-only and which state must be authoritative and replicated.
3. Implement the requested code, prefab, animation integration, networking, or Unity setup.
4. Review the final diff for accidental or unrelated changes.
5. Report the exact solo and host/client manual Unity test steps and expected results.
6. Ian opens Unity, allows compilation and import to finish, playtests the change, and reports concrete results, screenshots, videos, or Console errors.
7. Use Ian's playtest report for the next focused correction.

Treat Ian's manual playtest as the default source of truth for movement feel, animation quality, camera comfort, horror pacing, chase readability, sound timing, map flow, and other subjective gameplay behaviour. For shared gameplay correctness, explicitly test from both host and client perspectives rather than assuming host behaviour proves multiplayer correctness.

Build complete vertical slices instead of isolated speculative systems. Prefer finishing a small multiplayer-playable chain from exploration through puzzle, pursuit, and escape before expanding content or architecture.

## Unity and MCP workflow

- Prefer direct C# edits for code-only changes.
- Use Unity MCP, the Unity Editor, or a temporary Editor tool when the task genuinely requires live scene inspection, GameObject creation, component assignment, prefab wiring, network prefab registration, Animator Controller or Blend Tree setup, imported-asset settings, object references, Unity tests, or Editor serialization.
- Do not launch Unity, enter Play Mode, or run batch mode after every small change.
- Do not use Unity merely to judge subjective gameplay behaviour that Ian will immediately playtest.
- When Unity validation is required, run it once after completing the coherent group of changes rather than repeatedly after each edit.
- Never pretend MCP or the Unity Editor was used when it was unavailable. Clearly state what was and was not verified.
- Prefer Unity Editor or MCP operations over manually editing serialized `.unity`, `.prefab`, `.asset`, animation, or `.meta` YAML.
- Preserve Unity GUIDs. Keep each asset with its matching `.meta` file when moving or renaming it, and never manually invent replacement GUIDs.
- Do not modify the same scene or prefab concurrently with Ian. Obtain exclusive access before broad scene changes, asset moves, or operations that trigger large reimports.
- If dependent scene or prefab work requires successful script compilation, wait for Unity compilation and inspect the Console before continuing.
- Save modified scenes and assets explicitly when the task requires it. Do not save unrelated dirty scenes.
- Use Unity `6000.3.15f1` whenever Editor or batch-mode validation is performed.

Codex may create temporary Editor scripts, setup tools, generators, or migration commands when they make a task reliable and repeatable. Mark task-specific tools as temporary, use them, verify their output, and remove the tool and its matching `.meta` file before handoff. Keep a tool permanently only when it supports an ongoing workflow and Ian explicitly approves it.

## When Unity validation is required

Launch Unity, use MCP, or run batch mode when at least one of these applies:

- The task requires Unity Editor serialization.
- The task creates or substantially edits scenes, prefabs, network prefab registration, Animator Controllers, Blend Trees, Avatar Masks, imported-asset settings, or object references.
- A temporary Editor tool must be executed.
- Static inspection cannot reasonably validate the result.
- Ian explicitly requests Unity validation.
- A larger milestone is being prepared for commit or review.

For ordinary C# gameplay logic, small value adjustments, input logic, state changes, stamina, health, interaction logic, and focused bug fixes, prefer static inspection plus Ian's manual playtest unless Unity execution is specifically needed.

A networking change is not considered runtime-verified from a single local player alone. When practical, validate with at least one host and one client. If a two-instance test was not run, state that clearly and provide exact host/client test steps.

## Validation and handoff

Before handing off meaningful work:

- Perform relevant static checks on edited C# code.
- Run tests, compilation checks, Unity validation, or a host/client test only when they are available and proportionate to the change.
- Review the diff for accidental scene, prefab, package, animation, imported-asset, network-prefab registration, and `.meta` changes.
- Report the files changed, behaviour implemented, authority model, replicated state, checks performed, exact manual Unity test steps, and anything still requiring setup or verification.
- Never claim that a visual, scene, package, build, networking, or runtime change works unless it was actually opened or executed and verified.
- When Unity or a multi-instance test was not run, state that clearly and identify what Ian must verify manually.

## Git and pull requests

- Never delete, replace, or broadly rewrite unrelated files.
- For meaningful work, start from the latest `main` on a focused branch.
- Keep only the requested work on that branch, commit it, and push it.
- Open a pull request targeting `main` with a clear summary, validation results, and any required manual Unity checks.
- Leave pull requests open and unmerged for Ian to review.
- Push requested revisions to the same branch. Do not approve on Ian's behalf, merge into `main`, enable auto-merge, or delete the branch unless Ian explicitly requests it.
- Write directly to `main` only when Ian explicitly requests it or the change is clearly trivial and low-risk.
