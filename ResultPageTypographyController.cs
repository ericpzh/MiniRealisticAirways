using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UIComponents.GameOverScreens;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// Keeps the result-page aircraft-count label on one line for long locales.
/// The stock localizer still owns the text and font; this controller only
/// applies the final wrapping/width geometry after that localizer has settled.
/// </summary>
internal sealed class ResultPageTypographyController : MonoBehaviour
{
	private static readonly Type SkeletonType = AccessTools.TypeByName("UIComponents.GameOverScreens.Skeleton");

	private static readonly FieldInfo TotalLabelField = SkeletonType == null ? null : AccessTools.Field(SkeletonType, "totalNumberOfAircraftLabel");

	private TMP_Text totalLabel_;

	private Component skeleton_;

	private RectTransform labelRect_;

	private float baseWidth_;

	private float baseHeight_;

	private bool initialized_;

	private Coroutine refreshCoroutine_;

	private int refreshGeneration_;

	private bool refreshPending_;

	private int appliedLocaleRevision_ = -1;

	private string appliedText_;

	private float appliedWidth_ = -1f;

	internal static ResultPageTypographyController Attach(Component skeleton)
	{
		if (skeleton == null || skeleton.gameObject == null || TotalLabelField == null)
		{
			return null;
		}
		ResultPageTypographyController controller = skeleton.GetComponent<ResultPageTypographyController>();
		if (controller == null)
		{
			controller = skeleton.gameObject.AddComponent<ResultPageTypographyController>();
		}
		controller.Initialize(skeleton);
		return controller;
	}

	internal void ScheduleRefresh()
	{
		Initialize(skeleton_);
		refreshGeneration_++;
		refreshPending_ = true;
		if (refreshCoroutine_ == null && isActiveAndEnabled)
		{
			refreshCoroutine_ = StartCoroutine(RefreshCoroutine());
		}
	}

	private void Initialize(Component skeleton)
	{
		if (initialized_)
		{
			return;
		}
		if (skeleton == null)
		{
			skeleton = skeleton_;
		}
		if (skeleton == null)
		{
			return;
		}
		skeleton_ = skeleton;
		try
		{
			totalLabel_ = TotalLabelField.GetValue(skeleton) as TMP_Text;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Result page aircraft-count label lookup deferred: " + exception.GetBaseException().Message);
			return;
		}
		if (totalLabel_ == null)
		{
			return;
		}
		labelRect_ = totalLabel_.rectTransform;
		if (labelRect_ == null)
		{
			return;
		}
		baseWidth_ = Mathf.Max(0.01f, labelRect_.rect.width);
		baseHeight_ = Mathf.Max(0.01f, labelRect_.rect.height);
		initialized_ = true;
		ModLocalization.LocaleChanged -= OnLocaleChanged;
		ModLocalization.LocaleChanged += OnLocaleChanged;
	}

	private void OnLocaleChanged(string localeCode)
	{
		ScheduleRefresh();
	}

	private IEnumerator RefreshCoroutine()
	{
		while (true)
		{
			int generation = refreshGeneration_;
			yield return null;
			yield return new WaitForEndOfFrame();
			if (generation != refreshGeneration_)
			{
				continue;
			}
			ApplyNow();
			// ResultPage.Show starts an async fade and Unity's localization/layout
			// callbacks can finish one frame later. A second pass makes the final
			// stock text authoritative without polling in Update().
			yield return null;
			if (generation == refreshGeneration_)
			{
				ApplyNow();
			}
			if (generation == refreshGeneration_)
			{
				refreshPending_ = false;
				break;
			}
		}
		refreshCoroutine_ = null;
	}

	private void OnEnable()
	{
		if (refreshPending_ && refreshCoroutine_ == null)
		{
			ScheduleRefresh();
		}
	}

	private void ApplyNow()
	{
		Initialize(skeleton_);
		if (!initialized_ || totalLabel_ == null || labelRect_ == null)
		{
			return;
		}
		try
		{
			totalLabel_.enableWordWrapping = false;
			totalLabel_.enableAutoSizing = false;
			totalLabel_.overflowMode = TextOverflowModes.Overflow;
			totalLabel_.ForceMeshUpdate(false, true);
			float preferredWidth = totalLabel_.GetPreferredValues(totalLabel_.text ?? string.Empty).x;
			float desiredWidth = Mathf.Max(baseWidth_, Mathf.Ceil(preferredWidth + 0.1f));
			labelRect_.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredWidth);
			labelRect_.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseHeight_);
			totalLabel_.ForceMeshUpdate(false, true);
			bool changed = appliedLocaleRevision_ != ModLocalization.Revision || !string.Equals(appliedText_, totalLabel_.text, StringComparison.Ordinal) || Mathf.Abs(appliedWidth_ - desiredWidth) > 0.01f;
			if (changed)
			{
				appliedLocaleRevision_ = ModLocalization.Revision;
				appliedText_ = totalLabel_.text;
				appliedWidth_ = desiredWidth;
				Plugin.Log?.LogInfo("Result page aircraft-count label forced to one line: locale=" + ModLocalization.CurrentLocaleCode + ", width=" + desiredWidth.ToString("0.##") + ", text=" + (totalLabel_.text ?? string.Empty));
			}
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Result page aircraft-count layout deferred: " + exception.GetBaseException().Message);
		}
	}

	private void OnDestroy()
	{
		ModLocalization.LocaleChanged -= OnLocaleChanged;
		refreshGeneration_++;
		refreshCoroutine_ = null;
		refreshPending_ = false;
		totalLabel_ = null;
		skeleton_ = null;
		labelRect_ = null;
		initialized_ = false;
	}
}

/// <summary>The game keeps the result-page label on Skeleton, while ResultPage itself only handles clicks.</summary>
[HarmonyPatch(typeof(Skeleton), "Start")]
internal static class PatchResultPageSkeletonStart
{
	private static void Postfix(Skeleton __instance)
	{
		ResultPageTypographyController.Attach(__instance)?.ScheduleRefresh();
	}
}

[HarmonyPatch(typeof(Skeleton), "Show")]
internal static class PatchResultPageSkeletonShow

{
	private static void Postfix(Skeleton __instance)
	{
		ResultPageTypographyController.Attach(__instance)?.ScheduleRefresh();
	}
}
