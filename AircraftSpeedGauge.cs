using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways;

public class AircraftSpeedGauge : Gauge
{
	public Aircraft aircraft_;

	public IEnumerator GetTransitioningCoroutine(SpeedLevel speed, SpeedLevel targetSpeed)
	{
		return TransitioningCoroutine((int)speed, (int)targetSpeed);
	}

	public void UpdateGauge(SpeedLevel speed)
	{
		UpdateGaugeSpriteRenderers((int)(speed - 1));
	}

	private void Start()
	{
		if (aircraft_ != null)
		{
			TryInitialize();
		}
	}

	protected override void ConfigureRenderers()
	{
		if (aircraft_ == null)
		{
			throw new System.InvalidOperationException("仪表所属对象不可用。");
		}
		for (int i = 0; i < 3; i++)
		{
			gameObjects_[i].transform.SetParent(aircraft_.transform);
			gameObjects_[i].transform.localScale = new Vector3(1.5f, 1.5f, 1f);
			gameObjects_[i].transform.localPosition = new Vector3(1.5f + (float)i * 0.5f, -0.5f, -9f);
			gameObjects_[i].transform.rotation = Quaternion.AngleAxis(90f, Vector3.back);
		}
		AircraftVisualSortingController sorting = AircraftVisualSortingController.GetOrCreate(aircraft_);
		if (sorting != null)
		{
			sorting.RegisterGaugeRenderers(spriteRenderers_);
		}
	}
}
