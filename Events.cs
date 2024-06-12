using System;
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
            aircraftType.fuelOutTime_ = UnityEngine.Time.time + 1f * 300 /* Time per clock round */;
            aircraftType.lowFuelAircraft_ = true;
            return true;
        }
    }

    public class EventManager : MonoBehaviour
    {
        private void Start()
        {
            nextEventTime_ = GetNextEventTime();
            RunwayClose runwayClose = new RunwayClose();
            LowFuelArrival lowFuelArrival = new LowFuelArrival();
            events_ = new Event[] { runwayClose, lowFuelArrival };
            RandomizeEvents();
        }

        private void Update()
        {
            if (eventRestoreTime_ > 0 && UnityEngine.Time.time > eventRestoreTime_)
            {
                int lastEventIndex = GetIndex(index_ - 1);
                events_[lastEventIndex].Restore();
                eventRestoreTime_ = 0;
            }

            if (nextEventTime_ > 0 && UnityEngine.Time.time > nextEventTime_)
            {
                if (!events_[GetIndex(index_)].Trigger())
                {
                    nextEventTime_ = UnityEngine.Time.time + EVENT_RETRIGGER_INTERVAL;
                    return;
                }
                index_++;
                nextEventTime_ = UnityEngine.Time.time + GetNextEventTime();
                eventRestoreTime_ = UnityEngine.Time.time + EVENT_RESTORE_TIME;
            }
        }

        private int GetIndex(int index)
        {
            return Math.Abs(index % events_.Length);
        }

        private void RandomizeEvents()
        {   
            System.Random rng = new System.Random();
            int n = events_.Length;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                Event event_ = events_[k];  
                events_[k] = events_[n];  
                events_[n] = event_;  
            }  
        }

        private float GetNextEventTime()
        {
            return UnityEngine.Time.time + EVENT_BASE_TIME + 
                2 * EVENT_RANDOM_TIME_OFFSET_LIMIT * UnityEngine.Random.value - EVENT_RANDOM_TIME_OFFSET_LIMIT;
        }

        public static Runway closedRunway_ = null;
        public static Aircraft stoppedAircraft_ = null;
        private Event[] events_;
        private int index_ = 0;
        private float nextEventTime_ = 0;
        private float eventRestoreTime_ = 0;
        private const float EVENT_BASE_TIME = 8f * 300f /* Time per day */;
        private const float EVENT_RANDOM_TIME_OFFSET_LIMIT = 1f * 300f /* Time per day */;
        private const float EVENT_RETRIGGER_INTERVAL = 1f;
        private const float EVENT_RESTORE_TIME = 0.33f * 300f /* Time per day */;
    }
}