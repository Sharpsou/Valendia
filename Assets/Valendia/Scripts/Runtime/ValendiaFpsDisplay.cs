using UnityEngine;

namespace Valendia.Runtime
{
    public sealed class ValendiaFpsDisplay : MonoBehaviour
    {
        [SerializeField] private bool showFps = true;
        [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

        private readonly Rect displayRect = new Rect(14f, 12f, 160f, 34f);
        private GUIStyle style;
        private float elapsed;
        private int frames;
        private float displayedFps;

        private void Update()
        {
            if (!showFps)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            frames++;

            if (elapsed >= refreshInterval)
            {
                displayedFps = frames / elapsed;
                elapsed = 0f;
                frames = 0;
            }
        }

        private void OnGUI()
        {
            if (!showFps || !Application.isPlaying)
            {
                return;
            }

            style ??= CreateStyle();
            GUI.Label(displayRect, $"FPS {displayedFps:0}", style);
        }

        private static GUIStyle CreateStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = new Color(0.96f, 0.90f, 0.70f)
                }
            };
        }
    }
}
