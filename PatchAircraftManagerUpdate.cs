using System;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(AircraftManager), "Update", new Type[] { })]
internal class PatchAircraftManagerUpdate
{
	private static void Postfix(Camera ____camera)
	{
		if (____camera == null || Time.timeScale == 0f)
		{
			return;
		}
		// 航点命名会话激活期间，W/A/S/D 与滚轮属于文本输入，不下发飞机指令。
		if (WaypointNameInput.AnyActive || WaypointNameInput.HoveredTarget != null)
		{
			return;
		}
		bool slowDown = Speed.InputSlowDown();
		bool speedUp = Speed.InputSpeedUp();
		bool climb = Altitude.InputClimb();
		bool descend = Altitude.InputDescend();
		if (!slowDown && !speedUp && !climb && !descend)
		{
			return;
		}
		Vector3 vector = ____camera.ScreenToWorldPoint(Input.mousePosition);
		float num = float.PositiveInfinity;
		Aircraft aircraft = null;
		const float SelectionRadiusPerOrthographicSize = 2f / 13f;
		float num2 = SelectionRadiusPerOrthographicSize * ____camera.orthographicSize;
		float maxDistanceSquared = num2 * num2;
		Aircraft[] aircraft2 = AircraftManager.GetAircraft();
		if (aircraft2 == null)
		{
			return;
		}
		Aircraft[] array = aircraft2;
		foreach (Aircraft aircraft3 in array)
		{
			if (aircraft3 == null)
			{
				continue;
			}
			Vector2 delta = (Vector2)aircraft3.gameObject.transform.position - (Vector2)vector;
			float distanceSquared = delta.sqrMagnitude;
			if (distanceSquared < num && distanceSquared <= maxDistanceSquared)
			{
				num = distanceSquared;
				aircraft = aircraft3;
			}
		}
		if (aircraft != null && AircraftState.GetAircraftStates(aircraft, out var aircraftAltitude, out var aircraftSpeed, out var _))
		{
			if (slowDown)
			{
				aircraftSpeed.AircraftSlowDown();
			}
			else if (speedUp)
			{
				aircraftSpeed.AircraftSpeedUp();
			}
			else if (climb)
			{
				aircraftAltitude.AircraftClimb();
			}
			else if (descend)
			{
				aircraftAltitude.AircraftDescend();
			}
		}
	}
}
