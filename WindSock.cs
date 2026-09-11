using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MiniRealisticAirways;

internal partial class WindSock : MonoBehaviour
{
	public GameObject windsock_;

	public float windDirection_ = 0f;

	private GameObject textGameObject_;

	private TMP_Text text_;

	private Camera mainCamera_;

	private RectTransform windsockRectTransform_;

	private Image windsockImage_;

	private Canvas arrowCanvas_;

	private Image cachedArrowImage_;

	private Sprite cachedArrowSprite_;

	private Rect cachedArrowRect_;

	private bool cachedArrowPreserveAspect_;

	private bool cachedArrowUseSpriteMesh_;

	private Vector2[] cachedArrowVisiblePoints_;

	private bool arrowGeometryReady_;

	private ArrowGeometrySource arrowGeometrySource_;

	private string displayedText_;

	private int displayedWindDegree_ = int.MinValue;

	private bool displayedWindDisabled_;

	private int displayedLocaleRevision_ = -1;

	private readonly Vector3[] windArrowCorners_ = new Vector3[4];

	private Renderer textRenderer_;

	private Renderer windsockRenderer_;

	private float visibleTextMinLocalX_;

	private float visibleTextMaxLocalX_;

	private float visibleTextMinLocalY_;

	private float visibleTextMaxLocalY_;

	private float referenceCharacterWidth_;

	private bool visibleTextBoundsReady_;

	private int layoutLocaleRevision_ = -1;

	private bool windGeometryDiagnosticLogged_;

	private bool windGeometryMissingLogged_;

	private enum ArrowGeometrySource
	{
		None,
		SpritePhysicsShape,
		SpriteVertices,
		ImageRect,
		RendererBounds,
		RectTransform
	}

	// Keep the label just outside the visible arrow silhouette. The old code used
	// the full ESC_Button RectTransform, which includes transparent UI padding.
	// The visible edge is now measured from the cached Sprite shape below. One
	// full current-font character is the requested visual separation.
	private const float WindTextArrowDistanceFactor = 1f;

	// This is deliberately expressed in screen pixels. The arrow lives in a
	// Canvas while the label is a world-space TextMeshPro object, so a world-unit
	// gap cannot be compared between them reliably.
	private const float WindTextArrowOpticalGapPixels = 1f;

	private const float WindTextVerticalAlignmentTolerancePixels = 0.25f;

	private const float WindTextBaselineX = 0.75f;

	private const float WindTextBaselineY = 0.3f;

