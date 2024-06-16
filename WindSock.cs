
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways
{
    class WindSock : MonoBehaviour
    {
        public override string ToString()
        {
            return "Wind: " + (int)Math.Round(windDirection_) + "°";
        }

        public void InitializeText()
        {
            if (windsock_ == null)
            {
                return;
            }

            // Init wind text.
            textGameObject_ = Instantiate(new GameObject("Text"));
            text_ = textGameObject_.AddComponent<TextMeshPro>();
            
            text_.fontSize = 4f;
            text_.horizontalAlignment = HorizontalAlignmentOptions.Left;
            text_.verticalAlignment = VerticalAlignmentOptions.Top;
            text_.rectTransform.sizeDelta = new Vector2(2, 1);
            
            // make sorting layer of obj "Text"
            SortingGroup sg = textGameObject_.AddComponent<SortingGroup>();
            sg.sortingLayerName = "Text";
            sg.sortingOrder = 1;
        }

        public bool CanLand(float heading, Weight weight)
        {
            float angle = Math.Min((heading - windDirection_) < 0 ? heading - windDirection_ + 360 : heading - windDirection_,
                                   (windDirection_-heading) < 0 ? windDirection_ - heading + 360 : windDirection_ - heading);
            if (angle <= 90)
            {
                return true;
            }
            return GoAroundProbability(angle, weight) < UnityEngine.Random.value;
        }

        private void CorrectWindDirection()
        {
            if (windDirection_ < 0)
            {
                windDirection_ += 360;
            }
            else if (windDirection_ >= 360)
            {
                windDirection_ -= 360;
            }
        }

        private float GoAroundProbability(float x, Weight weight)
        {
            float f = (float)(1 / (1 + Math.Pow(1.05, 135 - x)));
            switch (weight)
            {
                case Weight.Light:
                    f += 0.1f;
                    break;
                case Weight.Medium:
                    f += 0.05f;
                    break;
                case Weight.Heavy:
                    break;
            }
            return (float)Math.Clamp(f, 0, 1);
        }

        private float RandomUniform(float limit)
        {
            return UnityEngine.Random.value * 2f * limit - limit;
        }

        private float RandomDirection()
        {
            float randomOffset = RandomUniform(WIND_RANDOM_OFFSET_LIMIT);
            float windShiftDirection = UnityEngine.Random.value > 0.5 ? 1 : -1;
            return WIND_RANDOM_BASE * windShiftDirection + randomOffset;
        }

        private IEnumerator UpdateWindCoroutine()
        {
            float updateTime = WIND_BASE_TIME + RandomUniform(WIND_RANDOM_TIME_OFFSET_LIMIT);
            float timeGradient = updateTime / UPDATE_COUNT;
            float windGradient = RandomDirection() / UPDATE_COUNT;

            for (int i = 0; i < UPDATE_COUNT; i++)
            {
                windDirection_ += windGradient;
                CorrectWindDirection();

                windsock_.transform.rotation = Quaternion.AngleAxis(windDirection_ - 90, Vector3.back);
                yield return new WaitForSeconds(timeGradient);
            }

            yield return UpdateWindCoroutine();
        }

        private void Start()
        {
            windDirection_ = UnityEngine.Random.value * 360f;
            StartCoroutine(UpdateWindCoroutine());
        }

        private void Update()
        {
            if (text_ == null || textGameObject_ == null)
            {
                return;
            }

            if (!Plugin.showText_)
            {
                text_.text = "";
                return;
            }

            float height = 2f * Camera.main.orthographicSize;
            float width = height * Camera.main.aspect;
            textGameObject_.transform.localPosition = new Vector3(-width / 2f + 3f, height / 2f - 1.5f, 0f);
            text_.text = ToString();
        }

        public GameObject windsock_;
        public float windDirection_ = 0;
        private GameObject textGameObject_;
        private TMP_Text text_;
        private const float WIND_RANDOM_BASE = 180f;
        private const float WIND_RANDOM_OFFSET_LIMIT = 30f;
        private const float WIND_BASE_TIME = 6f * 300f /* Time per day */;
        private const float WIND_RANDOM_TIME_OFFSET_LIMIT = 0.5f * 300f /* Time per day */;
        private const float UPDATE_COUNT = 360f;
    }
}