# Item overhaul audio sources

All source recordings are CC0 1.0. Commercial game use and source redistribution are permitted; attribution is optional. Voluntary credits and original URLs follow. No audio was purchased or generated.

| Source | Creator | Original page |
|---|---|---|
| Kenney | Kenney | https://kenney.nl/assets/impact-sounds |
| RPG | Kenney | https://kenney.nl/assets/rpg-audio |
| Firearms | Ben Jaszczak, Brian Nelson, Kevin Heras, Matthew Nanney | https://opengameart.org/content/the-free-firearm-sound-library |
| Melee | Ben Jaszczak and Brian Nelson | https://opengameart.org/content/medieval-sound-effects-weapon-textures |
| Wet | Independent.nu (Johannes Pinter), archived by qubodup | https://opengameart.org/content/8-wet-squish-slurp-impacts |
| Clicks | LFA | https://opengameart.org/content/equipment-clicks-iii |

Offline edits: mono 48 kHz PCM16, trimmed head/tail silence, removed DC, balanced peak levels, short anti-click fades. Rate changes and layering are listed below. AUDIO_SOURCES.json contains source hashes, exact time selections, offsets, gains, duration and level measurements.

| Local filename | Source recordings | Additional edits |
|---|---|---|
| item_revolver_fire.wav | Firearms: Firearms\Prepared SFX Library\Smith & Wesson 642\V_27P.wav | [None, None]s at 1× |
| item_shotgun_fire.wav | Firearms: Firearms\Prepared SFX Library\Nova\O_21P.wav | [None, None]s at 1× |
| item_tranq_fire.wav | Melee: Melee\Crossbow Shoot.wav; Clicks: equipment_clicks3.wav | [None, None]s at 1.4×; [2.15, 2.5]s at 1.4× |
| item_revolver_dry.wav | Clicks: equipment_clicks3.wav | [7.95, 8.2]s at 1× |
| item_shotgun_dry.wav | Clicks: equipment_clicks3.wav | [6.35, 6.75]s at 0.85× |
| item_tranq_dry.wav | Clicks: equipment_clicks3.wav | [18, 18.3]s at 1.3× |
| item_fireaxe_swing.wav | Melee: Melee\Axe Swing.wav | [None, None]s at 0.83× |
| item_crowbar_swing.wav | Melee: Melee\Katana Swing.wav | [None, None]s at 1.12× |
| item_fireaxe_wood_impact.wav | Kenney: Kenney\Audio\impactWood_heavy_001.ogg | [None, None]s at 0.86× |
| item_crowbar_pry.wav | RPG: RPG\Audio\creak2.ogg; Kenney: Kenney\Audio\impactMetal_medium_002.ogg | [None, None]s at 1×; [None, None]s at 1× |
| item_flashlight_toggle.wav | Clicks: equipment_clicks3.wav | [0.87, 1.12]s at 1.05× |
| item_revival_syringe_use.wav | Clicks: equipment_clicks3.wav | [9.57, 9.94]s at 1.13× |
| item_medkit_use.wav | RPG: RPG\Audio\handleSmallLeather.ogg; RPG: RPG\Audio\cloth1.ogg | [None, None]s at 1×; [None, None]s at 1× |
| item_adrenaline_use.wav | Clicks: equipment_clicks3.wav | [0.18, 0.55]s at 1.18× |
| item_reload_revolver.wav | Clicks: equipment_clicks3.wav | [1.43, 1.91]s at 0.95× |
| item_reload_shotgun.wav | Clicks: equipment_clicks3.wav | [2.64, 3.12]s at 0.84× |
| item_reload_tranq.wav | Clicks: equipment_clicks3.wav | [3.23, 3.63]s at 1.08× |
| item_revolver_insert.wav | RPG: RPG\Audio\handleCoins.ogg; Clicks: equipment_clicks3.wav | [None, None]s at 1×; [12.12, 12.34]s at 1× |
| item_shotgun_insert.wav | Clicks: equipment_clicks3.wav | [12.53, 12.98]s at 0.87× |
| item_tranq_insert.wav | Melee: Melee\Crossbow Place Bolt.wav | [None, None]s at 1.65× |
| item_revolver_close.wav | Clicks: equipment_clicks3.wav | [4.24, 4.7]s at 1× |
| item_shotgun_close.wav | Clicks: equipment_clicks3.wav | [13.08, 13.65]s at 0.87× |
| item_tranq_close.wav | Clicks: equipment_clicks3.wav | [16.17, 16.65]s at 1.15× |
| item_syringe_inject.wav | RPG: RPG\Audio\cloth4.ogg; Clicks: equipment_clicks3.wav | [None, None]s at 1×; [20.8, 21.1]s at 1.5× |
| item_adrenaline_inject.wav | RPG: RPG\Audio\cloth2.ogg; Clicks: equipment_clicks3.wav | [None, None]s at 1×; [18.5, 18.78]s at 1.4× |
| item_medkit_treat.wav | RPG: RPG\Audio\clothBelt.ogg; RPG: RPG\Audio\cloth3.ogg | [None, None]s at 1×; [None, None]s at 1× |
| item_syringe_complete.wav | Clicks: equipment_clicks3.wav | [17.43, 17.8]s at 1× |
| item_medkit_complete.wav | RPG: RPG\Audio\cloth4.ogg | [None, None]s at 1× |
| item_adrenaline_complete.wav | Clicks: equipment_clicks3.wav | [21.25, 21.65]s at 1.2× |
| item_flashlight_equip.wav | RPG: RPG\Audio\beltHandle1.ogg | [None, None]s at 1.1× |
| item_revival_syringe_equip.wav | RPG: RPG\Audio\cloth1.ogg | [None, None]s at 1.1× |
| item_medkit_equip.wav | RPG: RPG\Audio\handleSmallLeather2.ogg | [None, None]s at 1.1× |
| item_adrenaline_equip.wav | RPG: RPG\Audio\cloth2.ogg | [None, None]s at 1.1× |
| item_crowbar_equip.wav | RPG: RPG\Audio\drawKnife1.ogg | [None, None]s at 1.1× |
| item_fireaxe_equip.wav | RPG: RPG\Audio\clothBelt2.ogg | [None, None]s at 1.1× |
| item_revolver_equip.wav | RPG: RPG\Audio\beltHandle2.ogg | [None, None]s at 1.1× |
| item_shotgun_equip.wav | RPG: RPG\Audio\dropLeather.ogg | [None, None]s at 0.8× |
| item_tranq_equip.wav | RPG: RPG\Audio\drawKnife3.ogg | [None, None]s at 1.1× |
| item_revolver_ammo_pickup.wav | RPG: RPG\Audio\handleCoins2.ogg | [None, None]s at 1× |
| item_shotgun_shell_pickup.wav | RPG: RPG\Audio\cloth1.ogg; Kenney: Kenney\Audio\impactTin_medium_001.ogg | [None, None]s at 1×; [None, None]s at 1× |
| item_tranq_dart_pickup.wav | RPG: RPG\Audio\cloth2.ogg; Kenney: Kenney\Audio\impactGlass_medium_002.ogg | [None, None]s at 1×; [None, None]s at 1× |
| monster_hit_1.wav | Wet: Wet\impsplat\impactsplat01.mp3.flac; Kenney: Kenney\Audio\impactPunch_heavy_000.ogg | [None, None]s at 1×; [None, None]s at 1× |
| monster_hit_2.wav | Wet: Wet\impsplat\impactsplat02.mp3.flac; Kenney: Kenney\Audio\impactPunch_heavy_001.ogg | [None, None]s at 1×; [None, None]s at 1× |
| monster_hit_3.wav | Wet: Wet\impsplat\impactsplat03.mp3.flac; Kenney: Kenney\Audio\impactPunch_heavy_002.ogg | [None, None]s at 1×; [None, None]s at 1× |
| monster_death_burst.wav | Wet: Wet\impsplat\impactsplat04.mp3.flac; Wet: Wet\impsplat\impactsplat07.mp3.flac; Kenney: Kenney\Audio\impactPunch_heavy_003.ogg | [None, None]s at 0.72×; [None, None]s at 0.82×; [None, None]s at 0.65× |
| monster_dart_contact.wav | RPG: RPG\Audio\cloth1.ogg; Clicks: equipment_clicks3.wav | [None, None]s at 1×; [18, 18.3]s at 1.5× |
