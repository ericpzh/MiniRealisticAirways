using System;
using TMPro;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// Owns the aircraft's Mod-created HUD geometry. Text is rendered with one
/// locale Regular face and one size; altitude/speed levels are rendered by
/// shared solid-block sprites so their height never depends on a glyph.
/// Every row is measured as a whole and centered on the aircraft icon.
/// </summary>
internal sealed class AircraftHudLayoutController : MonoBehaviour
{
	private const float HudFontSize = HudVisualMetrics.FontSize;
	private const float LabelHeight = 2.5f;
	private const float IndicatorSize = HudVisualMetrics.BlockSize;
	private const float IndicatorGap = HudVisualMetrics.BlockGap;
	// The block origin is measured from the rendered colon edge, so localized
	// prefix widths cannot create a second, hidden gap. Both rows intentionally
	// use three current-font spaces for a consistent visual distance.
	private const float IndicatorTextOpticalGap = 0.08f;
	private const float AltitudeIndicatorSpaceCount = 3f;
	private const float SpeedIndicatorSpaceCount = 3f;
	private const float IndicatorSlotWidth = 3f * IndicatorSize + 2f * IndicatorGap;
	private const float ColumnGap = 0.6f;
	// AircraftState creates the labels around y=-3.6. Move the whole two-row
	// grid up by roughly one old line height so it sits closer to the icon.
	private const float FirstRowY = -2.55f;
	private const float MinimumRowGap = 0.05f;
	private const float TextZ = 5f;
	private const float IndicatorZ = 4.8f;

	private static int lastGeometryDiagnosticRevision_ = -1;

	private Aircraft aircraft_;
	private TMP_Text altitudePrefix_;
	private TMP_Text speedPrefix_;
	private TMP_Text fuelText_;
	private TMP_Text weightText_;
	private SpriteRenderer[] altitudeIndicators_;
	private SpriteRenderer[] speedIndicators_;
	private bool initialized_;
	private bool lastVisible_;
	private AltitudeLevel lastAltitude_;
	private SpeedLevel lastSpeed_;
	private int lastLocaleRevision_ = -1;
	private string lastFuelText_;
	private string lastWeightText_;

	internal static AircraftHudLayoutController GetOrCreate(Aircraft aircraft)
	{
		if (aircraft == null)
		{
			return null;
		}
		AircraftHudLayoutController controller = aircraft.GetComponent<AircraftHudLayoutController>();
		if (controller == null)
		{
			controller = aircraft.gameObject.AddComponent<AircraftHudLayoutController>();
		}
		return controller;
	}

	internal void Initialize(Aircraft aircraft, TMP_Text altitudePrefix, TMP_Text speedPrefix, TMP_Text fuelText, TMP_Text weightText, AircraftVisualSortingController sorting)
	{
		if (initialized_ || aircraft == null)
		{
			return;
		}
		aircraft_ = aircraft;
		altitudePrefix_ = altitudePrefix;
		speedPrefix_ = speedPrefix;
		fuelText_ = fuelText;
		weightText_ = weightText;
		ConfigureText(altitudePrefix_);
		ConfigureText(speedPrefix_);
		ConfigureText(fuelText_);
		ConfigureText(weightText_);
		CreateIndicators(ref altitudeIndicators_, "AltitudeBlock");
		CreateIndicators(ref speedIndicators_, "SpeedBlock");
		if (sorting != null)
		{
			sorting.RegisterHudIndicatorRenderers(altitudeIndicators_);
			sorting.RegisterHudIndicatorRenderers(speedIndicators_);
		}
		initialized_ = true;
	}

