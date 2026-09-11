# Animation asset provenance

Generated using the built-in image generation tool. No external CLI or API key was used.

## Gameplay assets
- assets/catmurai-motion.png: 6-column / 4-row pose atlas, referenced from the supplied Catmurai character. Runtime chroma-keying removes the green background. Source output: exec-05729329-ad34-447e-8752-9c725d906703.png.
- assets/catmurai-walk.png: 4-column / 2-row eight-pose walk atlas. Source output: exec-abbd1006-e08b-4daf-9ecc-dec39ba28a82.png.
- assets/animation-atlas.png and assets/animation-atlas-v2.png are rejected checkerboard-background drafts and are not loaded by the game.

## Prompt set
1. Generate a 6x4 transparent animation atlas preserving orange tabby Catmurai identity, red headband and cape, black/gold outfit, katana; rows walk, sword attack, casting, turning; equal cells, consistent feet and scale, no text.
2. Remove baked checkerboard and keep effects in their cells. The returned transparency was still baked, so this draft was rejected.
3. Replace only the checkerboard with pure #00FF00 green for runtime chroma-keying, preserve poses and geometry, no textured background.
4. Generate an eight-pose walk atlas in a 4x2 grid using the motion atlas as identity reference. Explicitly alternate left/right foot contacts and passing poses, counter-swing arms, moving cape and tail, preserve outfit and katana, use pure green background and padding.

## Frame corrections
Runtime component isolation removes detached neighboring-cell fragments. The extended sword frame has a wider source region to preserve its tip. Per-frame foot anchors correct registration. Trails begin at the attack's impact time. The later eight-pose walk replaces the first atlas's weak gait.

## Living Vale assets and prompts
Generated with the built-in image-generation tool.

- assets/vale-npcs.png (exec-a15c6ac8-6936-478b-92e7-b65b78697fc4.png): 6x4 pure-green atlas, six consistent poses per row for cream-white elder cat monk with staff, silver/red kitsune, indigo cat ronin, crimson feline Oni. Requested idle, alternating movement, anticipation, strike and recovery poses; full silhouettes with padding; hand-drawn Japanese fantasy style.
- assets/vale-props.png (exec-705b7568-5c1a-4273-aaf7-0531d006d7d3.png): 3x2 atlas of sakura, moonlit shrine, torii, pine, moss/flower rock cluster, stone lantern. Requested elevated RPG perspective, intricate painterly textures, teal shadows, warm amber light, isolated props and pure green background.
- assets/vale-props-draft.png was the first generation (exec-46e94100-6f1a-4ae7-a97e-c395d5949fc2.png). Its gradient background was rejected. The edit prompt explicitly replaced all background and interior holes with uniform #00FF00 while preserving prop art and arrangement. The corrected image is the one loaded by the game.

Weapon classes (built-in image generation): assets/weapon-classes.png, 1448 x 1086. Prompt: exact Catmurai identity from catmurai-walk.png; eight columns / three rows for battle axe, dual swords, broad two-handed greatsword. Each row: ready, left/right walk, wind-up, impact, follow-through/second slash, recovery, magic channel. Flat green chroma backdrop, consistent ground, no labels or effects. Runtime polygon extraction and largest-component isolation remove neighboring figures; weapon edge coordinates attach existing enchant VFX. Original generated image retained. Review: weapon-class-review.html.

Weapon animation repair v2: assets/weapon-classes-v2.png (1024 x 1536), built-in image generation. Prompt: repair layout to four columns and six rows; one small full-body Catmurai in each cell with green gutters; axe ready/walk/windup/chop/followthrough/recovery/channel, dual alternating strikes, greatsword heavy windup/contact/recovery. Preserve identity and complete weapon/cape silhouettes. Replaced destructive polygon masks with gutter-based crops and connected-component isolation. All 24 extracted frames pass a runtime alpha-edge check; foot anchors and glow edges re-registered. Dual follow-through mirrored to retain target direction. Static QA: weapon-class-review.html?frames. Prior atlas retained for provenance.

Grip repair v3 (built-in image generation): assets/weapon-classes-v3.png. Prompt: precision edit of v2; preserve 1024x1536 four-column/six-row layout; both axe paws wrapped around same wooden haft in every pose including casting; exactly two dual swords held by their hilts; redraw second dual strike facing right with cape/tail left rather than mirroring torso; preserve remaining poses. Re-registered expanded crop gutters and second-strike glow. All 24 frames pass alpha-edge validation. Removed whole-body dual frame flip. Shared pose schedules now drive playback and manual stepping; walk uses only walking poses and casting includes ready/recovery. Original versions retained.

## Shadow Ascension
Built-in image generation, reference-guided edit mode. User references preserved as
assets/shadow-reference.png and assets/shadow-aura.png. Runtime atlas: assets/shadow-poses.png.
Initial prompt requested exact shadow Catmurai identity, violet eyes and rune, red scarf,
gold trim, purple katana, eight full-body right-facing poses in a green 4x2 atlas:
idle, two walking steps, windup, slash, follow-through, recovery, casting.
Repair prompt: keep exact eight poses; scale to 70 percent within equal cells, at least
30px green gutters including sword tips; consistent feet baseline; no grid or text.
Final built-in output: exec-5c4f414b-cd9d-4f57-9ad9-958051434a53.png.
Runtime chromakey and measured crop anchors preserve animation feet alignment.
The first lower-row crop extends to x420 to contain the entire slash. Alpha boundary
validation runs when the atlas loads. Review: shadow-review.html (save disabled).

Visual polish pass: 350ms shadow entrance blend, stride-synchronized body sway,
attack lunge at contact, impact-timed violet claw arcs, depth-sorted orbiting spirits,
breathing aura opacity, and particles with smooth fade envelopes. Gameplay regression
suite passed; transformation and all eight poses inspected in the local browser.

Weapon polish pass: continuous class-weighted anticipation, contact lunge and recovery;
stride body sway; blade-local colored trails (amber axe, ice dual, lilac greatsword).
Hand and weapon use the same transform. Dual trails follow both contact timings.
Validated animation review rendering and numerical continuity through attack boundaries.

Weapon walking repair: replaced two-pose combat-atlas walking with a separate 24-frame
travel atlas (eight poses each for axe, dual, greatsword). Removed procedural body sway
from these walk cycles. Pose selection follows accumulated travel distance (0.8 cycle).
Built-in imagegen reference edit: weapon-classes-v3.png; requested strict 4x6 green atlas,
eight contact/down/passing/up poses per class, fixed grips and consistent feet baseline.
Output exec-58e6b26f-8fae-4cde-a170-576f820eb302.png copied to assets/weapon-walk.png.
Measured row foot baselines and expanded crop gutters accommodate complete weapons.

Idle and movement refinement: 140ms idle/walk-only pose blends, subdued foot-anchored
breathing, per-enemy breathing phases, velocity-filtered movement lean, exponential
camera settling. Skill, attack, hurt and death states bypass locomotion blending.
30/60/120 FPS settling equivalence and skill bypass tests pass; local movement inspected.
This remains sprite animation, not a skeletal gait with independently planted feet.

Terrain river correction: river ribbon and land clipping share a shoreline boundary.
Road and ground scatter are clipped out of water. Trees include trunk/canopy clearance;
random rocks and lanterns use shoreline margins. Moved the fixed bank rock out of the
channel. Road bends at x=-1400 onto a short wooden bridge with planks, rails and posts.
Extended water geometry beyond map bounds, corrected perspective width, and moved
shore stones onto the bank. terrain-review.html provides three save-disabled viewpoints.
