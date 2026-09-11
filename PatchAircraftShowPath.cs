using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

// Aircraft.ShowPath's stock completion delegate dereferences the aircraft's
// LineRenderer again after the 0.5 second fade-in. If the aircraft is removed
// in that window, DOTween reports a Renderer.GetMaterial exception. Keep the
// stock timing and colors, but guard the renderer/material at both stages.
[HarmonyPatch]
internal static class PatchAircraftShowPath
{
	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(Aircraft), "ShowPath", new[] { typeof(List<Vector3>), typeof(bool) })
			?? throw new MissingMethodException("Aircraft.ShowPath(List<Vector3>, bool) could not be located.");
	}

	[HarmonyPrefix]
	private static bool Prefix(Aircraft __instance, List<Vector3> path, bool success)
	{
		if (__instance == null || path == null || __instance.LandingGuideLine == null)
		{
			return false;
		}

		LineRenderer line = __instance.LandingGuideLine;
		Material material;
		try
		{
			line.positionCount = path.Count;
			line.SetPositions(path.ToArray());
			material = line.material;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Landing path renderer was unavailable; skipping path fade: " + exception.GetBaseException().Message);
			return false;
		}

		if (material == null)
		{
			return false;
		}

		material.DOKill();
		material.color = success ? new Color(1f, 1f, 1f, 0f) : new Color(1f, 0f, 0f, 0f);
		material.DOFade(1f, 0.5f)
			.SetUpdate(isIndependentUpdate: true)
			.OnComplete(delegate
			{
				if (__instance == null || line == null || material == null)
				{
					return;
				}
				try
				{
					material.DOFade(0f, 1.5f).SetUpdate(isIndependentUpdate: false);
				}
				catch (Exception exception)
				{
					Plugin.Log?.LogDebug("Landing path fade-out was skipped after renderer teardown: " + exception.GetBaseException().Message);
				}
			});
		return false;
	}

	internal static void KillPathTween(Aircraft aircraft)
	{
		if (aircraft == null || aircraft.LandingGuideLine == null)
		{
			return;
		}
		try
		{
			Material material = aircraft.LandingGuideLine.material;
			if (material != null)
			{
				material.DOKill();
			}
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Landing path tween cleanup skipped after renderer teardown: " + exception.GetBaseException().Message);
		}
	}
}

[HarmonyPatch(typeof(Aircraft), "OnDestroy")]
internal static class PatchAircraftPathCleanup
{
	[HarmonyPrefix]
	private static void Prefix(Aircraft __instance)
	{
		PatchAircraftShowPath.KillPathTween(__instance);
	}
}
