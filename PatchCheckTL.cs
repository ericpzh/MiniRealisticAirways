using System;
using HarmonyLib;

namespace MiniRealisticAirways;

[HarmonyPatch(typeof(GUIAutoHider), "CheckTL", new Type[] { })]
internal class PatchCheckTL
{
	private static bool Prefix(ref GUIAutoHider __instance)
	{
		return false;
	}
}
