using System;
using TMPro;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// Localized waypoint geometry. Aircraft HUD geometry is owned by
/// AircraftHudLayoutController so it can measure both rows and center them as a
/// whole. This class remains only for the separate waypoint labels.
/// </summary>
internal static class HudLayoutProfiles
{
	private readonly struct Tuning
	{
		internal readonly float GroupGap;
		internal readonly float FontScale;

		internal Tuning(float groupGap, float fontScale)
		{
			GroupGap = groupGap;
			FontScale = fontScale;
		}
	}

	private static readonly Tuning Default = new Tuning(0.28f, 1f);

	// Share aircraft HUD dimensions; do not shrink waypoint labels per locale.
	internal const float TextFontSize = HudVisualMetrics.FontSize;
	internal const float TextBlockGap = HudVisualMetrics.BlockGap;
	// 所有布局距离均使用航点局部单位；文字与方块共同继承父级变换。
	// 0.34 为中文全角冒号（"高度："/"速度："）下文本与实心方块的防重叠值。
	internal const float TextIndicatorGap = 0.34f;

	internal static float BlockSize(float fontScale)
	{
		return HudVisualMetrics.BlockSize;
	}

	// Measure the final glyph mesh, including bearings, before placing either group.
	// The three-block slots keep columns stationary when a level changes.
	internal static bool ApplyWaypoint(TMP_Text altitudeText, TMP_Text speedText, TMP_Text nameText, SpriteRenderer[] altitudeBlocks, SpriteRenderer[] speedBlocks)
	{
		Tuning tuning = GetTuning(ModLocalization.CurrentLocaleCode);
		float fontSize = TextFontSize;
		SetFontScale(nameText, 6f * tuning.FontScale);
		if (!TryMeasure(altitudeText, fontSize, out Bounds altitudeInk)
			|| !TryMeasure(speedText, fontSize, out Bounds speedInk)) return false;
		float blockSize = BlockSize(tuning.FontScale);
		float stride = blockSize + TextBlockGap;
		float slotWidth = 3f * blockSize + 2f * TextBlockGap;
		float groupGap = Mathf.Max(0.45f, tuning.GroupGap);
		float totalWidth = altitudeInk.size.x + TextIndicatorGap + slotWidth + groupGap
			+ speedInk.size.x + TextIndicatorGap + slotWidth;
		float leftX = -totalWidth * 0.5f;
		const float rowY = -1.35f;
		PlaceLabel(altitudeText, altitudeInk, leftX, rowY);
		PlaceBlockRow(altitudeBlocks, leftX + altitudeInk.size.x + TextIndicatorGap, rowY, blockSize, stride);
		float speedX = leftX + altitudeInk.size.x + TextIndicatorGap + slotWidth + groupGap;
		PlaceLabel(speedText, speedInk, speedX, rowY);
		PlaceBlockRow(speedBlocks, speedX + speedInk.size.x + TextIndicatorGap, rowY, blockSize, stride);
		return true;
	}

	private static bool TryMeasure(TMP_Text label, float fontSize, out Bounds ink)
	{
		ink = default;
		if (label == null || string.IsNullOrEmpty(label.text)) return false;
		// Match the sibling blocks: TMP mesh units must equal waypoint-local units.
		label.transform.localScale = Vector3.one;
		label.transform.localRotation = Quaternion.identity;
		label.enableAutoSizing = false;
		label.fontSize = fontSize;
		label.enableWordWrapping = false;
		label.overflowMode = TextOverflowModes.Overflow;
		label.horizontalAlignment = HorizontalAlignmentOptions.Left;
		label.verticalAlignment = VerticalAlignmentOptions.Middle;
		label.rectTransform.pivot = new Vector2(0f, 0.5f);
		label.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, label.GetPreferredValues(label.text).x), 1f);
		label.ForceMeshUpdate(false, true);
		bool found = false;
		// characterInfo is a reusable capacity buffer: only characterCount is current.
		for (int i = 0; i < label.textInfo.characterCount; i++)
		{
			TMP_CharacterInfo character = label.textInfo.characterInfo[i];
			if (!character.isVisible) continue;
			if (!found) ink = new Bounds(character.bottomLeft, Vector3.zero);
			ink.Encapsulate(character.bottomLeft);
			ink.Encapsulate(character.topRight);
			found = true;
		}
		return found && ink.size.x > 0f && !float.IsNaN(ink.size.x) && !float.IsInfinity(ink.size.x);
	}

	private static void PlaceLabel(TMP_Text label, Bounds ink, float leftX, float centerY)
	{
		label.transform.localPosition = new Vector3(leftX - ink.min.x, centerY - ink.center.y, label.transform.localPosition.z);
	}

	private static Tuning GetTuning(string locale)
	{
		if (string.IsNullOrEmpty(locale))
		{
			return Default;
		}
		if (locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.42f, 0.92f);
		}
		if (locale.StartsWith("zh-hant", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.3f, 1f);
		}
		if (locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.3f, 1f);
		}
		if (locale.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.32f, 1f);
		}
		if (locale.StartsWith("de", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.34f, 0.96f);
		}
		if (locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.38f, 0.95f);
		}
		if (locale.StartsWith("fr", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("pl", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("ru", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("uk", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.34f, 0.96f);
		}
		if (locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("es", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.34f, 0.97f);
		}
		if (locale.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
		{
			return new Tuning(0.3f, 0.98f);
		}
		return Default;
	}

	private static void PlaceBlockRow(SpriteRenderer[] blocks, float leftX, float y, float blockSize, float stride)
	{
		if (blocks == null)
		{
			return;
		}
		for (int i = 0; i < blocks.Length; i++)
		{
			if (blocks[i] == null)
			{
				continue;
			}
			blocks[i].transform.localPosition = new Vector3(leftX + blockSize * 0.5f + (float)i * stride, y, blocks[i].transform.localPosition.z);
			blocks[i].transform.localScale = new Vector3(blockSize, blockSize, 1f);
		}
	}

	private static void SetFontScale(TMP_Text label, float size)
	{
		if (label != null)
		{
			label.fontSize = size;
		}
	}
}

