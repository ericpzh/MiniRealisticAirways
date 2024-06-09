using HarmonyLib;
using System;

namespace MiniRealisticAirways
{
    [HarmonyPatch(typeof(UpgradeManager), "Start", new Type[] {})]
    class PatchUpgradeManagerStart
    {
        static bool Prefix(ref UpgradeManager __instance, ref float ___upgradeInterval)
        {
            // Double the speed for upgrade.
            ___upgradeInterval /= 2;
            return true;
        }
    }

    [HarmonyPatch(typeof(UpgradeManager), "ProcessUpgrade", new Type[] {typeof(UpgradeOpt)})]
    class PatchProcessUpgrade
    {
        static bool Prefix(UpgradeOpt upgradeOpt, ref int[] ___counter)
        {
            if (upgradeOpt == UpgradeOpt.AUTO_HEADING_PROP)
            {
                // Accounts for the additional waypoint.
                ___counter[(int)upgradeOpt]++;
            }
            return true;
        }
    }
}