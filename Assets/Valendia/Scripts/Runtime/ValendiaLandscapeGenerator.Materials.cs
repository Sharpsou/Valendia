using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
        private void EnsureMaterials()
        {
            if (skyboxMaterial == null) skyboxMaterial = CreateSkyboxMaterial();
            if (groundMaterial == null) groundMaterial = CreateMaterial("Valendia Fresh Valley Ground", new Color(0.30f, 0.56f, 0.38f), 0.18f);
            if (autumnGroundMaterial == null) autumnGroundMaterial = CreateMaterial("Valendia Autumn Grove Ground", new Color(0.42f, 0.48f, 0.30f), 0.18f);
            if (goldenGrassGroundMaterial == null) goldenGrassGroundMaterial = CreateMaterial("Valendia Golden Grass Ground", new Color(0.48f, 0.54f, 0.32f), 0.18f);
            if (lavenderGroundMaterial == null) lavenderGroundMaterial = CreateMaterial("Valendia Lavender Field Ground", new Color(0.34f, 0.52f, 0.42f), 0.18f);
            if (scrubGroundMaterial == null) scrubGroundMaterial = CreateMaterial("Valendia Mountain Scrub Ground", new Color(0.32f, 0.42f, 0.30f), 0.2f);
            if (pathMaterial == null) pathMaterial = CreateMaterial("Valendia Warm Dust Path", new Color(0.56f, 0.39f, 0.22f), 0.28f);
            if (pathMaterial != null && pathMaterial.HasProperty("_Cull")) pathMaterial.SetFloat("_Cull", 0f);
            if (trunkMaterial == null) trunkMaterial = CreateMaterial("Valendia Faceted Trunk", new Color(0.26f, 0.14f, 0.09f), 0.32f);
            if (leafMaterial == null) leafMaterial = CreateMaterial("Valendia Spring Leaf Crowns", new Color(0.20f, 0.49f, 0.25f), 0.28f);
            if (autumnLeafMaterial == null) autumnLeafMaterial = CreateMaterial("Valendia Autumn Leaf Crowns", new Color(0.63f, 0.38f, 0.18f), 0.28f);
            if (grassMaterial == null) grassMaterial = CreateMaterial("Valendia Fresh Green Grass Blades", new Color(0.34f, 0.62f, 0.34f), 0.2f);
            if (oliveGrassMaterial == null) oliveGrassMaterial = CreateMaterial("Valendia Olive Grass Blades", new Color(0.28f, 0.44f, 0.25f), 0.22f);
            if (goldenGrassBladeMaterial == null) goldenGrassBladeMaterial = CreateMaterial("Valendia Golden Straw Grass Blades", new Color(0.72f, 0.60f, 0.25f), 0.18f);
            if (roseGrassMaterial == null) roseGrassMaterial = CreateMaterial("Valendia Rose Heather Grass Blades", new Color(0.72f, 0.40f, 0.62f), 0.16f);
            if (flowerMaterial == null) flowerMaterial = CreateMaterial("Valendia Lavender Blossoms", new Color(0.92f, 0.36f, 0.72f), 0.12f);
            if (scrubMaterial == null) scrubMaterial = CreateMaterial("Valendia Mountain Scrub", new Color(0.29f, 0.39f, 0.25f), 0.24f);
            if (meadowMaterial == null) meadowMaterial = CreateMaterial("Valendia Meadow Brush", new Color(0.22f, 0.50f, 0.30f), 0.16f);
            if (goldenMeadowMaterial == null) goldenMeadowMaterial = CreateMaterial("Valendia Golden Meadow Brush", new Color(0.50f, 0.50f, 0.24f), 0.16f);
            if (lavenderMeadowMaterial == null) lavenderMeadowMaterial = CreateMaterial("Valendia Lavender Meadow Brush", new Color(0.58f, 0.38f, 0.58f), 0.16f);
            if (warmLeafMaterial == null) warmLeafMaterial = CreateMaterial("Valendia Warm Leaf Crowns", new Color(0.72f, 0.50f, 0.22f), 0.28f);
            if (darkLeafMaterial == null) darkLeafMaterial = CreateMaterial("Valendia Deep Green Crowns", new Color(0.12f, 0.27f, 0.12f), 0.3f);
            if (rockMaterial == null) rockMaterial = CreateMaterial("Valendia Warm Limestone", new Color(0.72f, 0.62f, 0.44f), 0.38f);
            if (cloudMaterial == null) cloudMaterial = CreateUnlitMaterial("Valendia Soft Autumn Cloud", new Color(0.96f, 0.88f, 0.66f));
            if (cloudShadowCasterMaterial == null) cloudShadowCasterMaterial = CreateMaterial("Valendia Cloud Shadow Caster", new Color(0.88f, 0.82f, 0.68f), 0f);

            EnsureGroundDetailTextures();
            ConfigureGroundDetailMaterial(groundMaterial, groundDetailTexture, groundNormalTexture, groundTextureTiling, groundNormalStrength);
            ConfigureGroundDetailMaterial(autumnGroundMaterial, groundDetailTexture, groundNormalTexture, groundTextureTiling, groundNormalStrength);
            ConfigureGroundDetailMaterial(goldenGrassGroundMaterial, groundDetailTexture, groundNormalTexture, groundTextureTiling, groundNormalStrength);
            ConfigureGroundDetailMaterial(lavenderGroundMaterial, groundDetailTexture, groundNormalTexture, groundTextureTiling, groundNormalStrength);
            ConfigureGroundDetailMaterial(scrubGroundMaterial, groundDetailTexture, groundNormalTexture, groundTextureTiling * 0.85f, groundNormalStrength * 0.75f);

            ConfigureDoubleSidedMaterial(leafMaterial);
            ConfigureDoubleSidedMaterial(autumnLeafMaterial);
            ConfigureDoubleSidedMaterial(warmLeafMaterial);
            ConfigureDoubleSidedMaterial(darkLeafMaterial);
            ConfigureDoubleSidedMaterial(grassMaterial);
            ConfigureDoubleSidedMaterial(oliveGrassMaterial);
            ConfigureDoubleSidedMaterial(goldenGrassBladeMaterial);
            ConfigureDoubleSidedMaterial(roseGrassMaterial);
            ConfigureDoubleSidedMaterial(flowerMaterial);
            ConfigureDoubleSidedMaterial(rockMaterial);
        }

        private void EnsureGroundDetailTextures()
        {
            if (groundDetailTexture == null)
            {
                groundDetailTexture = CreateGroundDetailTexture(
                    "Valendia Organic Ground Detail",
                    new Color(0.86f, 0.92f, 0.78f),
                    new Color(1f, 1f, 0.93f),
                    0f);
            }

            if (groundNormalTexture == null)
            {
                groundNormalTexture = CreateGroundNormalTexture("Valendia Organic Ground Normal", 0f, 1.45f);
            }
        }

        private Texture2D CreateGroundDetailTexture(string textureName, Color shadowTint, Color lightTint, float offset)
        {
            Texture2D texture = new Texture2D(GroundDetailTextureSize, GroundDetailTextureSize, TextureFormat.RGBA32, true)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < GroundDetailTextureSize; y++)
            {
                for (int x = 0; x < GroundDetailTextureSize; x++)
                {
                    float u = x / (float)GroundDetailTextureSize;
                    float v = y / (float)GroundDetailTextureSize;
                    float detail = GroundDetailTextureNoise(u, v, offset);
                    float fleck = Mathf.PerlinNoise(u * 57.3f + seed * 0.002f + offset, v * 61.1f - seed * 0.003f);
                    float mixed = Mathf.Clamp01(0.5f + (detail - 0.5f) * groundTextureStrength + (fleck - 0.5f) * groundTextureStrength * 0.24f);
                    Color color = Color.Lerp(shadowTint, lightTint, mixed);
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, true);
            return texture;
        }

        private Texture2D CreateGroundNormalTexture(string textureName, float offset, float normalContrast)
        {
            Texture2D texture = new Texture2D(GroundDetailTextureSize, GroundDetailTextureSize, TextureFormat.RGBA32, true, true)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            float step = 1f / GroundDetailTextureSize;
            for (int y = 0; y < GroundDetailTextureSize; y++)
            {
                for (int x = 0; x < GroundDetailTextureSize; x++)
                {
                    float u = x / (float)GroundDetailTextureSize;
                    float v = y / (float)GroundDetailTextureSize;
                    float left = GroundDetailTextureNoise(Wrap01(u - step), v, offset);
                    float right = GroundDetailTextureNoise(Wrap01(u + step), v, offset);
                    float down = GroundDetailTextureNoise(u, Wrap01(v - step), offset);
                    float up = GroundDetailTextureNoise(u, Wrap01(v + step), offset);
                    Vector3 normal = new Vector3((left - right) * normalContrast, (down - up) * normalContrast, 1f).normalized;
                    float encodedX = normal.x * 0.5f + 0.5f;
                    float encodedY = normal.y * 0.5f + 0.5f;
                    float encodedZ = normal.z * 0.5f + 0.5f;

                    texture.SetPixel(x, y, new Color(encodedX, encodedY, encodedZ, encodedX));
                }
            }

            texture.Apply(true, true);
            return texture;
        }

        private float GroundDetailTextureNoise(float u, float v, float offset)
        {
            float seedOffset = seed * 0.00037f + offset;
            float broad = Mathf.PerlinNoise(u * 3.7f + seedOffset, v * 3.1f - seedOffset);
            float mid = Mathf.PerlinNoise(u * 11.9f - seedOffset * 0.7f, v * 9.4f + seedOffset * 0.9f);
            float grain = Mathf.PerlinNoise(u * 29.5f + seedOffset * 1.3f, v * 33.2f - seedOffset * 1.1f);
            return Mathf.Clamp01(broad * 0.26f + mid * 0.34f + grain * 0.40f);
        }

        private static float Wrap01(float value)
        {
            value %= 1f;
            return value < 0f ? value + 1f : value;
        }

        private static void ConfigureGroundDetailMaterial(Material material, Texture2D albedo, Texture2D normal, float tiling, float normalStrength)
        {
            if (material == null)
            {
                return;
            }

            Vector2 scale = Vector2.one * Mathf.Max(1f, tiling);
            if (albedo != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", albedo);
                    material.SetTextureScale("_BaseMap", scale);
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", albedo);
                    material.SetTextureScale("_MainTex", scale);
                }
            }

            if (normal != null && normalStrength > 0f && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", scale);
                if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
        }

        private void ApplyAtmosphere()
        {
            ApplyRuntimeResolution();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.52f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.39f, 0.32f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.11f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.45f, 0.58f, 0.48f);
            RenderSettings.fogDensity = 0.00052f;
            RenderSettings.skybox = skyboxMaterial;

            Light sun = FindOrCreateSun();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.72f, 0.45f);
            sun.intensity = 1.10f;
            sun.shadows = LightShadows.Hard;
            sun.shadowStrength = 0.58f;
            sun.transform.rotation = Quaternion.Euler(24f, -42f, 0f);
            RenderSettings.sun = sun;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 900f;
            QualitySettings.shadowCascades = 4;

            DynamicGI.UpdateEnvironment();
        }

        private void ApplyRuntimeResolution()
        {
            if (!Application.isPlaying || !IsPlayableOptimized)
            {
                return;
            }

            if (Screen.width != 1280 || Screen.height != 800)
            {
                Screen.SetResolution(1280, 800, FullScreenMode.FullScreenWindow);
            }
        }

        private static Light FindOrCreateSun()
        {
            GameObject sunObject = GameObject.Find("Valendia Sun");
            if (sunObject == null)
            {
                sunObject = new GameObject("Valendia Sun");
            }

            Light sun = sunObject.GetComponent<Light>();
            if (sun == null)
            {
                sun = sunObject.AddComponent<Light>();
            }

            return sun;
        }

        private static Material CreateSkyboxMaterial()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader) { name = "Valendia Painted Cyan Sky" };
            if (material.HasProperty("_SkyTint")) material.SetColor("_SkyTint", new Color(0.12f, 0.55f, 0.40f));
            if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor", new Color(0.52f, 0.48f, 0.32f));
            if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 0.72f);
            if (material.HasProperty("_AtmosphereThickness")) material.SetFloat("_AtmosphereThickness", 0.80f);
            if (material.HasProperty("_SunSize")) material.SetFloat("_SunSize", 0.05f);
            if (material.HasProperty("_SunSizeConvergence")) material.SetFloat("_SunSizeConvergence", 5f);
            return material;
        }

        private static Material CreateMaterial(string materialName, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            material.enableInstancing = true;
            return material;
        }

        private static Material CreateUnlitMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return CreateMaterial(materialName, color, 0f);
            }

            Material material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            return material;
        }

        private static void ConfigureDoubleSidedMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.doubleSidedGI = true;
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            if (material.HasProperty("_CullMode")) material.SetFloat("_CullMode", 0f);
            if (material.HasProperty("_DoubleSidedEnable")) material.SetFloat("_DoubleSidedEnable", 1f);
        }
    }
}
