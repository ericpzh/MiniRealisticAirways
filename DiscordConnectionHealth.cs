#nullable enable
namespace MiniRealisticAirways;

// Independent from Unity/SDK so retry escalation can be regression-tested.
internal sealed class DiscordConnectionHealth
{
	internal const int FailureLimit = 3;
	internal int Failures { get; private set; }
	internal bool ReconnectRequired => Failures >= FailureLimit;
	internal bool AwaitingFirstSync { get; private set; }

	internal void Connected()
	{
		Failures = 0;
		AwaitingFirstSync = true;
	}

	internal void Failed() { if (Failures < FailureLimit) Failures++; }

	internal bool Synchronized()
	{
		bool first = AwaitingFirstSync;
		Failures = 0;
		AwaitingFirstSync = false;
		return first;
	}

	internal void Disconnected()
	{
		Failures = 0;
		AwaitingFirstSync = false;
	}

	internal static bool IsExpectedUnavailable(string? result)
	{
		return result == "NotRunning" || result == "NotInstalled";
	}
}
