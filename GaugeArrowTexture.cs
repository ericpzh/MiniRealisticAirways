using UnityEngine;

namespace MiniRealisticAirways;

public static class GaugeArrowTexture
{
	public static Rect rect_;

	public static Texture2D texture_;

	private static Sprite sprite_;

	public const int HEIGHT = 45;

	public const int WIDTH = 90;

	public const int STROKE = 20;

	private static Texture2D DrawArrow()
	{
		Texture2D texture = new Texture2D(90, 45);
		Color32[] pixels = new Color32[WIDTH * HEIGHT];
		for (int i = 0; i < texture.height; i++)
		{
			for (int j = 0; j < texture.width; j++)
			{
				pixels[i * WIDTH + j] = (j > i && j - STROKE < i) || (-j + WIDTH - STROKE < i && -j + WIDTH - STROKE > i - STROKE) ? (Color32)Animation.gaugeColor : (Color32)Color.clear;
			}
		}
		texture.SetPixels32(pixels);
		texture.Apply();
		return texture;
	}

	public static void PreLoadTexture()
	{
		if (texture_ != null)
		{
			return;
		}
		Plugin.Log?.LogInfo("Pre-rendered gauge texture.");
		texture_ = DrawArrow();
		rect_ = new Rect(0f, 3f, 90f, 30f);
	}

	public static Sprite GetSprite()
	{
		if (texture_ == null)
		{
			return null;
		}
		if (sprite_ == null)
		{
			sprite_ = Sprite.Create(texture_, rect_, Vector2.zero);
			sprite_.name = "MiniRealisticAirways Gauge Arrow";
			sprite_.hideFlags = HideFlags.HideAndDontSave;
		}
		return sprite_;
	}

	public static void DestroyTexture()
	{
		if (texture_ != null || sprite_ != null)
		{
			Plugin.Log?.LogInfo("Gauge texture destroyed.");
			if (sprite_ != null)
			{
				Object.Destroy(sprite_);
				sprite_ = null;
			}
			if (texture_ != null)
			{
				Object.Destroy(texture_);
			}
			texture_ = null;
		}
	}
}
