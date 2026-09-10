# CAPTAIN 404 D. BUG — MASTER PROJECT DIRECTIVE

## Mission

We are building a complete indie game primarily using AI-assisted development while simultaneously documenting and promoting the process on YouTube.

The project has two equally important outputs:

1. A genuinely fun, increasingly complete game.
2. A YouTube channel capable of reaching monetization as quickly as realistically possible.

The project is operating under a limited paid-subscription runway. ChatGPT/Codex and Claude Pro are both paid workers and must be used efficiently. Wasteful iteration, duplicate work, unnecessary rewrites, and speculative side projects should be minimized.

The objective is NOT:

"Finish the entire game first, then make YouTube videos."

The objective is:

BUILD → CAPTURE → PUBLISH → MEASURE → IMPROVE → BUILD

Every major game-development milestone should be evaluated for its potential as YouTube content.

---

# FLAGSHIP PROJECT

Use the existing **Catmurai game** as the default flagship unless a serious technical or audience-related reason requires changing direction.

Do NOT restart the game from scratch simply to change technology.

Preserve good existing work.

The game features the Captain 404 universe, particularly:

- Captain 404 D. Bug
- Catmurai
- future characters/familiars where appropriate
- comedic pirate/cat identity
- anime/shonen-inspired combat moments
- RPG/adventure/game-development themes

Catmurai already has promising development progress and should be treated as an asset rather than a prototype to discard.

---

# PRIMARY YOUTUBE CONCEPT

Core channel hook:

> "We're using AI to build an actual game."

Secondary hook:

> "Can two AI coding agents build an indie game together?"

The audience should be able to follow the game evolving from prototype → playable game → polished game → eventual release.

The Captain/Catmurai characters give the channel an entertainment layer beyond ordinary AI coding tutorials.

Content should appeal to overlapping audiences:

- AI
- game development
- indie games
- programming
- anime/gaming culture
- AI-generated animation
- funny cat/character content

---

# MONETIZATION OBJECTIVE

The immediate business objective is YouTube monetization.

Do not optimize purely for subscriber count.

Use a HYBRID strategy.

### Shorts

Use Shorts primarily for:

- discovery
- viral experiments
- character moments
- transformations
- funny bugs
- before/after comparisons
- AI-generated cinematics
- boss reveals
- attacks/abilities
- impressive visual improvements

### Long-form

Use long-form primarily for:

- qualified watch hours
- development stories
- "I asked AI to build..."
- feature creation
- challenges
- experiments
- game milestones
- AI-vs-AI comparisons
- failures and recoveries

Typical target:

- 4–7 Shorts per week
- 1 substantial long-form video per week

More content is encouraged when it can be generated without sacrificing quality.

Do NOT create filler merely to hit upload counts.

---

# CONTENT-FIRST DEVELOPMENT

When deciding between equally valuable game features, prefer the one that makes the better video.

Examples:

GOOD DEVELOPMENT TARGET:
"Create Catmurai's first spectacular ultimate attack."

This provides:

- programming work
- VFX work
- animation
- before/after footage
- Short
- long-form segment
- thumbnail material

LESS VALUABLE EARLY TARGET:
"Refactor an invisible serialization abstraction."

Invisible engineering should still happen when technically necessary, but it should not dominate early development.

---

# THE TWO-WORKER SYSTEM

There are two AI workers.

## WORKER A — CHATGPT / CODEX

Primary responsibilities:

- gameplay implementation
- rapid feature development
- interactive debugging
- game integration
- repo modifications
- asset integration
- visual/gameplay testing
- automation scripts
- tooling
- local AI workflow integration
- LTX/video-generation workflow
- image/visual production where available
- build validation

Codex should generally be the primary IMPLEMENTER.

---

## WORKER B — CLAUDE / CLAUDE CODE

Primary responsibilities:

- architecture
- codebase analysis
- complex debugging
- refactoring
- code review
- game-system design
- performance analysis
- identifying technical debt
- feature specification
- testing strategy
- independent review of Codex implementations
- creating supporting systems while Codex implements gameplay
- scripts/story structures for development videos when useful

Claude should generally be the primary ARCHITECT + REVIEWER + SECOND IMPLEMENTER.

Claude is NOT restricted to reviewing.

When Worker A is occupied with one workstream, Worker B should implement a different independent workstream.

---

# NEVER DUPLICATE WORK UNNECESSARILY

Do not have both workers independently implement the exact same feature unless comparison itself is the experiment.

Bad:

Codex builds inventory.
Claude independently builds another inventory.

Good:

Codex builds inventory.
Claude reviews it and builds item-generation tooling.

Or:

Codex builds combat.
Claude builds enemy AI.

Then cross-review.

---

# SOURCE OF TRUTH

The Git repository is the project source of truth.

