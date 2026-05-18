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
- `treeCount`: 920 in the current DA validation scene; tree meshes are baked after generation while trunk colliders stay separate.
- `forestPocketCount`: 12 in the current DA validation scene to create additional forest masses away from the path.
- `grassTuftCount`: 360000 in the current DA validation scene, split into material and spatial batches so the path-edge density now reads across the whole map.
- `heightScale`: 30, `distantMountainStrength`: 0.12, `borderMountainWallStrength`: 0.18, and `distantSpireCount`: 64 keep the valley soft while closing the horizon with overlapping mountain accents and sealed corner massifs, without adding a continuous wall surface.
- `borderVegetationClusterCount`: 144 keeps the mountain-scrub border from thinning out by adding clustered rocks, scrub, low trees, meadow strokes, and grass along all four edges.
- `qualityProfile`: `PlayableOptimized` is the default runtime profile; `HighVisual` keeps the full authoring density for comparison.
- Ground rendering uses `GroundBiomeAt` so the mountain-scrub logic no longer paints a hard dark circular terrain band.
- Border trees use mixed valley/autumn/golden/scrub biomes with limited conifers, avoiding all-spruce edges.
- Clouds are larger volumetric low-poly masses built from more puffs and multi-row banks; their positions use a stratified sky grid so the center does not randomly empty out. Visible clouds stay warm/unlit, and hidden `Cloud Shadow Caster` meshes use a lit material with `ShadowsOnly` so they can project real sunlight shadows.
- Border mountains and generated rocks receive approximate `BoxCollider` components so the player cannot walk through them.
- Trees receive trunk-level `CapsuleCollider` components so the player cannot walk through trunks while foliage remains non-blocking.
- Existing generated scenes self-heal through `Ensure Generated Landscape Complete`, which adds missing mountain accents, fills sparse borders with scrub, rocks, low trees, and grass, then bakes eligible static renderers.
- `flowerRibbonCount`: 32 in the current organic vegetation pass; violet/pink is an accent carried by grass/flowers, not a ground material.

## Current Performance Pass

- Main grass tufts are merged into spatial mesh chunks instead of individual GameObjects.
- Path-edge grass is merged into dedicated batches.
- Organic meadow color accents are also rendered as batched grass strokes instead of flat ground overlays.
- Grass and meadow batches use Unity `LODGroup` components with dense, medium, and light meshes, so detail follows the player camera instead of the path.
- Decorative static meshes are baked by material and shadow mode after generation, reducing active renderers while preserving collision. Grass batches stay out of this bake so their `LODGroup` can work at runtime.
- Tree trunk, rock, terrain, and mountain colliders remain present in both quality profiles so free exploration works across the whole map.
- Source renderers are stripped after baking, and empty generated hierarchy branches are pruned.
- Grass receives shadows but no longer casts per-blade shadows.
- Main light shadows use hard shadows, 900 distance, and 4 cascades so overhead cloud shadow casters remain in range.

## Git Hygiene

- Version source scripts, package manifests, project settings, docs, and `img_DA` references.
- Ignore Unity caches, local agent tooling, logs, generated preview PNGs, and the generated prototype scene.
- Keep procedural generation deterministic so ignored scene artifacts remain reproducible from committed source.
