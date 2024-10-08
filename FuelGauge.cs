using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRealisticAirways
{
    public static class FuelGaugeTextures
    {
        private static void DrawDropletX(int y, Color color, ref Texture2D texture)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (y < 14)
                {
                    // Circle.
                    int i = x - SIZE / 2;
                    int j = y - SIZE / 2;
                    Animation.SetPixel(Math.Sqrt(i * i + j * j) < SIZE / 2, x, y, color, ref texture);
                }
                else
                {
                    // Magic of desmos calculator.
                    Animation.SetPixel(Math.Sin((float)x / 12 + 3.25) + 0.05 < Math.Sin((float)y / 17.5 + 2.4),
                             x, y, color, ref texture);
                }

            }
        }

        private static Texture2D DrawDroplet(int step)
        {
            int percent = (int)(step * SIZE / REFRESH_GRADIENT);
            Texture2D texture = new Texture2D(SIZE, SIZE);
            for (int y = 0; y < percent; y++)
            {
                DrawDropletX(y, Color.white, ref texture);
            }
            for (int y = percent; y < texture.height; y++)
            {
                DrawDropletX(y, Color.gray, ref texture);
            }
            texture.Apply();
            return texture;
        }

        public static void PreLoadTextures()
        {
            Plugin.Log.LogInfo("Pre-rendered fuel gauge textures.");
            fuelTextures_ = new List<Texture2D>(REFRESH_GRADIENT + 1);
            for (int i = 0; i < REFRESH_GRADIENT + 1; i++)
            {
                fuelTextures_.Add(DrawDroplet(i));
            }
            rect_ = new Rect(0, 0, SIZE, SIZE);
        }

        public static void DestoryTextures()
        {
            if (fuelTextures_ == null)
            {
                return;
            }
            Plugin.Log.LogInfo("Fuel gauge textures destoried.");
            for (int i = 0; i < REFRESH_GRADIENT + 1; i++)
            {
                if (fuelTextures_[i])
                    Texture2D.Destroy(fuelTextures_[i]);
            }
            fuelTextures_.Clear();
        }
        public static Rect rect_;
        public static List<Texture2D> fuelTextures_;
        public const int SIZE = 35;
        public const int REFRESH_GRADIENT = 100;
    }

    public class FuelGauge : MonoBehaviour
    {
        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            gameObject_ = new GameObject();
            gameObject_.transform.SetParent(aircraft_.transform);
            gameObject_.transform.localPosition = new Vector3(1, 1, -5f);
            spriteRenderer_ = gameObject_.AddComponent<SpriteRenderer>();
            spriteRenderer_.sprite = Sprite.Create(FuelGaugeTextures.fuelTextures_[FuelGaugeTextures.REFRESH_GRADIENT],
                                                   FuelGaugeTextures.rect_, Vector2.zero);
        }

        private void OnDestroy()
        {
            Destroy(spriteRenderer_.sprite);
        }

        public Aircraft aircraft_;
        private GameObject gameObject_;
        public SpriteRenderer spriteRenderer_;
    }
}