	private void ResolveArrowVisual()
	{
		if (windsock_ == null)
		{
			return;
		}
		Image selectedImage = null;
		int selectedImageScore = int.MinValue;
		Image[] images = windsock_.GetComponentsInChildren<Image>(includeInactive: true);
		for (int i = 0; i < images.Length; i++)
		{
			Image candidate = images[i];
			if (candidate == null)
			{
				continue;
			}
			int score = 0;
			if (candidate.sprite != null)
			{
				score += 100;
			}
			if (candidate.gameObject == windsock_)
			{
				score += 50;
			}
			string objectName = candidate.gameObject.name ?? string.Empty;
			if (objectName.IndexOf("arrow", StringComparison.OrdinalIgnoreCase) >= 0 || objectName.IndexOf("wind", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				score += 25;
			}
			if (candidate.enabled)
			{
				score += 10;
			}
			if (score > selectedImageScore)
			{
				selectedImage = candidate;
				selectedImageScore = score;
			}
		}
		windsockImage_ = selectedImage;
		WindArrowVisual.Apply(windsockImage_);
		arrowCanvas_ = windsockImage_ == null ? null : windsockImage_.canvas;
		if (arrowCanvas_ == null && windsockImage_ != null)
		{
			arrowCanvas_ = windsockImage_.GetComponentInParent<Canvas>();
		}

		Renderer selectedRenderer = null;
		int selectedRendererScore = int.MinValue;
		Renderer[] renderers = windsock_.GetComponentsInChildren<Renderer>(includeInactive: true);
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer candidate = renderers[i];
			if (candidate == null || candidate.gameObject == textGameObject_)
			{
				continue;
			}
			int score = candidate is SpriteRenderer ? 100 : 0;
			string objectName = candidate.gameObject.name ?? string.Empty;
			if (objectName.IndexOf("arrow", StringComparison.OrdinalIgnoreCase) >= 0 || objectName.IndexOf("wind", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				score += 50;
			}
			if (candidate.gameObject == windsock_)
			{
				score += 25;
			}
			if (candidate.enabled)
			{
				score += 10;
			}
			if (score > selectedRendererScore)
			{
				selectedRenderer = candidate;
				selectedRendererScore = score;
			}
		}
		windsockRenderer_ = selectedRenderer;
		cachedArrowImage_ = null;
		cachedArrowSprite_ = null;
		cachedArrowRect_ = default(Rect);
		cachedArrowVisiblePoints_ = null;
		arrowGeometryReady_ = false;
		arrowGeometrySource_ = ArrowGeometrySource.None;
	}

	private bool EnsureArrowGeometryCache()
	{
		if (windsockImage_ == null || windsockImage_.rectTransform == null || windsockImage_.sprite == null)
		{
			return false;
		}
		RectTransform rectTransform = windsockImage_.rectTransform;
		Sprite sprite = windsockImage_.sprite;
		Rect currentRect = rectTransform.rect;
		if (arrowGeometryReady_ && cachedArrowImage_ == windsockImage_ && cachedArrowSprite_ == sprite && cachedArrowRect_.x == currentRect.x && cachedArrowRect_.y == currentRect.y && cachedArrowRect_.width == currentRect.width && cachedArrowRect_.height == currentRect.height && cachedArrowPreserveAspect_ == windsockImage_.preserveAspect && cachedArrowUseSpriteMesh_ == windsockImage_.useSpriteMesh)
		{
			return cachedArrowVisiblePoints_ != null && cachedArrowVisiblePoints_.Length > 0;
		}

		cachedArrowImage_ = windsockImage_;
		cachedArrowSprite_ = sprite;
		cachedArrowRect_ = currentRect;
		cachedArrowPreserveAspect_ = windsockImage_.preserveAspect;
		cachedArrowUseSpriteMesh_ = windsockImage_.useSpriteMesh;
		cachedArrowVisiblePoints_ = null;
		arrowGeometryReady_ = true;
		arrowGeometrySource_ = ArrowGeometrySource.None;

		try
		{
			List<Vector2> sourcePoints = new List<Vector2>(16);
			int physicsShapeCount = sprite.GetPhysicsShapeCount();
			for (int shapeIndex = 0; shapeIndex < physicsShapeCount; shapeIndex++)
			{
				List<Vector2> shape = new List<Vector2>(16);
				sprite.GetPhysicsShape(shapeIndex, shape);
				for (int pointIndex = 0; pointIndex < shape.Count; pointIndex++)
				{
					sourcePoints.Add(shape[pointIndex]);
				}
			}
			if (sourcePoints.Count > 0)
			{
				arrowGeometrySource_ = ArrowGeometrySource.SpritePhysicsShape;
			}
			else
			{
				Vector2[] vertices = sprite.vertices;
				if (vertices != null)
				{
					for (int i = 0; i < vertices.Length; i++)
					{
						sourcePoints.Add(vertices[i]);
					}
				}
				if (sourcePoints.Count > 0)
				{
					arrowGeometrySource_ = ArrowGeometrySource.SpriteVertices;
				}
			}

			Bounds spriteBounds = sprite.bounds;
			Vector2 spriteMin = spriteBounds.min;
			Vector2 spriteSize = spriteBounds.size;
			Rect drawingRect = GetImageDrawingRect(windsockImage_, sprite, currentRect);
			if (sourcePoints.Count == 0 || spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f)
			{
				sourcePoints.Clear();
				sourcePoints.Add(new Vector2(drawingRect.xMin, drawingRect.yMin));
				sourcePoints.Add(new Vector2(drawingRect.xMin, drawingRect.yMax));
				sourcePoints.Add(new Vector2(drawingRect.xMax, drawingRect.yMin));
				sourcePoints.Add(new Vector2(drawingRect.xMax, drawingRect.yMax));
				cachedArrowVisiblePoints_ = sourcePoints.ToArray();
				arrowGeometrySource_ = ArrowGeometrySource.ImageRect;
				return true;
			}

			Vector2[] localPoints = new Vector2[sourcePoints.Count];
			for (int i = 0; i < sourcePoints.Count; i++)
			{
				Vector2 normalized = new Vector2((sourcePoints[i].x - spriteMin.x) / spriteSize.x, (sourcePoints[i].y - spriteMin.y) / spriteSize.y);
				localPoints[i] = new Vector2(drawingRect.xMin + normalized.x * drawingRect.width, drawingRect.yMin + normalized.y * drawingRect.height);
			}
			cachedArrowVisiblePoints_ = localPoints;
			return true;
		}
		catch (Exception exception)
		{
			arrowGeometryReady_ = false;
			cachedArrowVisiblePoints_ = null;
			arrowGeometrySource_ = ArrowGeometrySource.None;
			Plugin.Log?.LogDebug("Wind arrow Sprite geometry was unavailable: " + exception.GetBaseException().Message);
			return false;
		}
	}

	private static Rect GetImageDrawingRect(Image image, Sprite sprite, Rect rect)
	{
		if (image == null || sprite == null || !image.preserveAspect || rect.width <= 0f || rect.height <= 0f || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
		{
			return rect;
		}
		float spriteAspect = sprite.rect.width / sprite.rect.height;
		float rectAspect = rect.width / rect.height;
		if (rectAspect > spriteAspect)
		{
			float width = rect.height * spriteAspect;
			return new Rect(rect.xMin + (rect.width - width) * 0.5f, rect.yMin, width, rect.height);
		}
		float height = rect.width / spriteAspect;
		return new Rect(rect.xMin, rect.yMin + (rect.height - height) * 0.5f, rect.width, height);
	}

	public override string ToString()
	{
		if (Settings.DISABLE_WIND)
		{
			return "";
		}
		return ModLocalization.Format("hud.wind", (int)Math.Round(windDirection_));
	}

	/// <summary>Invalidates the cached label after a language or settings change.</summary>
	public void RefreshText()
	{
		displayedText_ = null;
		displayedWindDegree_ = int.MinValue;
		displayedLocaleRevision_ = -1;
		layoutLocaleRevision_ = -1;
		visibleTextBoundsReady_ = false;
		visibleTextMinLocalX_ = 0f;
		visibleTextMaxLocalX_ = 0f;
		visibleTextMinLocalY_ = 0f;
		visibleTextMaxLocalY_ = 0f;
		referenceCharacterWidth_ = 0f;
		windGeometryDiagnosticLogged_ = false;
		windGeometryMissingLogged_ = false;
	}

	public void InitializeText()
	{
		if (!(windsock_ == null))
		{
			if (textGameObject_ != null)
			{
				return;
			}
			textGameObject_ = new GameObject("Text");
			text_ = textGameObject_.AddComponent<TextMeshPro>();
			text_.fontSize = 4f;
			text_.enableAutoSizing = false;
			text_.enableWordWrapping = false;
			text_.overflowMode = TextOverflowModes.Overflow;
			text_.horizontalAlignment = ModLocalization.IsRtl ? HorizontalAlignmentOptions.Right : HorizontalAlignmentOptions.Left;
			text_.verticalAlignment = VerticalAlignmentOptions.Top;
			text_.rectTransform.sizeDelta = new Vector2(2f, 1f);
			// The label always grows away from the arrow. RTL is expressed by the
			// glyph alignment/order inside this box, not by moving the box's pivot to
			// the arrow-facing side.
				text_.rectTransform.pivot = new Vector2(0f, text_.rectTransform.pivot.y);
				SortingGroup sortingGroup = textGameObject_.AddComponent<SortingGroup>();
				sortingGroup.sortingLayerName = "Text";
			sortingGroup.sortingOrder = 1;
			textRenderer_ = textGameObject_.GetComponent<Renderer>();
			windsockRectTransform_ = windsock_.GetComponent<RectTransform>();
			ResolveArrowVisual();
		}
	}

	private void Start()
	{
		if (!Settings.DISABLE_WIND)
		{
			windDirection_ = UnityEngine.Random.value * 360f;
			mainCamera_ = Camera.main;
			windsockRectTransform_ = windsock_ == null ? null : windsock_.GetComponent<RectTransform>();
			if (windsock_ != null && (windsockImage_ == null || arrowCanvas_ == null) && windsockRenderer_ == null)
			{
				ResolveArrowVisual();
			}
			if (windsockRectTransform_ != null)
			{
				windsockRectTransform_.pivot = new Vector2(0.5f, 0.5f);
			}
			// 仅在启动时确保一次可见；之后尊重游戏或其他 mod 对该按钮的隐藏意图。
			if (windsock_ != null && !windsock_.activeSelf)
			{
				Plugin.Log?.LogWarning("windsock_ isn't active at startup, activating it once.");
				windsock_.SetActive(value: true);
			}
			StartCoroutine(UpdateWindCoroutine());
		}
	}

	private void Update()
	{
		// Tab 文本显隐由 TextVisibilityHotkey 负责，与风向组件解耦。
		if (Time.timeScale != 0f && !(text_ == null) && !(textGameObject_ == null))
		{
			if (!Plugin.showText_)
			{
				if (text_.text.Length != 0)
				{
					ChineseTypography.SetText(text_, "");
				}
				displayedText_ = null;
				displayedWindDegree_ = int.MinValue;
				return;
			}
			if (mainCamera_ == null)
			{
				mainCamera_ = Camera.main;
			}
			if (mainCamera_ == null)
			{
				return;
			}
			int windDegree = (int)Math.Round(windDirection_);
			if (windDegree != displayedWindDegree_ || displayedWindDisabled_ != Settings.DISABLE_WIND || displayedLocaleRevision_ != ModLocalization.Revision || displayedText_ == null)
			{
				string currentText = ToString();
				displayedText_ = currentText;
				displayedWindDegree_ = windDegree;
				displayedWindDisabled_ = Settings.DISABLE_WIND;
				displayedLocaleRevision_ = ModLocalization.Revision;
				ChineseTypography.SetWindText(text_, currentText, "Wind HUD");
				ApplySingleLineLayout();
			}
		}
	}

	private bool windDirectionFailed_;

	private void LateUpdate()
	{
		if (Settings.DISABLE_WIND) return;
		if (mainCamera_ == null) mainCamera_ = Camera.main;
		// 暂停只冻结模拟与数值；视图仍跟随相机和布局变化，避免箭头与文字错位。
		bool directionReady = WindArrowVisual.SetWindDirection(windsockImage_, mainCamera_, windDirection_);
		if (!directionReady && !windDirectionFailed_) Plugin.Log?.LogDebug("Wind direction unavailable: check Image, parent RectTransform and cameras.");
		windDirectionFailed_ = !directionReady;
		if (Plugin.showText_ && text_ != null && textGameObject_ != null)
		{
			UpdateTextPosition();
		}
	}

	private void ApplySingleLineLayout()
	{
		if (text_ == null || text_.rectTransform == null)
		{
			return;
		}
		text_.enableAutoSizing = false;
		text_.enableWordWrapping = false;
		text_.overflowMode = TextOverflowModes.Overflow;
		text_.horizontalAlignment = ModLocalization.IsRtl ? HorizontalAlignmentOptions.Right : HorizontalAlignmentOptions.Left;
		text_.rectTransform.pivot = new Vector2(0f, text_.rectTransform.pivot.y);
		try
		{
			text_.ForceMeshUpdate(false, true);
			float preferredWidth = text_.GetPreferredValues(text_.text ?? string.Empty).x;
			text_.rectTransform.sizeDelta = new Vector2(Mathf.Max(2f, Mathf.Ceil(preferredWidth + 0.1f)), 1f);
			text_.ForceMeshUpdate(false, true);
			visibleTextBoundsReady_ = TryMeasureVisibleTextBounds(out visibleTextMinLocalX_, out visibleTextMaxLocalX_, out visibleTextMinLocalY_, out visibleTextMaxLocalY_);
			referenceCharacterWidth_ = MeasureReferenceCharacterWidth();
			layoutLocaleRevision_ = ModLocalization.Revision;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Wind HUD layout deferred until the locale font is ready: " + exception.GetBaseException().Message);
		}
	}

	private bool TryMeasureVisibleTextBounds(out float minimumX, out float maximumX, out float minimumY, out float maximumY)
	{
		minimumX = 0f;
		maximumX = 0f;
		minimumY = 0f;
		maximumY = 0f;
		if (text_ == null)
		{
			return false;
		}
		try
		{
			if (text_.textInfo == null || text_.textInfo.characterInfo == null)
			{
				return false;
			}
			bool found = false;
			for (int i = 0; i < text_.textInfo.characterCount; i++)
			{
				TMP_CharacterInfo character = text_.textInfo.characterInfo[i];
				if (!character.isVisible)
				{
					continue;
				}
				TryUpdateLocalBounds(character.bottomLeft, ref minimumX, ref maximumX, ref minimumY, ref maximumY, ref found);
				TryUpdateLocalBounds(character.topLeft, ref minimumX, ref maximumX, ref minimumY, ref maximumY, ref found);
				TryUpdateLocalBounds(character.topRight, ref minimumX, ref maximumX, ref minimumY, ref maximumY, ref found);
				TryUpdateLocalBounds(character.bottomRight, ref minimumX, ref maximumX, ref minimumY, ref maximumY, ref found);
			}
			return found;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Wind HUD visible-bound measurement deferred: " + exception.GetBaseException().Message);
			return false;
		}
	}

	private float MeasureReferenceCharacterWidth()
	{
		if (text_ == null)
		{
			return 0.5f;
		}
		try
		{
			float width = text_.GetPreferredValues("0").x;
			if (width > 0f && !float.IsNaN(width) && !float.IsInfinity(width))
			{
				return width;
			}
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Wind HUD reference-character measurement deferred: " + exception.GetBaseException().Message);
		}
		return 0.5f;
	}

	private void UpdateTextPosition()
	{
		if (mainCamera_ == null || textGameObject_ == null || text_ == null || string.IsNullOrEmpty(text_.text))
		{
			return;
		}
		Vector3 viewportPosition = mainCamera_.ViewportToWorldPoint(new Vector3(0.07f, 0.88f, 0f));
		if (layoutLocaleRevision_ != ModLocalization.Revision)
		{
			ApplySingleLineLayout();
		}
		Vector3 currentPosition = textGameObject_.transform.position;
		Vector3 baselinePosition = new Vector3(viewportPosition.x + WindTextBaselineX, viewportPosition.y + WindTextBaselineY, currentPosition.z);
		if ((currentPosition - baselinePosition).sqrMagnitude > 0.000001f)
		{
			textGameObject_.transform.position = baselinePosition;
		}

		bool hasArrow = TryGetArrowRightScreen(out float arrowRightPixels);
		bool hasArrowCenter = TryGetArrowCenterScreen(out float arrowCenterYPixels);
		bool hasVisibleText = TryGetVisibleTextScreenBounds(out float visibleLeftPixels, out float visibleRightPixels, out float visibleBottomPixels, out float visibleTopPixels);
		bool hasCharacterWidth = TryGetReferenceCharacterWidthScreen(out float referenceCharacterPixels);
		float targetGapPixels = Mathf.Max(1f, referenceCharacterPixels) * WindTextArrowDistanceFactor + WindTextArrowOpticalGapPixels;
		float horizontalDeltaPixels = 0f;
		float verticalDeltaPixels = 0f;
		bool horizontalAlignmentReady = hasArrow && hasVisibleText && hasCharacterWidth;
		bool verticalAlignmentReady = hasArrowCenter && hasVisibleText;
		if (horizontalAlignmentReady)
		{
			float desiredVisibleLeftPixels = arrowRightPixels + targetGapPixels;
			horizontalDeltaPixels = desiredVisibleLeftPixels - visibleLeftPixels;
			if (!IsFinite(horizontalDeltaPixels) || Mathf.Abs(horizontalDeltaPixels) > Mathf.Max(Screen.width, Screen.height) * 2f)
			{
				horizontalAlignmentReady = false;
			}
		}

		if (verticalAlignmentReady)
		{
			float textCenterY = (visibleBottomPixels + visibleTopPixels) * 0.5f;
			verticalDeltaPixels = arrowCenterYPixels - textCenterY;
			if (!IsFinite(verticalDeltaPixels) || Mathf.Abs(verticalDeltaPixels) > Mathf.Max(Screen.width, Screen.height) * 2f)
			{
				verticalAlignmentReady = false;
			}
		}

		bool positionedAgainstArrow = false;
		bool verticalAlignmentApplied = false;
		if (horizontalAlignmentReady || verticalAlignmentReady)
		{
			Vector3 textScreenPosition = mainCamera_.WorldToScreenPoint(baselinePosition);
			if (IsFinite(textScreenPosition.x) && IsFinite(textScreenPosition.y) && IsFinite(textScreenPosition.z) && textScreenPosition.z > 0f)
			{
				float screenShiftX = horizontalAlignmentReady ? horizontalDeltaPixels : 0f;
				float screenShiftY = verticalAlignmentReady && Mathf.Abs(verticalDeltaPixels) > WindTextVerticalAlignmentTolerancePixels ? verticalDeltaPixels : 0f;
				Vector3 shiftedWorldPosition = mainCamera_.ScreenToWorldPoint(new Vector3(textScreenPosition.x + screenShiftX, textScreenPosition.y + screenShiftY, textScreenPosition.z));
				if (IsFinite(shiftedWorldPosition.x) && IsFinite(shiftedWorldPosition.y) && IsFinite(shiftedWorldPosition.z))
				{
					Vector3 desiredPosition = new Vector3(shiftedWorldPosition.x, shiftedWorldPosition.y, baselinePosition.z);
					if ((textGameObject_.transform.position - desiredPosition).sqrMagnitude > 0.000001f)
					{
						textGameObject_.transform.position = desiredPosition;
					}
					positionedAgainstArrow = horizontalAlignmentReady || verticalAlignmentReady;
					verticalAlignmentApplied = verticalAlignmentReady && Mathf.Abs(verticalDeltaPixels) > WindTextVerticalAlignmentTolerancePixels;
				}
			}
		}

		if (!positionedAgainstArrow)
		{
			Vector3 fallbackPosition = new Vector3(viewportPosition.x + WindTextBaselineX, viewportPosition.y + WindTextBaselineY, baselinePosition.z);
			if ((textGameObject_.transform.position - fallbackPosition).sqrMagnitude > 0.000001f)
			{
				textGameObject_.transform.position = fallbackPosition;
			}
		}

		if (hasArrow && hasArrowCenter && hasCharacterWidth && !windGeometryDiagnosticLogged_ && TryGetVisibleTextScreenBounds(out float finalVisibleLeftPixels, out float finalVisibleRightPixels, out float finalVisibleBottomPixels, out float finalVisibleTopPixels))
		{
			float actualGapPixels = finalVisibleLeftPixels - arrowRightPixels;
			float finalTextCenterY = (finalVisibleBottomPixels + finalVisibleTopPixels) * 0.5f;
			float finalVerticalDeltaPixels = finalTextCenterY - arrowCenterYPixels;
			string fontName = text_.font == null ? "<null>" : text_.font.name;
			Plugin.Log?.LogInfo("Wind HUD geometry locale=" + ModLocalization.CurrentLocaleCode + " font=" + fontName + " source=" + ArrowGeometrySourceName(arrowGeometrySource_) + " positioningApplied=" + positionedAgainstArrow + " verticalApplied=" + verticalAlignmentApplied + " arrowRightPx=" + arrowRightPixels.ToString("0.###") + " textVisibleLeftPx=" + finalVisibleLeftPixels.ToString("0.###") + " actualGapPx=" + actualGapPixels.ToString("0.###") + " targetGapPx=" + targetGapPixels.ToString("0.###") + " arrowCenterYPx=" + arrowCenterYPixels.ToString("0.###") + " textCenterYPx=" + finalTextCenterY.ToString("0.###") + " verticalErrorPx=" + finalVerticalDeltaPixels.ToString("0.###") + " referenceCharacterPx=" + referenceCharacterPixels.ToString("0.###"));
			windGeometryDiagnosticLogged_ = true;
		}
	}

	private bool TryGetArrowCenterScreen(out float centerYPixels)
	{
		centerYPixels = 0f;
		RectTransform arrowRectTransform = windsockImage_ == null ? null : windsockImage_.rectTransform;
		if (arrowRectTransform == null)
		{
			arrowRectTransform = windsockRectTransform_;
		}
		if (arrowRectTransform == null)
		{
			return false;
		}
		try
		{
			// Use the arrow RectTransform pivot center rather than the rotated
			// silhouette. The pivot center stays stable while the wind direction
			// changes, so the label cannot bob vertically as the arrow turns.
			Vector3 worldPoint = arrowRectTransform.TransformPoint(arrowRectTransform.rect.center);
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetArrowProjectionCamera(), worldPoint);
			if (!IsFinite(screenPoint.x) || !IsFinite(screenPoint.y))
			{
				return false;
			}
			centerYPixels = screenPoint.y;
			return true;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Wind HUD arrow center was unavailable: " + exception.GetBaseException().Message);
			return false;
		}
	}

	private bool TryGetVisibleTextScreenBounds(out float visibleLeftPixels, out float visibleRightPixels, out float visibleBottomPixels, out float visibleTopPixels)
	{
		visibleLeftPixels = 0f;
		visibleRightPixels = 0f;
		visibleBottomPixels = 0f;
		visibleTopPixels = 0f;
		if (mainCamera_ == null || text_ == null || string.IsNullOrEmpty(text_.text))
		{
			return false;
		}
		try
		{
			// The visible glyph rectangle is stable until the text/font/locale
			// changes. Cache it in local space and only project its four corners
			// here, keeping the per-frame path independent of character count.
			if (!visibleTextBoundsReady_)
			{
				visibleTextBoundsReady_ = TryMeasureVisibleTextBounds(out visibleTextMinLocalX_, out visibleTextMaxLocalX_, out visibleTextMinLocalY_, out visibleTextMaxLocalY_);
			}
			if (visibleTextBoundsReady_)
			{
				bool found = false;
				TryUpdateScreenBounds(new Vector3(visibleTextMinLocalX_, visibleTextMinLocalY_, 0f), ref visibleLeftPixels, ref visibleRightPixels, ref visibleBottomPixels, ref visibleTopPixels, ref found);
				TryUpdateScreenBounds(new Vector3(visibleTextMinLocalX_, visibleTextMaxLocalY_, 0f), ref visibleLeftPixels, ref visibleRightPixels, ref visibleBottomPixels, ref visibleTopPixels, ref found);
				TryUpdateScreenBounds(new Vector3(visibleTextMaxLocalX_, visibleTextMaxLocalY_, 0f), ref visibleLeftPixels, ref visibleRightPixels, ref visibleBottomPixels, ref visibleTopPixels, ref found);
				TryUpdateScreenBounds(new Vector3(visibleTextMaxLocalX_, visibleTextMinLocalY_, 0f), ref visibleLeftPixels, ref visibleRightPixels, ref visibleBottomPixels, ref visibleTopPixels, ref found);
				if (found)
				{
					return true;
				}
			}
			if (textRenderer_ != null && textRenderer_.enabled)
			{
				return TryGetWorldBoundsScreenBounds(textRenderer_.bounds, mainCamera_, out visibleLeftPixels, out visibleRightPixels, out visibleBottomPixels, out visibleTopPixels);
			}
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Wind HUD text screen bounds were unavailable: " + exception.GetBaseException().Message);
		}
		return false;
	}

	private bool TryGetReferenceCharacterWidthScreen(out float widthPixels)
	{
		widthPixels = 0f;
		if (mainCamera_ == null || textGameObject_ == null || referenceCharacterWidth_ <= 0f)
		{
			return false;
		}
		try
		{
			Vector3 origin = mainCamera_.WorldToScreenPoint(textGameObject_.transform.TransformPoint(Vector3.zero));
			Vector3 end = mainCamera_.WorldToScreenPoint(textGameObject_.transform.TransformPoint(new Vector3(referenceCharacterWidth_, 0f, 0f)));
			widthPixels = Mathf.Abs(end.x - origin.x);
			return IsFinite(widthPixels) && widthPixels > 0.01f;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Wind HUD reference-character screen measurement deferred: " + exception.GetBaseException().Message);
			return false;
		}
	}

	private bool TryGetArrowRightScreen(out float arrowRightPixels)
	{
		arrowRightPixels = 0f;
		arrowGeometrySource_ = ArrowGeometrySource.None;
		Camera arrowCamera = GetArrowProjectionCamera();
		if (EnsureArrowGeometryCache() && cachedArrowImage_ != null && cachedArrowImage_.rectTransform != null && cachedArrowVisiblePoints_ != null && cachedArrowVisiblePoints_.Length > 0)
		{
			bool found = false;
			for (int i = 0; i < cachedArrowVisiblePoints_.Length; i++)
			{
				Vector3 worldPoint = cachedArrowImage_.rectTransform.TransformPoint(cachedArrowVisiblePoints_[i]);
				Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(arrowCamera, worldPoint);
				if (!IsFinite(screenPoint.x) || !IsFinite(screenPoint.y))
				{
					continue;
				}
				if (!found || screenPoint.x > arrowRightPixels)
				{
					arrowRightPixels = screenPoint.x;
					found = true;
				}
			}
			if (found)
			{
				return true;
			}
		}
		if (windsockRenderer_ != null && windsockRenderer_.enabled)
		{
			try
			{
				if (TryGetWorldBoundsScreenRight(windsockRenderer_.bounds, mainCamera_, out arrowRightPixels))
				{
					arrowGeometrySource_ = ArrowGeometrySource.RendererBounds;
					return true;
				}
			}
			catch (Exception exception)
			{
				Plugin.Log?.LogDebug("Wind arrow renderer bounds were unavailable: " + exception.GetBaseException().Message);
			}
		}
		if (windsockImage_ != null && windsockImage_.rectTransform != null)
		{
			try
			{
				if (TryGetRectScreenRight(windsockImage_.rectTransform, arrowCamera, out arrowRightPixels))
				{
					arrowGeometrySource_ = ArrowGeometrySource.ImageRect;
					return true;
				}
			}
			catch (Exception exception)
			{
				Plugin.Log?.LogDebug("Wind arrow bounds were unavailable: " + exception.GetBaseException().Message);
			}
		}
		if (windsockRectTransform_ != null)
		{
			try
			{
				if (TryGetRectScreenRight(windsockRectTransform_, arrowCamera, out arrowRightPixels))
				{
					arrowGeometrySource_ = ArrowGeometrySource.RectTransform;
					return true;
				}
			}
			catch (Exception exception)
			{
				Plugin.Log?.LogDebug("Wind arrow fallback bounds were unavailable: " + exception.GetBaseException().Message);
			}
		}
		if (!windGeometryMissingLogged_)
		{
			Plugin.Log?.LogWarning("Wind HUD arrow visual could not be resolved; wind text will use its fixed fallback position.");
			windGeometryMissingLogged_ = true;
		}
		return false;
	}

	private Camera GetArrowProjectionCamera()
	{
		if (arrowCanvas_ == null || arrowCanvas_.renderMode == RenderMode.ScreenSpaceOverlay)
		{
			return null;
		}
		return arrowCanvas_.worldCamera != null ? arrowCanvas_.worldCamera : mainCamera_;
	}

	private static void TryUpdateLocalBounds(Vector3 localPoint, ref float minimumX, ref float maximumX, ref float minimumY, ref float maximumY, ref bool found)
	{
		if (!found)
		{
			minimumX = localPoint.x;
			maximumX = localPoint.x;
			minimumY = localPoint.y;
			maximumY = localPoint.y;
			found = true;
			return;
		}
		if (localPoint.x < minimumX)
		{
			minimumX = localPoint.x;
		}
		if (localPoint.x > maximumX)
		{
			maximumX = localPoint.x;
		}
		if (localPoint.y < minimumY)
		{
			minimumY = localPoint.y;
		}
		if (localPoint.y > maximumY)
		{
			maximumY = localPoint.y;
		}
	}

	private void TryUpdateScreenBounds(Vector3 localPoint, ref float minimumX, ref float maximumX, ref float minimumY, ref float maximumY, ref bool found)
	{
		Vector3 screenPoint = mainCamera_.WorldToScreenPoint(text_.transform.TransformPoint(localPoint));
		if (!IsFinite(screenPoint.x) || !IsFinite(screenPoint.y))
		{
			return;
		}
		if (!found)
		{
			minimumX = screenPoint.x;
			maximumX = screenPoint.x;
			minimumY = screenPoint.y;
			maximumY = screenPoint.y;
			found = true;
			return;
		}
		if (screenPoint.x < minimumX)
		{
			minimumX = screenPoint.x;
		}
		if (screenPoint.x > maximumX)
		{
			maximumX = screenPoint.x;
		}
		if (screenPoint.y < minimumY)
		{
			minimumY = screenPoint.y;
		}
		if (screenPoint.y > maximumY)
		{
			maximumY = screenPoint.y;
		}
	}

	private bool TryGetRectScreenRight(RectTransform rectTransform, Camera camera, out float rightPixels)
	{
		rightPixels = 0f;
		if (rectTransform == null)
		{
			return false;
		}
		try
		{
			rectTransform.GetWorldCorners(windArrowCorners_);
			bool found = false;
			for (int i = 0; i < windArrowCorners_.Length; i++)
			{
				Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, windArrowCorners_[i]);
				if (!IsFinite(screenPoint.x) || !IsFinite(screenPoint.y))
				{
					continue;
				}
				if (!found || screenPoint.x > rightPixels)
				{
					rightPixels = screenPoint.x;
					found = true;
				}
			}
			return found;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Wind HUD RectTransform screen bounds were unavailable: " + exception.GetBaseException().Message);
			return false;
		}
	}

