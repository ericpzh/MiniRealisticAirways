using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRealisticAirways;

[HarmonyPatch]
public class MainMenuManagerPatch
{
	private static readonly FieldInfo EditorButtonField = AccessTools.Field(typeof(MainMenuManager), "EditorButton");

	private static bool missingEditorButtonLogged_;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MainMenuManager), "Start")]
	public static void StartPostfix(MainMenuManager __instance, ref Button ___EditorButton)
	{
		EnsureButton(___EditorButton);
	}

	// Some Linux/BepInEx launch orders attach this mod after MainMenuManager.Start.
	// This fallback only performs work until the button exists; it never rewrites
	// typography every frame.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MainMenuManager), "Update")]
	public static void EnsureButtonFallbackPostfix(MainMenuManager __instance, ref Button ___EditorButton)
	{
		if (Tutorial.tutorialButton == null)
		{
			EnsureButton(___EditorButton);
		}
	}

	internal static bool TryEnsureButton(MainMenuManager manager)
	{
		if (manager == null || EditorButtonField == null)
		{
			LogMissingEditorButton();
			return false;
		}
		Button editorButton;
		try
		{
			editorButton = EditorButtonField.GetValue(manager) as Button;
		}
		catch (System.Exception exception)
		{
			Plugin.Log?.LogWarning("QRH button lookup failed: " + exception.GetBaseException().Message);
			return false;
		}
		return EnsureButton(editorButton);
	}

	private static bool EnsureButton(Button editorButton)
	{
		if (Tutorial.tutorialButton != null)
		{
			Tutorial.SetQRHSource(editorButton);
			Tutorial.SubscribeLocaleChanges();
			Tutorial.SetQRHText();
			return true;
		}
		if (editorButton == null)
		{
			LogMissingEditorButton();
			return false;
		}
		GameObject tutorialObject = Object.Instantiate(editorButton.gameObject, editorButton.transform.parent);
		StockLocalizationUtility.Detach(tutorialObject);
		Tutorial.tutorialButton = tutorialObject == null ? null : tutorialObject.GetComponent<Button>();
		if (Tutorial.tutorialButton == null)
		{
			Plugin.Log?.LogWarning("QRH button creation failed: cloned EditorButton has no Button component.");
			return false;
		}
		Tutorial.tutorialButton.transform.localPosition = new Vector3(1280f, -430f, 0f);
		Tutorial.SetQRHSource(editorButton);
		Tutorial.SubscribeLocaleChanges();
		Tutorial.SetQRHText();
		Tutorial.tutorialButton.onClick.RemoveAllListeners();
		Tutorial.tutorialButton.onClick = new Button.ButtonClickedEvent();
		Tutorial.tutorialButton.onClick.AddListener(delegate
		{
			if (AudioManager.instance != null)
			{
				AudioManager.instance.StartCoroutine(Tutorial.ShowTutorialCoroutine(manualTrigger: true));
			}
		});
		Plugin.Log?.LogInfo("QRH button initialized.");
		return true;
	}

	private static void LogMissingEditorButton()
	{
		if (!missingEditorButtonLogged_)
		{
			missingEditorButtonLogged_ = true;
			Plugin.Log?.LogWarning("QRH button initialization is waiting for MainMenuManager.EditorButton.");
		}
	}
}
