---
name: sancturary-environment-dressing
description: Improve the visual quality of an existing Unity map room or zone in The Sancturary by arranging approved hospital assets and applying reference-derived material, lighting, prop-placement, and composition patterns without changing layout or gameplay. Use when Codex is asked to dress, clutter, light, polish, or visually refine a specific existing playable space with repository assets. Do not use for map design, room or route changes, puzzle or progression work, gameplay-object placement, source-asset edits, or unsolicited whole-map redesigns.
---

# Sancturary Environment Dressing

Refine an existing playable space while treating layout and gameplay as fixed constraints. Reuse the visual language of the FPS Horror Hospital reference; do not copy its scene wholesale.

## Read the project and references first

Before taking action:

1. Read the repository `AGENTS.md`, `README.md`, the closest applicable `AGENTS.md`, and the target scene context.
2. Read [reference-map-analysis.md](references/reference-map-analysis.md) completely for the observed visual language and current-map protection notes.
3. Read [asset-catalogue.md](references/asset-catalogue.md) completely before choosing assets. Obey each asset's status and restriction.
4. Read the relevant archetype in [room-archetypes.md](references/room-archetypes.md); read all archetypes only for an explicitly requested broad pass.
5. Read [validation-checklist.md](references/validation-checklist.md) before changing a scene, then use it before handoff.

If the task is analysis, planning, or review only, remain read-only and do not alter a scene.

## Protect ownership and source work

- Treat Ian's layouts, gameplay systems, routes, puzzles, and progression as fixed unless Ian explicitly requests a change.
- Treat Vlad's models, rigging, animation, environment art, and other original artwork as protected. Instantiate and arrange approved assets only. Do not replace, substantially redesign, discard, reimport, remesh, re-UV, retopologize, repaint, re-rig, or apply prefab or material changes to Vlad's source files without approval.
- Treat unclear asset ownership as protected until confirmed.
- Keep purchased pack content inside the repository workflow. Do not export or copy third-party source assets elsewhere; no pack-specific licence file was found during this skill's creation.
- Never apply overrides back to a source prefab. Never edit source `.prefab`, `.fbx`, `.mat`, `.png`, `.asset`, animation, or `.meta` files as part of dressing.

## Enforce the fixed gameplay boundary

- Preserve the existing map layout, room dimensions, floor elevations, doorway widths, and ceiling clearances.
- Do not redesign routes, puzzles, doors, locks, keys, monster behaviour, player movement, hiding, spawning, or progression.
- Do not move, rename, reparent, disable, or modify gameplay objects unless explicitly requested.
- Treat objects with `NetworkObject`, `InteractionTarget`, puzzle, pickup, door, locker, spawn, waypoint, trigger, or navigation components as protected.
- Do not obstruct doors, door swing volumes, important sightlines, pursuit routes, interactables, puzzle objects, spawn points, triggers, or required navigation.
- Keep major pursuit corridors readable and navigable from both player and monster perspectives.
- Do not add a `NetworkObject`, RPC, runtime manager, package, script, or other runtime system for static dressing. Escalate any requested stateful prop to a separate gameplay/networking task.
- Work on one explicitly requested room or zone at a time unless the user explicitly requests a broader pass.

## Use the dressing workflow

### 1. Confirm the target and obtain scene safety

- Identify one exact scene and room/zone boundary.
- Obtain exclusive access before broad scene work. Do not work in a scene Ian is editing.
- Inspect the loaded-scene list, active scene, dirty state, hierarchy, layers, tags, lighting, volumes, probes, NavMesh surfaces, and relevant Console messages.
- Record Git status and source-asset hashes or timestamps when accidental serialization would be risky.
- Capture useful before views without creating assets inside `Assets/`.

### 2. Mark protected space

- Identify the playable footprint, principal sightlines, door arcs, interaction approach zones, puzzle readability zones, player and monster spawns, patrol paths, hiding entrances, and chase turns.
- Inspect colliders and triggers, not just visible meshes.
- Treat current NavMesh connectivity as a requirement. Do not assume a visible gap is navigable.
- Keep an unobstructed inspection buffer around all protected objects. Base the buffer on actual colliders, agent radius, door motion, and interaction distance rather than a guessed universal number.

### 3. Choose a deliberate composition

