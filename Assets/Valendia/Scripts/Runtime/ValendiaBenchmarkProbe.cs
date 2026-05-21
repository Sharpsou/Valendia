using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Valendia.Runtime
{
    public sealed class ValendiaBenchmarkProbe : MonoBehaviour
    {
        private const string EnableArg = "-valendiaBenchmark";
        private const string ReportDirArg = "-valendiaReportDir";
        private const string ShadowDistanceArg = "-valendiaShadowDistance";
        private const string ShadowCascadesArg = "-valendiaShadowCascades";
        private const int ScreenshotWidth = 1280;
        private const int ScreenshotHeight = 800;

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        private string reportDirectory;
        private Camera benchmarkCamera;
        private ValendiaLandscapeGenerator generator;
        private CharacterController playerController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!HasArgument(args, EnableArg))
            {
                return;
            }

            GameObject probe = new GameObject("Valendia Benchmark Probe");
            DontDestroyOnLoad(probe);
            probe.AddComponent<ValendiaBenchmarkProbe>().reportDirectory = ReadArgumentValue(args, ReportDirArg);
        }

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Screen.SetResolution(ScreenshotWidth, ScreenshotHeight, FullScreenMode.Windowed);

            if (string.IsNullOrWhiteSpace(reportDirectory))
            {
                reportDirectory = Path.Combine(Application.persistentDataPath, "ValendiaBenchmark");
            }

            Directory.CreateDirectory(reportDirectory);

            yield return WaitForSceneReady();
            ApplyQualityOverrides();
            yield return null;
            yield return null;

            BenchmarkResult result = new BenchmarkResult();
            CaptureSceneStats(result);
            result.TreeCollision = TestTreeCollisions();
            result.TerrainCollision = TestTerrainCollisions();

            yield return CaptureView("valley", generator.GetScenicPoint(0.26f, 6.4f), generator.GetScenicPoint(0.72f, 7.2f) + new Vector3(22f, 0f, 0f), result);
            yield return CaptureView("overview", new Vector3(-generator.WorldHalfSize * 0.72f, generator.SampleHeight(-generator.WorldHalfSize * 0.72f, 0f) + 88f, -generator.WorldHalfSize * 0.18f), Vector3.up * 28f, result);
            yield return CaptureView("forest", generator.GetScenicPoint(0.72f, 2.8f) + new Vector3(36f, 2f, -18f), generator.GetScenicPoint(0.80f, 4.8f), result);

            WriteReport(result);
            Application.Quit(0);
        }

        private IEnumerator WaitForSceneReady()
        {
            float deadline = Time.realtimeSinceStartup + 120f;
            while (Time.realtimeSinceStartup < deadline)
            {
                generator = FindAnyObjectByType<ValendiaLandscapeGenerator>();
                benchmarkCamera = Camera.main;
                playerController = FindAnyObjectByType<CharacterController>();

                if (generator != null && benchmarkCamera != null && playerController != null && GameObject.Find("Generated Valendia Landscape") != null)
                {
                    yield break;
                }

                yield return null;
            }

            throw new TimeoutException("Valendia benchmark scene did not become ready in time.");
        }

        private static void ApplyQualityOverrides()
        {
            string[] args = Environment.GetCommandLineArgs();
            string shadowDistance = ReadArgumentValue(args, ShadowDistanceArg);
            if (!string.IsNullOrWhiteSpace(shadowDistance) && float.TryParse(shadowDistance, NumberStyles.Float, InvariantCulture, out float parsedShadowDistance))
            {
                QualitySettings.shadowDistance = parsedShadowDistance;
            }

            string shadowCascades = ReadArgumentValue(args, ShadowCascadesArg);
            if (!string.IsNullOrWhiteSpace(shadowCascades) && int.TryParse(shadowCascades, NumberStyles.Integer, InvariantCulture, out int parsedShadowCascades))
            {
                QualitySettings.shadowCascades = parsedShadowCascades;
            }
        }

        private IEnumerator CaptureView(string viewName, Vector3 position, Vector3 lookAt, BenchmarkResult result)
        {
            benchmarkCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position, Vector3.up));
            benchmarkCamera.fieldOfView = 68f;
            benchmarkCamera.nearClipPlane = 0.03f;
            benchmarkCamera.farClipPlane = 1600f;

            yield return null;
            yield return null;

            RenderTexture renderTexture = new RenderTexture(ScreenshotWidth, ScreenshotHeight, 24);
            RenderTexture previous = RenderTexture.active;
            benchmarkCamera.targetTexture = renderTexture;

            string screenshotPath = Path.Combine(reportDirectory, $"{viewName}.png");
            CaptureScreenshot(renderTexture, screenshotPath);

            for (int i = 0; i < 12; i++)
            {
                benchmarkCamera.Render();
                yield return null;
            }

            const int sampleFrames = 120;
            float minFps = float.PositiveInfinity;
            float maxDelta = 0f;
            System.Diagnostics.Stopwatch totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < sampleFrames; i++)
            {
                System.Diagnostics.Stopwatch frameStopwatch = System.Diagnostics.Stopwatch.StartNew();
                benchmarkCamera.Render();
                frameStopwatch.Stop();
                float delta = (float)frameStopwatch.Elapsed.TotalSeconds;
                if (delta > 0f)
                {
                    minFps = Mathf.Min(minFps, 1f / delta);
                    maxDelta = Mathf.Max(maxDelta, delta);
                }

            }

            totalStopwatch.Stop();
            benchmarkCamera.targetTexture = null;
            RenderTexture.active = previous;
            Destroy(renderTexture);

            float averageFps = sampleFrames / Mathf.Max(0.0001f, (float)totalStopwatch.Elapsed.TotalSeconds);
            result.AppendView(viewName, averageFps, minFps, maxDelta, screenshotPath);
        }

        private void CaptureScreenshot(RenderTexture renderTexture, string path)
        {
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            benchmarkCamera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
        }

        private void CaptureSceneStats(BenchmarkResult result)
        {
            result.MeshRenderers = FindObjectsByType<MeshRenderer>().Length;
            result.LodGroups = FindObjectsByType<LODGroup>().Length;
            result.MeshColliders = FindObjectsByType<MeshCollider>().Length;
            result.BoxColliders = FindObjectsByType<BoxCollider>().Length;
            result.CapsuleColliders = FindObjectsByType<CapsuleCollider>().Length;
            result.EnabledCapsuleColliders = CountEnabledCapsuleColliders();
            result.ShadowDistance = QualitySettings.shadowDistance;
            result.ShadowCascades = QualitySettings.shadowCascades;
        }

        private static int CountEnabledCapsuleColliders()
        {
            int count = 0;
            CapsuleCollider[] colliders = FindObjectsByType<CapsuleCollider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private CollisionCheck TestTreeCollisions()
        {
            CollisionCheck check = new CollisionCheck();
            CapsuleCollider[] colliders = FindObjectsByType<CapsuleCollider>();
            for (int i = 0; i < colliders.Length && check.Tested < 48; i++)
            {
                CapsuleCollider collider = colliders[i];
                if (collider == null || !collider.enabled || collider == playerController)
                {
                    continue;
                }

                Vector3 center = collider.transform.TransformPoint(collider.center);
                Vector3 side = collider.transform.right;
                Vector3 origin = center + side * Mathf.Max(1.2f, collider.radius * collider.transform.lossyScale.x + 0.9f);
                Vector3 direction = (center - origin).normalized;

                check.Tested++;
                RaycastHit[] hits = Physics.RaycastAll(origin, direction, 3.5f, ~0, QueryTriggerInteraction.Ignore);
                if (ContainsCollider(hits, collider))
                {
                    check.Passed++;
                }
            }

            return check;
        }

        private static bool ContainsCollider(RaycastHit[] hits, Collider collider)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == collider)
                {
                    return true;
                }
            }

            return false;
        }

        private CollisionCheck TestTerrainCollisions()
        {
            CollisionCheck check = new CollisionCheck();
            for (int i = 0; i <= 12; i++)
            {
                float t = Mathf.Lerp(0.12f, 0.88f, i / 12f);
                Vector3 point = generator.GetScenicPoint(t, 80f);
                check.Tested++;
                if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, 160f, ~0, QueryTriggerInteraction.Ignore) && hit.collider is MeshCollider)
                {
                    check.Passed++;
                }
            }

            return check;
        }

        private void WriteReport(BenchmarkResult result)
        {
            string path = Path.Combine(reportDirectory, "report.json");
            File.WriteAllText(path, result.ToJson(), Encoding.UTF8);
            Debug.Log($"Valendia benchmark report written to {path}");
        }

        private static bool HasArgument(string[] args, string argument)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], argument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadArgumentValue(string[] args, string argument)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argument, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private sealed class CollisionCheck
        {
            public int Tested;
            public int Passed;
        }

        private sealed class BenchmarkResult
        {
            public int MeshRenderers;
            public int LodGroups;
            public int MeshColliders;
            public int BoxColliders;
            public int CapsuleColliders;
            public int EnabledCapsuleColliders;
            public float ShadowDistance;
            public int ShadowCascades;
            public CollisionCheck TreeCollision;
            public CollisionCheck TerrainCollision;

            private readonly StringBuilder views = new StringBuilder();
            private bool hasView;

            public void AppendView(string name, float averageFps, float minFps, float maxDelta, string screenshotPath)
            {
                if (hasView)
                {
                    views.Append(',');
                }

                hasView = true;
                views.Append('{');
                AppendJsonField(views, "name", name);
                views.Append(',');
                AppendJsonField(views, "averageFps", averageFps);
                views.Append(',');
                AppendJsonField(views, "minInstantFps", minFps);
                views.Append(',');
                AppendJsonField(views, "maxDeltaMs", maxDelta * 1000f);
                views.Append(',');
                AppendJsonField(views, "screenshot", screenshotPath.Replace('\\', '/'));
                views.Append('}');
            }

            public string ToJson()
            {
                StringBuilder builder = new StringBuilder(2048);
                builder.Append('{');
                AppendJsonField(builder, "meshRenderers", MeshRenderers);
                builder.Append(',');
                AppendJsonField(builder, "lodGroups", LodGroups);
                builder.Append(',');
                AppendJsonField(builder, "meshColliders", MeshColliders);
                builder.Append(',');
                AppendJsonField(builder, "boxColliders", BoxColliders);
                builder.Append(',');
                AppendJsonField(builder, "capsuleColliders", CapsuleColliders);
                builder.Append(',');
                AppendJsonField(builder, "enabledCapsuleColliders", EnabledCapsuleColliders);
                builder.Append(',');
                AppendJsonField(builder, "shadowDistance", ShadowDistance);
                builder.Append(',');
                AppendJsonField(builder, "shadowCascades", ShadowCascades);
                builder.Append(',');
                AppendCollision(builder, "treeCollision", TreeCollision);
                builder.Append(',');
                AppendCollision(builder, "terrainCollision", TerrainCollision);
                builder.Append(",\"views\":[");
                builder.Append(views);
                builder.Append("]}");
                return builder.ToString();
            }

            private static void AppendCollision(StringBuilder builder, string name, CollisionCheck check)
            {
                builder.Append('"');
                builder.Append(name);
                builder.Append("\":{");
                AppendJsonField(builder, "tested", check.Tested);
                builder.Append(',');
                AppendJsonField(builder, "passed", check.Passed);
                builder.Append('}');
            }

            private static void AppendJsonField(StringBuilder builder, string name, int value)
            {
                builder.Append('"');
                builder.Append(name);
                builder.Append("\":");
                builder.Append(value.ToString(InvariantCulture));
            }

            private static void AppendJsonField(StringBuilder builder, string name, float value)
            {
                builder.Append('"');
                builder.Append(name);
                builder.Append("\":");
                builder.Append(value.ToString("0.###", InvariantCulture));
            }

            private static void AppendJsonField(StringBuilder builder, string name, string value)
            {
                builder.Append('"');
                builder.Append(name);
                builder.Append("\":\"");
                builder.Append(value.Replace("\\", "\\\\").Replace("\"", "\\\""));
                builder.Append('"');
            }
        }
    }
}
