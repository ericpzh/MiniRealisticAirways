using System;
using System.Collections;
using System.Collections.Generic;
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

            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return false;
            }

            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftType == null)
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

            Plugin.Log.LogInfo("Weather enabled.");
            weather_.enabled = true;
            EventManager.weather_ = weather_;
            return true;
        }

        public override void Restore()
        {
            Plugin.Log.LogInfo("Weather disabled.");
            weather_.DestoryWeather();
            EventManager.weather_ = null;
            weather_ = null;
        }

        Weather weather_;
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
            events_ = new List<Event>{new RunwayClose(), new LowFuelArrival(), new BadWeather()};
            Utils.Shuffle(ref events_);

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