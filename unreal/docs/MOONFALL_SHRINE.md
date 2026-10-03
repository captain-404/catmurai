# Moonfall Shrine: enemy animation and terrain pass

Delivered 2026-10-03 in the canonical Unreal 5.8 project. Reference: the user's Moonfall Shrine concept image. Local isolated source branch: `codex/moonfall-enemies-terrain`.

## Play

Run `D:\YoutubeChannel\catmurai-unreal\Play-Survival.bat`. It prefers the new standalone build in `Builds/Moonfall/Windows`, retaining the previous SpellArt build as fallback. WASD move, Space jump/double jump, Shift dash, LMB slash, Q flying slash, Escape pause, 1/2/3 upgrades, R retry after defeat/victory.

## Implemented

- Six procedural Bleep presentation states: breathing idle, bouncing stride, crouched attack windup, strike lunge, hit recoil, shrinking/tumbling defeat. The existing enemy is a static mesh; these are full-body procedural animations, not newly rigged skeletal clips. Animation modifies only the replaceable visual child. Enemy movement, hit rules and pooling remain independent of the mesh.
- Dead enemies reserve their pooled slot for a 0.32-second defeat pose. Pool reuse resets transforms and clocks. Pause freezes presentation; terminal victory/defeat safely clears dead visuals before the world pauses.
- Reference-inspired shrine arena: cracked stone courtyard, moon mosaic, curved red torii, layered basalt cliffs, flowering trees, rails, pink lanterns, four scenic shrine terraces and stair treads, animated spirit falls, low mist shapes, and purple sky. The satellite terraces and stairs are scenery; the playable combat area is the continuous flat central courtyard, not a connected multi-height island map.
- 1,368 scenery instances grouped into 12 HISM mesh/material batches, plus one sky sphere. Eight shadowless local lights; no ticking terrain actor, no scenery collision, opaque scenery shaders, one HISM tree build after population, reduced ornamental shadows and capped 12,000-unit scenery culling.
- Existing Bleep LOD0/1/2 reused by distance: high detail near player and on elites, medium detail from450units, lowest detail beyond1200units. Distant presentation updates at30Hz beyond1100units; near attacks remain full rate. Activation resets LOD/tick scheduling.
- Paving uses squared Voronoi distances, removing nine square roots per shaded pixel. Shrine material creation explicitly saves instancing permutations. Gameplay floor remains Z=0; invisible safety walls and existing player/enemy clamping prevent falls.

## Five optimization and validation rounds

Each round had implementation changes, a native build, actual GPU-rendered screenshots and the 80-enemy/64-effect workload. Times are measured world frame deltas at1600x900, Development build, localRTX3060 machine; they are not isolated GPU timings or broad hardware benchmarks. Visual complexity increased across rounds, so this is not a controlled before/after speedup claim.

| Round | Change and validation | Mean ms | p95 ms | Result |
|---|---|---:|---:|---|
|1|Procedural state implementation; pooled death reservation/reset, root invariance, pause behavior; baseline flat arena|5.82|6.64|PASS|
|2|Instanced shrine/material integration; terminal cleanup;81 floor samples and4 boundary traces. Screenshots found grey fallback, instancing flags corrected and rerun|5.99|6.63|PASS after correction|
|3|Distance-based animation rate and three authored mesh LODs; jagged cliffs replace round caps; supported gate terraces and stairs; near/far LOD assertions|6.38|7.23|PASS|
|4|One HISM tree build instead of repeated per-instance builds; reduced canopy shadows and culling range; moonlit atmosphere without continuous sky capture; visual review|6.63|7.56|PASS|
|5|Squared-distance paving shader and smooth basalt grain; clean overview and edge captures; final material and workload checks|6.63|7.48|PASS|

Final packaged run:1,212 timing samples,6.61ms mean,7.71ms p95,80 enemies and64 peak spell effects. Arena/material/instancing,81 floor samples,4 boundaries, six animation states, pool lifecycle, pause, terminal cleanup, near/far presentation settings and nonempty stress workloads all passed. Screenshots are in `moonfall-review/packaged`.

## Regression verification and repairs

- Packaging completed with `BUILD SUCCESSFUL`, full Windows cook/stage/archive.
- Packaged `CATMURAI_SMOKE_PASS`: spawn, real dash travel, delayed katana hit, projectile hit, death/XP, upgrade choices/resume, defeat, elite victory.
- Packaged `CATMURAI_V10_RUNTIME_PASS`: idle/run, jump/double jump, dash/air dash, landing, delayed slash arc, locked hand/weapon rotations, face/blink, coyote/buffered jumps, dash jump cancellation, haste. Older second-air-dash test waited0.65seconds after a shortened double jump and could run after landing. Moved its check to0.15seconds and added an explicit airborne/used-dash precondition; both passed. Gameplay player code was not changed.
- Packaged `COMBAT_RUNTIME_PASS` with rendering enabled: all three enemy render LODs, grounding, movement, attack telegraphs, dodge/knockback interruption, damage, dash/jump evasion, full-speed attacks, four-direction facing. First NullRHI attempt reported zero static-mesh render LODs because rendering was disabled; diagnostic labels exposed this and the complete GPU test passed with3LODs for every enemy. No gameplay defect was masked or assertion removed.
- Packaged `SPELL_RUNTIME_PASS`: existing spell textures, UI icons, upgrades and combat effects loaded and rendered in the new shrine.
- Independent source and screenshot audit completed; corrected terminal cleanup, material fallback and incomplete stress/terrain assertions. No remaining correctness blocker found in reviewed changes.

## Current limits

This is a playable terrain/art prototype inspired by the concept, not a reproduction of its finished art. Primitive geometry remains visible; the central courtyard stays flat and clear for survival combat. Enemy limbs are not independently rigged, and terrain terraces are not traversable. The80-enemy stress case produces overlapping telegraph rings. Niagara mist/waterfalls, authored enemy rigs, multi-height navigation, more varied map routes and final lighting/art remain future polish.

## Reproduce

Build `CatmuraiUnrealEditor Win64 Development`, regenerate materials with `Scripts/create_shrine_materials.py` through Unreal's Python commandlet, and run the game with `-MoonfallReview -ReviewRound=5 -windowed -ResX=1600 -ResY=900`. Logs end with `MOONFALL_ROUND_5_PASS`; screenshots are in `Saved/MoonfallReview/round-5`. Run Bleep/Combat review with rendering enabled, not NullRHI. All five round logs, original fallback evidence and final regression logs are preserved under `docs/moonfall-review`.
