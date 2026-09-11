using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "AircraftCollideGameOver", new Type[]
{
	typeof(Aircraft),
	typeof(Aircraft)
})]
internal class PatchAircraftCollideGameOver
{
	private static bool Prefix(Aircraft aircraft1, Aircraft aircraft2)
	{
		if (!AircraftState.GetAircraftStates(aircraft1, out var aircraftAltitude, out var aircraftSpeed, out var aircraftType) || !AircraftState.GetAircraftStates(aircraft2, out var aircraftAltitude2, out aircraftSpeed, out aircraftType))
		{
			return true;
		}
		return aircraftAltitude.altitude_ == aircraftAltitude2.altitude_;
	}
}
