# Item asset provenance

The September 2026 overhaul replaces eight item models with original Blender-authored assets in `Models/ItemOverhaul`, with editable packed sources and export/validation manifests in `SourceArt/Items` at the repository root. Adrenaline, MedKit, Flashlight and OldRevolver retain their original core art. Current item audio uses licensed CC0 recordings documented in [Audio/AUDIO_SOURCES.md](Audio/AUDIO_SOURCES.md); particle texture credits are in [Textures/ItemOverhaul/SOURCES.md](Textures/ItemOverhaul/SOURCES.md).

The following August record documents retained historical source assets. Its generated-audio description does not describe the current item definitions' sound sets.

## Historical provisional assets

Generated on 2026-08-07 for The Sancturary. These assets are provisional and may be replaced by Vlad's authored work. Meshy outputs were requested as textured FBX files with realistic PBR materials, clean UVs, 2K textures, and low-to-mid polygon density. Fal audio used the `cassetteai/sound-effects-generator` model and was imported as WAV. Unity wrapper prefabs provide non-destructive scale, pivot, grip, collider, and URP presentation adjustments; source FBX files are unchanged.

## Meshy models

- `RevivalSyringe.fbx`: "Game-ready original emergency revival injector from an abandoned psychiatric institution. Oversized readable medical syringe, aged stainless steel frame, thick glass barrel containing dark red medicinal fluid, worn rubber finger grips and metal plunger, practical late-20th-century design, unsettling but medically plausible, no sci-fi holograms, no gore, no hands, no text, no logo, no brand, neutral isolated object, realistic proportions around 22 cm long, clean PBR materials, low-mid poly, clean UVs, 2K textures."
- `Crowbar.fbx`: "Game-ready worn steel crowbar for an abandoned institution, approximately 65 cm long, strongly readable curved claw end and flattened pry end, dark oxidized steel with chipped faded red paint, scratches and age but not buried in rust, sturdy practical silhouette, no hands, no text, no logo, no brand, isolated neutral object, realistic scale, clean PBR materials, low-mid poly, clean UVs, 2K textures."
- `FireAxe.fbx`: "Game-ready institutional emergency fire axe, approximately 80 cm long, heavy red-painted steel axe head with chipped worn paint, aged wooden or dark fiberglass handle, practical believable proportions, readable blade and rear pick, maintained enough to function but visibly old, no blood, no gore, no hands, no text, no logo, no brand, isolated neutral object, realistic PBR, low-mid poly, clean UVs, 2K textures."
- `OldRevolver.fbx`: "Game-ready generic old six-shot double-action revolver, worn blued steel frame and cylinder, aged dark wooden grip, late-20th-century civilian or institutional weapon, mechanically believable but not copied from a branded firearm, no tactical accessories, no hands, no text, no logo, no serial numbers, no brand, isolated neutral object, approximately 27 cm long, realistic PBR, low-mid poly, clean UVs, 2K textures."
- `RevolverAmmo.fbx`: "Game-ready generic six-round revolver speedloader holding six brass cartridges, old but functional metal and dark polymer speedloader body, clear readable cartridge silhouette, no weapon included, no hands, no text, no logo, no brand, isolated neutral object, realistic scale, PBR materials, low-mid poly, clean UVs, 2K textures."
- `TranqGun.fbx`: "Game-ready original single-shot veterinary tranquilizer gun from an abandoned research institution, compact two-handed pneumatic dart gun, aged grey-green metal body, small pressure cylinder, simple iron sights, practical and clearly non-military, no modern tactical rails, no hands, no text, no logo, no brand, isolated neutral object, approximately 65 cm long, mechanically believable, realistic PBR, low-mid poly, clean UVs, 2K textures."
- `TranqDart.fbx`: "Game-ready single veterinary tranquilizer dart, large readable capped needle body, transparent or pale medical tube, dark red dose chamber, bright worn tail fins for identification, no syringe gun, no hands, no text, no logo, no brand, isolated neutral object, approximately 12 cm long, realistic PBR, low-mid poly, clean UVs, 2K textures."
- `SawedOffShotgun.fbx`: "Game-ready original old single-barrel break-action sawed-off shotgun, one-shell capacity, shortened worn steel barrel, aged receiver, cracked dark wooden grip and short fore-end, crude but mechanically believable survival weapon, no modern tactical parts, no hands, no text, no logo, no serial number, no brand, isolated neutral object, approximately 55 cm long, realistic PBR, low-mid poly, clean UVs, 2K textures."
- `ShotgunShell.fbx`: "Game-ready single generic 12-gauge shotgun shell, worn dark red plastic hull with brass base, clearly readable shape at inventory scale, no weapon, no hands, no printed text, no logo, no brand, isolated neutral object, realistic proportions around 6 cm long, realistic PBR, low-mid poly, clean UVs, 2K textures."

