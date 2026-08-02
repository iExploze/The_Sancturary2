# Environment dressing validation checklist

Use this checklist for every scene-changing dressing pass. Mark inapplicable items explicitly; do not silently skip them.

## 1. Scope and scene safety

- [ ] Confirm the exact target scene, room/zone, and user-requested scope.
- [ ] Confirm exclusive scene access and that Ian is not editing the same scene/prefab.
- [ ] Record the initial branch, `git status --short`, loaded scenes, active scene, and dirty state.
- [ ] Preserve unrelated dirty scenes and user changes. Do not save them.
- [ ] Capture before views without writing screenshot assets under `Assets/`.
- [ ] Confirm the task authorizes scene changes. If it requests analysis/review only, stop before mutation.

## 2. Protected hierarchy

- [ ] Re-inspect the target hierarchy; do not rely on the creation-time snapshot in `reference-map-analysis.md`.
- [ ] Identify and leave unchanged all objects with `NetworkObject`, `InteractionTarget`, pickup, puzzle, openable, lock, door, locker, spawn, waypoint, trigger, NavMesh, player, or monster components.
- [ ] Confirm no protected object was moved, renamed, reparented, disabled, scaled, or given an override.
- [ ] Confirm walls, floors, ceilings, room dimensions, door openings, stairs, obstacles, and authored routes are unchanged.
- [ ] Confirm each new object is under one `ENV_Dressing_<ZoneName>` root and the required size/function subgroup.
- [ ] Confirm the dressing root contains no pre-existing gameplay object or original Vlad asset moved from elsewhere.
- [ ] Confirm empty groups and accidental duplicates are removed.

## 3. Asset and reference integrity

- [ ] Confirm every new prefab path appears as `D` or approved `C` in `asset-catalogue.md`.
- [ ] Confirm no `R` or `U!` structural prefab was instantiated.
- [ ] Confirm `C` asset use was previewed and documented.
- [ ] Confirm prefab instances retain valid source links and show no missing prefab, missing mesh, missing material, or missing script.
- [ ] Confirm no override was applied back to a source prefab.
- [ ] Confirm shared pack materials were not edited, duplicated, recolored, or instanced.
- [ ] Confirm no `.fbx`, `.prefab`, `.mat`, `.png`, `.asset`, animation, `.meta`, script, package, or project setting changed unexpectedly.
- [ ] Confirm Vlad-owned or ownership-unclear artwork was only instantiated/arranged and not replaced or substantially redesigned.
- [ ] Confirm no third-party source file was copied outside its existing repository location.

## 4. Composition and visual language

- [ ] Confirm the existing room function remains readable.
- [ ] Confirm one primary story beat drives the composition.
- [ ] Confirm large anchors were resolved before medium supports and small clutter.
- [ ] Confirm negative space remains deliberate and the room is not filled uniformly.
- [ ] Confirm furniture is grounded, wall/desk/table relationships are believable, and nothing floats or intersects visibly.
- [ ] Confirm pipes/cables form coherent supported runs with aligned connectors and plausible endpoints.
- [ ] Confirm lamps, signs, sockets, switches, extinguishers, and electrical props are mounted at believable positions.
- [ ] Confirm signs are truthful and do not contradict route, puzzle, or exit progression.
- [ ] Confirm strong rotations are limited to plausible disturbed props; functional furniture remains aligned.
- [ ] Confirm material variation comes from existing prefab/material families rather than source edits.
- [ ] Confirm damage/grime is restrained. No arbitrary decals, blood, or dirt were introduced.
- [ ] Confirm repeated assets vary by spacing, compatible variant, and composition without random scaling.

## 5. Colliders, triggers, and physics

Every hospital-pack prefab inspected for this skill includes at least one `MeshCollider`; never assume a visual-only object is nonblocking.

