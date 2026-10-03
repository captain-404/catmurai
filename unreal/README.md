# Catmurai: Moonfall — Unreal survival

Unreal Engine 5.8.2 survival prototype with the v10 animated Catmurai character, Bleep enemies, automatic katana/flying-slash combat, dash, double jump, XP upgrades and a three-minute elite encounter.

## Play

Run `Play-Survival.bat`. It starts the local standalone Windows build when available, otherwise launches the survival map through Unreal Engine at `D:\UE_5.8`.

WASD moves; Space jumps/double jumps; Shift dashes; LMB or J attacks; Q casts the flying slash; 1/2/3 selects an upgrade; Escape pauses; R retries after a finished run. Automatic attacks remain enabled.

## Spell artwork update

The 15 supplied Factory spell images now illustrate ability cooldowns and upgrade choices and animate katana strikes, crescent projectiles, frost dash trails, jumps, hits and upgrades. Originals are preserved in `SourceAssets/Spells`; the imported art, materials and replaceable presentation library are in `Content/Spells`.

[Integration notes and validation](docs/SPELL_ART_INTEGRATION.md) explain the artwork mapping, safeguards, tested standalone build and 80-enemy stress sample. [Gameplay screenshots](docs/spell-review/02-phantom-claw.png) show the actual packaged game.

## Editor and MCP

Open `CatmuraiUnreal.uproject`; the startup map is `/Game/Maps/SurvivalArena`. Start Unreal Editor before Codex to use the built-in experimental MCP endpoint at `http://127.0.0.1:8000/mcp`. ModelContextProtocol and AllToolsets remain enabled. The editor-only tool plugins may report startup Python diagnostics when running the editor executable in game mode; the standalone build does not load those editor tools.

Keep game builds, intermediate/cache files and raw footage local. Develop on isolated `codex/` or `claude/` branches. The former OneDrive checkout is retained as a backup; the canonical playable project is `D:\YoutubeChannel\catmurai-unreal`.
