
using HarmonyLib;
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
            // Convert wind heading into aircraft heading.
            float convertedWindDirection_ = windDirection_ - 180;
            if (convertedWindDirection_ < 0)
            {
                convertedWindDirection_ += 360;
            }

            float angle = Math.Min((heading - convertedWindDirection_) < 0 ? heading - convertedWindDirection_ + 360 : heading - convertedWindDirection_,
                                   (convertedWindDirection_ - heading) < 0 ? convertedWindDirection_ - heading + 360 : convertedWindDirection_ - heading);
            if (angle <= 90)
            {
                return true;
            }

            if (GoAroundProbability(angle, weight) < UnityEngine.Random.value)
            {
                return true;
            }
            Plugin.Log.LogInfo(
                "Go-around induced by wind. Current wind: " + windDirection_ + ". Converted wind: " + convertedWindDirection_ +
                " Current Heading: " + heading + " Angle: " + angle + " Probabaility " + GoAroundProbability(angle, weight));
            return false;
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

            Plugin.Log.LogInfo("Wind updated, moving from " + windDirection_ + " to " + windGradient + " in time " + updateTime);

            for (int i = 0; i < UPDATE_COUNT; i++)
            {
                windDirection_ += windGradient;
                CorrectWindDirection();

                if (!windsock_.activeSelf)
                {
                    // Force windsock to become active.
                    Plugin.Log.LogWarning("windsock_ isn't active, forcing it to be. Current wind: " + windDirection_);
                    windsock_.SetActive(value: true);
                }
                windsock_.transform.rotation = Quaternion.AngleAxis(windDirection_, Vector3.back);
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
            if (Time.timeScale == 0f)
            {
                // Skip update during time pause.
                return;
            }

            if (text_ == null || textGameObject_ == null)
            {
                return;
            }

            if (!Plugin.showText_)
            {
                text_.text = "";
                return;
            }

            // TLPBR from GUIAutoHider.
            Vector3 escButtonBottomRight = Camera.main.ViewportToWorldPoint(new Vector3(0.07f, 0.88f, 0f));
            textGameObject_.transform.position = new Vector3(escButtonBottomRight.x + 1f, escButtonBottomRight.y + 0.5f, 0f);
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

    [HarmonyPatch(typeof(GUIAutoHider), "CheckTL", new Type[] { })]
    class PatchCheckTL
    {
        static bool Prefix(ref GUIAutoHider __instance)
        {
            // Disable auto-hiding of windsock.
            return false;
        }
    }

    [HarmonyPatch(typeof(GUIAutoHider), "Update", new Type[] { })]
    class PatchGUIAutoHiderUpdate
    {
        static void Postfix(ref GUIAutoHider __instance)
        {
            if (Time.timeScale == 0f)
            {
                // Skip update during time pause.
                return;
            }

            if (!GameOverManager.Instance.GameOverFlag && __instance.TL.alpha < 1f)
            {
                Plugin.Log.LogWarning("Windsock auto hidden in-game.");
                __instance.TL.alpha = 1f;
            }
        }
    }
}