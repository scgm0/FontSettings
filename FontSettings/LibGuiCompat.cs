using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using Vintagestory.Client.NoObf;

namespace FontSettings;

public static class LibGuiCompat {
	public static Dictionary<string, SKTypeface> SkiaCustomTypefaces { get; } = new(StringComparer.OrdinalIgnoreCase);
	public static SKTypeface[] FastTypefaceArray { get; set; } = [];

	public static readonly string[] HardcodedCjkFonts = [
		"Microsoft YaHei", "SimHei", "SimSun", "PingFang SC", "STHeiti",
		"Noto Sans CJK SC", "WenQuanYi Micro Hei", "Microsoft JhengHei", "Malgun Gothic"
	];

	public static void LoadFont(byte[] data) {
		try {
			using var skData = SKData.CreateCopy(data);
			var skTypeface = SKTypeface.FromData(skData);

			if (skTypeface is { FamilyName.Length: > 0 }) {
				SkiaCustomTypefaces[skTypeface.FamilyName] = skTypeface;
				FastTypefaceArray = [.. SkiaCustomTypefaces.Values];
			}
		} catch (Exception ex) {
			FontSettingsModSystem.CoreClientApi?.Logger.Warning($"字体设置：加载SkiaSharp字体失败：{ex.Message}");
		}
	}

	public static void SyncFont() {
		if (!FontSettingsModSystem.CustomFontCollection.TryGet(ClientSettings.DefaultFontName, out var family) ||
			!family.TryGetPaths(out var paths)) {
			return;
		}

		foreach (var path in paths) {
			LoadFont(File.ReadAllBytes(path));
		}
	}
}