using System.Collections;
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

    public enum TCASAction
    {
        None,
        Climb,
        Desend,
        Disabled,
    }

    public class Altitude : MonoBehaviour
    {
        public static string ToString(AltitudeLevel altitude)
        {
            switch(altitude)
            {
                case AltitudeLevel.Low:
                    return ">";
                case AltitudeLevel.Normal:
                    return ">>";
                case AltitudeLevel.High:
                    return ">>>";
            }
            return "-";
        }

        public static bool InputClimb()
        {
            return Input.GetKeyDown(KeyCode.W) || 
                   (!Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") > 0f);
        }

        public static bool InputDesend()
        {
            return Input.GetKeyDown(KeyCode.S) ||
                   (!Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") < 0f);
        }
    }

    public class AircraftAltitude : Altitude
    {
        override public string ToString()
        {
            if (altitude_ != targetAltitude_ && Animation.Blink())
            {
                // Trainsitional blink.
                return " ";
            }

            return ToString(altitude_);
        }

        public bool CanLand()
        {
            return targetAltitude_ <= AltitudeLevel.Low;
        }

        public IEnumerator EnableAltitudeGauge(AltitudeLevel altitude)
        {
            while (!altitudeGauge_.Ready())
            {
                yield return new WaitForFixedUpdate();
            }
            altitudeGauge_.UpdateGauge(altitude);
        }

        public void AircraftClimb()
        {
            if (altitudeDisabled_)
            {
                return;
            }

            if (targetAltitude_ >= AltitudeLevel.High)
            {
                return;
            }

            targetAltitude_ ++;
            AltitudeTransition();
        }

        public void AircraftDesend()
        {
            if (altitudeDisabled_)
            {
                return;
            }

            if (targetAltitude_ <= AltitudeLevel.Low)
            {
                return;
            }

            targetAltitude_ --;
            AltitudeTransition();
        }

        public void EmergencyClimb(bool piority = false)
        {
            if (altitudeDisabled_ || altitude_ == AltitudeLevel.Ground)
            {
                return;
            }

            tcasAction_ = TCASAction.Climb;

            if (targetAltitude_ >= AltitudeLevel.High)
            {
                return;
            }

            targetAltitude_ ++;
            EmergencyAltitudeTransition(piority);
        }

        public void EmergencyDesend()
        {
            if (altitudeDisabled_ || altitude_ == AltitudeLevel.Ground)
            {
                return;
            }

            tcasAction_ = TCASAction.Desend;

            if (targetAltitude_ <= AltitudeLevel.Low)
            {
                return;
            }

            targetAltitude_ --;
            EmergencyAltitudeTransition();
        }

        public bool IsLanding()
        {
            return aircraft_.state == Aircraft.State.Landing;
        }

        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            altitudeGauge_ = aircraft_.gameObject.AddComponent<AircraftAltitudeGauge>();
            altitudeGauge_.aircraft_ = aircraft_;

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
            if (aircraft_ == null) 
            {
                Destroy(gameObject);
                return;
            }

            TakeoffTouchdownArrivalProcess();

            if (Aircraft.CurrentCommandingAircraft == aircraft_)
            {
                if (InputClimb())
                {
                    AircraftClimb();
                }

                if (InputDesend())
                {
                    AircraftDesend();
                }
            }
        }

        private void AltitudeTransition()
        {
            if (transitioningCoroutine_ == null)
            {
                transitioningCoroutine_ = AltitudeTransitionCoroutine(targetAltitude_);
                StartCoroutine(transitioningCoroutine_);
            }
        }

        private void EmergencyAltitudeTransition(bool piority = false)
        {
            if (!piority && isEmergencyTransitioning_)
            {
                return;
            }
            isEmergencyTransitioning_ = true;

            // Stop current corutines.
            if (transitioningCoroutine_ != null)
            {
                StopCoroutine(transitioningCoroutine_);
            }
            if (blinkCoroutine_ != null)
            {
                StopCoroutine(blinkCoroutine_);
            }

            transitioningCoroutine_ = AltitudeTransitionCoroutine(targetAltitude_);
            StartCoroutine(transitioningCoroutine_);
        }

        private void TakeoffTouchdownArrivalProcess()
        {
            if (altitude_ == AltitudeLevel.Ground && aircraft_.direction == Aircraft.Direction.Outbound &&
                (aircraft_.state == Aircraft.State.Flying || aircraft_.state == Aircraft.State.HeadingAfterReachingWaypoint))
            {
                altitude_ = AltitudeLevel.Low;
                targetAltitude_ = AltitudeLevel.Low;
                if (enableAltitudeGaugeCoroutine_ == null)
                {
                    enableAltitudeGaugeCoroutine_ = EnableAltitudeGauge(altitude_);
                    StartCoroutine(enableAltitudeGaugeCoroutine_);
                }

                AircraftSpeed aircraftSpeed;
                if (AircraftState.GetAircraftStates(aircraft_, out _, out aircraftSpeed, out _) &&
                    aircraftSpeed.enableSpeedGaugeCoroutine_ == null)
                {
                    aircraftSpeed.enableSpeedGaugeCoroutine_ = aircraftSpeed.EnableSpeedGauge();
                    StartCoroutine(aircraftSpeed.enableSpeedGaugeCoroutine_);
                }
            }

            if (altitude_ != AltitudeLevel.Ground && AircraftState.DisableStateOnTouchedDown(aircraft_))
            {
                altitude_ = AltitudeLevel.Ground;
                targetAltitude_ = AltitudeLevel.Ground;
                altitudeGauge_.DisableSpriteRenderer();
            }

            Vector2 vector = Camera.main.WorldToViewportPoint(aircraft_.gameObject.transform.position);
            bool Inbound = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
            if (aircraft_.direction == Aircraft.Direction.Inbound && Inbound && enableAltitudeGaugeCoroutine_ == null)
            {
                enableAltitudeGaugeCoroutine_ = EnableAltitudeGauge(altitude_);
                StartCoroutine(enableAltitudeGaugeCoroutine_);
            }
        }

        private IEnumerator AltitudeTransitionCoroutine(AltitudeLevel targetAltitude)
        {
            while (!altitudeGauge_.Ready())
            {
                yield return new WaitForFixedUpdate();
            }

            blinkCoroutine_ = altitudeGauge_.GetTransitioningCoroutine(altitude_, targetAltitude);
            StartCoroutine(blinkCoroutine_);

            yield return new WaitForSeconds(TRANSITION_TIME);

            altitude_ = targetAltitude;
            if (tcasAction_ != TCASAction.Disabled)
            {
                tcasAction_ = TCASAction.None;
            }

            StopCoroutine(blinkCoroutine_);
            blinkCoroutine_ = null;

            altitudeGauge_.UpdateGauge(altitude_);

            if (altitude_ != targetAltitude_)
            {
                transitioningCoroutine_ = AltitudeTransitionCoroutine(targetAltitude_);
                yield return transitioningCoroutine_;
            }
            else
            {
                transitioningCoroutine_ = null;
                isEmergencyTransitioning_ = false;
            }
        }

        public Aircraft aircraft_;
        public AltitudeLevel altitude_;
        public AltitudeLevel targetAltitude_;
        public TCASAction tcasAction_ = TCASAction.None;
        public bool altitudeDisabled_ = false;
        public IEnumerator enableAltitudeGaugeCoroutine_ = null;
        private AircraftAltitudeGauge altitudeGauge_;
        private const float TRANSITION_TIME = 5f;
        private IEnumerator transitioningCoroutine_ = null;
        private IEnumerator blinkCoroutine_ = null;
        private bool isEmergencyTransitioning_ = false;
    }
    
    public class WaypointAltitude : Altitude
    {
        override public string ToString()
        {
            return ToString(altitude_);
        }

        private void Start()
        {
            altitude_ = AltitudeLevel.Normal;

            if (waypoint_ == null || waypoint_.Invisible || !(waypoint_ is BaseWaypointAutoHeading))
            {
                return;
            }

            altitudeGauge_ = waypoint_.gameObject.AddComponent<WaypointAltitudeGauge>();
            altitudeGauge_.waypoint_ = waypoint_;
        }

        private void Update()
        {
            if (waypoint_ == null)
            {
                Destroy(gameObject);
                return;
            }

            // Typing name, stop changing altitude.
            WaypointNameInput waypointNameInput = waypoint_.GetComponent<WaypointNameInput>();
            if (waypointNameInput != null && waypointNameInput.active)
            {
                return;
            }

            // Stop changing altitude for landing waypoint.
            if (!(waypoint_ is BaseWaypointAutoHeading))
            {
                return;
            }

            // Waypoint would hard follow mouse position when placed.
            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = waypoint_.transform.position;
            if (waypointPosition.x == _mousePos.x &&  waypointPosition.y == _mousePos.y)
            {
                if (altitude_ < AltitudeLevel.High && InputClimb())
                {
                    altitudeGauge_.UpdateWaypointAltitudeGauge(++altitude_);
                }

                if (altitude_ > AltitudeLevel.Low && InputDesend())
                {
                    altitudeGauge_.UpdateWaypointAltitudeGauge(--altitude_);
                }
            }
        }

        public PlaceableWaypoint waypoint_;
        public AltitudeLevel altitude_ { get; private set; }
        private WaypointAltitudeGauge altitudeGauge_;
    }
}