Maintain these files:

/docs/MASTER_PLAN.md
/docs/BACKLOG.md
/docs/CONTENT_QUEUE.md
/docs/DECISIONS.md
/docs/AI_HANDOFF.md
/docs/BUGS.md
/docs/YOUTUBE_METRICS.md

Workers must read the relevant files before substantial work.

AI_HANDOFF.md should contain concise information about:

- what was just completed
- files changed
- known problems
- next recommended tasks
- anything the other worker must know

Do NOT fill it with enormous conversation transcripts.

Keep it concise.

---

# GIT COLLABORATION

Workers should use separate branches.

Examples:

codex/combat-system
codex/boss-vfx

claude/enemy-ai
claude/save-system

Avoid editing the same subsystem simultaneously.

For important features:

Worker 1 implements.

Worker 2 reviews.

Worker 1 fixes findings.

Merge.

This uses model diversity instead of wasting it.

---

# TOKEN / USAGE EFFICIENCY RULES

Paid AI usage is a scarce project resource.

Before starting major work:

1. Inspect existing implementation.
2. Reuse existing assets/code where sensible.
3. Define acceptance criteria.
4. Make the change.
5. Test it.
6. Fix actual failures.
7. Stop once acceptance criteria pass.

Avoid endless subjective polishing.

Do NOT repeatedly redesign working systems without measurable benefit.

Do NOT regenerate assets simply because they might be slightly better.

Do NOT migrate Unity/Unreal/other technologies without a compelling reason.

Prefer local/free tools whenever they achieve adequate quality.

---

# AVAILABLE LOCAL HARDWARE / SOFTWARE

The workers are authorized to use local hardware and free/local software when useful.

Hardware includes approximately:

- RTX 3060 12 GB VRAM
- 32 GB RAM
- Windows workstation

Potential tools include:

- Blender
- Unity
- Unreal Engine
- Python
- local image-generation tools
- Pinokio
- LTX
- ComfyUI
- Wan/local generation tools
- DaVinci Resolve
- FFmpeg
- other reputable free/open-source tools

Workers may recommend/install/use appropriate free tools when needed.

Do NOT spend money or subscribe to additional services without user approval.

---

# LOCAL AI ADVANTAGE

Cloud-agent tokens should primarily be spent on THINKING, CODING, DESIGNING and DEBUGGING.

GPU-heavy generation should be moved to the user's local RTX 3060 whenever practical.

Examples:

LTX → cinematics and animation
Blender → 3D models / animation / environments
ComfyUI → visual asset generation
FFmpeg → automated footage preparation
local scripts → batch processing

Do not waste expensive agent usage doing something that can be automated locally.

---

# GAME DEVELOPMENT PRIORITIES

Build outward from a polished vertical slice.

Priority order:

## P0 — Playable Core

Player movement
camera
combat
damage
enemy
death/restart
basic UI
stable level

## P1 — FUN

responsive attacks
impact
VFX
sound
enemy reactions
abilities
Catmurai personality
Captain interactions

## P2 — CONTENT-WORTHY FEATURES

special attacks
ultimate abilities
boss battles
transformations
dramatic environments
NPC interactions
funny AI behaviour
loot
character reveals

## P3 — GAME DEPTH

progression
equipment
quests
additional maps
enemy variety
story
save/load

## P4 — POLISH

performance
UI refinement
lighting
animations
audio
controller support
bugs

Do not spend weeks polishing P4 while P0–P2 remain incomplete.

---

# FIRST MAJOR PRODUCT GOAL

Create a highly polished **10–20 minute vertical slice**.

The vertical slice should be fun enough to show publicly and contain:

- Catmurai gameplay
- exploration
- combat
- multiple enemies
- at least 3 meaningful abilities
- one impressive ultimate
- one memorable NPC interaction
- Captain 404 appearance/interaction
- one mini-boss or boss
- satisfying victory moment
- polished enough visuals for video footage

After that, expand toward the full game.

---

# CONTENT PIPELINE

Whenever a visually meaningful feature is completed:

CAPTURE FOOTAGE IMMEDIATELY.

Do not assume we will recreate the moment later.

Save:

/content/raw/YYYY-MM-DD_feature-name/

Include:

- before footage when relevant
- development footage
- failed attempts if entertaining
- final version
- screenshots
- cinematic angles
- bugs
- reactions/comparisons

---

# SHORTS FORMATS

Repeatedly experiment with formats such as:

"AI added THIS to my game."

"I told AI to make Catmurai angry."

"Can AI make an anime boss fight?"

"My AI created this attack."

"AI broke my game..."

"I gave two AIs the same game."

"Day X of building a game with AI."

"She found the legendary salami."

"Catmurai vs Captain 404."

"Normal Catmurai → SHONEN CATMURAI."

