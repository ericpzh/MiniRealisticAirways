using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "TurningRadius", MethodType.Getter)]
internal class PatchTurningRadius
{
	private static void Prefix(Aircraft __instance, out TurnSpeedScope __state)
	{
		__state = TurnSpeedScope.Enter(__instance);
	}

	private static void Postfix(ref TurnSpeedScope __state) => __state.Dispose();

	private static Exception Finalizer(ref TurnSpeedScope __state, Exception __exception)
	{
		__state.Dispose();
		return __exception;
	}
}
