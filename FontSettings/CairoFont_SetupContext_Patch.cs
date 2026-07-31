using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace FontSettings;

[HarmonyPatch(typeof(CairoFont), "SetupContext")]
public static class CairoFont_SetupContext_Patch {
	[HarmonyPrefix]
	public static void Prefix(CairoFont __instance) {
		if (FontSettingsModSystem._oldDefaultFontName != ClientSettings.DefaultFontName &&
			__instance.Fontname == FontSettingsModSystem._oldDefaultFontName) {
			__instance.Fontname = ClientSettings.DefaultFontName;
		}

		if (FontSettingsModSystem._oldDecorativeFontName != ClientSettings.DecorativeFontName &&
			__instance.Fontname == FontSettingsModSystem._oldDecorativeFontName) {
			__instance.Fontname = ClientSettings.DecorativeFontName;
		}
	}
}