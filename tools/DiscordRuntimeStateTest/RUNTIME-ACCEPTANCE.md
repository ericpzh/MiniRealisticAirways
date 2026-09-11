# Runway and Discord follow-up acceptance

Automated state tests do not constitute a Unity or Discord runtime pass.

## Runway closure

Policy: airborne approaches (Landing) to the closed runway go around immediately;
aircraft already in TouchedDown continue scaling/rollout. Taking-off aircraft
continue. New landing/takeoff commands remain rejected until restoration.
No aircraft speed is zeroed and no aircraft is deleted by the closure.

In the actual game, trigger the event with an outbound aircraft taking off and
an inbound aircraft targeting the same runway. Repeat at far approach and just
before touchdown: the inbound aircraft must go around and keep moving, with one
go-around count and no touchdown count. Repeat after touchdown/scaling starts
and during rollout: normal landing must finish with one touchdown count.
Test pause/resume and accelerated game speeds at the touchdown boundary.

Verify an approach to another runway is unaffected. Verify new approaches and
takeoffs to the closed runway are rejected, and commands work after restoration.
Restore must preserve the runway's original disabled flags.

## Discord

1. Start without Discord: retry connection after 600 unscaled game seconds;
   there must be no five-second connection retry loop. Only exact SDK results
   NotRunning/NotInstalled are informational. InternalError remains a warning.
2. Open Discord later: at the next connection attempt, initialization and then
   successful activity synchronization should be separately reported. Verify
   actual activity in Discord; initialization alone is not acceptance.
3. While connected, update managed-aircraft count: retain five-second activity
   cooldown. After a successful send, do not repeatedly log synchronization.
4. Close/reopen Discord: callback exceptions dispose and retry after 600 seconds.
   Three consecutive activity failures/timeouts also dispose and schedule that
   delay. Successful activity resets the failure count.
5. Deliver a stale callback after timeout or reconnect: it must not clear the
   latest activity or alter connection-health counters.
6. Quit during a pending update: no further sends; SDK resources are disposed.

The 600-second timer uses Time.unscaledTime: it requires the game's update loop
to run and is not a wall-clock recovery guarantee while suspended.
