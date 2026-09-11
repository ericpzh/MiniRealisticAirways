using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// 飞机与航点 HUD 共用的实心方块精灵：1×1 白色像素、点过滤、中心轴点。
/// 高度/速度等级由若干实心方块表示，视觉高度不随字形变化。
/// </summary>
internal static class HudIndicatorTexture
{
	private static Texture2D texture_;

	private static Sprite sprite_;

	internal static Sprite GetOrCreate()
	{
		if (sprite_ != null)
		{
			return sprite_;
		}
		texture_ = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
		{
			name = "MRA HUD Block Texture",
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp,
			hideFlags = HideFlags.HideAndDontSave
		};
		texture_.SetPixel(0, 0, Color.white);
		texture_.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		sprite_ = Sprite.Create(texture_, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
		sprite_.name = "MRA HUD Block";
		sprite_.hideFlags = HideFlags.HideAndDontSave;
		return sprite_;
	}

	internal static void Reset()
	{
		if (sprite_ != null)
		{
			UnityEngine.Object.Destroy(sprite_);
		}
		if (texture_ != null)
		{
			UnityEngine.Object.Destroy(texture_);
		}
		sprite_ = null;
		texture_ = null;
	}
}
