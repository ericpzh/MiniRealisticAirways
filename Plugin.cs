using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniRealisticAirways
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
    
        private void Awake()
        {
            Log = Logger;

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
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
            }
        }
        
        private void OnDestroy()
        {
            FuelGaugeTextures.DestoryTextures();
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
    }
}
