# Blender production source

Keep accepted, editable `.blend` sources here, outside Unity's `Assets/` folder. Git LFS already covers `.blend` files. Create asset folders only as needed, for example `Props/FrontierRevolver/Frontier_Revolver.blend`, `Characters/<asset>/`, or `Environment/<asset>/`.

After migration, the repository copy is the source of truth for future edits. Keep textures packed or alongside the source with relative paths, and verify the production copy opens without scratch dependencies. Respect Vlad's ownership before editing his work.

Export FBX and required textures explicitly into the closest established Unity feature directory. Saving a Blender file must not trigger an export. Keep backups, caches, experimental maps, and validation renders out of production source.

See [the Blender → Unity workflow](../../BLENDER_UNITY_PIPELINE.md). The Frontier Revolver source has not yet been migrated; see [its source record](../../REVOLVER_IMPORT.md#source).
