using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRealisticAirways;

/// <summary>Retains stock button metrics and grows only its width for localized text.</summary>
internal sealed class QrhButtonLayoutController : MonoBehaviour
{
	private RectTransform buttonRect_;
	private TMP_Text label_;
	private TMP_Text sourceLabel_;
	private float baseWidth_;
	private float baseHeight_;
	private float horizontalPadding_;
	private float fontSize_;
	private float fontSizeMin_;
	private float fontSizeMax_;
	private float characterSpacing_;
	private float wordSpacing_;
	private float lineSpacing_;
	private float paragraphSpacing_;
	private bool initialized_;
	private string requestedText_ = string.Empty;
	private Coroutine deferredRefresh_;
	private int refreshGeneration_;
	private bool refreshPending_;

	internal static QrhButtonLayoutController Attach(Button button, Button sourceButton = null)
	{
		if (button == null)
		{
			return null;
		}
		QrhButtonLayoutController controller = button.GetComponent<QrhButtonLayoutController>();
		if (controller == null)
		{
			controller = button.gameObject.AddComponent<QrhButtonLayoutController>();
		}
		controller.SetSource(sourceButton);
		controller.Initialize();
		return controller;
	}

	internal void SetLocalizedText(string text)
	{
		Initialize();
		if (label_ == null || buttonRect_ == null)
		{
			return;
		}
		requestedText_ = text ?? string.Empty;
		ApplyNow();
		ScheduleDeferredRefresh();
	}

	internal void SetSource(Button sourceButton)
	{
		TMP_Text sourceLabel = sourceButton == null ? null : sourceButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (sourceLabel != null)
		{
			sourceLabel_ = sourceLabel;
			// The stock Editor button is localized by the game. Its width and
			// preferred text metrics can therefore change after this controller has
			// initialized; keep the QRH baseline in sync with that live source.
			if (initialized_)
			{
				RefreshSourceMetrics();
			}
		}
	}

	private void ApplyNow()
	{
		Initialize();
		if (label_ == null || buttonRect_ == null)
		{
			return;
		}
		RefreshSourceMetrics();
		if (sourceLabel_ != null)
		{
			ChineseTypography.CopyStockTypography(label_, sourceLabel_);
			// The stock label may have auto-sized itself for a long locale. QRH
			// grows horizontally, so keep the source's intended maximum glyph size
			// instead of inheriting that temporary shrink.
			float standardFontSize = sourceLabel_.enableAutoSizing ? sourceLabel_.fontSizeMax : sourceLabel_.fontSize;
			if (standardFontSize > 0f)
			{
				label_.fontSize = standardFontSize;
				label_.fontSizeMin = standardFontSize;
				label_.fontSizeMax = standardFontSize;
			}
		}
		else
		{
			label_.fontSize = fontSize_;
			label_.fontSizeMin = fontSizeMin_;
			label_.fontSizeMax = fontSizeMax_;
			label_.characterSpacing = characterSpacing_;
			label_.wordSpacing = wordSpacing_;
			label_.lineSpacing = lineSpacing_;
			label_.paragraphSpacing = paragraphSpacing_;
		}
		// The source button can auto-size its own text. QRH uses the same maximum
		// size and spacing, but grows horizontally instead of shrinking the glyphs.
		label_.enableAutoSizing = false;
		label_.enableWordWrapping = false;
		label_.overflowMode = TextOverflowModes.Overflow;
		ChineseTypography.SetText(label_, requestedText_, ChineseTypography.StockTextRole.QrhButton);
		RefreshWidth();
	}

	private void ScheduleDeferredRefresh()
	{
		refreshGeneration_++;
		refreshPending_ = true;
		if (deferredRefresh_ != null || !isActiveAndEnabled)
		{
			return;
		}
		deferredRefresh_ = StartCoroutine(DeferredRefreshCoroutine());
	}

	private IEnumerator DeferredRefreshCoroutine()
	{
		while (true)
		{
			int generation = refreshGeneration_;
			yield return null;
			yield return new WaitForEndOfFrame();
			if (this == null || label_ == null)
			{
				break;
			}
			if (generation != refreshGeneration_)
			{
				continue;
			}
			ApplyNow();
			if (generation == refreshGeneration_)
			{
				refreshPending_ = false;
				break;
			}
		}
		deferredRefresh_ = null;
	}

