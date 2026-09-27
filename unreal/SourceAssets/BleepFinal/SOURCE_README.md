# Bleep - final detail pass

## Final files

- **Bleep_Final.blend**: editable Blender scene with packed textures. LOD0 is visible; the sculpt/reference and other LODs are hidden.
- **Bleep_Final_LOD0.glb / .fbx**: 65,000 triangles.
- **Bleep_Final_LOD1.glb / .fbx**: 25,000 triangles.
- **Bleep_Final_LOD2.glb / .fbx**: 8,000 triangles.
- **textures/**: 4K base color and emission, 2K roughness, and per-LOD 2K normal maps in OpenGL and DirectX conventions.
- **final-front.png**, **final-back.png**, **final-three-quarter.png**, **final-face-detail.png**: Blender review renders of the final LOD0 material.
- **validation.json**: fresh reimport checks of all six exports.

## This detail pass

Added crisp, tapered luminous eye accents and a clean crescent inside the existing forehead motif. Adjusted roughness to keep the face matte while giving the blue flames a more defined highlight. Normal strength increased modestly from 0.25 to 0.30. The new details are baked into the texture atlas, so they remain fixed to the model and do not depend on a camera or a Blender-only shader. Emission uses a 4K map for the finer markings.

Geometry, UV layout, overall scale and triangle budgets are preserved from the last validated version. No separate floating detail meshes or extra material slots were added. The original source and previous versions remain intact.

## Unity material settings

Import FBX alongside the textures. BaseColor and Emission use sRGB. Roughness is linear; convert to smoothness with **1 - Roughness** and pack it as required by your render pipeline. Import the matching LOD's **OpenGL** normal map as a Normal Map, with strength **0.30**. Use metallic **0.04**, emission multiplier **1.4**. Set up a LOD Group, with the matching normal map material for each LOD.

## Unreal material settings

Import FBX as a static mesh with normals/tangents. At standard unit conversion the model is approximately 100 cm tall. Connect BaseColor, Roughness and Emission; use the matching **DirectX** normal map with Normalmap compression and sRGB disabled. Do not additionally flip green. Flatten the normal by **0.70** to match the Blender strength of 0.30. Use metallic **0.04**, emission multiplier **1.4**. Assign matching normal materials to the additional LODs.

FBX does not reliably transfer all material settings: connect the supplied maps explicitly. GLB embeds the PBR textures and emissive-strength setting. Bloom is optional and controlled by the engine; the preview does not rely on bloom to make the accents readable.

## Verification and scope

The asset is approximately one metre tall with a bottom-centre origin. Validation welds coincident export vertices before checking edges because UV seams split vertices on export. It checks finite coordinates, valid mesh data, UV coverage, positive signed volume, scale, embedded GLB textures, and zero boundary/nonmanifold edges or zero-area faces.

This remains a **static, unrigged asset**. It has no skinning, deformation retopology, animation, authored collision or animated flame effects. The final checks use Blender rendering and independent FBX/GLB reimports; actual Unity and Unreal project imports have not been tested. Fine source reconstruction irregularities remain in the underlying sculpt.
