using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UIComponents.Modals;
using System.Collections;
using TMPro;
using UnityEngine.Video;

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
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                showText_ = !showText_;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"Scene loaded: {scene.name}");

            if (scene.name == "Menu")
            {
                AudioManager.instance.StartCoroutine(ShowModHintCoroutine());
            }

            if ((scene.name == "MapPlayer" || scene.name == "London") &&
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

        IEnumerator ShowModHintCoroutine()
        {
            yield return new WaitForSeconds(1);
            yield return new WaitUntil(() => ModalManager.Instance != null);
            ShowModHint();
        }
        private static void ShowModHint()
        {
            ModalWithButton modal = ModalManager.NewModalWithButtonStatic(PluginInfo.PLUGIN_GUID.ToString() + PluginInfo.PLUGIN_VERSION.ToString());
            modal.SetTitle("  Mod Enabled!  ");
            modal.SetHeading("Thank you for playing \"MiniRealisticAirways\"! Before you start, you might want to check out this mod's introduction to help you get the most out of the game!");
            modal.SetDescription("English: <b><u><link=\"ENG\">Click here</link></u></b>");
            modal.button.gameObject.SetActive(false);

            TMP_Text newTMP = Instantiate(modal.description, modal.description.transform);
            newTMP.transform.position = modal.description.transform.position - new Vector3(0, 150, 0);
            newTMP.text = "简体中文: <b><u><link=\"CHS\">点击这里</link></u></b>";
            modal.description.gameObject.AddComponent<LinkHandler>().url = "https://m0pt5uret4t.feishu.cn/docx/VURHdwhonozWZcxJAaHcG5tPnUg?from=from_copylink";
            newTMP.gameObject.AddComponent<LinkHandler>().url = "https://m0pt5uret4t.feishu.cn/docx/VaghdGDiEokiJmxeVRocJJonnhh?from=from_copylink";


            modal.Show();
        }

        internal static ManualLogSource Log;
        internal static bool showText_ = true;
        internal static WindSock windsock_;
        internal static EventManager eventManager_;
        internal static int MAX_WHILE_LOOP_ITER = 1000;
    }
}
