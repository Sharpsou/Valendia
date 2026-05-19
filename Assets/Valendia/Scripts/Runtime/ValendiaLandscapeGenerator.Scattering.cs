using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
        private void ScatterTrees()
        {
            Transform parent = CreateContainer("Trees");
            System.Random random = new System.Random(seed + 101);
            int placed = 0;
            int attempts = treeCount * 8;

            for (int i = 0; i < attempts && placed < treeCount; i++)
            {
                Vector3 point = RandomPoint(random);
                if (IsOnPath(point.x, point.z, pathWidth * 0.5f + pathVegetationClearance))
                {
                    continue;
                }

                float slope = SlopeAt(point.x, point.z);
                float fertility = Mathf.PerlinNoise((point.x + seed) * 0.012f, (point.z - seed) * 0.012f);
                if (slope > maxTreeSlope || fertility < 0.34f)
                {
                    continue;
                }

                point.y = HeightAt(point.x, point.z);
                CreateTree(parent, point, random, BiomeAt(point.x, point.z));
                placed++;
            }
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
                if (IsOnPath(point.x, point.z, pathWidth * 0.5f + 1.2f))
                {
                    continue;
                }

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
                if (IsOnPath(point.x, point.z, pathWidth * 0.5f + 1.5f))
                {
                    continue;
                }

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
                if (IsOnPath(point.x, point.z, pathWidth * 0.5f + 2.5f))
                {
                    continue;
                }

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

        private void ScatterPathEdgeVegetation()
        {
            Transform parent = CreateContainer("Path Edge Meadow Detail");
            System.Random random = new System.Random(seed + 606);
            List<Vector3>[] grassVertices = new List<Vector3>[GrassPaletteVariants];
            List<int>[] grassTriangles = new List<int>[GrassPaletteVariants];
            int[] grassBatchIndexes = new int[GrassPaletteVariants];

            for (int i = 0; i < pathEdgePatchCount; i++)
            {
                float t = Mathf.Lerp(0.08f, 0.92f, i / (float)Mathf.Max(1, pathEdgePatchCount - 1));
                t += Mathf.Lerp(-0.018f, 0.018f, (float)random.NextDouble());
                t = Mathf.Clamp01(t);

                float z = Mathf.Lerp(WorldMin.y, -WorldMin.y, t);
                float side = random.NextDouble() < 0.5 ? -1f : 1f;
                float distanceFromPath = pathWidth * 0.36f + Mathf.Lerp(1.2f, 6.8f, (float)random.NextDouble());
                float x = PathCenterX(z) + side * distanceFromPath;

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
                        $"Path Edge {GrassBatchPrefix(variant)}",
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
                    $"Path Edge {GrassBatchPrefix(variant)}",
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
                if (IsOnPath(point.x, point.z, pathWidth * 0.5f + pathVegetationClearance))
                {
                    continue;
                }

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

                    int trees = random.Next(3, 7);
                    for (int tree = 0; tree < trees; tree++)
                    {
                        Vector3 point = center + BorderScatterOffset(side, random, sideSpacing * 0.44f, chunkSize * 0.2f);
                        if (IsOnPath(point.x, point.z, pathWidth * 0.5f + pathVegetationClearance) || SlopeAt(point.x, point.z) > maxTreeSlope + 6f)
                        {
                            continue;
                        }

                        point.y = HeightAt(point.x, point.z);
                        Biome treeBiome = BorderTreeBiome(random);
                        float coniferChance = treeBiome == Biome.MountainScrub ? 0.38f : 0.12f;
                        GameObject borderTree = CreateTree(parent, point, random, treeBiome, coniferChance);
                        borderTree.transform.localScale *= Mathf.Lerp(0.78f, 1.05f, (float)random.NextDouble());
                    }

                    int grassTufts = random.Next(44, 84);
                    for (int grass = 0; grass < grassTufts; grass++)
                    {
                        Vector3 point = center + BorderScatterOffset(side, random, sideSpacing * 0.58f, chunkSize * 0.30f);
                        if (IsOnPath(point.x, point.z, pathWidth * 0.5f + 1.5f) || SlopeAt(point.x, point.z) > 34f)
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

        private static Biome BorderTreeBiome(System.Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.42)
            {
                return Biome.ValleyGrass;
            }

            if (roll < 0.68)
            {
                return Biome.AutumnGrove;
            }

            if (roll < 0.86)
            {
                return Biome.GoldenGrass;
            }

            return Biome.MountainScrub;
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
