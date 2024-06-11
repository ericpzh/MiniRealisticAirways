using System;
using UnityEngine;

namespace MiniRealisticAirways
{
    public static class GaugeArrowTexture
    {
        private static Texture2D DrawArrow()
        {
            Texture2D texture = new Texture2D(WIDTH, HEIGHT);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    // Magic of desmos calculator.
                    Animation.SetPixel((y > 2 && x > y && x - STROKE < y) ||
                             (y > 2 && -x + (WIDTH - STROKE) < y && -x + (WIDTH - STROKE) > y - STROKE),
                             x, y, Animation.gaugeColor, ref texture);

                }
            }
            texture.Apply();
            return texture;
        }

        public static void PreLoadTexture()
        {
            Plugin.Log.LogInfo("Pre-rendered gauge texture.");
            texture_ = DrawArrow();
            rect_ = new Rect(0, 0, WIDTH, HEIGHT - 7);
        }

        public static void DestoryTexture()
        {
            Plugin.Log.LogInfo("Gauge texture destoried.");
            Texture2D.Destroy(texture_);

        }
        public static Rect rect_;
        public static Texture2D texture_;
        public const int HEIGHT = 45;
        public const int WIDTH = 90;
        public const int STROKE = 15;
    }

    public static class GaugeLineTexture
    {
        private static Texture2D DrawLine()
        {
            Texture2D texture = new Texture2D(SIZE, SIZE);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    // Magic of desmos calculator.
                    Animation.SetPixel(x < SIZE / 2 && x > SIZE / 2 - STROKE,
                                       x, y, Animation.gaugeColor, ref texture);

                }
            }
            texture.Apply();
            return texture;
        }

        public static void PreLoadTexture()
        {
            Plugin.Log.LogInfo("Pre-rendered gauge texture.");
            texture_ = DrawLine();
            rect_ = new Rect(0, 0, SIZE, SIZE);
        }

        public static void DestoryTexture()
        {
            Plugin.Log.LogInfo("Gauge texture destoried.");
            Texture2D.Destroy(texture_);

        }
        public static Rect rect_;
        public static Texture2D texture_;
        public const int SIZE = 90;
        public const int STROKE = 15;
    }

    public class Gauge : MonoBehaviour
    {
        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            obj_ = new GameObject();
            obj_.transform.SetParent(aircraft_.transform);
            spriteRenderer_ = obj_.AddComponent<SpriteRenderer>();
            
            spriteRenderer_.enabled = false;
        }

        public Aircraft aircraft_;
        protected GameObject obj_;
        protected SpriteRenderer spriteRenderer_;
    }

    public class SpeedGauge : Gauge
    {
        private void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }

            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            if (aircraftState == null) 
            {
                return;
            }

            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            if (aircraftSpeed == null || aircraftAltitude == null)
            {
                return;
            }

            float SpeedDiff = Math.Abs(aircraft_.speed - aircraft_.targetSpeed);
            bool SpeedChanged = SpeedDiff > AircraftSpeed.SPEED_DELTA;
            spriteRenderer_.enabled = aircraftAltitude.altitude_ > AltitudeLevel.Ground &&
                                      !(SpeedChanged && Animation.BlinkLong());
            if(spriteRenderer_.sprite != null)
            {
                Destroy(spriteRenderer_.sprite);
            }
            switch (aircraftSpeed.GetSpeed())
            {
                case SpeedLevel.Slow:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    obj_.transform.localPosition = new Vector3(-1.15f, -1.15f, -9f);
                    obj_.transform.rotation = Quaternion.AngleAxis(270, Vector3.back);
                    return;
                case SpeedLevel.Normal:
                    spriteRenderer_.sprite = Sprite.Create(GaugeLineTexture.texture_, GaugeLineTexture.rect_, Vector2.zero);
                    obj_.transform.localPosition = new Vector3(-0.9f, -1.15f, -9f);
                    obj_.transform.rotation = Quaternion.identity;
                    return;
                case SpeedLevel.Fast:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    obj_.transform.localPosition = new Vector3(1.15f, 1.15f, -9f);
                    obj_.transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
                    return;
            }
        }

        private void OnDestroy()
        {
            Destroy(spriteRenderer_.sprite);
        }
    }

    public class AltitudeGauge : Gauge
    {
        private void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }
            
            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            if (aircraftState == null) 
            {
                return;
            }
            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            if (aircraftAltitude == null)
            {
                return;
            }

            bool AltitudeChanged = aircraftAltitude.altitude_ != aircraftAltitude.targetAltitude_;
            spriteRenderer_.enabled = aircraftAltitude.altitude_ > AltitudeLevel.Ground &&
                                      !(AltitudeChanged && Animation.BlinkLong());

            if(spriteRenderer_.sprite != null)
            {
                Destroy(spriteRenderer_.sprite);
            }
            switch (aircraftAltitude.altitude_)
            {
                case AltitudeLevel.Low:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    obj_.transform.localPosition = new Vector3(1.15f, -1.1f, -9f);
                    obj_.transform.rotation = Quaternion.AngleAxis(180, Vector3.back);
                    return;
                case AltitudeLevel.Normal:

                    spriteRenderer_.sprite = Sprite.Create(GaugeLineTexture.texture_, GaugeLineTexture.rect_, Vector2.zero);
                    obj_.transform.localPosition = new Vector3(-1.15f, 1f, -9f);
                    obj_.transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
                    
                    return;
                case AltitudeLevel.High:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    obj_.transform.localPosition = new Vector3(-1.15f, 1.25f, -9f);
                    obj_.transform.rotation = Quaternion.identity;
                    return;
            }
        }

        private void OnDestroy()
        {
            Destroy(spriteRenderer_.sprite);
        }
    }
}