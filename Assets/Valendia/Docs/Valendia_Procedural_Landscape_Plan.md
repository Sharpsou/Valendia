# Valendia Procedural Landscape Plan

## Visual Target

- Large explorable valley with pale limestone mountains and distant ridges.
- Medium-poly readability: faceted terrain silhouettes, simplified tree crowns, clear material blocks.
- Dense grass and vegetation retained from `img_DA`, with a deliberately heavy prototype pass around the path so the first-person view reads as a real meadow rather than isolated tufts.
- A winding path acts as the first navigation spine for a first-person prototype.
- Biome patches create the first Valendia identity layer: autumn groves, golden grass, lavender fields, and mountain scrub.
- Sky, cloud shapes, warm low sun, distant limestone spires, green meadow masses, olive/golden grass accents, and sparse violet floral strokes support the same stylized fantasy-valley mood as the references.

## Generation Strategy

1. Generate deterministically from a seed.
2. Build terrain in square mesh chunks so the scene can scale without one giant mesh.
3. Use layered Perlin noise for rolling hills and a radial ridge mask for distant mountains.
4. Add a bounded micro-relief pass for ground texture, masked near the path and softened near the horizon ridges.
5. Flatten and clear a sinusoidal path through the valley.
6. Assign visible terrain materials from deterministic biome masks and procedural detail/normal textures.
7. Scatter faceted rocks, stylized trees, dense grass clumps, lavender flowers, and scrub from seeded random streams.
8. Use double-sided foliage materials, outward-oriented blob normals, and flatter leaf cushion meshes so trees stay solid when viewed from underneath.
9. Add procedural sky ambience, low-poly clouds, and far limestone spires.
10. Once the composition feels right, keep the seed and scene object, or convert generated children to static scene content.

## First Prototype Setup

Fast path:

1. In Unity, use `Valendia > Create Prototype Scene`.
2. Open `Assets/Valendia/ValendiaPrototype.unity`.
3. Press Play to walk the generated valley.

Generated scene note:

- `Assets/Valendia/ValendiaPrototype.unity` is intentionally ignored by Git because the generated validation scene is very large.
- Regenerate it from the Unity menu after cloning or cleaning the workspace.
- `Assets/Valendia/Docs/ValendiaPrototypePreview.png` is also a local generated artifact.

Manual path:

1. In Unity, create an empty GameObject named `Valendia World`.
2. Add `ValendiaLandscapeGenerator`.
3. Use the component context menu: `Generate Valendia Landscape`.
4. Create a player capsule with `CharacterController`.
5. Add `ValendiaFirstPersonController`.
6. Assign the main camera as `cameraRoot`, parented under the player.
7. The project now declares `com.unity.inputsystem` and uses Unity's `Both` input handling mode; keyboard/mouse and legacy Input Manager fallbacks remain enabled.

## Recommended Defaults

- `chunksPerAxis`: 4 for prototype, 6-8 after performance profiling.
- `chunkSize`: 180-220.
- `verticesPerChunk`: 48 for medium-poly terrain, 64 if silhouettes need more detail.
- `terrainMicroReliefStrength`: 0.12 for a safe ground-detail pass; keep below 0.25 unless the terrain is visually rechecked.
- `groundTextureTiling`: 42 so the generated 128x128 ground detail repeats at meadow scale rather than across the full valley.
- `treeCount`: 920 in the current DA validation scene, 350-650 depending on target hardware before forest batching.
- `forestPocketCount`: 12 in the current DA validation scene to create additional forest masses away from the path.
- `grassTuftCount`: 90000 in the current DA validation scene with shorter tufts; use 4000-8000 for lighter editor iteration until instancing/batching is added.
- `heightScale`: 30 and `distantMountainStrength`: 0.12 in the current img2 recovery pass to keep the valley soft and avoid broken gullies.
- `flowerRibbonCount`: 32 in the current organic vegetation pass; violet/pink is an accent carried by grass/flowers, not a ground material.

## Next Technical Step

The current generator is intentionally editor-friendly and deterministic. The prototype scene builder now covers one-click scene setup, biomes, stylized trees, forest pockets, very dense short batched vegetation, removed ground-accent overlays, lower smooth ridge heights, taller solid double-sided canopies, clouds, and an iterated img2-first organic vegetation pass with a teal sky, warm low sun, stronger shadows, neutral green/olive terrain, and color accents carried by batched grass strokes instead of flat colored ground patches. The next pass should add:

- persistent asset baking for generated meshes/materials,
- GPU instancing or indirect rendering for vegetation if the batched meshes are still too heavy,
- quality tuning after a real Play Mode pass with the user camera, especially checking whether the new ground normal/detail texture adds enough matter without noisy shimmer,
- smoother biome transitions and authored points of interest along the path,
- occlusion/LOD groups before increasing scene size.

## Current Performance Pass

- Main grass tufts are merged into spatial mesh chunks instead of individual GameObjects.
- Path-edge grass is merged into dedicated batches.
- Organic meadow color accents are also rendered as batched grass strokes instead of flat ground overlays.
- Grass receives shadows but no longer casts per-blade shadows.
- Main light shadows use hard shadows, 180 distance, and 2 cascades for cheaper URP shadow rendering.

## Git Hygiene

- Version source scripts, package manifests, project settings, docs, and `img_DA` references.
- Ignore Unity caches, local agent tooling, logs, generated preview PNGs, and the generated prototype scene.
- Keep procedural generation deterministic so ignored scene artifacts remain reproducible from committed source.