	internal void Refresh(bool visible, AltitudeLevel altitude, SpeedLevel speed)
	{
		if (!initialized_)
		{
			return;
		}
		int localeRevision = ModLocalization.Revision;
		string fuelText = fuelText_ == null ? null : fuelText_.text;
		string weightText = weightText_ == null ? null : weightText_.text;
		if (visible == lastVisible_ && altitude == lastAltitude_ && speed == lastSpeed_ && localeRevision == lastLocaleRevision_ && string.Equals(fuelText, lastFuelText_, StringComparison.Ordinal) && string.Equals(weightText, lastWeightText_, StringComparison.Ordinal))
		{
			return;
		}
		// 三个等级块的位置始终预留；仅可见数量改变不需要重新测量 TMP。
		bool layoutChanged = visible != lastVisible_ || localeRevision != lastLocaleRevision_
			|| !string.Equals(fuelText, lastFuelText_, StringComparison.Ordinal)
			|| !string.Equals(weightText, lastWeightText_, StringComparison.Ordinal);
		lastVisible_ = visible;
		lastAltitude_ = altitude;
		lastSpeed_ = speed;
		lastLocaleRevision_ = localeRevision;
		lastFuelText_ = fuelText;
		lastWeightText_ = weightText;
		UpdateIndicators(altitudeIndicators_, visible ? AltitudeBlockCount(altitude) : 0);
		UpdateIndicators(speedIndicators_, visible ? SpeedBlockCount(speed) : 0);
		if (layoutChanged) LayoutRows();
	}

	private void ConfigureText(TMP_Text label)
	{
		if (label == null)
		{
			return;
		}
		label.fontSize = HudFontSize;
		label.fontStyle = FontStyles.Normal;
		label.fontWeight = FontWeight.Regular;
		label.horizontalAlignment = HorizontalAlignmentOptions.Left;
		label.verticalAlignment = VerticalAlignmentOptions.Middle;
		label.enableAutoSizing = false;
		label.enableWordWrapping = false;
		label.overflowMode = TextOverflowModes.Overflow;
		label.transform.localScale = Vector3.one;
		label.transform.localRotation = Quaternion.identity;
		label.transform.localPosition = new Vector3(label.transform.localPosition.x, label.transform.localPosition.y, TextZ);
		if (label.rectTransform != null)
		{
			label.rectTransform.pivot = new Vector2(0f, 0.5f);
			label.rectTransform.sizeDelta = new Vector2(1f, LabelHeight);
		}
	}

	private void CreateIndicators(ref SpriteRenderer[] indicators, string namePrefix)
	{
		indicators = new SpriteRenderer[3];
		for (int i = 0; i < indicators.Length; i++)
		{
			GameObject indicatorObject = new GameObject(namePrefix + (i + 1));
			indicatorObject.transform.SetParent(aircraft_.transform, worldPositionStays: false);
			indicatorObject.transform.localScale = new Vector3(IndicatorSize, IndicatorSize, 1f);
			indicatorObject.transform.localPosition = new Vector3(0f, 0f, IndicatorZ);
			SpriteRenderer renderer = indicatorObject.AddComponent<SpriteRenderer>();
			renderer.sprite = HudIndicatorTexture.GetOrCreate();
			renderer.color = Color.white;
			renderer.enabled = false;
			indicators[i] = renderer;
		}
	}

	private void UpdateIndicators(SpriteRenderer[] indicators, int count)
	{
		if (indicators == null)
		{
			return;
		}
		for (int i = 0; i < indicators.Length; i++)
		{
			SpriteRenderer renderer = indicators[i];
			if (renderer == null)
			{
				continue;
			}
			renderer.enabled = i < count;
		}
	}