- [ ] Inspect every new instance's collider types, bounds, convex state, enabled state, layer, and static flag.
- [ ] Confirm large/medium collider bounds match the intended physical silhouette closely enough for first-person movement.
- [ ] Confirm small wall/ceiling/detail colliders do not protrude into player head, shoulder, jump, crouch, or camera clearance.
- [ ] Confirm no collider overlaps a door swing/slide volume or stops a door from reaching authored states.
- [ ] Confirm no collider overlaps interaction rays, prompt approach space, pickup access, hiding entrances, ladders, or required traversal.
- [ ] Confirm no collider overlaps or blocks spawn points, monster spawn, player spawns, patrol waypoints, or chase turns.
- [ ] Display and inspect relevant trigger bounds. Confirm new colliders do not pre-trigger, suppress, or block entry into puzzle, door, escape, audio, checkpoint, or monster volumes.
- [ ] Confirm any per-instance collider override affects only a new dressing object, is documented, and was not applied to the source prefab.
- [ ] Confirm no new Rigidbody or dynamic physics object was introduced for dressing.

## 6. NavMesh and monster navigation

- [ ] Identify the target `NavMeshSurface`, agent type, radius, height, climb, slope, and included layers.
- [ ] Determine whether new static colliders participate in the NavMesh build. Do not assume layer defaults.
- [ ] Visualize current NavMesh coverage through the zone and all door thresholds.
- [ ] Confirm the principal corridor and every required player/monster route remain connected.
- [ ] Confirm corners, furniture edges, curtain legs, chair banks, shelves, and door approaches retain enough turning clearance for the Geo Monster.
- [ ] Request explicit approval before rebaking if baking would serialize broad or unrelated navigation changes.
- [ ] If a bake is authorized, compare before/after coverage and review the resulting asset/scene diff.
- [ ] Run a monster patrol through the zone and verify it reaches each adjacent waypoint without oscillation, invalid paths, or snagging.
- [ ] Run a pursuit through both directions of each major corridor/turn and verify the monster can follow, turn, and attack as designed.
- [ ] Verify props do not create unintended safe spots, one-way squeezes, jump exploits, or inaccessible pickups.

## 7. Movement and interactable readability

- [ ] Walk, sprint, jump, and crouch through every route in the dressed zone.
- [ ] Test entering/exiting each adjacent room and moving through doors in all valid states.
- [ ] Check the zone from player eye height, crouch height, and a monster-scale overview.
- [ ] Confirm pursuit corridors preserve long/short sightlines intended by the layout.
- [ ] Confirm interactables, keys, locks, switches, puzzle objects, hiding places, and exits remain visually distinct from static dressing.
- [ ] Confirm prompts are not covered by meshes, transparent props, bloom, darkness, or high-contrast clutter.
- [ ] Confirm red/green/emissive accents do not create false gameplay affordances.
- [ ] Confirm no static duplicate resembles a required pickup or networked stateful object.

## 8. Lighting, probes, and post-processing

- [ ] Inspect the target render pipeline, quality level, global/local Volumes, exposure/tonemapping, fog, LightingSettings, and existing lights/probes before additions.
- [ ] Confirm every added light is motivated by a visible fixture or an explicit environmental source.
- [ ] Confirm required route geometry, door edges, interactables, and monster silhouette remain readable in bright and dark approaches.
- [ ] Confirm added lights do not create false puzzle state, power state, or route color coding.
- [ ] Check per-object additional-light overlap against the current pipeline limit.
- [ ] Check shadow-caster count and Console warnings. The reference's fifteen shadowed spots overflowed the current 2048 additional-light shadow atlas and triggered automatic resolution reduction; do not accept the same warning blindly.
- [ ] Confirm shadow bias/normal bias do not create obvious leaks or detached contact shadows.
- [ ] Confirm bloom does not erase signs, prompts, emissive indicators, or glass silhouettes.
- [ ] Confirm motion blur, depth of field, chromatic aberration, vignette, lens distortion, and Panini settings were not increased merely to mimic the reference.
- [ ] Confirm each new reflection probe has justified coverage, blend, resolution, and update mode, with no unnecessary realtime probe.
- [ ] Check reflective glass/metal from multiple approaches for probe seams and over-bright highlights.
- [ ] Confirm no global Volume, LightingSettings, URP asset, renderer, quality setting, or project package changed without explicit approval.

