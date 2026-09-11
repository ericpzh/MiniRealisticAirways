using System;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "EnableVisualWarning", new Type[]
{
	typeof(GameObject),
	typeof(bool)
})]
internal class PatchEnableVisualWarning
{
	private static readonly int AircraftSafetyLayer = LayerMask.NameToLayer("AircraftSafety");

	private static void Postfix(GameObject other, bool isAircraftWarner, ref Aircraft __instance)
	{
		if (other != null && __instance != null && !isAircraftWarner && AircraftState.GetAircraftStates(__instance, out var aircraftAltitude, out var _, out var _) && (aircraftAltitude.altitude_ != AltitudeLevel.High || aircraftAltitude.targetAltitude_ != AltitudeLevel.High) && __instance.state != Aircraft.State.Landing && aircraftAltitude.tcasAction_ == TCASAction.None && other.layer == AircraftSafetyLayer)
		{
			AltitudeLevel previousTarget = aircraftAltitude.targetAltitude_;
			for (int i = (int)aircraftAltitude.targetAltitude_; i < (int)AltitudeLevel.High; i++)
			{
				aircraftAltitude.EmergencyClimb();
			}
			if (previousTarget != aircraftAltitude.targetAltitude_) Plugin.Log.LogInfo("GPWS activated, emergency climbing.");
		}
	}
}
