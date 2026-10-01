using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace FontSettings;

[HarmonyPatchCategory("immersivemaps")]
public class ImmersiveMapsCompat {

	[HarmonyTargetMethod]
	public static MethodBase? TargetMethod() {
		return AccessTools.Method("ImmersiveMaps.Rendering.WaypointGlyphCache:GetLabel");
	}

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
		var decorativeFontGetter =
			AccessTools.PropertyGetter(typeof(ClientSettings), nameof(ClientSettings.DecorativeFontName));

		foreach (var instruction in instructions) {
			if (instruction.opcode == OpCodes.Ldstr && instruction.operand is "Goudy Bookletter 1911") {
				yield return new(OpCodes.Call, decorativeFontGetter) {
					labels = instruction.labels,
					blocks = instruction.blocks
				};
			} else {
				yield return instruction;
			}
		}
	}
}