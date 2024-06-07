using HarmonyLib;
using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;

namespace MiniRealisticAirways
{
    [HarmonyPatch(typeof(UpgradeManager), "Start", new Type[] {})]
    class PatchProcessUpgrade
    {
        static bool Prefix(ref UpgradeManager __instance, ref float ___upgradeInterval)
        {
            // Double the speed for upgrade.
            ___upgradeInterval /= 2;
            return true;
        }
    }

    // [HarmonyPatch(typeof(UpgradeManager), "ProcessUpgrade", new Type[] {typeof(UpgradeOpt)})]
    // class PatchProcessUpgrade
    // {
    //     static bool Prefix(UpgradeOpt upgradeOpt, ref UpgradeManager __instance, ref object[] __state,
    //                        ref int[] ___counter, ref int ____doubleCheckIdx)
    //     {
    //         Plugin.Log.LogInfo("Here: " + upgradeOpt);
    //         if (upgradeOpt == UpgradeOpt.AUTO_HEADING_PROP)
    //         {
    //             // Spawn an additional waypoint.
    //             ___counter[(int)upgradeOpt]++;
    //             // __state = new object[] { 1 };
    //             WaypointPropsManager.Instance.SpawnWaypointAutoHeading();
    //             List<PlaceableWaypoint> waypointList = WaypointPropsManager.Instance.GetFieldValue<List<PlaceableWaypoint>>("placeableWaypoints");
    //             __state = new object[] {waypointList.Last()};

    //             WaypointPropsManager.Instance.HasPlacingProps = true;?

    //             // __instance.SelectUpgradeEvent.Invoke(upgradeOpt);
    //             // Debug.Log((object)upgradeOpt);
    //             // ____doubleCheckIdx = -1;

    //             // We are not done yet.
    //             // __instance.SetFieldValue<bool>("UpgradeComplete", false);

    //         }
    //         return true;
    //     }

    //     static void Postfix(UpgradeOpt upgradeOpt, ref UpgradeManager __instance, ref object[] __state)
    //     {
    //         Plugin.Log.LogInfo("Here:" + upgradeOpt);
    //         if (__state.Length > 0)
    //         {
    //             // WaypointPropsManager.Instance.SpawnWaypointAutoHeading();
    //             ((WaypointAutoHeading)__state[0]).Invoke("SetHeading", 0f);
    //         }
    //         // if (upgradeOpt == UpgradeOpt.AUTO_HEADING_PROP) {
    //         //     WaypointPropsManager.Instance.SpawnWaypointAutoHeading();
    //         // }
    //     }
    // }
}