# Catmurai: Moonfall

A single-player 2D browser RPG prototype inspired by classic point-and-click MMORPG combat. Features an original Catmurai avatar, the supplied title and portrait artwork, four skills, auto-attacks, enemy respawns, leveling, gold rewards, potion purchases, weapon upgrades, a shrine quest and Oni boss, and local saving.

## Play
Open index.html directly in a modern desktop browser, or run Play-Catmurai.bat (requires Node.js), then visit http://127.0.0.1:4173. Keep the server window running. If Node is unavailable, opening index.html is sufficient.

Click to move, click enemies to approach and auto-attack. WASD moves manually and cancels the current attack. Tab selects nearby enemies. 1–4 activate skills; Q drinks a potion; E speaks to Elder Mochi within the shrine; I opens equipment. Dialogues pause the game. The shrine is a safe zone.

Defeat six valley creatures, return for rewards, upgrade your blade, then follow the gates northeast to the Hollow Fang. Return to the elder after victory to complete the chapter. Gold and XP are collected automatically. Progress saves in your browser under catmurai-save; position and current health reset at the shrine on reload.

## Scope
This is a local, single-player prototype with procedural 2D scenery and a simplified drawn Catmurai avatar. It is not a 3D MMORPG and does not include multiplayer, accounts, networking, full animation assets, or a persistent server. The emote sheet is not used. Fonts are optional Google Fonts with local fallbacks; all gameplay works without external dependencies.

## Development
node --check game.js
node test.cjs
node server.cjs

## Illustrated graphic update
The playable avatar now renders assets/catmurai.png, an unmodified copy of the transparent user reference (1024 x 1536). The character is a 2D image-based actor, with facing, idle sway, movement bob, attack lean, shadow, and skill effects; it is not a rigged 3D mesh or a frame-animated walking model. visuals.js replaces the prototype scenery and actors with layered cherry trees, stone paths, a detailed shrine and torii, lantern glow, redesigned enemies, petals, and enhanced combat effects. Existing saves and game rules remain compatible.

## Animated edition
Run the local server for this edition (`node server.cjs` or the launcher). The renderer decodes chroma-keyed atlases once into isolated transparent canvas frames. Opening the HTML as a file may prevent texture decoding; use http://127.0.0.1:4173.

- Eight pose walking cycle with distance-driven timing and left/right facing.
- Six-pose sword sequence with windup, strike, follow-through, and recovery; Moon Cut holds the windup and adds a timed blue trail.
- Whirlwind uses front/side/back turning poses and layered rotating blade trails.
- Spirit Mend and Iron Fur use casting poses, rising particles, and green or gold effects, with gameplay changes at the release time.
- Hit reactions and defeat use existing poses with procedural recoil/collapse/fade; they are not separate hand-drawn clips. Idle breathing is procedural. This remains a 2D sprite game with limited poses, not skeletal or eight-direction animation.
- Movement locks during committed actions. Damage is delayed until strike frames and checks that the enemy is still alive and in range. Casting does not accept duplicate input. Dialogues and hidden tabs pause the animation clock.
- Select Animations in the footer to inspect each clip, slow playback, pause, step frames, or mirror facing. Return to game exits preview. Preview freezes world simulation.
- animation-review.html provides a read-only contact-sheet view of the registered poses.

Validation: gameplay regression checks; animation integration tests for costs, action locks, delayed damage/healing/guard, all clips, preview simulation isolation, and 30/60/120 FPS movement; browser inspection of transparency, pose registration, sword bounds, cast frames, and turning frames. Generated animation art is an adaptation of the reference and differs in proportions/details from the original static portrait.

## Living Vale update
Scenery and NPCs use two new generated atlases: assets/vale-props.png and assets/vale-npcs.png. The first contains sakura, pine, shrine, torii, mossy rocks, and stone lanterns. The second contains six poses each for Elder Mochi, kitsune, shade ronin, and the Oni. Runtime keying and connected-component cleanup isolate the frames; row bounds preserve ears/horns and a wider crop preserves the ronin sword.

The world has wind-driven foliage and grass, a traversable brook with swimming fish, ripples, reeds and stepping stones, butterflies, lantern flicker, shrine smoke, bells, and paper streamers. Trees fade when they would obscure Catmurai. The elder blinks and gestures after interaction. Enemies switch between idle, movement and attack frames; attacks apply damage after a 0.32-second windup and recheck range, death and shrine safety. Enemy corpse effects retain their illustrated appearance. Scenery motion is procedural; NPCs use limited-pose sprite animation.

View world-review.html to inspect the NPC contact sheet and six scenery props without changing gameplay progress. Browser reviews checked all poses and props, transparency, silhouette completeness and in-game scale. Automated checks cover rendering, enemy attack timing, dodging, safe zone, interrupted strikes and respawn state, alongside the existing game and character-animation regressions.

