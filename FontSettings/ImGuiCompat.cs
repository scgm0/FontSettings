using System;
using System.Linq;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using Vintagestory.Client.NoObf;

namespace FontSettings;

public class ImGuiCompat {

	public unsafe void ImGuiFontSync() {
		if (!FontSettingsModSystem.GetFontFamily(ClientSettings.DefaultFontName).TryGetPaths(out var paths)) return;
		var io = ImGui.GetIO();
		var f = io.Fonts.AddFontFromFileTTF(filename: paths.First(),
			ImGui.GetFontSize(),
			font_cfg: null,
			glyph_ranges: io.Fonts.GetGlyphRangesChineseFull());
		io.NativePtr->FontDefault = f.NativePtr;
		io.Fonts.Build();
		RecreateFontDeviceTexture();
	}

	public void RecreateFontDeviceTexture() {
		var io = ImGui.GetIO();
		io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out _);

		var mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

		var prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
		GL.ActiveTexture(TextureUnit.Texture0);
		var prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

		var mFontTexture = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, mFontTexture);
		GL.TexStorage2D(TextureTarget2d.Texture2D,
			mips,
			SizedInternalFormat.Rgba8,
			width,
			height);

		GL.TexSubImage2D(TextureTarget.Texture2D,
			0,
			0,
			0,
			width,
			height,
			PixelFormat.Bgra,
			PixelType.UnsignedByte,
			pixels);

		GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

		GL.TexParameter(TextureTarget.Texture2D,
			TextureParameterName.TextureWrapS,
			(int)TextureWrapMode.Repeat);
		GL.TexParameter(TextureTarget.Texture2D,
			TextureParameterName.TextureWrapT,
			(int)TextureWrapMode.Repeat);

		GL.TexParameter(TextureTarget.Texture2D,
			TextureParameterName.TextureMaxLevel,
			mips - 1);

		GL.TexParameter(TextureTarget.Texture2D,
			TextureParameterName.TextureMagFilter,
			(int)TextureMagFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D,
			TextureParameterName.TextureMinFilter,
			(int)TextureMinFilter.Linear);

		GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
		GL.ActiveTexture((TextureUnit)prevActiveTexture);

		io.Fonts.SetTexID(mFontTexture);

		io.Fonts.ClearTexData();
	}
}