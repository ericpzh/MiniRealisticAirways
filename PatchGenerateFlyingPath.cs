using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "GenerateFlyingPath", new Type[] { typeof(int) })]
internal class PatchGenerateFlyingPath
{
	private static bool Prefix(Aircraft __instance, out TurnSpeedScope __state)
	{
		__state = TurnSpeedScope.Enter(__instance);
		return true;
	}

	private static void Postfix(ref TurnSpeedScope __state)
	{
		__state.Dispose();
	}

	private static Exception Finalizer(ref TurnSpeedScope __state, Exception __exception)
	{
		__state.Dispose();
		return __exception;
	}
}