## Auto-farm
Press F or select Auto-farm in the footer. Catmurai targets the nearest living regular enemy anywhere in the valley, approaches, auto-attacks, and uses available skills. It reserves some spirit for healing, uses potions below 35% health, patrols hunting spots when all regular enemies are defeated, reacquires respawns along the route, and avoids deliberately targeting the boss or using Whirlwind beside it. Existing gold/XP collection remains automatic.

WASD, a ground/enemy click, Tab, Escape, F, or the toggle returns control to you. Dialogues, animation preview, and hidden tabs pause farming along with the game. Defeat stops the mode. Below 20% health without an immediately usable healing option, farming stops and Catmurai heads toward the shrine. Auto-farm starts off after reload and does not purchase items, upgrade equipment, or turn in quests.

Validated target selection, boss exclusion, distant target seeking, hunting-spot patrol, respawn reacquisition, healing/potions, retreat, stop behavior, dialog pause, and defeat handling in automated tests; browser verified the control and live target acquisition.


## MMORPG HUD and passive talent trees
Press N or select Talents in the footer. One point is available for each level after level 1, including previously earned levels. Three paths each have three talents with three ranks; max out the preceding talent to unlock the next. Passives modify damage, skill damage, finishing damage, maximum health, armor, health/spirit regeneration, healing and cooldowns. Learning is immediate and saved with the character. Reset talents is free and refunds all points; it never heals you. The tree supports 27 total invested points; later points remain unspent after all nodes are mastered.

The revised HUD includes a live world minimap, player resource frames, target health, distinct skill slots with hotkeys and descriptions, animation/cast status, compact quest tracking and a highlighted unspent-talent indicator. Equipment shows talent-adjusted basic damage and passive statistics. Dialogs pause gameplay. The interface adapts to narrower screens and talent panels scroll on short displays.

UI review: first pass approximately 7.5/10; second pass approximately 8/10 (subjective internal assessment of hierarchy, consistency, readability and available gameplay space). Browser review used an 888 x 912 viewport. Verified learning, rank cap, next-tier unlocking, save/reload and free respec. A final respec check exposed incorrect visual unlocks for missing zero-rank entries; both the renderer and respec normalization were corrected. Automated tests verify the point budget, prerequisites, serialization, malformed-rank repair, damage/armor/health/healing/cooldown effects and the existing gameplay suite. These scores are not an external usability study.

Weapon enchantment visuals follow the current katana upgrade (`Equipment [I] > Temper blade`). +0 has no aura; +1-3 frost, +4-6 azure, +7-9 violet, +10-14 arcane pink, +15 and above crimson with violet ribbons. Intensity grows up to +20, with bounded particle counts. The blade-local effect follows authored walk and combat poses and hides when the blade is concealed. These are Catmurai's own tiers inspired by the supplied reference. Read-only comparison: http://127.0.0.1:4173/enchantment-review.html.

Armory: Kaji the Bladesmith stands beside the shrine at (155, 60). Press V nearby to purchase level-gated blades using gold; press I to equip or temper owned weapons. Moonsteel, Emberfang, Jade Spirit and Eclipse share the sword animation set with different blade finishes and combat bonuses. Each blade retains its own enchantment. Existing saves keep their original Moonsteel upgrade.

Weapon classes: Crimson Battle Axe (level 5, 620 gold), Twin Crescent Swords (level 6, 720 gold), and Eclipse Greatblade (level 8, 900 gold) now use their own illustrated idle, walk, wind-up, strike, recovery, and casting poses. Axe basics impact at 0.56s / 1.30s cadence; dual basics hit at 0.28s and 0.47s for half damage each / 0.88s cadence; greatsword basics impact at 0.65s / 1.50s cadence. All use existing skills with class poses, mirrored facing, and weapon-attached enchantment. The short frame loops are sprite animation, not skeletal 3D. Existing Eclipse ownership and enchantments are retained. Read-only visual gallery: /weapon-class-review.html.

Boss trials: press B at the shrine for Awakened Hollow Fang (level 4), Broken Moon (7), or Stormforged (10). Each has telegraphed hazards and a 50% enrage phase. First clears grant an exclusive +4 weapon, 500 gold, 300 XP and 3 seals; repeat clears grant 100 gold, 100 XP and 1 seal. Spend 3 seals for +1 equipped enchantment. Health scales at entry with current attack strength. Trials disable auto-farm; death or leaving the arena resets the fight without rewards. Existing story Hollow Fang quest is separate. Records, relic ownership and seals save locally. Art uses existing animated NPC archetypes.
