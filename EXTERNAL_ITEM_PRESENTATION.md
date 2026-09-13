# External held-item presentation

This pass uses the existing equipment instance replication and confirmed item action sequence. Each client constructs the external wrapper locally. No new replicated fields, networked visual objects, bone synchronization, animation events, or gameplay authority changes were added.

## Catalog coverage

| Equipable definition | Hold style / pose | External action |
|---|---|---|
| Flashlight | OneHanded / OneHandedCarry | Toggle motion; existing replicated beam |
| Med Kit | OneHanded / OneHandedCarry | Raise and tilt the existing medical prop |
| Adrenaline | OneHanded / OneHandedCarry | Quicker, larger raise/tilt |
| Revival Syringe | OneHanded / OneHandedCarry | Forward injection reach |
| Crowbar | TwoHanded / TwoHandedTool | Shared tool swing |
| Fire Axe | TwoHanded / TwoHandedTool | Heavier tool swing |
| Old Revolver | OneHanded / Pistol | Recoil, dry fire, external reload |
| Tranq Gun | TwoHanded / LongGun | Light recoil, dry fire, external reload |
| Sawed-Off Shotgun | TwoHanded / LongGun | Heavy recoil, dry fire, external reload |

The catalog is authoritative. Ammunition, keys, and the other non-equipable definitions retain their existing rules. The crowbar now participates in the existing two-arm tool pose.

## Editing grips

Open `Assets/Inventory/Prefabs/Held<Item>Remote.prefab`. Move/rotate `VisualRoot` to change the item relative to the existing body anchor. Move/rotate its `RightHandGrip` and `LeftHandGrip` children to refine wrist placement. `HeldItemVisual` owns these references, and both targets move with the item during actions. Keep muzzle/effect origins aligned with their existing models. Do not move the wrapper root to tune placement: equipment resets that root when instantiating it.

Only the right arm participates for one-handed carry and pistol poses. Long guns and tools use both arms. Grip positions are deliberately approximate; finger closure and anatomical wrist alignment remain manual polish. No new Animator states, layers, masks, or clips were created. Existing item pose layers now blend with hand-target activation. The player Animator uses Always Animate so hidden/offscreen body bones do not freeze while equipment targets continue moving.

Reload is a presentation-only 1.1-second lower/tilt/recover motion, adjustable on each external wrapper through the Third-person Reload fields. It reacts to the existing confirmed Reload action. The current gameplay reload transfers ammo immediately when compatible ammunition is dragged onto the weapon; the visual does not delay or perform that transfer. Late joiners reconstruct equipped items and ongoing timed uses through existing state; completed one-shot actions are not replayed.

External item orientation follows the existing smoothed look direction rather than adding the crouching chest tilt, while its position stays attached to the torso. Short external actions advance by at most a quarter of their duration per rendered frame, preventing a slow frame from skipping recoil entirely. This only changes the cosmetic external clock.

The five rebuilt external models (Crowbar, Fire Axe, Revival Syringe, Tranq Gun and Sawed-Off Shotgun) use the newer Blender assets from `codex/item-combat-overhaul`, commit `ad7ea32875b398694e68817edfcf2d606dc63c4f`. Their FBXs, materials and textures are copied unchanged with their original GUIDs. The editable originals and export reports remain in `SourceArt/Items` on that branch. No downloaded third-party art was introduced. The four retained core models (Flashlight, Med Kit, Adrenaline, Old Revolver) remain as authored.

First-person prefab transforms and use motions are preserved. The longer injection reach and added reload apply only to external presentation.

## Manual solo and host/client checks

1. Open Unity 6000.3.15f1, wait for import/compilation, then use `SandboxPrototype` for the existing direct solo session. Equip each of the nine items. Check the owner sees one first-person item and the external model does not obstruct the camera.

