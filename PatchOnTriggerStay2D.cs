using System;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "OnTriggerStay2D", new Type[] { typeof(Collider2D) })]
internal class PatchOnTriggerStay2D
{
	private static readonly int AircraftSafetyLayer = LayerMask.NameToLayer("AircraftSafety");

	// TCAS 触发距离（平方），原魔法数字 9f。
	private const float TcasDistanceSquared = 9f;

	// Camera.main 每次调用都做场景查找；GPWS 判定在每物理帧触发，跨帧缓存。
	private static Camera cachedMainCamera_;

	internal static void ResetCameraCache()
	{
		cachedMainCamera_ = null;
	}

	private static bool Prefix(Collider2D other, ref bool ___mainMenuMode, ref Aircraft __instance)
	{
		if (other == null || __instance == null || ___mainMenuMode || !other.CompareTag("CollideCheck"))
		{
			return false;
		}
		AircraftSpeed aircraftSpeed;
		AircraftType aircraftType;
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
				if (aircraft.IsHovering || __instance.IsHovering)
				{
					return false;
				}
				if (!AircraftState.GetAircraftStates(__instance, out var aircraftAltitude, out aircraftSpeed, out aircraftType) || !AircraftState.GetAircraftStates(aircraft, out var aircraftAltitude2, out aircraftSpeed, out aircraftType))
				{
					return true;
				}
				if (aircraftAltitude.altitude_ == AltitudeLevel.Ground || __instance.OnTheGround || aircraftAltitude2.altitude_ == AltitudeLevel.Ground || aircraft.OnTheGround)
				{
					return false;
				}
				if (!AltitudeConflict.MayConflict(aircraftAltitude, aircraftAltitude2))
				{
					return false;
				}
				Vector2 delta = (Vector2)__instance.gameObject.transform.position - (Vector2)aircraft.gameObject.transform.position;
				if (!Settings.DISABLE_TCAS && delta.sqrMagnitude < TcasDistanceSquared && aircraftAltitude.tcasAction_ == TCASAction.None && aircraftAltitude2.tcasAction_ == TCASAction.None)
				{
					AltitudeLevel previousSelf = aircraftAltitude.targetAltitude_;
					AltitudeLevel previousOther = aircraftAltitude2.targetAltitude_;
					if (!aircraftAltitude.IsLanding() || !aircraftAltitude2.IsLanding())
					{
						if (aircraftAltitude.IsLanding())
						{
							aircraftAltitude2.EmergencyClimb();
						}
						else if (aircraftAltitude2.IsLanding())
						{
							aircraftAltitude.EmergencyClimb();
						}
						else if (aircraftAltitude.targetAltitude_ == AltitudeLevel.High && (aircraftAltitude2.targetAltitude_ == AltitudeLevel.High || aircraftAltitude2.altitude_ == AltitudeLevel.High))
						{
							aircraftAltitude.EmergencyDescend();
						}
						else if (aircraftAltitude2.targetAltitude_ == AltitudeLevel.High && (aircraftAltitude.targetAltitude_ == AltitudeLevel.High || aircraftAltitude.altitude_ == AltitudeLevel.High))
						{
							aircraftAltitude2.EmergencyDescend();
						}
						else if (aircraftAltitude.targetAltitude_ == AltitudeLevel.Low && (aircraftAltitude2.targetAltitude_ == AltitudeLevel.Low || aircraftAltitude2.altitude_ == AltitudeLevel.Low))
						{
							aircraftAltitude.EmergencyClimb();
						}
						else if (aircraftAltitude2.targetAltitude_ == AltitudeLevel.Low && (aircraftAltitude.targetAltitude_ == AltitudeLevel.Low || aircraftAltitude.altitude_ == AltitudeLevel.Low))
						{
							aircraftAltitude2.EmergencyClimb();
						}
						else if (aircraftAltitude.altitude_ == AltitudeLevel.Low && aircraftAltitude2.altitude_ == AltitudeLevel.Low)
						{
							aircraftAltitude2.EmergencyClimb();
						}
						else if (aircraftAltitude.altitude_ == AltitudeLevel.High && aircraftAltitude2.altitude_ == AltitudeLevel.High)
						{
							aircraftAltitude2.EmergencyDescend();
						}
						else
						{
							aircraftAltitude.EmergencyClimb();
							aircraftAltitude2.EmergencyDescend();
						}
					}
					if (previousSelf != aircraftAltitude.targetAltitude_ || previousOther != aircraftAltitude2.targetAltitude_)
						Plugin.Log?.LogInfo("TCAS activated");
				}
			}
		}
		else if (other.gameObject.layer == AircraftSafetyLayer)
		{
			Camera camera = cachedMainCamera_ == null ? (cachedMainCamera_ = Camera.main) : cachedMainCamera_;
			Vector2 vector = camera == null ? new Vector2(-1f, -1f) : camera.WorldToViewportPoint(__instance.gameObject.transform.position);
			bool flag = vector.x >= 0f && vector.x <= 1f && vector.y >= 0f && vector.y <= 1f;
			if (!AircraftState.GetAircraftStates(__instance, out var aircraftAltitude3, out aircraftSpeed, out aircraftType))
			{
				return true;
			}
			if (flag && aircraftAltitude3.altitude_ < AltitudeLevel.High && LevelManager.Instance != null)
			{
				LevelManager.Instance.CrashGameOver(__instance, null);
				return false;
			}
			if (flag && aircraftAltitude3.targetAltitude_ < AltitudeLevel.High)
			{
				AltitudeLevel previousTarget = aircraftAltitude3.targetAltitude_;
			for (int i = (int)aircraftAltitude3.targetAltitude_; i < (int)AltitudeLevel.High; i++)
				{
					aircraftAltitude3.EmergencyClimb(priority: true);
				}
				if (previousTarget == aircraftAltitude3.targetAltitude_) return true;
				Plugin.Log.LogInfo("GPWS activated, emergency climbing.");
				if (__instance.aircraftVoiceAndSubtitles != null)
				{
					__instance.aircraftVoiceAndSubtitles.PlayTerrain();
				}
			}
		}
		return true;
	}
}
