using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
        private const float TreeHlodCellSize = 96f;
        private const float TreeHlodFullNearDistance = 150f;
        private const float TreeHlodFullFarDistance = 210f;
        private const float TreeHlodMidNearDistance = 260f;
        private const float TreeHlodMidFarDistance = 330f;

        private Transform treeHlodRoot;
        private Dictionary<Vector2Int, TreeHlodBuildCell> treeHlodCells;
        private Dictionary<GameObject, TreeHlodProfile> treeHlodProfiles;

        private sealed class TreeHlodBuildCell
        {
            public readonly Vector2Int Key;
            public readonly Transform Root;
            public readonly Transform CollisionRoot;
            public readonly Transform ImpostorRoot;
            public readonly List<TreeHlodInstance> Instances = new List<TreeHlodInstance>(64);

            public TreeHlodBuildCell(Vector2Int key, Transform root, Transform collisionRoot, Transform impostorRoot)
            {
                Key = key;
                Root = root;
                CollisionRoot = collisionRoot;
                ImpostorRoot = impostorRoot;
            }
        }

        private readonly struct TreeHlodInstance
        {
            public readonly Vector3 Position;
            public readonly float YawDegrees;
            public readonly float Scale;
            public readonly GameObject FullPrefab;
            public readonly GameObject MidPrefab;
            public readonly GameObject HlodPrefab;

            public TreeHlodInstance(GameObject fullPrefab, GameObject midPrefab, GameObject hlodPrefab, Vector3 position, float yawDegrees, float scale)
            {
                FullPrefab = fullPrefab;
                MidPrefab = midPrefab;
                HlodPrefab = hlodPrefab;
                Position = position;
                YawDegrees = yawDegrees;
                Scale = scale;
            }
        }

        private sealed class TreeHlodProfile
        {
            public readonly TreeHlodPart[] Parts;

            public TreeHlodProfile(TreeHlodPart[] parts)
            {
                Parts = parts;
            }
        }

        private readonly struct TreeHlodPart
        {
            public readonly Material Material;
            public readonly Bounds LocalBounds;
            public readonly bool IsTrunk;

            public TreeHlodPart(Material material, Bounds localBounds, bool isTrunk)
            {
                Material = material;
                LocalBounds = localBounds;
                IsTrunk = isTrunk;
            }
        }

        private sealed class TreeHlodMeshBatch
        {
            public readonly Material Material;
            public readonly List<Vector3> Vertices = new List<Vector3>(4096);
            public readonly List<int> Triangles = new List<int>(8192);

            public TreeHlodMeshBatch(Material material)
            {
                Material = material;
            }
        }

        private void BeginTreeHlod()
        {
            treeHlodRoot = CreateContainer("Tree HLOD Cells");
            treeHlodCells = new Dictionary<Vector2Int, TreeHlodBuildCell>(128);
            treeHlodProfiles = new Dictionary<GameObject, TreeHlodProfile>();
        }

        private Transform TreeParentForPoint(Vector3 point)
        {
            if (treeHlodCells == null)
            {
                BeginTreeHlod();
            }

            TreeHlodBuildCell cell = GetTreeHlodCell(point);
            return cell.CollisionRoot;
        }

        private void RegisterTreeHlodInstance(GameObject fullPrefab, GameObject midPrefab, GameObject hlodPrefab, Vector3 point, float yawDegrees, float scale)
        {
            if (treeHlodCells == null || fullPrefab == null || midPrefab == null || hlodPrefab == null)
            {
                return;
            }

            TreeHlodBuildCell cell = GetTreeHlodCell(point);
            cell.Instances.Add(new TreeHlodInstance(fullPrefab, midPrefab, hlodPrefab, point, yawDegrees, scale));
        }

        private TreeHlodBuildCell GetTreeHlodCell(Vector3 point)
        {
            Vector2 min = WorldMin;
            int x = Mathf.FloorToInt((point.x - min.x) / TreeHlodCellSize);
            int z = Mathf.FloorToInt((point.z - min.y) / TreeHlodCellSize);
            Vector2Int key = new Vector2Int(x, z);

            if (treeHlodCells.TryGetValue(key, out TreeHlodBuildCell existing))
            {
                return existing;
            }

            GameObject rootObject = new GameObject($"Tree HLOD Cell {x:00}-{z:00}");
            rootObject.transform.SetParent(treeHlodRoot, false);

            GameObject collisionObject = new GameObject("Tree Colliders");
            collisionObject.transform.SetParent(rootObject.transform, false);

            GameObject impostorObject = new GameObject("Distant Tree Impostors");
            impostorObject.transform.SetParent(rootObject.transform, false);

            TreeHlodBuildCell cell = new TreeHlodBuildCell(key, rootObject.transform, collisionObject.transform, impostorObject.transform);
            treeHlodCells.Add(key, cell);
            return cell;
        }

        private void FinalizeTreeHlod()
        {
            if (treeHlodCells == null)
            {
                return;
            }

            foreach (TreeHlodBuildCell cell in treeHlodCells.Values)
            {
                BuildTreeHlodImpostors(cell);
                ConfigureTreeHlodCell(cell);
            }

            treeHlodCells = null;
            treeHlodProfiles = null;
        }

        private void ConfigureTreeHlodCell(TreeHlodBuildCell cell)
        {
            Bounds bounds = new Bounds(CellCenter(cell.Key), Vector3.one);
            bool hasBounds = false;

            for (int i = 0; i < cell.Instances.Count; i++)
            {
                Vector3 position = cell.Instances[i].Position;
                if (!hasBounds)
                {
                    bounds = new Bounds(position, Vector3.one * 10f);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(position + Vector3.up * 8f);
                    bounds.Encapsulate(position - Vector3.up * 1f);
                }
            }

            Vector3 center = bounds.center;
            float radius = Mathf.Max(TreeHlodCellSize * 0.5f, bounds.extents.magnitude);
            ValendiaTreeHlodCell runtimeCell = cell.Root.gameObject.AddComponent<ValendiaTreeHlodCell>();
            BuildTreeRuntimeArrays(
                cell,
                out GameObject[] fullPrefabs,
                out GameObject[] midPrefabs,
                out int[] prefabIndexes,
                out Vector3[] positions,
                out float[] yawDegrees,
                out float[] scales);
            runtimeCell.Configure(
                center,
                radius,
                TreeHlodFullNearDistance,
                TreeHlodFullFarDistance,
                TreeHlodMidNearDistance,
                TreeHlodMidFarDistance,
                cell.ImpostorRoot.GetComponentsInChildren<MeshRenderer>(true),
                fullPrefabs,
                midPrefabs,
                prefabIndexes,
                positions,
                yawDegrees,
                scales);
        }

        private static void BuildTreeRuntimeArrays(
            TreeHlodBuildCell cell,
            out GameObject[] fullPrefabs,
            out GameObject[] midPrefabs,
            out int[] prefabIndexes,
            out Vector3[] positions,
            out float[] yawDegrees,
            out float[] scales)
        {
            List<GameObject> uniqueFullPrefabs = new List<GameObject>(8);
            List<GameObject> uniqueMidPrefabs = new List<GameObject>(8);
            Dictionary<GameObject, int> prefabLookup = new Dictionary<GameObject, int>();
            int count = cell.Instances.Count;
            prefabIndexes = new int[count];
            positions = new Vector3[count];
            yawDegrees = new float[count];
            scales = new float[count];

            for (int i = 0; i < count; i++)
            {
                TreeHlodInstance instance = cell.Instances[i];
                if (!prefabLookup.TryGetValue(instance.FullPrefab, out int prefabIndex))
                {
                    prefabIndex = uniqueFullPrefabs.Count;
                    uniqueFullPrefabs.Add(instance.FullPrefab);
                    uniqueMidPrefabs.Add(instance.MidPrefab);
                    prefabLookup.Add(instance.FullPrefab, prefabIndex);
                }

                prefabIndexes[i] = prefabIndex;
                positions[i] = instance.Position;
                yawDegrees[i] = instance.YawDegrees;
                scales[i] = instance.Scale;
            }

            fullPrefabs = uniqueFullPrefabs.ToArray();
            midPrefabs = uniqueMidPrefabs.ToArray();
        }

        private Vector3 CellCenter(Vector2Int key)
        {
            Vector2 min = WorldMin;
            return new Vector3(
                min.x + (key.x + 0.5f) * TreeHlodCellSize,
                0f,
                min.y + (key.y + 0.5f) * TreeHlodCellSize);
        }

        private void BuildTreeHlodImpostors(TreeHlodBuildCell cell)
        {
            Dictionary<Material, TreeHlodMeshBatch> batches = new Dictionary<Material, TreeHlodMeshBatch>();
            Vector3 origin = cell.ImpostorRoot.position;

            for (int i = 0; i < cell.Instances.Count; i++)
            {
                TreeHlodInstance instance = cell.Instances[i];
                MeshRenderer[] renderers = instance.HlodPrefab.GetComponentsInChildren<MeshRenderer>(true);
                Matrix4x4 instanceMatrix = Matrix4x4.TRS(
                    instance.Position - origin,
                    Quaternion.Euler(0f, instance.YawDegrees, 0f),
                    Vector3.one * instance.Scale);

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    MeshRenderer renderer = renderers[rendererIndex];
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                    {
                        continue;
                    }

                    Matrix4x4 localMatrix = instance.HlodPrefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    AddTransformedMesh(GetBatch(batches, renderer.sharedMaterial), filter.sharedMesh, instanceMatrix * localMatrix);
                }
            }

            int batchIndex = 0;
            foreach (TreeHlodMeshBatch batch in batches.Values)
            {
                if (batch.Material == null || batch.Vertices.Count == 0)
                {
                    continue;
                }

                Mesh mesh = new Mesh { name = $"Tree HLOD mesh {cell.Key.x:00}-{cell.Key.y:00}-{batchIndex:00}" };
                mesh.indexFormat = batch.Vertices.Count > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(batch.Vertices);
                mesh.SetTriangles(batch.Triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                GameObject meshObject = CreateMeshObject($"Tree HLOD {batchIndex:00}", mesh, batch.Material, cell.ImpostorRoot);
                meshObject.isStatic = false;
                MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                batchIndex++;
            }
        }

        private static TreeHlodMeshBatch GetBatch(Dictionary<Material, TreeHlodMeshBatch> batches, Material material)
        {
            if (material == null)
            {
                return new TreeHlodMeshBatch(null);
            }

            if (!batches.TryGetValue(material, out TreeHlodMeshBatch batch))
            {
                batch = new TreeHlodMeshBatch(material);
                batches.Add(material, batch);
            }

            return batch;
        }

        private static void AddTransformedMesh(TreeHlodMeshBatch batch, Mesh mesh, Matrix4x4 matrix)
        {
            if (batch == null || batch.Material == null || mesh == null)
            {
                return;
            }

            Vector3[] sourceVertices = mesh.vertices;
            int[] sourceTriangles = mesh.GetTriangles(0);
            int offset = batch.Vertices.Count;
            for (int i = 0; i < sourceVertices.Length; i++)
            {
                batch.Vertices.Add(matrix.MultiplyPoint3x4(sourceVertices[i]));
            }

            for (int i = 0; i < sourceTriangles.Length; i++)
            {
                batch.Triangles.Add(offset + sourceTriangles[i]);
            }
        }

        private static void AddTreeHlodPartGeometry(TreeHlodMeshBatch batch, TreeHlodInstance instance, TreeHlodPart part, Vector3 origin)
        {
            if (batch == null || batch.Material == null)
            {
                return;
            }

            float yawRadians = instance.YawDegrees * Mathf.Deg2Rad;
            Vector3 center = instance.Position - origin + RotateYaw(part.LocalBounds.center * instance.Scale, yawRadians);
            Vector3 size = part.LocalBounds.size * instance.Scale;
            float radiusX = Mathf.Max(0.08f, size.x * 0.5f);
            float radiusZ = Mathf.Max(0.08f, size.z * 0.5f);
            float halfHeight = Mathf.Max(0.12f, size.y * 0.5f);

            if (part.IsTrunk)
            {
                AddFacetedTrunkPrism(batch.Vertices, batch.Triangles, center, radiusX, radiusZ, halfHeight, yawRadians, 5);
                return;
            }

            AddFacetedCanopyBlob(batch.Vertices, batch.Triangles, center, radiusX, radiusZ, halfHeight, yawRadians, 5);
        }

        private static void AddFacetedCanopyBlob(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 center,
            float radiusX,
            float radiusZ,
            float halfHeight,
            float yaw,
            int sides)
        {
            sides = Mathf.Clamp(sides, 4, 6);
            int top = vertices.Count;
            vertices.Add(center + Vector3.up * halfHeight);

            int ringStart = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = yaw + Mathf.PI * 2f * i / sides;
                float radialJitter = 1f + Mathf.Sin(angle * 2.31f) * 0.09f;
                float yJitter = Mathf.Sin(angle * 1.73f) * halfHeight * 0.14f;
                vertices.Add(center + new Vector3(
                    Mathf.Cos(angle) * radiusX * radialJitter,
                    yJitter,
                    Mathf.Sin(angle) * radiusZ * radialJitter));
            }

            int bottom = vertices.Count;
            vertices.Add(center - Vector3.up * halfHeight);

            for (int i = 0; i < sides; i++)
            {
                int current = ringStart + i;
                int next = ringStart + ((i + 1) % sides);
                AddTriangle(vertices, triangles, top, current, next);
                AddTriangle(vertices, triangles, bottom, next, current);
            }
        }

        private static void AddFacetedTrunkPrism(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 center,
            float radiusX,
            float radiusZ,
            float halfHeight,
            float yaw,
            int sides)
        {
            sides = Mathf.Clamp(sides, 4, 6);
            int bottomStart = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = yaw + Mathf.PI * 2f * i / sides;
                vertices.Add(center + new Vector3(
                    Mathf.Cos(angle) * radiusX,
                    -halfHeight,
                    Mathf.Sin(angle) * radiusZ));
            }

            int topStart = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = yaw + Mathf.PI * 2f * i / sides;
                vertices.Add(center + new Vector3(
                    Mathf.Cos(angle) * radiusX * 0.62f,
                    halfHeight,
                    Mathf.Sin(angle) * radiusZ * 0.62f));
            }

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                AddTriangle(vertices, triangles, bottomStart + i, topStart + i, bottomStart + next);
                AddTriangle(vertices, triangles, bottomStart + next, topStart + i, topStart + next);
            }
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private static void AddCrossQuad(List<Vector3> vertices, List<int> triangles, Vector3 bottom, Vector3 top, float halfWidth, float yaw, float topWidthScale)
        {
            Vector3 right = RotateYaw(Vector3.right, yaw) * halfWidth;
            Vector3 rightB = right;
            Vector3 rightT = right * topWidthScale;
            AddDoubleSidedQuad(vertices, triangles, bottom - rightB, top - rightT, top + rightT, bottom + rightB);

            Vector3 forward = RotateYaw(Vector3.forward, yaw) * halfWidth;
            Vector3 forwardB = forward;
            Vector3 forwardT = forward * topWidthScale;
            AddDoubleSidedQuad(vertices, triangles, bottom - forwardB, top - forwardT, top + forwardT, bottom + forwardB);
        }

        private static void AddDoubleSidedQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddDoubleSidedTriangle(vertices, triangles, a, b, c);
            AddDoubleSidedTriangle(vertices, triangles, a, c, d);
        }

        private TreeHlodProfile GetTreeHlodProfile(GameObject prefab)
        {
            if (treeHlodProfiles != null && treeHlodProfiles.TryGetValue(prefab, out TreeHlodProfile cached))
            {
                return cached;
            }

            List<TreeHlodPart> parts = new List<TreeHlodPart>(8);
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                Bounds localBounds = TransformBounds(renderer.transform.localToWorldMatrix, filter.sharedMesh.bounds);
                Vector3 size = localBounds.size;
                Color color = renderer.sharedMaterial.HasProperty("_Color") ? renderer.sharedMaterial.color : Color.white;
                float brightness = color.r + color.g + color.b;
                bool isTrunk = brightness < 1.25f || size.y > Mathf.Max(size.x, size.z) * 1.4f;
                parts.Add(new TreeHlodPart(renderer.sharedMaterial, localBounds, isTrunk));
            }

            TreeHlodProfile profile = new TreeHlodProfile(parts.ToArray());
            treeHlodProfiles?.Add(prefab, profile);
            return profile;
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
    }
}
