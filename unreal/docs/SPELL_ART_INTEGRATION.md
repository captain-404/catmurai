# Spell artwork integration — 2026-10-03

The supplied Factory spell artwork now appears in the survival game's ability HUD, upgrade cards, and animated combat effects. This preserves the existing katana, flying-slash, jump and dash mechanics, v10 skeletal animations, animation hit timing, sword grip locks, damage and upgrade balance.

Canonical playable project: `D:\YoutubeChannel\catmurai-unreal`.
Isolated source branch: `codex/spell-art-effects` in `D:\YoutubeChannel\catmurai-publish`.
Implementation owner: Codex. An independent read-only reviewer checked the presentation and gameplay diff; the cook-rule omission and early cleanup assertion it found were resolved.

## Play

Run `Play-Survival.bat`. It prefers the verified standalone build at `Builds/SpellArt/Windows/CatmuraiUnreal.exe` and falls back to the Unreal editor game launch if the standalone build is absent.

Controls: WASD move, Space jump/double jump, Shift dash, LMB or J katana, Q flying slash, 1/2/3 choose an upgrade, Escape pause, R retry after victory/defeat. Automatic combat still works.

## Artwork mapping

| Supplied PNG | In-game use |
|---|---|
| spell_phantom_claw_rend | Phantom Claw katana icon and animated strike; critical upgrade card |
| spell_shadow_claw_crescent | Shadow Crescent icon and travelling projectile |
| spell_frost_dash | Frost Dash icon and dash departure burst |
| spell_shadow_trio_dash | Movement-speed upgrade card and dash afterimages |
| spell_infernal_moon | Damage upgrade card and elite arrival cue |
| spell_sakura_bloom | Slash-area upgrade card and upgrade celebration |
| spell_spirit_bell | Cooldown upgrade card |
| spell_ember_potion | Maximum-health/healing upgrade card |
| spell_shadow_shuriken_trio | Additional-projectile upgrade card |
| spell_glacial_paw | Knockback upgrade card and normal impact |
| spell_guardian_eye_ward | Player hurt feedback |
| spell_moonlit_pounce | First-jump cue |
| spell_solar_pounce | Double-jump and critical-hit cues |
| spell_shadow_bomb | Enemy defeat cue |
| spell_verdant_lotus_paw | Healing upgrade celebration |

These are themed presentations of the existing abilities and upgrades; the artwork does not introduce 15 separate spell mechanics. The ward art signals an incoming hit, rather than adding shield protection.

## Presentation and performance safeguards

- Original PNGs are copied unchanged to `SourceAssets/Spells`; `manifest.json` records SHA256 hashes and uses.
- `Scripts/import_spell_assets.py` imports the 15 textures, constructs both materials, checks every material graph connection, and saves `/Game/Spells/DA_SpellPresentation`.
- RGB images use a soft luminance black key; RGBA images retain their alpha. No black rectangles surround the sprites.
- Runtime textures build at 512×512; source resolution remains 1254×1254. Both compression and resizing happen in Unreal.
- The ability cards show readiness, remaining cooldown and recharge progress. Upgrade cards show art, rarity and current rank.
- World effects grow, rotate and fade. They use two-sided unlit translucent planes and have no collision. Projectile effects follow the projectile and are destroyed with it.
- Dash trails emit at most once every 0.05 seconds. At most 64 transient spell effects exist per run. Failed projectile visual allocation is attempted once, rather than retried every frame.
- Pause and upgrade selection freeze effects, cooldowns and combat. Presentation failure cannot prevent attacks, movement or damage.
- Explicit cook directories retain the spell presentation library and the pre-existing prototype arena material. The GameFeatureData asset-manager rule fixes a pre-existing import/cook error from the enabled engine tool plugins.

## Verification

See `spell-review/` for captured gameplay and concise evidence. The full local logs remain under `Saved/Logs/`.

- Native editor and standalone Windows builds succeeded.
- Repeatable texture/material import passed with zero errors and warnings.
- `SPELL_RUNTIME_PASS` checks all 15 loaded textures at ≤512 pixels, RGB/alpha modes, effect scale/opacity animation, lifespan, collision isolation, katana hit timing, dash, projectile casting, upgrade pause/resume, attached projectile cleanup, the 64-effect cap and final reclamation.
- Existing `CATMURAI_SMOKE_PASS` verifies spawning, dash movement, katana timing, projectile hits, enemy death, XP pickup, upgrade selection/resume, defeat and elite victory.
- Actual standalone GPU screenshots verify the icons, upgrade art, katana strike, frost trail, crescent projectile and upgrade bloom.
- `-SpellReview -SpellStress` adds a 14-second 80-enemy combat sample with automatic attacks, eight projectiles per cast and repeated dashes. It records mean/p95 frame interval and peak effects; it is a controlled short sample, not a full three-minute balance or performance certification.

The final standalone sample at 1280×720 recorded 2,314 frames after its warmup, mean frame interval 5.19 ms, p95 5.78 ms and peak 64 effects with 80 active enemies. These are gameplay frame intervals on this machine, not isolated GPU timings. Final-run measurements are preserved in `spell-review/PackagedSpellStress.txt`.

## Reproduce

Build the editor target with Unreal 5.8's `Build.bat`, then run the asset script with `UnrealEditor-Cmd.exe <project> -run=pythonscript -script=<Scripts/import_spell_assets.py> -unattended`.

The normal editor game executable accepts `-SpellReview` to capture six staged screenshots and exit with pass/fail. Append `-SpellStress` for the dense-wave sample. The same flags work in the standalone Development build. `-CatmuraiSmokeTest -NullRHI -nosound -unattended` runs the existing gameplay gate.

Standalone build command: `RunUAT.bat BuildCookRun -project=<project> -noP4 -platform=Win64 -clientconfig=Development -build -cook -map=/Game/Maps/SurvivalArena -stage -pak -archive -archivedirectory=<Builds/SpellArt> -unattended -utf8output`.

The short scripted checks do not replace a human full-run playtest. The arena is still a prototype and audio, final arena art and progression expansion remain future work.
