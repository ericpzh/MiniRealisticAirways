using UnityEngine;
using UnityEngine.UI;

namespace MiniRealisticAirways;

// 使用 SVG 四边轮廓生成透明背景纹理，兼容按矩形绘制的游戏 UI。
internal sealed class WindArrowVisual : MonoBehaviour
{
	private Sprite sprite_;
	private Texture2D texture_;
	private Image image_;
	private Sprite originalSprite_;
	private Sprite originalOverrideSprite_;
	private Image.Type originalType_;
	private bool originalMesh_;
	private bool originalAspect_;

	internal static void Apply(Image image)
	{
		if (image == null) return;
		WindArrowVisual visual = image.GetComponent<WindArrowVisual>();
		if (visual == null) visual = image.gameObject.AddComponent<WindArrowVisual>();
		visual.Initialize(image);
	}

	private void Initialize(Image image)
	{
		if (sprite_ != null) return;
		image_ = image;
		originalSprite_ = image.sprite;
		originalOverrideSprite_ = image.overrideSprite;
		originalType_ = image.type;
		originalMesh_ = image.useSpriteMesh;
		originalAspect_ = image.preserveAspect;
		// SVG 的可见范围为 192 × 176；使用同样比例，尾部内凹 44 个单位。
		texture_ = new Texture2D(WindArrowShape.Width, WindArrowShape.Height, TextureFormat.RGBA32, false);
		texture_.name = "真实空管实心风向箭头";
		texture_.hideFlags = HideFlags.HideAndDontSave;
		Color32[] pixels = new Color32[WindArrowShape.Width * WindArrowShape.Height];
		for (int y = 0; y < WindArrowShape.Height; y++)
		for (int x = 0; x < WindArrowShape.Width; x++)
			pixels[y * WindArrowShape.Width + x] = new Color32(255, 255, 255, WindArrowShape.Coverage(x, y));
		texture_.wrapMode = TextureWrapMode.Clamp;
		texture_.filterMode = FilterMode.Bilinear;
		texture_.SetPixels32(pixels);
		texture_.Apply(false, true);
		sprite_ = Sprite.Create(texture_, new Rect(0, 0, WindArrowShape.Width, WindArrowShape.Height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
		sprite_.name = "四边实心风向箭头";
		sprite_.hideFlags = HideFlags.HideAndDontSave;
		// 原版 ESC_Button 使用 Arrow Left：保留左向初始边与原有旋转公式。
		// Override API 接收 Sprite Rect 像素坐标；pivot 和 pixelsPerUnit 由 Unity 换算。
		Vector2[] vertices = { new Vector2(0f, 88f), new Vector2(192f, 176f), new Vector2(148f, 88f), new Vector2(192f, 0f) };
		sprite_.OverrideGeometry(vertices, new ushort[] { 0, 1, 2, 0, 2, 3 });
		sprite_.OverridePhysicsShape(new System.Collections.Generic.List<Vector2[]> { vertices });
		image.overrideSprite = null;
		image.sprite = sprite_;
		image.type = Image.Type.Simple;
		image.useSpriteMesh = false;
		image.preserveAspect = true;
	}

	// windFromDegrees 使用游戏航向（北为零，顺时针）；尖头表示空气流去的方向。
	internal static bool SetWindDirection(Image image, Camera mapCamera, float windFromDegrees)
	{
		if (image == null || mapCamera == null) return false;
		RectTransform rect = image.rectTransform;
		RectTransform parent = rect.parent as RectTransform;
		if (parent == null) return false;
		Canvas canvas = image.canvas;
		Camera uiCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
			? null : canvas.worldCamera;
		Vector3 worldFlow = Quaternion.AngleAxis(windFromDegrees, Vector3.back) * Vector3.down;
		Vector3 origin = mapCamera.transform.position + mapCamera.transform.forward * 10f;
		Vector2 flow = (Vector2)(mapCamera.WorldToScreenPoint(origin + worldFlow) - mapCamera.WorldToScreenPoint(origin));
		if (flow.sqrMagnitude < 0.0001f) return false;
		Vector2 center = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center, uiCamera, out Vector2 a)
			|| !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center + flow.normalized * 100f, uiCamera, out Vector2 b)) return false;
		Vector2 direction = b - a;
		if (direction.sqrMagnitude < 0.0001f || Mathf.Abs(rect.localScale.x) < 0.0001f) return false;
		// 纹理尖头向左；负缩放会翻转尖头，必须一起计入。
		float tipAngle = rect.localScale.x < 0f ? 0f : 180f;
		Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - tipAngle);
		if (rect.localRotation != rotation) rect.localRotation = rotation;
		return true;
	}

	private void OnDestroy()
	{
		if (image_ != null && image_.sprite == sprite_)
		{
			image_.sprite = originalSprite_;
			image_.overrideSprite = originalOverrideSprite_;
			image_.type = originalType_;
			image_.useSpriteMesh = originalMesh_;
			image_.preserveAspect = originalAspect_;
		}
		if (sprite_ != null) Object.Destroy(sprite_);
		if (texture_ != null) Object.Destroy(texture_);
	}
}
