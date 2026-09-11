using System;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(PlaceableWaypoint), "Start", new Type[] { })]
internal class PatchPlaceableWaypointStart
{
	private static bool Prefix(ref PlaceableWaypoint __instance)
	{
		if (__instance == null)
		{
			return true;
		}
		WaypointState waypointState = __instance.GetComponent<WaypointState>();
		if (waypointState == null)
		{
			waypointState = __instance.gameObject.AddComponent<WaypointState>();
		}
		waypointState.waypoint_ = __instance;
		WaypointAltitude waypointAltitude = __instance.GetComponent<WaypointAltitude>();
		if (waypointAltitude == null)
		{
			waypointAltitude = __instance.gameObject.AddComponent<WaypointAltitude>();
		}
		waypointAltitude.waypoint_ = __instance;
		WaypointSpeed waypointSpeed = __instance.GetComponent<WaypointSpeed>();
		if (waypointSpeed == null)
		{
			waypointSpeed = __instance.gameObject.AddComponent<WaypointSpeed>();
		}
		waypointSpeed.waypoint_ = __instance;
		WaypointNameInput waypointNameInput = __instance.GetComponent<WaypointNameInput>();
		if (waypointNameInput == null)
		{
			waypointNameInput = __instance.gameObject.AddComponent<WaypointNameInput>();
		}
		waypointNameInput.waypoint_ = __instance;
		return true;
	}
}
