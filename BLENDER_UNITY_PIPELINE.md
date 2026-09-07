# Blender → Unity → Git

Codex orchestrates BlenderMCP and UnityMCP independently. The filesystem handoff is an explicit FBX export; there is no direct MCP-to-MCP bridge or export-on-save automation.

## Source and ownership

`ArtSource/Blender/` holds accepted editable Blender source, versioned with Git LFS. `Assets/` holds Unity-importable FBX files, textures, and Unity assets. Create source subfolders as assets arrive; do not reorganize established Unity art folders to mirror them.

Blender owns meshes, UVs, source materials, armatures, rigs, Blender-authored animation, export origins/pivots, and static architectural geometry. Unity owns gameplay prefabs/components, gameplay colliders, NavMesh configuration, interactive doors, triggers, item definitions, lighting configuration, scene wiring, Unity materials, Animator integration, network state, and prefab references. Ian owns technical/gameplay integration; respect Vlad's modelling, rigging, animation, and environment art. Inspect his source freely, but do not substantially redesign or overwrite it without explicit instruction.

Ian's external `E:\BlenderWithMCPtest` directory is an experiment/scratch area, not a repository dependency. Keep it intact. Once an asset is accepted, copy/save its source into `ArtSource/Blender/<category>/<asset>/`, validate the copy and its texture dependencies, then make that repository copy authoritative for future edits. Do not promote experimental hospital/example maps.

## An asset task

1. Read `AGENTS.md`, inspect Git status and relevant source/import/prefab locations, and check ownership and third-party licences. Preserve unrelated work; use a focused branch from latest `main` and an isolated worktree if the open checkout cannot safely switch.
2. Verify BlenderMCP scene access before creating/editing. Preserve unsaved Blender work before opening another file. Use scratch space for experiments and repository source for accepted assets.
3. Validate geometry, UVs, material slots, dimensions, names, pivots, and any rig/animation. Save the production `.blend`; reopen/inspect that copy and ensure textures are packed or available through repository-relative paths.
4. Explicitly export intended meshes and required textures into the closest existing Unity feature directory. For the Frontier Revolver these are `Assets/Inventory/Generated/Models/FrontierRevolver.fbx`, `FrontierRound.fbx`, and the existing `Generated/Textures/FrontierRevolver/` directory. Unity materials remain under `Generated/Materials/FrontierRevolver/`. Do not overwrite a validated export just to test this convention.
5. Allow Unity **6000.3.15f1** to import. Use UnityMCP to verify the correct project/version and Editor readiness, inspect imported assets, and configure importer/material/prefab/Animator references when needed. Use filesystem edits for C#. Check relevant Console errors. Save only intentionally changed assets/scenes; do not enter Play Mode for a source-only migration.
6. Review the full diff, including binaries and `.meta` files. Preserve GUIDs; stage only intended source, exports, textures, and Unity integration. Exclude machine configuration, backups, caches, logs, and scratch content. Commit, push, and open a PR to `main`; leave it unmerged. Report actual checks and remaining manual validation, including host/client checks if shared gameplay was affected.

## Scale and export

Use real-world scale: one Unity unit represents one meter. Use sensible origins and stable, descriptive mesh names. Apply transforms where appropriate; do not indiscriminately apply transforms to rigs. Verify final Unity orientation and dimensions, and exclude cameras, lights, and helper geometry. Preserve separate moving meshes/pivots for gameplay and animation.

The established [Frontier Revolver import](REVOLVER_IMPORT.md) records a 328 × 48 × 157 mm weapon and 41.2 mm cartridge, with held meshes facing Unity +Z at unit scale. Its importer uses `globalScale: 1`, `useFileScale: 1`, and `bakeAxisConversion: 0`. These are inspected Unity importer values, not a reconstructed Blender FBX preset. Reuse the original validated export settings when the sandbox is available and verify the result; exact Blender axis/scale options were not recorded in that document. Character exports require their own armature and animation settings rather than this static-prop convention.

## Local tools and Git LFS

Keep existing BlenderMCP and UnityMCP installations independent. Machine-specific commands, paths, ports, credentials, and client registrations stay in the user's local Codex configuration or already-ignored local files, never in Git. Do not replace a working registration or install another implementation. If registrations change, reconnect/restart Codex and confirm both tool inventories and actual scene/project reads before claiming connectivity.

Git LFS rules already cover `.blend`, `.fbx`, `.FBX`, common textures, audio, and other binary art formats. Verify a new source/export with `git check-attr filter diff merge -- <source.blend> <export.fbx>` (expect `lfs`) and inspect staged LFS pointers. Keep `.blend1` local; numbered backups and Blender simulation caches under `ArtSource/Blender/` are ignored. Do not migrate Git history or restage old binaries just to change storage.

The source area and conventions can be used without the scratch drive. The real Frontier source migration and Blender connection check must wait until the documented external source/installation is available; no replacement source should be fabricated.
