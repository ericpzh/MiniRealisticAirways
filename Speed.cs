using System;
using System.Collections;
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
                    return ">";
                case SpeedLevel.Normal:
                    return ">>";
                case SpeedLevel.Fast:
                    return ">>>";
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
                float targetSpeed = ToGameSpeed(ToModSpeed(aircraft_.targetSpeed) + 1);
                aircraft_.targetSpeed = targetSpeed;
                if (!transitioning_)
                {
                    StartCoroutine(SpeedTransitionCoroutine(targetSpeed));
                }
            }
        }

        public void AircraftSlowDown()
        {
            if (aircraft_.targetSpeed > ToGameSpeed(SpeedLevel.Slow))
            {
                float targetSpeed = ToGameSpeed(ToModSpeed(aircraft_.targetSpeed) - 1);
                aircraft_.targetSpeed = targetSpeed;
                if (!transitioning_)
                {
                    StartCoroutine(SpeedTransitionCoroutine(targetSpeed));
                }
            }
        }

        override public string ToString()
        {
            if (InTransition(aircraft_.targetSpeed) && Animation.Blink())
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

        private bool InTransition(float targetSpeed)
        {
            float SpeedDiff = Math.Abs(aircraft_.speed - targetSpeed);
            return SpeedDiff > SPEED_DELTA;
        }

        private IEnumerator SpeedTransitionCoroutine(float targetSpeed)
        {
            transitioning_ = true;

            while (!speedGauge_.Ready())
            {
                yield return new WaitForFixedUpdate();
            }

            IEnumerator blinkCoroutine = speedGauge_.GetTransitioningCoroutine(
                ToModSpeed(aircraft_.speed), ToModSpeed(targetSpeed));
            StartCoroutine(blinkCoroutine);

            while (InTransition(targetSpeed))
            {
                yield return new WaitForFixedUpdate();
            }

            StopCoroutine(blinkCoroutine);

            speedGauge_.UpdateGauge(ToModSpeed(aircraft_.speed));

            if (Math.Abs(aircraft_.targetSpeed - aircraft_.speed) > SPEED_DELTA)
            {
                yield return SpeedTransitionCoroutine(aircraft_.targetSpeed);
            }
            else
            {
                transitioning_ = false;
            }
        }

        private IEnumerator EnableSpeedGauge()
        {
            AircraftState aircraftState = aircraft_.GetComponent<AircraftState>();
            while (!speedGauge_.Ready() || aircraftState == null || !aircraftState.IsAirborne())
            {
                yield return new WaitForFixedUpdate();
            }

            // All aircraft start with normal speed level.
            speedGauge_.UpdateGauge(SpeedLevel.Normal);
        }

        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            // Initialize speed.
            aircraft_.TakeOffSpeedFactor = ToGameSpeed(SpeedLevel.Normal);

            if (aircraft_.direction == Aircraft.Direction.Outbound)
            {
                aircraft_.targetSpeed = ToGameSpeed(SpeedLevel.Normal);
            }
            if (aircraft_.direction == Aircraft.Direction.Inbound)
            {
                aircraft_.targetSpeed = ToGameSpeed(SpeedLevel.Normal);
            }

            speedGauge_ = aircraft_.gameObject.AddComponent<AircraftSpeedGauge>();
            speedGauge_.aircraft_ = aircraft_;

            StartCoroutine(EnableSpeedGauge());
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
        private AircraftSpeedGauge speedGauge_;
        private bool transitioning_ = false;
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

            if (waypoint_ == null || waypoint_.Invisible || !(waypoint_ is BaseWaypointAutoHeading))
            {
                return;
            }

            speedGauge_ = waypoint_.gameObject.AddComponent<WaypointSpeedGauge>();
            speedGauge_.waypoint_ = waypoint_;
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
            Vector3 waypointPosition = waypoint_.transform.position;
            if (waypointPosition.x == _mousePos.x &&  waypointPosition.y == _mousePos.y)
            {
                if (speed_ > SpeedLevel.Slow && InputSlowDown())
                {
                    speedGauge_.UpdateWaypointSpeedGauge(--speed_);
                }

                if (speed_ < SpeedLevel.Fast && InputSpeedUp())
                {
                    speedGauge_.UpdateWaypointSpeedGauge(++speed_);
                }
            }
        }

        public PlaceableWaypoint waypoint_;
        public SpeedLevel speed_;
        private WaypointSpeedGauge speedGauge_;
    }
}