namespace MiniRealisticAirways;

// 原版 OnPointUp、SetFlyHeading 两个重载与 SetVectorTo 两个重载会重设速度。
// Postfix/Finalizer 均恢复原目标；保留停车结果及新建立进近时自动选择的速度。
internal static class TargetSpeedProtection
{
	internal static float Capture(Aircraft aircraft) => aircraft == null ? 0f : aircraft.targetSpeed;
	internal static void Restore(Aircraft aircraft, float speed)
	{
		// OnPointUp 内部可能调用 TrySetupLanding，将轻型机由 Normal 减至 Slow。
		// 此时进近已经接管速度，不能被外层指令收尾恢复成原来的 Normal。
		if (aircraft != null && aircraft.state != Aircraft.State.Landing && aircraft.targetSpeed > 0f)
			aircraft.targetSpeed = speed;
	}
}
