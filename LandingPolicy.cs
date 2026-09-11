namespace MiniRealisticAirways;

internal static class LandingPolicy
{
	internal static bool IsSpeedAllowed(float speed, Weight weight)
	{
		if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0f) return false;
		SpeedLevel level = Speed.ToModSpeed(speed);
		return weight == Weight.Light ? level < SpeedLevel.Normal : level <= SpeedLevel.Normal;
	}
}
