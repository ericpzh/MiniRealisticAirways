using HarmonyLib;
using System;
using UnityEngine;

namespace MiniRealisticAirways
{
    [HarmonyPatch(typeof(AircraftManager), "Update", new Type[] {})]
    class PatchAircraftManagerUpdate
    {
        static void Postfix(Camera ____camera)
        {
            // Don't need to understand what these variables are, that's how the source code works.
            Vector3 val = ____camera.ScreenToWorldPoint(Input.mousePosition);
            float num = float.PositiveInfinity;
            Aircraft aircraft = null;
            float num2 = 0.15384616f * Camera.main.orthographicSize;
            Aircraft[] aircraft2 = AircraftManager.GetAircraft();
            foreach (Aircraft aircraft3 in aircraft2)
            {
                float num3 = Vector2.Distance((Vector2)(((Component)aircraft3).gameObject.transform.position), (Vector2)(val));
                if (num3 < num && num3 <= num2)
                {
                    num = num3;
                    aircraft = aircraft3;
                }
            }

            if ((UnityEngine.Object)(object)aircraft != (UnityEngine.Object)null)
            {
                // Process aircraft action on hover.
                AircraftAltitude aircraftAltitude;
                AircraftSpeed aircraftSpeed;
                AircraftType aircraftType;
                if (!AircraftState.GetAircraftStates(aircraft, out aircraftAltitude, out aircraftSpeed, out aircraftType))
                {
                    return;
                }

                if (AircraftSpeed.InputSlowDown())
                {
                    aircraftSpeed.AircraftSlowDown();
                    return;
                }

                if (AircraftSpeed.InputSpeedUp())
                {
                    aircraftSpeed.AircraftSpeedUp();
                    return;
                }

                if (AircraftAltitude.InputClimb())
                {
                    aircraftAltitude.AircraftClimb();
                    return;
                }

                if (AircraftAltitude.InputDesend())
                {
                    aircraftAltitude.AircraftDesend();
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(AircraftManager), "Start", new Type[] {})]
    class PatchAircraftManagerStart
    {
        static bool Prefix(ref AircraftManager __instance)
        {
            __instance.gameObject.AddComponent<ActiveAircraftType>();
            return true;
        }
    }

    [HarmonyPatch(typeof(AircraftManager), "CreateOutboundAircraft", new Type[] {
        typeof(Runway), typeof(Vector3), typeof(float), typeof(float), typeof(string), typeof(ColorCode.Option), typeof(ShapeCode.Option)})]
    class PatchCreateOutboundAircraft
    {
        static void Postfix(Runway runway, Vector3 position, float heading, float nominalHeading, string lr,
                            ColorCode.Option colorCode, ShapeCode.Option shapeCode,
                            ref AircraftManager __instance, ref Aircraft __result)
        {

            AircraftState aircraftState;
            if (!AircraftState.GetAircraftState(__result, out aircraftState) && 
                __result.direction == Aircraft.Direction.Outbound)
            {
                aircraftState = __result.gameObject.AddComponent<AircraftState>();
                aircraftState.aircraft_ = __result;
                aircraftState.Initialize();
            }

            AircraftType aircraftType = aircraftState.aircraftType_;
            ActiveAircraftType activeAircraftType = __instance.GetComponent<ActiveAircraftType>();
            if (aircraftType != null && activeAircraftType != null && activeAircraftType.active_)
            {
                // Transfer weight from apron to aircraft.
                aircraftType.weight_ = activeAircraftType.weight_;
                Plugin.Log.LogInfo("Transferred aircraft with weight: " + aircraftType.weight_);
            }
        }
    }
}