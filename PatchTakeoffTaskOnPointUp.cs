using System;
using DG.Tweening;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(TakeoffTask), "OnPointUp", new Type[] { })]
internal class PatchTakeoffTaskOnPointUp
{
	private static void RejectTakeoff(ref TakeoffTask __instance)
	{
		float duration = 0.5f;
		if (__instance.Panel != null)
		{
			__instance.Panel.transform.DOScale(1f, duration).SetUpdate(isIndependentUpdate: true);
		}
		if (__instance.transform != null && __instance.apron != null)
		{
			__instance.transform.DOMove(__instance.apron.gameObject.transform.position, duration).SetUpdate(isIndependentUpdate: true);
		}
		if (AudioManager.instance != null)
		{
			AudioManager.instance.PlayCanNotComply();
		}
		__instance.inCommand = false;
		TakeoffTask.CurrentCommandingTakeoffTask = null;
		TakeoffTask.CurrentCommandingTakeoffPoint = null;
		TakeoffTask.CurrentCommandingRunway = null;
		if (Runway.Runways == null)
		{
			return;
		}
		foreach (Runway runway in Runway.Runways)
		{
			if (runway != null)
			{
				runway.HideTakeoffPoints();
			}
		}
	}

	private static bool Prefix(ref TakeoffTask __instance)
	{
		if (__instance == null || !__instance.inCommand)
		{
			return false;
		}
		if (TakeoffTask.CurrentCommandingTakeoffPoint == null && __instance.apron != null && __instance.apron.gameObject != null)
		{
			return true;
		}
		BaseAircraftType component = __instance.GetComponent<BaseAircraftType>();
		if (component == null)
		{
			return true;
		}
		WindSock windsock_ = Plugin.windsock_;
		RunwayRef runwayRef = TakeoffTask.CurrentCommandingTakeoffPoint.GetComponent<RunwayRef>();
		if (runwayRef == null)
		{
			return true;
		}
		Runway runway = runwayRef.runway;
		if (windsock_ == null || runway == null)
		{
			return true;
		}
		float num = runway.heading;
		if (runway.TakeoffEnd != null && TakeoffTask.CurrentCommandingTakeoffPoint == runway.TakeoffEnd.gameObject)
		{
			num = (num + 180f) % 360f;
		}
		if (!windsock_.CanLand(num, component.weight_))
		{
			RejectTakeoff(ref __instance);
			return false;
		}
		if (RunwayClose.IsRunwayClosed(runway))
		{
			Plugin.Log?.LogInfo("Rejected due to runway closed event.");
			RejectTakeoff(ref __instance);
			return false;
		}
		return true;
	}
}
