using System;
using System.Collections;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed class ValendiaTreeHlodCell : MonoBehaviour
    {
        [SerializeField] private float fullNearDistance = 150f;
        [SerializeField] private float fullFarDistance = 210f;
        [SerializeField] private float midNearDistance = 260f;
        [SerializeField] private float midFarDistance = 330f;
        [SerializeField] private Vector3 cellCenter;
        [SerializeField] private float cellRadius = 48f;
        [SerializeField] private MeshRenderer[] impostorRenderers = Array.Empty<MeshRenderer>();
        [SerializeField] private GameObject[] fullPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] midPrefabs = Array.Empty<GameObject>();
        [SerializeField] private int[] prefabIndexes = Array.Empty<int>();
        [SerializeField] private Vector3[] positions = Array.Empty<Vector3>();
        [SerializeField] private float[] yawDegrees = Array.Empty<float>();
        [SerializeField] private float[] scales = Array.Empty<float>();

        private enum TreeCellState
        {
            Full,
            Mid,
            Hlod
        }

        private TreeCellState state = TreeCellState.Full;
        private Transform observer;
        private GameObject activeRoot;

        public void Configure(
            Vector3 center,
            float radius,
            float fullNearSwitchDistance,
            float fullFarSwitchDistance,
            float midNearSwitchDistance,
            float midFarSwitchDistance,
            MeshRenderer[] distantImpostorRenderers,
            GameObject[] treeFullPrefabs,
            GameObject[] treeMidPrefabs,
            int[] treePrefabIndexes,
            Vector3[] treePositions,
            float[] treeYawDegrees,
            float[] treeScales)
        {
            cellCenter = center;
            cellRadius = radius;
            fullNearDistance = fullNearSwitchDistance;
            fullFarDistance = Mathf.Max(fullNearSwitchDistance + 1f, fullFarSwitchDistance);
            midNearDistance = Mathf.Max(fullFarDistance + 1f, midNearSwitchDistance);
            midFarDistance = Mathf.Max(midNearDistance + 1f, midFarSwitchDistance);
            impostorRenderers = distantImpostorRenderers ?? Array.Empty<MeshRenderer>();
            fullPrefabs = treeFullPrefabs ?? Array.Empty<GameObject>();
            midPrefabs = treeMidPrefabs ?? Array.Empty<GameObject>();
            prefabIndexes = treePrefabIndexes ?? Array.Empty<int>();
            positions = treePositions ?? Array.Empty<Vector3>();
            yawDegrees = treeYawDegrees ?? Array.Empty<float>();
            scales = treeScales ?? Array.Empty<float>();
        }

        private IEnumerator Start()
        {
            while (observer == null)
            {
                Camera mainCamera = Camera.main;
                observer = mainCamera != null ? mainCamera.transform : null;
                yield return null;
            }

            UpdateState(true);
            while (enabled)
            {
                UpdateState(false);
                yield return null;
            }
        }

        private void UpdateState(bool force)
        {
            if (observer == null)
            {
                return;
            }

            float distance = Mathf.Max(0f, Vector3.Distance(observer.position, cellCenter) - cellRadius);
            TreeCellState nextState = state;
            if (force)
            {
                nextState = StateForDistance(distance);
            }
            else if (state == TreeCellState.Full && distance > fullFarDistance)
            {
                nextState = TreeCellState.Mid;
            }
            else if (state == TreeCellState.Mid && distance < fullNearDistance)
            {
                nextState = TreeCellState.Full;
            }
            else if (state == TreeCellState.Mid && distance > midFarDistance)
            {
                nextState = TreeCellState.Hlod;
            }
            else if (state == TreeCellState.Hlod && distance < midNearDistance)
            {
                nextState = TreeCellState.Mid;
            }

            if (!force && nextState == state)
            {
                return;
            }

            state = nextState;
            SetTreeRenderers(state);
        }

        private TreeCellState StateForDistance(float distance)
        {
            if (distance <= fullFarDistance)
            {
                return TreeCellState.Full;
            }

            return distance <= midFarDistance ? TreeCellState.Mid : TreeCellState.Hlod;
        }

        private void SetTreeRenderers(TreeCellState nextState)
        {
            if (activeRoot != null)
            {
                Destroy(activeRoot);
                activeRoot = null;
            }

            SetRenderers(impostorRenderers, nextState == TreeCellState.Hlod);
            if (nextState == TreeCellState.Full)
            {
                activeRoot = InstantiateTrees(fullPrefabs, "Runtime Full Tree Renderers", true);
            }
            else if (nextState == TreeCellState.Mid)
            {
                activeRoot = InstantiateTrees(midPrefabs, "Runtime Mid Tree Renderers", false);
            }
        }

        private GameObject InstantiateTrees(GameObject[] sourcePrefabs, string rootName, bool castShadows)
        {
            GameObject root = new GameObject(rootName);
            root.transform.SetParent(transform, false);

            int count = Mathf.Min(positions.Length, Mathf.Min(yawDegrees.Length, Mathf.Min(scales.Length, prefabIndexes.Length)));
            for (int i = 0; i < count; i++)
            {
                int prefabIndex = prefabIndexes[i];
                if (prefabIndex < 0 || prefabIndex >= sourcePrefabs.Length || sourcePrefabs[prefabIndex] == null)
                {
                    continue;
                }

                GameObject tree = Instantiate(sourcePrefabs[prefabIndex], positions[i], Quaternion.Euler(0f, yawDegrees[i], 0f), root.transform);
                tree.name = sourcePrefabs[prefabIndex].name;
                tree.transform.localScale = Vector3.one * scales[i];

                Collider[] colliders = tree.GetComponentsInChildren<Collider>(true);
                for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    Destroy(colliders[colliderIndex]);
                }

                MeshRenderer[] renderers = tree.GetComponentsInChildren<MeshRenderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    renderers[rendererIndex].shadowCastingMode = castShadows
                        ? UnityEngine.Rendering.ShadowCastingMode.On
                        : UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderers[rendererIndex].receiveShadows = true;
                }
            }

            return root;
        }

        private static void SetRenderers(MeshRenderer[] renderers, bool active)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = active;
                }
            }
        }
    }
}