	private static bool TryGetWorldBoundsScreenRight(Bounds bounds, Camera camera, out float rightPixels)
	{
		rightPixels = 0f;
		if (camera == null || bounds.size.sqrMagnitude <= 0.000001f)
		{
			return false;
		}
		bool found = false;
		Vector3 minimum = bounds.min;
		Vector3 maximum = bounds.max;
		for (int x = 0; x <= 1; x++)
		{
			for (int y = 0; y <= 1; y++)
			{
				for (int z = 0; z <= 1; z++)
				{
					Vector3 worldPoint = new Vector3(x == 0 ? minimum.x : maximum.x, y == 0 ? minimum.y : maximum.y, z == 0 ? minimum.z : maximum.z);
					Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
					if (!IsFinite(screenPoint.x) || !IsFinite(screenPoint.y))
					{
						continue;
					}
					if (!found || screenPoint.x > rightPixels)
					{
						rightPixels = screenPoint.x;
						found = true;
					}
				}
			}
		}
		return found;
	}

	private static bool TryGetWorldBoundsScreenBounds(Bounds bounds, Camera camera, out float leftPixels, out float rightPixels, out float bottomPixels, out float topPixels)
	{
		leftPixels = 0f;
		rightPixels = 0f;
		bottomPixels = 0f;
		topPixels = 0f;
		if (camera == null || bounds.size.sqrMagnitude <= 0.000001f)
		{
			return false;
		}
		bool found = false;
		Vector3 minimum = bounds.min;
		Vector3 maximum = bounds.max;
		for (int x = 0; x <= 1; x++)
		{
			for (int y = 0; y <= 1; y++)
			{
				for (int z = 0; z <= 1; z++)
				{
					Vector3 worldPoint = new Vector3(x == 0 ? minimum.x : maximum.x, y == 0 ? minimum.y : maximum.y, z == 0 ? minimum.z : maximum.z);
					Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
					if (!IsFinite(screenPoint.x) || !IsFinite(screenPoint.y))
					{
						continue;
					}
					if (!found)
					{
						leftPixels = screenPoint.x;
						rightPixels = screenPoint.x;
						bottomPixels = screenPoint.y;
						topPixels = screenPoint.y;
						found = true;
						continue;
					}
					if (screenPoint.x < leftPixels)
					{
						leftPixels = screenPoint.x;
					}
					if (screenPoint.x > rightPixels)
					{
						rightPixels = screenPoint.x;
					}
					if (screenPoint.y < bottomPixels)
					{
						bottomPixels = screenPoint.y;
					}
					if (screenPoint.y > topPixels)
					{
						topPixels = screenPoint.y;
					}
				}
			}
		}
		return found;
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static string ArrowGeometrySourceName(ArrowGeometrySource source)
	{
		switch (source)
		{
			case ArrowGeometrySource.SpritePhysicsShape:
				return "SpritePhysicsShape";
			case ArrowGeometrySource.SpriteVertices:
				return "SpriteVertices";
			case ArrowGeometrySource.ImageRect:
				return "ImageRect";
			case ArrowGeometrySource.RendererBounds:
				return "RendererBounds";
			case ArrowGeometrySource.RectTransform:
				return "RectTransform";
			default:
				return "None";
		}
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
		if (textGameObject_ != null)
		{
			UnityEngine.Object.Destroy(textGameObject_);
		}
		textGameObject_ = null;
		text_ = null;
		mainCamera_ = null;
		windsockRectTransform_ = null;
		windsockImage_ = null;
		arrowCanvas_ = null;
		cachedArrowImage_ = null;
		cachedArrowSprite_ = null;
		cachedArrowVisiblePoints_ = null;
		arrowGeometryReady_ = false;
		arrowGeometrySource_ = ArrowGeometrySource.None;
		textRenderer_ = null;
		windsockRenderer_ = null;
		displayedText_ = null;
		displayedWindDegree_ = int.MinValue;
		displayedLocaleRevision_ = -1;
		layoutLocaleRevision_ = -1;
		visibleTextMinLocalX_ = 0f;
		visibleTextMaxLocalX_ = 0f;
		visibleTextMinLocalY_ = 0f;
		visibleTextMaxLocalY_ = 0f;
		referenceCharacterWidth_ = 0f;
		visibleTextBoundsReady_ = false;
		windGeometryDiagnosticLogged_ = false;
		windGeometryMissingLogged_ = false;
	}
}
