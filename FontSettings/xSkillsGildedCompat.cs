using HarmonyLib;

namespace FontSettings;

[HarmonyPatchCategory("xSkillGilded")]
public static class XSkillsGildedCompat {
	[HarmonyPatch("xSkillGilded.VSImGuiFontPatcher", "StartPre")]
	[HarmonyPrefix]
	public static bool Prefix1() {
		return false;
	}

	[HarmonyPatch("xSkillGilded.VSImGuiFontPostPatcher", "AssetsLoaded")]
	[HarmonyPrefix]
	public static bool Prefix2() {
		return false;
	}
}