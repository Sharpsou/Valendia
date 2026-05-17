using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valendia.Runtime;

namespace Valendia.Editor
{
    public static class ValendiaPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Valendia/ValendiaPrototype.unity";
        private const string PreviewPath = "Assets/Valendia/Docs/ValendiaPrototypePreview.png";

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

            ValendiaLandscapeGenerator generator = Object.FindFirstObjectByType<ValendiaLandscapeGenerator>();
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

        private static ValendiaLandscapeGenerator CreateWorld()
        {
            GameObject world = new GameObject("Valendia World");
            ValendiaLandscapeGenerator generator = world.AddComponent<ValendiaLandscapeGenerator>();
            generator.Generate();
            return generator;
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
            QualitySettings.shadowDistance = 180f;
            QualitySettings.shadowCascades = 2;
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
