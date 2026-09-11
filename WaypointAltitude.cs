using UnityEngine;

namespace MiniRealisticAirways;

public class WaypointAltitude : Altitude
{
	public PlaceableWaypoint waypoint_;

	private WaypointAltitudeGauge altitudeGauge_;

	private Camera mainCamera_;


	public AltitudeLevel altitude_ { get; private set; }

	public override string ToString()
	{
		return Altitude.ToString(altitude_);
	}

	private void Start()
	{
		mainCamera_ = Camera.main;
		altitude_ = AltitudeLevel.Normal;
		if (!(waypoint_ == null) && !waypoint_.Invisible && waypoint_ is BaseWaypointAutoHeading)
		{
			altitudeGauge_ = waypoint_.GetComponent<WaypointAltitudeGauge>();
			if (altitudeGauge_ == null)
			{
				altitudeGauge_ = waypoint_.gameObject.AddComponent<WaypointAltitudeGauge>();
			}
			altitudeGauge_.waypoint_ = waypoint_;
		}
	}

	private void Update()
	{
		if (waypoint_ == null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if (Time.timeScale == 0f || WaypointNameInput.AnyActive || !(waypoint_ is BaseWaypointAutoHeading))
		{
			return;
		}
		if (PointerUtility.IsOver(waypoint_.transform, ref mainCamera_))
		{
			if (altitudeGauge_ != null && altitude_ < AltitudeLevel.High && Altitude.InputClimb())
			{
				altitudeGauge_.UpdateWaypointAltitudeGauge(++altitude_);
			}
			if (altitudeGauge_ != null && altitude_ > AltitudeLevel.Low && Altitude.InputDescend())
			{
				altitudeGauge_.UpdateWaypointAltitudeGauge(--altitude_);
			}
		}
	}
}