	private void LayoutRows()
	{
		HorizontalAlignmentOptions textAlignment = ModLocalization.IsRtl ? HorizontalAlignmentOptions.Right : HorizontalAlignmentOptions.Left;
		SetTextAlignment(altitudePrefix_, textAlignment);
		SetTextAlignment(speedPrefix_, textAlignment);
		SetTextAlignment(fuelText_, textAlignment);
		SetTextAlignment(weightText_, textAlignment);
		float altitudeWidth = Measure(altitudePrefix_);
		float speedWidth = Measure(speedPrefix_);
		float fuelWidth = Measure(fuelText_);
		float weightWidth = Measure(weightText_);
		float altitudeSpaceWidth = ToAircraftLocalDistance(altitudePrefix_, MeasureSpaceWidth(altitudePrefix_));
		float speedSpaceWidth = ToAircraftLocalDistance(speedPrefix_, MeasureSpaceWidth(speedPrefix_));
		float altitudeIndicatorTextGap = IndicatorTextOpticalGap + altitudeSpaceWidth * AltitudeIndicatorSpaceCount;
		float speedIndicatorTextGap = IndicatorTextOpticalGap + speedSpaceWidth * SpeedIndicatorSpaceCount;
		float altitudeSlotWidth = altitudeWidth + altitudeIndicatorTextGap + IndicatorSlotWidth;
		// Reserve the complete three-block slot even when the current level is
		// low/normal. This keeps the speed and aircraft-type columns fixed while
		// the visible block count changes.
		float altitudeColumnWidth = Mathf.Max(fuelWidth, altitudeSlotWidth);
		float speedColumnWidth = Mathf.Max(weightWidth, speedWidth + speedIndicatorTextGap + IndicatorSlotWidth);
		float totalWidth = altitudeColumnWidth + ColumnGap + speedColumnWidth;
		float firstRowHeight = Mathf.Max(IndicatorSize, LayoutHeight(altitudePrefix_), LayoutHeight(speedPrefix_));
		float secondRowHeight = Mathf.Max(LayoutHeight(fuelText_), LayoutHeight(weightText_));
		float secondRowY = FirstRowY - (firstRowHeight + secondRowHeight) * 0.5f - MinimumRowGap;
		float leftColumn = -totalWidth * 0.5f;
		float rightColumn = leftColumn + altitudeColumnWidth + ColumnGap;
		if (ModLocalization.IsRtl)
		{
			// Mirror the columns for right-to-left locales while keeping each
			// label and its blocks in one compact semantic group.
			float altitudeRight = totalWidth * 0.5f;
			float speedRight = leftColumn + speedColumnWidth;
			PlaceTextRight(altitudePrefix_, altitudeRight, FirstRowY, altitudeWidth);
			PlaceTextRight(speedPrefix_, speedRight, FirstRowY, speedWidth);
			PlaceTextRight(fuelText_, altitudeRight, secondRowY, fuelWidth);
			PlaceTextRight(weightText_, speedRight, secondRowY, weightWidth);
			// Arabic text is shaped into presentation forms before TMP lays it out.
			// RectTransform width therefore cannot tell us which side of the visible
			// glyphs is actually left. Read the final mesh bounds and put the blocks
			// just outside that edge; this keeps the speed blocks left of the Arabic
			// label instead of allowing them to cross to its right.
			float altitudeVisibleLeft = GetColonEdgeInAircraft(altitudePrefix_, left: true, altitudeRight - altitudeWidth);
			float speedVisibleLeft = GetColonEdgeInAircraft(speedPrefix_, left: true, speedRight - speedWidth);
			PlaceIndicatorsRtl(altitudeIndicators_, altitudeVisibleLeft - altitudeIndicatorTextGap, FirstRowY, VisibleCount(altitudeIndicators_));
			PlaceIndicatorsRtl(speedIndicators_, speedVisibleLeft - speedIndicatorTextGap, FirstRowY, VisibleCount(speedIndicators_));
			LogLayoutGeometry(altitudeIndicatorTextGap, speedIndicatorTextGap, altitudeSpaceWidth, speedSpaceWidth, altitudeVisibleLeft, speedVisibleLeft);
		}
		else
		{
			// Keep both rows in the same centered grid. Indicator positions below are
			// derived from the final rendered colon edge, not from the lower-row value
			// width, so each row's requested spacing is measured from its own glyphs.
			PlaceText(fuelText_, leftColumn, secondRowY, fuelWidth);
			PlaceText(altitudePrefix_, leftColumn, FirstRowY, altitudeWidth);
			PlaceText(speedPrefix_, rightColumn, FirstRowY, speedWidth);
			PlaceText(weightText_, rightColumn, secondRowY, weightWidth);
			float altitudeVisibleRight = GetColonEdgeInAircraft(altitudePrefix_, left: false, leftColumn + altitudeWidth);
			float speedVisibleRight = GetColonEdgeInAircraft(speedPrefix_, left: false, rightColumn + speedWidth);
			PlaceIndicators(altitudeIndicators_, altitudeVisibleRight + altitudeIndicatorTextGap, FirstRowY, VisibleCount(altitudeIndicators_));
			PlaceIndicators(speedIndicators_, speedVisibleRight + speedIndicatorTextGap, FirstRowY, VisibleCount(speedIndicators_));
			LogLayoutGeometry(altitudeIndicatorTextGap, speedIndicatorTextGap, altitudeSpaceWidth, speedSpaceWidth, altitudeVisibleRight, speedVisibleRight);
		}
	}

