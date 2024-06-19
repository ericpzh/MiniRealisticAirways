using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways
{
    public class AircraftState : MonoBehaviour
    {
        public static bool DisableStateOnTouchedDown(Aircraft aircraft)
        {
            return aircraft.direction == Aircraft.Direction.Inbound && aircraft.state == Aircraft.State.TouchedDown;
        }

        public static bool GetAircraftState(Aircraft aircraft, out AircraftState aircraftState)
        {
            aircraftState = aircraft.GetComponent<AircraftState>();
            return aircraftState != null;
        }

        public static bool GetAircraftStates(Aircraft aircraft, out AircraftAltitude aircraftAltitude,
                                             out AircraftSpeed aircraftSpeed, out AircraftType aircraftType)
        {
            aircraftAltitude = null;
            aircraftSpeed = null;
            aircraftType = null;
    
            AircraftState aircraftState;
            if (!GetAircraftState(aircraft, out aircraftState))
            {
                return false;
            }

            aircraftAltitude = aircraftState.aircraftAltitude_;
            aircraftSpeed = aircraftState.aircraftSpeed_;
            aircraftType = aircraftState.aircraftType_;
            return aircraftAltitude != null && aircraftSpeed != null && aircraftType != null;
        }

        public void Initialize()
        {
            if (aircraft_ == null)
            {
                return;
            }

            // Initialize states.
            aircraftAltitude_ = aircraft_.gameObject.AddComponent<AircraftAltitude>();
            aircraftAltitude_.aircraft_ = aircraft_;

            aircraftSpeed_ = aircraft_.gameObject.AddComponent<AircraftSpeed>();
            aircraftSpeed_.aircraft_ = aircraft_;

            aircraftType_ = aircraft_.gameObject.AddComponent<AircraftType>();
            aircraftType_.aircraft_ = aircraft_;
        }

        public IEnumerator DelayDestoryCoroutine()
        {
            yield return new WaitForSeconds(5f);
            aircraft_.ConditionalDestroy();
        }

        public bool IsAirborne()
        {
            if (aircraftAltitude_ == null)
            {
                return false;
            }
            return aircraftAltitude_.altitude_ > AltitudeLevel.Ground;
        }

        private void StartText(ref TMP_Text text, float fontSize, float x, float y, float z)
        {
            GameObject obj = Instantiate(new GameObject("Text"));
            text = obj.AddComponent<TextMeshPro>();
            
            text.fontSize = fontSize;
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

        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            StartText(ref altitudeText_,        2f, 0.4f, -3.6f, 5f);
            StartText(ref speedText_,           2f, 2.5f, -3.6f, 5f);
            StartText(ref altitudeLevelText_, 3.5f, 0.2f, -1.3f, 5f);
            StartText(ref speedLevelText_,    3.5f,   4f, -3.3f, 5f);
            StartText(ref fuelText_,          2.1f, 0.4f, -4.6f, 5f);
            StartText(ref weightText_,        2.5f,   3f, -4.5f, 5f);
            // Hacking on altitue level's text here.
            altitudeLevelText_.transform.localScale = new Vector3(1.5f,
                                                                  altitudeLevelText_.transform.localScale.y, 
                                                                  altitudeLevelText_.transform.localScale.z);
            altitudeLevelText_.transform.rotation = Quaternion.AngleAxis(270, Vector3.back);
        }

        private void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }

            if (TimeManager.Instance.Paused)
            {
                // Skip update during time pause.
                return;
            }

            if (altitudeText_ == null || speedText_ == null || altitudeLevelText_ == null || 
                speedLevelText_ == null || weightText_ == null || fuelText_ == null)
            {
                return;
            }

            if (!Plugin.showText_ || DisableStateOnTouchedDown(aircraft_))
            {
                altitudeText_.text = "";
                speedText_.text = "";
                altitudeLevelText_.text = ""; 
                speedLevelText_.text = ""; 
                fuelText_.text = "";
                weightText_.text = "";
                return;
            }

            AircraftAltitude aircraftAltitude;
            AircraftSpeed aircraftSpeed;
            AircraftType aircraftType;
            if (!GetAircraftStates(aircraft_, out aircraftAltitude, out aircraftSpeed, out aircraftType))
            {
                return;
            }

            if (IsAirborne())
            {
                altitudeText_.text = "ALT: ";
                speedText_.text = "SPD: ";
                altitudeLevelText_.text = aircraftAltitude.ToString(); 
                speedLevelText_.text = aircraftSpeed.ToString();
                fuelText_.text = aircraftType.GetFuelString();
                weightText_.text = aircraftType.weight_.ToString();
            }
        }

        public AircraftAltitude aircraftAltitude_;
        public AircraftSpeed aircraftSpeed_;
        public AircraftType aircraftType_;
        public Aircraft aircraft_;
        public PlaceableWaypoint commandingWaypoint_;
        public bool weatherAffected_ = false;
        private TMP_Text altitudeText_;
        private TMP_Text speedText_;
        private TMP_Text altitudeLevelText_;
        private TMP_Text speedLevelText_;
        private TMP_Text fuelText_;
        private TMP_Text weightText_;
    }
}
