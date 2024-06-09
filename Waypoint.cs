using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways
{

    public class WaypointState : MonoBehaviour
    {
        private TMP_Text altitudeText_;
        private TMP_Text speedText_;
    
        public Waypoint waypoint_;

        private void StartText(ref TMP_Text text, float x, float y, float z)
        {
            GameObject obj = GameObject.Instantiate(new GameObject("Text"));
            text = obj.AddComponent<TextMeshPro>();

            
            text.fontSize = 2f;
            text.horizontalAlignment = HorizontalAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Top;
            text.rectTransform.sizeDelta = new Vector2(2, 1);
            obj.transform.SetParent(waypoint_.transform);
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

            if (waypoint_ == null) return;
        }

        void Update()
        {
            if (waypoint_ == null)
            {
                Destroy(gameObject);
                return;
            }
            
            if (altitudeText_ == null || speedText_ == null)
            {
                return;
            }
           
            WaypointAltitude waypointAltitude = waypoint_.GetComponent<WaypointAltitude>();
            WaypointSpeed waypointSpeed = waypoint_.GetComponent<WaypointSpeed>();
            if (!waypoint_.Invisible &&
                waypointAltitude != null &&
                waypointSpeed != null &&
                waypoint_ is BaseWaypointAutoHeading)
            {
                altitudeText_.text = "\nALT: " + waypointAltitude.ToString(); 
                speedText_.text = "\nSPD: " + waypointSpeed.ToString();       
            }
        }
    }

    [HarmonyPatch(typeof(PlaceableWaypoint), "Start", new Type[] {})]
    class PatchStart
    {
        static bool Prefix(ref PlaceableWaypoint __instance)
        {
            Plugin.Log.LogInfo("PlaceableWaypoint created.");

            GameObject obj = GameObject.Instantiate(new GameObject("WaypointState"));
            WaypointState waypointState = __instance.gameObject.AddComponent<WaypointState>();
            waypointState.waypoint_ = __instance;
            waypointState.transform.SetParent(obj.transform);
            obj.transform.SetParent(__instance.transform);
            
            WaypointAltitude waypointAltitude = __instance.gameObject.AddComponent<WaypointAltitude>();
            waypointAltitude.waypoint_ = __instance;

            WaypointSpeed waypointSpeed = __instance.gameObject.AddComponent<WaypointSpeed>();
            waypointSpeed.waypoint_ = __instance;
            
            return true;
        }
    }
}