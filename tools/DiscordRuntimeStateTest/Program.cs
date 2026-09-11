using MiniRealisticAirways;

static class Program
{
    private static int Main()
    {
        NewRevisionCannotBeClearedByOldSuccess();
        ActivityUpdatesRespectCooldown();
        TimeoutInvalidatesLateCallback();
        ReconnectInvalidatesOldGeneration();
        RunwayClosureDivertsOnlyAirborneApproaches();
        ConnectionFailuresAreBounded();
        UnavailableClassificationIsConservative();
        Console.WriteLine("DiscordActivityState: 4/4 state cases passed.");
        Console.WriteLine("RunwayClosePolicy: touchdown-boundary cases passed.");
        Console.WriteLine("DiscordConnectionHealth: escalation, recovery and classification cases passed.");
        return 0;
    }

    private static void NewRevisionCannotBeClearedByOldSuccess()
    {
        DiscordActivityState state = ConnectedState();
        DiscordActivityState.RequestSnapshot first = state.BeginRequest(0f, 5f);
        state.MarkActivityChanged();
        Assert(state.TryComplete(first, success: true, now: 1f, retryDelay: 5f), "current callback should be accepted");
        Assert(state.ActivityDirty, "newer revision must remain dirty after old success");
    }

    private static void ActivityUpdatesRespectCooldown()
    {
        DiscordActivityState state = ConnectedState();
        DiscordActivityState.RequestSnapshot request = state.BeginRequest(0f, 5f);
        Assert(state.TryComplete(request, success: true, now: 1f, retryDelay: 5f), "success should be accepted");
        state.MarkActivityChanged();
        Assert(!state.CanSend(2f), "new activity must not bypass the retry cooldown");
        Assert(state.CanSend(6f), "new activity should become sendable after cooldown");
    }

    private static void TimeoutInvalidatesLateCallback()
    {
        DiscordActivityState state = ConnectedState();
        DiscordActivityState.RequestSnapshot request = state.BeginRequest(10f, 5f);
        Assert(state.TryExpire(15f), "expired request should be released");
        Assert(!state.TryComplete(request, success: true, now: 16f, retryDelay: 5f), "late timeout callback must be ignored");
        Assert(state.ActivityDirty && state.CanSend(15f), "timeout should leave the latest activity retryable");
    }

    private static void ReconnectInvalidatesOldGeneration()
    {
        DiscordActivityState state = ConnectedState();
        DiscordActivityState.RequestSnapshot oldRequest = state.BeginRequest(0f, 5f);
        state.InvalidateConnection(2f, 5f);
        Assert(!state.TryComplete(oldRequest, success: true, now: 3f, retryDelay: 5f), "disposed connection callback must be ignored");
        state.ConnectionEstablished();
        DiscordActivityState.RequestSnapshot newRequest = state.BeginRequest(8f, 5f);
        Assert(state.TryComplete(newRequest, success: true, now: 9f, retryDelay: 5f), "new connection callback should be accepted");
    }

    private static void RunwayClosureDivertsOnlyAirborneApproaches()
    {
        foreach (RunwayClosePhase phase in Enum.GetValues<RunwayClosePhase>())
        {
            Assert(RunwayClosePolicy.ShouldGoAround(phase, true) == (phase == RunwayClosePhase.Landing),
                "only airborne Landing on the closed runway must divert: " + phase);
            Assert(!RunwayClosePolicy.ShouldGoAround(phase, false), "another runway is unaffected: " + phase);
        }
    }

    private static void ConnectionFailuresAreBounded()
    {
        var health = new DiscordConnectionHealth();
        health.Connected();
        health.Failed();
        health.Failed();
        Assert(!health.ReconnectRequired, "allow two short retries");
        health.Failed();
        Assert(health.ReconnectRequired, "third consecutive failure requires reconnect");
        health.Disconnected();
        Assert(!health.ReconnectRequired, "disposal resets escalation");
        health.Connected();
        health.Failed();
        Assert(health.Synchronized(), "first successful sync emits recovery confirmation");
        Assert(health.Failures == 0 && !health.Synchronized(), "success resets failures and confirmation is once per connection");
    }

    private static void UnavailableClassificationIsConservative()
    {
        Assert(DiscordConnectionHealth.IsExpectedUnavailable("NotRunning"), "not running is informational");
        Assert(DiscordConnectionHealth.IsExpectedUnavailable("NotInstalled"), "not installed is informational");
        Assert(!DiscordConnectionHealth.IsExpectedUnavailable("InternalError"), "internal error is not proof Discord is closed");
        Assert(!DiscordConnectionHealth.IsExpectedUnavailable(null), "unknown exceptions remain warnings");
    }

    private static DiscordActivityState ConnectedState()
    {
        DiscordActivityState state = new DiscordActivityState();
        state.MarkActivityChanged();
        state.ConnectionEstablished();
        Assert(state.CanSend(0f), "connected state should send initial activity");
        return state;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
