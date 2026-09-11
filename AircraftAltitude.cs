using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways;

public class AircraftAltitude : Altitude
{
	public Aircraft aircraft_;

	public AltitudeLevel altitude_;

	public AltitudeLevel targetAltitude_;

	public TCASAction tcasAction_ = TCASAction.None;

	public bool altitudeDisabled_ = false;

	public IEnumerator enableAltitudeGaugeCoroutine_ = null;

	private AircraftAltitudeGauge altitudeGauge_;

	private const float TRANSITION_TIME = 5f;

	private IEnumerator transitioningCoroutine_ = null;

	private IEnumerator blinkCoroutine_ = null;

	private bool isEmergencyTransitioning_ = false;

	private Camera mainCamera_;

	private bool flightStateInitialized_;

	internal void RestoreFlightState(AltitudeLevel altitude, AltitudeLevel target)
	{
		flightStateInitialized_ = true;
		altitude_ = altitude;
		targetAltitude_ = target;
		tcasAction_ = TCASAction.Disabled;
	}

	public override string ToString()
	{
		if (altitude_ != targetAltitude_ && Animation.Blink())
		{
			return " ";
		}
		return Altitude.ToString(altitude_);
	}

	public bool CanLand()
	{
		// 建立进近只看指令，允许飞机在进近途中完成下降。
		return targetAltitude_ <= AltitudeLevel.Low;
	}

	public bool CanTouchDown()
	{
		return altitude_ == AltitudeLevel.Low && CanLand();
	}

	internal void CompleteTouchdown()
	{
		if (transitioningCoroutine_ != null) StopCoroutine(transitioningCoroutine_);
		if (blinkCoroutine_ != null) StopCoroutine(blinkCoroutine_);
		if (enableAltitudeGaugeCoroutine_ != null) StopCoroutine(enableAltitudeGaugeCoroutine_);
		transitioningCoroutine_ = null;
		blinkCoroutine_ = null;
		enableAltitudeGaugeCoroutine_ = null;
		isEmergencyTransitioning_ = false;
		altitude_ = AltitudeLevel.Ground;
		targetAltitude_ = AltitudeLevel.Ground;
		tcasAction_ = TCASAction.Disabled;
		altitudeGauge_?.DisableSpriteRenderer();
	}

	public IEnumerator EnableAltitudeGauge(AltitudeLevel altitude)
	{
		if (altitudeGauge_ == null)
		{
			yield break;
		}
		while (this != null && aircraft_ != null && altitudeGauge_ != null && !altitudeGauge_.Ready())
		{
			altitudeGauge_.TryInitialize();
			if (altitudeGauge_.InitializationFailed())
			{
				yield break;
			}
			yield return new WaitForFixedUpdate();
		}
		if (this != null && aircraft_ != null && altitudeGauge_ != null)
		{
			altitudeGauge_.UpdateGauge(altitude);
		}
	}

	public void AircraftClimb()
	{
		if (!altitudeDisabled_ && targetAltitude_ < AltitudeLevel.High)
		{
			targetAltitude_++;
			AltitudeTransition();
		}
	}

	public void AircraftDescend()
	{
		if (!altitudeDisabled_ && targetAltitude_ > AltitudeLevel.Low)
		{
			targetAltitude_--;
			AltitudeTransition();
		}
	}

	public void EmergencyClimb(bool priority = false)
	{
		if (!Settings.DISABLE_TCAS && !altitudeDisabled_ && altitude_ != AltitudeLevel.Ground)
		{
			if (targetAltitude_ < AltitudeLevel.High)
			{
				tcasAction_ = TCASAction.Climb;
				targetAltitude_++;
				EmergencyAltitudeTransition(priority);
			}
		}
	}

	public void EmergencyDescend()
	{
		if (!Settings.DISABLE_TCAS && !altitudeDisabled_ && altitude_ != AltitudeLevel.Ground)
		{
			if (targetAltitude_ > AltitudeLevel.Low)
			{
				tcasAction_ = TCASAction.Descend;
				targetAltitude_--;
				EmergencyAltitudeTransition();
			}
		}
	}

	public bool IsLanding()
	{
		return aircraft_ != null && aircraft_.state == Aircraft.State.Landing;
	}

	private void Start()
	{
		if (aircraft_ == null)
		{
			return;
		}
		mainCamera_ = Camera.main;
		altitudeGauge_ = aircraft_.GetComponent<AircraftAltitudeGauge>();
		if (altitudeGauge_ == null)
		{
			altitudeGauge_ = aircraft_.gameObject.AddComponent<AircraftAltitudeGauge>();
		}
		altitudeGauge_.aircraft_ = aircraft_;
		if (flightStateInitialized_)
		{
			if (altitude_ != targetAltitude_) AltitudeTransition();
			return;
		}
		flightStateInitialized_ = true;
		if (aircraft_.direction == Aircraft.Direction.Outbound)
		{
			altitude_ = AltitudeLevel.Ground;
			targetAltitude_ = AltitudeLevel.Low;
		}
		if (aircraft_.direction == Aircraft.Direction.Inbound)
		{
			if (aircraft_ is AirForceOneEVAircraft)
			{
				altitude_ = AltitudeLevel.Low;
				targetAltitude_ = AltitudeLevel.Low;
			}
			else
			{
				altitude_ = AltitudeLevel.High;
				targetAltitude_ = AltitudeLevel.High;
			}
		}
	}

