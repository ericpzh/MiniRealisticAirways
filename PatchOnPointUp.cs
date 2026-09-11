using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "OnPointUp", new Type[] { typeof(bool) })]
internal class PatchOnPointUp
{
	private static bool Prefix(bool external, Aircraft __instance, out float __state)
	{
		__state = TargetSpeedProtection.Capture(__instance);
		return true;
	}

	private static void Postfix(bool external, Aircraft __instance, float __state)
	{
		TargetSpeedProtection.Restore(__instance, __state);
	}

	private static Exception Finalizer(Aircraft __instance, float __state, Exception __exception)
	{
		TargetSpeedProtection.Restore(__instance, __state);
		return __exception;
	}
}
