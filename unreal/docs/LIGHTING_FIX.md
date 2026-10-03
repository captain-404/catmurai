# Runtime shrine lighting fix - 2026-10-03

The user's seven-unbuilt-objects warning reproduced exactly. Unreal's DumpUnbuiltLightInteractions identified one directional light and seven HISM components: Paving, Basalt, Masonry, Vermilion, RailBeams, RoofTiles and CherryBranches. The directional light inherited stationary mobility and requested baked static lighting for scenery created at runtime. The previous five-round checks did not assert engine unbuilt-lighting counters; this was a validation gap.

Sun and fill are now explicitly Movable; all eight lantern point lights are explicitly Movable before registration. Skylight was already Movable. Instanced terrain remains static for efficient batching. No warning-suppression command or forced counter clearing is used.

Full cook/stage/archive succeeded in Builds/MoonfallLightingFix. Final standalone GPU run reports zero unbuilt lighting objects, zero unbuilt reflections, zero lights with unbuilt interactions and zero primitives with unbuilt interactions. The regression checks all eleven light components, the actual engine counters after dense combat, the paused upgrade menu and subsequent gameplay resume. MOONFALL_ROUND_6_PASS is now emitted only after all checks complete. The upgrade screenshot is captured before resuming on a separate frame.

Gameplay smoke passed: spawning, dash, delayed slash, projectile, defeat/XP, upgrades/resume, defeat state and elite victory. Independent source review confirmed light mobility fixes and prompted stronger end-of-test ordering and skylight coverage. Full logs remain local; results.json contains the inspectable results.

Play-Survival.bat now prefers the fixed standalone build. The user's already running old Moonfall game was preserved and must be closed/reopened to load the correction. Previous builds remain available as fallback.
