using DG.Tweening;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MiniRealisticAirways
{
    [HarmonyPatch(typeof(Aircraft), "Start", new Type[] { })]
    class PatchAircraftStart
    {
        static void Postfix(ref Aircraft __instance)
        {
            if (__instance.direction == Aircraft.Direction.Inbound)
            {
                AircraftState aircraftState = __instance.gameObject.AddComponent<AircraftState>();
                aircraftState.aircraft_ = __instance;
                aircraftState.Initialize();
                AircraftType aircraftType = aircraftState.aircraftType_;
                if (aircraftType != null)
                {
                    aircraftType.weight_ = BaseAircraftType.RandomWeight();
                    // Only arrival aircraft have fuel limit.
                    aircraftType.percentFuelLeft_ = 99;
                    __instance.StartCoroutine(aircraftType.FuelManagementCoroutine());

                    aircraftType.UpdateSprite();
                }
            }
            else
            {
                AircraftType aircraftType;
                if (!AircraftState.GetAircraftStates(__instance, out _, out _, out aircraftType))
                {
                    return;
                }
                aircraftType.UpdateSprite();
            }
        }
    }

    // Methods to restore to specified speed.
    [HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] { })]
    class PatchSetFlyHeading
    {
        static bool Prefix(ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] { __instance.targetSpeed };
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

    [HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] { typeof(float) })]
    class PatchSetFlyHeadingFloat
    {
        static bool Prefix(float heading, ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] { __instance.targetSpeed };
            return true;
        }

        static void Postfix(float heading, ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1)
            {
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] { typeof(WaypointAutoHover) })]
    class PatchSetVectorToWaypointAutoHover
    {
        static bool Prefix(WaypointAutoHover waypoint, ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] { __instance.targetSpeed };
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

    [HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] { typeof(PlaceableWaypoint) })]
    class PatchSetVectorToPlaceableWaypoint
    {
        static bool Prefix(PlaceableWaypoint waypoint, ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] { __instance.targetSpeed };
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

    [HarmonyPatch(typeof(Aircraft), "OnPointUp", new Type[] { typeof(bool) })]
    class PatchOnPointUp
    {
        static bool Prefix(bool external, ref Aircraft __instance, ref object[] __state)
        {
            __state = new object[] { __instance.targetSpeed };
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
    public class PatchLandCoroutine
    {
        private static void GoAround(Aircraft __instance, ref object[] __state)
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

        [HarmonyPatch(typeof(Aircraft), "LandCoroutine")]
        [HarmonyPrefix]
        public static bool LandCoroutinePrefix(Aircraft __instance, ref object[] __state)
        {
            __state = new object[] { __instance.targetSpeed };
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

                AircraftAltitude aircraftAltitude;
                AircraftSpeed aircraftSpeed;
                AircraftType aircraftType;
                if (AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out aircraftSpeed, out aircraftType))
                {
                    // Go around when final landing altitude/speed did not meet requirement.
                    if ((aircraftAltitude != null && !aircraftAltitude.CanLand()) ||
                        (aircraftSpeed != null && aircraftType != null && !aircraftSpeed.CanLand(aircraftType.weight_)))
                    {
                        GoAround(__instance, ref __state);
                        aircraftType.windChecked_ = false;
                    }

                    if (__instance.state == Aircraft.State.Landing)
                    {
                        float distanceFromLanding = ((Vector2)__instance.gameObject.transform.position - __instance.landingStartPoint).magnitude;
                        if (distanceFromLanding < 0.2f)
                        {
                            // Go around due to wind direction.
                            WindSock windSock = Plugin.windsock_;
                            if (aircraftType != null && windSock != null && !aircraftType.windChecked_)
                            {
                                aircraftType.windChecked_ = true;
                                if (!windSock.CanLand(__instance.heading, aircraftType.weight_))
                                {
                                    Plugin.Log.LogInfo("Going around due to wind with heading: " + __instance.heading);
                                    GoAround(__instance, ref __state);
                                    aircraftType.windChecked_ = false;
                                }
                            }

                            // Go around due to runway close event.
                            if (EventManager.closedRunway_ != null && EventManager.closedRunway_ == __instance.LandingRunway)
                            {
                                Plugin.Log.LogInfo("Going around due to runway closed event.");
                                GoAround(__instance, ref __state);
                                aircraftType.windChecked_ = false;
                            }
                        }
                    }
                }

                yield return result.Current;
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "UpdateHeading", new Type[] { })]
    class PatchUpdateHeading
    {
        static bool Prefix(ref Aircraft __instance, ref PlaceableWaypoint ____HARWCurWP, ref object[] __state)
        {
            AircraftAltitude aircraftAltitude;
            AircraftSpeed aircraftSpeed;
            AircraftType aircraftType;
            if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out aircraftSpeed, out aircraftType))
            {
                return true;
            }

            __state = new object[] { Aircraft.TurnSpeed };
            aircraftType.PatchTurnSpeed();

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

            // Stablize the approach first before trying to land.
            int i = 0;
            while (aircraftAltitude != null && !aircraftAltitude.CanLand() && ++i < Plugin.MAX_WHILE_LOOP_ITER)
            {
                aircraftAltitude.AircraftDesend();
                if (i == Plugin.MAX_WHILE_LOOP_ITER - 1)
                {
                    Plugin.Log.LogWarning("INF Loop in UpdateHeading's prefix aircraftAltitude change.");
                }
            }

            i = 0;
            while (aircraftSpeed != null && !aircraftSpeed.CanLand(aircraftType.weight_) && ++i < Plugin.MAX_WHILE_LOOP_ITER)
            {
                aircraftSpeed.AircraftSlowDown();
                if (i == Plugin.MAX_WHILE_LOOP_ITER - 1)
                {
                    Plugin.Log.LogWarning("INF Loop in UpdateHeading's prefix aircraftSpeed change.");
                }
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

            AircraftState aircraftState;
            if (!AircraftState.GetAircraftState(__instance, out aircraftState))
            {
                return;
            }

            // Only control by waypoint once.
            if (aircraftState.commandingWaypoint_ == ____HARWCurWP)
            {
                return;
            }

            // Altitude sync.
            AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
            WaypointAltitude waypointAltitude = ____HARWCurWP.GetComponent<WaypointAltitude>();
            if (aircraftAltitude != null && waypointAltitude != null)
            {
                int i = 0;
                while (aircraftAltitude.targetAltitude_ < waypointAltitude.altitude_ && ++i < Plugin.MAX_WHILE_LOOP_ITER)
                {
                    aircraftAltitude.AircraftClimb();
                    if (i == Plugin.MAX_WHILE_LOOP_ITER - 1)
                    {
                        Plugin.Log.LogWarning("INF Loop in UpdateHeading's postfix AircraftClimb().");
                    }
                }
                i = 0;
                while (aircraftAltitude.targetAltitude_ > waypointAltitude.altitude_ && ++i < Plugin.MAX_WHILE_LOOP_ITER)
                {
                    aircraftAltitude.AircraftDesend();
                    if (i == Plugin.MAX_WHILE_LOOP_ITER - 1)
                    {
                        Plugin.Log.LogWarning("INF Loop in UpdateHeading's postfix AircraftDesend().");
                    }
                }
            }

            // Speed sync.
            AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
            WaypointSpeed waypointSpeed = ____HARWCurWP.GetComponent<WaypointSpeed>();
            if (aircraftSpeed != null && waypointSpeed != null)
            {
                float maxSpeed = Math.Min(Speed.ToGameSpeed(aircraftSpeed.MaxSpeed()),
                                          Speed.ToGameSpeed(waypointSpeed.speed_));
                int i = 0;
                while (Speed.ToModSpeed(__instance.targetSpeed) < Speed.ToModSpeed(maxSpeed) && ++i < Plugin.MAX_WHILE_LOOP_ITER)
                {
                    aircraftSpeed.AircraftSpeedUp();
                    if (i == Plugin.MAX_WHILE_LOOP_ITER - 1)
                    {
                        Plugin.Log.LogWarning("INF Loop in UpdateHeading's postfix AircraftSpeedUp().");
                    }
                }
                i = 0;
                while (Speed.ToModSpeed(__instance.targetSpeed) > waypointSpeed.speed_ && ++i < Plugin.MAX_WHILE_LOOP_ITER)
                {
                    aircraftSpeed.AircraftSlowDown();
                    if (i == Plugin.MAX_WHILE_LOOP_ITER - 1)
                    {
                        Plugin.Log.LogWarning("INF Loop in UpdateHeading's postfix AircraftSlowDown().");
                    }
                }


                __instance.targetSpeed = Math.Min(Speed.ToGameSpeed(aircraftSpeed.MaxSpeed()),
                                                  Speed.ToGameSpeed(waypointSpeed.speed_));
            }

            aircraftState.commandingWaypoint_ = ____HARWCurWP;
        }
    }

    // Patching turning speed.
    [HarmonyPatch(typeof(Aircraft), "GenerateFlyingPath", new Type[] { typeof(int) })]
    class PatchGenerateFlyingPath
    {
        static bool Prefix(ref Aircraft __instance, ref object[] __state)
        {
            AircraftType aircraftType;
            if (!AircraftState.GetAircraftStates(__instance, out _, out _, out aircraftType))
            {
                return true;
            }

            __state = new object[] { Aircraft.TurnSpeed };
            aircraftType.PatchTurnSpeed();
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

    [HarmonyPatch(typeof(Aircraft), "PredictPosAfterTurn", new Type[] { typeof(float) })]
    class PatchPredictPosAfterTurn
    {
        static bool Prefix(float angle, ref Aircraft __instance, ref object[] __state)
        {
            AircraftType aircraftType;
            if (!AircraftState.GetAircraftStates(__instance, out _, out _, out aircraftType))
            {
                return true;
            }

            __state = new object[] { Aircraft.TurnSpeed };
            aircraftType.PatchTurnSpeed();
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
            AircraftType aircraftType;
            if (!AircraftState.GetAircraftStates(__instance, out _, out _, out aircraftType))
            {
                return;
            }

            if (aircraftType.weight_ == Weight.Light)
            {
                __result /= AircraftType.LIGHT_TURN_FACTOR;
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "AircraftOOBGameOver", new Type[] { typeof(Aircraft) })]
    class PatchAircraftOOBGameOver
    {
        static bool Prefix(Aircraft aircraft)
        {
            // Turn OOB into warnings.
            if (RestrictedAreaManager.Instance.counter > 1)
            {
                AircraftState aircraftState;
                if (!AircraftState.GetAircraftState(aircraft, out aircraftState))
                {
                    return true;
                }
                // Give it some time for animation to complete.
                aircraftState.StartCoroutine(aircraftState.DelayDestoryCoroutine());
                RestrictedAreaManager.Instance.AreaEnter(aircraft);
                aircraft.aircraftEverInView = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "TrySetupLanding", new Type[] { typeof(Runway), typeof(bool) })]
    class PatchTrySetupLanding
    {
        static bool Prefix(Runway runway, bool doLand, ref Aircraft __instance, ref object[] __state)
        {

            AircraftAltitude aircraftAltitude;
            AircraftSpeed aircraftSpeed;
            AircraftType aircraftType;
            if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out aircraftSpeed, out aircraftType))
            {
                return true;
            }

            __state = new object[] { Aircraft.TurnSpeed };
            aircraftType.PatchTurnSpeed();

            // Auto slow down for aircraft to land when altitude is right.
            if (aircraftAltitude.CanLand())
            {
                while (!aircraftSpeed.CanLand(aircraftType.weight_))
                {
                    aircraftSpeed.AircraftSlowDown();
                }
            }

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

                AudioManager.instance.PlayCanNotComply();
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

    [HarmonyPatch(typeof(Aircraft), "FixedUpdate", new Type[] { })]
    class PatchFixedUpdate
    {
        static bool Prefix(ref Aircraft __instance)
        {
            // Continue to update speed while landing.
            if (__instance.state == Aircraft.State.Landing)
            {
                __instance.Invoke("UpdateSpeed", 0);
            }

            // Weather effects.
            AircraftState aircraftState;
            if (!AircraftState.GetAircraftState(__instance, out aircraftState))
            {
                return true;
            }

            if (EventManager.weather_ == null || !EventManager.weather_.enabled_)
            {
                // Reset weather flag once the current weather had passed.
                aircraftState.weatherAffected_ = false;
                return true;
            }
            if (aircraftState.weatherAffected_)
            {
                return true;
            }
            AircraftAltitude altitude = aircraftState.aircraftAltitude_;
            if (altitude == null)
            {
                return true;
            }

            if (altitude.altitude_ < AltitudeLevel.High &&
                EventManager.weather_.InCell(__instance.AP.transform.position))
            {
                Plugin.Log.LogInfo("Aircraft entered weather cell.");
                aircraftState.weatherAffected_ = true;
                RestrictedAreaManager.Instance.AreaEnter(__instance);
                if (__instance.state != Aircraft.State.Landing && altitude.tcasAction_ == TCASAction.None)
                {
                    // Climb the aircraft if there is no TCAS action.
                    for (int i = (int)altitude.targetAltitude_; i < (int)AltitudeLevel.High; i++)
                    {
                        Plugin.Log.LogInfo("Weather effected, emergency climbing.");
                        altitude.EmergencyClimb();
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "AircraftCollideGameOver", new Type[] { typeof(Aircraft), typeof(Aircraft) })]
    class PatchAircraftCollideGameOver
    {
        static bool Prefix(Aircraft aircraft1, Aircraft aircraft2)
        {
            // get altitude_ of both
            AircraftAltitude aircraftAltitude1;
            AircraftAltitude aircraftAltitude2;
            if (!AircraftState.GetAircraftStates(aircraft1, out aircraftAltitude1, out _, out _) ||
                !AircraftState.GetAircraftStates(aircraft2, out aircraftAltitude2, out _, out _))
            {
                return true;
            }
            return aircraftAltitude1.altitude_ == aircraftAltitude2.altitude_;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "OnTriggerEnter2D", new Type[] { typeof(Collider2D) })]
    class PatchOnTriggerEnter2D
    {
        static bool Prefix(Collider2D other, ref bool ___mainMenuMode, ref ColorCode.Option ___colorCode,
                           ref ShapeCode.Option ___shapeCode, ref Aircraft __instance, ref bool ___reachExit)
        {

            if (___mainMenuMode || !((Component)(object)other).CompareTag("CollideCheck"))
            {
                return false;
            }

            if (((Component)(object)other).gameObject.layer == LayerMask.NameToLayer("Waypoint"))
            {
                // Do not allow aircraft to exit unless higher than normal.
                Waypoint waypoint = ((Component)(object)other).GetComponent<WaypointRef>().waypoint;
                if (waypoint != null && ___colorCode == waypoint.colorCode && ___shapeCode == waypoint.shapeCode)
                {
                    AircraftAltitude aircraftAltitude;
                    if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out _, out _))
                    {
                        return true;
                    }

                    if (!aircraftAltitude.altitudeDisabled_ && aircraftAltitude.altitude_ >= AltitudeLevel.Normal)
                    {
                        WaypointManager.Instance.Handoff(waypoint);
                        __instance.aircraftVoiceAndSubtitles.PlayHandOff();
                        AircraftManager.Instance.AircraftHandOffEvent.Invoke(__instance.gameObject.transform.position);
                        ___reachExit = true;
                        __instance.Handoff();
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
                    AircraftAltitude aircraftAltitude1;
                    AircraftAltitude aircraftAltitude2;
                    if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude1, out _, out _) ||
                        !AircraftState.GetAircraftStates(aircraft, out aircraftAltitude2, out _, out _))
                    {
                        return true;
                    }

                    if (aircraftAltitude1.altitude_ != aircraftAltitude2.altitude_ &&
                        aircraftAltitude1.targetAltitude_ != aircraftAltitude2.altitude_ &&
                        aircraftAltitude1.altitude_ != aircraftAltitude2.targetAltitude_)
                    {
                        return false;
                    }
                }
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("AircraftSafety"))
            {
                // Do not collide with terrain in high altitude.
                Vector2 vector = Camera.main.WorldToViewportPoint(__instance.gameObject.transform.position);
                bool Inbound = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
                AircraftAltitude aircraftAltitude;
                if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out _, out _))
                {
                    return true;
                }

                if (Inbound && aircraftAltitude.altitude_ == AltitudeLevel.High)
                {
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "OnTriggerStay2D", new Type[] { typeof(Collider2D) })]
    class PatchOnTriggerStay2D
    {
        static bool Prefix(Collider2D other, ref bool ___mainMenuMode, ref Aircraft __instance, ref Dictionary<GameObject, VWIndicator> ___TCASWarns)
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
                    // Hovering aircraft don't trigger TCAS.
                    if (aircraft.IsHovering || __instance.IsHovering)
                    {
                        return false;
                    }

                    AircraftAltitude aircraftAltitude1;
                    AircraftAltitude aircraftAltitude2;
                    if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude1, out _, out _) ||
                        !AircraftState.GetAircraftStates(aircraft, out aircraftAltitude2, out _, out _))
                    {
                        return true;
                    }

                    // No TCAS action if any aircraft is on ground.
                    if (aircraftAltitude1.altitude_ == AltitudeLevel.Ground || __instance.OnTheGround ||
                        aircraftAltitude2.altitude_ == AltitudeLevel.Ground || aircraft.OnTheGround)
                    {
                        return false;
                    }

                    // No TCAS action if it's not warning.
                    if (!___TCASWarns.ContainsKey(other.gameObject))
                    {
                        return false;
                    }
                    // Reflex on private function is too slow.
                    // MethodInfo PathBasedCollidePredict= __instance.GetType().GetMethod(name: "PathBasedCollidePredict",
                    //     bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,  binder: null, types: new Type[] { typeof(Aircraft), typeof(Aircraft) },  modifiers: null );
                    // if (!(bool)PathBasedCollidePredict.Invoke(__instance, new object[] { __instance, aircraft }))
                    // {
                    //     return false;
                    // }

                    // No TCAS action if altitude being different.
                    if (aircraftAltitude1.altitude_ != aircraftAltitude2.altitude_ &&
                        aircraftAltitude1.targetAltitude_ != aircraftAltitude2.altitude_ &&
                        aircraftAltitude1.altitude_ != aircraftAltitude2.targetAltitude_)
                    {
                        return false;
                    }

                    // Only trigger TCAS action when the distance is nearby.
                    if (Vector2.Distance((Vector2)__instance.gameObject.transform.position, (Vector2)aircraft.gameObject.transform.position) < 3f &&
                        aircraftAltitude1.tcasAction_ == TCASAction.None && aircraftAltitude2.tcasAction_ == TCASAction.None)
                    {
                        Plugin.Log.LogInfo("TCAS activated");
                        // Active TCAS on aircrafts if there is no previous action.
                        if (aircraftAltitude1.IsLanding() && aircraftAltitude2.IsLanding())
                        {
                            // Both at landing, do nothing.
                        }
                        else if (aircraftAltitude1.IsLanding())
                        {
                            aircraftAltitude2.EmergencyClimb();
                        }
                        else if (aircraftAltitude2.IsLanding())
                        {
                            aircraftAltitude1.EmergencyClimb();
                        }
                        else if (aircraftAltitude1.targetAltitude_ == AltitudeLevel.High &&
                                (aircraftAltitude2.targetAltitude_ == AltitudeLevel.High || aircraftAltitude2.altitude_ == AltitudeLevel.High))
                        {
                            // 2 (about to be)at High, 1 about to be at high, command 1 to desend.
                            aircraftAltitude1.EmergencyDesend();
                        }
                        else if (aircraftAltitude2.targetAltitude_ == AltitudeLevel.High &&
                                (aircraftAltitude1.targetAltitude_ == AltitudeLevel.High || aircraftAltitude1.altitude_ == AltitudeLevel.High))
                        {
                            // 1 (about to be)at High, 2 about to be at high, command 2 to desend.
                            aircraftAltitude2.EmergencyDesend();
                        }
                        else if (aircraftAltitude1.targetAltitude_ == AltitudeLevel.Low &&
                                (aircraftAltitude2.targetAltitude_ == AltitudeLevel.Low || aircraftAltitude2.altitude_ == AltitudeLevel.Low))
                        {
                            // 2 (about to be)at low, 1 about to be at low, command 1 to climb.
                            aircraftAltitude1.EmergencyClimb();
                        }
                        else if (aircraftAltitude2.targetAltitude_ == AltitudeLevel.Low &&
                                (aircraftAltitude1.targetAltitude_ == AltitudeLevel.Low || aircraftAltitude1.altitude_ == AltitudeLevel.Low))
                        {
                            // 1 (about to be)at low, 2 about to be at low, command 2 to climb.
                            aircraftAltitude2.EmergencyClimb();
                        }
                        else if (aircraftAltitude1.altitude_ == AltitudeLevel.Low &&
                                 aircraftAltitude2.altitude_ == AltitudeLevel.Low)
                        {
                            // Both at high, command 2 to climb.
                            aircraftAltitude2.EmergencyClimb();
                        }
                        else if (aircraftAltitude1.altitude_ == AltitudeLevel.High &&
                                 aircraftAltitude2.altitude_ == AltitudeLevel.High)
                        {
                            // Both at high, command 2 to desend.
                            aircraftAltitude2.EmergencyDesend();
                        }
                        else
                        {
                            aircraftAltitude1.EmergencyClimb();
                            aircraftAltitude2.EmergencyDesend();
                        }
                    }
                }
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("AircraftSafety"))
            {
                // Do collide with terrain below high altitude.
                Vector2 vector = Camera.main.WorldToViewportPoint(__instance.gameObject.transform.position);
                bool Inbound = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;

                AircraftAltitude aircraftAltitude;
                if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out _, out _))
                {
                    return true;
                }
                if (Inbound && aircraftAltitude.altitude_ < AltitudeLevel.High)
                {
                    // Using reflex for __instance.Invoke("AircraftTerrainGameOver", 0); will crash the game.
                    // MethodInfo AircraftTerrainGameOver = __instance.GetType().GetMethod("AircraftTerrainGameOver", 
                    //     BindingFlags.NonPublic | BindingFlags.Instance);
                    // AircraftTerrainGameOver.Invoke(__instance, new object[] { __instance });
                    LevelManager.Instance.CrashGameOver(__instance, null);
                }
                if (Inbound && aircraftAltitude.targetAltitude_ < AltitudeLevel.High)
                {
                    // Active GPWS on aircraft if there is no TCAS action.
                    for (int i = (int)aircraftAltitude.targetAltitude_; i < (int)AltitudeLevel.High; i++)
                    {
                        Plugin.Log.LogInfo("GPWS activated, emergency climbing.");
                        // Let GWPS take piority when the aircraft is on top of terrain.
                        aircraftAltitude.EmergencyClimb(piority: true);
                        __instance.aircraftVoiceAndSubtitles.PlayTerrain();
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "PathBasedCollidePredict", new Type[] { typeof(List<Vector3>), typeof(Aircraft) })]
    class PatchPathBasedCollidePredictAirAir
    {
        static bool Prefix(List<Vector3> PathA, Aircraft otherAircraft, ref bool __result)
        {
            Aircraft aircraft1 = otherAircraft;
            foreach (Aircraft ac in AircraftManager.GetAircraft())
            {
                if (Vector2.Distance((Vector2)ac.gameObject.transform.position, (Vector2)PathA[0]) < 0.02f)
                {
                    aircraft1 = ac;
                    break;
                }
            }
            AircraftAltitude aircraftAltitude1;
            AircraftAltitude aircraftAltitude2;
            if (!AircraftState.GetAircraftStates(aircraft1, out aircraftAltitude1, out _, out _) ||
                !AircraftState.GetAircraftStates(otherAircraft, out aircraftAltitude2, out _, out _))
            {
                return true;
            }

            if (aircraftAltitude1.altitude_ != aircraftAltitude2.altitude_ &&
                aircraftAltitude1.targetAltitude_ != aircraftAltitude2.altitude_ &&
                aircraftAltitude1.altitude_ != aircraftAltitude2.targetAltitude_)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "PathBasedCollidePredict", new Type[] { typeof(List<Vector3>), typeof(Collider2D), typeof(float?), typeof(float?) })]
    class PatchPathBasedCollidePredictAirGround
    {
        static bool Prefix(List<Vector3> PathA, Collider2D restrictArea, float? PremittedHdg, float? HdgRange, ref bool __result)
        {
            Aircraft aircraft = null;
            foreach (Aircraft ac in AircraftManager.GetAircraft())
            {
                if (Vector2.Distance((Vector2)ac.gameObject.transform.position, (Vector2)PathA[0]) < 0.02f)
                {
                    aircraft = ac;
                    break;
                }
            }
            if (aircraft == null)
            {
                return true;
            }
            AircraftAltitude aircraftAltitude;
            if (!AircraftState.GetAircraftStates(aircraft, out aircraftAltitude, out _, out _))
            {
                return true;
            }

            if (restrictArea.gameObject.layer == LayerMask.NameToLayer("AircraftSafety"))
            {
                if (aircraftAltitude.altitude_ == AltitudeLevel.High && aircraftAltitude.targetAltitude_ == AltitudeLevel.High)
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "EnableVisualWarning", new Type[] { typeof(GameObject), typeof(bool) })]
    class PatchEnableVisualWarning
    {
        static void Postfix(GameObject other, bool isAircraftWarner, ref Aircraft __instance)
        {
            if (!isAircraftWarner)
            {
                AircraftAltitude aircraftAltitude;
                if (!AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out _, out _))
                {
                    return;
                }
                if (aircraftAltitude.altitude_ == AltitudeLevel.High && aircraftAltitude.targetAltitude_ == AltitudeLevel.High)
                {
                    return;
                }

                else if (__instance.state != Aircraft.State.Landing && aircraftAltitude.tcasAction_ == TCASAction.None)
                {
                    if (other.layer == LayerMask.NameToLayer("AircraftSafety"))
                    {
                        // Active GPWS on aircraft if there is no TCAS action.
                        for (int j = (int)aircraftAltitude.targetAltitude_; j < (int)AltitudeLevel.High; j++)
                        {
                            Plugin.Log.LogInfo("GPWS activated, emergency climbing.");
                            aircraftAltitude.EmergencyClimb();
                        }
                    }
                }
                return;
            }
        }
    }
}