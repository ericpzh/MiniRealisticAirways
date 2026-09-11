using System;
using System.Collections.Generic;

namespace MiniRealisticAirways;

public static class Utils
{
	private static readonly Random random = new Random();

	public static void Shuffle<T>(List<T> list)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index = random.Next(num + 1);
			T value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
	}
}
