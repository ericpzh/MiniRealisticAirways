using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniRealisticAirways
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Log = Logger;

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                if (commandLineArgs[i] == "-disableWind")
                {
                    Logger.LogInfo("Wind disabled");
                    DISABLE_WIND = true;
                }
                else if (commandLineArgs[i] == "-disableEvents")
                {
                    Logger.LogInfo("Event disabled");
                    DISABLE_EVENTS = true;
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"Scene loaded: {scene.name}");

            if (scene.name == "Menu" && AudioManager.instance != null)
            {
                AudioManager.instance.StartCoroutine(Tutorial.ShowTutorialCoroutine());
            }

            if ((scene.name == "MapPlayer" || scene.name == "London" || scene.name == "CreatorPlayer") &&
                AircraftManager.Instance != null && UpgradeManager.Instance != null)
            {
                // Pre-load textures for global use.
                FuelGaugeTextures.PreLoadTextures();
                GaugeArrowTexture.PreLoadTexture();
                WeatherCellTextures.PreLoadTextures();

                // Our windsock.
                GameObject esc_button = GameObject.Find("ESC_Button");
                if (esc_button != null)
                {
                    Logger.LogInfo("esc_button found, " + esc_button.name);
                    windsock_ = esc_button.gameObject.AddComponent<WindSock>();
                    windsock_.windsock_ = esc_button;
                    windsock_.InitializeText();

                    // Borrow esc_button to bind event/weather manager.
                    eventManager_ = esc_button.gameObject.AddComponent<EventManager>();
                }
            }
        }

        private void OnDestroy()
        {
            FuelGaugeTextures.DestoryTextures();
            GaugeArrowTexture.DestoryTexture();
            WeatherCellTextures.DestoryTextures();
        }

        internal static ManualLogSource Log;
        internal static bool showText_ = true;
        internal static WindSock windsock_;
        internal static EventManager eventManager_;
        internal static int MAX_WHILE_LOOP_ITER = 1000;
        internal static bool DISABLE_WIND = false;
        internal static bool DISABLE_EVENTS = false;
    }
}
