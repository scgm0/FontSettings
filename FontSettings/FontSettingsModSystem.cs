using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using SixLabors.Fonts;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace FontSettings;

public class FontSettingsModSystem : ModSystem {
	public static FontCollection CustomFontCollection { get; } = new();
	public static string[] FontNameArray { get; private set; } = [];

	public static string _oldDefaultFontName = ClientSettings.DefaultFontName;
	public static string _oldDecorativeFontName = ClientSettings.DecorativeFontName;

	public static int DefaultFontIndex =>
		CustomFontCollection.TryGet(ClientSettings.DefaultFontName, out var family)
			? CustomFontCollection.Families.ToList().IndexOf(family)
			: 0;

	public static int DecorativeFontIndex =>
		CustomFontCollection.TryGet(ClientSettings.DecorativeFontName, out var family)
			? CustomFontCollection.Families.ToList().IndexOf(family)
			: 0;

	internal static GuiCompositeSettings? _guiCompositeSettings;
	internal static System.Reflection.MethodBase? _onInterfaceOptions;

	public static ICoreClientAPI? CoreClientApi { get; private set; }

	private readonly Harmony _harmony = new("fontsettings");

	public override double ExecuteOrder() { return double.MinValue; }

	public override void StartPre(ICoreAPI api) {
		if (api.ModLoader.IsModEnabled("xskillsgilded")) {
			_harmony.PatchCategory("xSkillGilded");
			api.Logger.Debug("字体设置：已为 xSkillGilded 补丁字体");
		}
	}

	public override void StartClientSide(ICoreClientAPI api) {
		CoreClientApi = api;
		CustomFontCollection.AddSystemFonts();
		var fontAssets = api.Assets.Origins.SelectMany(o => o.GetAssets("fonts")).ToList();
		api.Logger.Notification($"字体设置：加载自定义字体 {fontAssets.Count} 个");
		foreach (var asset in fontAssets) {
			api.Logger.Notification($"字体设置：加载自定义字体 {asset.Location.Path}");
			try {
				using var stream = new MemoryStream(asset.Data);
				CustomFontCollection.Add(stream);
				api.Logger.Notification($"字体设置：加载自定义字体 {asset.Location.Path} 成功");
			} catch (Exception ex) {
				api.Logger.Warning($"字体设置：加载自定义字体 {asset.Location.Path} 失败 - {ex.Message}");
			}
		}

		FontNameArray = CustomFontCollection.Families.Select(f => f.Name).ToArray();

		if (api.ModLoader.IsModEnabled("vsimgui")) {
			ImGuiCompat.ImGuiFontSync();
			api.Logger.Notification("字体设置：已为 vsimgui 补丁字体");
		}

		_harmony.PatchAllUncategorized();
	}

	public override void Dispose() {
		_harmony?.UnpatchAll();
		base.Dispose();
	}

	public static GuiComposer InjectFontSettings(
		GuiComposer composer,
		string vtmlCode,
		CairoFont baseFont,
		ElementBounds bounds,
		string key) {
		composer.AddRichtext(vtmlCode, baseFont, bounds, key);

		if (key != "restartText") {
			return composer;
		}

		var elementBounds1 = ElementBounds.Fixed(0.0, bounds.fixedY, 475.0, 42.0);
		var elementBounds2 = ElementBounds.Fixed(495.0, bounds.fixedY + 4.0, 200.0, 20.0);
		ElementBounds elementBounds3;
		ElementBounds elementBounds4;

		composer
			.AddStaticText(Lang.Get("setting-name-default-font"),
				CairoFont.WhiteSmallishText(),
				elementBounds3 = elementBounds1.BelowCopy(fixedDeltaY: 2.0))
			.AddDropDown(
				FontNameArray,
				FontNameArray,
				DefaultFontIndex,
				(code, _) => {
					if (ClientSettings.DefaultFontName == code) {
						return;
					}

					_oldDefaultFontName = ClientSettings.DefaultFontName;
					ClientSettings.DefaultFontName = GuiStyle.StandardFontName = code;
					_onInterfaceOptions?.Invoke(_guiCompositeSettings, [true]);
					ImGuiCompat.ImGuiFontSync();
				},
				elementBounds4 = elementBounds2.BelowCopy(fixedDeltaY: 17.0).WithFixedSize(330.0, 30.0),
				"defaultFontName")
			.AddStaticText(Lang.Get("setting-name-decorative-font"),
				CairoFont.WhiteSmallishText(),
				elementBounds3.BelowCopy(fixedDeltaY: 1.0))
			.AddDropDown(
				FontNameArray,
				FontNameArray,
				DecorativeFontIndex,
				(code, _) => {
					if (ClientSettings.DecorativeFontName == code) {
						return;
					}

					_oldDecorativeFontName = ClientSettings.DecorativeFontName;
					ClientSettings.DecorativeFontName = GuiStyle.DecorativeFontName = code;
					_onInterfaceOptions?.Invoke(_guiCompositeSettings, [true]);
					ImGuiCompat.ImGuiFontSync();
				},
				elementBounds4.BelowCopy(fixedDeltaY: 15.0).WithFixedSize(330.0, 30.0),
				"decorativeFontName");

		composer.GetDropDown("defaultFontName").listMenu.MaxHeight = 200;
		composer.GetDropDown("decorativeFontName").listMenu.MaxHeight = 200;

		return composer;
	}
}

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
	public static void Postfix(GuiCompositeSettings __instance, System.Reflection.MethodBase __originalMethod) {
		FontSettingsModSystem._guiCompositeSettings = __instance;
		FontSettingsModSystem._onInterfaceOptions = __originalMethod;
	}
}

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