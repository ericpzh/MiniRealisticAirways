using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(AircraftManager), "Start", new Type[] { })]
internal class PatchAircraftManagerStart
{
	private static bool Prefix(ref AircraftManager __instance)
	{
		if (__instance == null)
		{
			return true;
		}
		if (__instance.GetComponent<ActiveAircraftType>() == null)
		{
			__instance.gameObject.AddComponent<ActiveAircraftType>();
		}
		return true;
	}

	private static void Postfix()
	{
		// Do not rely solely on Plugin's scene callback/coroutine: AircraftManager.Start
		// is the authoritative point at which gameplay services are available.
		Plugin.TryInitializeMapServices();
	}
}
