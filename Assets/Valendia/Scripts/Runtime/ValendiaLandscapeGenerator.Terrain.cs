using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
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
                    mesh.subMeshCount = biomeTriangles.Length;
                    for (int i = 0; i < biomeTriangles.Length; i++)
                    {
                        mesh.SetTriangles(biomeTriangles[i], i);
                    }

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
                        scrubGroundMaterial
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

        private void GenerateOuterMountainFootholdTerrain()
        {
            Transform parent = CreateContainer("Outer Mountain Foothold Terrain");
            float halfWorld = WorldSize * 0.5f;
            float outer = halfWorld + chunkSize * 0.62f;
            int alongQuads = Mathf.Max(24, verticesPerChunk);
            int depthQuads = Mathf.Max(8, verticesPerChunk / 4);

            CreateFootholdTerrainPatch(parent, "North Foothold Terrain", -halfWorld, halfWorld, halfWorld, outer, alongQuads, depthQuads);
            CreateFootholdTerrainPatch(parent, "South Foothold Terrain", -halfWorld, halfWorld, -outer, -halfWorld, alongQuads, depthQuads);
            CreateFootholdTerrainPatch(parent, "West Foothold Terrain", -outer, -halfWorld, -halfWorld, halfWorld, depthQuads, alongQuads);
            CreateFootholdTerrainPatch(parent, "East Foothold Terrain", halfWorld, outer, -halfWorld, halfWorld, depthQuads, alongQuads);

            CreateFootholdTerrainPatch(parent, "North West Foothold Terrain", -outer, -halfWorld, halfWorld, outer, depthQuads, depthQuads);
            CreateFootholdTerrainPatch(parent, "North East Foothold Terrain", halfWorld, outer, halfWorld, outer, depthQuads, depthQuads);
            CreateFootholdTerrainPatch(parent, "South West Foothold Terrain", -outer, -halfWorld, -outer, -halfWorld, depthQuads, depthQuads);
            CreateFootholdTerrainPatch(parent, "South East Foothold Terrain", halfWorld, outer, -outer, -halfWorld, depthQuads, depthQuads);
        }

        private void CreateFootholdTerrainPatch(
            Transform parent,
            string patchName,
            float xMin,
            float xMax,
            float zMin,
            float zMax,
            int xQuads,
            int zQuads)
        {
            int xVerts = xQuads + 1;
            int zVerts = zQuads + 1;
            Vector3[] vertices = new Vector3[xVerts * zVerts];
            Vector2[] uv = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[xQuads * zQuads * 6];

            for (int z = 0; z < zVerts; z++)
            {
                float tZ = z / (float)zQuads;
                float worldZ = Mathf.Lerp(zMin, zMax, tZ);
                for (int x = 0; x < xVerts; x++)
                {
                    float tX = x / (float)xQuads;
                    float worldX = Mathf.Lerp(xMin, xMax, tX);
                    int index = z * xVerts + x;
                    Biome biome = GroundBiomeAt(worldX, worldZ);
                    vertices[index] = new Vector3(worldX, HeightAt(worldX, worldZ), worldZ);
                    uv[index] = new Vector2(worldX / WorldSize, worldZ / WorldSize);
                    colors[index] = GroundVertexColorAt(worldX, worldZ, biome);
                }
            }

            int triangle = 0;
            for (int z = 0; z < zQuads; z++)
            {
                for (int x = 0; x < xQuads; x++)
                {
                    int i = z * xVerts + x;
                    triangles[triangle++] = i;
                    triangles[triangle++] = i + xVerts;
                    triangles[triangle++] = i + 1;
                    triangles[triangle++] = i + 1;
                    triangles[triangle++] = i + xVerts;
                    triangles[triangle++] = i + xVerts + 1;
                }
            }

            Mesh mesh = new Mesh { name = $"{patchName} mesh" };
            mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            GameObject patch = CreateMeshObject(patchName, mesh, scrubGroundMaterial, parent);
            patch.isStatic = true;
            MeshCollider collider = patch.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            MeshRenderer renderer = patch.GetComponent<MeshRenderer>();
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
