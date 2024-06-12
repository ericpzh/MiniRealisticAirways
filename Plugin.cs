using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
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

            if ((scene.name == "MapPlayer" || scene.name == "London") &&
                AircraftManager.Instance != null && UpgradeManager.Instance != null)
            {
                Logger.LogInfo("Hooking AircraftManager");
                AircraftManager.Instance.AircraftCreateEvent.AddListener(HookAircraft);

                Logger.LogInfo("Hooking UpgradeManager");
                UpgradeManager.Instance.SelectUpgradeEvent.AddListener(HookUpgrade);

                // Pre-load fuel gauge textures for global use.
                FuelGaugeTextures.PreLoadTextures();
                GaugeArrowTexture.PreLoadTexture();
                GaugeLineTexture.PreLoadTexture();

                // Our windsock.
                GameObject esc_button = GameObject.Find("ESC_Button");
                if (esc_button != null)
                {
                    Logger.LogInfo("esc_button found, " + esc_button.name);
                    windsock_ = esc_button.gameObject.AddComponent<WindSock>();
                    windsock_.windsock_ = esc_button;
                    windsock_.InitializeText();

                    
                    // Borrow esc_button to bind event manager.
                    eventManager_ = esc_button.gameObject.AddComponent<EventManager>();
                }
            }
        }

        private void OnDestroy()
        {
            FuelGaugeTextures.DestoryTextures();
            GaugeArrowTexture.DestoryTexture();
            GaugeLineTexture.DestoryTexture();
        }

        private void HookAircraft(Vector2 pos, Aircraft aircraft)
        {
            if (aircraft.direction == Aircraft.Direction.Inbound)
            {
                Logger.LogInfo("Aircraft created via HookAircraft: " + aircraft.name);

                AircraftState aircraftState = aircraft.gameObject.AddComponent<AircraftState>();
                aircraftState.aircraft_ = aircraft;
                aircraftState.Initialize();
                AircraftType aircraftType = aircraftState.aircraftType_;
                if (aircraftType != null)
                {
                    aircraftType.weight_ = BaseAircraftType.RandomWeight();
                    Logger.LogInfo("Aircraft created with weight: " + aircraftType.weight_);
                    // Only arrival aircraft have fuel limit.
                    aircraftType.fuelOutTime_ = aircraftType.GetFuelOutTime();
                }
            }
        }

        private void HookUpgrade(UpgradeOpt upgrade)
        {
            Logger.LogInfo("Upgrade selected: " + upgrade);
            
            if (upgrade == UpgradeOpt.AUTO_HEADING_PROP)
            {
                StartCoroutine(SpawnWaypointAutoHeadingCoroutine());
            }
        }

        private IEnumerator SpawnWaypointAutoHeadingCoroutine()
        {
            yield return new WaitForFixedUpdate();
            WaypointPropsManager.Instance.SpawnWaypointAutoHeading();
        }

        internal static ManualLogSource Log;
        internal static bool showText_ = true;
        internal static WindSock windsock_;
        internal static EventManager eventManager_;
    }
}
