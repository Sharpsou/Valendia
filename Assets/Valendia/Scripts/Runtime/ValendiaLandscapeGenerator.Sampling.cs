using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
        private float HeightAt(float x, float z)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float value = 0f;
            float normalizer = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sample = Mathf.PerlinNoise((x + seed * 13.17f) / noiseScale * frequency, (z - seed * 7.31f) / noiseScale * frequency);
                value += sample * amplitude;
                normalizer += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            value = normalizer > 0f ? value / normalizer : value;
            float rolling = Mathf.SmoothStep(0.08f, 0.92f, value) * heightScale;

            float radial = new Vector2(x, z).magnitude / (WorldSize * 0.5f);
            float mountainMask = Mathf.SmoothStep(0.64f, 1f, radial);
            float ridges = 1f - Mathf.Abs(2f * Mathf.PerlinNoise((x - seed) * 0.004f, (z + seed) * 0.004f) - 1f);
            float mountains = ridges * ridges * heightScale * 0.82f * mountainMask * distantMountainStrength;
            float ringNoise = Mathf.PerlinNoise((x + seed * 0.23f) * 0.0032f, (z - seed * 0.37f) * 0.0032f);
            float wallMask = Mathf.SmoothStep(0.76f, 1.03f, radial);
            float mountainWall = Mathf.Lerp(0.72f, 1.18f, ringNoise) * heightScale * wallMask * borderMountainWallStrength;

            float pathCenter = PathCenterX(z);
            float pathDistance = Mathf.Abs(x - pathCenter);
            float pathBlend = 1f - Mathf.SmoothStep(pathWidth * 0.5f, pathWidth * 1.8f, pathDistance);
            float baseHeight = rolling + mountains + mountainWall;
            float pathHeight = rolling * 0.96f + mountains * 0.62f + mountainWall * 0.72f;
            float height = Mathf.Lerp(baseHeight, pathHeight, pathBlend * 0.36f);

            return height + GroundMicroReliefAt(x, z, pathDistance);
        }

        private float SlopeAt(float x, float z)
        {
            const float step = 2f;
            float dx = Mathf.Abs(HeightAt(x + step, z) - HeightAt(x - step, z));
            float dz = Mathf.Abs(HeightAt(x, z + step) - HeightAt(x, z - step));
            return Mathf.Atan(Mathf.Max(dx, dz) / (step * 2f)) * Mathf.Rad2Deg;
        }

        private float GroundMicroReliefAt(float x, float z, float pathDistance)
        {
            if (terrainMicroReliefStrength <= 0f)
            {
                return 0f;
            }

            float pathMask = Mathf.SmoothStep(pathWidth * 0.85f, pathWidth * 2.15f, pathDistance);
            float radial = new Vector2(x, z).magnitude / (WorldSize * 0.5f);
            float ridgeCalm = Mathf.Lerp(1f, 0.45f, Mathf.SmoothStep(0.68f, 0.96f, radial));
            float scale = Mathf.Max(4f, terrainMicroReliefScale);
            float broad = Mathf.PerlinNoise((x + seed * 0.31f) / scale, (z - seed * 0.19f) / scale);
            float fine = Mathf.PerlinNoise((x - seed * 0.43f) / (scale * 0.48f), (z + seed * 0.27f) / (scale * 0.48f));
            float broken = Mathf.PerlinNoise((x + seed * 0.07f) / (scale * 1.9f), (z + seed * 0.11f) / (scale * 1.9f));
            float detail = (broad * 0.55f + fine * 0.30f + broken * 0.15f - 0.5f) * 2f;

            return detail * terrainMicroReliefStrength * pathMask * ridgeCalm;
        }

        private float PathCenterX(float z)
        {
            return Mathf.Sin((z + seed) * 0.012f) * WorldSize * 0.16f + Mathf.Sin((z - seed) * 0.027f) * WorldSize * 0.045f;
        }

        private bool IsOnPath(float x, float z, float radius)
        {
            return Mathf.Abs(x - PathCenterX(z)) <= radius;
        }

        private Biome BiomeAt(float x, float z)
        {
            float radial = new Vector2(x, z).magnitude / (WorldSize * 0.5f);
            float edgeNoise = Mathf.PerlinNoise((x + seed * 0.29f) * 0.004f, (z - seed * 0.33f) * 0.004f);
            float mountainScrubThreshold = Mathf.Lerp(0.88f, 0.96f, edgeNoise);
            if (radial > mountainScrubThreshold)
            {
                return Biome.MountainScrub;
            }

            float field = Mathf.PerlinNoise((x + seed * 3.9f) * 0.0065f, (z - seed * 2.7f) * 0.0065f);
            float lavenderBreakup = Mathf.PerlinNoise((x - seed * 1.1f) * 0.0042f, (z + seed * 1.9f) * 0.0042f);
            if (field > 0.56f && lavenderBreakup > 0.50f)
            {
                return Biome.LavenderField;
            }

            if (field < 0.26f)
            {
                return Biome.AutumnGrove;
            }

            return field > 0.80f ? Biome.GoldenGrass : Biome.ValleyGrass;
        }

        private Biome GroundBiomeAt(float x, float z)
        {
            Biome biome = BiomeAt(x, z);
            if (biome != Biome.MountainScrub)
            {
                return biome;
            }

            float field = Mathf.PerlinNoise((x + seed * 0.41f) * 0.006f, (z - seed * 0.49f) * 0.006f);
            if (field < 0.18f)
            {
                return Biome.AutumnGrove;
            }

            return field > 0.74f ? Biome.GoldenGrass : Biome.ValleyGrass;
        }

        private static Color BiomeGroundColor(Biome biome)
        {
            switch (biome)
            {
                case Biome.AutumnGrove:
                    return new Color(0.42f, 0.48f, 0.30f);
                case Biome.GoldenGrass:
                    return new Color(0.48f, 0.54f, 0.32f);
                case Biome.LavenderField:
                    return new Color(0.34f, 0.52f, 0.42f);
                case Biome.MountainScrub:
                    return new Color(0.34f, 0.43f, 0.30f);
                default:
                    return new Color(0.30f, 0.56f, 0.38f);
            }
        }

        private Color GroundVertexColorAt(float x, float z, Biome biome)
        {
            Color baseColor = IsOnPath(x, z, pathWidth * 0.5f)
                ? new Color(0.61f, 0.53f, 0.36f)
                : BiomeGroundColor(biome);

            float moss = Mathf.PerlinNoise((x + seed * 0.61f) * 0.038f, (z - seed * 0.52f) * 0.038f);
            float soil = Mathf.PerlinNoise((x - seed * 0.21f) * 0.092f, (z + seed * 0.73f) * 0.092f);
            float shade = Mathf.Lerp(0.88f, 1.06f, moss * 0.72f + soil * 0.28f);
            Color olive = new Color(0.34f, 0.43f, 0.26f);
            Color warmed = Color.Lerp(baseColor, olive, Mathf.Clamp01((1f - moss) * 0.18f));

            return new Color(
                Mathf.Clamp01(warmed.r * shade),
                Mathf.Clamp01(warmed.g * shade),
                Mathf.Clamp01(warmed.b * shade),
                1f);
        }

        private Vector3 RandomPoint(System.Random random)
        {
            Vector2 min = WorldMin;
            float x = min.x + (float)random.NextDouble() * WorldSize;
            float z = min.y + (float)random.NextDouble() * WorldSize;
            return new Vector3(x, 0f, z);
        }

        private Transform CreateContainer(string name)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(generatedRoot, false);
            return container.transform;
        }
    }
}
