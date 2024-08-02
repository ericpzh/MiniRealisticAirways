using HarmonyLib;
using System.Collections;
using System;
using UnityEngine;

namespace MiniRealisticAirways
{
    [HarmonyPatch(typeof(UpgradeManager), "Start", new Type[] { })]
    class PatchUpgradeManagerStart
    {
        static bool Prefix(ref float ___upgradeInterval)
        {
            // Double the speed for upgrade.
            ___upgradeInterval /= 2;
            return true;
        }

        static public IEnumerator AddApronAfterStart()
        {
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < 3; i++)
            {
                TakeoffTaskManager.Instance.AddApron();
            }
        }

        static void Postfix(ref int[] ___counter)
        {
            if (___counter.Length == 0)
            {
                return;
            }

            if (MapManager.gameMode == GameMode.SandBox)
            {
                return;
            }

            // Starts with 3 apron upgrade.
            for (int i = 0; i < 3; i++)
            {
                ___counter[(int)UpgradeOpt.LONGER_TAXIWAY]++;
            }
            // Delay this action to avoid nullptr.
            TakeoffTaskManager.Instance.StartCoroutine(AddApronAfterStart());
        }
    }
}