	internal void RefreshWidth()
	{
		if (!initialized_ || label_ == null || buttonRect_ == null)
		{
			return;
		}
		try
		{
			RefreshSourceMetrics();
			buttonRect_.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseWidth_);
			buttonRect_.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseHeight_);
			label_.ForceMeshUpdate(false, true);
			float preferredWidth = label_.GetPreferredValues(label_.text).x;
			float desiredWidth = Mathf.Max(baseWidth_, Mathf.Ceil(preferredWidth + horizontalPadding_));
			buttonRect_.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredWidth);
			buttonRect_.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseHeight_);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("QRH width measurement deferred until the stock locale font is ready: " + exception.GetBaseException().Message);
		}
	}

	private void OnEnable()
	{
		if (refreshPending_ && deferredRefresh_ == null)
		{
			ScheduleDeferredRefresh();
		}
	}

	private void RefreshSourceMetrics()
	{
		if (sourceLabel_ == null || sourceLabel_.rectTransform == null)
		{
			return;
		}
		try
		{
			Button sourceButton = sourceLabel_.GetComponentInParent<Button>();
			RectTransform sourceButtonRect = sourceButton == null ? null : sourceButton.transform as RectTransform;
			if (sourceButtonRect == null || sourceButtonRect.rect.width <= 0f || sourceButtonRect.rect.height <= 0f)
			{
				return;
			}
			float sourceWidth = sourceButtonRect.rect.width;
			float sourceHeight = sourceButtonRect.rect.height;
			sourceLabel_.ForceMeshUpdate(false, true);
			float sourcePreferredWidth = sourceLabel_.GetPreferredValues(sourceLabel_.text).x;
			if (sourcePreferredWidth <= 0f)
			{
				return;
			}
			baseWidth_ = sourceWidth;
			baseHeight_ = sourceHeight;
			// Preserve the stock button's horizontal padding for every locale. A
			// long QRH label grows from that current stock width; a shorter label
			// can never make the button smaller than the stock baseline.
			horizontalPadding_ = Mathf.Max(24f, sourceWidth - sourcePreferredWidth);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("QRH source metrics deferred until stock localization finishes: " + exception.GetBaseException().Message);
		}
	}

	private void Initialize()
	{
		if (initialized_)
		{
			return;
		}
		buttonRect_ = transform as RectTransform;
		label_ = GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (buttonRect_ == null || label_ == null)
		{
			return;
		}
		baseWidth_ = buttonRect_.rect.width;
		baseHeight_ = buttonRect_.rect.height;
		if (sourceLabel_ != null && sourceLabel_.rectTransform != null)
		{
			Button sourceButton = sourceLabel_.GetComponentInParent<Button>();
			RectTransform sourceButtonRect = sourceButton == null ? null : sourceButton.transform as RectTransform;
			if (sourceButtonRect != null)
			{
				baseWidth_ = sourceButtonRect.rect.width;
				baseHeight_ = sourceButtonRect.rect.height;
			}
		}
		fontSize_ = label_.fontSize;
		fontSizeMin_ = label_.fontSizeMin;
		fontSizeMax_ = label_.fontSizeMax;
		characterSpacing_ = label_.characterSpacing;
		wordSpacing_ = label_.wordSpacing;
		lineSpacing_ = label_.lineSpacing;
		paragraphSpacing_ = label_.paragraphSpacing;
		initialized_ = true;
		// Prefer the live stock Editor button as the baseline. If it has not
		// completed its first localization pass yet, the cloned button metrics
		// above remain a safe fallback and are replaced on the deferred refresh.
		RefreshSourceMetrics();
		if (horizontalPadding_ <= 0f)
		{
			float sourcePreferredWidth = label_.GetPreferredValues(label_.text).x;
			horizontalPadding_ = Mathf.Max(24f, baseWidth_ - sourcePreferredWidth);
		}
	}

	private void OnDestroy()
	{
		refreshGeneration_++;
		deferredRefresh_ = null;
		refreshPending_ = false;
		sourceLabel_ = null;
		label_ = null;
		buttonRect_ = null;
	}
}