- Define the room's existing function and one visual story beat before placing anything.
- Prefer deliberate composition over random scattering.
- Select only assets classified as demonstrated or approved-compatible in the catalogue.
- Place large compositional props first, then medium props, then limited small clutter.
- Use asymmetry and controlled variation while preserving clear negative space.
- Stop when the visual story reads; do not fill every empty surface.

### 4. Create a removable hierarchy

Create one clearly named root at scene level:

```text
ENV_Dressing_<ZoneName>
├── 01_Large
├── 02_Medium
├── 03_Small
├── 04_WallCeiling
└── 05_LightingAndProbes
```

- Put every newly generated dressing object under this root.
- Keep protected gameplay objects and pre-existing art outside it.
- Use stable descriptive instance names; avoid hundreds of indistinguishable default names.
- Keep lights and probes created for the pass under `05_LightingAndProbes`.
- Remove empty subgroups before handoff.

### 5. Place from large to small

- Place large anchors against walls or in existing dead space; never use them to invent a route or room division.
- Add medium props to explain use, support silhouette, and break repetition.
- Add small props only on believable support surfaces or as restrained floor details outside movement paths.
- Use wall and ceiling assets flush to their mounting surface. Keep pipe, cable, lamp, sign, and holder colliders out of head and shoulder clearance.
- Inspect each instantiated prefab's default `MeshCollider`; all 140 hospital-pack prefabs inspected while creating this skill included at least one.
- Prefer placements that make the existing collider safe. If a new decoration needs a collider override, keep it instance-only, document it, and validate the movement consequence; never change the source prefab.

### 6. Apply materials, damage, and repetition rules

- Use existing material assignments and demonstrated variants. Do not create replacement materials or edit shared material properties.
- Let the pack's stained plaster, tile, cement, trim, glass, and worn-prop textures carry most damage.
- Do not invent random blood, grime, decals, or damage. No dedicated decal prefabs were found in the hospital pack; inspect and obtain approval before using decals from another source.
- Avoid obvious copy-paste cadence. Alternate compatible lamp, chair, sign, pipe, cable, and medical-prop variants, and vary spacing more than rotation.
- Keep grounded furniture mostly square to architecture. Reserve stronger rotation offsets for displaced chairs, curtains, small tables, bowls, and loose medical objects.
- Do not scale functional furniture or wall fixtures non-uniformly merely to create variety.

### 7. Apply lighting with restraint

- Use the reference's localized pools, deep falloff, warm/cool contrast, emissive fixtures, and room-sized reflection coverage as patterns rather than presets.
- Do not copy the reference Volume, LightingSettings, URP assets, or all 15 shadow-casting spotlights into a target scene.
- Preserve interactable silhouettes, door edges, pursuit depth, and player orientation. Darkness may hide detail, not required information.
- Prefer a few motivated lights associated with visible fixtures. Limit overlapping additional lights and shadow casters.
- Check the target pipeline and existing global volume before adding anything. Do not change the render pipeline or install packages.
- Keep camera effects comfortable for movement-heavy first-person play. Avoid increasing motion blur, depth of field, chromatic aberration, bloom, or vignette solely to mimic the reference screenshot.

### 8. Validate, save, and hand off

- Run every applicable item in [validation-checklist.md](references/validation-checklist.md).
- Save only the explicitly targeted scene and intentionally created scene-local lighting data, if any.
- Review the final diff and revert accidental changes only to files created or changed by this pass. Preserve unrelated user work.
- Report the zone dressed, assets instantiated, hierarchy root, collider overrides, lighting/probe additions, protected objects checked, validation performed, and remaining manual checks.
- State clearly whether Unity, NavMesh, solo, and host/client validation were actually run.

## Stop conditions

Stop and request direction when:

- the requested composition requires moving a wall, doorway, route, puzzle object, gameplay prop, spawn, trigger, or patrol waypoint;
- a source prefab, material, model, texture, animation, or Vlad-owned asset would need modification;
- an asset's licence or ownership would need external copying or redistribution;
- the room or zone is not explicit and choosing one would materially change scope;
- exclusive scene access is unavailable;
- safe placement would require a new runtime or networking system;
- required navigation, door clearance, interaction visibility, or pursuit readability cannot be preserved.
