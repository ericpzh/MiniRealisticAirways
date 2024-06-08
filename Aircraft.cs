using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways
{
    public class AircraftState : MonoBehaviour
    {

        public AircraftAltitude aircraftAltitude_;
        public AircraftSpeed aircraftSpeed_;
        public AircraftType aircraftType_;
        public Aircraft aircraft_;
        private TMP_Text altitudeText_;
        private TMP_Text speedText_;

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
            if (aircraft_ == null) 
            {
                return;
            }

            StartText(ref altitudeText_, 1, -2f, 5);
            StartText(ref speedText_, 2.75f, -2f, 5);

            // Initialize states.
            aircraftAltitude_ = aircraft_.gameObject.AddComponent<AircraftAltitude>();
            aircraftAltitude_.aircraft_ = aircraft_;

            aircraftSpeed_ = aircraft_.gameObject.AddComponent<AircraftSpeed>();
            aircraftSpeed_.aircraft_ = aircraft_;

            aircraftType_ = aircraft_.gameObject.AddComponent<AircraftType>();
            aircraftType_.aircraft_ = aircraft_;
        }

        void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }
            
            if (altitudeText_ == null || speedText_ == null)
            {
                return;
            }
           
            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            if (aircraftState == null) 
            {
                return;
            }
            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            if (aircraftAltitude != null && aircraftSpeed != null && aircraftAltitude.altitude_ > AltitudeLevel.Ground)
            {
                altitudeText_.text = "\nALT: " + aircraftAltitude.ToString(); 
                speedText_.text = "\nSPD: " + aircraftSpeed.ToString();
            }
        }
    }
}
