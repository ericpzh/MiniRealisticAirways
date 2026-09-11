using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] { })]
internal class PatchSetFlyHeading
{
	private static bool Prefix(Aircraft __instance, out float __state)
	{
		__state = TargetSpeedProtection.Capture(__instance);
		return true;
	}

	private static void Postfix(Aircraft __instance, float __state)
	{
		TargetSpeedProtection.Restore(__instance, __state);
	}

	private static Exception Finalizer(Aircraft __instance, float __state, Exception __exception)
	{
		TargetSpeedProtection.Restore(__instance, __state);
		return __exception;
	}
}
