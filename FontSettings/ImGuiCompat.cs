using System;
using System.Linq;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using SixLabors.Fonts;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using VSImGui;

namespace FontSettings;

public class ImGuiCompat(ICoreClientAPI api) {

	public void ImGuiFontSync() {
		if (!SystemFonts.TryGet(ClientSettings.DefaultFontName, out var family) || !family.TryGetPaths(out var paths)) return;
		var vsi = api.ModLoader.GetModSystem("VSImGui.ImGuiModSystem") as ImGuiModSystem;
		vsi!.DefaultStyle!.FontName = ClientSettings.DefaultFontName;
		var io = ImGui.GetIO();
		var atlas = io.Fonts;
		var defaultFont = atlas.AddFontFromFileTTF(paths.First(), ImGui.GetFontSize(), font_cfg: null, glyph_ranges: io.Fonts.GetGlyphRangesChineseFull());
		unsafe {
			io.NativePtr->FontDefault = defaultFont.NativePtr;
		}
		io.Fonts.Build();
		RecreateFontDeviceTexture();
	}

	public void RecreateFontDeviceTexture() {
		var io = ImGui.GetIO();
		io.Fonts.GetTexDataAsRGBA32(out IntPtr outPixels, out var outWidth, out var outHeight, out var _);
		var levels = (int) Math.Floor(Math.Log((double) Math.Max(outWidth, outHeight), 2.0));
		var integer1 = GL.GetInteger(GetPName.ActiveTexture);
		GL.ActiveTexture(TextureUnit.Texture0);
		var integer2 = GL.GetInteger(GetPName.TextureBinding2D);
		var mFontTexture = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, mFontTexture);
		GL.TexStorage2D(TextureTarget2d.Texture2D, levels, SizedInternalFormat.Rgba8, outWidth, outHeight);
		GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, outWidth, outHeight, PixelFormat.Bgra, PixelType.UnsignedByte, outPixels);
		GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 10497);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 10497);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, levels - 1);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.BindTexture(TextureTarget.Texture2D, integer2);
		GL.ActiveTexture((TextureUnit) integer1);
		io.Fonts.SetTexID((IntPtr) mFontTexture);
		io.Fonts.ClearTexData();
	}
}