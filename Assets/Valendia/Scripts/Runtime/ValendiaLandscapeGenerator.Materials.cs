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
            groundDetailTexture ??= FindGroundTexture("_MainTex") ?? FindGroundTexture("_BaseMap");
            groundNormalTexture ??= FindGroundTexture("_BumpMap");

            if (groundDetailTexture == null)
            {
                groundDetailTexture = ValendiaGroundTextureFactory.CreateDetailTexture(
                    seed,
                    groundTextureStrength,
                    "Valendia Organic Ground Detail",
                    new Color(0.86f, 0.92f, 0.78f),
                    new Color(1f, 1f, 0.93f),
                    0f,
                    true);
            }

            if (groundNormalTexture == null)
            {
                groundNormalTexture = ValendiaGroundTextureFactory.CreateNormalTexture(
                    seed,
                    "Valendia Organic Ground Normal",
                    0f,
                    ValendiaGroundTextureFactory.NormalContrast,
                    true);
            }
        }

        private Texture2D FindGroundTexture(string propertyName)
        {
            return GetMaterialTexture(groundMaterial, propertyName)
                ?? GetMaterialTexture(autumnGroundMaterial, propertyName)
                ?? GetMaterialTexture(goldenGrassGroundMaterial, propertyName)
                ?? GetMaterialTexture(lavenderGroundMaterial, propertyName)
                ?? GetMaterialTexture(scrubGroundMaterial, propertyName);
        }

        private static Texture2D GetMaterialTexture(Material material, string propertyName)
        {
            if (material == null || !material.HasProperty(propertyName))
            {
                return null;
            }

            return material.GetTexture(propertyName) as Texture2D;
        }

        private static void ConfigureGroundDetailMaterial(Material material, Texture2D albedo, Texture2D normal, float tiling, float normalStrength)
        {
            ValendiaGroundTextureFactory.ConfigureMaterial(material, albedo, normal, tiling, normalStrength);
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
