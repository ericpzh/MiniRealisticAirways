using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniRealisticAirways
{
    public static class Settings
    {
        public static void ProcessLaunchOptions()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                if (commandLineArgs[i] == "-disableWind")
                {
                    Plugin.Log.LogInfo("Wind disabled");
                    DISABLE_WIND = true;
                }
                else if (commandLineArgs[i] == "-disableEvents")
                {
                    Plugin.Log.LogInfo("Event disabled");
                    DISABLE_EVENTS = true;
                }
                else if (commandLineArgs[i] == "-disableTCAS")
                {
                    Plugin.Log.LogInfo("TCAS disabled");
                    DISABLE_TCAS = true;
                }
            }
        }

        public static void SetupWindToggle(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText)
        {
            SetupToggle(265f, -225f, ref windToggle, ref ___SubtitlesButton, OnWindButtonClick, !DISABLE_WIND);
            SetupText(-300f, -70f, Tutorial.ShowEnLocale() ? "Enable Wind" : "启用风向", ref windText, ref ___ColorAccessibilityText);
        }

        public static void SetupEventToggle(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText)
        {
            SetupToggle(265f, -275f, ref eventToggle, ref ___SubtitlesButton, OnEventButtonClick, !DISABLE_EVENTS);
            SetupText(-300f, -130f, Tutorial.ShowEnLocale() ? "Enable Events" : "启用特情", ref eventText, ref ___ColorAccessibilityText);
        }

        public static void SetupTCASToggle(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText)
        {
            SetupToggle(265f, -335f, ref tcasToggle, ref ___SubtitlesButton, OnTCASButtonClick, !DISABLE_TCAS);
            SetupText(-300f, -190f, Tutorial.ShowEnLocale() ? "Enable TCAS" : "启用TCAS", ref tcasText, ref ___ColorAccessibilityText);
        }

        public static void OnWindButtonClick()
        {
            OnToggle(ref windToggle, DISABLE_WIND);
            DISABLE_WIND = !DISABLE_WIND;
            Plugin.Log.LogInfo("Disable wind: " + DISABLE_WIND);
            // ES3.Save<bool>("CC_Options_DisableWind", DISABLE_WIND);
        }

        public static void OnEventButtonClick()
        {
            OnToggle(ref eventToggle, DISABLE_EVENTS);
            DISABLE_EVENTS = !DISABLE_EVENTS;
            Plugin.Log.LogInfo("Disable events: " + DISABLE_EVENTS);
            // ES3.Save<bool>("CC_Options_DisableEvent", DISABLE_EVENTS);
        }

        public static void OnTCASButtonClick()
        {
            OnToggle(ref tcasToggle, DISABLE_TCAS);
            DISABLE_TCAS = !DISABLE_TCAS;
            Plugin.Log.LogInfo("Disable TCAS: " + DISABLE_TCAS);
            // ES3.Save<bool>("CC_Options_DisableTCAS", DISABLE_TCAS);
        }

        private static void SetupToggle(float x, float y, ref Button toggle, ref Button ___SubtitlesButton, UnityAction action, bool defaultValue)
        {
            toggle = GameObject.Instantiate(___SubtitlesButton.gameObject, ___SubtitlesButton.transform.parent).GetComponent<Button>();
            toggle.transform.localPosition = new Vector3(x, y, 0);
            toggle.onClick.AddListener(action);
            toggle.GetComponent<Image>().sprite = defaultValue ? On : Off;
        }

        private static void SetupText(float x, float y, string text, ref TMP_Text toggleText, ref TMP_Text ___ColorAccessibilityText)
        {
            toggleText = GameObject.Instantiate(___ColorAccessibilityText.gameObject, ___ColorAccessibilityText.transform.parent).GetComponent<TMP_Text>();
            toggleText.transform.localPosition = new Vector3(x, y, 0);
            toggleText.text = text;
            toggleText.fontSize = 20f;
            toggleText.horizontalAlignment = HorizontalAlignmentOptions.Right;
        }

        private static void OnToggle(ref Button toggle, bool value)
        {
            if (value && On != null)
            {
                toggle.GetComponent<Image>().sprite = On;
            }
            else if (!value && Off != null)
            {
                toggle.GetComponent<Image>().sprite = Off;
            }
        }

        public static bool DISABLE_WIND = false; //ES3.Load<bool>("CC_Options_DisableWind", false);
        public static bool DISABLE_EVENTS = false; //ES3.Load<bool>("CC_Options_DisableEvent", false);
        public static bool DISABLE_TCAS = false; //ES3.Load<bool>("CC_Options_DisableTCAS", false);
        public static Button windToggle;
        public static Button eventToggle;
        public static Button tcasToggle;
        public static TMP_Text windText;
        public static TMP_Text eventText;
        public static TMP_Text tcasText;
        public static Sprite On;
        public static Sprite Off;
    }

    [HarmonyPatch]
    public class OptionsManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OptionsManager), "Start")]
        public static void StartPostfix(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText, ref Sprite ___On, ref Sprite ___Off)
        {
            // Get On/Off Sprite.
            Settings.On = ___On;
            Settings.Off = ___Off;

            Settings.SetupWindToggle(ref ___SubtitlesButton, ref ___ColorAccessibilityText);
            Settings.SetupEventToggle(ref ___SubtitlesButton, ref ___ColorAccessibilityText);
            Settings.SetupTCASToggle(ref ___SubtitlesButton, ref ___ColorAccessibilityText);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OptionsManager), "OnSceneLoaded")]
        public static void OnSceneLoadedPostfix(ref Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name == "Menu")
            {
                Settings.windToggle.transform.parent.gameObject.SetActive(true);
                Settings.eventToggle.transform.parent.gameObject.SetActive(true);
                Settings.tcasToggle.transform.parent.gameObject.SetActive(true);
                Settings.windText.transform.parent.gameObject.SetActive(true);
                Settings.eventText.transform.parent.gameObject.SetActive(true);
                Settings.tcasText.transform.parent.gameObject.SetActive(true);
                
            }
            else
            {
                Settings.windToggle.transform.parent.gameObject.SetActive(false);
                Settings.eventToggle.transform.parent.gameObject.SetActive(false);
                Settings.tcasToggle.transform.parent.gameObject.SetActive(false);
                Settings.windText.transform.parent.gameObject.SetActive(false);
                Settings.eventText.transform.parent.gameObject.SetActive(false);
                Settings.tcasText.transform.parent.gameObject.SetActive(false);
            }
        }
    }
}
