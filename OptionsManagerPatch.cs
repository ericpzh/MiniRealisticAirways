using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniRealisticAirways;

[HarmonyPatch]
public class OptionsManagerPatch
{
	private static readonly FieldInfo SubtitlesButtonField = AccessTools.Field(typeof(OptionsManager), "SubtitlesButton");

	private static readonly FieldInfo ColorAccessibilityTextField = AccessTools.Field(typeof(OptionsManager), "ColorAccessibilityText");

	private static readonly FieldInfo OnField = AccessTools.Field(typeof(OptionsManager), "On");

	private static readonly FieldInfo OffField = AccessTools.Field(typeof(OptionsManager), "Off");

	private static bool lateBootstrapWarningLogged_;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(OptionsManager), "Start")]
	public static void StartPostfix(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText, ref Sprite ___On, ref Sprite ___Off)
	{
		ModLocalization.Initialize();
		Settings.EnsureSetup(___SubtitlesButton, ___ColorAccessibilityText, ___On, ___Off);
	}

	/// <summary>
	/// Handles a late BepInEx injection where OptionsManager.Start already ran
	/// before Harmony was installed. FieldInfo is cached once; the normal Start
	/// and scene callbacks still use direct Harmony field arguments.
	/// </summary>
	internal static bool TryEnsureSetup(OptionsManager manager)
	{
		// The first scene bootstrap can legitimately run before the persistent
		// OptionsManager exists. That is a normal no-op, not a missing-field
		// failure; the Menu scene callback will retry once the object is alive.
		if (manager == null)
		{
			return false;
		}
		if (SubtitlesButtonField == null || ColorAccessibilityTextField == null || OnField == null || OffField == null)
		{
			if (!lateBootstrapWarningLogged_)
			{
				lateBootstrapWarningLogged_ = true;
				Plugin.Log?.LogWarning("OptionsManager fields were not found; Mod settings setup will remain disabled.");
			}
			return false;
		}
		try
		{
			Button subtitlesButton = SubtitlesButtonField.GetValue(manager) as Button;
			TMP_Text colorAccessibilityText = ColorAccessibilityTextField.GetValue(manager) as TMP_Text;
			Sprite on = OnField.GetValue(manager) as Sprite;
			Sprite off = OffField.GetValue(manager) as Sprite;
			return Settings.EnsureSetup(subtitlesButton, colorAccessibilityText, on, off);
		}
		catch (Exception exception)
		{
			if (!lateBootstrapWarningLogged_)
			{
				lateBootstrapWarningLogged_ = true;
				Plugin.Log?.LogWarning("Late OptionsManager bootstrap failed: " + exception.GetBaseException().Message);
			}
			return false;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(OptionsManager), "OnSceneLoaded")]
	public static void OnSceneLoadedPostfix(ref Scene arg0, LoadSceneMode arg1, ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText, ref Sprite ___On, ref Sprite ___Off)
	{
		Settings.EnsureSetup(___SubtitlesButton, ___ColorAccessibilityText, ___On, ___Off);
		if (Settings.windToggle == null || Settings.eventToggle == null || Settings.tcasToggle == null || Settings.windText == null || Settings.eventText == null || Settings.tcasText == null)
		{
			return;
		}
		if (arg0.name == "Menu")
		{
			Settings.windToggle.transform.parent.gameObject.SetActive(value: true);
			Settings.eventToggle.transform.parent.gameObject.SetActive(value: true);
			Settings.tcasToggle.transform.parent.gameObject.SetActive(value: true);
			Settings.windText.transform.parent.gameObject.SetActive(value: true);
			Settings.eventText.transform.parent.gameObject.SetActive(value: true);
			Settings.tcasText.transform.parent.gameObject.SetActive(value: true);
		}
		else
		{
			Settings.windToggle.transform.parent.gameObject.SetActive(value: false);
			Settings.eventToggle.transform.parent.gameObject.SetActive(value: false);
			Settings.tcasToggle.transform.parent.gameObject.SetActive(value: false);
			Settings.windText.transform.parent.gameObject.SetActive(value: false);
			Settings.eventText.transform.parent.gameObject.SetActive(value: false);
			Settings.tcasText.transform.parent.gameObject.SetActive(value: false);
		}
	}
}
