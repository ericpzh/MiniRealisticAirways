using System;

namespace MiniRealisticAirways;

// 与确认的 SVG 相同的四边形，默认向左，与游戏原始 Arrow Left 一致；透明轮廓不依赖 UI 网格支持。
internal static class WindArrowShape
{
	internal const int Width = 192;
	internal const int Height = 176;

	internal static byte Coverage(int x, int y)
	{
		int covered = 0;
		const int samples = 4;
		for (int sy = 0; sy < samples; sy++)
		for (int sx = 0; sx < samples; sx++)
		{
			float px = 176f - (y + (sy + 0.5f) / samples);
			float py = x + (sx + 0.5f) / samples;
			float distance = Math.Abs(px - 88f);
			if (distance <= 88f * py / 192f && py <= 148f + distance * 0.5f) covered++;
		}
		return (byte)((covered * 255 + 8) / 16);
	}
}
