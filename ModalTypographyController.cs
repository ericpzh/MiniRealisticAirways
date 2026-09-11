using System.Collections;
using TMPro;
using UIComponents.Modals;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// Owns the four Mod text nodes inside a tutorial modal. It applies the
/// final typography after ModalWithButton.Show has enabled its hierarchy and
/// adapts only the title line spacing when a long locale wraps to two lines.
/// </summary>
internal sealed class ModalTypographyController : MonoBehaviour
{
	private ModalWithButton modal_;
	private TMP_Text title_;
	private TMP_Text heading_;
	private TMP_Text description_;
	private TMP_Text buttonLabel_;
	private float titleBaseLineSpacing_;
	private HorizontalAlignmentOptions titleBaseAlignment_;
	private HorizontalAlignmentOptions headingBaseAlignment_;
	private HorizontalAlignmentOptions buttonBaseAlignment_;
	private bool initialized_;
	private bool titleBaselineCaptured_;
	private bool alignmentBaselineCaptured_;
	private Coroutine refreshCoroutine_;
	private int refreshGeneration_;
	private int lastTitleAppliedRevision_ = -1;
	private string lastTitleAppliedText_;

	// r11 used half of the stock baseline distance. r12 makes that displayed
	// spacing about ten percent more open while leaving single-line titles alone.
	private const float WrappedTitleGapFactor = 0.55f;
	private const float WrappedTitleSpacingProbe = 1f;
	private const float WrappedTitleSpacingLimit = 128f;

	internal static ModalTypographyController Attach(ModalWithButton modal)
	{
		if (modal == null || modal.gameObject == null)
		{
			return null;
		}
		ModalTypographyController controller = modal.GetComponent<ModalTypographyController>();
		if (controller == null)
		{
			controller = modal.gameObject.AddComponent<ModalTypographyController>();
		}
		controller.Initialize(modal);
		return controller;
	}

	internal void ApplyNow()
	{
		Initialize(modal_);
		if (!initialized_)
		{
			return;
		}
		StockLocalizationUtility.TakeOwnership(title_);
		StockLocalizationUtility.TakeLayoutOwnership(title_);
		StockLocalizationUtility.TakeOwnership(heading_);
		StockLocalizationUtility.TakeOwnership(description_);
		StockLocalizationUtility.TakeOwnership(buttonLabel_);
		ChineseTypography.Apply(title_, ChineseTypography.StockTextRole.ModalTitle);
		ChineseTypography.Apply(heading_, ChineseTypography.StockTextRole.ModalHeading);
		ChineseTypography.Apply(description_, ChineseTypography.StockTextRole.ModalDescription);
		ChineseTypography.Apply(buttonLabel_, ChineseTypography.StockTextRole.ModalButton);
		ApplyLocaleAlignment();
		ApplyTitleLineSpacing();
	}

	private void ApplyLocaleAlignment()
	{
		if (ModLocalization.IsRtl)
		{
			if (title_ != null)
			{
				title_.horizontalAlignment = HorizontalAlignmentOptions.Right;
			}
			if (heading_ != null)
			{
				heading_.horizontalAlignment = HorizontalAlignmentOptions.Right;
			}
			if (description_ != null)
			{
				description_.horizontalAlignment = HorizontalAlignmentOptions.Right;
			}
			if (buttonLabel_ != null)
			{
				buttonLabel_.horizontalAlignment = HorizontalAlignmentOptions.Center;
			}
			return;
		}

		// Tutorial.ApplyModalLocalization has already restored the page-specific
		// description alignment (left/center). Restore the other nodes captured
		// from the stock modal so switching back from Arabic cannot leave the title
		// or button stuck on the previous locale's alignment.
		if (alignmentBaselineCaptured_)
		{
			if (title_ != null)
			{
				title_.horizontalAlignment = titleBaseAlignment_;
			}
			if (heading_ != null)
			{
				heading_.horizontalAlignment = headingBaseAlignment_;
			}
			if (buttonLabel_ != null)
			{
				buttonLabel_.horizontalAlignment = buttonBaseAlignment_;
			}
		}
	}

	internal void ScheduleRefresh(int revision)
	{
		Initialize(modal_);
		refreshGeneration_++;
		if (refreshCoroutine_ == null && isActiveAndEnabled)
		{
			refreshCoroutine_ = StartCoroutine(RefreshCoroutine(revision));
		}
	}

	private void Initialize(ModalWithButton modal)
	{
		if (initialized_ || modal == null)
		{
			return;
		}
		modal_ = modal;
		title_ = modal.title;
		heading_ = modal.heading;
		description_ = modal.description;
		buttonLabel_ = modal.button == null ? null : modal.button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (title_ != null && !titleBaselineCaptured_)
		{
			titleBaseLineSpacing_ = title_.lineSpacing;
			titleBaselineCaptured_ = true;
		}
		if (!alignmentBaselineCaptured_)
		{
			titleBaseAlignment_ = title_ == null ? HorizontalAlignmentOptions.Left : title_.horizontalAlignment;
			headingBaseAlignment_ = heading_ == null ? HorizontalAlignmentOptions.Left : heading_.horizontalAlignment;
			buttonBaseAlignment_ = buttonLabel_ == null ? HorizontalAlignmentOptions.Center : buttonLabel_.horizontalAlignment;
			alignmentBaselineCaptured_ = true;
		}
		StockLocalizationUtility.TakeOwnership(title_);
		StockLocalizationUtility.TakeLayoutOwnership(title_);
		StockLocalizationUtility.TakeOwnership(heading_);
		StockLocalizationUtility.TakeOwnership(description_);
		StockLocalizationUtility.TakeOwnership(buttonLabel_);
		initialized_ = title_ != null || heading_ != null || description_ != null || buttonLabel_ != null;
	}

