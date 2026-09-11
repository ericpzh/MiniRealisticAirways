using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(Aircraft), "UpdateHeading", new Type[] { })]
internal class PatchUpdateHeading
{
	private static bool Prefix(Aircraft __instance, PlaceableWaypoint ____HARWCurWP, out TurnSpeedScope __state)
	{
		__state = TurnSpeedScope.Enter(__instance);
		if (!AircraftState.GetAircraftStates(__instance, out var aircraftAltitude, out var aircraftSpeed, out var aircraftType))
		{
			return true;
		}
		if (__instance.state != Aircraft.State.HeadingAfterReachingWaypoint)
		{
			return true;
		}
		if (____HARWCurWP == null || !(____HARWCurWP is WaypointAutoLanding))
		{
			return true;
		}
		Runway fieldValue = ____HARWCurWP.GetFieldValue<Runway>("_targetRunway");
		if (fieldValue == null)
		{
			return true;
		}
		int num = 0;
		while (aircraftAltitude != null && !aircraftAltitude.CanLand() && ++num < Plugin.MAX_WHILE_LOOP_ITER)
		{
			var previous = aircraftAltitude.targetAltitude_;
			aircraftAltitude.AircraftDescend();
			if (aircraftAltitude.targetAltitude_ == previous) break;
			if (num == Plugin.MAX_WHILE_LOOP_ITER - 1)
			{
				Plugin.Log.LogWarning("INF Loop in UpdateHeading's prefix aircraftAltitude change.");
			}
		}
		num = 0;
		while (aircraftSpeed != null && !aircraftSpeed.CanLand(aircraftType.weight_) && ++num < Plugin.MAX_WHILE_LOOP_ITER)
		{
			var previous = __instance.targetSpeed;
			aircraftSpeed.AircraftSlowDown();
			if (__instance.targetSpeed == previous) break;
			if (num == Plugin.MAX_WHILE_LOOP_ITER - 1)
			{
				Plugin.Log.LogWarning("INF Loop in UpdateHeading's prefix aircraftSpeed change.");
			}
		}
		return true;
	}

	private static void Postfix(Aircraft __instance, PlaceableWaypoint ____HARWCurWP, ref TurnSpeedScope __state)
	{
		__state.Dispose();
		if (__instance == null || __instance.state != Aircraft.State.HeadingAfterReachingWaypoint || ____HARWCurWP == null || !(____HARWCurWP is BaseWaypointAutoHeading) || !AircraftState.GetAircraftState(__instance, out var aircraftState) || aircraftState.commandingWaypoint_ == ____HARWCurWP)
		{
			return;
		}
		AircraftAltitude aircraftAltitude_ = aircraftState.aircraftAltitude_;
		WaypointAltitude component = ____HARWCurWP.GetComponent<WaypointAltitude>();
		if (aircraftAltitude_ != null && component != null)
		{
			int num = 0;
			while (aircraftAltitude_.targetAltitude_ < component.altitude_ && ++num < Plugin.MAX_WHILE_LOOP_ITER)
			{
				var previous = aircraftAltitude_.targetAltitude_;
				aircraftAltitude_.AircraftClimb();
				if (aircraftAltitude_.targetAltitude_ == previous) break;
				if (num == Plugin.MAX_WHILE_LOOP_ITER - 1)
				{
					Plugin.Log.LogWarning("INF Loop in UpdateHeading's postfix AircraftClimb().");
				}
			}
			num = 0;
			while (aircraftAltitude_.targetAltitude_ > component.altitude_ && ++num < Plugin.MAX_WHILE_LOOP_ITER)
			{
				var previous = aircraftAltitude_.targetAltitude_;
				aircraftAltitude_.AircraftDescend();
				if (aircraftAltitude_.targetAltitude_ == previous) break;
				if (num == Plugin.MAX_WHILE_LOOP_ITER - 1)
				{
					Plugin.Log.LogWarning("INF Loop in UpdateHeading's postfix AircraftDescend().");
				}
			}
		}
		AircraftSpeed aircraftSpeed_ = aircraftState.aircraftSpeed_;
		WaypointSpeed component2 = ____HARWCurWP.GetComponent<WaypointSpeed>();
		if (aircraftSpeed_ != null && component2 != null)
		{
			aircraftSpeed_.SetTargetSpeed(component2.speed_);
		}
		aircraftState.commandingWaypoint_ = ____HARWCurWP;
	}

	private static Exception Finalizer(ref TurnSpeedScope __state, Exception __exception)
	{
		__state.Dispose();
		return __exception;
	}
}
