using System.Collections.Generic;
using UnityEngine;

namespace MiniRealisticAirways;

public static class WeatherCellTextures
{
	public static Rect rect_;

	public const int OPACITY_GRADIENT = 10;

	public static List<List<Texture2D>> textures_;

	private static List<List<Sprite>> sprites_;

	public static int RED = 0;

	public static int YELLOW = 1;

	public static int GREEN = 2;

	private static List<Color> colors_;

	private const int SIZE = 50;

	private static Texture2D DrawCell(Color color)
	{
		Texture2D texture2D = new Texture2D(50, 50);
		Color32[] pixels = new Color32[SIZE * SIZE];
		for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
		texture2D.SetPixels32(pixels);
		texture2D.Apply();
		return texture2D;
	}

	public static void PreLoadTextures()
	{
		if (textures_ != null && textures_.Count == 10 && sprites_ != null && sprites_.Count == 10)
		{
			return;
		}
		DestroyTextures();
		Plugin.Log?.LogInfo("Pre-rendered weather cell textures.");
		colors_ = new List<Color>
		{
			new Color(1f, 0f, 0f, 0.1f),
			new Color(1f, 1f, 0f, 0.1f),
			new Color(0f, 1f, 0f, 0.1f)
		};
		textures_ = new List<List<Texture2D>>();
		for (int i = 1; i < 11; i++)
		{
			List<Texture2D> list = new List<Texture2D>();
			foreach (Color item in colors_)
			{
				float a = (float)i / 10f * item.a;
				list.Add(DrawCell(new Color(item.r, item.g, item.b, a)));
			}
			textures_.Add(list);
		}
		rect_ = new Rect(0f, 0f, 50f, 50f);
		sprites_ = new List<List<Sprite>>(10);
		for (int k = 0; k < 10; k++)
		{
			sprites_.Add(new List<Sprite>(3) { null, null, null });
		}
	}

	public static Sprite GetSprite(int opacityIndex, int colorIndex)
	{
		if (textures_ == null || sprites_ == null || opacityIndex < 0 || opacityIndex >= textures_.Count || colorIndex < 0 || colorIndex >= textures_[opacityIndex].Count)
		{
			return null;
		}
		Sprite sprite = sprites_[opacityIndex][colorIndex];
		if (sprite == null && textures_[opacityIndex][colorIndex] != null)
		{
			sprite = Sprite.Create(textures_[opacityIndex][colorIndex], rect_, Vector2.zero);
			sprite.name = "MiniRealisticAirways Weather " + opacityIndex + "-" + colorIndex;
			sprite.hideFlags = HideFlags.HideAndDontSave;
			sprites_[opacityIndex][colorIndex] = sprite;
		}
		return sprite;
	}

	public static void DestroyTextures()
	{
		if (textures_ == null && sprites_ == null)
		{
			return;
		}
		Plugin.Log?.LogInfo("Weather cell textures destroyed.");
		if (sprites_ != null)
		{
			foreach (List<Sprite> sprites in sprites_)
			{
				foreach (Sprite sprite in sprites)
				{
					if (sprite != null)
					{
						Object.Destroy(sprite);
					}
				}
				sprites.Clear();
			}
			sprites_.Clear();
			sprites_ = null;
		}
		if (textures_ == null)
		{
			return;
		}
		foreach (List<Texture2D> item in textures_)
		{
			foreach (Texture2D item2 in item)
			{
				Object.Destroy(item2);
			}
		}
		textures_.Clear();
		textures_ = null;
		colors_?.Clear();
		colors_ = null;
	}
}
