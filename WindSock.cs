
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways
{
    class WindSock : MonoBehaviour
    {
        public float GetWindDirection()
        {
            float duration = endTime_ - startTime_;
            float timeTraveled = Time.time - startTime_;
            float assumedDirection = windTargetDirection_;
            if (windShiftDirection_ > 0 && windTargetDirection_ < windPreviousDirection_)
            {
                // Travel positively passed 360.
                assumedDirection += 360;
            }
            else if (windShiftDirection_ < 0 && windTargetDirection_ > windPreviousDirection_)
            {
                // Travel negatively passed 360.
                assumedDirection -= 360;
            }
            float traveled = timeTraveled / duration * Math.Abs(assumedDirection - windPreviousDirection_);
            return Math.Abs(windPreviousDirection_ + traveled * windShiftDirection_) % 360f;
        }

        public override string ToString()
        {
            return "Wind: " + (int)Math.Round(GetWindDirection()) + "°";
        }

        public void InitializeText()
        {
            if (windsock_ == null)
            {
                return;
            }

            // Init wind text.
            textObj_ = Instantiate(new GameObject("Text"));
            text_ = textObj_.AddComponent<TextMeshPro>();
            
            text_.fontSize = 4f;
            text_.horizontalAlignment = HorizontalAlignmentOptions.Left;
            text_.verticalAlignment = VerticalAlignmentOptions.Top;
            text_.rectTransform.sizeDelta = new Vector2(2, 1);
            
            // make sorting layer of obj "Text"
            SortingGroup sg = textObj_.AddComponent<SortingGroup>();
            sg.sortingLayerName = "Text";
            sg.sortingOrder = 1;
        }

        public bool CanLand(float heading, Weight weight)
        {
            float windDirection = GetWindDirection();
            float angle = Math.Min((heading - windDirection) < 0 ? heading - windDirection + 360 : heading - windDirection,
                                   (windDirection-heading) < 0 ? windDirection - heading + 360 : windDirection - heading);
            if (angle <= 90)
            {
                return true;
            }
            return GoAroundProbability(angle, weight) < UnityEngine.Random.value;
        }

        private float GoAroundProbability(float x, Weight weight)
        {
            float f = (float)(1 / (1 + Math.Pow(1.05, 135 - x)));
            switch (weight)
            {
                case Weight.Light:
                    f += 0.2f;
                    break;
                case Weight.Medium:
                    f += 0.1f;
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
            return (windPreviousDirection_ + 180f + randomOffset) % 360f;
        }

        private void UpdateWind()
        {
            windTargetDirection_ = RandomDirection();
            windShiftDirection_ = UnityEngine.Random.value > 0.5 ? 1 : -1;
            startTime_ = Time.time;
            endTime_ = startTime_ + WIND_BASE_TIME + RandomUniform(WIND_RANDOM_TIME_OFFSET_LIMIT);
            Plugin.Log.LogInfo("Wind updated, new direction: " + windTargetDirection_ + 
                               ", shifting direction: " + windShiftDirection_ + ", endtime: " + endTime_);
        }

        private void UpdateWindText()
        {
            if (text_ == null || textObj_ == null)
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
            textObj_.transform.localPosition = new Vector3(-width / 2f + 3f, height / 2f - 1.5f, 0f);
            text_.text = ToString();
        }

        private void Start()
        {
            windPreviousDirection_ = UnityEngine.Random.value * 360f;
            UpdateWind();
        }

        private void Update()
        {
            if (windsock_ == null)
            {
                return;
            }

            windsock_.transform.rotation = Quaternion.AngleAxis(GetWindDirection() - 90, Vector3.back);
            UpdateWindText();

            if (Math.Abs(GetWindDirection() - windTargetDirection_) < EQUAL_THRES)
            {
                windPreviousDirection_ = windTargetDirection_;
                UpdateWind();
            }
        }

        public GameObject windsock_;
        public float windPreviousDirection_ = 0;
        public float windTargetDirection_ = 0;
        GameObject textObj_;
        private TMP_Text text_;
        private int windShiftDirection_ = 0;
        private float startTime_ = 0;
        private float endTime_ = 0;
        private const float WIND_RANDOM_OFFSET_LIMIT = 30f;
        private const float WIND_BASE_TIME = 6f * 300f /* Time per day */;
        private const float WIND_RANDOM_TIME_OFFSET_LIMIT = 0.5f * 300f /* Time per day */;
        private const float EQUAL_THRES = 0.5f;
    }
}