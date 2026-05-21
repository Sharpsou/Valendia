using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
        private void ScatterAuthoredTreePrefabs()
        {
            if (!generateAuthoredTreePrefabs || authoredTreePrefabCount <= 0 || authoredTreePrefabs == null || authoredTreePrefabs.Length == 0)
            {
                return;
            }

            Transform parent = treeHlodCells == null ? CreateContainer("Authored Blender Trees") : null;
            System.Random random = new System.Random(seed + 1917);
            int placed = 0;
            int attempts = authoredTreePrefabCount * 10;

            for (int i = 0; i < attempts && placed < authoredTreePrefabCount; i++)
            {
                Vector3 point = RandomPoint(random);
                float slope = SlopeAt(point.x, point.z);
                float fertility = Mathf.PerlinNoise((point.x + seed * 0.83f) * 0.011f, (point.z - seed * 0.71f) * 0.011f);
                if (slope > maxTreeSlope || fertility < 0.36f)
                {
                    continue;
                }

                int prefabIndex = random.Next(authoredTreePrefabs.Length);
                GameObject prefab = authoredTreePrefabs[prefabIndex];
                if (prefab == null)
                {
                    continue;
                }

                point.y = HeightAt(point.x, point.z);
                float scale = Mathf.Lerp(1.37f, 2.16f, (float)random.NextDouble());
                float yaw = (float)random.NextDouble() * 360f;
                if (treeHlodCells != null)
                {
                    CreateAuthoredTreeCollider(TreeParentForPoint(point), prefab.name, point, yaw, scale);
                    RegisterTreeHlodInstance(prefab, MidPrefabForIndex(prefabIndex, prefab), HlodPrefabForIndex(prefabIndex, prefab), point, yaw, scale);
                }
                else
                {
                    InstantiateAuthoredTree(prefab, parent, point, yaw, scale);
                }

                placed++;
            }

            if (Debug.isDebugBuild)
            {
                Debug.Log($"Valendia authored trees placed: {placed}/{authoredTreePrefabCount} from {authoredTreePrefabs.Length} prefab variants.");
            }
        }

        private void ScatterPerimeterForest()
        {
            if (!generatePerimeterForest || perimeterForestTreeCount <= 0 || authoredTreePrefabs == null || authoredTreePrefabs.Length == 0)
            {
                return;
            }

            Transform parent = treeHlodCells == null ? CreateContainer("Perimeter Forest Ring") : null;
            System.Random random = new System.Random(seed + 2419);
            float halfWorld = WorldSize * 0.5f;
            float minInset = WorldSize * Mathf.Min(perimeterForestMinWidthRatio, perimeterForestMaxWidthRatio);
            float maxInset = WorldSize * Mathf.Max(perimeterForestMinWidthRatio, perimeterForestMaxWidthRatio);
            int placed = 0;
            int attempts = perimeterForestTreeCount * 16;

            for (int attempt = 0; attempt < attempts && placed < perimeterForestTreeCount; attempt++)
            {
                Vector3 point = PerimeterForestPoint(random, halfWorld, minInset, maxInset);
                float slope = SlopeAt(point.x, point.z);
                float forestNoise = Mathf.PerlinNoise((point.x + seed * 0.53f) * 0.018f, (point.z - seed * 0.47f) * 0.018f);
                if (slope > maxTreeSlope + 8f || forestNoise < 0.08f)
                {
                    continue;
                }

                int prefabIndex = random.Next(authoredTreePrefabs.Length);
                GameObject prefab = authoredTreePrefabs[prefabIndex];
                if (prefab == null)
                {
                    continue;
                }

                point.y = HeightAt(point.x, point.z);
                float edgeDistance = Mathf.Min(halfWorld - Mathf.Abs(point.x), halfWorld - Mathf.Abs(point.z));
                float depth = Mathf.InverseLerp(maxInset, minInset, edgeDistance);
                float scale = Mathf.Lerp(1.46f, 2.34f, (float)random.NextDouble()) * Mathf.Lerp(0.92f, 1.12f, depth);
                float yaw = (float)random.NextDouble() * 360f;
                if (treeHlodCells != null)
                {
                    CreateAuthoredTreeCollider(TreeParentForPoint(point), prefab.name, point, yaw, scale);
                    RegisterTreeHlodInstance(prefab, MidPrefabForIndex(prefabIndex, prefab), HlodPrefabForIndex(prefabIndex, prefab), point, yaw, scale);
                }
                else
                {
                    InstantiateAuthoredTree(prefab, parent, point, yaw, scale);
                }

                placed++;
            }

            if (Debug.isDebugBuild)
            {
                Debug.Log($"Valendia perimeter forest placed: {placed}/{perimeterForestTreeCount} in {minInset:0.#}-{maxInset:0.#} world-unit border band.");
            }
        }

        private Vector3 PerimeterForestPoint(System.Random random, float halfWorld, float minInset, float maxInset)
        {
            int side = random.Next(4);
            float along = Mathf.Lerp(-halfWorld, halfWorld, (float)random.NextDouble());
            float inset = Mathf.Lerp(minInset, maxInset, Mathf.Sqrt((float)random.NextDouble()));
            float sideJitter = Mathf.Lerp(-chunkSize * 0.018f, chunkSize * 0.018f, (float)random.NextDouble());

            switch (side)
            {
                case 0:
                    return new Vector3(Mathf.Clamp(along + sideJitter, -halfWorld, halfWorld), 0f, -halfWorld + inset);
                case 1:
                    return new Vector3(Mathf.Clamp(along + sideJitter, -halfWorld, halfWorld), 0f, halfWorld - inset);
                case 2:
                    return new Vector3(-halfWorld + inset, 0f, Mathf.Clamp(along + sideJitter, -halfWorld, halfWorld));
                default:
                    return new Vector3(halfWorld - inset, 0f, Mathf.Clamp(along + sideJitter, -halfWorld, halfWorld));
            }
        }

        private static GameObject InstantiateAuthoredTree(GameObject prefab, Transform parent, Vector3 point, float yawDegrees, float scale)
        {
            GameObject tree = Instantiate(prefab, point, Quaternion.Euler(0f, yawDegrees, 0f), parent);
            tree.name = prefab.name;
            tree.transform.localScale = Vector3.one * scale;
            tree.isStatic = true;
            ConfigureAuthoredTreeInstance(tree);
            return tree;
        }

        private GameObject HlodPrefabForIndex(int index, GameObject fallback)
        {
            if (authoredTreeHlodPrefabs != null
                && index >= 0
                && index < authoredTreeHlodPrefabs.Length
                && authoredTreeHlodPrefabs[index] != null)
            {
                return authoredTreeHlodPrefabs[index];
            }

            return fallback;
        }

        private GameObject MidPrefabForIndex(int index, GameObject fallback)
        {
            if (authoredTreeMidPrefabs != null
                && index >= 0
                && index < authoredTreeMidPrefabs.Length
                && authoredTreeMidPrefabs[index] != null)
            {
                return authoredTreeMidPrefabs[index];
            }

            return fallback;
        }

        private static void CreateAuthoredTreeCollider(Transform parent, string treeName, Vector3 point, float yawDegrees, float scale)
        {
            GameObject tree = new GameObject($"{treeName} Collider");
            tree.transform.SetParent(parent, false);
            tree.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yawDegrees, 0f));
            tree.transform.localScale = Vector3.one * scale;
            tree.isStatic = true;

            CapsuleCollider collider = tree.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.15f, 0f);
            collider.radius = 0.32f;
            collider.height = 2.3f;
        }

        private static void ConfigureAuthoredTreeInstance(GameObject tree)
        {
            foreach (Transform child in tree.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.isStatic = true;
            }

            foreach (MeshRenderer renderer in tree.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            CapsuleCollider collider = tree.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = tree.AddComponent<CapsuleCollider>();
            }

            collider.center = new Vector3(0f, 1.15f, 0f);
            collider.radius = 0.32f;
            collider.height = 2.3f;
        }

        private void ScatterMeadowPatches()
        {
            Transform parent = CreateContainer("Organic Meadow Stroke Seeds");
            System.Random random = new System.Random(seed + 909);
            int targetCount = EffectiveMeadowPatchCount;
            int placed = 0;
            int attempts = targetCount * 5;

            for (int i = 0; i < attempts && placed < targetCount; i++)
            {
                Vector3 point = RandomPoint(random);
                if (SlopeAt(point.x, point.z) > 20f)
                {
                    continue;
                }

                Biome biome = BiomeAt(point.x, point.z);
                if (biome == Biome.MountainScrub)
                {
                    continue;
                }

                CreateMeadowPatch(parent, point, biome, random);
                placed++;
            }
        }

        private void ScatterGrass()
        {
            Transform parent = CreateContainer("Grass Tuft Batches");
            System.Random random = new System.Random(seed + 303);
            List<Vector3>[] batchVertices = new List<Vector3>[GrassPaletteVariants];
            List<int>[] batchTriangles = new List<int>[GrassPaletteVariants];
            int[] batchIndexes = new int[GrassPaletteVariants];

            int targetCount = EffectiveGrassTuftCount;
            for (int i = 0; i < targetCount; i++)
            {
                Vector3 point = RandomPoint(random);
                float fertility = Mathf.PerlinNoise((point.x - seed) * 0.028f, (point.z + seed) * 0.028f);
                if (fertility < 0.002f || SlopeAt(point.x, point.z) > 30f)
                {
                    continue;
                }

                point.y = HeightAt(point.x, point.z);
                int variant = GrassPaletteVariantAt(point.x, point.z, random);
                batchVertices[variant] ??= new List<Vector3>(MaxGrassBatchVertices);
                batchTriangles[variant] ??= new List<int>(MaxGrassBatchVertices);
                AddGrassTuftToBatch(
                    parent,
                    batchVertices[variant],
                    batchTriangles[variant],
                    ref batchIndexes[variant],
                    GrassBatchPrefix(variant),
                    GrassMaterialForVariant(variant),
                    point,
                    random);
            }

            for (int variant = 0; variant < GrassPaletteVariants; variant++)
            {
                if (batchVertices[variant] == null)
                {
                    continue;
                }

                FlushGrassBatch(
                    parent,
                    batchVertices[variant],
                    batchTriangles[variant],
                    ref batchIndexes[variant],
                    GrassBatchPrefix(variant),
                    GrassMaterialForVariant(variant));
            }
        }

        private void ScatterFlowerPatches()
        {
            Transform parent = CreateContainer("Flower Patches");
            System.Random random = new System.Random(seed + 404);
            int targetCount = EffectiveFlowerPatchCount;
            int placed = 0;
            int attempts = targetCount * 7;

            for (int i = 0; i < attempts && placed < targetCount; i++)
            {
                Vector3 point = RandomPoint(random);
                Biome biome = BiomeAt(point.x, point.z);
                if (biome != Biome.LavenderField && biome != Biome.GoldenGrass)
                {
                    continue;
                }

                float slope = SlopeAt(point.x, point.z);
                if (slope > 16f)
                {
                    continue;
                }

                point.y = HeightAt(point.x, point.z);
                CreateFlowerPatch(parent, point, random);
                placed++;
            }
        }

        private void ScatterForegroundMeadowDetail()
        {
            Transform parent = CreateContainer("Foreground Meadow Detail");
            System.Random random = new System.Random(seed + 606);
            List<Vector3>[] grassVertices = new List<Vector3>[GrassPaletteVariants];
            List<int>[] grassTriangles = new List<int>[GrassPaletteVariants];
            int[] grassBatchIndexes = new int[GrassPaletteVariants];

            for (int i = 0; i < foregroundMeadowDetailCount; i++)
            {
                float t = Mathf.Sqrt((i + 0.5f) / Mathf.Max(1f, foregroundMeadowDetailCount));
                float angle = i * 2.39996323f + Mathf.Lerp(-0.42f, 0.42f, (float)random.NextDouble());
                float x = Mathf.Cos(angle) * t * WorldSize * Mathf.Lerp(0.18f, 0.48f, (float)random.NextDouble());
                float z = Mathf.Sin(angle) * t * WorldSize * Mathf.Lerp(0.14f, 0.42f, (float)random.NextDouble()) + WorldSize * Mathf.Lerp(-0.08f, 0.16f, (float)random.NextDouble());
                x = Mathf.Clamp(x + Mathf.Lerp(-18f, 18f, (float)random.NextDouble()), WorldMin.x + 6f, -WorldMin.x - 6f);
                z = Mathf.Clamp(z + Mathf.Lerp(-18f, 18f, (float)random.NextDouble()), WorldMin.y + 6f, -WorldMin.y - 6f);

                if (SlopeAt(x, z) > 18f)
                {
                    continue;
                }

                Vector3 point = new Vector3(x, HeightAt(x, z), z);
                Biome biome = BiomeAt(x, z);
                CreateMeadowPatch(parent, point, biome, random, Mathf.Lerp(1.1f, 2.0f, (float)random.NextDouble()));

                int extraGrass = random.Next(7, 16);
                for (int grass = 0; grass < extraGrass; grass++)
                {
                    Vector3 grassPoint = point + new Vector3(Mathf.Lerp(-2.2f, 2.2f, (float)random.NextDouble()), 0f, Mathf.Lerp(-2.2f, 2.2f, (float)random.NextDouble()));
                    grassPoint.y = HeightAt(grassPoint.x, grassPoint.z);
                    int variant = GrassPaletteVariantAt(grassPoint.x, grassPoint.z, random);
                    grassVertices[variant] ??= new List<Vector3>(MaxGrassBatchVertices);
                    grassTriangles[variant] ??= new List<int>(MaxGrassBatchVertices);
                    AddGrassTuftToBatch(
                        parent,
                        grassVertices[variant],
                        grassTriangles[variant],
                        ref grassBatchIndexes[variant],
                        $"Foreground {GrassBatchPrefix(variant)}",
                        GrassMaterialForVariant(variant),
                        grassPoint,
                        random);
                }

                if (random.NextDouble() < 0.42)
                {
                    Vector3 flowerPoint = point + new Vector3(Mathf.Lerp(-2.2f, 2.2f, (float)random.NextDouble()), 0f, Mathf.Lerp(-2.2f, 2.2f, (float)random.NextDouble()));
                    flowerPoint.y = HeightAt(flowerPoint.x, flowerPoint.z);
                    CreateFlowerPatch(parent, flowerPoint, random);
                }
            }

            for (int variant = 0; variant < GrassPaletteVariants; variant++)
            {
                if (grassVertices[variant] == null)
                {
                    continue;
                }

                FlushGrassBatch(
                    parent,
                    grassVertices[variant],
                    grassTriangles[variant],
                    ref grassBatchIndexes[variant],
                    $"Foreground {GrassBatchPrefix(variant)}",
                    GrassMaterialForVariant(variant));
            }
        }

        private void ScatterMountainScrub()
        {
            Transform parent = CreateContainer("Mountain Scrub");
            System.Random random = new System.Random(seed + 505);
            int placed = 0;
            int attempts = scrubCount * 6;

            for (int i = 0; i < attempts && placed < scrubCount; i++)
            {
                Vector3 point = RandomPoint(random);
                if (BiomeAt(point.x, point.z) != Biome.MountainScrub)
                {
                    continue;
                }

                point.y = HeightAt(point.x, point.z);
                CreateScrub(parent, point, random);
                placed++;
            }
        }

        private void ScatterBorderVegetation()
        {
            Transform parent = CreateContainer("Border Vegetation Fill");
            System.Random random = new System.Random(seed + 515);
            List<Vector3>[] grassVertices = new List<Vector3>[GrassPaletteVariants];
            List<int>[] grassTriangles = new List<int>[GrassPaletteVariants];
            int[] grassBatchIndexes = new int[GrassPaletteVariants];
            int clustersPerSide = Mathf.Max(8, Mathf.CeilToInt(borderVegetationClusterCount / 4f));
            float halfWorld = WorldSize * 0.5f;
            float sideSpacing = WorldSize / clustersPerSide;

            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < clustersPerSide; i++)
                {
                    float along = -halfWorld + sideSpacing * (i + 0.5f) + Mathf.Lerp(-sideSpacing * 0.34f, sideSpacing * 0.34f, (float)random.NextDouble());
                    float inset = Mathf.Lerp(chunkSize * 0.05f, chunkSize * 0.46f, (float)random.NextDouble());
                    Vector3 center = BorderPoint(side, along, inset);
                    center.y = HeightAt(center.x, center.z);

                    int rocks = random.Next(4, 9);
                    for (int rock = 0; rock < rocks; rock++)
                    {
                        Vector3 point = center + BorderScatterOffset(side, random, sideSpacing * 0.48f, chunkSize * 0.22f);
                        point.y = HeightAt(point.x, point.z);
                        if (SlopeAt(point.x, point.z) <= 32f)
                        {
                            CreateRock(parent, point, random);
                        }
                    }

                    int scrub = random.Next(7, 14);
                    for (int s = 0; s < scrub; s++)
                    {
                        Vector3 point = center + BorderScatterOffset(side, random, sideSpacing * 0.52f, chunkSize * 0.24f);
                        point.y = HeightAt(point.x, point.z);
                        if (SlopeAt(point.x, point.z) <= 34f)
                        {
                            CreateScrub(parent, point, random);
                        }
                    }

                    int grassTufts = random.Next(44, 84);
                    for (int grass = 0; grass < grassTufts; grass++)
                    {
                        Vector3 point = center + BorderScatterOffset(side, random, sideSpacing * 0.58f, chunkSize * 0.30f);
                        if (SlopeAt(point.x, point.z) > 34f)
                        {
                            continue;
                        }

                        point.y = HeightAt(point.x, point.z);
                        int variant = GrassPaletteVariantAt(point.x, point.z, random);
                        grassVertices[variant] ??= new List<Vector3>(MaxGrassBatchVertices);
                        grassTriangles[variant] ??= new List<int>(MaxGrassBatchVertices);
                        AddGrassTuftToBatch(
                            parent,
                            grassVertices[variant],
                            grassTriangles[variant],
                            ref grassBatchIndexes[variant],
                            $"Border {GrassBatchPrefix(variant)}",
                            GrassMaterialForVariant(variant),
                            point,
                            random);
                    }

                    if (random.NextDouble() < 0.86)
                    {
                        Vector3 meadowPoint = center + BorderScatterOffset(side, random, sideSpacing * 0.32f, chunkSize * 0.18f);
                        meadowPoint.y = HeightAt(meadowPoint.x, meadowPoint.z);
                        CreateMeadowPatch(parent, meadowPoint, Biome.MountainScrub, random, Mathf.Lerp(0.85f, 1.35f, (float)random.NextDouble()));
                    }
                }
            }

            for (int variant = 0; variant < GrassPaletteVariants; variant++)
            {
                if (grassVertices[variant] == null)
                {
                    continue;
                }

                FlushGrassBatch(
                    parent,
                    grassVertices[variant],
                    grassTriangles[variant],
                    ref grassBatchIndexes[variant],
                    $"Border {GrassBatchPrefix(variant)}",
                    GrassMaterialForVariant(variant));
            }
        }

        private Vector3 BorderPoint(int side, float along, float inset)
        {
            float halfWorld = WorldSize * 0.5f;
            switch (side)
            {
                case 0:
                    return new Vector3(along, 0f, -halfWorld + inset);
                case 1:
                    return new Vector3(along, 0f, halfWorld - inset);
                case 2:
                    return new Vector3(-halfWorld + inset, 0f, along);
                default:
                    return new Vector3(halfWorld - inset, 0f, along);
            }
        }

        private static Vector3 BorderScatterOffset(int side, System.Random random, float alongRadius, float depthRadius)
        {
            float along = Mathf.Lerp(-alongRadius, alongRadius, (float)random.NextDouble());
            float depth = Mathf.Lerp(-depthRadius, depthRadius, (float)random.NextDouble());
            return side < 2
                ? new Vector3(along, 0f, depth)
                : new Vector3(depth, 0f, along);
        }

        private void ScatterRocks()
        {
            Transform parent = CreateContainer("Rocks");
            System.Random random = new System.Random(seed + 202);

            for (int i = 0; i < rockCount; i++)
            {
                Vector3 point = RandomPoint(random);
                point.y = HeightAt(point.x, point.z);
                CreateRock(parent, point, random);
            }
        }
    }
}
