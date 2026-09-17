using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SkiaSharp;
using Vintagestory.API.Config;

namespace FontSettings;

[HarmonyPatchCategory("gui")]
public static class FontRunSplitterPatch {
	static private string? _lastRawLocale;
	static private string[] _bcp47Cache = ["en"];

	public static string[] GetBcp47() {
		var locale = Lang.CurrentLocale;

		if (_lastRawLocale != locale) {
			_lastRawLocale = locale;

			if (string.IsNullOrEmpty(locale) || string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase)) {
				_bcp47Cache = ["en"];
			} else {
				_bcp47Cache = [locale, "en"];
			}
		}

		return _bcp47Cache;
	}

	[HarmonyPatch("Gui.Rendering.Text.FontRunSplitter", "ResolveTypeface")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> ResolveTypefaceTranspiler(IEnumerable<CodeInstruction> instructions) {
		var matchCharacterMethod = AccessTools.Method(typeof(SKFontManager),
			nameof(SKFontManager.MatchCharacter),
			[
				typeof(string), typeof(int), typeof(int), typeof(SKFontStyleSlant), typeof(string[]), typeof(int)
			]);

		var customFallbackMethod = AccessTools.Method(typeof(FontRunSplitterPatch), nameof(CustomMatchCharacter));

		var patched = false;

		foreach (var instruction in instructions) {
			if (!patched && instruction.Calls(matchCharacterMethod)) {
				yield return new(OpCodes.Call, customFallbackMethod);
				patched = true;
			} else {
				yield return instruction;
			}
		}
	}

	public static SKTypeface? CustomMatchCharacter(
		SKFontManager fontManager,
		string? familyName,
		int weight,
		int width,
		SKFontStyleSlant slant,
		string[]? bcp47,
		int codepoint) {
		foreach (var customTypeface in LibGuiCompat.FastTypefaceArray) {
			if (customTypeface.GetGlyph(codepoint) != 0) {
				return customTypeface;
			}
		}

		var customBcp47 = GetBcp47();
		var systemFallback = fontManager.MatchCharacter(familyName, weight, width, slant, customBcp47, codepoint);
		if (systemFallback?.GetGlyph(codepoint) != 0) {
			return systemFallback;
		}

		systemFallback = fontManager.MatchCharacter(familyName, weight, width, slant, null, codepoint);
		if (systemFallback?.GetGlyph(codepoint) != 0) {
			return systemFallback;
		}

		var fontStyle = new SKFontStyle(weight, width, slant);
		foreach (var family in LibGuiCompat.HardcodedCjkFonts) {
			var hardFont = fontManager.MatchFamily(family, fontStyle);
			if (hardFont?.GetGlyph(codepoint) != 0) {
				return hardFont;
			}
		}

		return null;
	}
}