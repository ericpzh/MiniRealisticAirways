using System;
using System.Collections;
using System.Collections.Generic;
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
                    Animation.SetPixel(
                        (x > y && x - STROKE < y) ||
                        (-x + (WIDTH - STROKE) < y && -x + (WIDTH - STROKE) > y - STROKE),
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
            rect_ = new Rect(0, 3, WIDTH, HEIGHT - 15);
        }

        public static void DestoryTexture()
        {
            if (texture_ == null)
            {
                return;
            }
            Plugin.Log.LogInfo("Gauge texture destoried.");
            Texture2D.Destroy(texture_);

        }
        public static Rect rect_;
        public static Texture2D texture_;
        public const int HEIGHT = 45;
        public const int WIDTH = 90;
        public const int STROKE = 20;
    }

    public abstract class Gauge : MonoBehaviour
    {
        public bool Ready()
        {
            return spriteRenderers_ != null;
        }

        public void EnableSpriteRenderer(int count = GAUGE_COUNT)
        {
            if (spriteRenderers_ == null)
            {
                return;
            }

            for (int i = 0; i < Math.Min(spriteRenderers_.Count, count); i++)
            {
                spriteRenderers_[i].enabled = true;
            }
        }

        public void DisableSpriteRenderer()
        {
            if (spriteRenderers_ == null)
            {
                return;
            }

            foreach (SpriteRenderer spriteRenderer in spriteRenderers_)
            {
                spriteRenderer.enabled = false;
            }
        }

        protected void Initialize()
        {
            gameObjects_ = new List<GameObject>(GAUGE_COUNT);
            spriteRenderers_ = new List<SpriteRenderer>(GAUGE_COUNT);
            for (int i = 0; i < GAUGE_COUNT; i++)
            {
                GameObject gameObject = new GameObject();
                SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = Sprite.Create(GaugeArrowTexture.texture_, GaugeArrowTexture.rect_, Vector2.zero);
                spriteRenderer.enabled = false;

                gameObjects_.Add(gameObject);
                spriteRenderers_.Add(spriteRenderer);
            }
        }

        protected IEnumerator TransitioningCoroutine(int level, int targetLevel)
        {
            if (level == 1 && targetLevel == 2)
            {
                return Animation.BlinkCoroutine(spriteRenderers_[1]);
            }
            else if (level == 2 && targetLevel == 3)
            {
                return Animation.BlinkCoroutine(spriteRenderers_[2]);
            }
            else if (level == 3 && targetLevel == 2)
            {
                return Animation.BlinkCoroutine(spriteRenderers_[2]);
            }
            else if (level == 2 && targetLevel == 1)
            {
                return Animation.BlinkCoroutine(spriteRenderers_[1]);
            }
            return Animation.BlinkCoroutine(spriteRenderers_[2]);
        }

        protected void UpdateGaugeSpriteRenderers(int level)
        {
            if (spriteRenderers_ == null || spriteRenderers_.Count < GAUGE_COUNT)
            {
                return;
            }
            for (int i = 0; i < GAUGE_COUNT; i++)
            {
                spriteRenderers_[i].enabled = i <= level;
            }
        }

        private void OnDestroy()
        {
            foreach (SpriteRenderer spriteRenderer_ in spriteRenderers_)
            {
                Destroy(spriteRenderer_.sprite);
            }
        }

        protected List<GameObject> gameObjects_;
        protected List<SpriteRenderer> spriteRenderers_;
        protected const int GAUGE_COUNT = 3;
        protected const float GAUGE_OFFSET = 0.5f;
    }

    public class AircraftAltitudeGauge : Gauge
    {
        public IEnumerator GetTransitioningCoroutine(AltitudeLevel altitude, AltitudeLevel targetAltitude)
        {
            return TransitioningCoroutine((int)altitude, (int)targetAltitude);
        }

        public void UpdateGauge(AltitudeLevel altitude)
        {
            UpdateGaugeSpriteRenderers((int)altitude - 1);
        }

        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            Initialize();
            for (int i = 0; i < GAUGE_COUNT; i++)
            {
                gameObjects_[i].transform.SetParent(aircraft_.transform);
                gameObjects_[i].transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                gameObjects_[i].transform.localPosition = new Vector3(-2.8f, -1.5f + i * GAUGE_OFFSET, -9f);
            }
        }

        public Aircraft aircraft_;
    }

    public class AircraftSpeedGauge : Gauge
    {
        public IEnumerator GetTransitioningCoroutine(SpeedLevel speed, SpeedLevel targetSpeed)
        {
            return TransitioningCoroutine((int)speed, (int)targetSpeed);
        }

        public void UpdateGauge(SpeedLevel speed)
        {
            UpdateGaugeSpriteRenderers((int)speed - 1);
        }

        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            Initialize();
            for (int i = 0; i < GAUGE_COUNT; i++)
            {
                gameObjects_[i].transform.SetParent(aircraft_.transform);
                gameObjects_[i].transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                gameObjects_[i].transform.localPosition = new Vector3(1.5f + i * GAUGE_OFFSET, -0.5f, -9f);
                gameObjects_[i].transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
            }
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

            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = ((Component)waypoint_).transform.position;
            if (waypointPosition.x != _mousePos.x || waypointPosition.y != _mousePos.y)
            {
                return;
            }

            UpdateGaugeSpriteRenderers((int)altitude - 1);
        }

        private void Start()
        {
            if (waypoint_ == null)
            {
                return;
            }

            Initialize();
            for (int i = 0; i < GAUGE_COUNT; i++)
            {
                gameObjects_[i].transform.SetParent(waypoint_.transform);
                gameObjects_[i].transform.localScale = new Vector3(1f, 1f, 1f);
                gameObjects_[i].transform.localPosition = new Vector3(-1.5f, -0.5f + i * GAUGE_OFFSET * 0.5f, -9f);
            }
            EnableSpriteRenderer(2);
        }

        public PlaceableWaypoint waypoint_;
    }

    public class WaypointSpeedGauge : Gauge
    {
        public void UpdateWaypointSpeedGauge(SpeedLevel speed)
        {
            if (waypoint_.Invisible || !(waypoint_ is BaseWaypointAutoHeading))
            {
                return;
            }

            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = ((Component)waypoint_).transform.position;
            if (waypointPosition.x != _mousePos.x || waypointPosition.y != _mousePos.y)
            {
                return;
            }

            UpdateGaugeSpriteRenderers((int)speed - 1);
        }

        private void Start()
        {
            if (waypoint_ == null)
            {
                return;
            }

            Initialize();
            for (int i = 0; i < GAUGE_COUNT; i++)
            {
                gameObjects_[i].transform.SetParent(waypoint_.transform);
                gameObjects_[i].transform.localScale = new Vector3(1f, 1f, 1f);
                gameObjects_[i].transform.localPosition = new Vector3(0.8f + i * GAUGE_OFFSET * 0.5f, 0.45f, -9f);
                gameObjects_[i].transform.rotation = Quaternion.AngleAxis(90, Vector3.back);
            }
            EnableSpriteRenderer(2);
        }

        public PlaceableWaypoint waypoint_;
    }
}