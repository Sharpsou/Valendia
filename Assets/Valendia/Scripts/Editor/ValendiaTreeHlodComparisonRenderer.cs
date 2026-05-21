using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Valendia.Editor
{
    public static class ValendiaTreeHlodComparisonRenderer
    {
        private const string SourcePrefabPath = "Assets/Valendia/Art/Environment/Trees/OptimizedFBX/tree_reference_oak_broad_01_optimized.fbx";
        private const string HlodPrefabPath = "Assets/Valendia/Art/Environment/Trees/HlodFBX/tree_reference_oak_broad_01_hlod.fbx";
        private const string OutputPath = "Logs/tree_hlod_comparison_unity.png";

        public static void RenderComparison()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            GameObject hlodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HlodPrefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException($"Missing tree prefab at {SourcePrefabPath}");
            }
            if (hlodPrefab == null)
            {
                throw new FileNotFoundException($"Missing tree HLOD prefab at {HlodPrefabPath}");
            }

            GameObject normal = Object.Instantiate(prefab, new Vector3(-2.8f, 0f, 0f), Quaternion.identity);
            normal.name = "Normal tree";
            normal.transform.localScale = Vector3.one * 1.55f;

            GameObject low = new GameObject("Volume matched HLOD tree");
            low.transform.position = new Vector3(2.8f, 0f, 0f);
            low.transform.localScale = Vector3.one * 1.55f;
            BuildLowTree(hlodPrefab, low.transform);

            CreateGround();
            CreateLighting();
            Camera camera = CreateCamera();

            RenderTexture rt = new RenderTexture(1600, 900, 24);
            Texture2D tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            camera.Render();
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            camera.targetTexture = null;
            RenderTexture.active = previous;

            string absolutePath = Path.GetFullPath(OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, tex.EncodeToPNG());

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log($"Valendia HLOD comparison written to {absolutePath}");
        }

        private static void BuildLowTree(GameObject prefab, Transform parent)
        {
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            Dictionary<Material, MeshBatch> batches = new Dictionary<Material, MeshBatch>();

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                Bounds bounds = TransformBounds(renderer.transform.localToWorldMatrix, filter.sharedMesh.bounds);
                Vector3 size = bounds.size;
                Color color = renderer.sharedMaterial.HasProperty("_Color") ? renderer.sharedMaterial.color : Color.white;
                float brightness = color.r + color.g + color.b;
                bool isTrunk = brightness < 1.25f || size.y > Mathf.Max(size.x, size.z) * 1.4f;
                MeshBatch batch = GetBatch(batches, renderer.sharedMaterial);
                if (isTrunk)
                {
                    AddTrunk(batch.Vertices, batch.Triangles, bounds.center, size, 5);
                }
                else
                {
                    AddCanopy(batch.Vertices, batch.Triangles, bounds.center, size, 5);
                }
            }

            int index = 0;
            foreach (KeyValuePair<Material, MeshBatch> entry in batches)
            {
                MeshBatch batch = entry.Value;
                if (batch.Vertices.Count == 0)
                {
                    continue;
                }

                Mesh mesh = new Mesh { name = $"Comparison HLOD {index:00}" };
                mesh.indexFormat = batch.Vertices.Count > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(batch.Vertices);
                mesh.SetTriangles(batch.Triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                GameObject part = new GameObject($"HLOD material batch {index:00}");
                part.transform.SetParent(parent, false);
                MeshFilter filter = part.AddComponent<MeshFilter>();
                MeshRenderer renderer = part.AddComponent<MeshRenderer>();
                filter.sharedMesh = mesh;
                renderer.sharedMaterial = entry.Key;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                index++;
            }
        }

        private static MeshBatch GetBatch(Dictionary<Material, MeshBatch> batches, Material material)
        {
            if (!batches.TryGetValue(material, out MeshBatch batch))
            {
                batch = new MeshBatch();
                batches.Add(material, batch);
            }

            return batch;
        }

        private static void AddCanopy(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size, int sides)
        {
            float radiusX = Mathf.Max(0.08f, size.x * 0.5f);
            float radiusZ = Mathf.Max(0.08f, size.z * 0.5f);
            float halfHeight = Mathf.Max(0.12f, size.y * 0.5f);
            int top = vertices.Count;
            vertices.Add(center + Vector3.up * halfHeight);
            int ring = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                float jitter = 1f + Mathf.Sin(angle * 2.31f) * 0.09f;
                vertices.Add(center + new Vector3(
                    Mathf.Cos(angle) * radiusX * jitter,
                    Mathf.Sin(angle * 1.73f) * halfHeight * 0.14f,
                    Mathf.Sin(angle) * radiusZ * jitter));
            }

            int bottom = vertices.Count;
            vertices.Add(center - Vector3.up * halfHeight);
            for (int i = 0; i < sides; i++)
            {
                int current = ring + i;
                int next = ring + ((i + 1) % sides);
                AddTriangle(triangles, top, current, next);
                AddTriangle(triangles, bottom, next, current);
            }
        }

        private static void AddTrunk(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size, int sides)
        {
            float radiusX = Mathf.Max(0.08f, size.x * 0.5f);
            float radiusZ = Mathf.Max(0.08f, size.z * 0.5f);
            float halfHeight = Mathf.Max(0.12f, size.y * 0.5f);
            int bottom = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                vertices.Add(center + new Vector3(Mathf.Cos(angle) * radiusX, -halfHeight, Mathf.Sin(angle) * radiusZ));
            }

            int top = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                vertices.Add(center + new Vector3(Mathf.Cos(angle) * radiusX * 0.62f, halfHeight, Mathf.Sin(angle) * radiusZ * 0.62f));
            }

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                AddTriangle(triangles, bottom + i, top + i, bottom + next);
                AddTriangle(triangles, bottom + next, top + i, top + next);
            }
        }

        private static void AddTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Bounds transformed = new Bounds(matrix.MultiplyPoint3x4(min), Vector3.zero);
            transformed.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z)));
            transformed.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z)));
            transformed.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z)));
            transformed.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z)));
            transformed.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z)));
            transformed.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z)));
            transformed.Encapsulate(matrix.MultiplyPoint3x4(max));
            return transformed;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Comparison Camera");
            cameraObject.transform.position = new Vector3(0f, 3.2f, -8.5f);
            cameraObject.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.Skybox;
            return camera;
        }

        private static void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.48f);
            GameObject lightObject = new GameObject("Sun");
            lightObject.transform.rotation = Quaternion.Euler(44f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.88f, 0.68f);
            light.intensity = 1.7f;
            light.shadows = LightShadows.Hard;
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Comparison Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.34f, 0.56f, 0.35f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private sealed class MeshBatch
        {
            public readonly List<Vector3> Vertices = new List<Vector3>(256);
            public readonly List<int> Triangles = new List<int>(512);
        }
    }
}
