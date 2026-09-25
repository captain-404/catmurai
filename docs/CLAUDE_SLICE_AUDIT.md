# Claude — independent slice audit

2026-09-10. Read MASTER_DIRECTIVE.md, MASTER_PLAN.md, BACKLOG.md, BUGS.md, AI_HANDOFF.md, DECISIONS.md first. Reviewed game.js, capture.js, server.cjs, visuals.js, world.js, index.html script order, test.cjs directly (not just docs). No engine/stack change proposed — Canvas 2D + vanilla JS confirmed appropriate; not re-litigating that decision.

## Three highest-impact items, ranked

### 1. Depth/occlusion bug is real, already shipping, and undocumented-vs-code mismatch (evidence, not just Codex's note)
README's "Living Vale update" claims "Trees fade when they would obscure Catmurai." No fade code exists anywhere in the repo — grepped `fade` across every `.js` file; the only match is an unrelated skill-cast fade in `animation.js`. The actual mechanism (`visuals.js` `tree()`, drawn through the `items` y-sorted array built in `world.js`) draws each tree's canopy as a noise-scattered cluster centered around `y - r*1.3` with `r = size*1.5` — canopy paint extends roughly 2x the tree's anchor radius above its own sort key. Depth is resolved purely by sorting all items (trees, player, enemies, NPC) on that one anchor `y`, with no accounting for an item's visual extent. Net effect: a tree whose anchor is marginally "in front of" (greater world-y than) the player paints after the player and can visually bury Catmurai and nearby enemies mid-combat even when the trunk is spatially behind them. This matches DECISIONS.md's own note ("Actual browser blockers remain visible terrain occluding combat... review occlusion/draw ordering first") — confirming it's real, not just a subjective UI complaint, and that the documented mitigation isn't actually implemented.

Why it's structural, not a one-off: `test.cjs`'s renderer smoke test only asserts `imageDraws >= 2` after calling `draw()` — it confirms the renderer executes and paints *something*, never that depth/z-order is spatially correct. This class of bug is invisible to the current test suite by design; it can only be caught by eyes-on browser play, which is exactly why it shipped despite "13 regression groups" passing.

Fix direction (not implemented, flagging for whoever picks this up): split scenery into an always-background canopy pass drawn once before the actor sort, vs. a thin trunk/base element that participates in the y-sort — or actually implement the alpha-fade-on-overlap the README already promises.

### 2. Cross-module coupling is entirely duck-typed, in the hot path, and fails silently
`game.js`'s core `hit()`, `skill()`, and `update()` gate six-plus separate mechanics behind `typeof X === 'function'` / `typeof X !== 'undefined'` checks: `weaponDamage`, `talentBonus` (three separate call sites: damage, execute, mana/regen), `CLASS_PROFILES`, `queueEnemyStrike`, `claimBossReward`. Every add-on system (`weapons.js`, `weapon-classes.js`, `talents.js`, `bosses.js`, `mmorpg.js`) hooks in purely by defining a same-named global that the core loop conditionally calls if present — there is no registration step and no assertion that a script actually loaded. Confirmed via `index.html`'s script order (`talents, game, visuals, enchantment, animation, world, auto-farm, mmorpg, weapons, weapon-classes, bosses, capture` — 12 plain `<script>` tags, no modules, no bundler): correctness depends on this exact order and on every file loading without a syntax error, silently.

Concrete risk given the project's own workflow (two agents, parallel branches, frequent merges): a renamed function, a dropped script tag, or a load-order change during a merge doesn't throw or warn — the game just quietly reverts to base behavior (e.g., talents stop applying, weapon-class attack timing reverts to default) with nothing in the console to point at why. This is the kind of regression that's expensive precisely because it's silent.

Fix direction: at minimum, `console.warn` once at boot for each expected global that's missing (cheap, non-breaking); ideally a small explicit registry (`Systems.register('talents', {talentBonus})`) so a missing system is loud instead of quiet. Not proposing a bundler/module rewrite — that's disproportionate to the actual problem and would fight the zero-build setup that's working fine otherwise.

### 3. Fresh-player balance/pacing is genuinely unverified (pending, not yet evidence-backed)
AI_HANDOFF.md is explicit that Codex's own first-cycle verification "used the existing level-75 save; this is NOT a fresh-player balance test." No fresh-save data exists anywhere in the repo's docs or commit history. I'm not going to guess a verdict from the numbers alone (enemy HP/damage, XP curve, move speed are all readable in `game.js`, but travel time, UI friction, and whether six-kills-then-boss actually feels like 10–20 minutes is exactly the kind of thing that has to be played, not calculated). Attempted to run this live — see below; blocked by a tooling constraint, not a game issue.

## Codex diff review (b7906cc..b2c025b: capture.js, server.cjs additions)

