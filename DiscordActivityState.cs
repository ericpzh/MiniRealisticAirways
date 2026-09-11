namespace MiniRealisticAirways;

// Unity-independent state guard for Discord activity requests. A request is
// valid only while its connection generation, request id, and activity
// revision all match the current state.
internal sealed class DiscordActivityState
{
	internal readonly struct RequestSnapshot
	{
		internal readonly int ConnectionGeneration;
		internal readonly int RequestId;
		internal readonly int Revision;

		internal RequestSnapshot(int connectionGeneration, int requestId, int revision)
		{
			ConnectionGeneration = connectionGeneration;
			RequestId = requestId;
			Revision = revision;
		}
	}

	private int revision_;
	private int nextRequestId_;
	private int connectionGeneration_;
	private int inFlightRequestId_ = -1;
	private int inFlightRevision_;
	private int inFlightGeneration_;

	internal bool ActivityDirty { get; private set; }

	internal bool ActivityInFlight { get; private set; }

	internal float NextRetryTime { get; private set; }

	internal float DeadlineTime { get; private set; }

	internal int Revision => revision_;

	internal int ConnectionGeneration => connectionGeneration_;

	internal void MarkActivityChanged()
	{
		revision_++;
		ActivityDirty = true;
	}

	internal void ConnectionEstablished()
	{
		connectionGeneration_++;
		ActivityInFlight = false;
		inFlightRequestId_ = -1;
		DeadlineTime = 0f;
		NextRetryTime = 0f;
		ActivityDirty = true;
	}

	internal bool CanSend(float now)
	{
		return ActivityDirty && !ActivityInFlight && now >= NextRetryTime;
	}

	internal RequestSnapshot BeginRequest(float now, float timeout)
	{
		RequestSnapshot snapshot = new RequestSnapshot(connectionGeneration_, ++nextRequestId_, revision_);
		ActivityInFlight = true;
		inFlightRequestId_ = snapshot.RequestId;
		inFlightRevision_ = snapshot.Revision;
		inFlightGeneration_ = snapshot.ConnectionGeneration;
		DeadlineTime = now + timeout;
		NextRetryTime = DeadlineTime;
		return snapshot;
	}

	internal bool TryComplete(RequestSnapshot snapshot, bool success, float now, float retryDelay)
	{
		if (!IsCurrent(snapshot))
		{
			return false;
		}
		ActivityInFlight = false;
		inFlightRequestId_ = -1;
		DeadlineTime = 0f;
		NextRetryTime = now + retryDelay;
		if (success && snapshot.Revision == revision_)
		{
			ActivityDirty = false;
		}
		else
		{
			ActivityDirty = true;
		}
		return true;
	}

	internal bool AbortRequest(RequestSnapshot snapshot, float now, float retryDelay)
	{
		return TryComplete(snapshot, success: false, now, retryDelay);
	}

	internal bool TryExpire(float now)
	{
		if (!ActivityInFlight || now < DeadlineTime)
		{
			return false;
		}
		ActivityInFlight = false;
		inFlightRequestId_ = -1;
		DeadlineTime = 0f;
		NextRetryTime = now;
		ActivityDirty = true;
		return true;
	}

	internal void InvalidateConnection(float now, float retryDelay)
	{
		connectionGeneration_++;
		ActivityInFlight = false;
		inFlightRequestId_ = -1;
		DeadlineTime = 0f;
		NextRetryTime = now + retryDelay;
		ActivityDirty = true;
	}

	private bool IsCurrent(RequestSnapshot snapshot)
	{
		return ActivityInFlight
			&& snapshot.ConnectionGeneration == connectionGeneration_
			&& snapshot.RequestId == inFlightRequestId_
			&& snapshot.Revision == inFlightRevision_
			&& snapshot.ConnectionGeneration == inFlightGeneration_;
	}
}
