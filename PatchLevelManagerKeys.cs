using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

// 原版 LevelManager.Update 把 Space/Enter/小键盘 Enter/Esc/1/2/x 等键绑定为暂停与
// 时间档切换。航点命名键入这些字符时，若不拦截，确认键会在结束命名的同一帧触发
// 原版行为（Space 暂停、Enter 归位常速、Esc 打开菜单）。四个前缀仅在命名会话
// 消费了对应按键的帧让行；暂停/时间档按钮的鼠标点击不含这些键盘按键，照常放行。
[HarmonyPatch(typeof(LevelManager), "OnPauseTimeBtnPressed", new System.Type[] { })]
internal static class PatchLevelManagerPauseKey
{
	private static bool Prefix()
	{
		// Space 是命名的开启/结束键：悬停航点时开启，会话中结束，两种情况都拦截暂停。
		return !WaypointNameInput.ConsumePauseKey();
	}
}

[HarmonyPatch(typeof(LevelManager), "OnNormalTimeBtnPressed", new System.Type[] { })]
internal static class PatchLevelManagerNormalTimeKey
{
	private static bool Prefix()
	{
		return !WaypointNameInput.AnyActive;
	}
}

[HarmonyPatch(typeof(LevelManager), "OnFastTimeBtnPressed", new System.Type[] { })]
internal static class PatchLevelManagerFastTimeKey
{
	private static bool Prefix()
	{
		return !WaypointNameInput.AnyActive;
	}
}

[HarmonyPatch(typeof(LevelManager), "OnEscPressed", new System.Type[] { })]
internal static class PatchLevelManagerEscKey
{
	private static bool Prefix()
	{
		return !WaypointNameInput.AnyActive;
	}
}
