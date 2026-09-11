using System;
using DG.Tweening;
using HarmonyLib;
using UnityEngine.UI;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(TakeoffTask), "Start", new Type[] { })]
internal class PatchTakeoffTaskStart
{
	private static void Postfix(ref TakeoffTask __instance, ref Image ___AP)
	{
		if (__instance == null)
		{
			return;
		}
		BaseAircraftType baseAircraftType = __instance.GetComponent<BaseAircraftType>();
		if (baseAircraftType == null)
		{
			baseAircraftType = __instance.gameObject.AddComponent<BaseAircraftType>();
		}
		baseAircraftType.weight_ = BaseAircraftType.RandomWeight();
		Plugin.Log.LogInfo("TakeoffTask started with weight: " + baseAircraftType.weight_);
		if (___AP != null)
		{
			___AP.raycastTarget = true;
			___AP.transform.DOScale(baseAircraftType.GetScaleFactor(), 0.5f).SetUpdate(isIndependentUpdate: true);
		}
		if (__instance.Panel != null)
		{
			__instance.Panel.raycastTarget = true;
		}
	}
}
