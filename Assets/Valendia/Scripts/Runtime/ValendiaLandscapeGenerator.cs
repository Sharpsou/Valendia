using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    [ExecuteAlways]
    public sealed partial class ValendiaLandscapeGenerator : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private int seed = 170517;
        [SerializeField, Range(1, 8)] private int chunksPerAxis = 4;
        [SerializeField, Min(32f)] private float chunkSize = 180f;
        [SerializeField, Range(16, 96)] private int verticesPerChunk = 64;
        [SerializeField, Min(1f)] private float heightScale = 30f;
        [SerializeField, Min(10f)] private float noiseScale = 240f;
        [SerializeField, Range(1, 6)] private int octaves = 4;
        [SerializeField, Range(0.2f, 0.8f)] private float persistence = 0.48f;
        [SerializeField, Range(1.5f, 3.5f)] private float lacunarity = 2.1f;

        [Header("Ground Detail")]
        [SerializeField, Range(0f, 0.6f)] private float terrainMicroReliefStrength = 0.12f;
        [SerializeField, Min(4f)] private float terrainMicroReliefScale = 12f;
        [SerializeField, Range(0f, 1f)] private float groundTextureStrength = 0.28f;
        [SerializeField, Range(4f, 64f)] private float groundTextureTiling = 42f;
        [SerializeField, Range(0f, 2f)] private float groundNormalStrength = 0.18f;

        [Header("Composition")]
        [SerializeField, Min(2f)] private float pathWidth = 12f;
        [SerializeField, Min(0f)] private float pathVegetationClearance = 8f;
        [SerializeField, Range(0f, 1f)] private float distantMountainStrength = 0.12f;
        [SerializeField, Range(0f, 0.35f)] private float borderMountainWallStrength = 0.18f;

        [Header("Vegetation")]
        [SerializeField, Min(0)] private int treeCount = 920;
        [SerializeField, Min(0)] private int authoredGroveCount = 14;
        [SerializeField, Min(0)] private int forestPocketCount = 12;
        [SerializeField, Min(0)] private int meadowPatchCount = 2400;
        [SerializeField, Min(0)] private int flowerRibbonCount = 32;
        [SerializeField, Min(0)] private int pathEdgePatchCount = 2400;
        [SerializeField, Min(0)] private int grassTuftCount = 360000;
        [SerializeField, Min(0)] private int rockCount = 260;
        [SerializeField, Min(0)] private int flowerPatchCount = 680;
        [SerializeField, Min(0)] private int scrubCount = 300;
        [SerializeField, Min(0)] private int borderVegetationClusterCount = 144;
        [SerializeField, Range(0f, 35f)] private float maxTreeSlope = 24f;

        [Header("Atmosphere")]
        [SerializeField, Min(0)] private int cloudCount = 18;
        [SerializeField, Min(0)] private int cloudBankCount = 8;
        [SerializeField, Min(0)] private int horizonCloudBankCount = 9;
        [SerializeField, Min(0)] private int distantSpireCount = 64;

        [Header("Runtime")]
        [SerializeField] private GenerationQualityProfile qualityProfile = GenerationQualityProfile.PlayableOptimized;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material autumnGroundMaterial;
        [SerializeField] private Material goldenGrassGroundMaterial;
        [SerializeField] private Material lavenderGroundMaterial;
        [SerializeField] private Material scrubGroundMaterial;
        [SerializeField] private Material pathMaterial;
        [SerializeField] private Material meadowMaterial;
        [SerializeField] private Material goldenMeadowMaterial;
        [SerializeField] private Material lavenderMeadowMaterial;
        [SerializeField] private Material trunkMaterial;
        [SerializeField] private Material leafMaterial;
        [SerializeField] private Material warmLeafMaterial;
        [SerializeField] private Material darkLeafMaterial;
        [SerializeField] private Material autumnLeafMaterial;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material oliveGrassMaterial;
        [SerializeField] private Material goldenGrassBladeMaterial;
        [SerializeField] private Material roseGrassMaterial;
        [SerializeField] private Material flowerMaterial;
        [SerializeField] private Material scrubMaterial;
        [SerializeField] private Material rockMaterial;
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private Material cloudShadowCasterMaterial;

        private const int GrassBatchGrid = 8;
        private const int GrassPaletteVariants = 4;
        private const int MaxGrassBatchVertices = 60000;
        private const int MaxGrassTuftVertices = 160;
        private const int MaxMeadowBatchVertices = 60000;
        private const int MaxMeadowPatchVertices = 720;
        private const int GroundDetailTextureSize = 128;
        private const int MaxCombinedRendererVertices = 900000;
        private const float GrassLod0ScreenHeight = 0.095f;
        private const float GrassLod1ScreenHeight = 0.046f;
        private const float GrassLod2ScreenHeight = 0.001f;

        private Transform generatedRoot;
        private Transform meadowBatchRoot;
        private MeadowBatchState greenMeadowBatch;
        private MeadowBatchState goldenMeadowBatch;
        private MeadowBatchState lavenderMeadowBatch;
        private Texture2D groundDetailTexture;
        private Texture2D groundNormalTexture;

        private readonly struct StaticBakeKey : IEquatable<StaticBakeKey>
        {
            public readonly Material Material;
            public readonly UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode;
            public readonly bool ReceiveShadows;

            public StaticBakeKey(Material material, UnityEngine.Rendering.ShadowCastingMode shadowCastingMode, bool receiveShadows)
            {
                Material = material;
                ShadowCastingMode = shadowCastingMode;
                ReceiveShadows = receiveShadows;
            }

            public bool Equals(StaticBakeKey other)
            {
                return Material == other.Material
                    && ShadowCastingMode == other.ShadowCastingMode
                    && ReceiveShadows == other.ReceiveShadows;
            }

            public override bool Equals(object obj)
            {
                return obj is StaticBakeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Material != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Material) : 0;
                    hash = (hash * 397) ^ (int)ShadowCastingMode;
                    hash = (hash * 397) ^ ReceiveShadows.GetHashCode();
                    return hash;
                }
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                return;
            }

            generatedRoot = transform.Find("Generated Valendia Landscape");
            if (generatedRoot != null)
            {
                EnsureGeneratedLandscapeComplete();
            }
        }

        private sealed class MeadowBatchState
        {
            public readonly string Prefix;
            public readonly Material Material;
            public readonly List<Vector3>[] Vertices;
            public readonly List<int>[] Triangles;
            public readonly int[] Indexes;

            public MeadowBatchState(string prefix, Material material, int chunkCount)
            {
                Prefix = prefix;
                Material = material;
                Vertices = new List<Vector3>[chunkCount];
                Triangles = new List<int>[chunkCount];
                Indexes = new int[chunkCount];
            }
        }

        private float WorldSize => chunksPerAxis * chunkSize;
        private Vector2 WorldMin => new Vector2(-WorldSize * 0.5f, -WorldSize * 0.5f);
        public float WorldHalfSize => WorldSize * 0.5f;
        private bool IsPlayableOptimized => qualityProfile == GenerationQualityProfile.PlayableOptimized;
        private int EffectiveGrassTuftCount => IsPlayableOptimized ? Mathf.RoundToInt(grassTuftCount * 0.90f) : grassTuftCount;
        private int EffectiveMeadowPatchCount => meadowPatchCount;
        private int EffectiveFlowerPatchCount => IsPlayableOptimized ? Mathf.RoundToInt(flowerPatchCount * 0.82f) : flowerPatchCount;

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (generatedRoot == null)
            {
                generatedRoot = transform.Find("Generated Valendia Landscape");
            }

            if (generatedRoot != null)
            {
                EnsureMaterials();
                ApplyAtmosphere();
                EnsureGeneratedLandscapeComplete();
                return;
            }

            if (generateOnStart)
            {
                Generate();
            }
        }

        [ContextMenu("Generate Valendia Landscape")]
        public void Generate()
        {
            Clear();
            EnsureMaterials();
            ApplyAtmosphere();

            generatedRoot = new GameObject("Generated Valendia Landscape").transform;
            generatedRoot.SetParent(transform, false);

            BeginMeadowBatches();
            GenerateTerrainChunks();
            GenerateOuterMountainFootholdTerrain();
            GenerateSmoothPathRibbon();
            ScatterMeadowPatches();
            GenerateDistantSpires();
            ScatterRocks();
            ScatterTrees();
            GenerateAuthoredGroves();
            GenerateForestPockets();
            GenerateFlowerRibbons();
            ScatterGrass();
            ScatterFlowerPatches();
            ScatterPathEdgeVegetation();
            FlushAllMeadowBatches();
            ScatterMountainScrub();
            ScatterBorderVegetation();
            GenerateHorizonCloudBelt();
            GenerateCloudBanks();
            GenerateClouds();
            OptimizeGeneratedRenderers();
        }

        [ContextMenu("Clear Generated Landscape")]
        public void Clear()
        {
            Transform existing = transform.Find("Generated Valendia Landscape");
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }

            generatedRoot = null;
        }

        [ContextMenu("Ensure Generated Landscape Complete")]
        public void EnsureGeneratedLandscapeComplete()
        {
            if (generatedRoot == null)
            {
                generatedRoot = transform.Find("Generated Valendia Landscape");
            }

            if (generatedRoot == null)
            {
                return;
            }

            EnsureMaterials();

            if (generatedRoot.Find("Encircling Limestone Mountains") == null)
            {
                GenerateDistantSpires();
            }

            if (generatedRoot.Find("Border Vegetation Fill") == null)
            {
                ScatterBorderVegetation();
            }

            if (generatedRoot.Find("Baked Static Renderers") == null)
            {
                OptimizeGeneratedRenderers();
            }
        }

        public Vector3 GetPathPoint(float normalizedZ, float heightOffset = 0f)
        {
            normalizedZ = Mathf.Clamp01(normalizedZ);
            float z = Mathf.Lerp(WorldMin.y, -WorldMin.y, normalizedZ);
            float x = PathCenterX(z);
            return new Vector3(x, HeightAt(x, z) + heightOffset, z);
        }

        public float SampleHeight(float x, float z)
        {
            return HeightAt(x, z);
        }

        private enum Biome
        {
            ValleyGrass,
            AutumnGrove,
            GoldenGrass,
            LavenderField,
            MountainScrub
        }

        private enum GenerationQualityProfile
        {
            HighVisual,
            PlayableOptimized
        }
    }
}
