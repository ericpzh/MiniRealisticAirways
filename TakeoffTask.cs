using DG.Tweening;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
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

    [HarmonyPatch(typeof(TakeoffTask), "HasConflictLandingAircraft", new Type[] {})]
    class PatchHasConflictLandingAircraft
    {
        static void Postfix(ref TakeoffTask __instance)
        {
            // TODO: Type based takeoff checking.
        }
    }
    
}