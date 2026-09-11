using System;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "OnTriggerEnter2D", new Type[] { typeof(Collider2D) })]
internal class PatchOnTriggerEnter2D
{
	private static readonly int WaypointLayer = LayerMask.NameToLayer("Waypoint");

	private static readonly int AircraftSafetyLayer = LayerMask.NameToLayer("AircraftSafety");

	private static bool Prefix(Collider2D other, ref bool ___mainMenuMode, ref ColorCode.Option ___colorCode, ref ShapeCode.Option ___shapeCode, ref Aircraft __instance, ref bool ___reachExit)
	{
		if (other == null || __instance == null || ___mainMenuMode || !other.CompareTag("CollideCheck"))
		{
			return false;
		}
		AircraftSpeed aircraftSpeed;
		AircraftType aircraftType;
		if (other.gameObject.layer == WaypointLayer)
		{
			WaypointRef waypointRef = other.GetComponent<WaypointRef>();
			Waypoint waypoint = waypointRef == null ? null : waypointRef.waypoint;
			if (waypoint != null && ___colorCode == waypoint.colorCode && ___shapeCode == waypoint.shapeCode)
			{
				if (!AircraftState.GetAircraftStates(__instance, out var aircraftAltitude, out aircraftSpeed, out aircraftType))
				{
					return true;
				}
				if (!aircraftAltitude.altitudeDisabled_ && aircraftAltitude.altitude_ >= AltitudeLevel.Normal)
				{
					if (WaypointManager.Instance != null)
					{
						WaypointManager.Instance.Handoff(waypoint);
					}
					if (__instance.aircraftVoiceAndSubtitles != null)
					{
						__instance.aircraftVoiceAndSubtitles.PlayHandOff();
					}
					if (AircraftManager.Instance != null && AircraftManager.Instance.AircraftHandOffEvent != null)
					{
						AircraftManager.Instance.AircraftHandOffEvent.Invoke(__instance.gameObject.transform.position);
					}
					___reachExit = true;
					__instance.Handoff();
				}
				return false;
			}
		}
		AircraftRef aircraftRef = other.GetComponent<AircraftRef>();
		if (aircraftRef != null)
		{
			Aircraft aircraft = aircraftRef.aircraft;
			if (aircraft == null)
			{
				return false;
			}
			if (other.name == "TCAS")
			{
				if (!AircraftState.GetAircraftStates(__instance, out var aircraftAltitude2, out aircraftSpeed, out aircraftType) || !AircraftState.GetAircraftStates(aircraft, out var aircraftAltitude3, out aircraftSpeed, out aircraftType))
				{
					return true;
				}
				if (aircraftAltitude2.altitude_ == AltitudeLevel.Ground || __instance.OnTheGround || aircraftAltitude3.altitude_ == AltitudeLevel.Ground || aircraft.OnTheGround) return false;
				if (!AltitudeConflict.MayConflict(aircraftAltitude2, aircraftAltitude3))
				{
					return false;
				}
			}
		}
		else if (other.gameObject.layer == AircraftSafetyLayer)
		{
			Camera camera = Camera.main;
			Vector2 vector = camera == null ? new Vector2(-1f, -1f) : camera.WorldToViewportPoint(__instance.gameObject.transform.position);
			bool flag = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
			if (!AircraftState.GetAircraftStates(__instance, out var aircraftAltitude4, out aircraftSpeed, out aircraftType))
			{
				return true;
			}
			if (flag && aircraftAltitude4.altitude_ == AltitudeLevel.High)
			{
				return false;
			}
		}
		return true;
	}
}
