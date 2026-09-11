using System;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(AircraftManager), "CreateOutboundAircraft", new Type[]
{
	typeof(Runway),
	typeof(Vector3),
	typeof(float),
	typeof(float),
	typeof(string),
	typeof(ColorCode.Option),
	typeof(ShapeCode.Option)
})]
internal class PatchCreateOutboundAircraft
{
	private static void Postfix(Runway runway, Vector3 position, float heading, float nominalHeading, string lr, ColorCode.Option colorCode, ShapeCode.Option shapeCode, ref AircraftManager __instance, ref Aircraft __result)
	{
		if (__result == null || __instance == null)
		{
			return;
		}
		AircraftState.GetAircraftState(__result, out var aircraftState);
		if (aircraftState == null && __result.direction == Aircraft.Direction.Outbound)
		{
			aircraftState = __result.gameObject.AddComponent<AircraftState>();
		}
		if (aircraftState == null)
		{
			return;
		}
		aircraftState.aircraft_ = __result;
		aircraftState.Initialize();
		AircraftType aircraftType_ = aircraftState.aircraftType_;
		ActiveAircraftType component = __instance.GetComponent<ActiveAircraftType>();
		if (aircraftType_ != null && component != null && component.active_)
		{
			aircraftType_.weight_ = component.weight_;
			Plugin.Log.LogDebug("Transferred aircraft with weight: " + aircraftType_.weight_);
		}
	}
}