Reviewed against the specific points in AI_HANDOFF.md's ask (capture lifecycle, unsupported-browser/error paths, hidden-tab behavior, resource cleanup, usable export).

- **Lifecycle**: start → `MediaRecorder` on a 30fps canvas stream → 1s dataavailable chunks → 60s auto-stop timer → `onstop` builds the blob, POSTs to `/api/captures`, falls back to a client-side download link on failure. Sound, and matches the README's stated behavior exactly.
- **Unsupported-browser path**: guards `canvas.captureStream`/`MediaRecorder` existence and probes three mimeTypes via `isTypeSupported` before picking one; throws a readable, user-facing message ("Recording unavailable in this browser. Use Chrome.") rather than failing silently. Good.
- **Hidden-tab behavior**: `visibilitychange` → stops recording when the tab is hidden, matching the documented 60s-or-hidden-tab cutoff.
- **Resource cleanup**: `cleanup()` clears the timer/interval and stops every stream track; called from both `onstop` and `onerror`. Previous `blobUrl` is revoked before a new one is created, so repeated recordings in one session don't leak object URLs.
- **One real, narrow gap**: `beforeunload` only prevents/warns on unload while `recorder.state !== 'inactive'`. Once "Stop recording" fires, the recorder becomes inactive immediately, but the async `fetch('/api/captures', ...)` save can still be in flight for a moment after that — there's no unload guard covering that window. Closing the tab in the second or two right after stopping (before the local-save fetch resolves) can silently lose an otherwise-successful recording, with no warning shown. Low-frequency, but worth a one-line fix (extend the `beforeunload` condition to also cover an in-flight save, or track a `saving` flag).
- **Server-side (`server.cjs`)**: origin and content-type are checked before accepting a capture; 100MB cap enforced by counting bytes as they stream in rather than buffering unbounded; filenames are server-generated (`randomUUID`), so there's no path-traversal surface from client input on the write side. Static file serving separately blocks dotfile traversal. No issues found here.

## Fresh-save timed route — attempted, blocked by tooling, not by the game

Ran this live via a browser automation pane on the Captain's machine (isolated context: confirmed empty `localStorage` before starting, so the Captain's real save was never touched). Reached the title screen, started a new game, spoke to Elder Mochi, and accepted "A debt of nine lives" (defeat 6 valley creatures) — all of that worked and matched the docs exactly.

Then hit a hard blocker: the character never advanced toward any target, no matter how long I waited or how many times I clicked/TAB-targeted an enemy. Root-caused it directly in the live page rather than guessing:

- Read the module state directly: `p.x`/`p.y` (player world position) were bit-for-bit identical across an 11+ second window with a live, non-dead `target` set — movement should have been continuous per `game.js`'s own `update(dt)` logic.
- `time` (the game's internal simulation clock, incremented every frame inside `update(dt)`) had advanced only ~2.3 simulated seconds total, despite far more real time having passed since the game started running.
- Confirmed the actual cause with a minimal test: registered a bare `requestAnimationFrame` callback on the live page and it never fired, even 3 full seconds later, even after bringing the automation pane to the foreground (`document.visibilityState` reporting `"visible"` the whole time).

Conclusion: `game.js`'s entire simulation — movement, combat, cooldowns, regen, the quest counter — is gated behind `requestAnimationFrame`, which is correct and normal. But the specific browser automation surface available to me in this session doesn't drive a real compositor/paint loop, so `requestAnimationFrame` never fires there regardless of wait time. That's an environment limitation of the tool I had available, not a bug in Catmurai — the game behaved exactly as its own code says it should the moment a real animation-frame loop is actually running (confirmed by reading the source, not by observation alone).

I don't have a second working browser automation path in this session to route around it (Chrome-extension automation reported not connected). This item stays unverified. Two ways to close it out:
1. The Captain runs it directly: open `http://127.0.0.1:4173` in a private/incognito window (or clear `localStorage['catmurai-save']` in a throwaway profile), do six kills → talk to Elder Mochi → Hollow Fang → return, and note the wall-clock time plus anything confusing along the way.
2. Connect the Claude-in-Chrome browser extension to this session — that drives a real, ordinary Chrome tab rather than an off-screen automation pane, and should not hit this same rAF stall. I can then run the route myself.

Either way, no numbers are being reported for route time or pacing from this pass — flagging the blocker honestly rather than fabricating a timing.

## Scope note
Did not touch `capture.js`, `server.cjs`, or any production/doc files Codex owns this cycle. Did not duplicate the recorder. Claiming: route/balance report (pending live run) and Captain NPC interaction spec, per BACKLOG.md's "Claude proposed" rows — will add the interaction spec as a follow-up in this same file or a new one, once the route timing is in hand.
