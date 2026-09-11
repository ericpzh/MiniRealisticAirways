using UnityEngine;

namespace MiniRealisticAirways;

// CreateInboundAircraft 返回后、Aircraft.Start 前挂载；一次提交所有继承状态。
internal sealed class ReturnFlightState : MonoBehaviour
{
	internal Weight Weight;
	internal AltitudeLevel Altitude;
	internal AltitudeLevel TargetAltitude;
	internal float Speed;
	internal float TargetSpeed;
	internal string CallSign;

	internal void Apply(AircraftState state)
	{
		state.aircraftAltitude_.RestoreFlightState(Altitude, TargetAltitude);
		state.aircraftSpeed_.RestoreFlightState(Speed, TargetSpeed);
		state.aircraftType_.weight_ = Weight;
		state.aircraftType_.percentFuelLeft_ = 20;
		if (state.aircraft_.callsignText != null)
		{
			state.aircraft_.ShowCallSign(true);
			state.aircraft_.callsignText.text = CallSign;
		}
	}
}
