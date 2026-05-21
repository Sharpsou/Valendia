using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
        private void BeginMeadowBatches()
        {
            meadowBatchRoot = CreateContainer("Organic Meadow Grass Batches");
            int chunkCount = GrassBatchGrid * GrassBatchGrid;
            greenMeadowBatch = new MeadowBatchState("Green Meadow Stroke Batch", meadowMaterial, chunkCount);
            goldenMeadowBatch = new MeadowBatchState("Golden Meadow Stroke Batch", goldenMeadowMaterial, chunkCount);
            lavenderMeadowBatch = new MeadowBatchState("Lavender Meadow Stroke Batch", lavenderMeadowMaterial, chunkCount);
        }

        private void CreateMeadowPatch(Transform parent, Vector3 center, Biome biome, System.Random random, float sizeMultiplier = 1f)
        {
            if (meadowBatchRoot == null)
            {
                BeginMeadowBatches();
            }

            MeadowBatchState batch = MeadowBatchForBiome(biome, random);
            int chunk = GrassChunkIndex(center);
            batch.Vertices[chunk] ??= new List<Vector3>(MaxMeadowBatchVertices);
            batch.Triangles[chunk] ??= new List<int>(MaxMeadowBatchVertices);

            if (batch.Vertices[chunk].Count > MaxMeadowBatchVertices - MaxMeadowPatchVertices)
            {
                FlushMeadowBatchChunk(batch, chunk);
            }

            AddOrganicMeadowPatchGeometry(batch.Vertices[chunk], batch.Triangles[chunk], center, biome, random, sizeMultiplier);
        }

        private MeadowBatchState MeadowBatchForBiome(Biome biome, System.Random random)
        {
            if (biome == Biome.LavenderField)
            {
                return lavenderMeadowBatch;
            }

            if (biome == Biome.GoldenGrass || biome == Biome.AutumnGrove)
            {
                if (random.NextDouble() < 0.58)
                {
                    return greenMeadowBatch;
                }

                return goldenMeadowBatch;
            }

            return greenMeadowBatch;
        }

        private void FlushAllMeadowBatches()
        {
            FlushMeadowBatchState(greenMeadowBatch);
            FlushMeadowBatchState(goldenMeadowBatch);
            FlushMeadowBatchState(lavenderMeadowBatch);
        }

        private void FlushMeadowBatchState(MeadowBatchState batch)
        {
            if (batch == null)
            {
                return;
            }

            for (int chunk = 0; chunk < batch.Vertices.Length; chunk++)
            {
                FlushMeadowBatchChunk(batch, chunk);
            }
        }

        private void FlushMeadowBatchChunk(MeadowBatchState batch, int chunk)
        {
            List<Vector3> vertices = batch.Vertices[chunk];
            List<int> triangles = batch.Triangles[chunk];
            if (vertices == null || vertices.Count == 0)
            {
                return;
            }

            Mesh mesh = new Mesh { name = $"{batch.Prefix} mesh {chunk:00}-{batch.Indexes[chunk]:00}" };
            mesh.indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Transform parent = meadowBatchRoot != null ? meadowBatchRoot : generatedRoot;
            CreateGrassLodObject($"{batch.Prefix} {chunk:00}-{batch.Indexes[chunk]:00}", mesh, batch.Material, parent);

            vertices.Clear();
            triangles.Clear();
            batch.Indexes[chunk]++;
        }

        private void AddOrganicMeadowPatchGeometry(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 center,
            Biome biome,
            System.Random random,
            float sizeMultiplier)
        {
            float radiusX = Mathf.Lerp(1.7f, 5.4f, (float)random.NextDouble()) * sizeMultiplier;
            float radiusZ = Mathf.Lerp(0.55f, 1.8f, (float)random.NextDouble()) * sizeMultiplier;
            float rotation = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
            float biomeDensity = biome == Biome.LavenderField ? 1.05f : biome == Biome.GoldenGrass ? 0.72f : 0.86f;
            int clumps = Mathf.RoundToInt(Mathf.Lerp(7f, 16f, (float)random.NextDouble()) * Mathf.Clamp(sizeMultiplier, 0.75f, 1.9f) * biomeDensity);

            for (int clump = 0; clump < clumps; clump++)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                float radius = Mathf.Sqrt((float)random.NextDouble());
                float localX = Mathf.Cos(angle) * radiusX * radius * Mathf.Lerp(0.72f, 1.12f, (float)random.NextDouble());
                float localZ = Mathf.Sin(angle) * radiusZ * radius * Mathf.Lerp(0.72f, 1.18f, (float)random.NextDouble());
                Vector3 offset = RotateYaw(new Vector3(localX, 0f, localZ), rotation);
                float worldX = center.x + offset.x;
                float worldZ = center.z + offset.z;
                float worldY = HeightAt(worldX, worldZ) + 0.035f;
                Vector3 clumpCenter = new Vector3(worldX, worldY, worldZ);

                int blades = biome == Biome.LavenderField ? random.Next(3, 6) : random.Next(4, 7);
                for (int blade = 0; blade < blades; blade++)
                {
                    float bladeAngle = rotation + angle + Mathf.Lerp(-0.9f, 0.9f, (float)random.NextDouble());
                    Vector3 baseOffset = RotateYaw(
                        new Vector3(
                            Mathf.Lerp(-0.16f, 0.16f, (float)random.NextDouble()),
                            0f,
                            Mathf.Lerp(-0.16f, 0.16f, (float)random.NextDouble())),
                        bladeAngle);
                    float height = biome == Biome.LavenderField
                        ? Mathf.Lerp(0.20f, 0.46f, (float)random.NextDouble())
                        : Mathf.Lerp(0.12f, 0.36f, (float)random.NextDouble());
                    float width = biome == Biome.LavenderField
                        ? Mathf.Lerp(0.025f, 0.055f, (float)random.NextDouble())
                        : Mathf.Lerp(0.035f, 0.08f, (float)random.NextDouble());
                    AddGrassBlade(
                        vertices,
                        triangles,
                        clumpCenter + baseOffset,
                        bladeAngle,
                        height,
                        width,
                        Mathf.Lerp(0.04f, 0.16f, (float)random.NextDouble()));
                }
            }
        }

        private int GrassChunkIndex(Vector3 point)
        {
            Vector2 min = WorldMin;
            int x = Mathf.Clamp(Mathf.FloorToInt((point.x - min.x) / WorldSize * GrassBatchGrid), 0, GrassBatchGrid - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((point.z - min.y) / WorldSize * GrassBatchGrid), 0, GrassBatchGrid - 1);
            return z * GrassBatchGrid + x;
        }

        private int GrassPaletteVariantAt(float x, float z, System.Random random)
        {
            float warm = Mathf.PerlinNoise((x + seed * 0.17f) * 0.009f, (z - seed * 0.23f) * 0.009f);
            float accent = Mathf.PerlinNoise((x - seed * 0.31f) * 0.018f, (z + seed * 0.29f) * 0.018f);
            float scatter = (float)random.NextDouble();
            Biome biome = GroundBiomeAt(x, z);

            if ((biome == Biome.LavenderField && accent > 0.54f) || (accent > 0.84f && scatter > 0.38f))
            {
                return 3;
            }

            if (biome == Biome.GoldenGrass || warm > 0.72f || (warm > 0.62f && scatter > 0.76f))
            {
                return 2;
            }

            if (biome == Biome.AutumnGrove || biome == Biome.MountainScrub || warm < 0.30f)
            {
                return 1;
            }

            return 0;
        }

        private Material GrassMaterialForVariant(int variant)
        {
            switch (variant)
            {
                case 1:
                    return oliveGrassMaterial;
                case 2:
                    return goldenGrassBladeMaterial;
                case 3:
                    return roseGrassMaterial;
                default:
                    return grassMaterial;
            }
        }

        private static string GrassBatchPrefix(int variant)
        {
            switch (variant)
            {
                case 1:
                    return "Olive Grass Batch";
                case 2:
                    return "Golden Grass Batch";
                case 3:
                    return "Rose Grass Batch";
                default:
                    return "Fresh Grass Batch";
            }
        }

        private void AddGrassTuftToBatch(
            Transform parent,
            List<Vector3> vertices,
            List<int> triangles,
            ref int batchIndex,
            string objectPrefix,
            Material material,
            Vector3 position,
            System.Random random)
        {
            if (vertices.Count > MaxGrassBatchVertices - MaxGrassTuftVertices)
            {
                FlushGrassBatch(parent, vertices, triangles, ref batchIndex, objectPrefix, material);
            }

            AddGrassTuftGeometry(vertices, triangles, position, random);
        }

        private void FlushGrassBatch(
            Transform parent,
            List<Vector3> vertices,
            List<int> triangles,
            ref int batchIndex,
            string objectPrefix,
            Material material)
        {
            if (vertices.Count == 0)
            {
                return;
            }

            Mesh mesh = new Mesh { name = $"{objectPrefix} mesh {batchIndex:00}" };
            mesh.indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            CreateGrassLodObject($"{objectPrefix} {batchIndex:00}", mesh, material, parent);

            vertices.Clear();
            triangles.Clear();
            batchIndex++;
        }

        private void CreateFlowerPatch(Transform parent, Vector3 position, System.Random random)
        {
            GameObject patch = new GameObject("Lavender Flower Patch");
            patch.transform.SetParent(parent, false);
            patch.transform.position = position;
            patch.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            patch.transform.localScale = Vector3.one * Mathf.Lerp(0.68f, 1.08f, (float)random.NextDouble());

            List<Vector3> stemVertices = new List<Vector3>(224);
            List<int> stemTriangles = new List<int>(336);
            List<Vector3> blossomVertices = new List<Vector3>(224);
            List<int> blossomTriangles = new List<int>(336);
            int stems = random.Next(12, 22);

            for (int i = 0; i < stems; i++)
            {
                float radius = Mathf.Lerp(0.08f, 1.05f, (float)random.NextDouble());
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                Vector3 basePoint = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float stemAngle = angle + Mathf.Lerp(-0.5f, 0.5f, (float)random.NextDouble());
                float height = Mathf.Lerp(0.22f, 0.58f, (float)random.NextDouble());
                AddGrassBlade(stemVertices, stemTriangles, basePoint, stemAngle, height, 0.028f, 0.06f);
                AddFlowerBlossom(blossomVertices, blossomTriangles, basePoint + Vector3.up * height, stemAngle, Mathf.Lerp(0.035f, 0.068f, (float)random.NextDouble()));
            }

            Mesh stemMesh = new Mesh { name = "Lavender stems mesh" };
            stemMesh.SetVertices(stemVertices);
            stemMesh.SetTriangles(stemTriangles, 0);
            stemMesh.RecalculateNormals();
            stemMesh.RecalculateBounds();
            GameObject stemObject = CreateMeshObject("Lavender Stems", stemMesh, grassMaterial, patch.transform);
            ConfigureGrassRenderer(stemObject.GetComponent<MeshRenderer>());

            Mesh blossomMesh = new Mesh { name = "Lavender blossoms mesh" };
            blossomMesh.SetVertices(blossomVertices);
            blossomMesh.SetTriangles(blossomTriangles, 0);
            blossomMesh.RecalculateNormals();
            blossomMesh.RecalculateBounds();
            GameObject blossoms = CreateMeshObject("Lavender Blossoms", blossomMesh, flowerMaterial, patch.transform);
            ConfigureSoftVegetation(blossoms.GetComponent<MeshRenderer>());
        }

        private void CreateScrub(Transform parent, Vector3 position, System.Random random)
        {
            GameObject scrub = CreateMeshObject(
                "Faceted Mountain Scrub",
                CreateFacetedBlobMesh(random, 0.34f),
                scrubMaterial,
                parent);
            scrub.transform.position = position;
            scrub.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            float scale = Mathf.Lerp(0.5f, 1.25f, (float)random.NextDouble());
            scrub.transform.localScale = new Vector3(scale * 1.4f, scale * 0.45f, scale);
        }
    }
}
