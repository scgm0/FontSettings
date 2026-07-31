using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace FontSettings;

[HarmonyPatch(typeof(GuiCompositeSettings), "OnInterfaceOptions")]
public static class GuiCompositeSettings_OnInterfaceOptions_Patch {
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
		var targetMethod = AccessTools.Method(typeof(GuiComposerHelpers),
			"AddRichtext",
			[typeof(GuiComposer), typeof(string), typeof(CairoFont), typeof(ElementBounds), typeof(string)]);

		var replacementMethod =
			AccessTools.Method(typeof(FontSettingsModSystem), nameof(FontSettingsModSystem.InjectFontSettings));

		foreach (var inst in instructions) {
			if (inst.Calls(targetMethod)) {
				yield return new(OpCodes.Call, replacementMethod);
			} else {
				yield return inst;
			}
		}
	}

	[HarmonyPostfix]
	public static void Postfix(GuiCompositeSettings __instance, MethodBase __originalMethod) {
		FontSettingsModSystem._guiCompositeSettings = __instance;
		FontSettingsModSystem._onInterfaceOptions = __originalMethod;
	}
}