## 9. Performance

- [ ] Compare editor rendering stats before/after from the same camera and quality level when the pass is material.
- [ ] Check draw calls, batches, triangles, vertices, set-pass calls, and visible shadow casters.
- [ ] Confirm no accidental material duplicates broke batching.
- [ ] Check transparent glass overlap and overdraw, especially clustered medical glass and windows.
- [ ] Check the cost of added shadowed lights and reflection probes.
- [ ] Check collider count and complexity; file cabinets, medical bedside cabinets, and other multi-part props have multiple mesh colliders.
- [ ] Confirm static flags and layers are consistent with the target scene's established workflow; do not change them globally.
- [ ] Inspect occlusion/culling behaviour in long corridors and rooms with large anchors.
- [ ] Check the Console for import, missing-reference, shader, shadow-atlas, physics, and NavMesh warnings introduced by the pass.
- [ ] Do not claim performance is acceptable from a zero-draw-call or unfocused Editor stats sample.

## 10. Solo and multiplayer runtime checks

Static scene dressing normally needs no Fusion component, but its colliders and visibility affect every peer.

- [ ] Start a one-player Fusion session using the existing debug/session flow.
- [ ] Traverse the dressed zone and complete any adjacent door, pickup, hiding, or puzzle interaction without new obstruction.
- [ ] Start one host and at least one client when practical.
- [ ] Confirm both peers see the same static dressing and neither receives missing-object or scene mismatch errors.
- [ ] From both host and client, traverse doorways, interact with adjacent gameplay objects, and verify prompts/line of sight remain correct.
- [ ] Trigger a monster patrol/chase while both peers occupy the zone; verify dressing does not desynchronize authoritative monster decisions or block one peer unexpectedly.
- [ ] Disconnect a client in/near the zone and confirm static dressing has no ownership or reservation behaviour.
- [ ] Confirm no dressing object gained `NetworkObject`, authority, RPC, inventory, or gameplay state.
- [ ] If host/client testing was not run, state that clearly and give these exact checks to Ian.

## 11. Scene references and save review

- [ ] Save only the explicit target scene and intentionally created scene-local lighting data, if authorized.
- [ ] Reopen or reload the saved target scene after compilation/import is idle.
- [ ] Confirm the `ENV_Dressing_<ZoneName>` root and all prefab links survive reload.
- [ ] Confirm object references, materials, lights, probes, colliders, layers, tags, and active states are intact.
- [ ] Confirm no missing GUID or broken prefab reference appears in Inspector or Console.
- [ ] Confirm no unrelated dirty scene was saved.
- [ ] Run `git status --short` and `git diff --name-status`.
- [ ] Fail validation if a source pack asset, Vlad-owned source asset, gameplay script, prefab, material, model, texture, animation, package, project setting, or `.meta` changed unexpectedly.
- [ ] Review the target scene diff through Unity or a semantic scene-diff workflow; do not manually normalize/rewrite scene YAML.
- [ ] Confirm the final diff contains only the requested zone's scene changes and any explicitly authorized scene-local lighting/navigation data.

## 12. Handoff record

- [ ] Report the scene and zone.
- [ ] Report the new dressing root and child groups.
- [ ] List instantiated prefab paths by category and count.
- [ ] List per-instance overrides, especially colliders, layers, static flags, lights, and probes.
- [ ] List protected gameplay objects and routes checked.
- [ ] Report Unity version and whether the scene was opened/reloaded.
- [ ] Report NavMesh, monster, movement, lighting, performance, solo, host, and client checks actually performed.
- [ ] Give Ian exact remaining manual test steps and expected results.
- [ ] Confirm source prefabs, models, materials, textures, animations, gameplay code, and Vlad-owned artwork were not modified.