2. Open `Assets/Scenes/SandboxLaunch.unity` in each Editor, or launch two Windows instances with `-sandbox`. Create/Join the same room, Ready both players, then Start Game on the host. Use the sandbox item area to acquire items; repeat the following once with the host holding them and once with the client holding them.

3. Equip Flashlight, Revival Syringe or Med Kit, Crowbar, Fire Axe, Old Revolver, Tranq Gun, and Sawed-Off Shotgun. The observer should see the correct external model, right-arm-only small item/pistol poses, and both hands following tools/long guns. Toggle the flashlight and confirm its remote beam follows the replicated on/off state and look direction.

4. Walk, sprint, crouch, and turn with each family. Legs must retain locomotion while hands hold the item. Look up/down and observe that the item remains attached. Rapidly switch Flashlight â†' Fire Axe â†' Old Revolver â†' Tranq Gun â†' Flashlight; no stale model or unused arm pose should remain.

5. Use the staging wall's `Hurt SELF - 50 HP` and `Down SELF - revive test` buttons with F, then use medical items; revive a downed teammate with the syringe while aiming within the existing interaction range. The observer should see a readable raise/use or forward reach. Confirm normal authoritative healing, revival, and consumption still occur.

6. Swing each tool. Reload each firearm by dragging compatible ammunition onto it, then fire; shotgun recoil should be larger than revolver/tranq recoil. Empty the weapon and check the small dry-fire motion. Visual motion must not cause extra damage, ammo changes, or item consumption.

7. Unequip, drop, consume, enter/leave a locker, and die while equipped. The external item and IK must clear where appropriate and rebuild correctly afterward. Rejoin while another player holds an item and confirm the current item appears without replaying an old shot or reload.

## Validation performed

- Unity 6000.3.15f1 compiled the final C# changes. Final Windows development build succeeded with zero errors (493 existing project/asset/shader warnings). Build: `Builds/ExternalItems/TheSancturary.exe`.

- Four focused PlayMode tests passed: all catalog equipables, hand reach and unused-arm release, owner suppression, unequip/consume/drop/death cleanup, presentation-only use/reload, and slow-frame recoil.

- EditMode suite: 70/71 passed. The existing world-item collision test expects a MeshCollider on `WorldOldRevolver`; that unchanged prefab intentionally uses compound boxes as documented in `REVOLVER_IMPORT.md`. No world pickup collider changes were made here.

- The rebuilt external-model regression cases passed for all five newer assets; all transferred meshes and materials resolve correctly.
- Two standalone Windows instances used the existing Photon host/client menu/lobby flow and SandboxPrototype. All nine items passed on both peers: correct external definition, distinct owner/proxy representations, appropriate hand weights, and reachable grips. Final measured right-hand error was under 3 cm; left-hand error was under 7 cm for the two-handed families.

- Confirmed use actions for Flashlight, Med Kit, Adrenaline, Crowbar and Fire Axe, plus firing/recoil, reload and dry fire for all three firearms, passed in both host-to-client and client-to-host directions. The final external action measurements recorded motion from the rest pose.

- Rapid Flashlight/Fire Axe switching, walking/sprinting, crouching with forward item orientation, and equipped dropping passed in both directions. A host revived a downed client: the client observed syringe use, health became 50, and consumption cleared the syringe. A late-joining client reconstructed an already-equipped revolver without replaying an old action.

- Existing client lobby startup code logged an unspawned `FusionLobbyPlayerState.Slot` read, but the session proceeded successfully. This pass does not alter that networking code.

- First-person prefab assets, existing source models/materials, Animator controllers/clips/masks, scenes, packages, Photon configuration and registration were checked unchanged. Temporary connection/setup and multiplayer diagnostic scripts and their metas were removed before the final build.

Remaining manual verification: locker entry/exit and vent transitions, four-player load, network loss/latency stress, flashlight beam appearance, and subjective locomotion/action feel. All wrist/finger fits are rough, especially wrist rotation and the long-gun support palms; refine the prefab grips in Unity as needed. No additional setup is required.
