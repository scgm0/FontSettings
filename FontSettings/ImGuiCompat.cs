using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using VSImGui;

namespace FontSettings;

public static class ImGuiCompat {

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "GlyphRanges")]
	static private extern ref Dictionary<string, IntPtr> GetGlyphRanges([UnsafeAccessorType("VSImGui.API.FontManager, VSImGui")] object? manager);

	public static readonly Dictionary<string, IntPtr> GlyphRanges = GetGlyphRanges(null);

	static ImGuiCompat() {
		GlyphRanges["zh-tw"] = ImGui.GetIO().Fonts.GetGlyphRangesChineseFull();
		GlyphRanges["ja"] = ImGui.GetIO().Fonts.GetGlyphRangesJapanese();
		GlyphRanges["ko"] = ImGui.GetIO().Fonts.GetGlyphRangesKorean();
	}

	public static void ImGuiFontSync() {
		if (!FontSettingsModSystem.CustomFontCollection.TryGet(ClientSettings.DefaultFontName, out var family) ||
			!family.TryGetPaths(out var paths)) {
			return;
		}

		var vsi = FontSettingsModSystem.CoreClientApi!.ModLoader.GetModSystem("VSImGui.ImGuiModSystem") as ImGuiModSystem;
		vsi!.DefaultStyle!.FontName = ClientSettings.DefaultFontName;

		var ranges = GlyphRanges.GetValueOrDefault(Lang.CurrentLocale, ImGui.GetIO().Fonts.GetGlyphRangesDefault());
		var io = ImGui.GetIO();
		var atlas = io.Fonts;
		var defaultFont = atlas.AddFontFromFileTTF(paths.First(),
			ImGui.GetFontSize(),
			font_cfg: new(),
			glyph_ranges: ranges);
		unsafe {
			io.NativePtr->FontDefault = defaultFont.NativePtr;
		}

		io.Fonts.Build();
		RecreateFontDeviceTexture();
	}

	public static void RecreateFontDeviceTexture() {
		var io = ImGui.GetIO();
		io.Fonts.GetTexDataAsRGBA32(out IntPtr outPixels, out var outWidth, out var outHeight, out var _);
		var levels = (int)Math.Floor(Math.Log((double)Math.Max(outWidth, outHeight), 2.0));
		var integer1 = GL.GetInteger(GetPName.ActiveTexture);
		GL.ActiveTexture(TextureUnit.Texture0);
		var integer2 = GL.GetInteger(GetPName.TextureBinding2D);
		var mFontTexture = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, mFontTexture);
		GL.TexStorage2D(TextureTarget2d.Texture2D, levels, SizedInternalFormat.Rgba8, outWidth, outHeight);
		GL.TexSubImage2D(TextureTarget.Texture2D,
			0,
			0,
			0,
			outWidth,
			outHeight,
			PixelFormat.Bgra,
			PixelType.UnsignedByte,
			outPixels);
		GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 10497);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 10497);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, levels - 1);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.BindTexture(TextureTarget.Texture2D, integer2);
		GL.ActiveTexture((TextureUnit)integer1);
		io.Fonts.SetTexID(mFontTexture);
		io.Fonts.ClearTexData();
	}
}