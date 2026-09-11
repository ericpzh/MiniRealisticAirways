using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "TrySetupLanding", new Type[]
{
	typeof(Runway),
	typeof(bool)
})]
internal class PatchTrySetupLanding
{
	private static readonly MethodInfo GenerateLandingPathMethod = typeof(Aircraft).GetMethod("GenerateLandingPathL1", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo ShowPathMethod = typeof(Aircraft).GetMethod("ShowPath", BindingFlags.Instance | BindingFlags.NonPublic);

	private static bool Prefix(Runway runway, bool doLand, Aircraft __instance, PlaceableWaypoint ____HARWCurWP, out TurnSpeedScope __state)
	{
		__state = TurnSpeedScope.Enter(__instance);
		Runway requestedRunway = runway ? runway : Aircraft.CurrentCommandingRunway;
		if (RunwayClose.IsRunwayClosed(requestedRunway))
		{
			RejectLanding(____HARWCurWP);
			return false;
		}
		if (!AircraftState.GetAircraftStates(__instance, out var aircraftAltitude, out var aircraftSpeed, out var aircraftType))
		{
			return true;
		}
		if (aircraftAltitude.CanLand())
		{
			int guard = 0;
			while (!aircraftSpeed.CanLand(aircraftType.weight_) && ++guard < Plugin.MAX_WHILE_LOOP_ITER)
			{
				float previous = __instance.targetSpeed;
				aircraftSpeed.AircraftSlowDown();
				if (__instance.targetSpeed == previous) break;
			}
		}
		if (!aircraftAltitude.CanLand() || !aircraftSpeed.CanLand(aircraftType.weight_))
		{
			Runway runway2 = (runway ? runway : Aircraft.CurrentCommandingRunway);
			Runway fieldValue = __instance.GetFieldValue<Runway>("LandingRunway");
			bool flag = __instance.state == Aircraft.State.Landing && runway2 == fieldValue;
			if (runway2 == null || GenerateLandingPathMethod == null || ShowPathMethod == null)
			{
				Plugin.Log?.LogError("Landing rejected because the runway or landing-path methods are unavailable.");
				RejectLanding(____HARWCurWP);
				return false;
			}
			try
			{
				object[] array = new object[4] { runway2, null, flag, true };
				GenerateLandingPathMethod.Invoke(__instance, array);
				List<Vector3> list = (List<Vector3>)array[1];
				if (list == null)
				{
					Plugin.Log?.LogError("Landing rejected because the landing path was not generated.");
					RejectLanding(____HARWCurWP);
					return false;
				}
				ShowPathMethod.Invoke(__instance, new object[2] { list, false });
			}
			catch (Exception exception)
			{
				Plugin.Log?.LogError("Landing path validation failed: " + exception.GetBaseException());
				RejectLanding(____HARWCurWP);
				return false;
			}
			RejectLanding(____HARWCurWP);
			return false;
		}
		return true;
	}

	private static void RejectLanding(PlaceableWaypoint currentWaypoint)
	{
		if (!(currentWaypoint is WaypointAutoLanding) && AudioManager.instance != null)
		{
			AudioManager.instance.PlayCanNotComply();
		}
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
