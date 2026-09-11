using System;
using System.Collections;
using ArabicSupport;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiniRealisticAirways;

public static class Settings
{
	public static bool DISABLE_WIND;

	public static bool DISABLE_EVENTS;

	public static bool DISABLE_TCAS;

	public static Button windToggle;

	public static Button eventToggle;

	public static Button tcasToggle;

	public static TMP_Text windText;

	public static TMP_Text eventText;

	public static TMP_Text tcasText;

	public static Sprite On;

	public static Sprite Off;

	private static GameObject setupRoot_;

	private static bool localeSubscribed_;

	private static TMP_Text fontSource_;

	private static Coroutine deferredRefresh_;

	private static int refreshGeneration_;

	private static GameObject setupLoggedRoot_;

	public static void ProcessLaunchOptions()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == "-disableWind")
			{
				Plugin.Log?.LogInfo("Wind disabled");
				DISABLE_WIND = true;
			}
			else if (commandLineArgs[i] == "-disableEvents")
			{
				Plugin.Log?.LogInfo("Event disabled");
				DISABLE_EVENTS = true;
			}
			else if (commandLineArgs[i] == "-disableTCAS")
			{
				Plugin.Log?.LogInfo("TCAS disabled");
				DISABLE_TCAS = true;
			}
		}
	}

	public static void SetupWindToggle(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText)
	{
		ModLocalization.Initialize();
		SubscribeLocaleChanges();
		RememberSetupRoot(___SubtitlesButton);
		if (windToggle == null)
		{
			SetupToggle(265f, -225f, ref windToggle, ref ___SubtitlesButton, OnWindButtonClick, !DISABLE_WIND, SettingsCloneMarker.Role.WindToggle);
		}
		if (windText == null)
		{
			SetupText(-300f, -70f, ModLocalization.Get("settings.wind"), ref windText, ref ___ColorAccessibilityText, SettingsCloneMarker.Role.WindText);
		}
	}

	public static void SetupEventToggle(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText)
	{
		ModLocalization.Initialize();
		SubscribeLocaleChanges();
		RememberSetupRoot(___SubtitlesButton);
		if (eventToggle == null)
		{
			SetupToggle(265f, -275f, ref eventToggle, ref ___SubtitlesButton, OnEventButtonClick, !DISABLE_EVENTS, SettingsCloneMarker.Role.EventToggle);
		}
		if (eventText == null)
		{
			SetupText(-300f, -130f, ModLocalization.Get("settings.events"), ref eventText, ref ___ColorAccessibilityText, SettingsCloneMarker.Role.EventText);
		}
	}

	public static void SetupTCASToggle(ref Button ___SubtitlesButton, ref TMP_Text ___ColorAccessibilityText)
	{
		ModLocalization.Initialize();
		SubscribeLocaleChanges();
		RememberSetupRoot(___SubtitlesButton);
		if (tcasToggle == null)
		{
			SetupToggle(265f, -335f, ref tcasToggle, ref ___SubtitlesButton, OnTCASButtonClick, !DISABLE_TCAS, SettingsCloneMarker.Role.TcasToggle);
		}
		if (tcasText == null)
		{
			SetupText(-300f, -190f, ModLocalization.Get("settings.tcas"), ref tcasText, ref ___ColorAccessibilityText, SettingsCloneMarker.Role.TcasText);
		}
	}

	private static void RememberSetupRoot(Button subtitlesButton)
	{
		if (subtitlesButton != null && subtitlesButton.transform != null)
		{
			setupRoot_ = subtitlesButton.transform.parent == null ? null : subtitlesButton.transform.parent.gameObject;
		}
	}

	/// <summary>
	/// Reconnects the persistent settings clones after a scene round-trip. The
	/// stock OptionsManager survives scene loads, so this operation is
	/// deliberately idempotent and never creates a second set of controls.
	/// </summary>
	internal static bool EnsureSetup(Button subtitlesButton, TMP_Text colorAccessibilityText, Sprite on, Sprite off)
	{
		if (subtitlesButton == null || subtitlesButton.transform == null || subtitlesButton.transform.parent == null)
		{
			return false;
		}
		GameObject root = subtitlesButton.transform.parent.gameObject;
		if (setupRoot_ == null || setupRoot_ != root)
		{
			setupRoot_ = root;
			windToggle = null;
			eventToggle = null;
			tcasToggle = null;
			windText = null;
			eventText = null;
			tcasText = null;
		}
		if (colorAccessibilityText != null)
		{
			fontSource_ = colorAccessibilityText;
		}
		On = on;
		Off = off;
		ReacquireClones();
		if (windToggle == null || windText == null)
		{
			SetupWindToggle(ref subtitlesButton, ref colorAccessibilityText);
		}
		ReacquireClones();
		if (eventToggle == null || eventText == null)
		{
			SetupEventToggle(ref subtitlesButton, ref colorAccessibilityText);
		}
		ReacquireClones();
		if (tcasToggle == null || tcasText == null)
		{
			SetupTCASToggle(ref subtitlesButton, ref colorAccessibilityText);
		}
		ReacquireClones();
		SubscribeLocaleChanges();
		RefreshLocalizedTexts();
		bool ready = IsSetupFor(subtitlesButton);
		if (ready && setupLoggedRoot_ != setupRoot_)
		{
			setupLoggedRoot_ = setupRoot_;
			Plugin.Log?.LogInfo("MiniRealisticAirways settings controls initialized and locale-bound.");
		}
		return ready;
	}

	private static void ReacquireClones()
	{
		if (setupRoot_ == null)
		{
			return;
		}
		SettingsCloneMarker[] markers = setupRoot_.GetComponentsInChildren<SettingsCloneMarker>(includeInactive: true);
		for (int i = 0; i < markers.Length; i++)
		{
			SettingsCloneMarker marker = markers[i];
			if (marker == null)
			{
				continue;
			}
			switch (marker.role_)
			{
				case SettingsCloneMarker.Role.WindToggle:
					windToggle = marker.GetComponent<Button>();
					break;
				case SettingsCloneMarker.Role.EventToggle:
					eventToggle = marker.GetComponent<Button>();
					break;
				case SettingsCloneMarker.Role.TcasToggle:
					tcasToggle = marker.GetComponent<Button>();
					break;
				case SettingsCloneMarker.Role.WindText:
					windText = marker.GetComponent<TMP_Text>();
					break;
				case SettingsCloneMarker.Role.EventText:
					eventText = marker.GetComponent<TMP_Text>();
					break;
				case SettingsCloneMarker.Role.TcasText:
					tcasText = marker.GetComponent<TMP_Text>();
					break;
			}
		}
	}

	/// <summary>Refreshes all cloned settings labels after a locale change.</summary>
	public static void RefreshLocalizedTexts()
	{
		SetLocalizedText(windText, "settings.wind");
		SetLocalizedText(eventText, "settings.events");
		SetLocalizedText(tcasText, "settings.tcas");
	}

	private static void SubscribeLocaleChanges()
	{
		if (localeSubscribed_)
		{
			return;
		}
		ModLocalization.LocaleChanged += OnLocaleChanged;
		localeSubscribed_ = true;
	}

	private static void OnLocaleChanged(string localeCode)
	{
		RefreshLocalizedTexts();
		ScheduleDeferredRefresh();
	}

	private static void ScheduleDeferredRefresh()
	{
		refreshGeneration_++;
		if (deferredRefresh_ != null || windText == null)
		{
			return;
		}
		deferredRefresh_ = windText.StartCoroutine(DeferredRefreshCoroutine());
	}

	private static void StopDeferredRefresh()
	{
		if (deferredRefresh_ != null && windText != null)
		{
			windText.StopCoroutine(deferredRefresh_);
		}
		deferredRefresh_ = null;
	}

	private static IEnumerator DeferredRefreshCoroutine()
	{
		while (true)
		{
			int generation = refreshGeneration_;
			yield return null;
			yield return new WaitForEndOfFrame();
			if (windText == null)
			{
				break;
			}
			if (generation != refreshGeneration_)
			{
				continue;
			}
			RefreshLocalizedTexts();
			if (generation == refreshGeneration_)
			{
				break;
			}
		}
		deferredRefresh_ = null;
	}

	private static void SetLocalizedText(TMP_Text label, string key)
	{
		if (label == null)
		{
			return;
		}
		string value = ModLocalization.Get(key);
		if (fontSource_ != null)
		{
			ChineseTypography.CopyStockTypography(label, fontSource_);
		}
		ApplySettingsLabelTypography(label, value);
	}

	private static void ApplySettingsLabelTypography(TMP_Text label, string value)
	{
		if (label == null)
		{
			return;
		}
		float standardFontSize = ResolveSettingsFontSize();
		// The stock source can keep auto-sizing enabled for long localized
		// labels. Mod labels must use the same intended size in every locale;
		// length is handled by the label's available width, never by shrinking
		// the glyphs.
		label.enableAutoSizing = false;
		label.fontSize = standardFontSize;
		label.fontSizeMin = standardFontSize;
		label.fontSizeMax = standardFontSize;
		ChineseTypography.SetText(label, value, ChineseTypography.StockTextRole.SettingsLabel);
		// CopyStockTypography mirrors the stock source before this method is
		// called. Reassert alignment after the copy and font resolution so locale
		// changes cannot restore the source label's left/center alignment.
		label.horizontalAlignment = HorizontalAlignmentOptions.Right;
		ApplySettingsLabelLayout(label);
	}

	private static void ApplySettingsLabelLayout(TMP_Text label)
	{
		RectTransform rect = label == null ? null : label.rectTransform;
		if (rect == null)
		{
			return;
		}
		SettingsCloneMarker marker = label.GetComponent<SettingsCloneMarker>();
		if (marker == null)
		{
			// All current labels carry a marker.  Keeping this fallback makes the
			// typography path safe for older saved menu objects without silently
			// changing their transform hierarchy.
			label.enableWordWrapping = false;
			label.overflowMode = TextOverflowModes.Overflow;
			return;
		}

		if (!marker.labelLayoutCaptured_)
		{
			float width = Mathf.Max(0.01f, rect.rect.width);
			marker.labelBaseWidth_ = width;
			marker.labelBaseHeight_ = Mathf.Max(0.01f, rect.rect.height);
			marker.labelRightEdge_ = rect.localPosition.x + width * (1f - rect.pivot.x);
			marker.labelPivotY_ = rect.pivot.y;
			marker.labelLayoutCaptured_ = true;
		}

		// A settings option is one semantic row.  Let it grow horizontally while
		// preserving the stock font size; never let TMP create a second line in the
		// 60-unit row spacing.  The right edge remains fixed so the label stays
		// beside its toggle when the width changes.
		label.enableWordWrapping = false;
		label.overflowMode = TextOverflowModes.Overflow;
		try
		{
			label.ForceMeshUpdate(false, true);
			float preferredWidth = label.GetPreferredValues(label.text ?? string.Empty).x;
			float desiredWidth = Mathf.Max(marker.labelBaseWidth_, Mathf.Ceil(preferredWidth + 4f));
			rect.pivot = new Vector2(1f, marker.labelPivotY_);
			rect.sizeDelta = new Vector2(desiredWidth, marker.labelBaseHeight_);
			Vector3 position = rect.localPosition;
			position.x = marker.labelRightEdge_;
			rect.localPosition = position;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Settings label layout deferred until the locale font is ready: " + exception.GetBaseException().Message);
		}
	}

	private static float ResolveSettingsFontSize()
	{
		if (fontSource_ != null)
		{
			float size = fontSource_.enableAutoSizing ? fontSource_.fontSizeMax : fontSource_.fontSize;
			if (size > 0f)
			{
				return size;
			}
		}
		return 20f;
	}

	internal static void ResetSceneState()
	{
		// OptionsManager and its menu children are persistent across map loads.
		// Keep their references and locale subscription alive; only clear them if
		// Unity has actually destroyed the root.
		if (setupRoot_ != null)
		{
			ReacquireClones();
			if (setupRoot_ != null && (windToggle != null || eventToggle != null || tcasToggle != null || windText != null || eventText != null || tcasText != null))
			{
				StopDeferredRefresh();
				SubscribeLocaleChanges();
				refreshGeneration_++;
				return;
			}
		}
		if (localeSubscribed_)
		{
			ModLocalization.LocaleChanged -= OnLocaleChanged;
			localeSubscribed_ = false;
		}
		windToggle = null;
		eventToggle = null;
		tcasToggle = null;
		windText = null;
		eventText = null;
		tcasText = null;
		setupRoot_ = null;
		setupLoggedRoot_ = null;
		fontSource_ = null;
		refreshGeneration_++;
		StopDeferredRefresh();
	}

	internal static bool IsSetupFor(Button subtitlesButton)
	{
		ReacquireClones();
		return subtitlesButton != null && subtitlesButton.transform != null &&
			setupRoot_ != null && subtitlesButton.transform.parent != null &&
			subtitlesButton.transform.parent.gameObject == setupRoot_ &&
			windToggle != null && eventToggle != null && tcasToggle != null &&
			windText != null && eventText != null && tcasText != null;
	}

	public static void OnWindButtonClick()
	{
		OnToggle(ref windToggle, DISABLE_WIND);
		DISABLE_WIND = !DISABLE_WIND;
		Plugin.Log?.LogInfo("Disable wind: " + DISABLE_WIND);
	}

	public static void OnEventButtonClick()
	{
		OnToggle(ref eventToggle, DISABLE_EVENTS);
		DISABLE_EVENTS = !DISABLE_EVENTS;
		Plugin.Log?.LogInfo("Disable events: " + DISABLE_EVENTS);
	}

	public static void OnTCASButtonClick()
	{
		OnToggle(ref tcasToggle, DISABLE_TCAS);
		DISABLE_TCAS = !DISABLE_TCAS;
		Plugin.Log?.LogInfo("Disable TCAS: " + DISABLE_TCAS);
	}

	private static void SetupToggle(float x, float y, ref Button toggle, ref Button ___SubtitlesButton, UnityAction action, bool defaultValue, SettingsCloneMarker.Role role)
	{
		if (___SubtitlesButton == null || ___SubtitlesButton.transform == null)
		{
			toggle = null;
			return;
		}
		toggle = UnityEngine.Object.Instantiate(___SubtitlesButton.gameObject, ___SubtitlesButton.transform.parent).GetComponent<Button>();
		if (toggle == null)
		{
			return;
		}
		StockLocalizationUtility.Detach(toggle.gameObject);
		SettingsCloneMarker.Attach(toggle.gameObject, role);
		toggle.transform.localPosition = new Vector3(x, y, 0f);
		toggle.onClick.RemoveAllListeners();
		toggle.onClick.AddListener(action);
		Image image = toggle.GetComponent<Image>();
		if (image != null)
		{
			image.sprite = (defaultValue ? On : Off);
		}
	}

	private static void SetupText(float x, float y, string text, ref TMP_Text toggleText, ref TMP_Text ___ColorAccessibilityText, SettingsCloneMarker.Role role)
	{
		if (___ColorAccessibilityText == null || ___ColorAccessibilityText.transform == null)
		{
			toggleText = null;
			return;
		}
		toggleText = UnityEngine.Object.Instantiate(___ColorAccessibilityText.gameObject, ___ColorAccessibilityText.transform.parent).GetComponent<TMP_Text>();
		if (toggleText == null)
		{
			return;
		}
		StockLocalizationUtility.Detach(toggleText.gameObject);
		SettingsCloneMarker.Attach(toggleText.gameObject, role);
		if (fontSource_ == null)
		{
			fontSource_ = ___ColorAccessibilityText;
		}
		toggleText.transform.localPosition = new Vector3(x, y, 0f);
		ChineseTypography.CopyStockTypography(toggleText, fontSource_);
		ApplySettingsLabelTypography(toggleText, text);
	}

	private static void OnToggle(ref Button toggle, bool value)
	{
		if (toggle == null)
		{
			return;
		}
		if (value && On != null)
		{
			Image image = toggle.GetComponent<Image>();
			if (image != null)
			{
				image.sprite = On;
			}
		}
		else if (!value && Off != null)
		{
			Image image2 = toggle.GetComponent<Image>();
			if (image2 != null)
			{
				image2.sprite = Off;
			}
		}
	}
}
