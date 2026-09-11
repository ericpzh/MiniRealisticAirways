using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] { typeof(WaypointAutoHover) })]
internal class PatchSetVectorToWaypointAutoHover
{
	private static bool Prefix(WaypointAutoHover waypoint, Aircraft __instance, out float __state)
	{
		__state = TargetSpeedProtection.Capture(__instance);
		return true;
	}

	private static void Postfix(WaypointAutoHover waypoint, Aircraft __instance, float __state)
	{
		TargetSpeedProtection.Restore(__instance, __state);
	}

	private static Exception Finalizer(Aircraft __instance, float __state, Exception __exception)
	{
		TargetSpeedProtection.Restore(__instance, __state);
		return __exception;
	}
}
