using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(UpgradeManager), "Start", new Type[] { })]
internal class PatchUpgradeManagerStart
{
	// Start 在实例生命周期内只应执行一次；用实例级标记防止任何重复调用导致
	// upgradeInterval 被 2^N 递减、停机坪奖励重复发放。新场景的新实例仍会正常生效。
	private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UpgradeManager, object> appliedInstances_ = new System.Runtime.CompilerServices.ConditionalWeakTable<UpgradeManager, object>();

	private static bool Prefix(UpgradeManager __instance, ref float ___upgradeInterval, out bool __state)
	{
		__state = __instance != null && !appliedInstances_.TryGetValue(__instance, out _);
		if (__state)
		{
			appliedInstances_.Add(__instance, null);
			___upgradeInterval /= 2f;
		}
		return true;
	}

	public static IEnumerator AddApronAfterStart()
	{
		yield return new WaitForSeconds(0.5f);
		if (TakeoffTaskManager.Instance == null)
		{
			yield break;
		}
		for (int i = 0; i < 3; i++)
		{
			TakeoffTaskManager.Instance.AddApron();
		}
	}

	private static void Postfix(bool __state, ref int[] ___counter)
	{
		if (__state && ___counter != null && ___counter.Length > 1 && MapManager.gameMode != GameMode.SandBox && TakeoffTaskManager.Instance != null)
		{
			for (int i = 0; i < 3; i++)
			{
				___counter[1]++;
			}
			TakeoffTaskManager.Instance.StartCoroutine(AddApronAfterStart());
		}
	}
}
