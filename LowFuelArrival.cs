using UnityEngine;

namespace MiniRealisticAirways;

public class LowFuelArrival : Event
{
	public override bool Trigger()
	{
		Aircraft aircraft = null;
		Aircraft[] inboundAircraft = AircraftManager.GetInboundAircraft();
		Camera camera = Camera.main;
		if (inboundAircraft == null || camera == null)
		{
			return false;
		}
		foreach (Aircraft aircraft2 in inboundAircraft)
		{
			if (aircraft2 == null)
			{
				continue;
			}
			Vector2 vector = camera.WorldToViewportPoint(aircraft2.gameObject.transform.position);
			if (!(vector.x >= 0f) || !(vector.x <= 1f) || !(vector.y >= 0f) || !(vector.y <= 1f))
			{
				aircraft = aircraft2;
				break;
			}
		}
		if (aircraft == null)
		{
			return false;
		}
		if (!AircraftState.GetAircraftStates(aircraft, out var _, out var _, out var aircraftType))
		{
			return false;
		}
		Plugin.Log.LogInfo("Generated emergency low fuel aircraft.");
		aircraftType.percentFuelLeft_ = 40;
		Plugin.Log?.LogInfo("LowFuelArrival Triggered.");
		return true;
	}
}
