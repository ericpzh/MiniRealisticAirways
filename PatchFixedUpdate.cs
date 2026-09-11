using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "FixedUpdate", new Type[] { })]
internal class PatchFixedUpdate
{
	// 只在初始化时解析一次私有方法；正常物理帧直接调用开放实例委托。
	private static readonly Action<Aircraft> UpdateSpeed = CreateUpdateSpeedDelegate();
	private static bool missingUpdateSpeedLogged_;

	private static Action<Aircraft> CreateUpdateSpeedDelegate()
	{
		try
		{
			var method = AccessTools.Method(typeof(Aircraft), "UpdateSpeed", Type.EmptyTypes);
			return method == null ? null : (Action<Aircraft>)Delegate.CreateDelegate(typeof(Action<Aircraft>), method);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogWarning("Could not bind Aircraft.UpdateSpeed; retaining Unity Invoke fallback: " + exception.GetBaseException().Message);
			return null;
		}
	}

	private static bool Prefix(ref Aircraft __instance)
	{
		if (__instance == null)
		{
			return true;
		}
		if (__instance.state == Aircraft.State.Landing)
		{
			if (UpdateSpeed != null)
			{
				UpdateSpeed(__instance);
			}
			else
			{
				if (!missingUpdateSpeedLogged_)
				{
					missingUpdateSpeedLogged_ = true;
					Plugin.Log?.LogWarning("Aircraft.UpdateSpeed delegate unavailable; landing speed uses Unity Invoke fallback.");
				}
				__instance.Invoke("UpdateSpeed", 0f);
			}
		}
		if (!AircraftState.GetAircraftState(__instance, out var aircraftState))
		{
			return true;
		}
		if (EventManager.weather_ == null || !EventManager.weather_.enabled_)
		{
			aircraftState.weatherAffected_ = false;
			return true;
		}
		if (aircraftState.weatherAffected_)
		{
			return true;
		}
		AircraftAltitude aircraftAltitude_ = aircraftState.aircraftAltitude_;
		if (aircraftAltitude_ == null)
		{
			return true;
		}
		if (aircraftAltitude_.altitude_ < AltitudeLevel.High && __instance.AP != null && EventManager.weather_.InCell(__instance.AP.transform.position))
		{
			Plugin.Log.LogInfo("Aircraft entered weather cell.");
			aircraftState.weatherAffected_ = true;
			if (RestrictedAreaManager.Instance != null)
			{
				RestrictedAreaManager.Instance.AreaEnter(__instance);
			}
			if (__instance.state != Aircraft.State.Landing && aircraftAltitude_.tcasAction_ == TCASAction.None)
			{
				for (int i = (int)aircraftAltitude_.targetAltitude_; i < (int)AltitudeLevel.High; i++)
				{
					aircraftAltitude_.EmergencyClimb();
				}
				Plugin.Log.LogInfo("Weather effected, emergency climbing.");
			}
		}
		return true;
	}
}
