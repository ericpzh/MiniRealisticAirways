using HarmonyLib;
using System;
using UnityEngine;

namespace MiniRealisticAirways
{
    public enum Speed
    {
        Stopped,
        Slow,
        Normal,
        Fast,
    }

    public class AircraftSpeed : MonoBehaviour
    {
        public static float ToGameSpeed(Speed speed)
        {
            switch(speed)
            {
                case Speed.Slow:
                    return 20;
                case Speed.Normal:
                    return 24;
                case Speed.Fast:
                    return 28;
            }
            return 0;
        }

        public static Speed ToModSpeed(float speed)
        {
            if (speed < ToGameSpeed(Speed.Slow) - SPEED_DELTA) {
                return Speed.Stopped;
            }

            if (speed < ToGameSpeed(Speed.Normal) - SPEED_DELTA) {
                return Speed.Slow;
            }

            if (speed < ToGameSpeed(Speed.Fast) - SPEED_DELTA) {
                return Speed.Normal;
            }

            return Speed.Fast;
        }

        public bool CanLand() {
            return ToModSpeed(aircraft_.targetSpeed) <= Speed.Normal;
        }

        public void AircraftSpeedUp() {
            if (aircraft_.targetSpeed < ToGameSpeed(Speed.Fast))
            {
                aircraft_.targetSpeed = ToGameSpeed(ToModSpeed(aircraft_.targetSpeed) + 1);
            }
        }

        public void AircraftSlowDown() {
            if (aircraft_.targetSpeed > ToGameSpeed(Speed.Slow))
            {
                aircraft_.targetSpeed = ToGameSpeed(ToModSpeed(aircraft_.targetSpeed) - 1);
            }
        }

        override public string ToString() {
            float SpeedDiff = Math.Abs(aircraft_.speed - aircraft_.targetSpeed);
            if (Math.Abs(SpeedDiff) > SPEED_DELTA && Math.Abs(SpeedDiff) % 0.5 < 0.25) {
                return " ";
            }

            switch(ToModSpeed(aircraft_.speed))
            {
                case Speed.Slow:
                    return "<";
                case Speed.Normal:
                    return "|";
                case Speed.Fast:
                    return ">";
            }
            return "";
        }

        public Aircraft aircraft_;

        public Speed GetSpeed() { return ToModSpeed(aircraft_.speed); }

        private const float SPEED_DELTA = 2f; 

        private void Update()
        {
            if (aircraft_ == null)
                Destroy(gameObject);
                       
            if (Aircraft.CurrentCommandingAircraft == aircraft_)
            {
                if (Input.GetKeyDown(KeyCode.A))
                {
                    AircraftSlowDown();
                }

                if (Input.GetKeyDown(KeyCode.D))
                {
                    AircraftSpeedUp();
                }
            }
        }
    }

    // Methods to restore to specified speed.
    [HarmonyPatch(typeof(Aircraft), "OnPointUp", new Type[] {typeof(bool)})]
    class PatchOnPointUp
    {
        static bool Prefix(bool external, ref Aircraft __instance, ref object[] __state) {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(bool external, ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] {})]
    class PatchSetFlyHeading
    {
        static bool Prefix(ref Aircraft __instance, ref object[] __state) {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }
    
        static void Postfix(ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] {typeof(float)})]
    class PatchSetFlyHeadingFloat
    {
        static bool Prefix(float heading,  ref Aircraft __instance, ref object[] __state) {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(float heading,  ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] {typeof(WaypointAutoHover)})]
    class PatchSetVectorTo
    {
        static bool Prefix(WaypointAutoHover waypoint,  ref Aircraft __instance, ref object[] __state) {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(WaypointAutoHover waypoint, ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] {typeof(PlaceableWaypoint)})]
    class PatchSetVectorToPlaceable
    {
        static bool Prefix(PlaceableWaypoint waypoint,  ref Aircraft __instance, ref object[] __state) {
            __state = new object[] {__instance.targetSpeed};
            return true;
        }

        static void Postfix(PlaceableWaypoint waypoint, ref Aircraft __instance, ref object[] __state)
        {
            if (__instance.targetSpeed > 0 && __state.Length == 1) {  // Sanity check.
                __instance.targetSpeed = (float)__state[0];
            }
        }
    }

    // [HarmonyPatch(typeof(Aircraft), "LandCoroutine", new Type[] {})]
    // class PatchLandCoroutine
    // {
    //     static bool Prefix(PlaceableWaypoint waypoint,  ref Aircraft __instance, ref object[] __state) {
    //         __state = new object[] {__instance.targetSpeed};
    //         return true;
    //     }

    //     static void Postfix(PlaceableWaypoint waypoint, ref Aircraft __instance, ref object[] __state)
    //     {
    //         if (__instance.targetSpeed > 0 && __state.Length == 1) {  // Sanity check.
    //             __instance.targetSpeed = (float)__state[0];
    //         }
    //     }
    // }
}