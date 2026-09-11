using System;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace MiniRealisticAirways;

// The bad replacement glyph is a single known entry in the ValuedSupporters
// asset. Clean that entry while creating its UI item, without changing the
// ScriptableObject or touching any other game's text.
[HarmonyPatch(typeof(SupporterContainer), "Awake")]
internal static class PatchSupporterContainer
{
	private const string ValuedSupportersAssetName = "ValuedSupporters";
	private const string BrokenMonschiWithSpace = "Monschi \uFFFD";
	private const string BrokenMonschiWithoutSpace = "Monschi\uFFFD";

	private static bool Prefix(SupporterContainer __instance)
	{
		if (__instance == null || __instance.supporters == null || __instance.prefab == null)
		{
			return true;
		}

		if (!string.Equals(__instance.supporters.name, ValuedSupportersAssetName, StringComparison.Ordinal))
		{
			return true;
		}

		if (__instance.supporters.data == null)
		{
			return false;
		}

		foreach (string datum in __instance.supporters.data)
		{
			GameObject item = UnityEngine.Object.Instantiate(__instance.prefab, __instance.transform);
			TMP_Text text = item == null ? null : item.GetComponentInChildren<TMP_Text>();
			if (text == null)
			{
				continue;
			}
			text.text = CleanKnownEntry(datum);
		}

		return false;
	}

	private static string CleanKnownEntry(string value)
	{
		if (value == BrokenMonschiWithSpace || value == BrokenMonschiWithoutSpace)
		{
			return "Monschi";
		}
		return value;
	}
}
