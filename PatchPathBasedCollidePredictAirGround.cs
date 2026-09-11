using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "PathBasedCollidePredict", new Type[]
{
	typeof(List<Vector3>),
	typeof(Collider2D),
	typeof(float?),
	typeof(float?)
})]
internal class PatchPathBasedCollidePredictAirGround
{
	private static readonly int AircraftSafetyLayer = LayerMask.NameToLayer("AircraftSafety");

	private static bool Prefix(Aircraft __instance, List<Vector3> PathA, Collider2D restrictArea, float? PremittedHdg, float? HdgRange, ref bool __result)
	{
		if (PathA == null || PathA.Count == 0 || restrictArea == null)
		{
			return true;
		}
		Aircraft aircraft = __instance;
		if (!AircraftState.GetAircraftStates(aircraft, out var aircraftAltitude, out var _, out var _))
		{
			return true;
		}
		if (restrictArea.gameObject != null && restrictArea.gameObject.layer == AircraftSafetyLayer && aircraftAltitude.altitude_ == AltitudeLevel.High && aircraftAltitude.targetAltitude_ == AltitudeLevel.High)
		{
			__result = false;
			return false;
		}
		return true;
	}
}
