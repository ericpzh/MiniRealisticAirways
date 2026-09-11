using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MiniRealisticAirways;

// 在原版移动和航向判定之后、写入 TouchedDown 之前校验；不复制原版进近算法。
[HarmonyPatch]
internal static class PatchLandingTouchdown
{
	[HarmonyTargetMethod]
	internal static MethodBase TargetMethod()
	{
		return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(Aircraft), "LandCoroutine"))
			?? throw new MissingMethodException("Aircraft.LandCoroutine iterator could not be located.");
	}

	[HarmonyTranspiler]
	internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		var codes = new List<CodeInstruction>(instructions);
		if (codes.Exists(code => code.blocks.Count != 0))
			throw new InvalidOperationException("Unsupported Aircraft.LandCoroutine: touchdown guard cannot return through an exception block.");
		FieldInfo state = AccessTools.Field(typeof(Aircraft), nameof(Aircraft.state));
		int match = -1;
		int matches = 0;
		for (int i = 1; i < codes.Count; i++)
		{
			if (codes[i].opcode == OpCodes.Stfld && Equals(codes[i].operand, state)
				&& codes[i - 1].LoadsConstant((int)Aircraft.State.TouchedDown))
			{
				match = i - 1;
				matches++;
			}
		}
		if (matches != 1)
			throw new InvalidOperationException("Unsupported Aircraft.LandCoroutine: expected one touchdown state assignment, found " + matches + ".");
		// 此处栈顶已经是 Aircraft。失败时弹出该引用并结束 MoveNext，
		// 防止随后触发落地事件、计分、缩放和播报；成功则继续原版赋值。
		Label proceed = generator.DefineLabel();
		var duplicate = new CodeInstruction(OpCodes.Dup);
		duplicate.labels.AddRange(codes[match].labels);
		codes[match].labels.Clear();
		duplicate.blocks.AddRange(codes[match].blocks);
		codes[match].blocks.Clear();
		codes[match].labels.Add(proceed);
		codes.InsertRange(match, new[]
		{
			duplicate,
			new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchLandCoroutine), nameof(PatchLandCoroutine.AllowTouchdown))),
			new CodeInstruction(OpCodes.Brtrue, proceed),
			new CodeInstruction(OpCodes.Pop),
			new CodeInstruction(OpCodes.Ldc_I4_0),
			new CodeInstruction(OpCodes.Ret)
		});
		return codes;
	}
}