	private IEnumerator RefreshCoroutine(int revision)
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
			Canvas.ForceUpdateCanvases();
			yield return null;
			Canvas.ForceUpdateCanvases();
			// The stock modal can rebuild its layout one frame after Show(). Run
			// the measured pass again after that rebuild so the final wrapped title
			// gap, rather than a pre-layout estimate, is authoritative.
			ApplyLocaleAlignment();
			ApplyTitleLineSpacing();
			Canvas.ForceUpdateCanvases();
			if (generation == refreshGeneration_)
			{
				break;
			}
		}
		refreshCoroutine_ = null;
	}

	private void ApplyTitleLineSpacing()
	{
		if (title_ == null || !titleBaselineCaptured_)
		{
			return;
		}
		try
		{
			title_.lineSpacing = titleBaseLineSpacing_;
			title_.ForceMeshUpdate(false, true);
			if (!TryMeasureTitleBaselineDistance(out float baseDistance, out int lineCount) || lineCount <= 1)
			{
				return;
			}

			// Use the final rendered baseline distance directly. The previous
			// implementation clamped this target to glyph ascender/descender height;
			// for many official fonts that floor was already as large as the original
			// distance, so the requested 50% reduction became a no-op.
			float targetDistance = Mathf.Max(0.01f, baseDistance * WrappedTitleGapFactor);
			float spacing = titleBaseLineSpacing_;
			for (int iteration = 0; iteration < 4; iteration++)
			{
				if (!TryMeasureTitleBaselineDistance(out float currentDistance, out _))
				{
					break;
				}
				float error = targetDistance - currentDistance;
				if (Mathf.Abs(error) < 0.01f)
				{
					break;
				}

				float probeSpacing = spacing - WrappedTitleSpacingProbe;
				title_.lineSpacing = probeSpacing;
				title_.ForceMeshUpdate(false, true);
				if (!TryMeasureTitleBaselineDistance(out float probeDistance, out _))
				{
					break;
				}
				float slope = (probeDistance - currentDistance) / (probeSpacing - spacing);
				if (Mathf.Abs(slope) < 0.0001f)
				{
					// A few unusual TMP assets quantize line spacing. Fall back to a
					// bounded one-font-size tightening rather than spinning or writing
					// the property every frame.
					spacing = titleBaseLineSpacing_ - Mathf.Min(WrappedTitleSpacingLimit, Mathf.Max(1f, title_.fontSize));
					break;
				}
				spacing += error / slope;
				spacing = Mathf.Clamp(spacing, titleBaseLineSpacing_ - WrappedTitleSpacingLimit, titleBaseLineSpacing_ + WrappedTitleSpacingLimit);
				title_.lineSpacing = spacing;
				title_.ForceMeshUpdate(false, true);
			}
			// Do not mark the parent for another rebuild here.  The parent layout has
			// already settled; requesting another pass can restore the stock TMP
			// spacing immediately after this measured correction.
			title_.ForceMeshUpdate(false, true);
			if (lastTitleAppliedRevision_ != ModLocalization.Revision || !string.Equals(lastTitleAppliedText_, title_.text, System.StringComparison.Ordinal))
			{
				lastTitleAppliedRevision_ = ModLocalization.Revision;
				lastTitleAppliedText_ = title_.text;
				Plugin.Log?.LogInfo("QRH title spacing applied: locale=" + ModLocalization.CurrentLocaleCode + ", node=" + title_.name + ", lines=" + lineCount + ", baseline=" + baseDistance.ToString("0.###") + ", target=" + targetDistance.ToString("0.###") + ", lineSpacing=" + title_.lineSpacing.ToString("0.###"));
			}
		}
		catch (System.Exception exception)
		{
			Plugin.Log?.LogDebug("Modal title line spacing update deferred: " + exception.GetBaseException().Message);
		}
	}

	private bool TryMeasureTitleBaselineDistance(out float distance, out int lineCount)
	{
		distance = 0f;
		lineCount = title_ == null || title_.textInfo == null ? 0 : title_.textInfo.lineCount;
		if (title_ == null || title_.textInfo == null || lineCount < 2)
		{
			return lineCount > 0;
		}
		TMP_LineInfo first = title_.textInfo.lineInfo[0];
		TMP_LineInfo second = title_.textInfo.lineInfo[1];
		distance = Mathf.Abs(first.baseline - second.baseline);
		return true;
	}

	private void OnDestroy()
	{
		refreshGeneration_++;
		refreshCoroutine_ = null;
		modal_ = null;
		title_ = null;
		heading_ = null;
		description_ = null;
		buttonLabel_ = null;
	}
}
