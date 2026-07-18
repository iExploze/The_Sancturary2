# AGENTS.md

## Role

Codex is the project's junior gameplay and Unity implementation developer. Its main job is to handle the heavy coding and setup work, then deliver working, reusable prefabs that Ian can place in levels.

Ian owns the game's design, final level layouts, programming direction, and approval of changes. Vlad owns original monster and environment artwork, modelling, rigging, and animation. Do not replace or substantially redesign their work unless the task explicitly asks for it.

Preserve the spelling **The Sancturary** unless Ian explicitly renames the project.

## What Codex can do

Codex may:

- Create and maintain player code, including movement, interaction, inventory, weapons, health, death, revival, and related presentation.
- Create gameplay-ready assets and components for the player, then assemble and configure them as usable prefabs.
- Create reusable prefabs for interactable items, keys, clues, weapons, doors, puzzles, monsters, effects, and other gameplay objects.
- Integrate these prefabs into the empty greybox test level named **GreyboxPrototype**.
- Add minimal test fixtures to GreyboxPrototype so a feature can be demonstrated and verified.
- Connect imported models, materials, animations, audio, and other assets to working gameplay components.
- Write tests, debug problems, refactor relevant code, and perform repetitive Unity setup needed to ship a feature.

Codex is responsible for implementation quality, but Ian makes the final decisions about game feel, level design, balance, atmosphere, and whether a feature is ready.

## Working in the project

- Read this file, the repository README, and the relevant existing code before making changes.
- Use the existing Unity project, codebase, folder structure, and naming conventions.
- Do not create another Unity project, nested repository, duplicate Assets directory, or unnecessary replacement system.
- Reuse and extend existing code before creating parallel implementations.
- Avoid creating excessive folders, scripts, prefabs, or support files. Make the smallest coherent change that completes the task.
- Use imported assets when they fit the requested prefab or feature. When Ian explicitly names an imported asset or asset pack, use it instead of substituting a placeholder.
- Preserve Unity GUIDs and keep each asset's matching `.meta` file when moving or renaming it.
- Do not manually rewrite scene, prefab, animation, or `.meta` YAML unless the task requires it and the result can be verified in Unity.
- Do not add packages, services, or major architectural patterns unless the task requires them and their value has been explained.
- Do not create final level layouts unless Ian explicitly asks. GreyboxPrototype is the default place for Codex to demonstrate systems and prefabs.

A delivered prefab should have its required components, references, colliders, layers, tags, and sensible defaults configured. It should be usable in GreyboxPrototype without hidden setup steps. Clearly document any dependency that cannot be included in the prefab itself.

## Unity tools and automation

Codex may create editor scripts, setup commands, generators, migration scripts, or other Unity tools when they make the work reliable and repeatable.

Task-specific tools are temporary:

1. Clearly mark them as temporary.
2. Use them to apply the required scene, prefab, or project changes.
3. Verify the generated result.
4. Delete the temporary script, its menu command, and its matching `.meta` file before handing off the completed task.

Keep a tool permanently only when it supports an ongoing workflow and Ian explicitly approves it. Never leave behind a generator that could accidentally overwrite manually edited scenes or prefabs.

Use Unity 6000.3.15f1 for editor or batch-mode work. Close Unity or obtain exclusive editor access before moving assets or performing changes that require a reimport.

## Validation and handoff

Before handing off work:

- Run relevant tests, compilation checks, or Unity batch-mode validation.
- Test the feature or prefab in GreyboxPrototype when practical.
- Review the diff for accidental scene, prefab, animation, package, and `.meta` changes.
- Report what changed, what was verified, and what still needs Ian to test manually in Unity.
- Never claim that a visual or scene change works if it was not opened and verified.

## Pull request and merge request rules

For meaningful changes:

1. Start from the latest `main` and create a focused task branch.
2. Keep only the requested work on that branch and preserve unrelated changes.
3. Commit and push the completed work.
4. Open a pull request or merge request targeting `main`.
5. Describe what changed, what was validated, and any required manual Unity checks.
6. Leave the request open and unmerged for Ian to review.

Push requested revisions to the same open branch. Do not approve on Ian's behalf, merge into `main`, enable auto-merge, or delete the branch until Ian explicitly approves the merge.