## Fal sound effects

- `item_flashlight_toggle.wav`: "Short dry close-up mechanical thumb switch click from a small old metal flashlight, one firm click with subtle casing movement, no ambience, no music, less than half a second."
- `item_revival_syringe_use.wav`: "Close-up emergency medical injector action, rubber grip movement, metal plunger click and brief compressed medicinal hiss, tense and practical, no voice, no music, under one second."
- `item_medkit_use.wav`: "Close-up survival horror first-aid kit use: cloth medical pouch unzips, gauze unwraps, compact antiseptic bottle cap clicks, one brief bandage pull. Dry tactile foley, no voice, no music, no ambience, no silence before the sound, single concise action."
- `item_adrenaline_use.wav`: "Fast urgent use of a small stimulant medicine bottle, sharp cap snap, quick gulp and subtle breath intake without speech, no music, dry close recording, about one second." A second Fal result from this same brief was selected for its clearer injector-like mechanical transient.
- `item_crowbar_swing.wav`: "Fast heavy steel crowbar swing through air, close dry whoosh with slight metal vibration, no impact, no music, under one second."
- `item_crowbar_pry.wav`: "Heavy steel crowbar forcing open an old wooden and metal institutional obstruction, strained wood creak, metal scrape and final latch pop, no voice, no music, about one second."
- `item_fireaxe_swing.wav`: "Heavy fire axe swung rapidly through air, strong close dry whoosh, no impact, no music, under one second."
- `item_fireaxe_wood_impact.wav`: "Heavy fire axe striking and splitting an old wooden barricade, solid wood crack with short metal head impact, no voice, no music, under one second."
- `item_revolver_fire.wav`: "Single old revolver gunshot recorded close indoors, sharp mechanical report with controlled short room tail, powerful but not cinematic explosion, no voices, no music, under two seconds."
- `item_revolver_dry.wav`: "Single empty revolver trigger pull and hammer click, close dry mechanical sound, no gunshot, no ambience, under half a second."
- `item_tranq_fire.wav`: "Single pneumatic veterinary tranquilizer gun firing, compressed gas pop, spring action and small mechanical clack, clearly quieter than a firearm, no voice, no music, under one second."
- `item_tranq_dry.wav`: "Empty pneumatic dart gun trigger click with tiny valve clack, no projectile and no gunshot, dry close recording, under half a second."
- `item_shotgun_fire.wav`: "Single sawed-off shotgun blast recorded close indoors, heavy low-frequency report with short controlled room tail and mechanical break-action rattle, no voices, no music, under two seconds."
- `item_shotgun_dry.wav`: "Single empty old shotgun hammer and trigger click, heavy dry mechanical clack, no shot, no ambience, under half a second."
- `item_reload_revolver.wav`: "Very short revolver speedloader insertion and cylinder snap, six cartridges shifting with precise metal clicks, no gunshot, no music, under one second."
- `item_reload_single_round.wav`: "Very short single shell or dart inserted into a break-action chamber with one clean metal click, no gunshot, no music, under one second."

The selected Fal results were converted to mono 48 kHz PCM, trimmed only around inactive head/tail space, peak/RMS normalized, and given short anti-click fades. This production pass preserves the generated sound content while making playback level and latency consistent in Unity. Superseded retry files were removed after the final clips were verified.

## Existing licensed source assets reused

- Flashlight wrappers reuse `Assets/Flashlight/Model/Flashlight.prefab`, imported from Unity Asset Store package **Flashlight**, product ID `18972`, package version `1.61`.
- Med Kit and Adrenaline wrappers reuse `Assets/Props/Serum.prefab`, imported from Unity Asset Store package **FPS Horror Hospital Pack**, product ID `335086`, package version `1.0`. Each wrapper uses its own material variant; the source prefab is not modified.

No imported third-party source asset was destructively edited. No generated model or sound is represented as Vlad's work or as a final shippable asset.
