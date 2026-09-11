using System;
using System.Collections;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch]
public class PatchLandCoroutine
{
	private static readonly System.Reflection.FieldInfo LandingCoroutineField = AccessTools.Field(typeof(Aircraft), "landCoroutine");

	internal static void GoAround(Aircraft aircraft, bool stopLandingCoroutine = false)
	{
		if (aircraft == null || aircraft.state != Aircraft.State.Landing) return;
		if (stopLandingCoroutine)
		{
			StopLandingCoroutine(aircraft);
		}
		AircraftState.GetAircraftStates(aircraft, out var _, out var _, out var type);
		if (type != null) type.windChecked_ = false;
		aircraft.aircraftVoiceAndSubtitles?.PlayFailedToLand();
		aircraft.state = Aircraft.State.GoingAround;
		if (aircraft.GoAroundWarner != null)
		{
			aircraft.GoAroundWarner.SetActive(true);
			Sequence sequence = DOTween.Sequence();
			sequence.Append(aircraft.GoAroundWarner.transform.DOScale(2f, 1f));
			sequence.Append(aircraft.GoAroundWarner.transform.DOScale(2f, 1f).OnComplete(delegate
			{
				if (aircraft != null && aircraft.GoAroundWarner != null) aircraft.GoAroundWarner.SetActive(false);
			}));
			sequence.Play();
		}
		// 与原版复飞一致，重新接入跑道离场航点并计入统计。
		if (aircraft.LandingRunway != null)
		{
			PlaceableWaypoint waypoint = aircraft.LandingRunway.GetTakeOffWaypointByLandingStartPoint(aircraft.landingStartPoint);
			if (waypoint != null) aircraft.SetVectorTo(waypoint);
		}
		aircraft.statistics?.AddGoAround();
		// SetVectorTo 会写入 Normal，最后恢复轻型机可用的复飞速度。
		aircraft.targetSpeed = Speed.ToGameSpeed(type != null && type.weight_ == Weight.Light ? SpeedLevel.Slow : SpeedLevel.Normal);
	}

	private static void StopLandingCoroutine(Aircraft aircraft)
	{
		if (aircraft == null || LandingCoroutineField == null)
		{
			return;
		}
		try
		{
			Coroutine coroutine = LandingCoroutineField.GetValue(aircraft) as Coroutine;
			if (coroutine != null)
			{
				aircraft.StopCoroutine(coroutine);
			}
		}
		finally
		{
			LandingCoroutineField.SetValue(aircraft, null);
		}
	}

	internal static bool CheckRunway(Aircraft aircraft)
	{
		// 已触地的滑跑不再受进近禁令影响。
		if (aircraft == null) return false;
		if (aircraft.state != Aircraft.State.Landing) return true;
		if (RunwayClose.IsRunwayClosed(aircraft.LandingRunway))
		{
			Plugin.Log?.LogInfo("Going around due to runway closed event.");
			GoAround(aircraft);
			return false;
		}
		if (AircraftState.GetAircraftStates(aircraft, out var _, out var _, out var type)
			&& Plugin.windsock_ != null && !type.windChecked_)
		{
			type.windChecked_ = true;
			if (!Plugin.windsock_.CanLand(aircraft.heading, type.weight_))
			{
				Plugin.Log?.LogInfo("Going around due to wind with heading: " + aircraft.heading);
				GoAround(aircraft);
				return false;
			}
		}
		return true;
	}

	internal static bool AllowTouchdown(Aircraft aircraft)
	{
		if (aircraft == null || aircraft.state != Aircraft.State.Landing) return false;
		if (AircraftState.GetAircraftStates(aircraft, out var altitude, out var speed, out var type)
			&& (!altitude.CanTouchDown() || !speed.CanTouchDown(type.weight_)))
		{
			GoAround(aircraft);
			return false;
		}
		// 在原版移动完成、触地副作用发生前兜底，包含一步跨过 0.2 / 0.1 的情况。
		if (!CheckRunway(aircraft)) return false;
		altitude?.CompleteTouchdown();
		return true;
	}

	private static bool CheckApproach(Aircraft aircraft)
	{
		if (aircraft == null) return false;
		if (aircraft.state != Aircraft.State.Landing) return true;
		// 关闭后任何尚未触地的进近立即复飞，不必等到跑道入口。
		if (RunwayClose.IsRunwayClosed(aircraft.LandingRunway)) return CheckRunway(aircraft);
		if (AircraftState.GetAircraftStates(aircraft, out var altitude, out var speed, out var type)
			&& (!altitude.CanLand() || !speed.CanLand(type.weight_)))
		{
			GoAround(aircraft);
			return false;
		}
		return ((Vector2)aircraft.transform.position - aircraft.landingStartPoint).sqrMagnitude >= 0.04f || CheckRunway(aircraft);
	}

	[HarmonyPatch(typeof(Aircraft), "LandCoroutine")]
	[HarmonyPrefix]
	public static bool LandCoroutinePrefix(Aircraft __instance, out float __state)
	{
		__state = __instance == null ? float.NaN : __instance.targetSpeed;
		return true;
	}

	[HarmonyPatch(typeof(Aircraft), "LandCoroutine")]
	[HarmonyPostfix]
	public static IEnumerator LandCoroutinePostfix(IEnumerator result, Aircraft __instance, float __state)
	{
		if (result == null) yield break;
		try
		{
			while (CheckApproach(__instance))
			{
				bool moved;
				// 迭代器调用不会执行协程体；每次 MoveNext 才需要转弯系数，绝不跨 yield。
				using (TurnSpeedScope.Enter(__instance)) moved = result.MoveNext();
				if (!moved) yield break;
				if (__instance != null && __instance.state == Aircraft.State.Landing)
				{
					if (__instance.targetSpeed > 0f && !float.IsNaN(__state)) __instance.targetSpeed = __state;
					if (!CheckApproach(__instance)) yield break;
				}
				yield return result.Current;
			}
		}
		finally
		{
			try { (result as IDisposable)?.Dispose(); }
			finally
			{
				// 提前复飞会跳过原迭代器尾部的清理。仅在已离开进近/滑跑时清理，
				// 不触碰之后接替本协程的新进近或仍在继续的触地滑跑。
				if (__instance != null && __instance.state != Aircraft.State.Landing && __instance.state != Aircraft.State.TouchedDown)
					LandingCoroutineField?.SetValue(__instance, null);
			}
		}
	}
}
