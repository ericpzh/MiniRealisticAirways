using System;

namespace MiniRealisticAirways;

// Unity 同线程嵌套调用共享未修正基准；值类型避免每次航向更新产生堆分配。
internal struct TurnSpeedScope : IDisposable
{
	[ThreadStatic] private static int depth_;
	[ThreadStatic] private static float baseline_;
	private float previous_;
	private bool active_;

	internal static TurnSpeedScope Enter(Aircraft aircraft)
	{
		float factor = AircraftState.GetAircraftStates(aircraft, out var _, out var _, out var type)
			&& type.weight_ == Weight.Light ? AircraftType.LIGHT_TURN_FACTOR : 1f;
		var scope = new TurnSpeedScope { previous_ = Aircraft.TurnSpeed, active_ = true };
		if (depth_ == 0) baseline_ = Aircraft.TurnSpeed;
		depth_++;
		Aircraft.TurnSpeed = baseline_ * factor;
		return scope;
	}

	public void Dispose()
	{
		// Harmony Postfix/Finalizer 必须以 ref 共享 __state，避免双重恢复。
		if (!active_) return;
		active_ = false;
		Aircraft.TurnSpeed = previous_;
		depth_--;
	}
}
