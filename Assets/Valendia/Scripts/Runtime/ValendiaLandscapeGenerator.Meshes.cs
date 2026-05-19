using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed partial class ValendiaLandscapeGenerator
    {
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

        private static void AddGrassTuftGeometry(List<Vector3> vertices, List<int> triangles, Vector3 position, System.Random random)
        {
            int blades = random.Next(7, 13);
            float yaw = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());

            for (int i = 0; i < blades; i++)
            {
                float radius = Mathf.Lerp(0.025f, 0.52f, (float)random.NextDouble());
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                Vector3 localBase = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 basePoint = position + RotateYaw(localBase, yaw);
                float bladeAngle = yaw + angle + Mathf.Lerp(-0.55f, 0.55f, (float)random.NextDouble());
                float height = Mathf.Lerp(0.16f, 0.50f, (float)random.NextDouble());
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
    }
}
