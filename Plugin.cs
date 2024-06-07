using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MiniRealisticAirways
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;
    
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
                AircraftManager.Instance != null)
            {
                Logger.LogInfo("Hooking AircraftManager");
                AircraftManager.Instance.AircraftCreateEvent.AddListener(HookAircraft);
            }
        }
        
        private void HookAircraft(Vector2 pos, Aircraft aircraft)
        {
            Logger.LogInfo("Aircraft created: " + aircraft.name);
            
            GameObject obj = GameObject.Instantiate(new GameObject("AircraftStatHelper"));
            AircraftStatHelper cp = aircraft.gameObject.AddComponent<AircraftStatHelper>();
            cp.aircraft_ = aircraft;
            cp.transform.SetParent(obj.transform);
            obj.transform.SetParent(aircraft.transform);
            
            AircraftAltitude altitude = aircraft.gameObject.AddComponent<AircraftAltitude>();
            altitude.aircraft_ = aircraft;

            AircraftSpeed speed = aircraft.gameObject.AddComponent<AircraftSpeed>();
            speed.aircraft_ = aircraft;
        }
    }

    public class AircraftStatHelper : MonoBehaviour
    {
        private TMP_Text altitudeText_;
        private TMP_Text speedText_;
    
        public Aircraft aircraft_;
        bool inited_ = false;

        private void StartText(ref TMP_Text text, float x, float y, float z)
        {
            GameObject obj = GameObject.Instantiate(new GameObject("Text"));
            text = obj.AddComponent<TextMeshPro>();

            
            text.fontSize = 2f;
            text.horizontalAlignment = HorizontalAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Top;
            text.rectTransform.sizeDelta = new Vector2(2, 1);
            obj.transform.SetParent(aircraft_.transform);
            obj.transform.localPosition = new Vector3(x, y, z);
            
            // make sorting layer of obj "Text"
            SortingGroup sg = obj.AddComponent<SortingGroup>();
            sg.sortingLayerName = "Text";
            sg.sortingOrder = 1;
        }

        void Start()
        {
            StartText(ref altitudeText_, 1, -2f, 5);
            StartText(ref speedText_, 2.75f, -2f, 5);
            inited_ = true;

            if (aircraft_ == null) return;

            aircraft_.TakeOffSpeedFactor = AircraftSpeed.ToGameSpeed(Speed.Normal);

            if (aircraft_.direction == Aircraft.Direction.Outbound) {
                aircraft_.targetSpeed = AircraftSpeed.ToGameSpeed(Speed.Normal);
            }
            if (aircraft_.direction == Aircraft.Direction.Inbound) {
                aircraft_.targetSpeed = AircraftSpeed.ToGameSpeed(Speed.Normal);
            }
        }

        void Update()
        {
            if (aircraft_ == null)
                Destroy(gameObject);
            
            if (!inited_ || altitudeText_ == null || speedText_ == null)
            {
                return;
            }
           
            AircraftAltitude altitude = aircraft_.GetComponent<AircraftAltitude>();
            AircraftSpeed speed = aircraft_.GetComponent<AircraftSpeed>();
            if (altitude != null && speed != null && altitude.altitude_ > Altitude.Ground)
            {
                altitudeText_.text = "\nALT: " + altitude.ToString(); 
                speedText_.text = "\nSPD: " + speed.ToString();       
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "TrySetupLanding", new Type[] {typeof(Runway), typeof(bool)})]
    class PatchTrySetupLanding
    {
        static bool Prefix(Runway runway, bool doLand, ref Aircraft __instance)
        {
            AircraftAltitude altitude = __instance.GetComponent<AircraftAltitude>();
            AircraftSpeed speed = __instance.GetComponent<AircraftSpeed>();
            if (!altitude.CanLand() || !speed.CanLand())
            {
                Runway runway2 = (runway ? runway : Aircraft.CurrentCommandingRunway);

                // Reflex for  __instance.LandingRunway;
                Runway LandingRunway = __instance.GetFieldValue<Runway>("LandingRunway");
                bool flag = __instance.state == Aircraft.State.Landing && runway2 == LandingRunway;

                // Reflex for bool flag2 = __instance.GenerateLandingPathL1(runway2, out path, flag);
                MethodInfo GenerateLandingPathL1 = typeof(Aircraft).GetMethod("GenerateLandingPathL1", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                object[] args = new object[] { runway2, null, flag, true };
                GenerateLandingPathL1.Invoke(__instance, args);
                List<Vector3> path = (List<Vector3>)args[1];

                // Reflex for  __instance.ShowPath(path, false /* success */);
                MethodInfo ShowPath = __instance.GetType().GetMethod("ShowPath", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                ShowPath.Invoke(__instance, new object[] { path, false /* success */ });

                __instance.aircraftVoiceAndSubtitles.PlayAngleTooSteep();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(WaypointAutoLanding), "OnLeavingFrom", new Type[] {typeof(Aircraft)})]
    class PatchOnLeavingFrom
    {
        static bool Prefix(Aircraft aircraft, ref WaypointAutoLanding __instance, ref Runway ____targetRunway)
        {
            if (____targetRunway != null)
            {
                Plugin.Log.LogInfo("Landing waypoint reached.");
                // Stablize the approach first before trying to land.
                AircraftAltitude altitude = aircraft.GetComponent<AircraftAltitude>();
                while (altitude != null && !altitude.CanLand()) {
                    altitude.AircraftDesend(); 
                }

                AircraftSpeed speed = aircraft.GetComponent<AircraftSpeed>();
                while (speed != null && !speed.CanLand()) {
                    speed.AircraftSlowDown(); 
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "FixedUpdate", new Type[] {})]
    class PatchFixedUpdate
    {
        static bool Prefix(ref Aircraft __instance)
        {
            // Continue to update speed while landing.
            if (__instance.state != Aircraft.State.Crashed &&
                __instance.state == Aircraft.State.Landing)
            {
                __instance.Invoke("UpdateSpeed", 0);
            }
            return true;
        }
    }
}
