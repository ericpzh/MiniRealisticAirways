namespace MiniRealisticAirways;

internal static class AltitudeConflict
{
	internal static bool MayConflict(AircraftAltitude left, AircraftAltitude right)
	{
		return left.altitude_ == right.altitude_
			|| left.targetAltitude_ == right.altitude_
			|| left.altitude_ == right.targetAltitude_
			|| left.targetAltitude_ == right.targetAltitude_;
	}
}
