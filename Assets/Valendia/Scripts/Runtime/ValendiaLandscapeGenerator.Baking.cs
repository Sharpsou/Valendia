using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
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
            if (gameObject.GetComponent<MeshCollider>() != null || gameObject.GetComponentInParent<LODGroup>() != null)
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
    }
}
