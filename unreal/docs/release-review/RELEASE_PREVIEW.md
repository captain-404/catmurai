# Catmurai single-player preview - 2026-10-05

A standalone Windows Shipping build is available at `Builds/SinglePlayerPreview/Windows/CatmuraiUnreal.exe`. `Play-Survival.bat` opens this version. It does not require the Unreal editor. Earlier builds remain available.

## Added in this development pass

- Original Catmurai title artwork, three starting disciplines, armory, settings, pause and results screens. Mouse, keyboard and controller menu support.
- Permanent damage, health and Fortune training purchased with earned moon shards. Profile validation, guarded purchases, save failure feedback, and one reward per finished run.
- Ember, Frost and Storm infusions, chain lightning, Steam Burst / Shatter / Tempest Nova combinations, orbiting spirit blades that evolve at rank 3, healing and XP upgrades, and charged Moonfall Judgment.
- Eight-minute solo hunt with rotating clearing, shrine attunement and spirit collection objectives. Objectives heal and reward experience.
- Frost Warden at 2:30, Shade Ronin at 5:00, and Moon Eater at 8:00. Boss health is 1600 / 2600 / 4200. Earlier champions must be defeated before the next arrives; only the final boss ends the hunt.
- Chaser, runner, brute and ranged Hex Caller behavior; distinct role and boss palettes, readable windups, locked firing directions, swept projectile collisions and enemy spacing.
- Four original low-cost shrine meshes, painted basalt paving, cloud artwork, courtyard ruins and petal beds, spell-art shrine markers, active shrine guidance, bloom and depth fog.
- Original procedural ambient music and eleven sound cues, live music/SFX settings and per-cue voice throttling.
- Clean retry state, bounded enemies/effects/projectiles/hazards, three enemy mesh LODs and reduced distant animation updates.

The Spell Brigade inspired escalating hordes, rotating objectives and elemental combinations. These systems and the artwork are original to Catmurai; no assets from that game were copied. Reference: https://store.steampowered.com/app/2904000/The_Spell_Brigade/

## Optimization and validation rounds

| Round | Change | Result | Frame time mean / p95 |
|---|---|---|---|
| 1 | Integration: corrected boss health multiplication and safe spawn approaches. | PASS (21 checks) | 6.47 / 7.55 ms |
| 2 | Menus and combat: fixed heading overlap, capture timing, camera framing, enemy spacing and sound caps. | PASS (21 checks) | 6.48 / 7.55 ms |
| 3 | Expanded objective/save QA: tested disk reload, shrine attunement, spirit collection, pause, dodges and retry; exposed a timing assumption in the telegraph test. | FAIL (31 checks) | 6.68 / 8.23 ms |
| 4 | Fixed telegraph test timing; added backdrop coverage and low courtyard detail. All checks passed. | PASS (31 checks) | 6.52 / 7.42 ms |
| 5 | 1080p polish: spell-art markers, shrine guide, ranged warnings, reset and Fortune rank consistency. | PASS (31 checks) | 8.21 / 9.58 ms |
| 6 | Final enemy palettes, audio feedback and backdrop coverage. All checks passed. | PASS (31 checks) | 7.62 / 8.32 ms |

The round 3 failure was a verification timing assumption: an existing warning expired before the check. The test now waits for a queued warning and verifies that pausing freezes it and moving out avoids damage. The gameplay hazard logic did not require a timing change.

The legacy gameplay smoke passed (spawn, dash, delayed katana hit, projectile, death, XP pickup, upgrade selection/resume, defeat and elite victory). Native rounds directly checked zero unbuilt lighting objects and zero unbuilt reflections. Lighting diagnostics are excluded from Shipping compilation because Unreal removes those counters there.

The final Shipping build/cook/archive succeeded. Its executable completed the acceptance scenarios and returned exit code 0, including all objective types, boss scheduling and health, ultimate, profile reload, purchase rejection, rewards, retry and effect/population caps. Twelve screenshots were captured from that packaged executable. Test saves use CatmuraiReleaseReviewProfile, separate from the player's CatmuraiReleaseProfile.

Performance measurements are local automated 80-enemy stress scenarios, not a minimum-hardware or whole-run guarantee. Review mode accelerates objective and boss milestones and uses damage immunity to keep the visual tests alive; these tests do not certify normal-play difficulty. The screenshots are rendered game frames, not concept mockups.

## Play and controls

Run Play-Survival.bat. Select Moonblade, Ember or Frost, then Begin the Hunt.

WASD / left stick moves; Space / A jumps and double jumps; Shift / B dashes; LMB / X attacks; Q casts Crescent; E / Y casts Moonfall at 100 charge; Tab toggles auto attacks; Esc / Start pauses; M opens options. Arrow keys / D-pad and Enter / A navigate menus. Settings save immediately.

Defeat earns a small shard reward. Defeating the Moon Eater earns a larger reward. Armory training applies to the next hunt. Returning to title midway forfeits that hunt's rewards.

## Remaining release gates

This is a playable solo preview, not release certification. Fresh-save human playthroughs are still needed to tune the full eight-minute difficulty curve, upgrade odds and audio mix. Physical controller testing, accessibility/remapping, lower-spec and varied display testing, installer/store integration, final asset/license review and broader crash/soak coverage remain. Enemies still use procedural animation of the supplied static Bleep mesh; bespoke skeletal enemy animation and additional enemy models remain art work. The map remains one arena with decorative satellite shrines.

## Artwork and source

Original generated title, paving and cloud PNGs are preserved in SourceAssets/Release; exact generation calls are in ARTWORK_PROMPTS.md. Meshes have editable ReleaseEnvironment.blend and FBX sources. Music and cues are original procedural audio. Existing user-supplied spell and character art are preserved. The user should complete the final distribution rights review for supplied art before public release.

Source changes are isolated on codex/single-player-release. Main and previous packaged builds are preserved.
