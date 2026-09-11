using System;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(GUIAutoHider), "Update", new Type[] { })]
internal class PatchGUIAutoHiderUpdate
{
	private static void Postfix(ref GUIAutoHider __instance)
	{
		if (__instance != null && __instance.TL != null && GameOverManager.Instance != null && Time.timeScale != 0f && !GameOverManager.Instance.GameOverFlag && __instance.TL.alpha < 1f)
		{
			// 原版渐隐会逐帧重试，此处只恢复可见性，不逐帧写日志。
			__instance.TL.alpha = 1f;
		}
	}
}
