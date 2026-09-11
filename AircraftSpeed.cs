using System;
using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways;

public class AircraftSpeed : Speed
{
	public Aircraft aircraft_;

	public bool speedDisabled_ = false;

	public IEnumerator enableSpeedGaugeCoroutine_;

	private AircraftSpeedGauge speedGauge_;

	private bool transitioning_ = false;

	private Camera mainCamera_;

	private bool flightStateInitialized_;
	private static readonly WaitForFixedUpdate FixedStep = new WaitForFixedUpdate();

	internal void RestoreFlightState(float speed, float target)
	{
		flightStateInitialized_ = true;
		aircraft_.speed = speed;
		aircraft_.targetSpeed = target;
	}

	public bool CanLand(Weight weight)
	{
		return aircraft_ != null && LandingPolicy.IsSpeedAllowed(aircraft_.targetSpeed, weight);
	}

	public bool CanTouchDown(Weight weight)
	{
		return aircraft_ != null && LandingPolicy.IsSpeedAllowed(aircraft_.speed, weight);
	}

	public SpeedLevel MaxSpeed()
	{
		if (aircraft_ == null)
		{
			return SpeedLevel.Normal;
		}
		if (AircraftState.GetAircraftStates(aircraft_, out var _, out var _, out var aircraftType) && aircraftType.weight_ == Weight.Light)
		{
			return SpeedLevel.Normal;
		}
		return SpeedLevel.Fast;
	}

	public void AircraftSpeedUp()
	{
		if (aircraft_ != null && aircraft_.state != Aircraft.State.TakingOff && !speedDisabled_ && !(aircraft_.targetSpeed >= Speed.ToGameSpeed(MaxSpeed())))
		{
			// 非标准 targetSpeed（如落地过渡值）可能让 +1 越过枚举边界，钳制在机型上限内。
			SpeedLevel nextLevel = Speed.ToModSpeed(aircraft_.targetSpeed) + 1;
			if (nextLevel > MaxSpeed())
			{
				nextLevel = MaxSpeed();
			}
			float targetSpeed = Speed.ToGameSpeed(nextLevel);
			aircraft_.targetSpeed = targetSpeed;
			if (!transitioning_)
			{
				StartCoroutine(SpeedTransitionCoroutine(targetSpeed));
			}
		}
	}

	public void AircraftSlowDown()
	{
		if (aircraft_ != null && aircraft_.state != Aircraft.State.TakingOff && !speedDisabled_ && !(aircraft_.targetSpeed <= Speed.ToGameSpeed(SpeedLevel.Slow)))
		{
			// 下限钳制在 Slow，避免低速边界值（如 21）算出 Stopped 使空中目标速度归零。
			SpeedLevel nextLevel = Speed.ToModSpeed(aircraft_.targetSpeed) - 1;
			if (nextLevel < SpeedLevel.Slow)
			{
				nextLevel = SpeedLevel.Slow;
			}
			float targetSpeed = Speed.ToGameSpeed(nextLevel);
			aircraft_.targetSpeed = targetSpeed;
			if (!transitioning_)
			{
				StartCoroutine(SpeedTransitionCoroutine(targetSpeed));
			}
		}
	}

	// A waypoint requests an exact level, even when the old value rounds to that level.
	internal bool SetTargetSpeed(SpeedLevel level)
	{
		if (aircraft_ == null || aircraft_.state == Aircraft.State.TakingOff || speedDisabled_) return false;
		SpeedLevel maximum = MaxSpeed();
		if (level < SpeedLevel.Slow) level = SpeedLevel.Slow;
		if (level > maximum) level = maximum;
		float target = Speed.ToGameSpeed(level);
		if (aircraft_.targetSpeed == target) return true;
		aircraft_.targetSpeed = target;
		if (!transitioning_) StartCoroutine(SpeedTransitionCoroutine(target));
		return true;
	}

	public override string ToString()
	{
		if (aircraft_ == null)
		{
			return Speed.ToString(SpeedLevel.Stopped);
		}
		if (InTransition(aircraft_.targetSpeed) && Animation.Blink())
		{
			return " ";
		}
		return Speed.ToString(Speed.ToModSpeed(aircraft_.speed));
	}

	public SpeedLevel GetSpeed()
	{
		if (aircraft_ == null)
		{
			return SpeedLevel.Slow;
		}
		return Speed.ToModSpeed(aircraft_.speed);
	}

