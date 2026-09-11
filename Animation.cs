using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways;

public static class Animation
{
	public static Color gaugeColor = new Color(1f, 1f, 1f, 0.5f);

	public static IEnumerator BlinkCoroutine(SpriteRenderer spriteRenderer)
	{
		while (spriteRenderer != null)
		{
			spriteRenderer.enabled = false;
			yield return new WaitForSecondsRealtime(0.4f);
			if (spriteRenderer == null)
			{
				yield break;
			}
			spriteRenderer.enabled = true;
			yield return new WaitForSecondsRealtime(0.4f);
		}
	}

	public static IEnumerator BlinkFastCoroutine(SpriteRenderer spriteRenderer)
	{
		while (spriteRenderer != null)
		{
			spriteRenderer.enabled = false;
			yield return new WaitForSecondsRealtime(0.2f);
			if (spriteRenderer == null)
			{
				yield break;
			}
			spriteRenderer.enabled = true;
			yield return new WaitForSecondsRealtime(0.2f);
		}
	}

	public static bool Blink()
	{
		return Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f;
	}

	public static bool BlinkLong()
	{
		return Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.1f;
	}

	public static void SetPixel(bool cond, int x, int y, Color color, ref Texture2D texture)
	{
		if (cond)
		{
			texture.SetPixel(x, y, color);
		}
		else
		{
			texture.SetPixel(x, y, Color.clear);
		}
	}
}
