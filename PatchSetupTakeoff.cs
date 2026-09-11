using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(TakeoffTask), "SetupTakeoff", new Type[] { })]
internal class PatchSetupTakeoff
{
	private static bool Prefix(ref TakeoffTask __instance)
	{
		if (__instance == null || AircraftManager.Instance == null)
		{
			return true;
		}
		ActiveAircraftType component = AircraftManager.Instance.GetComponent<ActiveAircraftType>();
		BaseAircraftType component2 = __instance.GetComponent<BaseAircraftType>();
		if (component == null || component2 == null)
		{
			return true;
		}
		component.weight_ = component2.weight_;
		component.active_ = true;
		return true;
	}

	private static void Postfix(ref TakeoffTask __instance)
	{
		if (AircraftManager.Instance == null)
		{
			return;
		}
		ActiveAircraftType component = AircraftManager.Instance.GetComponent<ActiveAircraftType>();
		if (!(component == null))
		{
			component.active_ = false;
		}
	}

	private static Exception Finalizer(ref TakeoffTask __instance, Exception __exception)
	{
		// Prefix 已置 active_ 后若原方法抛异常，Postfix 不会执行；在此兜底复位，
		// 避免 ActiveAircraftType 卡在 active 状态污染后续机型判定。
		if (__exception != null && AircraftManager.Instance != null)
		{
			ActiveAircraftType component = AircraftManager.Instance.GetComponent<ActiveAircraftType>();
			if (component != null)
			{
				component.active_ = false;
			}
		}
		return __exception;
	}
}
