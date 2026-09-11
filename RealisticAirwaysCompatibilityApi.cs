namespace MiniRealisticAirways;

/// <summary>
/// Small, optional integration surface for map mods. Consumers may discover
/// this type by reflection and remain independent of this assembly at compile
/// time.
/// </summary>
public static class RealisticAirwaysCompatibilityApi
{
	public const int ApiVersion = 1;

	public const bool SupportsAircraftVisualSorting = true;

	public const bool SupportsAircraftHudLayout = true;

	public const bool SupportsStrictStockTypography = true;

	public const bool SupportsRunwayClosedQuery = true;

	/// <summary>Gets the currently closed runway, or null when no runway event is active.</summary>
	public static Runway ClosedRunway => EventManager.closedRunway_;

	public static bool IsRunwayClosed(Runway runway)
	{
		return RunwayClose.IsRunwayClosed(runway);
	}

	public static bool TryGetClosedRunway(out Runway runway)
	{
		runway = EventManager.closedRunway_;
		return runway != null;
	}

	public static bool RefreshAircraftVisualSorting(Aircraft aircraft)
	{
		AircraftVisualSortingController controller = aircraft == null ? null : aircraft.GetComponent<AircraftVisualSortingController>();
		if (controller == null)
		{
			return false;
		}
		controller.RefreshVisualGroup();
		return true;
	}
}
