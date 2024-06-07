using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace MiniRealisticAirways
{
    public enum Altitude
    {
        Ground,
        Low,
        Normal,
        High,
    }


    public class AircraftAltitude : MonoBehaviour
    {
        override public string ToString() {
            if (altitude_ != targetAltitude_ && transitionTimer_ % 50 < 25)
                return " ";

            switch(altitude_)
            {
                case Altitude.Low:
                    return "v";
                case Altitude.Normal:
                    return "—";
                case Altitude.High:
                    return "^";
            }
            return "";
        }

        public bool CanLand() {
            return targetAltitude_ <= Altitude.Low;
        }

        public void AircraftClimb() {
            if (targetAltitude_ < Altitude.High)
            {
                targetAltitude_ ++;
                transitionTimer_ = REACTION_TIME + TRANSITION_TIME * Math.Abs(targetAltitude_ - altitude_);
            }
        }

        public void AircraftDesend() {
            if (targetAltitude_ > Altitude.Low)
            {
                targetAltitude_ --;
                transitionTimer_ = REACTION_TIME + TRANSITION_TIME * Math.Abs(targetAltitude_ - altitude_);
            }
        }

        public Aircraft aircraft_;

        public Altitude altitude_ { get; private set; }
        public Altitude targetAltitude_ { get; private set; }
        private const int TRANSITION_TIME = 200;
        private const int REACTION_TIME = 100;
        private int transitionTimer_ = REACTION_TIME + TRANSITION_TIME;
        
        private void Start()
        {
            if (aircraft_.direction == Aircraft.Direction.Outbound)
            {
                altitude_ = Altitude.Ground;
                targetAltitude_ = Altitude.Low;
            }

            if (aircraft_.direction == Aircraft.Direction.Inbound)
            {
                altitude_ = Altitude.High;
                targetAltitude_ = Altitude.High;
            }
        }

        private void Update()
        {
            if (aircraft_ == null)
                Destroy(gameObject);
            
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
            if (altitude_ == Altitude.Ground && aircraft_.direction == Aircraft.Direction.Outbound &&
                aircraft_.state == Aircraft.State.Flying)
            {
                altitude_ = Altitude.Low;
                targetAltitude_ = Altitude.Low;
            }

            if (altitude_ != Altitude.Ground && aircraft_.direction == Aircraft.Direction.Inbound &&
                aircraft_.state == Aircraft.State.TouchedDown)
            {
                altitude_ = Altitude.Ground;
                targetAltitude_ = Altitude.Ground;
            }
        }

        private void AltitudeUpdate()
        {
            if (altitude_ == Altitude.Ground)
            {
                return;
            }

            if (--transitionTimer_ < 0)
            {
                altitude_ = targetAltitude_;
                transitionTimer_ = REACTION_TIME + TRANSITION_TIME;
            }
        }

    }
    
    [HarmonyPatch(typeof(Aircraft), "AircraftCollideGameOver", new Type[] {typeof(Aircraft), typeof(Aircraft)})]
    class PatchAircraftCollideGameOver
    {
        static bool Prefix(Aircraft aircraft1, Aircraft aircraft2)
        {
            // get altitude_ of both
            AircraftAltitude altitude1 = aircraft1.GetComponent<AircraftAltitude>();
            AircraftAltitude altitude2 = aircraft2.GetComponent<AircraftAltitude>();

            if (altitude1 != null && altitude2 != null && altitude1.altitude_ != altitude2.altitude_)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "OnTriggerEnter2D", new Type[] {typeof(Collider2D)})]
    class PatchOnTriggerEnter2D
    {
        static bool Prefix(
            Collider2D other,
            ref bool ___mainMenuMode,
            ref ColorCode.Option ___colorCode,
            ref ShapeCode.Option ___shapeCode,
            ref Aircraft __instance,
            ref bool ___reachExit,
            ref Camera ____mainCamera
            )
        {

            if (___mainMenuMode || !((Component)(object)other).CompareTag("CollideCheck"))
            {
                return false;
            }

            if (((Component)(object)other).gameObject.layer == LayerMask.NameToLayer("Waypoint"))
            {
                // Do not allow aircraft to exit unless higher than normal.
                Waypoint waypoint = ((Component)(object)other).GetComponent<WaypointRef>().waypoint;
                if (___colorCode == waypoint.colorCode && ___shapeCode == waypoint.shapeCode)
                {
                    AircraftAltitude altitude = __instance.GetComponent<AircraftAltitude>();
                    if (altitude.altitude_ >= Altitude.Normal)
                    {
                        WaypointManager.Instance.Handoff(waypoint);
                        __instance.aircraftVoiceAndSubtitles.PlayHandOff();
                        AircraftManager.Instance.AircraftHandOffEvent.Invoke(__instance.gameObject.transform.position);
                        ___reachExit = true;
                        __instance.Invoke("ConditionalDestroy", 2f);
                    }
                   
                    return false;
                }
            }

   
            if (other.GetComponent<AircraftRef>() != null)
            {
                // Do not sound TCAS when altitudes are different.
                Aircraft aircraft = other.GetComponent<AircraftRef>().aircraft;
                if (other.name != "TCAS")
                {
                    AircraftAltitude altitude1 = __instance.GetComponent<AircraftAltitude>();
                    AircraftAltitude altitude2 = aircraft.GetComponent<AircraftAltitude>();
                    if (altitude1 != null && altitude2 != null && 
                        altitude1.altitude_ != altitude2.altitude_ &&
                        altitude1.targetAltitude_ != altitude2.altitude_ &&
                        altitude1.altitude_ != altitude2.targetAltitude_)
                    {
                        return false;
                    }
                }
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("AircraftSafety"))
            {
                // Do not collide with terrain in high altitude.
                Vector2 vector = ____mainCamera.WorldToViewportPoint(__instance.gameObject.transform.position);
                bool Inbound = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
                AircraftAltitude altitude = __instance.GetComponent<AircraftAltitude>();
                if (Inbound && altitude.altitude_ == Altitude.High)
                {
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "OnTriggerStay2D", new Type[] {typeof(Collider2D)})]
    class PatchOnTriggerStay2D
    {
        static bool Prefix(
            Collider2D other,
            ref bool ___mainMenuMode,
            ref Aircraft __instance,
            ref Camera ____mainCamera
            )
        {

            if (___mainMenuMode || !((Component)(object)other).CompareTag("CollideCheck"))
            {
                return false;
            }

            if (other.GetComponent<AircraftRef>() != null)
            {
                // Do not sound TCAS when altitudes are different.
                Aircraft aircraft = other.GetComponent<AircraftRef>().aircraft;
                if (other.name != "TCAS")
                {
                    AircraftAltitude altitude1 = __instance.GetComponent<AircraftAltitude>();
                    AircraftAltitude altitude2 = aircraft.GetComponent<AircraftAltitude>();
                    if (altitude1 != null && altitude2 != null && 
                        altitude1.altitude_ != altitude2.altitude_ &&
                        altitude1.targetAltitude_ != altitude2.altitude_ &&
                        altitude1.altitude_ != altitude2.targetAltitude_)
                    {
                        return false;
                    }
                }
            } else if (other.gameObject.layer == LayerMask.NameToLayer("AircraftSafety")) {

                // Do collide with terrain below high altitude.
                Vector2 vector = ____mainCamera.WorldToViewportPoint(__instance.gameObject.transform.position);
                bool Inbound = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
                AircraftAltitude altitude = __instance.GetComponent<AircraftAltitude>();
                if (Inbound && altitude.altitude_ < Altitude.High)
                {
                    Plugin.Log.LogInfo("AircraftTerrainGameOver invoked.");

                    // Using reflex for __instance.Invoke("AircraftTerrainGameOver", 0); will crash the game.
                    // MethodInfo AircraftTerrainGameOver = __instance.GetType().GetMethod("AircraftTerrainGameOver", 
                    //     BindingFlags.NonPublic | BindingFlags.Instance);
                    // AircraftTerrainGameOver.Invoke(__instance, new object[] { __instance });
                    LevelManager.Instance.CrashGameOver(__instance, null);
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "IsObstacleInFrontOfMe", new Type[] {})]
    class PatchIsObstacleInFrontOfMe
    {
        static void Postfix(ref bool __result, ref Aircraft __instance)
        {
            AircraftAltitude altitude = __instance.GetComponent<AircraftAltitude>();
            if (altitude.altitude_ == Altitude.High && altitude.targetAltitude_ == Altitude.High) {
                __result = false;
            }
        }
    }
}