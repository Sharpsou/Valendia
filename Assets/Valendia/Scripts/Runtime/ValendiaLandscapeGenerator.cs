using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    [ExecuteAlways]
    public sealed class ValendiaLandscapeGenerator : MonoBehaviour
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
        private const float FullGrassDetailPathDistance = 72f;
        private const float ReducedGrassDetailPathDistance = 210f;
        private const float DetailColliderPathDistance = 180f;

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
                    int hash = Material != null ? Material.GetInstanceID() : 0;
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
        private bool IsPlayableOptimized => qualityProfile == GenerationQualityProfile.PlayableOptimized;
        private int EffectiveGrassTuftCount => IsPlayableOptimized ? Mathf.RoundToInt(grassTuftCount * 0.86f) : grassTuftCount;
        private int EffectiveMeadowPatchCount => IsPlayableOptimized ? Mathf.RoundToInt(meadowPatchCount * 0.88f) : meadowPatchCount;
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

        private void GenerateTerrainChunks()
        {
            int quads = Mathf.Max(2, verticesPerChunk);
            int verts = quads + 1;
            Vector2 min = WorldMin;

            for (int cz = 0; cz < chunksPerAxis; cz++)
            {
                for (int cx = 0; cx < chunksPerAxis; cx++)
                {
                    float originX = min.x + cx * chunkSize;
                    float originZ = min.y + cz * chunkSize;
                    Mesh mesh = new Mesh { name = $"Valendia terrain chunk {cx}-{cz}" };

                    Vector3[] vertices = new Vector3[verts * verts];
                    Vector2[] uv = new Vector2[vertices.Length];
                    Color[] colors = new Color[vertices.Length];
                    List<int>[] biomeTriangles = CreateBiomeTriangleLists(quads);
                    List<int> pathTriangles = new List<int>(quads * quads);

                    for (int z = 0; z < verts; z++)
                    {
                        for (int x = 0; x < verts; x++)
                        {
                            int index = z * verts + x;
                            float worldX = originX + x / (float)quads * chunkSize;
                            float worldZ = originZ + z / (float)quads * chunkSize;
                            float height = HeightAt(worldX, worldZ);
                            Biome biome = GroundBiomeAt(worldX, worldZ);

                            vertices[index] = new Vector3(worldX, height, worldZ);
                            uv[index] = new Vector2(worldX / WorldSize, worldZ / WorldSize);
                            colors[index] = GroundVertexColorAt(worldX, worldZ, biome);
                        }
                    }

                    for (int z = 0; z < quads; z++)
                    {
                        for (int x = 0; x < quads; x++)
                        {
                            int i = z * verts + x;
                            float centerX = originX + (x + 0.5f) / quads * chunkSize;
                            float centerZ = originZ + (z + 0.5f) / quads * chunkSize;
                            List<int> target = biomeTriangles[(int)GroundBiomeAt(centerX, centerZ)];

                            target.Add(i);
                            target.Add(i + verts);
                            target.Add(i + 1);
                            target.Add(i + 1);
                            target.Add(i + verts);
                            target.Add(i + verts + 1);
                        }
                    }

                    mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                    mesh.vertices = vertices;
                    mesh.uv = uv;
                    mesh.colors = colors;
                    mesh.subMeshCount = biomeTriangles.Length + 1;
                    for (int i = 0; i < biomeTriangles.Length; i++)
                    {
                        mesh.SetTriangles(biomeTriangles[i], i);
                    }

                    mesh.SetTriangles(pathTriangles, biomeTriangles.Length);
                    mesh.RecalculateNormals();
                    mesh.RecalculateTangents();
                    mesh.RecalculateBounds();

                    GameObject chunk = new GameObject($"Terrain Chunk {cx}-{cz}");
                    chunk.transform.SetParent(generatedRoot, false);
                    MeshFilter filter = chunk.AddComponent<MeshFilter>();
                    MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
                    MeshCollider collider = chunk.AddComponent<MeshCollider>();

                    filter.sharedMesh = mesh;
                    renderer.sharedMaterial = groundMaterial;
                    collider.sharedMesh = mesh;
                    renderer.sharedMaterials = new[]
                    {
                        groundMaterial,
                        autumnGroundMaterial,
                        goldenGrassGroundMaterial,
                        lavenderGroundMaterial,
                        scrubGroundMaterial,
                        pathMaterial
                    };
                }
            }
        }

        private static List<int>[] CreateBiomeTriangleLists(int quads)
        {
            int capacity = quads * quads;
            return new[]
            {
                new List<int>(capacity),
                new List<int>(capacity),
                new List<int>(capacity),
                new List<int>(capacity),
                new List<int>(capacity)
            };
        }

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

        private bool ShouldUseDetailCollider(Vector3 position)
        {
            if (!IsPlayableOptimized)
            {
                return true;
            }

            return Mathf.Abs(position.x - PathCenterX(position.z)) <= DetailColliderPathDistance;
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

        private GameObject CreateTree(Transform parent, Vector3 position, System.Random random, Biome biome, float coniferChanceOverride = -1f)
        {
            GameObject tree = new GameObject("Stylized Faceted Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            float scale = Mathf.Lerp(0.82f, 1.36f, (float)random.NextDouble());
            tree.transform.localScale = Vector3.one * scale;

            float trunkHeight = Mathf.Lerp(2.15f, 3.15f, (float)random.NextDouble());
            GameObject trunk = CreateMeshObject(
                "Faceted Trunk",
                CreateTaperedCylinderMesh(0.27f, 0.16f, trunkHeight, 7, random),
                trunkMaterial,
                tree.transform);
            trunk.transform.localRotation = Quaternion.Euler(
                Mathf.Lerp(-2f, 2f, (float)random.NextDouble()),
                0f,
                Mathf.Lerp(-3f, 3f, (float)random.NextDouble()));
            if (ShouldUseDetailCollider(position))
            {
                AddTreeTrunkCollider(tree, trunkHeight);
            }

            Material crownMaterial = LeafMaterialForBiome(biome, random);
            float coniferChance = coniferChanceOverride >= 0f
                ? coniferChanceOverride
                : biome == Biome.MountainScrub ? 0.72f : 0.06f;
            bool conifer = random.NextDouble() < coniferChance;
            if (conifer)
            {
                CreateConiferCanopy(tree.transform, crownMaterial, trunkHeight, random);
            }
            else
            {
                CreateBroadCanopy(tree.transform, crownMaterial, trunkHeight, random);
            }

            return tree;
        }

        private Material LeafMaterialForBiome(Biome biome, System.Random random)
        {
            switch (biome)
            {
                case Biome.AutumnGrove:
                    return random.NextDouble() < 0.68 ? autumnLeafMaterial : warmLeafMaterial;
                case Biome.GoldenGrass:
                    return random.NextDouble() < 0.45 ? warmLeafMaterial : leafMaterial;
                case Biome.MountainScrub:
                    return darkLeafMaterial;
                default:
                    return random.NextDouble() < 0.2 ? warmLeafMaterial : leafMaterial;
            }
        }

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

            AddOrganicMeadowPatchGeometry(batch.Vertices[chunk], batch.Triangles[chunk], center, biome, random, sizeMultiplier, GrassDetailAt(center));
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
            GameObject batchObject = CreateMeshObject($"{batch.Prefix} {chunk:00}-{batch.Indexes[chunk]:00}", mesh, batch.Material, parent);
            batchObject.isStatic = true;
            ConfigureGrassRenderer(batchObject.GetComponent<MeshRenderer>());

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
            float sizeMultiplier,
            float detail)
        {
            float radiusX = Mathf.Lerp(1.7f, 5.4f, (float)random.NextDouble()) * sizeMultiplier;
            float radiusZ = Mathf.Lerp(0.55f, 1.8f, (float)random.NextDouble()) * sizeMultiplier;
            float rotation = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
            float biomeDensity = biome == Biome.LavenderField ? 1.05f : biome == Biome.GoldenGrass ? 0.72f : 0.86f;
            int clumps = Mathf.RoundToInt(Mathf.Lerp(7f, 16f, (float)random.NextDouble()) * Mathf.Clamp(sizeMultiplier, 0.75f, 1.9f) * biomeDensity * Mathf.Lerp(0.58f, 1f, detail));

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
                blades = Mathf.Max(2, Mathf.RoundToInt(blades * Mathf.Lerp(0.62f, 1f, detail)));
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

            AddGrassTuftGeometry(vertices, triangles, position, random, GrassDetailAt(position));
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

            GameObject batch = CreateMeshObject($"{objectPrefix} {batchIndex:00}", mesh, material, parent);
            batch.isStatic = true;
            ConfigureGrassRenderer(batch.GetComponent<MeshRenderer>());

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
            ConfigureGrassRenderer(CreateMeshObject("Lavender Stems", stemMesh, grassMaterial, patch.transform).GetComponent<MeshRenderer>());

            Mesh blossomMesh = new Mesh { name = "Lavender blossoms mesh" };
            blossomMesh.SetVertices(blossomVertices);
            blossomMesh.SetTriangles(blossomTriangles, 0);
            blossomMesh.RecalculateNormals();
            blossomMesh.RecalculateBounds();
            ConfigureSoftVegetation(CreateMeshObject("Lavender Blossoms", blossomMesh, flowerMaterial, patch.transform).GetComponent<MeshRenderer>());
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

        private void CreateRock(Transform parent, Vector3 position, System.Random random)
        {
            GameObject rock = CreateMeshObject(
                "Faceted Limestone Rock",
                CreateFacetedBlobMesh(random, 0.42f),
                rockMaterial,
                parent);
            rock.transform.position = position;
            rock.transform.rotation = Quaternion.Euler(
                (float)random.NextDouble() * 25f,
                (float)random.NextDouble() * 360f,
                (float)random.NextDouble() * 25f);
            rock.transform.localScale = new Vector3(
                Mathf.Lerp(1.1f, 2.9f, (float)random.NextDouble()),
                Mathf.Lerp(0.24f, 0.72f, (float)random.NextDouble()),
                Mathf.Lerp(0.9f, 2.5f, (float)random.NextDouble()));
            rock.isStatic = true;
            if (ShouldUseDetailCollider(position))
            {
                AddApproximateBoxCollider(rock, new Vector3(0f, 0.02f, 0f), new Vector3(1.35f, 1.15f, 1.35f));
            }
        }

        private void CreateBroadCanopy(Transform tree, Material crownMaterial, float trunkHeight, System.Random random)
        {
            GameObject underside = CreateMeshObject(
                "Solid Leaf Underside",
                CreateLeafCushionMesh(random, 0.08f),
                crownMaterial,
                tree);
            ConfigureLeafRenderer(underside.GetComponent<MeshRenderer>());
            underside.transform.localPosition = new Vector3(0f, trunkHeight - 0.28f, 0f);
            underside.transform.localScale = new Vector3(1.22f, 0.46f, 1.22f);

            GameObject belly = CreateMeshObject(
                "Leaf Belly Volume",
                CreateLeafCushionMesh(random, 0.14f),
                crownMaterial,
                tree);
            ConfigureLeafRenderer(belly.GetComponent<MeshRenderer>());
            belly.transform.localPosition = new Vector3(0f, trunkHeight + 0.06f, 0f);
            belly.transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 360f, (float)random.NextDouble()), 0f);
            belly.transform.localScale = new Vector3(1.42f, 1.08f, 1.42f);

            int lobes = random.Next(4, 7);
            for (int i = 0; i < lobes; i++)
            {
                float angle = Mathf.PI * 2f * i / lobes + Mathf.Lerp(-0.35f, 0.35f, (float)random.NextDouble());
                float offset = i == 0 ? 0f : Mathf.Lerp(0.45f, 1.15f, (float)random.NextDouble());
                GameObject lobe = CreateMeshObject(
                    i == 0 ? "Main Leaf Crown" : "Leaf Crown Lobe",
                    CreateLeafCushionMesh(random, 0.18f),
                    crownMaterial,
                    tree);
                ConfigureLeafRenderer(lobe.GetComponent<MeshRenderer>());
                float vertical = trunkHeight + Mathf.Lerp(0.08f, 1.02f, (float)random.NextDouble());
                lobe.transform.localPosition = new Vector3(Mathf.Cos(angle) * offset, vertical, Mathf.Sin(angle) * offset);
                lobe.transform.localRotation = Quaternion.Euler(
                    Mathf.Lerp(-8f, 8f, (float)random.NextDouble()),
                    Mathf.Lerp(0f, 360f, (float)random.NextDouble()),
                    Mathf.Lerp(-8f, 8f, (float)random.NextDouble()));
                lobe.transform.localScale = new Vector3(
                    Mathf.Lerp(1.05f, 1.62f, (float)random.NextDouble()),
                    Mathf.Lerp(1.02f, 1.55f, (float)random.NextDouble()),
                    Mathf.Lerp(1.05f, 1.62f, (float)random.NextDouble()));
            }

            int branches = random.Next(3, 6);
            for (int i = 0; i < branches; i++)
            {
                float angle = Mathf.PI * 2f * i / branches + Mathf.Lerp(-0.4f, 0.4f, (float)random.NextDouble());
                GameObject branch = CreateMeshObject(
                    "Simple Branch",
                    CreateTaperedCylinderMesh(0.075f, 0.032f, Mathf.Lerp(0.75f, 1.22f, (float)random.NextDouble()), 5, random),
                    trunkMaterial,
                    tree);
                branch.transform.localPosition = new Vector3(0f, trunkHeight * Mathf.Lerp(0.64f, 0.86f, (float)random.NextDouble()), 0f);
                branch.transform.localRotation = Quaternion.Euler(Mathf.Lerp(54f, 70f, (float)random.NextDouble()), angle * Mathf.Rad2Deg, Mathf.Lerp(-6f, 6f, (float)random.NextDouble()));
            }
        }

        private void CreateConiferCanopy(Transform tree, Material crownMaterial, float trunkHeight, System.Random random)
        {
            int tiers = random.Next(3, 5);
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)Mathf.Max(1, tiers - 1);
                GameObject tier = CreateMeshObject(
                    "Faceted Conifer Tier",
                    CreateConeMesh(Mathf.Lerp(1.15f, 0.45f, t), Mathf.Lerp(1.05f, 1.42f, (float)random.NextDouble()), 7),
                    crownMaterial,
                    tree);
                ConfigureLeafRenderer(tier.GetComponent<MeshRenderer>());
                tier.transform.localPosition = new Vector3(0f, trunkHeight * 0.43f + i * 0.66f, 0f);
                tier.transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 360f, (float)random.NextDouble()), 0f);
            }
        }

        private void CreateCloud(Transform parent, Vector3 position, System.Random random)
        {
            GameObject cloud = new GameObject("Soft Low Poly Cloud");
            cloud.transform.SetParent(parent, false);
            cloud.transform.position = position;
            cloud.transform.rotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 360f, (float)random.NextDouble()), 0f);
            float cloudScale = Mathf.Lerp(12f, 20f, (float)random.NextDouble());
            cloud.transform.localScale = Vector3.one * cloudScale;

            int blobs = random.Next(12, 20);
            for (int i = 0; i < blobs; i++)
            {
                float layer = i / (float)Mathf.Max(1, blobs - 1);
                float verticalMass = Mathf.Sin(layer * Mathf.PI);
                GameObject blob = CreateMeshObject(
                    "Cloud Puff",
                    CreateFacetedBlobMesh(random, 0.10f),
                    cloudMaterial,
                    cloud.transform);
                blob.transform.localPosition = new Vector3(
                    Mathf.Lerp(-1.85f, 1.85f, (float)random.NextDouble()),
                    Mathf.Lerp(-0.20f, 0.50f, (float)random.NextDouble()) + verticalMass * 0.28f,
                    Mathf.Lerp(-0.86f, 0.86f, (float)random.NextDouble()));
                blob.transform.localScale = new Vector3(
                    Mathf.Lerp(1.0f, 2.15f, (float)random.NextDouble()),
                    Mathf.Lerp(0.42f, 0.86f, (float)random.NextDouble()),
                    Mathf.Lerp(0.74f, 1.42f, (float)random.NextDouble()));

                MeshRenderer renderer = blob.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = false;
                CreateCloudShadowCaster(blob);
            }
        }

        private void CreateCloudBank(Transform parent, Vector3 position, System.Random random)
        {
            GameObject bank = new GameObject("Wide Painted Cloud Bank");
            bank.transform.SetParent(parent, false);
            bank.transform.position = position;
            bank.transform.rotation = Quaternion.Euler(0f, Mathf.Lerp(-12f, 12f, (float)random.NextDouble()), 0f);
            float bankScale = Mathf.Lerp(8.5f, 14f, (float)random.NextDouble());
            bank.transform.localScale = Vector3.one * bankScale;

            int puffs = random.Next(12, 20);
            for (int i = 0; i < puffs; i++)
            {
                float t = puffs <= 1 ? 0.5f : i / (float)(puffs - 1);
                float row = i % 3 - 1f;
                float crown = Mathf.Sin(t * Mathf.PI);
                GameObject puff = CreateMeshObject(
                    "Cloud Bank Puff",
                    CreateFacetedBlobMesh(random, 0.075f),
                    cloudMaterial,
                    bank.transform);
                puff.transform.localPosition = new Vector3(
                    Mathf.Lerp(-2.45f, 2.45f, t) + Mathf.Lerp(-0.70f, 0.70f, (float)random.NextDouble()),
                    crown * Mathf.Lerp(0.20f, 0.72f, (float)random.NextDouble()) + Mathf.Lerp(-0.10f, 0.18f, (float)random.NextDouble()),
                    row * Mathf.Lerp(0.38f, 0.68f, (float)random.NextDouble()) + Mathf.Lerp(-0.24f, 0.24f, (float)random.NextDouble()));
                puff.transform.localScale = new Vector3(
                    Mathf.Lerp(1.0f, 1.85f, (float)random.NextDouble()),
                    Mathf.Lerp(0.38f, 0.86f, (float)random.NextDouble()),
                    Mathf.Lerp(0.70f, 1.28f, (float)random.NextDouble()));

                MeshRenderer renderer = puff.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = false;
                CreateCloudShadowCaster(puff);
            }
        }

        private void CreateCloudShadowCaster(GameObject visibleCloudPart)
        {
            MeshFilter sourceFilter = visibleCloudPart.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                return;
            }

            GameObject caster = CreateMeshObject(
                "Cloud Shadow Caster",
                sourceFilter.sharedMesh,
                cloudShadowCasterMaterial,
                visibleCloudPart.transform.parent);
            caster.transform.localPosition = visibleCloudPart.transform.localPosition;
            caster.transform.localRotation = visibleCloudPart.transform.localRotation;
            caster.transform.localScale = visibleCloudPart.transform.localScale;
            MeshRenderer renderer = caster.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = false;
            caster.isStatic = true;
        }

        private static GameObject CreateMeshObject(string objectName, Mesh mesh, Material material, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            return gameObject;
        }

        private static void ConfigureSoftVegetation(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private static void ConfigureGrassRenderer(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private static void ConfigureLeafRenderer(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = false;
        }

        private void OptimizeGeneratedRenderers()
        {
            if (generatedRoot == null)
            {
                return;
            }

            MeshRenderer[] renderers = generatedRoot.GetComponentsInChildren<MeshRenderer>(true);
            Dictionary<StaticBakeKey, List<MeshRenderer>> groups = new Dictionary<StaticBakeKey, List<MeshRenderer>>();

            foreach (MeshRenderer renderer in renderers)
            {
                if (!CanBakeRenderer(renderer, out MeshFilter filter))
                {
                    continue;
                }

                StaticBakeKey key = new StaticBakeKey(renderer.sharedMaterial, renderer.shadowCastingMode, renderer.receiveShadows);
                if (!groups.TryGetValue(key, out List<MeshRenderer> group))
                {
                    group = new List<MeshRenderer>(128);
                    groups.Add(key, group);
                }

                group.Add(renderer);
            }

            if (groups.Count == 0)
            {
                return;
            }

            Transform bakeRoot = CreateContainer("Baked Static Renderers");
            foreach (KeyValuePair<StaticBakeKey, List<MeshRenderer>> group in groups)
            {
                BakeRendererGroup(bakeRoot, group.Key, group.Value);
            }

            PruneEmptyGeneratedChildren(generatedRoot);
        }

        private bool CanBakeRenderer(MeshRenderer renderer, out MeshFilter filter)
        {
            filter = null;
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return false;
            }

            filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0)
            {
                return false;
            }

            GameObject gameObject = renderer.gameObject;
            if (gameObject.GetComponent<MeshCollider>() != null)
            {
                return false;
            }

            string name = gameObject.name;
            return !name.Contains("Smooth Dust Path")
                && !name.StartsWith("Terrain Chunk", StringComparison.Ordinal);
        }

        private void BakeRendererGroup(Transform bakeRoot, StaticBakeKey key, List<MeshRenderer> renderers)
        {
            List<CombineInstance> combine = new List<CombineInstance>(256);
            int vertexCount = 0;
            int batchIndex = 0;
            Matrix4x4 rootMatrix = generatedRoot.worldToLocalMatrix;

            foreach (MeshRenderer renderer in renderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                int meshVertices = filter.sharedMesh.vertexCount;
                if (combine.Count > 0 && vertexCount + meshVertices > MaxCombinedRendererVertices)
                {
                    FlushCombinedRendererBatch(bakeRoot, key, combine, batchIndex++);
                    combine.Clear();
                    vertexCount = 0;
                }

                combine.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    subMeshIndex = 0,
                    transform = rootMatrix * renderer.transform.localToWorldMatrix
                });
                vertexCount += meshVertices;

                StripBakedRenderer(renderer, filter);
            }

            FlushCombinedRendererBatch(bakeRoot, key, combine, batchIndex);
        }

        private void FlushCombinedRendererBatch(Transform bakeRoot, StaticBakeKey key, List<CombineInstance> combine, int batchIndex)
        {
            if (combine.Count == 0)
            {
                return;
            }

            Mesh mesh = new Mesh { name = $"{key.Material.name} baked mesh {batchIndex:00}" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine.ToArray(), true, true, false);
            mesh.RecalculateBounds();

            GameObject batch = CreateMeshObject($"{key.Material.name} Baked Renderers {batchIndex:00}", mesh, key.Material, bakeRoot);
            batch.isStatic = true;
            MeshRenderer renderer = batch.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = key.ShadowCastingMode;
            renderer.receiveShadows = key.ReceiveShadows;
        }

        private static void StripBakedRenderer(MeshRenderer renderer, MeshFilter filter)
        {
            DestroyUnityObject(renderer);
            DestroyUnityObject(filter);
        }

        private static void PruneEmptyGeneratedChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                PruneEmptyGeneratedChildren(child);

                if (child.childCount == 0 && child.GetComponents<Component>().Length == 1)
                {
                    DestroyUnityObject(child.gameObject);
                }
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void AddApproximateBoxCollider(GameObject gameObject, Vector3 center, Vector3 size)
        {
            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            collider.center = center;
            collider.size = size;
        }

        private static void AddTreeTrunkCollider(GameObject tree, float trunkHeight)
        {
            CapsuleCollider collider = tree.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = tree.AddComponent<CapsuleCollider>();
            }

            collider.direction = 1;
            collider.center = new Vector3(0f, trunkHeight * 0.48f, 0f);
            collider.height = Mathf.Max(1.2f, trunkHeight * 0.96f);
            collider.radius = 0.34f;
        }

        private static Mesh CreateTaperedCylinderMesh(float bottomRadius, float topRadius, float height, int sides, System.Random random)
        {
            List<Vector3> vertices = new List<Vector3>(sides * 2 + 2);
            List<int> triangles = new List<int>(sides * 12);
            float wobbleX = Mathf.Lerp(-0.12f, 0.12f, (float)random.NextDouble());
            float wobbleZ = Mathf.Lerp(-0.12f, 0.12f, (float)random.NextDouble());

            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * bottomRadius, 0f, Mathf.Sin(angle) * bottomRadius));
                vertices.Add(new Vector3(Mathf.Cos(angle) * topRadius + wobbleX, height, Mathf.Sin(angle) * topRadius + wobbleZ));
            }

            int bottomCenter = vertices.Count;
            vertices.Add(Vector3.zero);
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(wobbleX, height, wobbleZ));

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int bottomA = i * 2;
                int topA = bottomA + 1;
                int bottomB = next * 2;
                int topB = bottomB + 1;

                triangles.Add(bottomA);
                triangles.Add(topA);
                triangles.Add(bottomB);
                triangles.Add(bottomB);
                triangles.Add(topA);
                triangles.Add(topB);

                triangles.Add(bottomCenter);
                triangles.Add(bottomB);
                triangles.Add(bottomA);

                triangles.Add(topCenter);
                triangles.Add(topA);
                triangles.Add(topB);
            }

            return CreateFlatMesh("Faceted tapered cylinder", vertices, triangles);
        }

        private static Mesh CreateConeMesh(float radius, float height, int sides)
        {
            List<Vector3> vertices = new List<Vector3>(sides + 2);
            List<int> triangles = new List<int>(sides * 6);
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            int top = vertices.Count;
            vertices.Add(new Vector3(0f, height, 0f));
            int center = vertices.Count;
            vertices.Add(Vector3.zero);

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles.Add(i);
                triangles.Add(top);
                triangles.Add(next);

                triangles.Add(center);
                triangles.Add(next);
                triangles.Add(i);
            }

            return CreateFlatMesh("Faceted cone", vertices, triangles);
        }

        private static Mesh CreateLayeredMassifMesh(float width, float height, float depth, System.Random random)
        {
            List<Vector3> vertices = new List<Vector3>(18);
            List<int> triangles = new List<int>(72);
            int layers = 3;

            for (int layer = 0; layer < layers; layer++)
            {
                float t = layer / (float)(layers - 1);
                float y = height * t;
                float layerWidth = Mathf.Lerp(width, width * 0.35f, t) * Mathf.Lerp(0.85f, 1.15f, (float)random.NextDouble());
                float layerDepth = Mathf.Lerp(depth, depth * 0.28f, t) * Mathf.Lerp(0.85f, 1.15f, (float)random.NextDouble());
                float skew = Mathf.Lerp(-width * 0.12f, width * 0.12f, (float)random.NextDouble()) * t;
                vertices.Add(new Vector3(-layerWidth * 0.5f + skew, y, -layerDepth * 0.5f));
                vertices.Add(new Vector3(layerWidth * 0.5f + skew, y, -layerDepth * 0.42f));
                vertices.Add(new Vector3(layerWidth * 0.42f + skew, y, layerDepth * 0.5f));
                vertices.Add(new Vector3(-layerWidth * 0.45f + skew, y, layerDepth * 0.42f));
            }

            for (int layer = 0; layer < layers - 1; layer++)
            {
                int a = layer * 4;
                int b = (layer + 1) * 4;
                for (int i = 0; i < 4; i++)
                {
                    int next = (i + 1) % 4;
                    triangles.Add(a + i);
                    triangles.Add(b + i);
                    triangles.Add(a + next);
                    triangles.Add(a + next);
                    triangles.Add(b + i);
                    triangles.Add(b + next);
                }
            }

            triangles.Add(8);
            triangles.Add(9);
            triangles.Add(10);
            triangles.Add(8);
            triangles.Add(10);
            triangles.Add(11);

            triangles.Add(0);
            triangles.Add(2);
            triangles.Add(1);
            triangles.Add(0);
            triangles.Add(3);
            triangles.Add(2);

            return CreateFlatMesh("Layered limestone massif mesh", vertices, triangles);
        }

        private static Mesh CreateFacetedBlobMesh(System.Random random, float irregularity)
        {
            const int segments = 8;
            float[] ringY = { 0.55f, 0.05f, -0.45f };
            float[] ringRadius = { 0.68f, 1f, 0.72f };
            List<Vector3> vertices = new List<Vector3>(segments * ringY.Length + 2);
            List<int> triangles = new List<int>(segments * 18);

            int top = vertices.Count;
            vertices.Add(new Vector3(0f, 0.95f, 0f));

            for (int ring = 0; ring < ringY.Length; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = Mathf.PI * 2f * i / segments;
                    float jitter = Mathf.Lerp(1f - irregularity, 1f + irregularity, (float)random.NextDouble());
                    vertices.Add(new Vector3(Mathf.Cos(angle) * ringRadius[ring] * jitter, ringY[ring], Mathf.Sin(angle) * ringRadius[ring] * jitter));
                }
            }

            int bottom = vertices.Count;
            vertices.Add(new Vector3(0f, -0.82f, 0f));

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(top);
                triangles.Add(1 + i);
                triangles.Add(1 + next);
            }

            for (int ring = 0; ring < ringY.Length - 1; ring++)
            {
                int current = 1 + ring * segments;
                int nextRing = current + segments;
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    triangles.Add(current + i);
                    triangles.Add(nextRing + i);
                    triangles.Add(current + next);
                    triangles.Add(current + next);
                    triangles.Add(nextRing + i);
                    triangles.Add(nextRing + next);
                }
            }

            int lastRing = 1 + (ringY.Length - 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(lastRing + next);
                triangles.Add(lastRing + i);
                triangles.Add(bottom);
            }

            return CreateCenteredFlatMesh("Faceted blob mesh", vertices, triangles);
        }

        private static Mesh CreateLeafCushionMesh(System.Random random, float irregularity)
        {
            const int segments = 9;
            float[] ringY = { 0.46f, 0.15f, -0.12f, -0.32f };
            float[] ringRadius = { 0.68f, 1f, 0.9f, 0.46f };
            List<Vector3> vertices = new List<Vector3>(segments * ringY.Length + 2);
            List<int> triangles = new List<int>(segments * 24);

            int top = vertices.Count;
            vertices.Add(new Vector3(0f, 0.74f, 0f));

            for (int ring = 0; ring < ringY.Length; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = Mathf.PI * 2f * i / segments;
                    float jitter = Mathf.Lerp(1f - irregularity, 1f + irregularity, (float)random.NextDouble());
                    vertices.Add(new Vector3(Mathf.Cos(angle) * ringRadius[ring] * jitter, ringY[ring], Mathf.Sin(angle) * ringRadius[ring] * jitter));
                }
            }

            int bottom = vertices.Count;
            vertices.Add(new Vector3(0f, -0.40f, 0f));

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(top);
                triangles.Add(1 + i);
                triangles.Add(1 + next);
            }

            for (int ring = 0; ring < ringY.Length - 1; ring++)
            {
                int current = 1 + ring * segments;
                int nextRing = current + segments;
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    triangles.Add(current + i);
                    triangles.Add(nextRing + i);
                    triangles.Add(current + next);
                    triangles.Add(current + next);
                    triangles.Add(nextRing + i);
                    triangles.Add(nextRing + next);
                }
            }

            int lastRing = 1 + (ringY.Length - 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(lastRing + next);
                triangles.Add(lastRing + i);
                triangles.Add(bottom);
            }

            return CreateCenteredFlatMesh("Leaf cushion mesh", vertices, triangles);
        }

        private static Mesh CreateFlatMesh(string meshName, List<Vector3> sourceVertices, List<int> sourceTriangles)
        {
            Vector3[] vertices = new Vector3[sourceTriangles.Count];
            int[] triangles = new int[sourceTriangles.Count];
            for (int i = 0; i < sourceTriangles.Count; i++)
            {
                vertices[i] = sourceVertices[sourceTriangles[i]];
                triangles[i] = i;
            }

            Mesh mesh = new Mesh { name = meshName };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCenteredFlatMesh(string meshName, List<Vector3> sourceVertices, List<int> sourceTriangles)
        {
            Vector3[] vertices = new Vector3[sourceTriangles.Count];
            int[] triangles = new int[sourceTriangles.Count];

            for (int i = 0; i < sourceTriangles.Count; i += 3)
            {
                Vector3 a = sourceVertices[sourceTriangles[i]];
                Vector3 b = sourceVertices[sourceTriangles[i + 1]];
                Vector3 c = sourceVertices[sourceTriangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Vector3 center = (a + b + c) / 3f;

                if (Vector3.Dot(normal, center) < 0f)
                {
                    Vector3 swap = b;
                    b = c;
                    c = swap;
                }

                vertices[i] = a;
                vertices[i + 1] = b;
                vertices[i + 2] = c;
                triangles[i] = i;
                triangles[i + 1] = i + 1;
                triangles[i + 2] = i + 2;
            }

            Mesh mesh = new Mesh { name = meshName };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddGrassBlade(List<Vector3> vertices, List<int> triangles, Vector3 basePoint, float angle, float height, float width, float lean)
        {
            Vector3 side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * width;
            Vector3 forward = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * lean;
            Vector3 p0 = basePoint - side;
            Vector3 p1 = basePoint + Vector3.up * height + forward;
            Vector3 p2 = basePoint + side;
            AddSingleTriangle(vertices, triangles, p0, p1, p2);
        }

        private float GrassDetailAt(Vector3 position)
        {
            if (!IsPlayableOptimized)
            {
                return 1f;
            }

            float pathDistance = Mathf.Abs(position.x - PathCenterX(position.z));
            float distanceFade = 1f - Mathf.SmoothStep(FullGrassDetailPathDistance, ReducedGrassDetailPathDistance, pathDistance);
            float radial = new Vector2(position.x, position.z).magnitude / Mathf.Max(1f, WorldSize * 0.5f);
            float borderBoost = Mathf.SmoothStep(0.78f, 0.98f, radial) * 0.14f;
            return Mathf.Clamp(Mathf.Lerp(0.44f, 1f, distanceFade) + borderBoost, 0.44f, 1f);
        }

        private static void AddGrassTuftGeometry(List<Vector3> vertices, List<int> triangles, Vector3 position, System.Random random, float detail)
        {
            int blades = Mathf.Max(4, Mathf.RoundToInt(random.Next(7, 13) * Mathf.Clamp01(detail)));
            float yaw = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());

            for (int i = 0; i < blades; i++)
            {
                float radius = Mathf.Lerp(0.025f, 0.52f, (float)random.NextDouble());
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                Vector3 localBase = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 basePoint = position + RotateYaw(localBase, yaw);
                float bladeAngle = yaw + angle + Mathf.Lerp(-0.55f, 0.55f, (float)random.NextDouble());
                float height = Mathf.Lerp(0.15f, 0.50f, (float)random.NextDouble()) * Mathf.Lerp(0.88f, 1f, detail);
                float width = Mathf.Lerp(0.03f, 0.085f, (float)random.NextDouble());
                AddGrassBlade(vertices, triangles, basePoint, bladeAngle, height, width, Mathf.Lerp(0.025f, 0.10f, (float)random.NextDouble()));
            }
        }

        private static Vector3 RotateYaw(Vector3 value, float yaw)
        {
            float cos = Mathf.Cos(yaw);
            float sin = Mathf.Sin(yaw);
            return new Vector3(value.x * cos - value.z * sin, value.y, value.x * sin + value.z * cos);
        }

        private static void AddFlowerBlossom(List<Vector3> vertices, List<int> triangles, Vector3 center, float angle, float size)
        {
            Vector3 side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * size;
            Vector3 forward = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * size;
            Vector3 up = Vector3.up * size * 1.2f;
            AddDoubleSidedTriangle(vertices, triangles, center + up, center + side, center - up);
            AddDoubleSidedTriangle(vertices, triangles, center + up, center - side, center - up);
            AddDoubleSidedTriangle(vertices, triangles, center + up, center + forward, center - up);
            AddDoubleSidedTriangle(vertices, triangles, center + up, center - forward, center - up);
        }

        private static void AddDoubleSidedTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);

            index = vertices.Count;
            vertices.Add(c);
            vertices.Add(b);
            vertices.Add(a);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        private static void AddSingleTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

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
