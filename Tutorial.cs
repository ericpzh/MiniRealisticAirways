using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UIComponents.Modals;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRealisticAirways;

/// <summary>
/// Owns the mod tutorial and QRH button. All text is resolved through
/// <see cref="ModLocalization"/> so language changes apply to an already-open
/// modal and to the menu without restarting the game.
/// </summary>
public static class Tutorial
{
	public static Button tutorialButton;

	private static ModalWithButton modal;

	private static int tutorialPage;

	private static bool localeSubscribed_;

	private static bool manualSessionActive_;

	private static bool modalManualTrigger_;

	private static QrhButtonLayoutController qrhLayout_;

	private static Button qrhSourceButton_;

	private static readonly List<TutorialPage> tutorialPages = new List<TutorialPage>
	{
		new TutorialPage("tutorial.page.altitude.heading", "tutorial.page.altitude.description"),
		new TutorialPage("tutorial.page.speed.heading", "tutorial.page.speed.description"),
		new TutorialPage("tutorial.page.type.heading", "tutorial.page.type.description"),
		new TutorialPage("tutorial.page.events.heading", "tutorial.page.events.description"),
		new TutorialPage("tutorial.page.wind.heading", "tutorial.page.wind.description"),
		new TutorialPage("tutorial.page.last.heading", "tutorial.page.last.description"),
		new TutorialPage("tutorial.page.thanks.heading", "tutorial.page.thanks.description")
	};

	public static IEnumerator ShowTutorialCoroutine(bool manualTrigger = false)
	{
		if (manualTrigger)
		{
			if (tutorialButton == null || manualSessionActive_ || modal != null)
			{
				yield break;
			}
			manualSessionActive_ = true;
		}
		else
		{
			yield return new WaitForSeconds(1f);
			if (modal != null)
			{
				yield break;
			}
		}

		yield return new WaitUntil(() => ModalManager.Instance != null);
		if (manualTrigger && tutorialButton == null)
		{
			manualSessionActive_ = false;
			yield break;
		}

		tutorialPage = 0;
		ShowModTutorial(manualTrigger);
		if (modal == null)
		{
			manualSessionActive_ = false;
			yield break;
		}

		yield return new WaitUntil(() => modal == null);
		tutorialPage = 0;
		modalManualTrigger_ = false;
		manualSessionActive_ = false;
	}

	/// <summary>Updates the cloned menu button while retaining stock font localization.</summary>
	public static void SetQRHText()
	{
		if (tutorialButton == null)
		{
			return;
		}
		string text = ModLocalization.Get("tutorial.qrh");
		qrhLayout_ = qrhLayout_ == null ? QrhButtonLayoutController.Attach(tutorialButton, qrhSourceButton_) : qrhLayout_;
		qrhLayout_?.SetSource(qrhSourceButton_);
		qrhLayout_?.SetLocalizedText(text);
	}

	internal static void SetQRHSource(Button sourceButton)
	{
		qrhSourceButton_ = sourceButton;
		if (qrhLayout_ != null)
		{
			qrhLayout_.SetSource(sourceButton);
		}
	}

	public static void SubscribeLocaleChanges()
	{
		ModLocalization.Initialize();
		if (localeSubscribed_)
		{
			return;
		}
		ModLocalization.LocaleChanged += OnLocaleChanged;
		localeSubscribed_ = true;
	}

	public static void UnsubscribeLocaleChanges()
	{
		if (localeSubscribed_)
		{
			ModLocalization.LocaleChanged -= OnLocaleChanged;
			localeSubscribed_ = false;
		}
	}

	/// <summary>Clears scene-owned UI references when a menu/map is replaced.</summary>
	internal static void ResetSceneState()
	{
		manualSessionActive_ = false;
		modalManualTrigger_ = false;
		modal = null;
		tutorialButton = null;
		qrhLayout_ = null;
		qrhSourceButton_ = null;
		tutorialPage = 0;
		UnsubscribeLocaleChanges();
	}

	private static void OnLocaleChanged(string localeCode)
	{
		SetQRHText();
		if (Plugin.windsock_ != null)
		{
			Plugin.windsock_.RefreshText();
		}
		RefreshModalLocalization();
	}

	/// <summary>Compatibility helper retained for older callers; English is the default locale.</summary>
	public static bool ShowEnLocale()
	{
		return string.Equals(ModLocalization.CurrentLocaleCode, "en", StringComparison.OrdinalIgnoreCase);
	}

