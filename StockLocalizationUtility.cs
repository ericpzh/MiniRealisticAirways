using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedObjects;

namespace MiniRealisticAirways;

/// <summary>
/// Removes stock localization ownership from Mod-created text. The original
/// localizer is still allowed to control unrelated stock objects, but a Mod
/// label must not be overwritten after its language/font transaction commits.
/// </summary>
internal static class StockLocalizationUtility
{
	internal static void Detach(GameObject root)
	{
		if (root == null)
		{
			return;
		}
		GameObjectLocalizer[] objectLocalizers = root.GetComponentsInChildren<GameObjectLocalizer>(includeInactive: true);
		for (int i = 0; i < objectLocalizers.Length; i++)
		{
			GameObjectLocalizer localizer = objectLocalizers[i];
			if (localizer == null)
			{
				continue;
			}
			RemoveLocalizedTextAndTypographyProperties(localizer);
		}
		LocalizeStringEvent[] stringEvents = root.GetComponentsInChildren<LocalizeStringEvent>(includeInactive: true);
		for (int i = 0; i < stringEvents.Length; i++)
		{
			LocalizeStringEvent stringEvent = stringEvents[i];
			if (stringEvent == null)
			{
				continue;
			}
			stringEvent.enabled = false;
			UnityEngine.Object.Destroy(stringEvent);
		}
	}

	/// <summary>
	/// Leaves a stock label's text, font and material variants under the game
	/// localizer, but removes layout properties that a Mod controller must keep
	/// stable after the locale transaction (for example QRH title line spacing).
	/// </summary>
	internal static void TakeLayoutOwnership(TMP_Text label)
	{
		if (label == null)
		{
			return;
		}
		GameObjectLocalizer[] objectLocalizers = label.GetComponentsInParent<GameObjectLocalizer>(includeInactive: true);
		for (int i = 0; i < objectLocalizers.Length; i++)
		{
			GameObjectLocalizer localizer = objectLocalizers[i];
			if (localizer == null)
			{
				continue;
			}
			RemovePropertiesForLabel(localizer, label, removeText: false, removeTypography: false, removeLayout: true);
		}
	}

	/// <summary>
	/// Takes ownership of one text component, including when the stock
	/// GameObjectLocalizer is attached to a parent and tracks the child.
	/// </summary>
	internal static void TakeOwnership(TMP_Text label)
	{
		if (label == null)
		{
			return;
		}

		// LocalizeStringEvent only writes the TMP component on its own GameObject;
		// never disable a parent event that may own a different stock label.
		LocalizeStringEvent[] stringEvents = label.GetComponents<LocalizeStringEvent>();
		for (int i = 0; i < stringEvents.Length; i++)
		{
			LocalizeStringEvent stringEvent = stringEvents[i];
			if (stringEvent == null)
			{
				continue;
			}
			stringEvent.enabled = false;
			UnityEngine.Object.Destroy(stringEvent);
		}

		GameObjectLocalizer[] objectLocalizers = label.GetComponentsInParent<GameObjectLocalizer>(includeInactive: true);
		for (int i = 0; i < objectLocalizers.Length; i++)
		{
			GameObjectLocalizer localizer = objectLocalizers[i];
			if (localizer == null)
			{
				continue;
			}
			RemovePropertiesForLabel(localizer, label, removeText: true, removeTypography: true, removeLayout: false);
		}
	}

	private static void RemoveLocalizedTextAndTypographyProperties(GameObjectLocalizer localizer)
	{
		List<TrackedObject> trackedObjects = localizer.TrackedObjects;
		for (int i = trackedObjects.Count - 1; i >= 0; i--)
		{
			TrackedObject trackedObject = trackedObjects[i];
			if (trackedObject == null)
			{
				trackedObjects.RemoveAt(i);
				continue;
			}
			if (!(trackedObject.Target is TMP_Text))
			{
				// A cloned QRH root must keep its child font variants, but its
				// RectTransform/property variants must not move the resident button.
				trackedObjects.RemoveAt(i);
				continue;
			}
			for (int propertyIndex = trackedObject.TrackedProperties.Count - 1; propertyIndex >= 0; propertyIndex--)
			{
				string propertyPath = trackedObject.TrackedProperties[propertyIndex]?.PropertyPath;
				if (IsTextProperty(propertyPath) || IsTypographyProperty(propertyPath))
				{
					trackedObject.TrackedProperties.RemoveAt(propertyIndex);
				}
			}
		}
	}

	private static void RemovePropertiesForLabel(GameObjectLocalizer localizer, TMP_Text label, bool removeText, bool removeTypography, bool removeLayout)
	{
		List<TrackedObject> trackedObjects = localizer.TrackedObjects;
		for (int i = trackedObjects.Count - 1; i >= 0; i--)
		{
			TrackedObject trackedObject = trackedObjects[i];
			if (trackedObject == null)
			{
				trackedObjects.RemoveAt(i);
				continue;
			}
			if (!(trackedObject.Target is TMP_Text target) || target != label)
			{
				continue;
			}
			for (int propertyIndex = trackedObject.TrackedProperties.Count - 1; propertyIndex >= 0; propertyIndex--)
			{
				string propertyPath = trackedObject.TrackedProperties[propertyIndex]?.PropertyPath;
				if ((removeText && IsTextProperty(propertyPath)) || (removeTypography && IsTypographyProperty(propertyPath)) || (removeLayout && IsLayoutProperty(propertyPath)))
				{
					trackedObject.TrackedProperties.RemoveAt(propertyIndex);
				}
			}
		}
	}

	private static bool IsTextProperty(string propertyPath)
	{
		return string.Equals(propertyPath, "m_text", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "text", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_UnicodeText", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "unicodeText", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsTypographyProperty(string propertyPath)
	{
		return string.Equals(propertyPath, "m_fontAsset", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "fontAsset", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_sharedMaterial", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "fontSharedMaterial", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_fontStyle", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "fontStyle", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_fontWeight", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "fontWeight", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsLayoutProperty(string propertyPath)
	{
		return string.Equals(propertyPath, "m_lineSpacing", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "lineSpacing", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_enableWordWrapping", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "enableWordWrapping", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_enableAutoSizing", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "enableAutoSizing", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_horizontalAlignment", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "horizontalAlignment", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "m_verticalAlignment", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(propertyPath, "verticalAlignment", StringComparison.OrdinalIgnoreCase);
	}
}
