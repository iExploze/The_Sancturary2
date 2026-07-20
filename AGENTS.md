# AGENTS.md

## Project goal

The Sancturary is a small, movement-heavy, first-person escape-horror prototype. The player explores an abandoned institution, finds environmental clues and keys, solves a short chain of puzzles, evades a roaming monster, unlocks the final route, and escapes.

Prioritize completing one playable and scary vertical slice over building impressive architecture or speculative systems. Preserve the spelling **The Sancturary** unless Ian explicitly renames the project.

## Ownership

Ian owns programming, technical implementation, gameplay systems, final level layouts, and overall design decisions.

Vlad owns Blender work, original monster design, modelling, rigging, animation, and environment art. Do not replace, substantially redesign, or discard Vlad's work without discussing it first.

## Current technical boundary

- Unity 6.3 LTS, pinned to editor version `6000.3.15f1`.
- Universal Render Pipeline.
- C#.
- Unity Input System.
- Unity AI Navigation.
- Blender assets exported as FBX.
- Windows and Steam are the initial targets.

The current prototype is single-player. Do not add or expand multiplayer, Photon Fusion, Netcode for GameObjects, Unity Multiplayer Services, lobbies, session flows, network ownership, security-camera gameplay, or remotely controlled security doors unless Ian explicitly brings those systems back into scope.

Existing networking code, assets, or packages may remain temporarily. Do not build new gameplay on top of them. Remove legacy networking material only as part of a focused, explicitly requested cleanup after confirming that nothing still depends on it.

## How Codex should work

- Read this file, `README.md`, the closest applicable `AGENTS.md`, and the relevant existing code before changing anything.
- Inspect the current branch and working tree before making edits. Preserve unrelated user work.
- Inspect the live Unity project state before making claims about scenes, prefabs, packages, compiler errors, or runtime behaviour.
- Make the smallest coherent change that fully completes the request.
- Reuse existing code, folders, prefabs, and imported assets before creating parallel replacements.
- Avoid unnecessary folders, scripts, managers, abstractions, and support files.
- Do not introduce packages, frameworks, online services, or major architectural patterns without explaining their value and trade-offs.
- Do not create or substantially redesign final level layouts unless Ian explicitly requests it.
- Use imported assets when they fit the task. When Ian names a specific imported asset or pack, use it unless there is a concrete technical blocker.
- Check licences before copying third-party art, audio, models, animations, effects, or sample code.
- Ask for clarification only when the answer would materially change the design, risk existing work, or create significant extra work. Otherwise, state a reasonable assumption briefly and continue.

A delivered gameplay prefab should include its required components, references, colliders, layers, tags, and sensible defaults. It should not depend on undocumented setup steps.

## Unity and MCP workflow

- Prefer Unity MCP for inspecting and modifying live scenes, GameObjects, components, prefabs, assets, the Console, Play Mode, and Unity tests when the MCP connection is available.
- Never pretend MCP or the Unity Editor was used when it was unavailable. Clearly state what was and was not verified.
- Prefer Unity Editor or MCP operations over manually editing serialized `.unity`, `.prefab`, `.asset`, animation, or `.meta` YAML.
- Preserve Unity GUIDs. Keep each asset with its matching `.meta` file when moving or renaming it, and never manually invent replacement GUIDs.
- Do not modify the same scene or prefab concurrently with Ian. Obtain exclusive access before broad scene changes, asset moves, or operations that trigger large reimports.
- After changing C# scripts, wait for Unity compilation to finish and inspect the Console before continuing with dependent scene or prefab work.
- Save modified scenes and assets explicitly when the task requires it. Do not save unrelated dirty scenes.
- Use Unity `6000.3.15f1` for Editor or batch-mode validation.

Codex may create temporary Editor scripts, setup tools, generators, or migration commands when they make a task reliable and repeatable. Mark task-specific tools as temporary, use them, verify their output, and remove the tool and its matching `.meta` file before handoff. Keep a tool permanently only when it supports an ongoing workflow and Ian explicitly approves it.

## Validation and handoff

Before handing off meaningful work:

- Run relevant tests, compilation checks, or Unity batch-mode validation.
- Use Unity MCP or the Editor to test the affected scene, prefab, or gameplay path when practical.
- Review the diff for accidental scene, prefab, package, animation, imported-asset, and `.meta` changes.
- Report what changed, what was verified, and what still requires manual testing in Unity.
- Never claim that a visual, scene, package, build, or runtime change works unless it was actually opened or executed and verified.

## Git and pull requests

- Never delete, replace, or broadly rewrite unrelated files.
- For meaningful work, start from the latest `main` on a focused branch.
- Keep only the requested work on that branch, commit it, and push it.
- Open a pull request targeting `main` with a clear summary, validation results, and any required manual Unity checks.
- Leave pull requests open and unmerged for Ian to review.
- Push requested revisions to the same branch. Do not approve on Ian's behalf, merge into `main`, enable auto-merge, or delete the branch unless Ian explicitly requests it.
- Write directly to `main` only when Ian explicitly requests it or the change is clearly trivial and low-risk.
