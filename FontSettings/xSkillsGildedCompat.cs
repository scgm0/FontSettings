using HarmonyLib;
using xSkillGilded;

namespace FontSettings;

[HarmonyPatchCategory("xSkillGilded")]
public static class XSkillsGildedCompat {
	[HarmonyPatch(typeof(VSImGuiFontPatcher), nameof(VSImGuiFontPatcher.StartPre))]
	[HarmonyPrefix]
	public static bool Prefix1() {
		return false;
	}
	
	[HarmonyPatch(typeof(VSImGuiFontPostPatcher), nameof(VSImGuiFontPostPatcher.AssetsLoaded))]
	[HarmonyPrefix]
	public static bool Prefix2() {
		return false;
	}
}