	private static void SetTextAlignment(TMP_Text label, HorizontalAlignmentOptions alignment)
	{
		if (label != null)
		{
			label.horizontalAlignment = alignment;
		}
	}

	private void PlaceText(TMP_Text label, float x, float y, float width)
	{
		if (label == null)
		{
			return;
		}
		if (label.rectTransform != null)
		{
			label.rectTransform.pivot = new Vector2(0f, 0.5f);
			label.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.01f, width), Mathf.Max(0.01f, LayoutHeight(label)));
		}
		label.transform.localPosition = new Vector3(x, y, TextZ);
	}

	private void PlaceTextRight(TMP_Text label, float rightEdge, float y, float width)
	{
		if (label == null)
		{
			return;
		}
		if (label.rectTransform != null)
		{
			label.rectTransform.pivot = new Vector2(1f, 0.5f);
			label.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.01f, width), Mathf.Max(0.01f, LayoutHeight(label)));
		}
		label.transform.localPosition = new Vector3(rightEdge, y, TextZ);
	}

	private void PlaceIndicators(SpriteRenderer[] indicators, float x, float y, int count)
	{
		if (indicators == null)
		{
			return;
		}
		for (int i = 0; i < indicators.Length; i++)
		{
			SpriteRenderer renderer = indicators[i];
			if (renderer == null)
			{
				continue;
			}
			renderer.transform.localPosition = new Vector3(x + IndicatorSize * 0.5f + i * (IndicatorSize + IndicatorGap), y, IndicatorZ);
			renderer.transform.localScale = new Vector3(IndicatorSize, IndicatorSize, 1f);
			renderer.enabled = i < count;
		}
	}

	private void PlaceIndicatorsRtl(SpriteRenderer[] indicators, float rightEdge, float y, int count)
	{
		if (indicators == null)
		{
			return;
		}
		for (int i = 0; i < indicators.Length; i++)
		{
			SpriteRenderer renderer = indicators[i];
			if (renderer == null)
			{
				continue;
			}
			renderer.transform.localPosition = new Vector3(rightEdge - IndicatorSize * 0.5f - i * (IndicatorSize + IndicatorGap), y, IndicatorZ);
			renderer.transform.localScale = new Vector3(IndicatorSize, IndicatorSize, 1f);
			renderer.enabled = i < count;
		}
	}

	private float GetVisibleEdgeInAircraft(TMP_Text label, bool left, float fallback)
	{
		if (label == null || aircraft_ == null)
		{
			return fallback;
		}
		try
		{
			label.ForceMeshUpdate(false, true);
			if (label.textInfo == null || label.textInfo.characterInfo == null)
			{
				return fallback;
			}
			bool found = false;
			float edge = left ? float.PositiveInfinity : float.NegativeInfinity;
			for (int i = 0; i < label.textInfo.characterCount; i++)
			{
				TMP_CharacterInfo character = label.textInfo.characterInfo[i];
				if (!character.isVisible)
				{
					continue;
				}
				float characterEdge = left ? character.bottomLeft.x : character.topRight.x;
				if (!found || (left ? characterEdge < edge : characterEdge > edge))
				{
					edge = characterEdge;
					found = true;
				}
			}
			if (!found)
			{
				return fallback;
			}
			Vector3 worldPoint = label.transform.TransformPoint(new Vector3(edge, 0f, 0f));
			return aircraft_.transform.InverseTransformPoint(worldPoint).x;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Aircraft HUD visible-bound measurement deferred: " + exception.GetBaseException().Message);
			return fallback;
		}
	}

	private float GetColonEdgeInAircraft(TMP_Text label, bool left, float fallback)
	{
		if (label == null || aircraft_ == null)
		{
			return fallback;
		}
		try
		{
			label.ForceMeshUpdate(false, true);
			if (label.textInfo != null && label.textInfo.characterInfo != null)
			{
				for (int i = 0; i < label.textInfo.characterCount; i++)
				{
					TMP_CharacterInfo character = label.textInfo.characterInfo[i];
					if (!character.isVisible || (character.character != ':' && character.character != '\uFF1A'))
					{
						continue;
					}
					float edge = left ? character.bottomLeft.x : character.topRight.x;
					Vector3 worldPoint = label.transform.TransformPoint(new Vector3(edge, 0f, 0f));
					return aircraft_.transform.InverseTransformPoint(worldPoint).x;
				}
			}
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Aircraft HUD colon-bound measurement deferred: " + exception.GetBaseException().Message);
		}
		return GetVisibleEdgeInAircraft(label, left, fallback);
	}

	private float ToAircraftLocalDistance(TMP_Text label, float textLocalDistance)
	{
		if (label == null || aircraft_ == null || textLocalDistance <= 0f || float.IsNaN(textLocalDistance) || float.IsInfinity(textLocalDistance))
		{
			return 0f;
		}
		try
		{
			Vector3 origin = aircraft_.transform.InverseTransformPoint(label.transform.TransformPoint(Vector3.zero));
			Vector3 end = aircraft_.transform.InverseTransformPoint(label.transform.TransformPoint(new Vector3(textLocalDistance, 0f, 0f)));
			float distance = Mathf.Abs(end.x - origin.x);
			return distance > 0f && !float.IsNaN(distance) && !float.IsInfinity(distance) ? distance : 0f;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Aircraft HUD space-width coordinate conversion deferred: " + exception.GetBaseException().Message);
			return 0f;
		}
	}

	private void LogLayoutGeometry(float altitudeIndicatorTextGap, float speedIndicatorTextGap, float altitudeSpaceWidth, float speedSpaceWidth, float altitudeColonEdge, float speedColonEdge)
	{
		if (lastGeometryDiagnosticRevision_ == ModLocalization.Revision || aircraft_ == null || altitudeIndicators_ == null || speedIndicators_ == null || altitudeIndicators_.Length == 0 || speedIndicators_.Length == 0 || altitudeIndicators_[0] == null || speedIndicators_[0] == null)
		{
			return;
		}
		float altitudeIndicatorEdge = IndicatorFacingEdge(altitudeIndicators_[0]);
		float speedIndicatorEdge = IndicatorFacingEdge(speedIndicators_[0]);
		float altitudeActualGap = ModLocalization.IsRtl ? altitudeColonEdge - altitudeIndicatorEdge : altitudeIndicatorEdge - altitudeColonEdge;
		float speedActualGap = ModLocalization.IsRtl ? speedColonEdge - speedIndicatorEdge : speedIndicatorEdge - speedColonEdge;
		if (!IsFinite(altitudeActualGap) || !IsFinite(speedActualGap))
		{
			return;
		}
		Plugin.Log?.LogInfo("Aircraft HUD geometry locale=" + ModLocalization.CurrentLocaleCode + " rtl=" + ModLocalization.IsRtl + " altitudeSpaceAdvance=" + altitudeSpaceWidth.ToString("0.###") + " speedSpaceAdvance=" + speedSpaceWidth.ToString("0.###") + " altitudeTargetGap=" + altitudeIndicatorTextGap.ToString("0.###") + " speedTargetGap=" + speedIndicatorTextGap.ToString("0.###") + " altitudeGap=" + altitudeActualGap.ToString("0.###") + " speedGap=" + speedActualGap.ToString("0.###") + " altitudeColon=" + altitudeColonEdge.ToString("0.###") + " speedColon=" + speedColonEdge.ToString("0.###"));
		lastGeometryDiagnosticRevision_ = ModLocalization.Revision;
	}

	private float IndicatorFacingEdge(SpriteRenderer renderer)
	{
		if (renderer == null)
		{
			return float.NaN;
		}
		float halfSize = Mathf.Abs(renderer.transform.localScale.x) * 0.5f;
		return renderer.transform.localPosition.x + (ModLocalization.IsRtl ? halfSize : -halfSize);
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static float Measure(TMP_Text label)
	{
		if (label == null || string.IsNullOrEmpty(label.text))
		{
			return 0f;
		}
		try
		{
			label.ForceMeshUpdate(false, true);
			return Mathf.Max(0f, label.GetPreferredValues(label.text).x);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Aircraft HUD width measurement deferred: " + exception.GetBaseException().Message);
			return 0f;
		}
	}

	private static float MeasureSpaceWidth(TMP_Text label)
	{
		if (label == null)
		{
			return 0f;
		}
		try
		{
			label.ForceMeshUpdate(false, true);
			// TMP trims leading/trailing whitespace when measuring a stand-alone
			// string. Measure an internal space instead, then subtract the same
			// characters without the space so the glyph advance survives trimming.
			float withSpace = label.GetPreferredValues("0 0").x;
			float withoutSpace = label.GetPreferredValues("00").x;
			float width = withSpace - withoutSpace;
			if (!(width > 0f) || float.IsNaN(width) || float.IsInfinity(width))
			{
				withSpace = label.GetPreferredValues("1 1").x;
				withoutSpace = label.GetPreferredValues("11").x;
				width = withSpace - withoutSpace;
			}
			return width > 0f && !float.IsNaN(width) && !float.IsInfinity(width) ? width : 0f;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Aircraft HUD space-width measurement deferred: " + exception.GetBaseException().Message);
			return 0f;
		}
	}

	private static float MeasureHeight(TMP_Text label)
	{
		if (label == null || string.IsNullOrEmpty(label.text))
		{
			return 0f;
		}
		try
		{
			label.ForceMeshUpdate(false, true);
			return Mathf.Max(0f, label.GetPreferredValues(label.text).y);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Aircraft HUD height measurement deferred: " + exception.GetBaseException().Message);
			return 0f;
		}
	}

	private static float LayoutHeight(TMP_Text label)
	{
		if (label == null || string.IsNullOrEmpty(label.text))
		{
			return 0f;
		}
		float measured = MeasureHeight(label);
		return measured > 0f ? measured : LabelHeight;
	}

	private static int VisibleCount(SpriteRenderer[] indicators)
	{
		if (indicators == null)
		{
			return 0;
		}
		int count = 0;
		for (int i = 0; i < indicators.Length; i++)
		{
			if (indicators[i] != null && indicators[i].enabled)
			{
				count++;
			}
		}
		return count;
	}

	private static int AltitudeBlockCount(AltitudeLevel altitude)
	{
		switch (altitude)
		{
			case AltitudeLevel.Low:
				return 1;
			case AltitudeLevel.Normal:
				return 2;
			case AltitudeLevel.High:
				return 3;
			default:
				return 0;
		}
	}

	private static int SpeedBlockCount(SpeedLevel speed)
	{
		switch (speed)
		{
			case SpeedLevel.Slow:
				return 1;
			case SpeedLevel.Normal:
				return 2;
			case SpeedLevel.Fast:
				return 3;
			default:
				return 0;
		}
	}

	internal static void ResetSharedSprite()
	{
		// 实心方块纹理已上移到 HudIndicatorTexture 共享；这里只保留诊断戳重置。
		HudIndicatorTexture.Reset();
		lastGeometryDiagnosticRevision_ = -1;
	}

	private void OnDestroy()
	{
		aircraft_ = null;
		altitudePrefix_ = null;
		speedPrefix_ = null;
		fuelText_ = null;
		weightText_ = null;
		altitudeIndicators_ = null;
		speedIndicators_ = null;
		initialized_ = false;
	}
}
