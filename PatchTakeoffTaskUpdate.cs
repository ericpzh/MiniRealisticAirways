using System;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(TakeoffTask), "Update", new Type[] { })]
internal class PatchTakeoffTaskUpdate
{
	private static void Postfix(ref TakeoffTask __instance, ref Image ___AP)
	{
		if (Time.timeScale != 0f && __instance != null && ___AP != null && !__instance.inCommand && !DOTween.IsTweening(___AP.transform))
		{
			BaseAircraftType component = __instance.gameObject.GetComponent<BaseAircraftType>();
			if (!(component == null))
			{
				float scaleFactor = component.GetScaleFactor();
				Vector3 localScale = ___AP.transform.localScale;
				if (Mathf.Abs(localScale.x - scaleFactor) > 0.001f || Mathf.Abs(localScale.y - scaleFactor) > 0.001f)
				{
					___AP.transform.DOScale(scaleFactor, 0.5f).SetUpdate(isIndependentUpdate: true);
				}
			}
		}
	}
}
