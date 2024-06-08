using System;
using UnityEngine;

namespace MiniRealisticAirways
{
    public enum AltitudeLevel
    {
        Ground,
        Low,
        Normal,
        High,
    }

    public class Altitude : MonoBehaviour
    {
        public static string ToString(AltitudeLevel altitude) {
            switch(altitude)
            {
                case AltitudeLevel.Low:
                    return "v";
                case AltitudeLevel.Normal:
                    return "—";
                case AltitudeLevel.High:
                    return "^";
            }
            return "";
        }
    }

    public class AircraftAltitude : Altitude
    {
        override public string ToString() {
            if (altitude_ != targetAltitude_ && transitionTimer_ % 50 < 25) {
                // Trainsitional blink.
                return " ";
            }

            return ToString(altitude_);
        }

        public bool CanLand() {
            return targetAltitude_ <= AltitudeLevel.Low;
        }

        public void AircraftClimb() {
            if (targetAltitude_ < AltitudeLevel.High)
            {
                targetAltitude_ ++;
                transitionTimer_ = REACTION_TIME + TRANSITION_TIME * Math.Abs(targetAltitude_ - altitude_);
            }
        }

        public void AircraftDesend() {
            if (targetAltitude_ > AltitudeLevel.Low)
            {
                targetAltitude_ --;
                transitionTimer_ = REACTION_TIME + TRANSITION_TIME * Math.Abs(targetAltitude_ - altitude_);
            }
        }

        public Aircraft aircraft_;

        public AltitudeLevel altitude_ { get; private set; }
        public AltitudeLevel targetAltitude_ { get; private set; }
        private const int TRANSITION_TIME = 200;
        private const int REACTION_TIME = 100;
        private int transitionTimer_ = 0;
        
        private void Start()
        {
            if (aircraft_.direction == Aircraft.Direction.Outbound)
            {
                altitude_ = AltitudeLevel.Ground;
                targetAltitude_ = AltitudeLevel.Low;
            }

            if (aircraft_.direction == Aircraft.Direction.Inbound)
            {
                altitude_ = AltitudeLevel.High;
                targetAltitude_ = AltitudeLevel.High;
            }
        }

        private void Update()
        {
            if (aircraft_ == null) {
                Destroy(gameObject);
                return;
            }
            
            TakeoffTouchdownProcess();
            AltitudeUpdate();
            
            if (Aircraft.CurrentCommandingAircraft == aircraft_)
            {
                if (Input.GetKeyDown(KeyCode.W))
                {
                    AircraftClimb();
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    AircraftDesend();
                }
            }
        }

        private void TakeoffTouchdownProcess()
        {
            if (altitude_ == AltitudeLevel.Ground && aircraft_.direction == Aircraft.Direction.Outbound &&
                (aircraft_.state == Aircraft.State.Flying || aircraft_.state == Aircraft.State.HeadingAfterReachingWaypoint))
            {
                altitude_ = AltitudeLevel.Low;
                targetAltitude_ = AltitudeLevel.Low;
            }

            if (altitude_ != AltitudeLevel.Ground && aircraft_.direction == Aircraft.Direction.Inbound &&
                aircraft_.state == Aircraft.State.TouchedDown)
            {
                altitude_ = AltitudeLevel.Ground;
                targetAltitude_ = AltitudeLevel.Ground;
            }
        }

        private void AltitudeUpdate()
        {
            if (altitude_ == AltitudeLevel.Ground)
            {
                return;
            }

            if (--transitionTimer_ < 0)
            {
                altitude_ = targetAltitude_;
            }
        }

    }
    
    public class WaypointAltitude : Altitude
    {
        override public string ToString() {
            return ToString(altitude_);
        }

        public Waypoint waypoint_;

        public AltitudeLevel altitude_ { get; private set; }
        
        private void Start()
        {
            altitude_ = AltitudeLevel.Normal;
        }

        private void Update()
        {
            if (waypoint_ == null) {
                Destroy(gameObject);
                return;
            }
            
            // Waypoint would hard follow mouse position when placed.
            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = ((Component)waypoint_).transform.position;
            if (waypointPosition.x == _mousePos.x &&  waypointPosition.y == _mousePos.y)
            {
                if (altitude_ < AltitudeLevel.High && (
                    Input.GetKeyDown(KeyCode.W) || 
                    (!Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") > 0f)))
                {
                    altitude_++;
                }

                if (altitude_ > AltitudeLevel.Low && (
                    Input.GetKeyDown(KeyCode.S) ||
                    (!Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") < 0f)))
                {
                    altitude_--;
                }
            }
        }
    }
}