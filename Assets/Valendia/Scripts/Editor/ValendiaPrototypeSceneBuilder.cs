using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valendia.Runtime;

namespace Valendia.Editor
{
    public static class ValendiaPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Valendia/ValendiaPrototype.unity";
        private const string BootstrapScenePath = "Assets/Valendia/ValendiaBootstrap.unity";
        private const string GeneratedBuildScenePath = "Assets/Valendia/Generated/ValendiaBootstrapBuild.unity";
        private const string PreviewPath = "Assets/Valendia/Docs/ValendiaPrototypePreview.png";
        private const string WindowsBuildPath = "Builds/Windows/Valendia.exe";
        private const string MaterialFolder = "Assets/Valendia/Materials";
        private static readonly string[] AuthoredTreePrefabPaths =
        {
            "Assets/Valendia/Art/Environment/Trees/Exports/FBX/tree_reference_oak_broad_01.fbx",
            "Assets/Valendia/Art/Environment/Trees/Exports/FBX/tree_reference_oak_tall_01.fbx",
            "Assets/Valendia/Art/Environment/Trees/Exports/FBX/tree_reference_oak_core_01.fbx",
            "Assets/Valendia/Art/Environment/Trees/Exports/FBX/tree_reference_oak_low_01.fbx",
            "Assets/Valendia/Art/Environment/Trees/Exports/FBX/tree_reference_oak_slim_01.fbx"
        };

