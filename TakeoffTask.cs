using DG.Tweening;
using HarmonyLib;
using System;
using UnityEngine.UI;

namespace MiniRealisticAirways
{
    [HarmonyPatch(typeof(TakeoffTask), "Start", new Type[] {})]
    class PatchTakeoffTaskStart
    {
        static void Postfix(ref TakeoffTask __instance, ref Image ___AP)
        {
            BaseAircraftType currentAircraftType = __instance.gameObject.AddComponent<BaseAircraftType>();
            currentAircraftType.weight_ = BaseAircraftType.RandomWeight();
            
            Plugin.Log.LogInfo("TakeoffTask started with weight: " + currentAircraftType.weight_);

            ___AP.transform.DOScale(currentAircraftType.GetScaleFactor(), 0.5f).SetUpdate(isIndependentUpdate: true);
        }
    }

    [HarmonyPatch(typeof(TakeoffTask), "Update", new Type[] {})]
    class PatchTakeoffTaskUpdate
    {
        static void Postfix(ref TakeoffTask __instance, ref Image ___AP)
        {
            if (UnityEngine.Time.timeScale == 0f)
            {
                // Skip update during time pause.
                return;
            }

            BaseAircraftType currentAircraftType = __instance.gameObject.GetComponent<BaseAircraftType>();
            if (currentAircraftType == null)
            {
                return;
            }

            ___AP.transform.DOScale(currentAircraftType.GetScaleFactor(), 0.5f).SetUpdate(isIndependentUpdate: true);
        }
    }

    [HarmonyPatch(typeof(TakeoffTask), "SetupTakeoff", new Type[] {})]
    class PatchSetupTakeoff
    {
        static bool Prefix(ref TakeoffTask __instance)
        {
            ActiveAircraftType activeAircraftType = AircraftManager.Instance.GetComponent<ActiveAircraftType>();
            BaseAircraftType currentAircraftType = __instance.GetComponent<BaseAircraftType>();
            if (activeAircraftType == null || currentAircraftType == null)
            {
                return true;
            }

            activeAircraftType.weight_ = currentAircraftType.weight_;
            activeAircraftType.active_ = true;

            return true;
        }

        static void Postfix(ref TakeoffTask __instance)
        {
            ActiveAircraftType activeAircraftType = AircraftManager.Instance.GetComponent<ActiveAircraftType>();
            if (activeAircraftType == null)
            {
                return;
            }

            activeAircraftType.active_ = false;

        }
    }


    [HarmonyPatch(typeof(TakeoffTask), "OnPointUp", new Type[] {})]
    class PatchTakeoffTaskOnPointUp
    {
        static void RejectTakeoff(ref TakeoffTask __instance)
        {
            float duration2 = 0.5f;
            __instance.Panel.transform.DOScale(1f, duration2).SetUpdate(isIndependentUpdate: true);
            __instance.transform.DOMove(__instance.apron.gameObject.transform.position, duration2).SetUpdate(isIndependentUpdate: true);
            AudioManager.instance.PlayRejectTakeoff();

            __instance.inCommand = false;
            TakeoffTask.CurrentCommandingTakeoffTask = null;
            TakeoffTask.CurrentCommandingTakeoffPoint = null;
            TakeoffTask.CurrentCommandingRunway = null;
            foreach (Runway runway_ in Runway.Runways)
            {
                runway_.HideTakeoffPoints();
            }
        }

        static bool Prefix(ref TakeoffTask __instance)
        {
            if (!__instance.inCommand)
            {
                return false;
            }
            if (TakeoffTask.CurrentCommandingTakeoffPoint == null && __instance.apron != null && __instance.apron.gameObject != null)
            {
                return true;
            }

            // Can't take-off when wind isn't right.
            BaseAircraftType currentAircraftType = __instance.GetComponent<BaseAircraftType>();
            if (currentAircraftType == null)
            {
                return true;
            }

            WindSock windSock = Plugin.windsock_;
            Runway runway = TakeoffTask.CurrentCommandingTakeoffPoint.GetComponent<RunwayRef>().runway;
            if (windSock == null || runway == null)
            {
                return true;
            }

            float heading = runway.heading;
            if (TakeoffTask.CurrentCommandingTakeoffPoint == runway.TakeoffEnd.gameObject)
            {
                heading = (heading + 180f) % 360f;
            }
            if (!windSock.CanLand(heading, currentAircraftType.weight_))
            {
                RejectTakeoff(ref __instance);
                return false;
            }

            // Can't take-off when runway is closed.
            if (EventManager.closedRunway_ != null && EventManager.closedRunway_ == runway)
            {
                Plugin.Log.LogInfo("Rejected due to runway closed event.");
                RejectTakeoff(ref __instance);
                return false;
            }

            // TODO: Type based takeoff checking.

            return true;
        }
    }
}