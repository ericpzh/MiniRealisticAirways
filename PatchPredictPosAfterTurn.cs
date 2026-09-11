using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "PredictPosAfterTurn", new Type[] { typeof(float) })]
internal class PatchPredictPosAfterTurn
{
	private static bool Prefix(float angle, Aircraft __instance, out TurnSpeedScope __state)
	{
		__state = TurnSpeedScope.Enter(__instance);
		return true;
	}

	private static void Postfix(float angle, ref TurnSpeedScope __state)
	{
		__state.Dispose();
	}

	private static Exception Finalizer(float angle, ref TurnSpeedScope __state, Exception __exception)
	{
		__state.Dispose();
		return __exception;
	}
}
