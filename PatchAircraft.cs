using DG.Tweening;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
            { 
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
            { 
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
            {
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
            { 
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
            { 
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(Aircraft), "LandCoroutine")]
        [HarmonyPrefix]
        public static bool LandCoroutinePrefix(Aircraft __instance, ref object[] __state)
        {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        [HarmonyPatch(typeof(Aircraft), "LandCoroutine")]
        [HarmonyPostfix]
        public static IEnumerator LandCoroutinePostfix(IEnumerator result, Aircraft __instance, object[] __state)
        {
            while (result.MoveNext())
            {
                if (__instance.targetSpeed > 0 && __state.Length == 1) 
                { 
                    __instance.targetSpeed = (float)__state[0];
                }

                // Go around when final landing altitude/speed did not meet requirement.
                AircraftState aircraftState = __instance.GetComponent<AircraftState>();
                if (aircraftState != null)
                {
                    AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
                    AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
                    AircraftType aircraftType = aircraftState.aircraftType_;
                    if ((aircraftAltitude != null && !aircraftAltitude.CanLand()) ||
                        (aircraftSpeed != null && aircraftType != null && !aircraftSpeed.CanLand(aircraftType.weight_)))
                    {
                        __instance.aircraftVoiceAndSubtitles.PlayFailedToLand();
                        __instance.targetSpeed = 24f;
                        if (__state.Length == 1) 
                        { 
                            __state[0] = 24f;
                        }
                        __instance.state = Aircraft.State.GoingAround;
                        if (__instance.GoAroundWarner != null)
                        {
                            __instance.GoAroundWarner.SetActive(value: true);
                            Sequence sequence = DOTween.Sequence();
                            sequence.Append(__instance.GoAroundWarner.transform.DOScale(2f, 1f));
                            sequence.Append(__instance.GoAroundWarner.transform.DOScale(2f, 1f).OnComplete(delegate
                            {
                                __instance.GoAroundWarner.SetActive(value: false);
                            }));
                            sequence.Play();
                        }
                    }
                }

                yield return result.Current;
            }
        }
    }
    
    [HarmonyPatch(typeof(Aircraft), "UpdateHeading", new Type[] {})]
    class PatchUpdateHeading
    {
        static bool Prefix(ref Aircraft __instance, ref PlaceableWaypoint ____HARWCurWP, ref object[] __state)
        {

            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return true;
            }
            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftType != null)
            {
                __state = new object[] {Aircraft.TurnSpeed};
                aircraftType.PatchTurnSpeed();
            }

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
            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            while (aircraftAltitude != null && !aircraftAltitude.CanLand())
            {
                aircraftAltitude.AircraftDesend(); 
            }
            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            while (aircraftSpeed != null && !aircraftSpeed.CanLand(aircraftType.weight_))
            {
                aircraftSpeed.AircraftSlowDown(); 
            }
            return true;
        }

        static void Postfix(ref Aircraft __instance, ref PlaceableWaypoint ____HARWCurWP, ref object[] __state)
        {
            if (__state != null && __state.Length > 0)
            {
                // Restore the global turning speed.
                Aircraft.TurnSpeed = (float)__state[0];
            }

            if (__instance.state != Aircraft.State.HeadingAfterReachingWaypoint)
            {
                return;
            }

            if (____HARWCurWP == null || !(____HARWCurWP is BaseWaypointAutoHeading))
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

            Plugin.Log.LogInfo("BaseWaypointAutoHeading commanded an aircraft.");

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

    // Patching turning speed.
    [HarmonyPatch(typeof(Aircraft), "GenerateFlyingPath", new Type[] {})]
    class PatchGenerateFlyingPath
    {
        static bool Prefix(ref Aircraft __instance, ref object[] __state)
        {
            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return true;
            }
            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftType != null)
            {
                __state = new object[] {Aircraft.TurnSpeed};
                aircraftType.PatchTurnSpeed();
            }
            return true;
        }

        static void Postfix(ref Aircraft __instance, ref object[] __state)
        {
            if (__state != null && __state.Length > 0)
            {
                // Restore the global turning speed.
                Aircraft.TurnSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "PredictPosAfterTurn", new Type[] {typeof(float)})]
    class PatchPredictPosAfterTurn
    {
        static bool Prefix(float angle, ref Aircraft __instance, ref object[] __state)
        {
            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return true;
            }
            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftType != null)
            {
                __state = new object[] {Aircraft.TurnSpeed};
                aircraftType.PatchTurnSpeed();
            }
            return true;
        }

        static void Postfix(float angle, ref Aircraft __instance, ref object[] __state)
        {
            if (__state != null && __state.Length > 0)
            {
                // Restore the global turning speed.
                Aircraft.TurnSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "TurningRadius", MethodType.Getter)]
    class PatchTurningRadius
    {
        static void Postfix(ref Aircraft __instance, ref float __result)
        {
            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return;
            }
            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftType != null && aircraftType.weight_ == Weight.Light)
            {
                __result /= AircraftType.LIGHT_TURN_FACTOR;
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "TrySetupLanding", new Type[] {typeof(Runway), typeof(bool)})]
    class PatchTrySetupLanding
    {
        static bool Prefix(Runway runway, bool doLand, ref Aircraft __instance, ref object[] __state)
        {
            
            AircraftState aircraftState = __instance.GetComponent<AircraftState>();
            if (aircraftState == null)
            {
                return true;
            }

            AircraftType aircraftType = aircraftState.aircraftType_;
            if (aircraftType != null)
            {
                __state = new object[] {Aircraft.TurnSpeed};
                aircraftType.PatchTurnSpeed();
            }

            Plugin.Log.LogInfo("TrySetupLanding invoked.");

            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            if (!aircraftAltitude.CanLand() || !aircraftSpeed.CanLand(aircraftType.weight_))
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

        static void Postfix(Runway runway, bool doLand, ref Aircraft __instance, ref object[] __state)
        {
            if (__state != null && __state.Length > 0)
            {
                // Restore the global turning speed.
                Aircraft.TurnSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "FixedUpdate", new Type[] {})]
    class PatchFixedUpdate
    {
        static bool Prefix(ref Aircraft __instance)
        {
            // Continue to update speed while landing.
            if (__instance.state == Aircraft.State.Landing)
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

            Vector3 vector = Quaternion.AngleAxis(__instance.heading, Vector3.back) * Vector2.up;
            string[] layerNames = new string[1] { "AircraftSafety" };
            RaycastHit2D[] array = Physics2D.CircleCastAll(__instance.transform.position, 0.5f, vector, 3f, LayerMask.GetMask(layerNames));
            for (int i = 0; i < array.Length; i++)
            {
                RaycastHit2D raycastHit2D = array[i];
                if (raycastHit2D.collider.name == "Tanel")
                {
                    AircraftAltitude altitude = aircraftState.aircraftAltitude_;
                    if (altitude != null && altitude.altitude_ == AltitudeLevel.High && altitude.targetAltitude_ == AltitudeLevel.High)
                    {
                        __result = false;
                        return;
                    }
                    else if (__instance.state != Aircraft.State.Landing && altitude.tcasAction_ == TCASAction.None)
                    {
                        // Active GPWS on aircrafts if there is no TCAS action.
                        Plugin.Log.LogInfo("GPWS activated.");
                        altitude.TCASClimb();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "EnableVisualWarning", new Type[] {typeof(GameObject), typeof(bool)})]
    class PatchEnableVisualWarning
    {
        static void Postfix(GameObject other, bool isAircraftWarner, ref Aircraft __instance)
        {
            if (!isAircraftWarner)
            {
                return;
            }

            Aircraft aircraft = other.GetComponent<AircraftRef>().aircraft;
            if (aircraft == null || other.name != "TCAS")
            {
                return;
            }

            AircraftState aircraftState1 = __instance.GetComponent<AircraftState>();
            AircraftState aircraftState2 = aircraft.GetComponent<AircraftState>();
            if (aircraftState1 == null || aircraftState2 == null)
            {
                return;
            }

            AircraftAltitude altitude1 = aircraftState1.aircraftAltitude_;
            AircraftAltitude altitude2 = aircraftState2.aircraftAltitude_;
            if (altitude1 == null || altitude2 == null)
            {
                return;
            }

            if (altitude1.tcasAction_ == TCASAction.None && altitude2.tcasAction_ == TCASAction.None)
            {
                // Active TCAS on aircrafts if there is no previous action.
                Plugin.Log.LogInfo("TCAS activated.");
                if (aircraftState1.IsLanding() && aircraftState2.IsLanding())
                {
                    // Both at landing, do nothing.
                }
                else if (aircraftState1.IsLanding())
                {
                    altitude2.TCASClimb();
                }
                else if (aircraftState2.IsLanding())
                {
                    altitude1.TCASClimb();
                }
                else if (altitude1.targetAltitude_ == AltitudeLevel.High && (altitude2.targetAltitude_ == AltitudeLevel.High || altitude2.altitude_ == AltitudeLevel.High))
                {
                    // 2 (about to be)at High, 1 about to be at high, command 1 to desend.
                    altitude1.TCASDesend();
                }
                else if (altitude2.targetAltitude_ == AltitudeLevel.High && (altitude1.targetAltitude_ == AltitudeLevel.High || altitude1.altitude_ == AltitudeLevel.High))
                {
                    // 1 (about to be)at High, 2 about to be at high, command 2 to desend.
                    altitude2.TCASDesend();
                }
                else if (altitude1.targetAltitude_ == AltitudeLevel.Low && (altitude2.targetAltitude_ == AltitudeLevel.Low || altitude2.altitude_ == AltitudeLevel.Low))
                {
                    // 2 (about to be)at low, 1 about to be at low, command 1 to climb.
                    altitude1.TCASClimb();
                }
                else if (altitude2.targetAltitude_ == AltitudeLevel.Low && (altitude1.targetAltitude_ == AltitudeLevel.Low || altitude1.altitude_ == AltitudeLevel.Low))
                {
                    // 1 (about to be)at low, 2 about to be at low, command 2 to climb.
                    altitude2.TCASClimb();
                }
                else if (altitude1.altitude_ == AltitudeLevel.Low && altitude2.altitude_ == AltitudeLevel.Low)
                {
                    // Both at high, command 2 to climb.
                    altitude2.TCASClimb();
                }
                else if (altitude1.altitude_ == AltitudeLevel.High && altitude2.altitude_ == AltitudeLevel.High)
                {
                    // Both at high, command 2 to desend.
                    altitude2.TCASDesend();
                }
                else
                {
                    altitude1.TCASClimb();
                    altitude2.TCASDesend();
                }
            }
        }
    }
}