	private bool InTransition(float targetSpeed)
	{
		if (aircraft_ == null)
		{
			return false;
		}
		float num = Math.Abs(aircraft_.speed - targetSpeed);
		return num > 2f;
	}

	private IEnumerator SpeedTransitionCoroutine(float targetSpeed)
	{
		transitioning_ = true;
		IEnumerator blink = null;
		try
		{
			while (this != null && aircraft_ != null && speedGauge_ != null)
			{
				if (!speedGauge_.TryInitialize())
				{
					if (speedGauge_.InitializationFailed()) yield break;
					yield return FixedStep;
					continue;
				}
				targetSpeed = aircraft_.targetSpeed;
				if (!InTransition(targetSpeed))
				{
					speedGauge_.UpdateGauge(GetSpeed());
					yield break;
				}
				blink = speedGauge_.GetTransitioningCoroutine(GetSpeed(), Speed.ToModSpeed(targetSpeed));
				if (blink != null) StartCoroutine(blink);
				// 目标被取消时立即结束旧闪烁，下一轮采用新目标。
				while (aircraft_ != null && speedGauge_ != null && aircraft_.targetSpeed == targetSpeed && InTransition(targetSpeed))
					yield return FixedStep;
				if (blink != null) StopCoroutine(blink);
				blink = null;
			}
		}
		finally
		{
			if (this != null && blink != null) StopCoroutine(blink);
			transitioning_ = false;
		}
	}

	public IEnumerator EnableSpeedGauge(SpeedLevel speed = SpeedLevel.Normal)
	{
		// 可能早于 Start 被调用：等待组件就绪，不丢弃唯一一次显示请求。
		AircraftState aircraftState;
		while (this != null && aircraft_ != null && (speedGauge_ == null || !speedGauge_.Ready() || !AircraftState.GetAircraftState(aircraft_, out aircraftState) || !aircraftState.IsAirborne()))
		{
			if (speedGauge_ != null)
			{
				speedGauge_.TryInitialize();
				if (speedGauge_.InitializationFailed())
				{
					yield break;
				}
			}
			yield return FixedStep;
		}
		if (this != null && aircraft_ != null && speedGauge_ != null)
		{
			speedGauge_.UpdateGauge(speed);
		}
	}

	private void Start()
	{
		if (!(aircraft_ == null))
		{
			mainCamera_ = Camera.main;
			aircraft_.TakeOffSpeedFactor = Speed.ToGameSpeed(SpeedLevel.Normal);
			if (!flightStateInitialized_ && (aircraft_.direction == Aircraft.Direction.Outbound || aircraft_.direction == Aircraft.Direction.Inbound))
			{
				aircraft_.targetSpeed = Speed.ToGameSpeed(SpeedLevel.Normal);
			}
			flightStateInitialized_ = true;
			speedGauge_ = aircraft_.GetComponent<AircraftSpeedGauge>();
			if (speedGauge_ == null)
			{
				speedGauge_ = aircraft_.gameObject.AddComponent<AircraftSpeedGauge>();
			}
			speedGauge_.aircraft_ = aircraft_;
		}
	}

	private void Update()
	{
		if (aircraft_ == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		TouchedDownArrivalProcess();
	}

	private void TouchedDownArrivalProcess()
	{
		if (mainCamera_ == null)
		{
			mainCamera_ = Camera.main;
		}
		bool flag = AircraftViewport.IsVisible(aircraft_, mainCamera_);
		bool outboundAirborne = aircraft_.direction == Aircraft.Direction.Outbound
			&& AircraftState.GetAircraftState(aircraft_, out var state) && state.IsAirborne();
		// 由速度组件独立发起显示；不依赖高度 Update 与速度 Start 的执行顺序。
		if (((aircraft_.direction == Aircraft.Direction.Inbound && flag) || outboundAirborne) && enableSpeedGaugeCoroutine_ == null)
		{
			enableSpeedGaugeCoroutine_ = EnableSpeedGauge(GetSpeed());
			StartCoroutine(enableSpeedGaugeCoroutine_);
		}
		if (AircraftState.DisableStateOnTouchedDown(aircraft_) && speedGauge_ != null)
		{
			speedGauge_.DisableSpriteRenderer();
		}
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
		speedGauge_ = null;
		aircraft_ = null;
		transitioning_ = false;
	}
}
