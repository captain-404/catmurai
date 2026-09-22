# Catmurai: Moonfall — Unreal prototype

This is the Unreal Engine 5.8 companion prototype for the Catmurai game.

## MCP workflow

The project enables Unreal MCP and All Toolsets. The editor is configured to
serve the MCP endpoint locally at `http://127.0.0.1:8000/mcp`. Start Unreal
Editor first, then open Codex from this project root.

MCP remains loopback-only and requires no remote exposure. Tool calls that
write to the project are configured to request approval by default.

## First editor run

Requires Unreal Engine 5.8.2 and Visual Studio C++ build tools. Open
`CatmuraiUnreal.uproject` and allow Unreal to build the project modules.
The included startup map is `/Game/Maps/SurvivalArena`.
After building, use `Play-Survival.bat` (adjust its engine path if necessary).

The current version includes the rigged Catmurai V2 character, textured katana,
idle/run/slash animations, survival combat, upgrades, and the reference-inspired HUD.
See `docs/CATMURAI_HANDOFF_V2.md` for integration details and verification results.

This is a source project, not a packaged standalone game. Generated binaries,
caches, logs, local agent settings, and import backups are excluded. Imported
Unreal assets are included; the external FBX handoff is needed only for reimporting.