        [MenuItem("Valendia/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ValendiaLandscapeGenerator generator = CreateWorld();
            CreateLighting();
            CreatePlayer(generator);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = generator.gameObject;
            EditorGUIUtility.PingObject(generator.gameObject);
        }

        [MenuItem("Valendia/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            CreateBootstrapSceneAsset();
        }

        [MenuItem("Valendia/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            CreateBootstrapSceneAsset();
            string buildScenePath = CreateGeneratedBuildSceneAsset();

            string absoluteBuildPath = Path.GetFullPath(WindowsBuildPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteBuildPath));

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { buildScenePath },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Valendia Windows build: {summary.result}, {summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0.0}s, {WindowsBuildPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException($"Valendia Windows build failed: {summary.result}");
            }
        }

        [MenuItem("Valendia/Create Prototype Preview")]
        public static void CreatePrototypePreview()
        {
            CreatePrototypeScene();

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("Valendia preview skipped: no Main Camera found.");
                return;
            }

            ValendiaLandscapeGenerator generator = Object.FindAnyObjectByType<ValendiaLandscapeGenerator>();
            if (generator != null)
            {
                Vector3 cameraPoint = generator.GetPathPoint(0.26f, 6.4f);
                Vector3 lookPoint = generator.GetPathPoint(0.72f, 7.2f) + new Vector3(22f, 0f, 0f);
                camera.transform.position = cameraPoint;
                camera.transform.rotation = Quaternion.LookRotation(lookPoint - cameraPoint, Vector3.up);
            }
            else
            {
                camera.transform.position = new Vector3(-78f, 34f, -205f);
                camera.transform.rotation = Quaternion.Euler(10f, 24f, 0f);
            }

            camera.fieldOfView = 68f;
            camera.farClipPlane = 1800f;

            RenderTexture renderTexture = new RenderTexture(1280, 720, 24);
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;

            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            camera.targetTexture = null;
            RenderTexture.active = previous;

            string absolutePath = Path.GetFullPath(PreviewPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(PreviewPath);
            Debug.Log($"Valendia preview written to {PreviewPath}");
        }

        [MenuItem("Valendia/Create Diagnostic Previews")]
        public static void CreateDiagnosticPreviews()
        {
            CreatePrototypeScene();

            Camera camera = Camera.main;
            ValendiaLandscapeGenerator generator = Object.FindAnyObjectByType<ValendiaLandscapeGenerator>();
            if (camera == null || generator == null)
            {
                Debug.LogWarning("Valendia diagnostic previews skipped: missing camera or generator.");
                return;
            }

            string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
            RenderPreview(
                camera,
                generator.GetPathPoint(0.26f, 6.4f),
                generator.GetPathPoint(0.72f, 7.2f) + new Vector3(22f, 0f, 0f),
                $"Assets/Valendia/Docs/ValendiaPreview_path_{stamp}.png");
            RenderPreview(
                camera,
                new Vector3(-WorldHalf(generator) * 0.72f, generator.SampleHeight(-WorldHalf(generator) * 0.72f, 0f) + 88f, -WorldHalf(generator) * 0.18f),
                Vector3.zero + Vector3.up * 28f,
                $"Assets/Valendia/Docs/ValendiaPreview_overview_{stamp}.png");
            RenderPreview(
                camera,
                new Vector3(WorldHalf(generator) * 0.18f, generator.SampleHeight(WorldHalf(generator) * 0.18f, WorldHalf(generator) * 0.62f) + 34f, WorldHalf(generator) * 0.62f),
                new Vector3(0f, 36f, 0f),
                $"Assets/Valendia/Docs/ValendiaPreview_clouds_{stamp}.png");
        }

        [MenuItem("Valendia/Benchmark Generation")]
        public static void BenchmarkGeneration()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject world = new GameObject("Valendia World Benchmark");
            ValendiaLandscapeGenerator generator = world.AddComponent<ValendiaLandscapeGenerator>();
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            generator.Generate();
            stopwatch.Stop();

            int rendererCount = Object.FindObjectsByType<MeshRenderer>().Length;
            int lodGroupCount = Object.FindObjectsByType<LODGroup>().Length;
            int meshColliderCount = Object.FindObjectsByType<MeshCollider>().Length;
            int boxColliderCount = Object.FindObjectsByType<BoxCollider>().Length;
            int capsuleColliderCount = Object.FindObjectsByType<CapsuleCollider>().Length;

            Debug.Log(
                $"Valendia generation benchmark: {stopwatch.Elapsed.TotalSeconds:0.00}s, " +
                $"{rendererCount} MeshRenderers, {lodGroupCount} LODGroups, " +
                $"{meshColliderCount} MeshColliders, {boxColliderCount} BoxColliders, {capsuleColliderCount} CapsuleColliders.");
        }

        private static float WorldHalf(ValendiaLandscapeGenerator generator)
        {
            return generator.WorldHalfSize;
        }

        private static void RenderPreview(Camera camera, Vector3 position, Vector3 lookAt, string path)
        {
            camera.transform.position = position;
            camera.transform.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            camera.fieldOfView = 68f;
            camera.farClipPlane = 1800f;

            RenderTexture renderTexture = new RenderTexture(1280, 720, 24);
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;

            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            camera.targetTexture = null;
            RenderTexture.active = previous;

            string absolutePath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            Debug.Log($"Valendia diagnostic preview written to {path}");
        }

        private static ValendiaLandscapeGenerator CreateWorld()
        {
            GameObject world = new GameObject("Valendia World");
            ValendiaLandscapeGenerator generator = world.AddComponent<ValendiaLandscapeGenerator>();
            AssignAuthoredTreePrefabs(generator);
            generator.Generate();
            return generator;
        }

        private static ValendiaLandscapeGenerator CreateBootstrapWorld()
        {
            return CreateBootstrapWorld(false);
        }

        private static ValendiaLandscapeGenerator CreateBootstrapWorld(bool generateLandscape)
        {
            GameObject world = new GameObject("Valendia World");
            ValendiaLandscapeGenerator generator = world.AddComponent<ValendiaLandscapeGenerator>();
            AssignRuntimeMaterials(generator);
            AssignAuthoredTreePrefabs(generator);

            if (generateLandscape)
            {
                generator.Generate();
                SetGenerateOnStart(generator, false);
            }
            else
            {
                SetGenerateOnStart(generator, true);
            }

            return generator;
        }

        private static ValendiaLandscapeGenerator CreateBootstrapSceneAsset()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ValendiaLandscapeGenerator generator = CreateBootstrapWorld();
            CreateLighting();
            CreatePlayer(generator);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            AssetDatabase.ImportAsset(BootstrapScenePath);
            Selection.activeGameObject = generator.gameObject;
            EditorGUIUtility.PingObject(generator.gameObject);
            Debug.Log($"Valendia bootstrap scene written to {BootstrapScenePath}");
            return generator;
        }

