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
        Desend
    }

    public class Altitude : MonoBehaviour
    {
        public static string ToString(AltitudeLevel altitude)
        {
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

        private IEnumerator EnableAltitudeGauge(AltitudeLevel altitude)
        {
            while (altitudeGauge_.spriteRenderer_ == null)
            {
                yield return new WaitForFixedUpdate();
            }
            altitudeGauge_.spriteRenderer_.enabled = true;
            altitudeGauge_.UpdateGauge(altitude);
            blinkCoroutine_ = Animation.BlinkCoroutine(altitudeGauge_.spriteRenderer_);
        }

        private IEnumerator AltitudeTransitionCoroutine(AltitudeLevel targetAltitude)
        {
            while (altitudeGauge_.spriteRenderer_ == null)
            {
                yield return new WaitForFixedUpdate();
            }

            StartCoroutine(blinkCoroutine_);
            yield return new WaitForSeconds(TRANSITION_TIME);
            altitude_ = targetAltitude;
            tcasAction_ = TCASAction.None;

            altitudeGauge_.UpdateGauge(altitude_);

            StopCoroutine(blinkCoroutine_);
            altitudeGauge_.spriteRenderer_.enabled = true;

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

        public void AircraftClimb()
        {
            if (targetAltitude_ < AltitudeLevel.High)
            {
                targetAltitude_ ++;
                AltitudeTransition();
            }
        }

        public void AircraftDesend()
        {
            if (targetAltitude_ > AltitudeLevel.Low)
            {
                targetAltitude_ --;
                AltitudeTransition();
            }
        }

        public void EmergencyClimb()
        {
            tcasAction_ = TCASAction.Climb;
            if (targetAltitude_ < AltitudeLevel.High)
            {
                targetAltitude_ ++;
                EmergencyAltitudeTransition();
            }
        }

        public void EmergencyDesend()
        {
            tcasAction_ = TCASAction.Desend;
            if (targetAltitude_ > AltitudeLevel.Low)
            {
                targetAltitude_ --;
                EmergencyAltitudeTransition();
            }
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
                StartCoroutine(EnableAltitudeGauge(altitude_));
            }
        }

        private void Update()
        {
            if (aircraft_ == null) 
            {
                Destroy(gameObject);
                return;
            }

            TakeoffTouchdownProcess();
            
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

        private void EmergencyAltitudeTransition()
        {
            if (isEmergencyTransitioning_)
            {
                return;
            }
            isEmergencyTransitioning_ = true;

            if (transitioningCoroutine_ != null)
            {
                // Stop current corutine.
                StopCoroutine(blinkCoroutine_);
                StopCoroutine(transitioningCoroutine_);
            }
            transitioningCoroutine_ = AltitudeTransitionCoroutine(targetAltitude_);
            StartCoroutine(transitioningCoroutine_);
        }

        private void TakeoffTouchdownProcess()
        {
            if (altitude_ == AltitudeLevel.Ground && aircraft_.direction == Aircraft.Direction.Outbound &&
                (aircraft_.state == Aircraft.State.Flying || aircraft_.state == Aircraft.State.HeadingAfterReachingWaypoint))
            {
                altitude_ = AltitudeLevel.Low;
                targetAltitude_ = AltitudeLevel.Low;
                StartCoroutine(EnableAltitudeGauge(altitude_));
            }

            if (altitude_ != AltitudeLevel.Ground && aircraft_.direction == Aircraft.Direction.Inbound &&
                aircraft_.state == Aircraft.State.TouchedDown)
            {
                altitude_ = AltitudeLevel.Ground;
                targetAltitude_ = AltitudeLevel.Ground;
                if (altitudeGauge_.spriteRenderer_ != null)
                {
                    altitudeGauge_.spriteRenderer_.enabled = false;
                }
            }
        }

        public Aircraft aircraft_;
        public AltitudeLevel altitude_;
        public AltitudeLevel targetAltitude_;
        public TCASAction tcasAction_ = TCASAction.None;
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

            if (waypoint_ == null)
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
            // Waypoint would hard follow mouse position when placed.
            Vector3 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 waypointPosition = ((Component)waypoint_).transform.position;
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