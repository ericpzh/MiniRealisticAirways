namespace MiniRealisticAirways;

// Landing precedes the stock touchdown assignment and scaling animation.
// Only airborne approaches to the closed runway must go around.
internal enum RunwayClosePhase
{
	Other,
	Landing,
	TouchedDown,
	TakingOff
}

internal static class RunwayClosePolicy
{
	internal static bool ShouldGoAround(RunwayClosePhase phase, bool runwayMatches)
	{
		return phase == RunwayClosePhase.Landing && runwayMatches;
	}
}
