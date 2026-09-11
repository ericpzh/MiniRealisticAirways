using UnityEngine;

namespace MiniRealisticAirways;

// 跑道关闭事件只关闭跑道本身：禁用标记亮起，期间禁止新的起飞和进近。
// 触发时尚未触地的进近飞机复飞；已触地和正在起飞的飞机照常完成流程；
// 事件之后才试图建立进近的飞机按原逻辑直接拒绝（PatchTrySetupLanding）。
// 设计原则：要么不干涉，要么复飞——绝不压住任何飞机的速度或协程。
public class RunwayClose : Event
{
	public Runway runway_;

	private static RunwayClose activeInstance_;

	private ClosedRunwayState closedRunwayState_;

	internal static bool IsRunwayClosed(Runway runway)
	{
		if (runway == null)
		{
			return false;
		}
		if (EventManager.closedRunway_ == runway)
		{
			return true;
		}
		return activeInstance_ != null && activeInstance_.runway_ == runway;
	}

	public override bool Trigger()
	{
		if (activeInstance_ != null || runway_ != null || EventManager.closedRunway_ != null)
		{
			return false;
		}

		Aircraft[] outboundAircraft = AircraftManager.GetOutboundAircraft();
		if (outboundAircraft == null)
		{
			return false;
		}

		// 沿用原有候选搜索：事件绑定在一条正在起飞滑跑的跑道上（事故叙事），
		// 但那架飞机不再受任何干涉，照常离地。
		Aircraft candidate = null;
		foreach (Aircraft aircraft in outboundAircraft)
		{
			if (aircraft != null && aircraft.state == Aircraft.State.TakingOff && aircraft.takeOffRunway != null)
			{
				candidate = aircraft;
				break;
			}
		}
		if (candidate == null || candidate.takeOffRunway == null)
		{
			return false;
		}

		return TriggerForRunway(candidate.takeOffRunway);
	}

	private bool TriggerForRunway(Runway runway)
	{
		if (runway == null || activeInstance_ != null || runway_ != null || EventManager.closedRunway_ != null)
			return false;
		runway_ = runway;

		// The event's business target is the runway selected for this takeoff.
		// Do not infer additional closures from aircraft/safety colliders: those
		// bounds also cover detector and protected-area helpers.
		closedRunwayState_ = new ClosedRunwayState(runway_);
		closedRunwayState_.SetClosed();

		EventManager.closedRunway_ = runway_;
		activeInstance_ = this;
		int goAroundCount = AbortAirborneApproaches(runway_);
		Plugin.Log?.LogInfo("RunwayClose Triggered; runway disabled markers enabled, " + goAroundCount + " airborne approach(es) go around; touched-down aircraft continue, new approaches rejected.");
		return true;
	}

	public override void Restore()
	{
		RestoreInternal();
		Plugin.Log?.LogWarning("RunwayClose Restored.");
	}

	private void RestoreInternal()
	{
		closedRunwayState_?.Restore();
		closedRunwayState_ = null;

		if (EventManager.closedRunway_ == runway_)
		{
			EventManager.closedRunway_ = null;
		}
		if (activeInstance_ == this)
		{
			activeInstance_ = null;
		}
		runway_ = null;
	}

	// Landing 尚未触地；TouchedDown 已进入缩小/滑跑，绝不停止其协程。
	private int AbortAirborneApproaches(Runway runway)
	{
		Aircraft[] inboundAircraft = AircraftManager.GetInboundAircraft();
		if (runway == null || inboundAircraft == null)
		{
			return 0;
		}

		int count = 0;
		foreach (Aircraft aircraft in inboundAircraft)
		{
			if (aircraft == null)
			{
				continue;
			}
			RunwayClosePhase phase = GetPhase(aircraft.state);
			if (RunwayClosePolicy.ShouldGoAround(phase, aircraft.LandingRunway == runway))
			{
				PatchLandCoroutine.GoAround(aircraft, stopLandingCoroutine: true);
				count++;
			}
		}
		return count;
	}

	private static RunwayClosePhase GetPhase(Aircraft.State state)
	{
		if (state == Aircraft.State.Landing) return RunwayClosePhase.Landing;
		if (state == Aircraft.State.TouchedDown) return RunwayClosePhase.TouchedDown;
		if (state == Aircraft.State.TakingOff) return RunwayClosePhase.TakingOff;
		return RunwayClosePhase.Other;
	}

	private void OnDestroy()
	{
		RestoreInternal();
	}

	private sealed class ClosedRunwayState
	{
		internal readonly Runway runway;
		private readonly bool originalDisableTakeoffStart_;
		private readonly bool originalDisableTakeoffEnd_;
		private readonly bool originalDisablePILSStart_;
		private readonly bool originalDisablePILSEnd_;

		internal ClosedRunwayState(Runway runway)
		{
			this.runway = runway;
			originalDisableTakeoffStart_ = runway.DisableTakeoffStart;
			originalDisableTakeoffEnd_ = runway.DisableTakeoffEnd;
			originalDisablePILSStart_ = runway.DisablePILSStart;
			originalDisablePILSEnd_ = runway.DisablePILSEnd;
		}

		internal void SetClosed()
		{
			if (runway == null)
			{
				return;
			}
			runway.DisableTakeoffStart = true;
			runway.DisableTakeoffEnd = true;
			runway.DisablePILSStart = true;
			runway.DisablePILSEnd = true;
			ApplyDisabledIndicators();
		}

		internal void Restore()
		{
			if (runway == null)
			{
				return;
			}
			runway.DisableTakeoffStart = originalDisableTakeoffStart_;
			runway.DisableTakeoffEnd = originalDisableTakeoffEnd_;
			runway.DisablePILSStart = originalDisablePILSStart_;
			runway.DisablePILSEnd = originalDisablePILSEnd_;
			ApplyDisabledIndicators();
		}

		private void ApplyDisabledIndicators()
		{
			if (runway.SPTOStartDisabled != null) runway.SPTOStartDisabled.enabled = runway.DisableTakeoffStart;
			if (runway.SPTOEndDisabled != null) runway.SPTOEndDisabled.enabled = runway.DisableTakeoffEnd;
			if (runway.SPLandStartDisabled != null) runway.SPLandStartDisabled.enabled = runway.DisablePILSStart;
			if (runway.SPLandEndDisabled != null) runway.SPLandEndDisabled.enabled = runway.DisablePILSEnd;
		}
	}
}
