using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImGuiController_OpenTK;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using VSImGui;

namespace FontSettings;

public static class ImGuiCompat {

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "GlyphRanges")]
	static private extern ref Dictionary<string, IntPtr> GetGlyphRanges(
		[UnsafeAccessorType("VSImGui.API.FontManager, VSImGui")]
		object? manager);

	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_Loaded")]
	static private extern Dictionary<(string, int), ImFontPtr> GetLoaded(
		[UnsafeAccessorType("VSImGui.API.FontManager, VSImGui")]
		object? manager);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "mMainImGuiRenderer")]
	static private extern ref ImGuiRenderer GetMainImGuiRenderer(ImGuiController controller);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RecreateFontDeviceTexture")]
	static private extern void InvokeRecreateFontDeviceTexture(ImGuiRenderer renderer);

	public static readonly int[] Sizes = [6, 8, 10, 14, 18, 24, 30, 36, 48, 60];

	public static readonly int[] MinSizes = [6, 6, 6, 12, 18, 24, 30, 30, 30, 30];

	public static readonly Dictionary<string, IntPtr> GlyphRanges = GetGlyphRanges(null);
	public static readonly Dictionary<(string, int), ImFontPtr> Loaded = GetLoaded(null);

	static private readonly int MaxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);

	static ImGuiCompat() {
		GlyphRanges["zh-cn"] = ImGui.GetIO().Fonts.GetGlyphRangesChineseFull();
		GlyphRanges["zh-tw"] = ImGui.GetIO().Fonts.GetGlyphRangesChineseFull();
		GlyphRanges["ja"] = ImGui.GetIO().Fonts.GetGlyphRangesJapanese();
		GlyphRanges["ko"] = ImGui.GetIO().Fonts.GetGlyphRangesKorean();
	}

	public static void ImGuiFontSync() {
		if (!TryResolveFont(out var fontPath, out var vsi, out var ranges)) {
			return;
		}

		var io = ImGui.GetIO();
		var atlas = io.Fonts;
		var defaultSize = vsi?.DefaultStyle?.FontSize ?? 18;
		atlas.TexDesiredWidth = MaxTextureSize;

		if (!BuildAtlas(atlas, io, fontPath, ranges, defaultSize, Sizes)) {
			FontSettingsModSystem.CoreClientApi?.Logger?.Warning("字体设置：字体纹理尺寸超出GPU纹理上限，字体尺寸降级为5个");
			BuildAtlas(atlas, io, fontPath, ranges, defaultSize, MinSizes, true);
		}

		RecreateFontDeviceTexture();
		vsi?.DefaultStyle?.FontName = ClientSettings.DefaultFontName;
	}

	static private bool BuildAtlas(
		ImFontAtlasPtr atlas,
		ImGuiIOPtr io,
		string fontPath,
		IntPtr ranges,
		int defaultSize,
		int[] sizes,
		bool noOversample = false) {
		Loaded.Clear();
		atlas.Clear();

		unsafe {
			ImFontConfigPtr cfg = ImGuiNative.ImFontConfig_ImFontConfig();
			try {
				if (noOversample) {
					cfg.OversampleH = 1;
					cfg.OversampleV = 1;
				}

				var defaultFont = atlas.AddFontFromFileTTF(fontPath.AsSpan(), defaultSize, cfg, ranges);
				Loaded.TryAdd((ClientSettings.DefaultFontName, defaultSize), defaultFont);
				io.NativePtr->FontDefault = defaultFont.NativePtr;

				foreach (var size in sizes) {
					if (size != defaultSize) {
						Loaded.TryAdd((ClientSettings.DefaultFontName, size),
							atlas.AddFontFromFileTTF(fontPath.AsSpan(), size, cfg, ranges));
					}
				}
			} finally {
				cfg.Destroy();
			}
		}

		io.Fonts.Build();
		io.Fonts.GetTexDataAsRGBA32(out nint _, out var width, out var height, out _);
		if (width <= MaxTextureSize && height <= MaxTextureSize) {
			return true;
		}

		FontSettingsModSystem.CoreClientApi?.Logger?.Warning(
			$"字体设置：字体纹理尺寸 {width} * {height} 超出GPU纹理上限，宽高单边上限各为{MaxTextureSize}");
		return false;
	}

	static private bool TryResolveFont(out string fontPath, out ImGuiModSystem? vsi, out IntPtr ranges) {
		fontPath = "";
		vsi = null;
		ranges = 0;

		if (!FontSettingsModSystem.CustomFontCollection.TryGet(ClientSettings.DefaultFontName, out var family) ||
			!family.TryGetPaths(out var paths)) {
			return false;
		}

		fontPath = paths.First();
		vsi = FontSettingsModSystem.CoreClientApi!.ModLoader.GetModSystem("VSImGui.ImGuiModSystem") as ImGuiModSystem;
		ranges = GlyphRanges.GetValueOrDefault(Lang.CurrentLocale, ImGui.GetIO().Fonts.GetGlyphRangesDefault());
		return true;
	}

	public static void RecreateFontDeviceTexture() {
		var controller = ImGuiControllerSingletonPatch.Instance;
		if (controller is null) {
			FontSettingsModSystem.CoreClientApi?.Logger?.Warning("字体设置：ImGuiController单例不可用，跳过字体纹理重建");
			return;
		}

		var renderer = GetMainImGuiRenderer(controller);
		InvokeRecreateFontDeviceTexture(renderer);
	}
}