	private static void ShowModTutorial(bool manualTrigger)
	{
		if (tutorialPages.Count == 0)
		{
			return;
		}
		modalManualTrigger_ = manualTrigger;
		modal = ModalManager.NewModalWithButtonStatic(manualTrigger ? "MiniRealisticAirways" + UnityEngine.Random.value : "MiniRealisticAirways");
		if (modal == null)
		{
			return;
		}
		ApplyModalLocalization();
		modal.Show();
		// Show/OnEnable runs the stock modal localizers. The controller owns the
		// four Mod text nodes and performs the authoritative second commit after
		// those callbacks and the first Canvas layout pass.
		ModalTypographyController.Attach(modal)?.ScheduleRefresh(ModLocalization.Revision);
		// ModalWithButton.Show() intentionally does nothing when the stock
		// "don't show again" flag is set. Release that invisible instance so the
		// coroutine cannot hold a stale modal and block the resident QRH button.
		CanvasGroup canvasGroup = modal.GetComponent<CanvasGroup>();
		if (canvasGroup != null && !canvasGroup.interactable)
		{
			UnityEngine.Object.Destroy(modal.gameObject);
			modal = null;
		}
	}

	private static void RefreshModalLocalization()
	{
		if (modal == null)
		{
			return;
		}
		ApplyModalLocalization();
	}

	private static void ApplyModalLocalization()
	{
		if (modal == null || tutorialPages.Count == 0)
		{
			return;
		}
		if (tutorialPage < 0)
		{
			tutorialPage = 0;
		}
		if (tutorialPage >= tutorialPages.Count)
		{
			tutorialPage = tutorialPages.Count - 1;
		}
		TutorialPage page = tutorialPages[tutorialPage];
		modal.SetTitle(ModLocalization.Get("tutorial.title"));
		modal.SetHeading(ModLocalization.Get(page.HeadingKey_));
		modal.SetDescription(ModLocalization.Get(page.DescriptionKey_));
		SetButton(modal, modalManualTrigger_);
		// ArabicTextFormatter supplies visual-order presentation forms and leaves
		// TMP's RTL pass disabled. Alignment is therefore independent: Arabic
		// modal copy is right aligned, while the stock button remains centered.
		modal.SetDescriptionTextAlign(ModLocalization.IsRtl ? TextAlignmentOptions.Right : (IsLastPage() ? TextAlignmentOptions.Center : TextAlignmentOptions.Left));
		DontShowAgainToggle dontShowAgainToggle = modal.GetComponentInChildren<DontShowAgainToggle>();
		if (dontShowAgainToggle != null)
		{
			dontShowAgainToggle.gameObject.SetActive(IsLastPage() && !modalManualTrigger_);
		}
		CloseButton closeButton = modal.GetComponentInChildren<CloseButton>();
		if (closeButton != null)
		{
			closeButton.gameObject.SetActive(IsLastPage() || modalManualTrigger_);
		}
		ApplyModalTypography();
	}

	private static void ApplyModalTypography()
	{
		if (modal == null)
		{
			return;
		}
		ModalTypographyController.Attach(modal)?.ApplyNow();
	}

	private static void SetButton(ModalWithButton currentModal, bool manualTrigger)
	{
		if (currentModal == null || currentModal.button == null || currentModal.description == null)
		{
			return;
		}
		if (IsLastPage())
		{
			currentModal.button.onClick.RemoveAllListeners();
			currentModal.button.gameObject.SetActive(false);
			LinkHandler linkHandler = currentModal.description.GetComponent<LinkHandler>();
			if (linkHandler == null)
			{
				linkHandler = currentModal.description.gameObject.AddComponent<LinkHandler>();
			}
			linkHandler.url = ModLocalization.Get("tutorial.docsUrl");
			return;
		}

		currentModal.button.gameObject.SetActive(true);
		// SetButtonOnClick appends to UnityEvent. Clear the previous page/locale
		// listener so changing language while the modal is open cannot advance two
		// pages from one click.
		currentModal.button.onClick.RemoveAllListeners();
		currentModal.SetButtonText(ModLocalization.Get("tutorial.next"));
		currentModal.SetButtonOnClick(delegate
		{
			tutorialPage = Math.Min(tutorialPages.Count - 1, tutorialPage + 1);
			currentModal.PostHide();
			ShowModTutorial(manualTrigger);
		});
	}

	private static bool IsLastPage()
	{
		return tutorialPage >= tutorialPages.Count - 1;
	}
}
