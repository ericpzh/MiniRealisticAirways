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
        protected GameObject gameObject_;
        public SpriteRenderer spriteRenderer_;
    }

    public class AircraftSpeedGauge : Gauge
    {
        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            gameObject_ = new GameObject();
            gameObject_.transform.SetParent(aircraft_.transform);
            spriteRenderer_ = gameObject_.AddComponent<SpriteRenderer>();
            spriteRenderer_.enabled = false;
        }

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

            float speedDiff = Math.Abs(aircraft_.speed - aircraft_.targetSpeed);
            bool speedChanged = speedDiff > AircraftSpeed.SPEED_DELTA;
            spriteRenderer_.enabled = aircraftAltitude.altitude_ > AltitudeLevel.Ground &&
                                      !(speedChanged && Animation.BlinkLong());

            if(spriteRenderer_.sprite != null)
            {
                Destroy(spriteRenderer_.sprite);
            }
            switch (aircraftSpeed.GetSpeed())
            {
                case SpeedLevel.Slow:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-1.15f, -1.15f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(270, Vector3.back);
                    return;
                case SpeedLevel.Normal:
                    spriteRenderer_.sprite = Sprite.Create(GaugeLineTexture.texture_, GaugeLineTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-0.9f, -1.15f, -9f);
                    gameObject_.transform.rotation = Quaternion.identity;
                    return;
                case SpeedLevel.Fast:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(1.15f, 1.15f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
                    return;
            }
        }

        private void OnDestroy()
        {
            Destroy(spriteRenderer_.sprite);
        }

        public Aircraft aircraft_;

    }

    public class WaypointSpeedGauge : Gauge
    {
        public void UpdateWaypointSpeedGauge(SpeedLevel speed)
        {
            if (waypoint_.Invisible || !(waypoint_ is BaseWaypointAutoHeading))
            {
                return;
            }

            if (waypoint_.GetFieldValue<int>("state") != 2/*PlaceableWaypoint.State.WaitingForPlacing*/)
            {
                return;
            }

            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = ((Component)waypoint_).transform.position;
            if (waypointPosition.x != _mousePos.x ||  waypointPosition.y != _mousePos.y)
            {
                return;
            }

            if(spriteRenderer_.sprite != null)
            {
                Destroy(spriteRenderer_.sprite);
            }
            switch (speed)
            {
                case SpeedLevel.Slow:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-0.68f, -0.73f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(270, Vector3.back);
                    return;
                case SpeedLevel.Normal:
                    spriteRenderer_.sprite = Sprite.Create(GaugeLineTexture.texture_, GaugeLineTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-0.605f, -0.785f, -9f);
                    gameObject_.transform.rotation = Quaternion.identity;
                    return;
                case SpeedLevel.Fast:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(0.6f, 0.75f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
                    return;
            }
        }

        private void Start()
        {
            if (waypoint_ == null)
            {
                return;
            }

            gameObject_ = new GameObject();
            gameObject_.transform.SetParent(waypoint_.transform);
            gameObject_.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            spriteRenderer_ = gameObject_.AddComponent<SpriteRenderer>();
            spriteRenderer_.enabled = true;
        }

        private void OnDestroy()
        {
            Destroy(spriteRenderer_.sprite);
        }

        public PlaceableWaypoint waypoint_;
    }

    public class AircraftAltitudeGauge : Gauge
    {
        public void UpdateGauge(AltitudeLevel altitude)
        {
            if (spriteRenderer_ == null)
            {
                return;
            }

            if(spriteRenderer_.sprite != null)
            {
                Destroy(spriteRenderer_.sprite);
            }

            switch (altitude)
            {
                case AltitudeLevel.Low:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(1.15f, -1.1f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(180, Vector3.back);
                    return;
                case AltitudeLevel.Normal:
                    spriteRenderer_.sprite = Sprite.Create(GaugeLineTexture.texture_, GaugeLineTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-1.15f, 1f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
                    return;
                case AltitudeLevel.High:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-1.15f, 1.25f, -9f);
                    gameObject_.transform.rotation = Quaternion.identity;
                    return;
            }
        }

        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            gameObject_ = new GameObject();
            gameObject_.transform.SetParent(aircraft_.transform);
            spriteRenderer_ = gameObject_.AddComponent<SpriteRenderer>();
            spriteRenderer_.enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(spriteRenderer_.sprite);
        }

        public Aircraft aircraft_;
    }

    public class WaypointAltitudeGauge : Gauge
    {
        public void UpdateWaypointAltitudeGauge(AltitudeLevel altitude)
        {
            if (waypoint_.Invisible || !(waypoint_ is BaseWaypointAutoHeading))
            {
                return;
            }

            if (waypoint_.GetFieldValue<int>("state") != 2 /*PlaceableWaypoint.State.WaitingForPlacing*/)
            {
                return;
            }

            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = ((Component)waypoint_).transform.position;
            if (waypointPosition.x != _mousePos.x ||  waypointPosition.y != _mousePos.y)
            {
                return;
            }

            if(spriteRenderer_.sprite != null)
            {
                Destroy(spriteRenderer_.sprite);
            }
            switch (altitude)
            {
                case AltitudeLevel.Low:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(0.72f, -0.73f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(180, Vector3.back);
                    return;
                case AltitudeLevel.Normal:
                    spriteRenderer_.sprite = Sprite.Create(GaugeLineTexture.texture_, GaugeLineTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-0.725f, 0.63f, -9f);
                    gameObject_.transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
                    return;
                case AltitudeLevel.High:
                    spriteRenderer_.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                    gameObject_.transform.localPosition = new Vector3(-0.69f, 0.68f, -9f);
                    gameObject_.transform.rotation = Quaternion.identity;
                    return;
            }
        }

        private void Start()
        {
            if (waypoint_ == null)
            {
                return;
            }

            gameObject_ = new GameObject();
            gameObject_.transform.SetParent(waypoint_.transform);
            gameObject_.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            spriteRenderer_ = gameObject_.AddComponent<SpriteRenderer>();
            spriteRenderer_.enabled = true;
        }

        private void OnDestroy()
        {
            Destroy(spriteRenderer_.sprite);
        }

        public PlaceableWaypoint waypoint_;
    }
}