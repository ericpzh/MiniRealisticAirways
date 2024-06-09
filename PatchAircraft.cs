using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRealisticAirways
{
    // Methods to restore to specified speed.
    [HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] {})]
    class PatchSetFlyHeading
    {
        static bool Prefix(ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }
    
        static void Postfix(ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) 
            {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] {typeof(float)})]
    class PatchSetFlyHeadingFloat
    {
        static bool Prefix(float heading,  ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(float heading,  ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) 
            {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] {typeof(WaypointAutoHover)})]
    class PatchSetVectorToWaypointAutoHover
    {
        static bool Prefix(WaypointAutoHover waypoint,  ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(WaypointAutoHover waypoint, ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) 
            {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] {typeof(PlaceableWaypoint)})]
    class PatchSetVectorToPlaceableWaypoint
    {
        static bool Prefix(PlaceableWaypoint waypoint, ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(PlaceableWaypoint waypoint, ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1)
            {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "OnPointUp", new Type[] {typeof(bool)})]
    class PatchOnPointUp
    {
        static bool Prefix(bool external, ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(bool external, ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) 
            {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    
    [HarmonyPatch(typeof(Aircraft), "UpdateHeading", new Type[] {})]
    class PatchUpdateHeading
    {
        static bool Prefix(ref Aircraft __instance, ref PlaceableWaypoint ____HARWCurWP)
        {
            if (__instance.state != Aircraft.State.HeadingAfterReachingWaypoint) 
            {
                return true;
            }

            if (____HARWCurWP == null || !(____HARWCurWP is WaypointAutoLanding)) 
            {
                return true;
            }

            Runway targetRunway = ____HARWCurWP.GetFieldValue<Runway>("_targetRunway");
            if (targetRunway == null)
            {
                return true;
            }

            Plugin.Log.LogInfo("WaypointAutoLanding commanded an aircraft.");

            // Stablize the approach first before trying to land.
            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return true;
            }

            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            while (aircraftAltitude != null && !aircraftAltitude.CanLand())
            {
                aircraftAltitude.AircraftDesend(); 
            }

            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            while (aircraftSpeed != null && !aircraftSpeed.CanLand())
            {
                aircraftSpeed.AircraftSlowDown(); 
            }
            return true;
        }

        static void Postfix(ref Aircraft __instance, ref PlaceableWaypoint ____HARWCurWP)
        {

            if (__instance.state != Aircraft.State.HeadingAfterReachingWaypoint)
            {
                return;
            }

            if (____HARWCurWP == null || !(____HARWCurWP is WaypointAutoHeading))
            {
                return;
            }



            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return;
            }

            // Only control by waypoint once.
            if (aircraftState.commandingWaypoint_ == ____HARWCurWP)
            {
                return;
            }

            Plugin.Log.LogInfo("WaypointAutoHeading commanded an aircraft.");

            // Altitude sync.
            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            WaypointAltitude waypointAltitude = ____HARWCurWP.GetComponent<WaypointAltitude>();
            if (aircraftAltitude != null && waypointAltitude != null)
            {
                while (aircraftAltitude.targetAltitude_ < waypointAltitude.altitude_)
                {
                    aircraftAltitude.AircraftClimb();
                }

                while (aircraftAltitude.targetAltitude_ > waypointAltitude.altitude_)
                {
                    aircraftAltitude.AircraftDesend();
                }
            }

            // Speed sync.
            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            WaypointSpeed waypointSpeed =____HARWCurWP.GetComponent<WaypointSpeed>();
            if (aircraftSpeed != null && waypointSpeed != null)
            {
                __instance.targetSpeed = Math.Min(Speed.ToGameSpeed(aircraftSpeed.MaxSpeed()),
                                                  Speed.ToGameSpeed(waypointSpeed.speed_));
            }

            aircraftState.commandingWaypoint_ = ____HARWCurWP;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "TrySetupLanding", new Type[] {typeof(Runway), typeof(bool)})]
    class PatchTrySetupLanding
    {
        static bool Prefix(Runway runway, bool doLand, ref Aircraft __instance)
        {
            
            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return true;
            }
            
            Plugin.Log.LogInfo("TrySetupLanding invoked.");

            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            if (!aircraftAltitude.CanLand() || !aircraftSpeed.CanLand())
            {
                Runway runway2 = (runway ? runway : Aircraft.CurrentCommandingRunway);

                // Reflex for  __instance.LandingRunway;
                Runway LandingRunway = __instance.GetFieldValue<Runway>("LandingRunway");
                bool flag = __instance.state == Aircraft.State.Landing && runway2 == LandingRunway;

                // Reflex for bool flag2 = __instance.GenerateLandingPathL1(runway2, out path, flag);
                MethodInfo GenerateLandingPathL1 = typeof(Aircraft).GetMethod("GenerateLandingPathL1", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                object[] args = new object[] { runway2, null, flag, true };
                GenerateLandingPathL1.Invoke(__instance, args);
                List<Vector3> path = (List<Vector3>)args[1];

                // Reflex for  __instance.ShowPath(path, false /* success */);
                MethodInfo ShowPath = __instance.GetType().GetMethod("ShowPath", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                ShowPath.Invoke(__instance, new object[] { path, false /* success */ });

                __instance.aircraftVoiceAndSubtitles.PlayAngleTooSteep();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "FixedUpdate", new Type[] {})]
    class PatchFixedUpdate
    {
        static bool Prefix(ref Aircraft __instance)
        {
            // Continue to update speed while landing.
            if (__instance.state != Aircraft.State.Crashed &&
                __instance.state == Aircraft.State.Landing)
            {
                __instance.Invoke("UpdateSpeed", 0);
            }
            return true;
        }
    }
    
    [HarmonyPatch(typeof(Aircraft), "AircraftCollideGameOver", new Type[] {typeof(Aircraft), typeof(Aircraft)})]
    class PatchAircraftCollideGameOver
    {
        static bool Prefix(Aircraft aircraft1, Aircraft aircraft2)
        {
            // get altitude_ of both
            AircraftState aircraftState1 = aircraft1.GetComponent<AircraftState>();
            AircraftState aircraftState2 = aircraft2.GetComponent<AircraftState>();
            if (aircraftState1 == null || aircraftState2 == null)
            {
                return true;
            }
            AircraftAltitude altitude1 = aircraftState1.aircraftAltitude_;
            AircraftAltitude altitude2 = aircraftState2.aircraftAltitude_;
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
                    AircraftState aircraftState1 = __instance.GetComponent<AircraftState>();
                    if (aircraftState1 == null)
                    {
                        return true;
                    }
                    AircraftAltitude altitude = aircraftState1.aircraftAltitude_;
                    if (altitude != null && altitude.altitude_ >= AltitudeLevel.Normal)
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
                if (other.name == "TCAS")
                {
                    AircraftState aircraftState1 = __instance.GetComponent<AircraftState>();
                    AircraftState aircraftState2 = aircraft.GetComponent<AircraftState>();
                    if (aircraftState1 == null || aircraftState2 == null)
                    {
                        return true;
                    }
                    AircraftAltitude altitude1 = aircraftState1.aircraftAltitude_;
                    AircraftAltitude altitude2 = aircraftState2.aircraftAltitude_;
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
                
                AircraftState aircraftState = __instance.GetComponent<AircraftState>();
                if (aircraftState == null)
                {
                    return true;
                }
                AircraftAltitude altitude = aircraftState.aircraftAltitude_;
                if (Inbound && altitude!= null && altitude.altitude_ == AltitudeLevel.High)
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
                if (other.name == "TCAS")
                {
                    AircraftState aircraftState1 = __instance.GetComponent<AircraftState>();
                    AircraftState aircraftState2 = aircraft.GetComponent<AircraftState>();
                    if (aircraftState1 == null || aircraftState2 == null)
                    {
                        return true;
                    }
                    AircraftAltitude altitude1 = aircraftState1.aircraftAltitude_;
                    AircraftAltitude altitude2 = aircraftState2.aircraftAltitude_;
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
                // Do collide with terrain below high altitude.
                Vector2 vector = ____mainCamera.WorldToViewportPoint(__instance.gameObject.transform.position);
                bool Inbound = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
                AircraftState aircraftState = __instance.GetComponent<AircraftState>();
                if (aircraftState == null)
                {
                    return true;
                }
                AircraftAltitude altitude = aircraftState.aircraftAltitude_;
                if (Inbound && altitude != null && altitude.altitude_ < AltitudeLevel.High)
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
            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return;
            }
            AircraftAltitude altitude = aircraftState.aircraftAltitude_;
            if (altitude != null && altitude.altitude_ == AltitudeLevel.High && altitude.targetAltitude_ == AltitudeLevel.High)
            {
                __result = false;
            }
        }
    }
}