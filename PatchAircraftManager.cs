using HarmonyLib;
using System;
using UnityEngine;

namespace MiniRealisticAirways
{
    [HarmonyPatch(typeof(AircraftManager), "Update", new Type[] {})]
    class PatchAircraftManagerUpdate
    {
        static void Postfix(ref AircraftManager __instance, Camera ____camera)
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
                AircraftState aircraftState = aircraft.GetComponent<AircraftState>();
                if (aircraftState == null) {
                    return;
                }

                AircraftSpeed aircraftSpeed = aircraftState.aircraftSpeed_;
                if (aircraftSpeed != null && (
                    Input.GetKeyDown(KeyCode.A) ||
                    (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") < 0f)))
                {
                    aircraftSpeed.AircraftSlowDown();
                    return;
                }

                if (aircraftSpeed != null && (
                    Input.GetKeyDown(KeyCode.D) ||
                    (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") > 0f)))
                {
                    aircraftSpeed.AircraftSpeedUp();
                    return;
                }

                AircraftAltitude aircraftAltitude = aircraftState.aircraftAltitude_;
                if (aircraftAltitude != null && (
                    Input.GetKeyDown(KeyCode.W) ||
                    Input.GetAxis("Mouse ScrollWheel") > 0f))
                {
                    aircraftAltitude.AircraftClimb();
                    return;
                }

                if (aircraftAltitude != null && (
                    Input.GetKeyDown(KeyCode.S) ||
                    Input.GetAxis("Mouse ScrollWheel") < 0f))
                {
                    aircraftAltitude.AircraftDesend();
                    return;
                }
            }

        }
    }
}