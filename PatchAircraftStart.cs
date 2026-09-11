using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "Start", new Type[] { })]
internal class PatchAircraftStart
{
	private static void Postfix(ref Aircraft __instance)
	{
		if (__instance == null)
		{
			return;
		}
		AircraftAltitude aircraftAltitude;
		AircraftSpeed aircraftSpeed;
		AircraftType aircraftType;
		if (__instance.direction == Aircraft.Direction.Inbound)
		{
			AircraftState aircraftState = __instance.GetComponent<AircraftState>();
			if (aircraftState == null)
			{
				aircraftState = __instance.gameObject.AddComponent<AircraftState>();
			}
			aircraftState.aircraft_ = __instance;
			aircraftState.Initialize();
			AircraftType aircraftType_ = aircraftState.aircraftType_;
			if (!(__instance is AirForceOneEVAircraft) && aircraftType_ != null)
			{
				ReturnFlightState returning = __instance.GetComponent<ReturnFlightState>();
				if (returning != null) returning.Apply(aircraftState);
				else
				{
					aircraftType_.weight_ = BaseAircraftType.RandomWeight();
					aircraftType_.percentFuelLeft_ = 99;
				}
				// 燃油与其闪烁子协程由同一个组件拥有。
				aircraftType_.StartCoroutine(aircraftType_.FuelManagementCoroutine());
				if (returning != null)
				{
					aircraftType_.StartCoroutine(aircraftType_.DisableFuelGaugeCoroutine());
					UnityEngine.Object.Destroy(returning);
				}
				aircraftType_.UpdateSprite();
			}
		}
		else if (AircraftState.GetAircraftStates(__instance, out aircraftAltitude, out aircraftSpeed, out aircraftType))
		{
			aircraftType.UpdateSprite();
		}
	}
}
