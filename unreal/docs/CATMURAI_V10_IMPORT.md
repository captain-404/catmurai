# Catmurai v10 Unreal update — 2026-09-27

Source pack: `D:\YoutubeChannel\handoff\Catmurai_UE_v10_2026-09-27`.
Branch: `codex/catmurai-v10-moves`. Implementation and verification: Codex.
The supplied pack, Unity checkout, shared plans and `main` are unchanged.

## Play

Run `Play-Survival.bat` in the canonical `D:\YoutubeChannel\catmurai-unreal` project.
WASD / left stick move; Space / gamepad South jumps; press again for a front flip;
Shift / gamepad East dashes; J, left mouse or gamepad West slashes; Q fires the
existing wave; Escape pauses. Existing automatic combat remains enabled.

## Assets and scale

- Mesh: `/Game/Characters/CatmuraiV10/SK_Catmurai_v10`.
- Fresh skeleton: `SK_Catmurai_v10_Skeleton`; 50 source bones plus FBX armature root.
- Nine `A_Catmurai_*` sequences: Idle, Run, Slash, JumpStart, JumpRise, JumpFall,
  JumpLand, JumpFlip and Dash. Whole takes, 30 fps, in place, root motion disabled.
- Legacy FBX import scale **153** for mesh and animation. Body height: **149.736 cm**.
  Combined bounds are **208.358 cm** because the drawn sword extends beyond the body.
- Body heights for LOD1/2/3: **149.637 / 149.443 / 149.617 cm**.
- Four LODs: **79,005 / 26,364 / 14,748 / 10,268** total triangles, including the
  retained 7,366-triangle sword. Screen thresholds: 0.60 / 0.30 / 0.10 / 0.02.
- Lower-detail meshes come from the supplied LOD FBX. The preparation script adds
  the skinned sword and exports at 0.01 to compensate for the FBX unit conversion.
- Body base colour and flipped-green normal map, masked two-sided cape (0.5 clip),
  tail texture, and katana colour/emission with retained v2 ORM. All material slots,
  including LOD-created suffix slots, explicitly support skeletal meshes and morphs.
- Blink and MouthOpen are present on LOD0. The scabbard is absent from the v10 mesh.
- Original source assets are copied under `SourceAssets/CatmuraiV10`. Old v2/v9
  game assets remain intact for comparison and rollback.

## Behaviour

`V10Movement.cpp` ports the supplied baseline: 367 cm/s movement, 720 degrees/s
turning, 168 cm jump, 0.12 s coyote and buffer windows, 1.6x falling/released-jump
gravity, one 0.9-height air jump, 60% directional redirect, 612 cm / 0.2 s dash,
0.6 s dash cooldown and one air dash. Dash collision uses CharacterMovement;
air dash pauses gravity, and a jump cancels dash with 1.3x movement momentum.

Slash lasts 0.95 s, slows movement to 20%, and checks its 245 cm / 110-degree arc
at 35% of the animation for 12 baseline damage. Existing damage/area/critical
upgrades remain supported. Haste scales both slash playback and impact timing.
Manual attacks can swing without a target; automatic attacks retain target aiming.

`UV10AnimInstance` implements the requested state graph in native C++, with
crossfades, velocity-based rise/fall blending, landing transitions and run playback
clamped to 0.8–4x. It is a native equivalent, not an editable Animation Blueprint.
Gameplay collision, movement and damage remain independent of the presentation.

Bounded damped springs add cape, tail, ear and ribbon motion after animation;
the last two tail bones remain rigid and ears use the calmer six-degree limit.
Blink and mouth breathing/panting/slash morphs run on the character. A soft fill
light and disabled motion blur keep the dark costume and fast moves readable.

## Sword constraint

The pack contradicted itself about Slash. Inspection found up to 126.48 degrees
of wrist rotation and 51.01 degrees of weapon rotation in that clip. The explicit
non-negotiable rule takes precedence: both local rotations are locked to bind
pose in imported clips and enforced after animation blending at runtime. The
original translation keys, including the supplied paw-grip offset, are retained.
Shoulder/elbow animation still drives the arm. Source FBXs are not modified.

## Verification and review

- UE 5.8.2 Development Editor build: succeeded.
- Fresh process asset reopen: `CATMURAI_V10_REOPEN_PASS`.
- Rendered movement/animation suite: `CATMURAI_V10_RUNTIME_PASS`.
- Existing survival regression: `CATMURAI_SMOKE_PASS` (spawn, dash, delayed katana,
  projectile, kills, XP, choices/resume, defeat and elite victory).
- Rendered checks cover jump height, air limits, dash distance/height/cancellation,
  coyote time, short hops, buffered landings, ledge falls, delayed damage arc,
  wrist/weapon rotations, face morphs, blinks and haste timing.
- Reviewed captures include idle, run, jump, upside-down flip, landing, dash,
  slash and all four LODs. See [the review gallery](v10-review/index.html).
  The v9 comparison uses the retained old mesh/idle under the same review camera
  and lighting; it is not a screenshot of the original arena lighting.
- Detailed logs remain in `Saved/*v10.log`; a compact evidence record accompanies
  the gallery. Tests use real world actors; screenshot runs use a fixed 60 Hz step.

## Reproduce

Build the editor target before running scripts that call `V10AssetTools`.
`Scripts/prepare_v10_lods.py` runs in Blender and uses project-relative paths.
`Content/Python/import_v10.py` sets up new assets and preserves existing verified
mesh/animation assets on rerun. `finalize_v10.py` rebuilds the LOD chain, remaps its
materials, applies grip constraints and saves. `validate_v10.py` is read-only.
Do not use stale automatic Interchange reimport settings for a new source revision.

Launch the game with `-CatmuraiV10Test -UseFixedTimeStep -FPS=60` for rendered
verification and `Saved/V10Review` captures; add `-CatmuraiBeforeV10` for the retained
old-model comparison. `-CatmuraiSmokeTest -nullrhi` runs the survival regression.

## Limits

Secondary motion is a bounded spring approximation, without cloth/body collision.
Keyboard/gamepad bindings are configured. Desktop window activation failed during the final interactive check, so no manual keyboard or physical controller playthrough is claimed. Face morphs
are LOD0 only as supplied. The stricter grip correction changes the source Slash's
blade path and still merits the artist's approval. This is not a full balance or
performance certification. Experimental engine toolsets report pre-existing
GameFeatureData configuration errors in commandlet startup; explicit validation
markers and absence of Python failures establish the asset-check result.
