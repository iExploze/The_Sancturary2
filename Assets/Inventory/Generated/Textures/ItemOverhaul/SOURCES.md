# Item overhaul art sources

The eight replacement item models and their material textures are original Blender work created for The Sancturary. Editable sources are in `SourceArt/Items`; runtime FBX assets are in `Assets/Inventory/Generated/Models/ItemOverhaul`. The `.blend` files pack their textures. Matching texture files are also retained beside the sources.

`blood_splat.png` is **Blood Splat** by **TobiasM**, released under **CC0** (commercial use and redistribution permitted; attribution optional): https://opengameart.org/content/blood-splat . Downloaded from the original attachment, without changing the artwork. Used in the monster contact and death particle materials.

The local first-person arms derive from this repository's existing `Assets/PlayerModels/Ch16_nonPBR.fbx` player and its existing materials. The shared third-person source remains unchanged. Derived meshes retain the original skinning and only the arm triangles.

The existing adrenaline, medkit, flashlight, and old revolver core models/materials remain in use. Added local treatment implements and moving-part attachment points are presentation additions. `Effects/SleepMistFalloff.asset` is an original procedural radial falloff texture for the distinct sleep mist.
