using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "AircraftOOBGameOver", new Type[] { typeof(Aircraft) })]
internal class PatchAircraftOOBGameOver
{
	private static bool Prefix(Aircraft aircraft)
	{
		RestrictedAreaManager restrictedAreaManager = RestrictedAreaManager.Instance;
		if (aircraft == null || restrictedAreaManager == null)
		{
			return true;
		}
		if (restrictedAreaManager.counter > 1)
		{
			if (!AircraftState.GetAircraftState(aircraft, out var aircraftState))
			{
				return true;
			}
			aircraftState.StartCoroutine(aircraftState.DelayDestroyCoroutine());
			restrictedAreaManager.AreaEnter(aircraft);
			aircraft.aircraftEverInView = false;
			return false;
		}
		return true;
	}
}
