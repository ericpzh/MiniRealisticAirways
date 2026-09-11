using System;
using TMPro;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// Applies the game's locale direction to Mod-owned text while preserving the
/// typography selected by the stock prefab/localizer. The aircraft HUD has a
/// separate Regular-font path because it is Mod-owned world-space text.
/// </summary>
internal static class ChineseTypography
{
	internal enum StockTextRole
	{
		Generic,
		QrhButton,
		ModalTitle,
		ModalHeading,
		ModalDescription,
		ModalButton,
		SettingsLabel
	}

	internal static void SetText(TMP_Text label, string logicalText, StockTextRole role = StockTextRole.Generic)
	{
		if (label == null)
		{
			return;
		}
		ArabicTextFormatter.SetText(label, logicalText);
		Apply(label, role);
	}

	internal static void SetHudText(TMP_Text label, string logicalText, string context)
	{
		SetHudText(label, logicalText, context, bold: false);
	}

	/// <summary>
	/// Sets a Mod-owned world-space HUD label. Wind text has its own explicit
	/// Bold role so it can use the locale's official face without changing the
	/// Regular weight used by aircraft and waypoint HUD labels.
	/// </summary>
	internal static void SetHudText(TMP_Text label, string logicalText, string context, bool bold)
	{
		if (label == null)
		{
			return;
		}
		ArabicTextFormatter.SetText(label, logicalText);
		LocalizedFontRegistry.ApplyHud(label, StripRichText(label.text), context, bold);
		RefreshMesh(label);
	}

	internal static void SetWindText(TMP_Text label, string logicalText, string context)
	{
		// Direction and weight are independent concerns. Arabic uses the same
		// official Bold role as every other locale; RTL placement is handled by the
		// text formatter and WindSock's rendered-edge anchor.
		SetHudText(label, logicalText, context, bold: true);
	}

	internal static void Apply(TMP_Text label, StockTextRole role = StockTextRole.Generic)
	{
		if (label == null)
		{
			return;
		}

		ArabicTextFormatter.Apply(label);
		// Keep the stock role's metrics and material, but repair the primary
		// official face when its serialized TMP atlas does not contain this text.
		// The service never adds another family to a fallback table.
		LocalizedFontRegistry.ApplyStock(label, StripRichText(label.text), role);
		RefreshMesh(label);
	}

	/// <summary>Kept for source compatibility; stock QRH typography is authoritative.</summary>
	internal static void ApplyQrhWeight(TMP_Text label)
	{
		Apply(label, StockTextRole.QrhButton);
	}

	/// <summary>Copies the active stock control's TMP metrics only.</summary>
	internal static bool CopyStockTypography(TMP_Text target, TMP_Text source)
	{
		if (target == null || source == null)
		{
			return false;
		}
		try
		{
			target.fontStyle = source.fontStyle;
			target.fontWeight = source.fontWeight;
			target.fontSize = source.fontSize;
			target.fontSizeMin = source.fontSizeMin;
			target.fontSizeMax = source.fontSizeMax;
			target.characterSpacing = source.characterSpacing;
			target.wordSpacing = source.wordSpacing;
			target.lineSpacing = source.lineSpacing;
			target.paragraphSpacing = source.paragraphSpacing;
			target.margin = source.margin;
			target.horizontalAlignment = source.horizontalAlignment;
			target.verticalAlignment = source.verticalAlignment;
			target.enableWordWrapping = source.enableWordWrapping;
			target.overflowMode = source.overflowMode;
			target.richText = source.richText;
			target.enableAutoSizing = source.enableAutoSizing;
			return true;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Stock TMP typography is not ready; deferred retry will resync it: " + exception.GetBaseException().Message);
			return false;
		}
	}

	internal static string StripRichText(string text)
	{
		if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
		{
			return text;
		}
		char[] buffer = new char[text.Length];
		int length = 0;
		bool insideTag = false;
		for (int i = 0; i < text.Length; i++)
		{
			char character = text[i];
			if (character == '<')
			{
				insideTag = true;
				continue;
			}
			if (insideTag && character == '>')
			{
				insideTag = false;
				continue;
			}
			if (!insideTag)
			{
				buffer[length++] = character;
			}
		}
		return new string(buffer, 0, length);
	}

	private static void RefreshMesh(TMP_Text label)
	{
		try
		{
			label.ForceMeshUpdate(false, true);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Localized text mesh refresh failed: " + exception.GetBaseException().Message);
		}
	}

	internal static void Reset()
	{
		LocalizedFontRegistry.Reset();
		ArabicTextFormatter.Reset();
	}

	internal static void ResetSceneState()
	{
		LocalizedFontRegistry.ResetSceneState();
		ArabicTextFormatter.Reset();
	}
}
