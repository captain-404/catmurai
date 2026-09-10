# Bugs and verification gaps

| ID | Priority | Finding | Evidence / action |
|---|---|---|---|
| B001 | P0 workflow | Local game unavailable when server is stopped | Browser returned connection refused; node server.cjs restored access. Keep launcher/server running. |
| B002 | P0 verification | Full fresh-save slice duration/balance unknown | Regression tests are not a timed human playtest. Assign independent route audit. |
| B003 | P1 workflow | No repeatable saved milestone footage | Add capture control; verify actual exported video before closing. |
| B004 | P1 technical debt | Modules wrap shared global functions; test suite loads some partial module sources | Claude to review integration boundaries and full-browser coverage; no speculative rewrite. |

No new reproducible core gameplay regression found in the existing automated suite. Missing ultimate/Captain interaction are backlog features, not fixed bugs. Record browser findings in AI_HANDOFF.md.

| B005 | P1 visual | Boss health overlapped zone title | Fixed: hide zone label only during active trials; browser screenshot verified. |
| B006 | P1 readability | Torii/scenery obscure player and boss in the arena | Observed in capture; next gameplay readability task. |