        private static string CreateGeneratedBuildSceneAsset()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(GeneratedBuildScenePath)));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ValendiaLandscapeGenerator generator = CreateBootstrapWorld(true);
            CreateLighting();
            CreatePlayer(generator);

            EditorSceneManager.SaveScene(scene, GeneratedBuildScenePath);
            AssetDatabase.ImportAsset(GeneratedBuildScenePath);
            Debug.Log($"Valendia generated build scene written to {GeneratedBuildScenePath}");
            return GeneratedBuildScenePath;
        }

        private static void AssignRuntimeMaterials(ValendiaLandscapeGenerator generator)
        {
            EnsureMaterialFolder();
            SerializedObject serializedGenerator = new SerializedObject(generator);
            SetMaterial(serializedGenerator, "skyboxMaterial", EnsureSkyboxMaterial("Valendia Painted Cyan Sky"));
            SetMaterial(serializedGenerator, "groundMaterial", EnsureLitMaterial("Valendia Fresh Valley Ground", new Color(0.30f, 0.56f, 0.38f), 0.18f, true));
            SetMaterial(serializedGenerator, "autumnGroundMaterial", EnsureLitMaterial("Valendia Autumn Grove Ground", new Color(0.42f, 0.48f, 0.30f), 0.18f, true));
            SetMaterial(serializedGenerator, "goldenGrassGroundMaterial", EnsureLitMaterial("Valendia Golden Grass Ground", new Color(0.48f, 0.54f, 0.32f), 0.18f, true));
            SetMaterial(serializedGenerator, "lavenderGroundMaterial", EnsureLitMaterial("Valendia Lavender Field Ground", new Color(0.34f, 0.52f, 0.42f), 0.18f, true));
            SetMaterial(serializedGenerator, "scrubGroundMaterial", EnsureLitMaterial("Valendia Mountain Scrub Ground", new Color(0.32f, 0.42f, 0.30f), 0.2f, true));
            SetMaterial(serializedGenerator, "pathMaterial", EnsureLitMaterial("Valendia Warm Dust Path", new Color(0.56f, 0.39f, 0.22f), 0.28f, true));
            SetMaterial(serializedGenerator, "meadowMaterial", EnsureLitMaterial("Valendia Meadow Brush", new Color(0.22f, 0.50f, 0.30f), 0.16f, true));
            SetMaterial(serializedGenerator, "goldenMeadowMaterial", EnsureLitMaterial("Valendia Golden Meadow Brush", new Color(0.50f, 0.50f, 0.24f), 0.16f, true));
            SetMaterial(serializedGenerator, "lavenderMeadowMaterial", EnsureLitMaterial("Valendia Lavender Meadow Brush", new Color(0.58f, 0.38f, 0.58f), 0.16f, true));
            SetMaterial(serializedGenerator, "trunkMaterial", EnsureLitMaterial("Valendia Faceted Trunk", new Color(0.26f, 0.14f, 0.09f), 0.32f, false));
            SetMaterial(serializedGenerator, "leafMaterial", EnsureLitMaterial("Valendia Spring Leaf Crowns", new Color(0.20f, 0.49f, 0.25f), 0.28f, true));
            SetMaterial(serializedGenerator, "warmLeafMaterial", EnsureLitMaterial("Valendia Warm Leaf Crowns", new Color(0.72f, 0.50f, 0.22f), 0.28f, true));
            SetMaterial(serializedGenerator, "darkLeafMaterial", EnsureLitMaterial("Valendia Deep Green Crowns", new Color(0.12f, 0.27f, 0.12f), 0.3f, true));
            SetMaterial(serializedGenerator, "autumnLeafMaterial", EnsureLitMaterial("Valendia Autumn Leaf Crowns", new Color(0.63f, 0.38f, 0.18f), 0.28f, true));
            SetMaterial(serializedGenerator, "grassMaterial", EnsureLitMaterial("Valendia Fresh Green Grass Blades", new Color(0.34f, 0.62f, 0.34f), 0.2f, true));
            SetMaterial(serializedGenerator, "oliveGrassMaterial", EnsureLitMaterial("Valendia Olive Grass Blades", new Color(0.28f, 0.44f, 0.25f), 0.22f, true));
            SetMaterial(serializedGenerator, "goldenGrassBladeMaterial", EnsureLitMaterial("Valendia Golden Straw Grass Blades", new Color(0.72f, 0.60f, 0.25f), 0.18f, true));
            SetMaterial(serializedGenerator, "roseGrassMaterial", EnsureLitMaterial("Valendia Rose Heather Grass Blades", new Color(0.72f, 0.40f, 0.62f), 0.16f, true));
            SetMaterial(serializedGenerator, "flowerMaterial", EnsureLitMaterial("Valendia Lavender Blossoms", new Color(0.92f, 0.36f, 0.72f), 0.12f, true));
            SetMaterial(serializedGenerator, "scrubMaterial", EnsureLitMaterial("Valendia Mountain Scrub", new Color(0.29f, 0.39f, 0.25f), 0.24f, false));
            SetMaterial(serializedGenerator, "rockMaterial", EnsureLitMaterial("Valendia Warm Limestone", new Color(0.72f, 0.62f, 0.44f), 0.38f, false));
            SetMaterial(serializedGenerator, "cloudMaterial", EnsureUnlitMaterial("Valendia Soft Autumn Cloud", new Color(0.96f, 0.88f, 0.66f), true));
            SetMaterial(serializedGenerator, "cloudShadowCasterMaterial", EnsureLitMaterial("Valendia Cloud Shadow Caster", new Color(0.88f, 0.82f, 0.68f), 0f, false));
            serializedGenerator.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/Valendia", "Materials");
            }
        }

        private static void SetMaterial(SerializedObject serializedObject, string propertyName, Material material)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue = material;
        }

        private static void AssignAuthoredTreePrefabs(ValendiaLandscapeGenerator generator)
        {
            SerializedObject serializedGenerator = new SerializedObject(generator);
            SerializedProperty prefabs = serializedGenerator.FindProperty("authoredTreePrefabs");
            prefabs.arraySize = AuthoredTreePrefabPaths.Length;

            for (int i = 0; i < AuthoredTreePrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuthoredTreePrefabPaths[i]);
                if (prefab == null)
                {
                    throw new System.InvalidOperationException($"Missing authored tree prefab: {AuthoredTreePrefabPaths[i]}");
                }

                prefabs.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
            }

            serializedGenerator.FindProperty("generateAuthoredTreePrefabs").boolValue = true;
            serializedGenerator.FindProperty("generateLegacyProceduralTrees").boolValue = false;
            serializedGenerator.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetGenerateOnStart(ValendiaLandscapeGenerator generator, bool generateOnStart)
        {
            SerializedObject serializedGenerator = new SerializedObject(generator);
            serializedGenerator.FindProperty("generateOnStart").boolValue = generateOnStart;
            serializedGenerator.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material EnsureLitMaterial(string materialName, Color color, float smoothness, bool doubleSided)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material material = EnsureMaterialAsset(materialName, shader);
            ConfigureMaterialColor(material, color, smoothness);
            if (doubleSided)
            {
                ConfigureDoubleSidedMaterial(material);
            }

            return material;
        }

        private static Material EnsureUnlitMaterial(string materialName, Color color, bool doubleSided)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            Material material = EnsureMaterialAsset(materialName, shader);
            ConfigureMaterialColor(material, color, 0f);
            if (doubleSided)
            {
                ConfigureDoubleSidedMaterial(material);
            }

            return material;
        }

        private static Material EnsureSkyboxMaterial(string materialName)
        {
            Material material = EnsureMaterialAsset(materialName, Shader.Find("Skybox/Procedural"));
            if (material.HasProperty("_SkyTint")) material.SetColor("_SkyTint", new Color(0.12f, 0.55f, 0.40f));
            if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor", new Color(0.52f, 0.48f, 0.32f));
            if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 0.72f);
            if (material.HasProperty("_AtmosphereThickness")) material.SetFloat("_AtmosphereThickness", 0.80f);
            if (material.HasProperty("_SunSize")) material.SetFloat("_SunSize", 0.05f);
            if (material.HasProperty("_SunSizeConvergence")) material.SetFloat("_SunSizeConvergence", 5f);
            return material;
        }

        private static Material EnsureMaterialAsset(string materialName, Shader shader)
        {
            if (shader == null)
            {
                throw new System.InvalidOperationException($"Missing shader for Valendia material '{materialName}'.");
            }

            string path = $"{MaterialFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureMaterialColor(Material material, Color color, float smoothness)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        }

        private static void ConfigureDoubleSidedMaterial(Material material)
        {
            material.doubleSidedGI = true;
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            if (material.HasProperty("_CullMode")) material.SetFloat("_CullMode", 0f);
            if (material.HasProperty("_DoubleSidedEnable")) material.SetFloat("_DoubleSidedEnable", 1f);
        }

        private static void CreateLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.52f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.39f, 0.32f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.11f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.45f, 0.58f, 0.48f);
            RenderSettings.fogDensity = 0.00052f;

            GameObject sun = GameObject.Find("Valendia Sun");
            if (sun == null)
            {
                sun = new GameObject("Valendia Sun");
            }

            Light light = sun.GetComponent<Light>();
            if (light == null)
            {
                light = sun.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.color = new Color(1f, 0.72f, 0.45f);
            light.intensity = 1.10f;
            light.shadows = LightShadows.Hard;
            light.shadowStrength = 0.58f;
            sun.transform.rotation = Quaternion.Euler(24f, -42f, 0f);
            RenderSettings.sun = light;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 900f;
            QualitySettings.shadowCascades = 4;
        }

        private static void CreatePlayer(ValendiaLandscapeGenerator generator)
        {
            Vector3 spawn = FindScenicPathPoint(generator, 1.1f);

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Valendia Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            Object.DestroyImmediate(player.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(player.GetComponent<MeshFilter>());
            player.transform.position = spawn;
            player.transform.rotation = Quaternion.Euler(0f, 12f, 0f);

            CharacterController character = player.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.34f;
            character.center = new Vector3(0f, 0.9f, 0f);

            ValendiaFirstPersonController controller = player.AddComponent<ValendiaFirstPersonController>();
            player.AddComponent<ValendiaFpsDisplay>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 1600f;

            AudioListener listener = cameraObject.AddComponent<AudioListener>();
            listener.enabled = true;

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("cameraRoot").objectReferenceValue = cameraObject.transform;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 FindScenicPathPoint(ValendiaLandscapeGenerator generator, float heightOffset)
        {
            Vector3 best = generator.GetPathPoint(0.42f, heightOffset);
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i <= 28; i++)
            {
                float t = Mathf.Lerp(0.18f, 0.78f, i / 28f);
                Vector3 point = generator.GetPathPoint(t, heightOffset);
                float centerBias = Mathf.Abs(t - 0.46f) * 30f;
                float score = point.y + centerBias;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = point;
                }
            }

            return best;
        }
    }
}