	private void Update()
	{
		if (aircraft_ == null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		TakeoffTouchdownArrivalProcess();
	}

	private void AltitudeTransition()
	{
		if (transitioningCoroutine_ == null)
		{
			transitioningCoroutine_ = AltitudeTransitionCoroutine(targetAltitude_);
			StartCoroutine(transitioningCoroutine_);
		}
	}

	private void EmergencyAltitudeTransition(bool priority = false)
	{
		if (priority || !isEmergencyTransitioning_)
		{
			isEmergencyTransitioning_ = true;
			if (transitioningCoroutine_ != null)
			{
				StopCoroutine(transitioningCoroutine_);
			}
			if (blinkCoroutine_ != null)
			{
				StopCoroutine(blinkCoroutine_);
			}
			transitioningCoroutine_ = AltitudeTransitionCoroutine(targetAltitude_);
			StartCoroutine(transitioningCoroutine_);
		}
	}

	private void TakeoffTouchdownArrivalProcess()
	{
		if (altitude_ == AltitudeLevel.Ground && aircraft_.direction == Aircraft.Direction.Outbound && (aircraft_.state == Aircraft.State.Flying || aircraft_.state == Aircraft.State.HeadingAfterReachingWaypoint))
		{
			altitude_ = AltitudeLevel.Low;
			targetAltitude_ = AltitudeLevel.Low;
			if (enableAltitudeGaugeCoroutine_ == null)
			{
				enableAltitudeGaugeCoroutine_ = EnableAltitudeGauge(altitude_);
				StartCoroutine(enableAltitudeGaugeCoroutine_);
			}
		}
		if (altitude_ != AltitudeLevel.Ground && AircraftState.DisableStateOnTouchedDown(aircraft_))
		{
			CompleteTouchdown();
		}
		if (mainCamera_ == null)
		{
			mainCamera_ = Camera.main;
		}
		bool flag = AircraftViewport.IsVisible(aircraft_, mainCamera_);
		if (aircraft_.direction == Aircraft.Direction.Inbound && flag && enableAltitudeGaugeCoroutine_ == null)
		{
			enableAltitudeGaugeCoroutine_ = EnableAltitudeGauge(altitude_);
			StartCoroutine(enableAltitudeGaugeCoroutine_);
		}
	}

	private IEnumerator AltitudeTransitionCoroutine(AltitudeLevel targetAltitude)
	{
		// 仪表只是显示层；即使缺失或初始化失败，高度和避撞仍按时推进。
		while (aircraft_ != null)
		{
			blinkCoroutine_ = altitudeGauge_ != null && altitudeGauge_.TryInitialize()
				? altitudeGauge_.GetTransitioningCoroutine(altitude_, targetAltitude)
				: null;
			if (blinkCoroutine_ != null)
			{
				StartCoroutine(blinkCoroutine_);
			}
			yield return new WaitForSeconds(TRANSITION_TIME);
			if (aircraft_ == null)
			{
				transitioningCoroutine_ = null;
				isEmergencyTransitioning_ = false;
				yield break;
			}
			// 同帧内触地可早于 Update：过期的下降/TCAS协程不得恢复空中高度。
			if (AircraftState.DisableStateOnTouchedDown(aircraft_))
			{
				CompleteTouchdown();
				yield break;
			}
			altitude_ = targetAltitude;
			if (tcasAction_ != TCASAction.Disabled)
			{
				tcasAction_ = TCASAction.None;
			}
			if (blinkCoroutine_ != null)
			{
				StopCoroutine(blinkCoroutine_);
				blinkCoroutine_ = null;
			}
			if (altitudeGauge_ != null && altitudeGauge_.Ready())
			{
				altitudeGauge_.UpdateGauge(altitude_);
			}
			if (altitude_ == targetAltitude_)
			{
				transitioningCoroutine_ = null;
				isEmergencyTransitioning_ = false;
				yield break;
			}
			targetAltitude = targetAltitude_;
		}
		transitioningCoroutine_ = null;
		isEmergencyTransitioning_ = false;
	}

	private void OnDestroy()
	{
		if (transitioningCoroutine_ != null)
		{
			StopCoroutine(transitioningCoroutine_);
		}
		if (blinkCoroutine_ != null)
		{
			StopCoroutine(blinkCoroutine_);
		}
		transitioningCoroutine_ = null;
		blinkCoroutine_ = null;
		altitudeGauge_ = null;
		aircraft_ = null;
	}
}
