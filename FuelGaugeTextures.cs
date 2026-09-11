using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRealisticAirways;

public static class FuelGaugeTextures
{
	public static Rect rect_;

	public static List<Texture2D> fuelTextures_;

	private static List<Sprite> fuelSprites_;

	public const int SIZE = 35;

	public const int REFRESH_GRADIENT = 100;

	private static Texture2D DrawDroplet(int step)
	{
		int filledRows = step * SIZE / REFRESH_GRADIENT;
		Texture2D texture = new Texture2D(SIZE, SIZE);
		Color32[] pixels = new Color32[SIZE * SIZE];
		for (int y = 0; y < SIZE; y++)
		for (int x = 0; x < SIZE; x++)
		{
			bool inside = y < 14
				? Math.Sqrt((x - 17) * (x - 17) + (y - 17) * (y - 17)) < 17.0
				: Math.Sin((double)((float)x / 12f) + 3.25) + 0.05 < Math.Sin((double)(float)y / 17.5 + 2.4);
			pixels[y * SIZE + x] = inside ? (Color32)(y < filledRows ? Color.white : Color.gray) : (Color32)Color.clear;
		}
		texture.SetPixels32(pixels);
		texture.Apply();
		return texture;
	}

	public static void PreLoadTextures()
	{
		if (fuelTextures_ != null && fuelTextures_.Count == 101 && fuelSprites_ != null && fuelSprites_.Count == 101)
		{
			return;
		}
		DestroyTextures();
		Plugin.Log?.LogInfo("Pre-rendered fuel gauge textures.");
		fuelTextures_ = new List<Texture2D>(101);
		for (int i = 0; i <= REFRESH_GRADIENT; i++)
		{
			fuelTextures_.Add(DrawDroplet(i));
		}
		rect_ = new Rect(0f, 0f, 35f, 35f);
		fuelSprites_ = new List<Sprite>(101);
		for (int j = 0; j <= REFRESH_GRADIENT; j++)
		{
			fuelSprites_.Add(null);
		}
	}

	public static Sprite GetSprite(int percent)
	{
		if (fuelTextures_ == null || fuelSprites_ == null || percent < 0 || percent >= fuelTextures_.Count)
		{
			return null;
		}
		Sprite sprite = fuelSprites_[percent];
		if (sprite == null && fuelTextures_[percent] != null)
		{
			sprite = Sprite.Create(fuelTextures_[percent], rect_, Vector2.zero);
			sprite.name = "MiniRealisticAirways Fuel " + percent;
			sprite.hideFlags = HideFlags.HideAndDontSave;
			fuelSprites_[percent] = sprite;
		}
		return sprite;
	}

	public static void DestroyTextures()
	{
		if (fuelTextures_ == null && fuelSprites_ == null)
		{
			return;
		}
		Plugin.Log?.LogInfo("Fuel gauge textures destroyed.");
		if (fuelSprites_ != null)
		{
			for (int i = 0; i < fuelSprites_.Count; i++)
			{
				if (fuelSprites_[i] != null)
				{
					UnityEngine.Object.Destroy(fuelSprites_[i]);
				}
			}
			fuelSprites_.Clear();
			fuelSprites_ = null;
		}
		if (fuelTextures_ == null)
		{
			return;
		}
		for (int i = 0; i < fuelTextures_.Count; i++)
		{
			if ((bool)fuelTextures_[i])
			{
				UnityEngine.Object.Destroy(fuelTextures_[i]);
			}
		}
		fuelTextures_.Clear();
		fuelTextures_ = null;
	}
}
