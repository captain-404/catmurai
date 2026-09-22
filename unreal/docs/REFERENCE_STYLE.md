# Catmurai interface style

Reference: https://chatgpt.com/s/m_6ab12241410c8191a6f29fc5351778c8

The Path of Shadows reference was visually inspected on 2026-09-21. Its
brush lettering, parchment highlights, crimson marks, violet energy and
near-black panels inform the HUD. The game retains its Moonfall title.

Approximate sRGB palette, selected visually:

| Role | Hex |
| --- | --- |
| Ink | #0F0914 |
| Panel | #1F1023 |
| Parchment | #F7D8A3 |
| Crimson / health | #CD264E |
| Violet / XP | #B14DED |
| Lilac / ability text | #DDAEF6 |
| Secondary text | #C3ADB1 |

Permanent Marker provides the brush-inspired headings, rather than claiming
to reproduce the generated reference lettering exactly. Small numeric labels
and instructions use Unreal's readable small font. The existing local font
and its Apache 2.0 license are bundled in Content/UI/Fonts and staged with
packaged builds. This is an interface theme, not a replacement of character
textures or environment art.

The HUD uses a centered 1280x720 design space scaled uniformly to the viewport.
Health/XP remain labeled so color is not the only way to distinguish them.

## Verification

- UE 5.8.2 Development Editor build succeeded (Saved/build-reference-style.log).
- Rendered gameplay HUD and upgrade cards inspected at 1280x720; brush
  headings, health/XP bars, cooldown text, and controls were readable.
- Selecting upgrade 1 resumed actual gameplay, with live health, XP and
  cooldown values updating.
- Runtime font loading confirmed in Saved/reference-style-preview.log.
  File-backed fonts use LazyLoad; Inline triggered an assertion during the
  initial test and was corrected before the successful build.
- Packaged distribution has not been tested.
