# Static Bleep enemies

All three enemy profiles and the elite use SM_Bleep_Final. AI, attacks, health, speeds, drops and pooling are unchanged. No skeleton or animation is required. Original profile sizes and the elite size multiplier remain; Bleep keeps its own finished materials rather than the old primitive tint.

Source: artifacts/bleep-refined/Bleep-Final/Bleep_Final.blend. Exact source hash and export geometry are recorded in SourceAssets/BleepFinal/source-manifest.json. The original blend is preserved outside the repository. Scripts/export_bleep.py exports only the three finished meshes and their packed textures.

The mesh has authored 65k / 25k / 8k triangle LODs. Each uses its own normal map, with green flipped for Unreal, normal strength 0.3, metallic 0.04 and emission strength 1.4. Unreal build scale converts the imported meter-sized coordinates to centimeters. A separate scene root keeps gameplay positioning independent of the bottom-origin visual. Missing-asset fallback retains the original primitives.

Run Content/Python/import_bleep.py with the full Unreal Editor's -ExecutePythonScript option (the static mesh subsystem is unavailable in the Python commandlet). It preserves existing assets and can safely be rerun. Use -BleepReview in game mode for a rendered four-variant check plus mesh, LOD, grounding, pursuit, defeat and reuse checks. Screenshots go to Saved/BleepReview. Existing -CatmuraiSmokeTest exercises the survival gameplay loop.

Verified 2026-09-27: editor build succeeded; BLEEP_IMPORT_PASS (99.91 cm, three material slots); BLEEP_RUNTIME_PASS with rendered image inspection; canonical assets/source/binary hashes matched; canonical CATMURAI_SMOKE_PASS covered spawn, dash, katana, projectile, death, XP, upgrade choices/resume, defeat and elite victory. Final facing offset is -90 degrees. No animation was added.
