# Environment asset catalogue

## Contents

- [Status key](#status-key)
- [Selection rules](#selection-rules)
- [Large and medium props](#large-and-medium-props)
- [Small medical props](#small-medical-props)
- [Wall, ceiling, and semantic props](#wall-ceiling-and-semantic-props)
- [Cables](#cables)
- [Pipes](#pipes)
- [Structural modules: reference only](#structural-modules-reference-only)
- [Materials and project-level assets](#materials-and-project-level-assets)

## Status key

| Status | Meaning |
|---|---|
| `D` | Demonstrated in `FPS Horror Hospital Environment Scene.unity` and eligible for dressing when its placement rules are met. |
| `D!` | Demonstrated, but semantically or mechanically risky. Do not use by default; inspect the target and obtain explicit direction when it may resemble an interactable or change navigation. |
| `C` | Compatible pack asset not demonstrated in the reference scene. Preview and inspect it before use; use only when it completes an otherwise demonstrated family. |
| `R` | Reference-only structural asset. It may explain the visual language but must not be instantiated by this dressing skill. |
| `U!` | Not demonstrated and outside dressing scope. Do not use. |

## Selection rules

- Instantiate source pack prefabs from `Assets/FPS Horror Hospital Pack/Art/Prefabs/`; do not copy or edit them.
- Do not use similarly named gameplay prefabs under `Assets/Prefabs/` as decoration. Those may contain Fusion, interaction, lock, pickup, or openable behaviour.
- Inspect prefab hierarchy, renderer materials, scale, and colliders before placement. Every one of the 140 pack prefabs inspected for this catalogue contains at least one `MeshCollider`.
- Use structure prefabs only as visual reference. Dressing must preserve the existing shell, routes, dimensions, walls, floors, doors, roofs, columns, stairs, and arches.
- Keep semantic assets truthful. Exit signs, electrical panels, switches, aid kits, file cabinets, and bedside cabinets can mislead players when the map uses matching interactive versions.
- Prefer a small coherent subset per room. Do not treat this catalogue as a checklist to fill.

## Large and medium props

| Status | Exact prefab path | Intended usage and approximate placement | Restrictions |
|---|---|---|---|
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Bed.prefab` | Primary patient-room anchor. Align broadly to a wall; normally one bed in a small room or an existing two-bed composition in a larger room. | Never place in a corridor or narrow pursuit lane. Preserve both bedside approach and monster pathing. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Chair.prefab` | Single waiting, office, or displaced chair. Keep most square to a wall or desk; use a modest angle for one disturbed accent. | Do not create snag points at corners or door approaches. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Chairs.prefab` | Fixed chair bank for waiting or reception edges. Place parallel to a long wall with the seat side facing usable open space. | Keep out of principal corridor width and chase turns; do not repeat at every bay. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Desk.prefab` | Reception/office anchor or corridor-side abandoned workstation where the room already supports that function. | Do not use as a new barricade or room divider. Keep the player-facing side and any interactable beyond it reachable. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/File Cabinet.prefab` | Office, records, or storage wall anchor; one or two units, usually flush to architecture. | The playable map has an interactive/networked file-cabinet variant. Avoid static lookalikes near keys or searches unless Ian explicitly approves. This prefab has eleven mesh colliders. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Folding Curtain_01.prefab` | Mostly closed treatment/privacy divider beside an existing bed or treatment area. | Do not divide a route, conceal a required object, or block monster sight/navigation needed by design. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Folding Curtain_02.prefab` | Alternate/open privacy divider for an asymmetric treatment composition. | Use one curtain form as the dominant variant in a room. Keep the folding footprint outside movement clearance. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Heater.prefab` | Repeating radiator/heater under windows or along corridor/room walls. Use at most one per relevant wall bay and skip bays with focal gameplay. | Keep its collider tight to the wall and out of hiding/door approaches. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Ladder.prefab` | Maintenance/storage story prop, upright against a wall or equipment zone. | Do not imply a climbable route, block overhead clearance, or place where players may mistake it for progression. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Bedside.prefab` | Patient-room support, normally zero or one per bed. Place within believable reach without pinching the approach. | The playable map has an interactive/networked bedside variant. Avoid static duplicates in puzzle/search spaces. This source prefab has six mesh colliders. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Lamp.prefab` | Treatment-room focal support beside a medical table or bed. Aim the head toward the work surface. | Do not cast an unmotivated light automatically; the mesh and the Unity Light are separate decisions. Keep the arm out of head clearance. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Table.prefab` | Mobile treatment/work surface; secondary anchor near a bed, curtain, or lamp. | Keep caster footprint out of the route. Use small props on its top rather than surrounding it with floor clutter. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Serum Holder.prefab` | Vertical medical silhouette beside a bed or treatment table; usually one, occasionally two in a larger treatment room. | Preserve visibility around puzzle/interactable silhouettes. Avoid placing the stand in a pursuit line. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Shelf.prefab` | Storage, maintenance, office, or records anchor. Place flush to a wall; usually one dominant shelf or a short pair. | Do not fill it with every small asset. Preserve nav clearance and avoid occluding wall gameplay. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Mirror.prefab` | Bathroom/treatment wall detail at believable standing height. | Keep flush to the wall. Check reflection cost and do not add a realtime reflection system. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Sink.prefab` | Clinical utility sink where plumbing/function already makes sense. | Wall-align and keep approach clear. Do not invent a plumbing puzzle or interaction. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Toilet.prefab` | Existing bathroom/stall dressing only. | Do not create or resize a bathroom with it. The prefab has two mesh colliders. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Washbasin.prefab` | Existing bathroom wash station, flush to a plumbing wall. | Keep the usable floor area clear; do not use as generic medical furniture. |

## Small medical props

Use these as tabletop or tray-scale clusters. A typical patient/office room needs none or one small cluster; a treatment room may use two clusters separated by function. Avoid loose glass on the main floor.

| Status | Exact prefab path | Intended usage | Restrictions |
|---|---|---|---|
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Hospital Bowl.prefab` | One bowl on a medical table, bedside surface, shelf, or plausible low surface; a slightly rotated placement can carry disorder. | Keep stable and visibly supported; do not scatter multiples across the floor. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Serum.prefab` | Serum bottle paired with a holder or placed on a treatment/storage surface. | Do not present it as a pickup if no interaction exists. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_01.prefab` | Glass family variant for treatment/tabletop clusters. | Choose two to five variants for a focused cluster, not every variant. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_02.prefab` | Glass family variant for treatment/tabletop clusters. | Limit transparent overlap and overdraw. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_03.prefab` | Glass family variant for treatment/tabletop clusters. | Keep off routes and interaction approach zones. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_04.prefab` | Glass family variant for treatment/tabletop clusters. | Use only with a believable support surface. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_05.prefab` | Glass family variant for treatment/tabletop clusters. | Avoid uniform grid placement. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_06.prefab` | Most-repeated reference glass variant; useful as the quiet base of a cluster. | Do not let reference frequency justify excessive scene count. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_07.prefab` | Glass family accent variant. | Keep semantic and visual priority below interactables. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_8.prefab` | Glass family accent variant; note the exact filename uses `_8`, not `_08`. | Preserve the exact path and source name. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_09.prefab` | Glass family accent variant. | Limit transparent overlaps. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Medical Glass_10.prefab` | Glass family accent variant. | Do not place where it resembles a highlighted pickup. |

## Wall, ceiling, and semantic props

| Status | Exact prefab path | Intended usage and approximate placement | Restrictions |
|---|---|---|---|
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Aid Kit Box.prefab` | Wall-mounted medical cabinet only where it is intentionally non-gameplay or explicitly integrated by a gameplay task. | The playable map has an openable/networked variant. A static duplicate is normally misleading. Two mesh colliders. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Electric Box.prefab` | Maintenance electrical focal object on a service wall. | Do not place near the power puzzle or networked switches unless explicitly requested; never imply false progression. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Fire Extinguisher Holder.prefab` | Red semantic accent near corridor junctions, reception, treatment, or exits; mount at believable reach height. | Use sparingly and keep clear of interactables. Two mesh colliders. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Lamp_A.prefab` | Repeating corridor/room fixture; pair with a motivated pool if lighting is in scope. | The reference uses many; the playable map should use fewer. Keep collider out of head clearance. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Lamp_B_01.prefab` | Alternate wall/ceiling fixture for room or transition variation. | Do not alternate mechanically every bay; vary by zone/function. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Lamp_B_02.prefab` | Alternate lamp state/form for sparse broken rhythm. | Verify whether the mesh reads as on/off before pairing with a Unity Light. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Sign Exit.prefab` | Actual exit or authoritative route-signage location. | Never place as generic decoration or contradict escape progression. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Sign_01.prefab` | Institutional wayfinding/signage selected by visible meaning. | Inspect the sign face before use; avoid false route information. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Sign_02.prefab` | Institutional wayfinding/signage selected by visible meaning. | Keep readable and truthful; do not use as random wall noise. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Sign_03.prefab` | Institutional wayfinding/signage selected by visible meaning. | Do not obscure door frames or interaction prompts. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Sign_04.prefab` | Institutional wayfinding/signage selected by visible meaning. | Confirm orientation and message in Game view. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Sign_05.prefab` | Institutional wayfinding/signage selected by visible meaning. | Use only when its message fits the existing room. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Socket.prefab` | Low wall utility detail near desks, treatment equipment, or maintenance runs; roughly one per functional wall pocket. | Keep flush; do not place where it resembles a usable interaction. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Switch.prefab` | Wall utility detail only outside switch/puzzle contexts. | The playable map has a networked light switch. Avoid false interactables by default. |
| `D!` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Toilet Door Base.prefab` | Reference stall partition/base component. | Treat as structural, despite its `Props` folder. Do not instantiate in a dressing pass. Two mesh colliders. |

## Cables

Use cables as surface-bound secondary detail after furniture and wall fixtures. Keep a cable run coherent, supported, and short enough to read. Do not cross walkable floor, door arcs, prompts, or head clearance.

| Status | Exact prefab path | Intended usage | Restrictions |
|---|---|---|---|
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_01.prefab` | Demonstrated cable shape for wall/ceiling service runs. | Surface-mount and inspect collider. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_02.prefab` | Demonstrated alternate cable shape. | Use as part of one readable run. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_03.prefab` | Demonstrated cable accent. | Sparse use only. |
| `C` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_04.prefab` | Compatible unused member of the same imported cable family. | Not demonstrated in the reference. Preview in Unity and use only when another demonstrated cable cannot complete the run. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_05.prefab` | Demonstrated cable shape for maintenance variation. | Avoid unsupported ends. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_06.prefab` | Frequently demonstrated wall/ceiling cable. | Do not over-repeat because the reference used it often. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_07.prefab` | Demonstrated cable variation. | Keep away from readable signage. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_08.prefab` | Demonstrated rare cable accent. | Use as an accent, not a repeating base. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_09.prefab` | Demonstrated cable variation. | Check silhouette against dark walls. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_10.prefab` | Frequently demonstrated cable variation. | Vary spacing and pairings, not random scale. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_11.prefab` | Demonstrated cable accent. | Preserve mounting logic. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Props/Cable_12.prefab` | Demonstrated rare cable accent. | Avoid cluttering principal sightlines. |

## Pipes

Use pipe prefabs only for coherent wall/ceiling service systems. Establish a start, run, turn, support rhythm, and plausible continuation. Keep major corridors clear. Long runs generally align to 2 m/4 m module lengths; connectors and holders should explain joints rather than decorate every segment.

### Large pipe family

| Status | Exact prefab path | Intended usage | Restrictions |
|---|---|---|---|
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Large_2m.prefab` | Short large-diameter straight run. | Wall/ceiling only; keep out of head clearance. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Large_4m.prefab` | Primary long large-diameter run. | Do not copy the reference's very high count into a small zone. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Large_Bent_2m.prefab` | Large bend/offset. | Use only to make a physically coherent turn. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Large_Cor_1m.prefab` | Large corner transition. | Align endpoints precisely. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Holder_Large.prefab` | Support for large runs at plausible intervals and joints. | Do not attach without a pipe; keep flush. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Large_Con_01.prefab` | Large connector variation. | Use at joints, not as evenly spaced noise. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Large_Con_02.prefab` | Large connector variation. | Alternate only where joint function supports it. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Large_Con_03.prefab` | Large connector variation. | Preserve endpoint alignment. |

### Medium pipe family

| Status | Exact prefab path | Intended usage | Restrictions |
|---|---|---|---|
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Med_2m.prefab` | Short medium straight run. | Prefer maintenance, treatment-service, and corridor ceilings. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Med_4m.prefab` | Primary long medium run. | Use fewer parallel lines in low ceilings. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Med_Bent_2m.prefab` | Medium bend/offset. | Use for coherent routing only. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Med_Cor_01_1m.prefab` | Medium corner variation. | Align endpoints and avoid z-fighting. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_A_Med_Cor_02_1m.prefab` | Alternate medium corner. | Do not alternate randomly. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_B_Med_2m.prefab` | Alternate medium service line. | Use to distinguish one system, not every segment. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_B_Med_Bent_2m.prefab` | Alternate-system bend. | Pair with Pipe B runs. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_B_Med_Cor_01_1m.prefab` | Alternate-system corner. | Pair with Pipe B runs. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_B_Med_Cor_02_1m.prefab` | Alternate-system corner variation. | Keep system logic consistent. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Holder_Small.prefab` | Support for medium runs. | Place at joints/intervals; keep flush. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Med_Con_01.prefab` | Medium connector variation. | Use only at joints. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Med_Con_02.prefab` | Medium connector variation. | Use only at joints. |
| `D` | `Assets/FPS Horror Hospital Pack/Art/Prefabs/Pipes/Pipe_Med_Con_03.prefab` | Most-repeated reference connector. | Reference frequency does not justify dense production use. |

## Structural modules: reference only

All paths below are outside this skill's dressing scope. They would change or visually replace the fixed map shell, doorway language, room dimensions, elevation, or route. Do not instantiate them.

### Demonstrated but restricted (`R`)

```text
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Arch.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Arch_Half.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Arch_Half_Con_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Arch_Half_Con_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Column.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_A_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_A_Cor_Ou_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_A_Door_Single_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_A_Window_01_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_B_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_B_Cor_Ou_4x4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_B_Door_Double_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_B_Door_Single_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_B_Window_01_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_B_Window_02_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Door_A_Frame.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Door_B_Frame.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Door_C_Frame.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Door_D_Frame.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_A_02_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_A_4x4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_B_01_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_B_02_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_B_4x4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_Detail_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Metal_Column_01_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Metal_Column_02_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Metal_Column_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Roof_A_2x4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Roof_A_4x4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Roof_A_Window.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Roof_B_4x4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Column.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_Cor_In_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_Cor_Ou_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_Door_Single_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_Window_01_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_Window_02_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Railing_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Railing_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Bottom_Center_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Bottom_Left.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Bottom_Right.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Middle_Center_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Middle_Left.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Middle_Right.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Top_Center_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Top_Left.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Stairs_Wall_Top_Right.prefab
```

### Unused and restricted (`U!`)

These ten structural prefabs were not demonstrated in the reference scene and must not be introduced by this skill:

```text
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_A_Cor_In_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_A_Door_Double_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Corridor_Wall_A_Window_02_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_A_01_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Floor_Detail_2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Roof_B_01_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Roof_B_02_2x2m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Roof_B_2x4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_Window_01_4m.prefab
Assets/FPS Horror Hospital Pack/Art/Prefabs/Structures/Room_Wall_Window_02_4m.prefab
```

## Materials and project-level assets

### Existing shared materials

Use only through the source prefab's existing renderer assignments:

```text
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Cement_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Plaster_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Tiles_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Trim_01_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Trim_02_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Trim_03_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Props_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Props_Emissive_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_Props_Glass_Mat.mat
Assets/FPS Horror Hospital Pack/Art/Textures/Horror_Hospital_WindowGlass_Mat.mat
```

Do not edit, duplicate, recolor, or create instances of these materials for a dressing pass.

### Reference-only scene and rendering assets

Do not assign or copy these into the playable scene as part of dressing:

```text
Assets/FPS Horror Hospital Pack/Scenes/FPS Horror Hospital Environment Scene.unity
Assets/FPS Horror Hospital Pack/Scenes/FPS Horror Models Scene.unity
Assets/FPS Horror Hospital Pack/Lighting/FPS Horror Hospital Lighting.lighting
Assets/FPS Horror Hospital Pack/Volume/FPS Horror Hospital  Volume.asset
Assets/FPS Horror Hospital Pack/URP Files/FPS Horror Hospital.asset
Assets/FPS Horror Hospital Pack/URP Files/FPS Horror Hospital_Renderer.asset
```

Use them only to inspect values and visual patterns. Preserve the target scene's current `LightingSettings`, Volume stack, renderer, pipeline asset, and project packages unless Ian explicitly requests a separate lighting or rendering change.
