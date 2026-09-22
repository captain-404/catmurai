# Catmurai Survival: first Unreal playable milestone

## Architecture
UE 5.8.2 C++ runtime module `CatmuraiSurvival`, with Blueprint-editable tuning structs and a `USurvivalTuning` Data Asset. Gameplay does not depend on the placeholder presentation meshes. `CombatCue` is a Blueprint event intended for replaceable Niagara/audio feedback.

- `Source/CatmuraiSurvival/Survival.h`: reflected data, component and actor contracts.
- `SurvivalActors.cpp`: player movement/dash, health, modular two-ability loadout, pooled enemies/XP, flying slash.
- `SurvivalRun.cpp`: director, arena dressing, upgrade selection, run lifecycle, canvas HUD.
- `Content/Maps/SurvivalArena.umap`: dedicated entry arena; geometry is constructed by game mode.
- `Content/Materials/M_Prototype.uasset`: reusable parameterized placeholder material.
- `Scripts/`: reproducible Unreal asset setup scripts. Map setup replaces the prototype arena; do not rerun over a hand-authored level.

## Play
Double-click `Play-Survival.bat`. This developer launcher requires the installed UE 5.8 engine and compiled editor module; it is not a packaged distributable.
WASD move; Space dash; automatic katana and flying slash attack nearby enemies; left click / Q request those attacks. 1/2/3 selects an upgrade. Escape pauses/resumes; R restarts after defeat/victory.

Three enemy stat profiles enter progressively. Enemy/XP actors are pooled; the director updates AI centrally with an 80-enemy cap. Eight upgrade types have capped ranks. Survive three minutes, then defeat the elite to win.

## Implementation order
1. Complete: native compile, movement, dash, attacks, arena, enemy director, damage/death, XP and three-choice progression, elite and end states.
2. Next: replace geometric placeholders with rigged Catmurai/enemy assets, authored movement and attacks, Niagara and real audio; improve enemy separation and readability.
3. Then: balance full-length runs, profile large swarms, add ability evolutions and persistent progression, build packaged Windows release.

## Verification and limits
The initial native build and runtime smoke passed. The visible game rendered, collected XP, presented three upgrade choices and resumed combat when 1 was pressed. The final strengthened smoke additionally checks dash displacement, projectile segment damage and actual orb pickup; see `Saved/smoke-slice.log` for its result.
Smoke is opt-in via `-CatmuraiSmokeTest -game -NullRHI`; it exercises actual actors but calls some ticks directly, so it is not a substitute for a full human playthrough.

This is a greybox gameplay milestone, not final visual quality. All three enemies use distinct primitive silhouettes rather than finished models. Attacks use debug-drawn placeholder VFX. Audio hooks exist but no sounds are supplied. HUD uses Canvas text rather than production UMG/icons. No meta-progression, packaging or full-duration balance/performance certification yet. Existing experimental engine toolset plugins produce PythonTestRunner/GameFeatureData startup diagnostics unrelated to the native gameplay smoke; these remain to be resolved separately.

Ownership: Codex implemented this isolated Unreal module. Independent Claude audit has not occurred. Browser Catmurai source was not modified by this milestone.
