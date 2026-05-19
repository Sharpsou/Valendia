using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
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
            AddApproximateBoxCollider(rock, new Vector3(0f, 0.02f, 0f), new Vector3(1.35f, 1.15f, 1.35f));
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

        private static GameObject CreateGrassLodObject(string objectName, Mesh lod0Mesh, Material material, Transform parent)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            root.isStatic = true;

            Mesh lod1Mesh = CreateTriangleLodMesh($"{lod0Mesh.name} LOD1", lod0Mesh, 2);
            Mesh lod2Mesh = CreateTriangleLodMesh($"{lod0Mesh.name} LOD2", lod0Mesh, 4);
            Renderer lod0 = CreateGrassLodRenderer(root.transform, "LOD0", lod0Mesh, material);
            Renderer lod1 = CreateGrassLodRenderer(root.transform, "LOD1", lod1Mesh, material);
            Renderer lod2 = CreateGrassLodRenderer(root.transform, "LOD2", lod2Mesh, material);

            LODGroup lodGroup = root.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(GrassLod0ScreenHeight, new[] { lod0 }),
                new LOD(GrassLod1ScreenHeight, new[] { lod1 }),
                new LOD(GrassLod2ScreenHeight, new[] { lod2 })
            });
            lodGroup.RecalculateBounds();

            return root;
        }

        private static Renderer CreateGrassLodRenderer(Transform parent, string objectName, Mesh mesh, Material material)
        {
            GameObject child = CreateMeshObject(objectName, mesh, material, parent);
            child.isStatic = true;
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            ConfigureGrassRenderer(renderer);
            return renderer;
        }

        private static Mesh CreateTriangleLodMesh(string meshName, Mesh source, int triangleStep)
        {
            int[] sourceTriangles = source.triangles;
            Vector3[] sourceVertices = source.vertices;
            int triangleCount = sourceTriangles.Length / 3;
            int keptTriangles = Mathf.Max(1, Mathf.CeilToInt(triangleCount / (float)Mathf.Max(1, triangleStep)));
            List<Vector3> vertices = new List<Vector3>(keptTriangles * 3);
            List<int> triangles = new List<int>(keptTriangles * 3);

            for (int triangle = 0; triangle < triangleCount; triangle += triangleStep)
            {
                int sourceIndex = triangle * 3;
                int targetIndex = vertices.Count;
                vertices.Add(sourceVertices[sourceTriangles[sourceIndex]]);
                vertices.Add(sourceVertices[sourceTriangles[sourceIndex + 1]]);
                vertices.Add(sourceVertices[sourceTriangles[sourceIndex + 2]]);
                triangles.Add(targetIndex);
                triangles.Add(targetIndex + 1);
                triangles.Add(targetIndex + 2);
            }

            Mesh mesh = new Mesh { name = meshName };
            mesh.indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
