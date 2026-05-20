using UnityEngine;

namespace Valendia.Runtime
{
    public static class ValendiaGroundTextureFactory
    {
        public const int TextureSize = 128;
        public const float NormalContrast = 1.45f;

        public static Texture2D CreateDetailTexture(
            int seed,
            float textureStrength,
            string textureName,
            Color shadowTint,
            Color lightTint,
            float offset,
            bool makeNoLongerReadable)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)TextureSize;
                    float v = y / (float)TextureSize;
                    float detail = DetailNoise(seed, u, v, offset);
                    float fleck = Mathf.PerlinNoise(u * 57.3f + seed * 0.002f + offset, v * 61.1f - seed * 0.003f);
                    float mixed = Mathf.Clamp01(0.5f + (detail - 0.5f) * textureStrength + (fleck - 0.5f) * textureStrength * 0.24f);
                    Color color = Color.Lerp(shadowTint, lightTint, mixed);
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, makeNoLongerReadable);
            return texture;
        }

        public static Texture2D CreateNormalTexture(
            int seed,
            string textureName,
            float offset,
            float normalContrast,
            bool makeNoLongerReadable)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, true)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            float step = 1f / TextureSize;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)TextureSize;
                    float v = y / (float)TextureSize;
                    float left = DetailNoise(seed, Wrap01(u - step), v, offset);
                    float right = DetailNoise(seed, Wrap01(u + step), v, offset);
                    float down = DetailNoise(seed, u, Wrap01(v - step), offset);
                    float up = DetailNoise(seed, u, Wrap01(v + step), offset);
                    Vector3 normal = new Vector3((left - right) * normalContrast, (down - up) * normalContrast, 1f).normalized;

                    float encodedX = normal.x * 0.5f + 0.5f;
                    float encodedY = normal.y * 0.5f + 0.5f;
                    float encodedZ = normal.z * 0.5f + 0.5f;
                    texture.SetPixel(x, y, new Color(encodedX, encodedY, encodedZ, encodedX));
                }
            }

            texture.Apply(true, makeNoLongerReadable);
            return texture;
        }

        public static void ConfigureMaterial(Material material, Texture albedo, Texture normal, float tiling, float normalStrength)
        {
            if (material == null)
            {
                return;
            }

            Vector2 scale = Vector2.one * Mathf.Max(1f, tiling);
            if (albedo != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", albedo);
                    material.SetTextureScale("_BaseMap", scale);
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", albedo);
                    material.SetTextureScale("_MainTex", scale);
                }
            }

            if (normal != null && normalStrength > 0f && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", scale);
                if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
        }

        private static float DetailNoise(int seed, float u, float v, float offset)
        {
            float seedOffset = seed * 0.00037f + offset;
            float broad = Mathf.PerlinNoise(u * 3.7f + seedOffset, v * 3.1f - seedOffset);
            float mid = Mathf.PerlinNoise(u * 11.9f - seedOffset * 0.7f, v * 9.4f + seedOffset * 0.9f);
            float grain = Mathf.PerlinNoise(u * 29.5f + seedOffset * 1.3f, v * 33.2f - seedOffset * 1.1f);
            return Mathf.Clamp01(broad * 0.26f + mid * 0.34f + grain * 0.40f);
        }

        private static float Wrap01(float value)
        {
            value %= 1f;
            return value < 0f ? value + 1f : value;
        }
    }
}
