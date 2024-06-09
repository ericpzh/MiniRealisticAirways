using System;
using UnityEngine;

namespace MiniRealisticAirways
{
    public enum SpeedLevel
    {
        Stopped,
        Slow,
        Normal,
        Fast,
    }

    public class Speed: MonoBehaviour
    {
        public const float SPEED_DELTA = 2f; 

        public static float ToGameSpeed(SpeedLevel speed)
        {
            switch(speed)
            {
                case SpeedLevel.Slow:
                    return 20;
                case SpeedLevel.Normal:
                    return 24;
                case SpeedLevel.Fast:
                    return 28;
            }
            return 0;
        }

        public static SpeedLevel ToModSpeed(float speed)
        {
            if (speed < ToGameSpeed(SpeedLevel.Slow) - SPEED_DELTA)
            {
                return SpeedLevel.Stopped;
            }

            if (speed < ToGameSpeed(SpeedLevel.Normal) - SPEED_DELTA)
            {
                return SpeedLevel.Slow;
            }

            if (speed < ToGameSpeed(SpeedLevel.Fast) - SPEED_DELTA)
            {
                return SpeedLevel.Normal;
            }

            return SpeedLevel.Fast;
        }

        public static string ToString(SpeedLevel speed) 
        {
            switch(speed)
            {
                case SpeedLevel.Slow:
                    return "<";
                case SpeedLevel.Normal:
                    return "|";
                case SpeedLevel.Fast:
                    return ">";
            }
            return "";
        }

        public static bool InputSpeedUp() 
        {
            return Input.GetKeyDown(KeyCode.D) || 
                   (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") > 0f);
        }

        public static bool InputSlowDown() 
        {
            return Input.GetKeyDown(KeyCode.A) || 
                   (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") < 0f);
        }
    }

    public class AircraftSpeed : Speed
    {
        
        public bool CanLand(Weight weight)
        {
            if (weight == Weight.Light)
            {
                return ToModSpeed(aircraft_.targetSpeed) < SpeedLevel.Normal;
            }
            return ToModSpeed(aircraft_.targetSpeed) <= SpeedLevel.Normal;
        }

        public SpeedLevel MaxSpeed()
        {
            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftType != null && aircraftType.weight_ == Weight.Light)
            {
                // Light acraft only have max speed to Normal.
                return SpeedLevel.Normal;
            }
            return SpeedLevel.Fast;
        }

        public void AircraftSpeedUp()
        {
            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return;
            }

            if (aircraft_.targetSpeed < ToGameSpeed(MaxSpeed()))
            {
                aircraft_.targetSpeed = ToGameSpeed(ToModSpeed(aircraft_.targetSpeed) + 1);
            }
        }

        public void AircraftSlowDown()
        {
            if (aircraft_.targetSpeed > ToGameSpeed(SpeedLevel.Slow))
            {
                aircraft_.targetSpeed = ToGameSpeed(ToModSpeed(aircraft_.targetSpeed) - 1);
            }
        }

        override public string ToString()
        {
            float SpeedDiff = Math.Abs(aircraft_.speed - aircraft_.targetSpeed);
            if (Math.Abs(SpeedDiff) > SPEED_DELTA && 
                DateTimeOffset.Now.ToUnixTimeMilliseconds() % 500 < 250)
            {
                // Trainsitional blink.
                return " ";
            }

            return ToString(ToModSpeed(aircraft_.speed));
        }

        public SpeedLevel GetSpeed() 
        { 
            return ToModSpeed(aircraft_.speed); 
        }

        void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            // Initialize speed.
            aircraft_.TakeOffSpeedFactor = Speed.ToGameSpeed(SpeedLevel.Normal);

            if (aircraft_.direction == Aircraft.Direction.Outbound)
            {
                aircraft_.targetSpeed = Speed.ToGameSpeed(SpeedLevel.Normal);
            }
            if (aircraft_.direction == Aircraft.Direction.Inbound)
            {
                aircraft_.targetSpeed = Speed.ToGameSpeed(SpeedLevel.Normal);
            }
        }

        private void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }
                       
            if (Aircraft.CurrentCommandingAircraft == aircraft_)
            {
                if (InputSlowDown())
                {
                    AircraftSlowDown();
                }

                if (InputSpeedUp())
                {
                    AircraftSpeedUp();
                }
            }
        }

        public Aircraft aircraft_;
    }

    public class WaypointSpeed : Speed
    {
        
        override public string ToString()
        {
            return ToString(speed_);
        }

        private void Start()
        {
            speed_ = SpeedLevel.Normal;
        }

        private void Update()
        {
            if (waypoint_ == null)
            {
                Destroy(gameObject);
                return;
            }

            // Waypoint would hard follow mouse position when placed.
            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = ((Component)waypoint_).transform.position;
            if (waypointPosition.x == _mousePos.x &&  waypointPosition.y == _mousePos.y)
            {
                if (speed_ > SpeedLevel.Slow && InputSlowDown())
                {
                    speed_--;
                }

                if (speed_ < SpeedLevel.Fast && InputSpeedUp())
                {
                    speed_++;
                }
            }
        }

        public Waypoint waypoint_;

        public SpeedLevel speed_;
    }
}