Strong visual payoff should appear extremely early.

Avoid lengthy introductions.

---

# LONG-FORM SERIES

Potential core series:

## Building a Game With AI

Episode examples:

1. I Asked AI to Build My Game
2. Giving Catmurai Her First Combat System
3. I Let Two AIs Work on the Same Game
4. AI Created Our First Boss
5. Can AI Make Anime Combat Actually Look Good?
6. We Added Captain 404 to the Game
7. AI Completely Broke Our Character
8. Turning Our Prototype Into a Real Game
9. Building an Entire Level With AI
10. The First Playable Version Is Finally Here

Videos should tell a STORY rather than merely show code.

Structure:

HOOK
→ objective
→ attempt
→ problem
→ improvement
→ setback
→ final result
→ tease next milestone

---

# AI-GENERATED CINEMATICS

Use LTX strategically.

Do not try to replace all gameplay footage with generated video.

Use it for:

- introductions
- transformations
- character teasers
- dream sequences
- ability showcases
- trailers
- transitions
- promotional Shorts

Whenever identity consistency matters, start from strong character reference images.

Prefer short controlled shots rather than excessively long generations.

---

# WEEKLY PARALLEL WORKFLOW

Example:

### Worker A / Codex

Build the week's headline gameplay feature.

Example:
Catmurai ultimate attack.

### Worker B / Claude

Simultaneously:

build supporting enemy mechanics,
review combat architecture,
prepare automated tests,
outline the video story.

Then:

Claude reviews Codex implementation.

Codex addresses important review findings.

Both move immediately to the next independent tasks.

---

# DAILY OPERATING PRINCIPLE

At any moment there should ideally be:

ONE GAME FEATURE being built

AND

ONE INDEPENDENT SUPPORT/CONTENT/TECHNICAL FEATURE being built.

Do not leave one paid worker idle because the other worker owns the project.

---

# FIRST 30-DAY SPRINT

## WEEK 1 — FOUNDATION + PUBLISH

Do NOT spend Week 1 planning.

Stabilize current Catmurai game.

Identify existing strong features.

Fix blockers.

Create one spectacular visual/gameplay improvement.

Publish initial Shorts.

Prepare first long-form development video.

---

## WEEK 2 — COMBAT SHOWCASE

Improve combat feel.

Create 3 abilities.

Create first spectacular ultimate.

Produce:

multiple Shorts
one development episode
cinematic ability teaser

---

## WEEK 3 — CHARACTER / STORY

Integrate Captain 404 meaningfully.

Create memorable interaction.

Add mini-boss/boss.

Create Captain vs Catmurai or cooperative narrative potential.

Generate several highly visual clips.

---

## WEEK 4 — VERTICAL SLICE

Connect systems into a coherent playable experience.

Polish the best 10–20 minutes.

Release:

vertical-slice showcase
major development video
trailer/teaser
several Shorts

Then evaluate analytics before planning Month 2.

---

# METRICS

Track every upload.

For Shorts:

views
viewed vs swiped away
average percentage viewed
rewatches
likes
comments
subscribers gained

For long-form:

impressions
CTR
average view duration
average percentage viewed
watch hours
subscriber conversion
retention graph

Record observations in:

/docs/YOUTUBE_METRICS.md

Do NOT optimize based on feelings when analytics exist.

---

# EXPERIMENTATION RULE

One content variable should change at a time whenever practical.

Examples:

hook
video length
title
thumbnail
style
character
format

When something performs unusually well:

DO MORE OF IT QUICKLY.

Do not wait several weeks.

Turn winning content into a mini-series.

---

# SUBSCRIPTION ROI CHECKPOINTS

The subscriptions are not permanent entitlements.

They must earn their continuation.

At approximately:

DAY 14
DAY 30
DAY 60

evaluate:

- development velocity
- amount of publishable content created
- views
- subscribers
- watch hours
- CTR/retention trends
- whether either AI subscription is substantially underused

If a worker isn't creating measurable value, change its responsibilities or cancel that subscription.

---

# CRITICAL RULE

Shipping beats polishing.

A working 85% feature that produces a great video is usually more valuable at this stage than spending another two days making it 95%.

Do not sacrifice technical integrity, but avoid perfectionism.

---

# IMMEDIATE FIRST ACTIONS

Both workers should begin by inspecting the current project rather than rebuilding it.

Worker A:

Perform a technical/gameplay audit and begin fixing the highest-value problem blocking the Catmurai vertical slice.

Worker B:

Perform an independent architecture/game-design audit and identify the three highest-impact improvements, then begin work on the highest-value task that DOES NOT conflict with Worker A.

Create/update the shared documentation.

Within the first development cycle, produce something visually demonstrable enough to become YouTube footage.

The project has now moved from experimentation into PRODUCTION.