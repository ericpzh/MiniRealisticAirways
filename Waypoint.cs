using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways
{

    public class WaypointState : MonoBehaviour
    {
        private void StartText(ref TMP_Text text, float x, float y, float z = 5f, float size = 2f,
                               HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Left)
        {
            GameObject obj = Instantiate(new GameObject("Text"));
            text = obj.AddComponent<TextMeshPro>();

            text.fontSize = size;
            text.horizontalAlignment = horizontalAlignment;
            text.verticalAlignment = VerticalAlignmentOptions.Top;
            text.rectTransform.sizeDelta = new Vector2(2, 1);
            obj.transform.SetParent(waypoint_.transform);
            obj.transform.localPosition = new Vector3(x, y, z);
            
            // make sorting layer of obj "Text"
            SortingGroup sg = obj.AddComponent<SortingGroup>();
            sg.sortingLayerName = "Text";
            sg.sortingOrder = 1;
        }

        private void Start()
        {
            if (waypoint_ == null)
            {
                return;
            }

            StartText(ref altitudeText_, 0.7f, -2f);
            StartText(ref speedText_, 2.75f, -2f);
            StartText(ref nameText_, 0f, 0.5f, 5f, 3f, HorizontalAlignmentOptions.Center);
        }

        private void Update()
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
           
            if (!Plugin.showText_)
            {
                altitudeText_.text = "";
                speedText_.text = "";
                nameText_.text = "";
                return;
            }

            WaypointAltitude waypointAltitude = waypoint_.GetComponent<WaypointAltitude>();
            WaypointSpeed waypointSpeed = waypoint_.GetComponent<WaypointSpeed>();
            if (!waypoint_.Invisible && waypointAltitude != null && waypointSpeed != null &&
                waypoint_ is BaseWaypointAutoHeading)
            {
                altitudeText_.text = "\nALT: " + waypointAltitude.ToString();
                speedText_.text = "\nSPD: " + waypointSpeed.ToString();
            }

            WaypointNameInput waypointNameInput = waypoint_.GetComponent<WaypointNameInput>();
            if (!waypoint_.Invisible && waypointNameInput != null)
            {
                nameText_.text = waypointNameInput.text;
            }
        }

        public PlaceableWaypoint waypoint_;
        private TMP_Text altitudeText_;
        private TMP_Text speedText_;
        private TMP_Text nameText_;
    }

    public class WaypointNameInput : MonoBehaviour
    {
        private void Update()
        {
            // Waypoint would hard follow mouse position when placed.
            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = waypoint_.transform.position;
            if (waypointPosition.x == _mousePos.x &&  waypointPosition.y == _mousePos.y)
            {
                Toggle();

                if (active)
                {
                    Type();
                }
            }
        }

        private void Toggle()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                active = !active;
            }
        }
        private void Type()
        {
            if (Input.GetKeyDown(KeyCode.Backspace) && text.Length > 0)
            {
                text = text.Substring(0, text.Length - 1);
            }
            else
            {
                foreach (char ch in Input.inputString)
                {
                    if ((Char.IsLetter(ch) || Char.IsNumber(ch)) && text.Length < MAX_LENGTH)
                    {
                        text += ch;
                        text = text.ToUpper();
                    }
                }
            }
        }

        public string text = "";
        public PlaceableWaypoint waypoint_;
        public bool active;
        private const int MAX_LENGTH = 5;
    }

    [HarmonyPatch(typeof(PlaceableWaypoint), "Start", new Type[] {})]
    class PatchPlaceableWaypointStart
    {
        static bool Prefix(ref PlaceableWaypoint __instance)
        {
            GameObject obj = GameObject.Instantiate(new GameObject("WaypointState"));
            WaypointState waypointState = __instance.gameObject.AddComponent<WaypointState>();
            waypointState.waypoint_ = __instance;
            waypointState.transform.SetParent(obj.transform);
            obj.transform.SetParent(__instance.transform);

            WaypointAltitude waypointAltitude = __instance.gameObject.AddComponent<WaypointAltitude>();
            waypointAltitude.waypoint_ = __instance;

            WaypointSpeed waypointSpeed = __instance.gameObject.AddComponent<WaypointSpeed>();
            waypointSpeed.waypoint_ = __instance;

            WaypointNameInput nameInput_ = __instance.gameObject.AddComponent<WaypointNameInput>();
            nameInput_.waypoint_ = __instance;

            return true;
        }
    }
}