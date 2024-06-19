using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiniRealisticAirways
{
    public class Event : MonoBehaviour
    {
        public virtual bool Trigger() 
        {
            return true;
        }

        public virtual void Restore() {}
    }

    public class RunwayClose : Event
    {
        public override bool Trigger()
        {
            Plugin.Log.LogWarning("RunwayClose Triggered.");
            foreach (Aircraft aircraft in AircraftManager.GetOutboundAircraft())
            {
                if (aircraft.state == Aircraft.State.TakingOff && 
                    aircraft.takeOffRunway != null)
                {
                    aircraft_ = aircraft;
                    break;
                }
            }

            if (aircraft_ == null)
            {
                return false;
            }

            Runway runway = aircraft_.takeOffRunway;
            if (runway == null)
            {
                return false;
            }
            runway_ = runway;

            // Color the runway red.
            Renderer renderer = runway.Square.GetComponent<Renderer>();
            if (renderer == null)
            {
                return false;
            }
            Material material = renderer.material;
            if (material == null)
            {
                return false;
            }
            runwayColor_ = new Color(material.color.r, material.color.g, material.color.b, material.color.a);
            material.color = new Color(0.7f, 0, 0);

            EventManager.closedRunway_ = runway_;
            EventManager.stoppedAircraft_ = aircraft_;

            aircraft_.TakeOffSpeedFactor = 0f;
            aircraft_.AP.GetComponent<Renderer>().material.color = new Color(0.7f, 0, 0, 0.3f);
            aircraft_.Panel.GetComponent<Renderer>().material.color = new Color(0.7f, 0, 0, 0.3f);
            return true;
        }

        public override void Restore()
        {
            Plugin.Log.LogWarning("RunwayClose Restoreed.");
            if (runway_ == null)
            {
                return;
            }

            // Restore runway color.
            Renderer renderer = runway_.Square.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }
            Material material = renderer.material;
            if (material == null)
            {
                return;
            }
            material.color = runwayColor_;

            runway_ = null;
            EventManager.closedRunway_ = null;

            if (aircraft_ == null)
            {
                return;
            }
            aircraft_.ConditionalDestroy();

            aircraft_ = null;
            EventManager.stoppedAircraft_ = null;
        }

        public Runway runway_;
        private Color runwayColor_;
        private Aircraft aircraft_;
    }

    public class LowFuelArrival : Event
    {
        public override bool Trigger()
        {
            Plugin.Log.LogWarning("LowFuelArrival Triggered.");
            Aircraft aircraft_ = null;
            foreach (Aircraft aircraft in AircraftManager.GetInboundAircraft())
            {
                Vector2 vector = Camera.main.WorldToViewportPoint(aircraft.gameObject.transform.position);
                bool Inbound = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
                if (!Inbound)
                {
                    aircraft_ = aircraft;
                    break;
                }
            }

            if (aircraft_ == null)
            {
                return false;
            }

            AircraftType aircraftType;
            if (!AircraftState.GetAircraftStates(aircraft_, out _, out _, out aircraftType))
            {
                return false;
            }

            Plugin.Log.LogInfo("Generated emergency low fuel aircraft.");
            aircraftType.percentFuelLeft_ = AircraftType.LOW_FUEL_WARNING_PERCENT;
            return true;
        }
    }

    public class BadWeather : Event
    {
        public override bool Trigger()
        {
            Plugin.Log.LogWarning("BadWeather Triggered.");
            GameObject esc_button = GameObject.Find("ESC_Button");
            if (esc_button == null)
            {
                return false;
            }

            weather_ = esc_button.gameObject.AddComponent<Weather>();
            if (weather_ == null)
            {
                return false;
            }

            weather_.enabled = true;
            EventManager.weather_ = weather_;
            return true;
        }

        public override void Restore()
        {
            Plugin.Log.LogWarning("BadWeather Restoreed.");
            weather_.DestoryWeather();
            EventManager.weather_ = null;
            weather_ = null;
        }

        Weather weather_;
    }

    public class EngineOut : Event
    {
        public override bool Trigger()
        {
            Plugin.Log.LogWarning("EngineOut Triggered.");
            Aircraft aircraft = null;
            foreach (Aircraft aircraft_ in AircraftManager.GetOutboundAircraft())
            {
                if (aircraft_.state == Aircraft.State.TakingOff)
                {
                    aircraft = aircraft_;
                    break;
                }
            }

            if (aircraft == null)
            {
                return false;
            }

            StartCoroutine(EngineOutCoroutine(aircraft));

            return true;
        }

        private IEnumerator EngineOutCoroutine(Aircraft aircraft)
        {
            InitCallSign();

            while(aircraft == null || aircraft.state == Aircraft.State.TakingOff)
            {
                yield return new WaitForSeconds(1f);
            }

            yield return new WaitForSeconds(
                RANDOM_BASE_TIME + 2 * RANDOM_TIME_OFFSET_LIMIT * UnityEngine.Random.value - RANDOM_TIME_OFFSET_LIMIT);

            // Recored original state and disable aircraft update.
            AircraftAltitude aircraftAltitude;
            AircraftSpeed aircraftSpeed;
            AircraftType aircraftType;
            while (!AircraftState.GetAircraftStates(aircraft, out aircraftAltitude, out aircraftSpeed, out aircraftType))
            {
                yield return new WaitForFixedUpdate();
            }
            
            aircraftAltitude.tcasAction_ = TCASAction.Disabled;
            aircraftAltitude.altitudeDisabled_ = true;
            aircraftSpeed.speedDisabled_ = true;

            // Start event.
            ShowCallSign(aircraft);
            yield return TextDisplayCoroutine(aircraft, "Mayday, Mayday, Mayday!", 3f, 2f);
            yield return TextDisplayCoroutine(aircraft, callSign_ + ". We have one engine failure.", 5f, 3f);
            yield return TextDisplayCoroutine(aircraft, "We need to return to the field immediately.", 5f, 3f);

            // Record all information before destory.
            Vector3 position = aircraft.AP.transform.position;
            float heading = aircraft.heading;
            AltitudeLevel altitude = aircraftAltitude.altitude_;
            AltitudeLevel targetAltitude = aircraftAltitude.targetAltitude_;
            float speed = aircraft.speed;
            float targetSpeed = aircraft.targetSpeed;
            Weight weight = aircraftType.weight_;

            // Swap.
            aircraft.ConditionalDestroy();
            aircraft = AircraftManager.Instance.CreateInboundAircraft(position, heading);

            // Copy aircraft states over.
            while (!AircraftState.GetAircraftStates(aircraft, out aircraftAltitude, out aircraftSpeed, out aircraftType))
            {
                yield return new WaitForFixedUpdate();
            }

            // Copy altitude over.
            aircraftAltitude.altitude_ = altitude;
            aircraftAltitude.targetAltitude_ = targetAltitude;
            aircraftAltitude.tcasAction_ = TCASAction.Disabled;
            if (aircraftAltitude.enableAltitudeGaugeCoroutine_ != null)
            {
                StopCoroutine(aircraftAltitude.enableAltitudeGaugeCoroutine_);
            }
            aircraftAltitude.enableAltitudeGaugeCoroutine_ = aircraftAltitude.EnableAltitudeGauge(aircraftAltitude.altitude_);
            StartCoroutine(aircraftAltitude.enableAltitudeGaugeCoroutine_);

            // Copy speed over.
            aircraft.speed = speed;
            aircraft.targetSpeed = targetSpeed;
            if (aircraftSpeed.enableSpeedGaugeCoroutine_ != null)
            {
                StopCoroutine(aircraftSpeed.enableSpeedGaugeCoroutine_);
            }
            aircraftSpeed.enableSpeedGaugeCoroutine_ = aircraftSpeed.EnableSpeedGauge(Speed.ToModSpeed(aircraft.speed));
            StartCoroutine(aircraftSpeed.enableSpeedGaugeCoroutine_);

            // Copy type over.
            aircraftType.weight_ = weight;
            aircraftType.percentFuelLeft_ = AircraftType.LOW_FUEL_WARNING_PERCENT / 2;
            StartCoroutine(aircraftType.DisableFuelGaugeCoroutine());

            ShowCallSign(aircraft);
        }

        private void ShowCallSign(Aircraft aircraft)
        {
            if (aircraft.callsignText != null)
            {
                aircraft.ShowCallSign(true);
                aircraft.callsignText.text = callSign_;
            }
        }

        private IEnumerator TextDisplayCoroutine(Aircraft aircraft, string text, float dialogueLength, float disappearTime)
        {
            // Setup text.
            GameObject obj = Instantiate(new GameObject("Text"));
            obj.transform.SetParent(aircraft.transform);
            obj.transform.localPosition = new Vector3(0f, 2f, -9f);
            TMP_Text dialogue = obj.AddComponent<TextMeshPro>();
            dialogue.fontSize = 4f;
            dialogue.horizontalAlignment = HorizontalAlignmentOptions.Center;
            dialogue.verticalAlignment = VerticalAlignmentOptions.Top;
            dialogue.rectTransform.sizeDelta = new Vector2(10, 1);

            // Start playing.
            dialogue.gameObject.SetActive(value: true);
            dialogue.text = "";
            dialogue.color = Color.white;
            int textLength = text.Length;
            float timePassed = 0f;
            while (timePassed < dialogueLength)
            {
                dialogue.text = text.Substring(0, (int)((float)textLength * (timePassed / dialogueLength)));
                timePassed += Time.unscaledDeltaTime * 1.75f;
                yield return null;
            }
            dialogue.text = text;
            yield return new WaitForSeconds(disappearTime);
            dialogue.DOFade(0f, 1f).SetUpdate(isIndependentUpdate: true);
            yield return new WaitForSeconds(1f);
            dialogue.gameObject.SetActive(value: false);
        }

        private void InitCallSign()
        {
            callSign_ = "CA" + UnityEngine.Random.Range(1000, 9999);
        }

        private string callSign_ = "CA3115";
        private const float RANDOM_BASE_TIME = 15f;
        private const float RANDOM_TIME_OFFSET_LIMIT = 5f;
    }

    public class EventManager : MonoBehaviour
    {

        private IEnumerator StartEventCoroutine()
        {
            yield return new WaitForSeconds(
                EVENT_BASE_TIME + 2 * EVENT_RANDOM_TIME_OFFSET_LIMIT * UnityEngine.Random.value - EVENT_RANDOM_TIME_OFFSET_LIMIT);

            while (!events_[GetIndex(index_)].Trigger())
            {
                yield return new WaitForSeconds(EVENT_RETRIGGER_INTERVAL);
            }

            yield return new WaitForSeconds(EVENT_RESTORE_TIME);

            events_[GetIndex(index_)].Restore();
            index_++;

            yield return StartEventCoroutine();
        }

        private void Start()
        {
            EngineOut engineOutEvent = gameObject.AddComponent<EngineOut>();
            events_ = new List<Event>{engineOutEvent, new RunwayClose(), new LowFuelArrival(), new BadWeather()};
            Utils.Shuffle(ref events_);
            Plugin.Log.LogInfo("Event setup completed.");
            StartCoroutine(StartEventCoroutine());
        }

        private int GetIndex(int index)
        {
            return Math.Abs(index % events_.Count);
        }

        public static Runway closedRunway_ = null;
        public static Aircraft stoppedAircraft_ = null;
        public static Weather weather_ = null;
        public const float EVENT_RESTORE_TIME = 0.33f * 300f /* Time per day */;
        private List<Event> events_;
        private int index_ = 0;
        private const float EVENT_BASE_TIME = 6f * 300f /* Time per day */;
        private const float EVENT_RANDOM_TIME_OFFSET_LIMIT = 1f * 300f /* Time per day */;
        private const float EVENT_RETRIGGER_INTERVAL = 1f;
    }
}