using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways
{
    public class AircraftState : MonoBehaviour
    {
        public bool IsLanding()
        {
            return aircraft_.state == Aircraft.State.Landing;
        }

        private void StartText(ref TMP_Text text, float fontSize, float x, float y, float z)
        {
            GameObject obj = GameObject.Instantiate(new GameObject("Text"));
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

        void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            StartText(ref altitudeText_,        2f, 0.4f, -3.6f, 5f);
            StartText(ref speedText_,           2f, 2.5f, -3.6f, 5f);
            StartText(ref altitudeLevelText_, 3.5f, 1.5f, -3.3f, 5f);
            StartText(ref speedLevelText_,    3.5f,   4f, -3.3f, 5f);
            StartText(ref fuelText_,          2.1f, 0.4f,   -4.6f, 5f);
            StartText(ref weightText_,        2.5f,   3f,   -4.5f, 5f);

            if (aircraft_.direction == Aircraft.Direction.Inbound)
            {
                // Add fuel gauge to only arrivals.
                fuelGauge_ = aircraft_.gameObject.AddComponent<FuelGauge>();
                fuelGauge_.aircraft_ = aircraft_;
            }
            altitudeGauge_ = aircraft_.gameObject.AddComponent<AltitudeGauge>();
            altitudeGauge_.aircraft_ = aircraft_;
            speedGauge_ = aircraft_.gameObject.AddComponent<SpeedGauge>();
            speedGauge_.aircraft_ = aircraft_;
        }

        void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }


            if (altitudeText_ == null || speedText_ == null || altitudeLevelText_ == null || 
                speedLevelText_ == null || weightText_ == null || fuelText_ == null)
            {
                return;
            }

            if (!Plugin.showText_)
            {
                altitudeText_.text = "";
                speedText_.text = "";
                altitudeLevelText_.text = ""; 
                speedLevelText_.text = ""; 
                fuelText_.text = "";
                weightText_.text = "";
                return;
            }

            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            if (aircraftState == null) 
            {
                return;
            }
            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftAltitude != null && aircraftSpeed != null && aircraftType != null && 
                aircraftAltitude.altitude_ > AltitudeLevel.Ground)
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
        private bool showText_ = false;
        private TMP_Text altitudeText_;
        private TMP_Text speedText_;
        private TMP_Text altitudeLevelText_;
        private TMP_Text speedLevelText_;
        private TMP_Text fuelText_;
        private TMP_Text weightText_;
        private FuelGauge fuelGauge_;
        private AltitudeGauge altitudeGauge_;
        private SpeedGauge speedGauge_;
    }
}
