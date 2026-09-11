using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "SetVectorTo", new Type[] { typeof(PlaceableWaypoint) })]
internal class PatchSetVectorToPlaceableWaypoint
{
	private static bool Prefix(PlaceableWaypoint waypoint, Aircraft __instance, out float __state)
	{
		__state = TargetSpeedProtection.Capture(__instance);
		return true;
	}

	private static void Postfix(PlaceableWaypoint waypoint, Aircraft __instance, float __state)
	{
		TargetSpeedProtection.Restore(__instance, __state);
	}

	private static Exception Finalizer(Aircraft __instance, float __state, Exception __exception)
	{
		TargetSpeedProtection.Restore(__instance, __state);
		return __exception;
	}
}
