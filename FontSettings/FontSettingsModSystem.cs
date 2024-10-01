using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SixLabors.Fonts;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace FontSettings;

public class FontSettingsModSystem : ModSystem {
	public static string[] FontNameArray =>
		SystemFonts.Families
			.Select(family => family.Name).ToArray();

	static private string _oldDefaultFontName = ClientSettings.DefaultFontName;
	static private string _oldDecorativeFontName = ClientSettings.DecorativeFontName;

	public static int DefaultFontIndex =>
		Array.IndexOf(
			FontNameArray
				.Select(f => f.ToLower().Replace(" ", "")).ToArray(),
			ClientSettings.DefaultFontName.ToLower().Replace(" ", ""));

	public static int DecorativeFontIndex =>
		Array.IndexOf(
			FontNameArray
				.Select(f => f.ToLower().Replace(" ", "")).ToArray(),
			ClientSettings.DecorativeFontName.ToLower().Replace(" ", ""));

	static private readonly MethodInfo GuiCompositeSettingsOnInterfaceOptions =
		AccessTools.Method(typeof(GuiCompositeSettings), "OnInterfaceOptions");

	static private readonly MethodInfo SetupContext = AccessTools.Method(typeof(CairoFont), nameof(SetupContext));

	static private readonly MethodInfo AddRichText = AccessTools.Method(typeof(GuiComposerHelpers),
		"AddRichtext",
		[
			typeof(GuiComposer),
			typeof(string),
			typeof(CairoFont),
			typeof(ElementBounds),
			typeof(string)
		]);

	static private readonly MethodInfo GuiCompositeSettingsOnInterfaceOptionsPostFixInfo =
		AccessTools.Method(typeof(FontSettingsModSystem), "GuiCompositeSettingsOnInterfaceOptionsPostFix");

	static private readonly MethodInfo CairoFontSetupContextPreFixInfo =
		AccessTools.Method(typeof(FontSettingsModSystem), "CairoFontSetupContextPreFix");

	static private readonly MethodInfo GuiComposerHelpersAddRichTextPreFixInfo =
		AccessTools.Method(typeof(FontSettingsModSystem), "GuiComposerHelpersAddRichTextPrefix");

	static private ICoreAPI? _api;
	static private GuiCompositeSettings? _guiCompositeSettings;
	static private MethodBase? _onInterfaceOptions;

	public string HarmonyId => Mod.Info.ModID;

	public Harmony HarmonyInstance => new(HarmonyId);

	public override void StartClientSide(ICoreClientAPI? api) {
		_api = api;
		HarmonyInstance.Patch(SetupContext,
			(HarmonyMethod)CairoFontSetupContextPreFixInfo);
		HarmonyInstance.Patch(AddRichText,
			(HarmonyMethod)GuiComposerHelpersAddRichTextPreFixInfo);
		HarmonyInstance.Patch(GuiCompositeSettingsOnInterfaceOptions,
			postfix: (HarmonyMethod)GuiCompositeSettingsOnInterfaceOptionsPostFixInfo);
	}

	public override void Dispose() {
		base.Dispose();
		HarmonyInstance.Unpatch(GuiCompositeSettingsOnInterfaceOptions,
			GuiCompositeSettingsOnInterfaceOptionsPostFixInfo);
		HarmonyInstance.Unpatch(SetupContext,
			CairoFontSetupContextPreFixInfo);
		HarmonyInstance.Unpatch(AddRichText,
			GuiComposerHelpersAddRichTextPreFixInfo);
		HarmonyInstance.Unpatch(GuiCompositeSettingsOnInterfaceOptions,
			GuiCompositeSettingsOnInterfaceOptionsPostFixInfo);
	}

	public static FontFamily GetFontFamily(string name) {
		return SystemFonts.Families.FirstOrDefault(family =>
			family.Name.ToLower().Replace(" ", "") == name.ToLower().Replace(" ", ""));
	}

	public static void GuiCompositeSettingsOnInterfaceOptionsPostFix(
		GuiCompositeSettings __instance,
		MethodBase __originalMethod) {
		_guiCompositeSettings = __instance;
		_onInterfaceOptions = __originalMethod;
	}

	public static void CairoFontSetupContextPreFix(CairoFont __instance, out string __state) {
		__state = __instance.Fontname;
		if (_oldDefaultFontName != GuiStyle.StandardFontName &&
			__instance.Fontname == _oldDefaultFontName) {
			__instance.Fontname = GuiStyle.StandardFontName;
		}

		if (_oldDecorativeFontName != GuiStyle.DecorativeFontName &&
			__instance.Fontname == _oldDecorativeFontName) {
			__instance.Fontname = GuiStyle.DecorativeFontName;
		}
	}

	public static bool GuiComposerHelpersAddRichTextPrefix(
		ref GuiComposer __result,
		GuiComposer composer,
		string vtmlCode,
		CairoFont baseFont,
		ElementBounds bounds,
		string key) {
		if (key != "restartText")
			return true;
		var elementBounds1 = ElementBounds.Fixed(0.0, bounds.fixedY, 475.0, 42.0);
		var elementBounds2 = ElementBounds.Fixed(495.0, bounds.fixedY + 4.0, 200.0, 20.0);
		ElementBounds elementBounds3;
		ElementBounds elementBounds4;
		__result = composer
			.AddInteractiveElement(new GuiElementRichtext(composer.Api,
					VtmlUtil.Richtextify(composer.Api, vtmlCode, baseFont),
					bounds),
				key).AddStaticText(Lang.Get("setting-name-default-font"),
				CairoFont.WhiteSmallishText(),
				elementBounds3 = elementBounds1.BelowCopy(fixedDeltaY: 2.0)).AddDropDown(
				FontNameArray,
				FontNameArray,
				DefaultFontIndex,
				(code, _) => {
					if (ClientSettings.DefaultFontName == code)
						return;
					_oldDefaultFontName = ClientSettings.DefaultFontName;
					ClientSettings.DefaultFontName = GuiStyle.StandardFontName = code;
					_onInterfaceOptions?.Invoke(_guiCompositeSettings,
					[
						true
					]);
				},
				elementBounds4 = elementBounds2.BelowCopy(fixedDeltaY: 17.0).WithFixedSize(330.0, 30.0),
				"defaultFontName")
			.AddStaticText(Lang.Get("setting-name-decorative-font"),
				CairoFont.WhiteSmallishText(),
				elementBounds3.BelowCopy(fixedDeltaY: 1.0)).AddDropDown(FontNameArray,
				FontNameArray,
				DecorativeFontIndex,
				(code, _) => {
					if (ClientSettings.DecorativeFontName == code)
						return;
					_oldDecorativeFontName = ClientSettings.DecorativeFontName;
					ClientSettings.DecorativeFontName = GuiStyle.DecorativeFontName = code;
					_onInterfaceOptions?.Invoke(_guiCompositeSettings,
					[
						true
					]);
				},
				elementBounds4.BelowCopy(fixedDeltaY: 15.0).WithFixedSize(330.0, 30.0),
				"decorativeFontName");
		composer.GetDropDown("defaultFontName").listMenu.MaxHeight = 200;
		composer.GetDropDown("decorativeFontName").listMenu.MaxHeight = 200;
		return false;
	}
}