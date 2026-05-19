using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
        private void GenerateSmoothPathRibbon()
        {
            Transform parent = CreateContainer("Smooth Path Ribbon");
            int segments = Mathf.Max(24, chunksPerAxis * 42);
            float halfWidth = pathWidth * 0.42f;
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float z = Mathf.Lerp(WorldMin.y, -WorldMin.y, t);
                float x = PathCenterX(z);
                float aheadZ = Mathf.Lerp(WorldMin.y, -WorldMin.y, Mathf.Clamp01(t + 1f / segments));
                float behindZ = Mathf.Lerp(WorldMin.y, -WorldMin.y, Mathf.Clamp01(t - 1f / segments));
                Vector3 tangent = new Vector3(PathCenterX(aheadZ) - PathCenterX(behindZ), 0f, aheadZ - behindZ).normalized;
                Vector3 normal = new Vector3(-tangent.z, 0f, tangent.x);
                float widthNoise = Mathf.Lerp(0.86f, 1.15f, Mathf.PerlinNoise(seed * 0.021f, t * 8.3f));
                Vector3 left = new Vector3(x, 0f, z) - normal * halfWidth * widthNoise;
                Vector3 right = new Vector3(x, 0f, z) + normal * halfWidth * widthNoise;
                left.y = HeightAt(left.x, left.z) + 0.08f;
                right.y = HeightAt(right.x, right.z) + 0.08f;

                int v = i * 2;
                vertices[v] = left;
                vertices[v + 1] = right;
                uv[v] = new Vector2(0f, t);
                uv[v + 1] = new Vector2(1f, t);
            }

            for (int i = 0; i < segments; i++)
            {
                int v = i * 2;
                int tri = i * 6;
                triangles[tri] = v;
                triangles[tri + 1] = v + 1;
                triangles[tri + 2] = v + 2;
                triangles[tri + 3] = v + 1;
                triangles[tri + 4] = v + 3;
                triangles[tri + 5] = v + 2;
            }

            Mesh mesh = new Mesh { name = "Smooth Valendia path ribbon" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject ribbon = CreateMeshObject("Smooth Dust Path", mesh, pathMaterial, parent);
            MeshRenderer renderer = ribbon.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void GenerateDistantSpires()
        {
            Transform parent = CreateContainer("Encircling Limestone Mountains");
            System.Random random = new System.Random(seed + 707);
            int mountainsPerSide = Mathf.Max(14, Mathf.CeilToInt(distantSpireCount / 4f), chunksPerAxis * 4);
            float halfWorld = WorldSize * 0.5f;
            float outerOffset = chunkSize * 0.18f;
            float sideSpacing = WorldSize / mountainsPerSide;

            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < mountainsPerSide; i++)
                {
                    float along = -halfWorld + sideSpacing * (i + 0.5f) + Mathf.Lerp(-sideSpacing * 0.22f, sideSpacing * 0.22f, (float)random.NextDouble());
                    float outside = halfWorld + outerOffset + Mathf.Lerp(-chunkSize * 0.05f, chunkSize * 0.2f, (float)random.NextDouble());
                    float x = side < 2 ? along : side == 2 ? -outside : outside;
                    float z = side < 2 ? side == 0 ? -outside : outside : along;
                    float ringBreakup = Mathf.PerlinNoise((x + seed * 0.13f) * 0.0035f, (z - seed * 0.17f) * 0.0035f);
                    float baseHeight = HeightAt(x, z) - Mathf.Lerp(2f, 8f, (float)random.NextDouble());
                    float height = Mathf.Lerp(42f, 102f, (float)random.NextDouble()) * Mathf.Lerp(0.86f, 1.18f, ringBreakup);
                    float width = sideSpacing * Mathf.Lerp(1.85f, 2.35f, (float)random.NextDouble()) * Mathf.Lerp(0.95f, 1.15f, ringBreakup);
                    float depth = chunkSize * Mathf.Lerp(0.32f, 0.52f, (float)random.NextDouble()) * Mathf.Lerp(0.88f, 1.14f, ringBreakup);

                    GameObject spire = CreateMeshObject(
                        "Layered Limestone Mountain",
                        CreateLayeredMassifMesh(width, height, depth, random),
                        rockMaterial,
                        parent);
                    spire.transform.position = new Vector3(x, baseHeight, z);
                    float rotation = side < 2 ? 0f : 90f;
                    spire.transform.rotation = Quaternion.Euler(0f, rotation + Mathf.Lerp(-7f, 7f, (float)random.NextDouble()), 0f);
                    spire.isStatic = true;
                    AddApproximateBoxCollider(
                        spire,
                        new Vector3(0f, height * 0.42f, 0f),
                        new Vector3(width * 0.84f, height * 0.86f, depth * 0.88f));
                }
            }

            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner < 2 ? -halfWorld - outerOffset * 0.72f : halfWorld + outerOffset * 0.72f;
                float z = corner == 0 || corner == 2 ? -halfWorld - outerOffset * 0.72f : halfWorld + outerOffset * 0.72f;
                float baseHeight = HeightAt(x, z) - Mathf.Lerp(3f, 9f, (float)random.NextDouble());
                float height = Mathf.Lerp(64f, 122f, (float)random.NextDouble());
                float width = sideSpacing * Mathf.Lerp(2.25f, 2.85f, (float)random.NextDouble());
                float depth = chunkSize * Mathf.Lerp(0.46f, 0.7f, (float)random.NextDouble());

                GameObject cornerMountain = CreateMeshObject(
                    "Corner Limestone Mountain",
                    CreateLayeredMassifMesh(width, height, depth, random),
                    rockMaterial,
                    parent);
                cornerMountain.transform.position = new Vector3(x, baseHeight, z);
                cornerMountain.transform.rotation = Quaternion.Euler(0f, 45f + corner * 90f + Mathf.Lerp(-8f, 8f, (float)random.NextDouble()), 0f);
                cornerMountain.isStatic = true;
                AddApproximateBoxCollider(
                    cornerMountain,
                    new Vector3(0f, height * 0.42f, 0f),
                    new Vector3(width * 0.86f, height * 0.86f, depth * 0.9f));
            }
        }

        private void GenerateAuthoredGroves()
        {
            Transform parent = CreateContainer("Authored Path Groves");
            System.Random random = new System.Random(seed + 1001);

            for (int grove = 0; grove < authoredGroveCount; grove++)
            {
                float t = Mathf.Lerp(0.14f, 0.86f, (grove + 0.5f) / Mathf.Max(1f, authoredGroveCount));
                t += Mathf.Lerp(-0.035f, 0.035f, (float)random.NextDouble());
                float z = Mathf.Lerp(WorldMin.y, -WorldMin.y, Mathf.Clamp01(t));
                float side = grove % 2 == 0 ? -1f : 1f;
                float x = PathCenterX(z) + side * Mathf.Lerp(20f, 48f, (float)random.NextDouble());
                Vector3 center = new Vector3(x, HeightAt(x, z), z);
                int trees = random.Next(18, 32);

                for (int i = 0; i < trees; i++)
                {
                    float angle = Mathf.PI * 2f * i / trees + Mathf.Lerp(-0.45f, 0.45f, (float)random.NextDouble());
                    float radius = Mathf.Lerp(3.0f, 25f, (float)random.NextDouble());
                    Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    point.y = HeightAt(point.x, point.z);

                    if (IsOnPath(point.x, point.z, pathWidth * 0.5f + 3f) || SlopeAt(point.x, point.z) > maxTreeSlope)
                    {
                        continue;
                    }

                    Biome biome = grove % 3 == 0 ? Biome.AutumnGrove : grove % 3 == 1 ? Biome.GoldenGrass : Biome.ValleyGrass;
                    GameObject tree = CreateTree(parent, point, random, biome);
                    tree.name = i == 0 ? "Hero Grove Tree" : "Grove Tree";
                    tree.transform.localScale *= i == 0 ? 1.55f : Mathf.Lerp(0.95f, 1.25f, (float)random.NextDouble());
                }
            }
        }

        private void GenerateForestPockets()
        {
            Transform parent = CreateContainer("Autumn Forest Pockets");
            System.Random random = new System.Random(seed + 1212);
            int placedPockets = 0;
            int attempts = forestPocketCount * 8;

            for (int attempt = 0; attempt < attempts && placedPockets < forestPocketCount; attempt++)
            {
                Vector3 center = RandomPoint(random);
                if (IsOnPath(center.x, center.z, pathWidth * 0.5f + 28f) || SlopeAt(center.x, center.z) > maxTreeSlope - 3f)
                {
                    continue;
                }

                Biome centerBiome = BiomeAt(center.x, center.z);
                if (centerBiome == Biome.MountainScrub)
                {
                    continue;
                }

                center.y = HeightAt(center.x, center.z);
                int trees = random.Next(16, 31);
                float pocketRadius = Mathf.Lerp(14f, 34f, (float)random.NextDouble());
                bool autumnPocket = random.NextDouble() < 0.62;

                for (int i = 0; i < trees; i++)
                {
                    float angle = Mathf.PI * 2f * i / trees + Mathf.Lerp(-0.7f, 0.7f, (float)random.NextDouble());
                    float radius = pocketRadius * Mathf.Sqrt((float)random.NextDouble());
                    Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                    if (IsOnPath(point.x, point.z, pathWidth * 0.5f + pathVegetationClearance) || SlopeAt(point.x, point.z) > maxTreeSlope)
                    {
                        continue;
                    }

                    point.y = HeightAt(point.x, point.z);
                    Biome treeBiome = autumnPocket
                        ? (random.NextDouble() < 0.72 ? Biome.AutumnGrove : Biome.GoldenGrass)
                        : (random.NextDouble() < 0.35 ? Biome.GoldenGrass : centerBiome);
                    GameObject tree = CreateTree(parent, point, random, treeBiome);
                    tree.name = autumnPocket ? "Autumn Forest Tree" : "Valley Forest Tree";
                    tree.transform.localScale *= Mathf.Lerp(0.9f, 1.38f, (float)random.NextDouble());
                }

                int undergrowth = random.Next(6, 11);
                for (int i = 0; i < undergrowth; i++)
                {
                    Vector3 patchPoint = center + new Vector3(
                        Mathf.Lerp(-pocketRadius, pocketRadius, (float)random.NextDouble()),
                        0f,
                        Mathf.Lerp(-pocketRadius, pocketRadius, (float)random.NextDouble()));
                    patchPoint.y = HeightAt(patchPoint.x, patchPoint.z);
                    CreateMeadowPatch(parent, patchPoint, autumnPocket ? Biome.GoldenGrass : centerBiome, random, Mathf.Lerp(0.9f, 1.8f, (float)random.NextDouble()));
                }

                placedPockets++;
            }
        }

        private void GenerateFlowerRibbons()
        {
            Transform parent = CreateContainer("Authored Flower Ribbons");
            System.Random random = new System.Random(seed + 1111);

            for (int ribbon = 0; ribbon < flowerRibbonCount; ribbon++)
            {
                float t = Mathf.Lerp(0.12f, 0.88f, (ribbon + 0.5f) / Mathf.Max(1f, flowerRibbonCount));
                float z = Mathf.Lerp(WorldMin.y, -WorldMin.y, t);
                float side = random.NextDouble() < 0.5 ? -1f : 1f;
                float x = PathCenterX(z) + side * Mathf.Lerp(12f, 42f, (float)random.NextDouble());
                Biome biome = random.NextDouble() < 0.82 ? Biome.LavenderField : Biome.GoldenGrass;

                int strokes = random.Next(12, 22);
                for (int i = 0; i < strokes; i++)
                {
                    Vector3 point = new Vector3(
                        x + Mathf.Lerp(-22f, 22f, (float)random.NextDouble()),
                        0f,
                        z + Mathf.Lerp(-28f, 28f, (float)random.NextDouble()));
                    point.y = HeightAt(point.x, point.z);

                    if (IsOnPath(point.x, point.z, pathWidth * 0.5f + 1.5f) || SlopeAt(point.x, point.z) > 18f)
                    {
                        continue;
                    }

                    CreateMeadowPatch(parent, point, biome, random, Mathf.Lerp(1.7f, 2.9f, (float)random.NextDouble()));
                }
            }
        }

        private void GenerateClouds()
        {
            Transform parent = CreateContainer("Small Floating Clouds");
            System.Random random = new System.Random(seed + 808);
            int columns = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, cloudCount) * 1.35f));
            int rows = Mathf.CeilToInt(cloudCount / (float)columns);
            float xMin = -WorldSize * 0.62f;
            float xMax = WorldSize * 0.62f;
            float zMin = -WorldSize * 0.42f;
            float zMax = WorldSize * 0.62f;

            for (int i = 0; i < cloudCount; i++)
            {
                int column = i % columns;
                int row = i / columns;
                float xT = (column + 0.5f + Mathf.Lerp(-0.28f, 0.28f, (float)random.NextDouble())) / columns;
                float zT = (row + 0.5f + Mathf.Lerp(-0.26f, 0.26f, (float)random.NextDouble())) / rows;
                Vector3 position = new Vector3(
                    Mathf.Lerp(xMin, xMax, Mathf.Clamp01(xT)),
                    Mathf.Lerp(128f, 190f, (float)random.NextDouble()),
                    Mathf.Lerp(zMin, zMax, Mathf.Clamp01(zT)));
                CreateCloud(parent, position, random);
            }
        }

        private void GenerateCloudBanks()
        {
            Transform parent = CreateContainer("Illustrative Cloud Banks");
            System.Random random = new System.Random(seed + 818);
            int columns = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, cloudBankCount) * 1.2f));
            int rows = Mathf.CeilToInt(cloudBankCount / (float)columns);
            float xMin = -WorldSize * 0.60f;
            float xMax = WorldSize * 0.60f;
            float zMin = -WorldSize * 0.36f;
            float zMax = WorldSize * 0.56f;

            for (int i = 0; i < cloudBankCount; i++)
            {
                int column = i % columns;
                int row = i / columns;
                float xT = (column + 0.5f + Mathf.Lerp(-0.22f, 0.22f, (float)random.NextDouble())) / columns;
                float zT = (row + 0.5f + Mathf.Lerp(-0.24f, 0.24f, (float)random.NextDouble())) / rows;
                Vector3 position = new Vector3(
                    Mathf.Lerp(xMin, xMax, Mathf.Clamp01(xT)),
                    Mathf.Lerp(116f, 168f, (float)random.NextDouble()),
                    Mathf.Lerp(zMin, zMax, Mathf.Clamp01(zT)));
                CreateCloudBank(parent, position, random);
            }
        }

        private void GenerateHorizonCloudBelt()
        {
            Transform parent = CreateContainer("Horizon Cloud Belt");
            System.Random random = new System.Random(seed + 828);

            for (int i = 0; i < horizonCloudBankCount; i++)
            {
                float t = horizonCloudBankCount <= 1 ? 0.5f : i / (float)(horizonCloudBankCount - 1);
                Vector3 position = new Vector3(
                    Mathf.Lerp(-WorldSize * 0.56f, WorldSize * 0.56f, t) + Mathf.Lerp(-18f, 18f, (float)random.NextDouble()),
                    Mathf.Lerp(112f, 152f, (float)random.NextDouble()),
                    Mathf.Lerp(WorldSize * 0.05f, WorldSize * 0.34f, (float)random.NextDouble()));
                CreateCloudBank(parent, position, random);
            }
        }
    }
}
