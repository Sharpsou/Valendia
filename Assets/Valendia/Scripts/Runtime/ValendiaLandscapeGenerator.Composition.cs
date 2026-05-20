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

            for (int i = 0; i < cloudCount; i++)
            {
                Vector3 position = CloudFieldPoint(random, i, cloudCount, WorldSize * 0.62f, WorldSize * 0.50f, 128f, 190f);
                CreateCloud(parent, position, random);
            }
        }

        private void GenerateCloudBanks()
        {
            Transform parent = CreateContainer("Illustrative Cloud Banks");
            System.Random random = new System.Random(seed + 818);

            for (int i = 0; i < cloudBankCount; i++)
            {
                Vector3 position = CloudFieldPoint(random, i, cloudBankCount, WorldSize * 0.60f, WorldSize * 0.46f, 116f, 168f);
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

        private Vector3 CloudFieldPoint(System.Random random, int index, int count, float xRadius, float zRadius, float yMin, float yMax)
        {
            const float goldenAngle = 2.39996323f;
            float countSafe = Mathf.Max(1, count);
            float ring = Mathf.Sqrt((index + 0.5f) / countSafe);
            ring = Mathf.Clamp01(ring + Mathf.Lerp(-0.09f, 0.09f, (float)random.NextDouble()));
            float angle = index * goldenAngle + Mathf.Lerp(-0.38f, 0.38f, (float)random.NextDouble());
            float x = Mathf.Cos(angle) * ring * xRadius + Mathf.Lerp(-18f, 18f, (float)random.NextDouble());
            float z = Mathf.Sin(angle) * ring * zRadius + WorldSize * 0.10f + Mathf.Lerp(-16f, 16f, (float)random.NextDouble());
            return new Vector3(x, Mathf.Lerp(yMin, yMax, (float)random.NextDouble()), z);
        }
    }
}
