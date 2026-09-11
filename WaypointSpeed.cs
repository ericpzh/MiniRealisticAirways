using UnityEngine;

namespace MiniRealisticAirways;

public class WaypointSpeed : Speed
{
	public PlaceableWaypoint waypoint_;

	public SpeedLevel speed_;

	private WaypointSpeedGauge speedGauge_;

	private Camera mainCamera_;


	public override string ToString()
	{
		return Speed.ToString(speed_);
	}

	private void Start()
	{
		mainCamera_ = Camera.main;
		speed_ = SpeedLevel.Normal;
		if (!(waypoint_ == null) && !waypoint_.Invisible && waypoint_ is BaseWaypointAutoHeading)
		{
			speedGauge_ = waypoint_.GetComponent<WaypointSpeedGauge>();
			if (speedGauge_ == null)
			{
				speedGauge_ = waypoint_.gameObject.AddComponent<WaypointSpeedGauge>();
			}
			speedGauge_.waypoint_ = waypoint_;
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
			if (speedGauge_ != null && speed_ > SpeedLevel.Slow && Speed.InputSlowDown())
			{
				speedGauge_.UpdateWaypointSpeedGauge(--speed_);
			}
			if (speedGauge_ != null && speed_ < SpeedLevel.Fast && Speed.InputSpeedUp())
			{
				speedGauge_.UpdateWaypointSpeedGauge(++speed_);
			}
		}
	}
}
