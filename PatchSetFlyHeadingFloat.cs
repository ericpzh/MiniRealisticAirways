using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "SetFlyHeading", new Type[] { typeof(float) })]
internal class PatchSetFlyHeadingFloat
{
	private static bool Prefix(float heading, Aircraft __instance, out float __state)
	{
		__state = TargetSpeedProtection.Capture(__instance);
		return true;
	}

	private static void Postfix(float heading, Aircraft __instance, float __state)
	{
		TargetSpeedProtection.Restore(__instance, __state);
	}

	private static Exception Finalizer(float heading, Aircraft __instance, float __state, Exception __exception)
	{
		TargetSpeedProtection.Restore(__instance, __state);
		return __exception;
	}
}
