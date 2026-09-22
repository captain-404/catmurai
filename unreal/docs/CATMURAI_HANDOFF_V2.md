# Catmurai character and sword integration

Source: `C:\AI-Lab\Catmurai_Unreal_Handoff\`, imported 2026-09-22.

The playable character uses `Characters/CatmuraiV2/SK_Catmurai_Drawn`:
the supplied rigged character, katana on `weapon_r`, and scabbard on `sheath_l`.
The previous static character and cube blade are no longer used by the player.
Gameplay collision and combat remain independent of the presentation mesh.

Idle, Run, and Slash sequences share the base skeleton. Movement switches to Run;
attacks play Slash and return to locomotion. The supplied blade is not stretched
by upgrades; Sweeping Katana increases gameplay slash reach.

## Import details

- Importer: `Content/Python/import_handoff_v2.py` (legacy FBX factory).
- Asset check: `Content/Python/validate_handoff_v2.py`.
- Actual source coordinates imported at 0.977 Unreal units tall. Uniform scale
  **153** produces a measured **149.54 cm** height. The handoff's proposed 1.53
  setting alone did not produce the required centimeter dimensions.
- Body, cape, tail, and katana have dedicated materials using the supplied textures.
- Cape uses masked alpha at 0.5 and is two-sided.
- Katana uses base color, emissive, and linear ORM (R AO, G roughness, B metallic).
- All four materials explicitly enable skeletal-mesh usage.
- Original FBX, textures, Blender files, and prior static assets are preserved.
- Intermediate unsuccessful imports are recoverable in `Saved/HandoffImportBackup`
  and `Saved/HandoffScaleBackup`; these are not game content.

## Verification

- C++ editor build succeeded (`Saved/build-handoff-v2.log`).
- Fresh asset reopen passed: non-null shared skeleton, 4 mapped skeletal materials,
  149.54 cm bounds, masked cape, linear ORM, and 3 non-empty animation sequences
  (`Saved/validate-handoff-v2-final.log`).
- Runtime combat/progression smoke passed (`Saved/smoke-handoff-v2.log`).
- On-screen close-up confirmed textured Catmurai and held sword, dash/run pose,
  and the sword following the hand through the combat slash. No missing-material
  usage warnings remain in `Saved/handoff-v2-preview-final.log`.

Experimental engine toolset plugins still emit startup diagnostics in game mode;
the Python commandlet can return exit 1 for unrelated GameFeatureData settings.
Use the explicit validation markers, not an exit-code-only claim.

## Remaining art work

This update does not implement procedural spring bones/cloth, LOD imports, or a
sheathe/draw mechanic. The supplied Drawn variant includes the working sword;
standalone weapon exports and alternate variants remain available in the handoff.
