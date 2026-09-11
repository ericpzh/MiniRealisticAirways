using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "PathBasedCollidePredict", new Type[]
{
	typeof(List<Vector3>),
	typeof(Aircraft)
})]
internal class PatchPathBasedCollidePredictAirAir
{
	private static bool Prefix(Aircraft __instance, List<Vector3> PathA, Aircraft otherAircraft, ref bool __result)
	{
		if (PathA == null || PathA.Count == 0 || otherAircraft == null)
		{
			return true;
		}
		Aircraft aircraft = __instance;
		if (!AircraftState.GetAircraftStates(aircraft, out var aircraftAltitude, out var aircraftSpeed, out var aircraftType) || !AircraftState.GetAircraftStates(otherAircraft, out var aircraftAltitude2, out aircraftSpeed, out aircraftType))
		{
			return true;
		}
		if (!AltitudeConflict.MayConflict(aircraftAltitude, aircraftAltitude2))
		{
			__result = false;
			return false;
		}
		return true;
	}
}
