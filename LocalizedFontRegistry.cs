using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MiniRealisticAirways;

/// <summary>
/// Resolves the game's localized TMP assets for Mod-owned text and repairs a
/// missing glyph in the same official font face when the serialized atlas is
/// incomplete. No fallback asset or mixed-font rich-text span is installed.
/// </summary>
internal static class LocalizedFontRegistry
{
	private sealed class LocaleFontProfile
	{
		internal readonly string LocalePrefix;
		internal readonly string HudRegularName;
		internal readonly string HudRegularAlternateName;
		internal readonly string BoldName;
		internal readonly string RegularName;
		internal readonly string SourceBoldName;
		internal readonly string SourceRegularName;
		internal readonly bool RequiresRuntimePrimaryAtlas;

		internal LocaleFontProfile(string localePrefix, string hudRegularName, string hudRegularAlternateName, string boldName, string regularName, string sourceBoldName, string sourceRegularName, bool requiresRuntimePrimaryAtlas)
		{
			LocalePrefix = localePrefix;
			HudRegularName = hudRegularName;
			HudRegularAlternateName = hudRegularAlternateName;
			BoldName = boldName;
			RegularName = regularName;
			SourceBoldName = sourceBoldName;
			SourceRegularName = sourceRegularName;
			RequiresRuntimePrimaryAtlas = requiresRuntimePrimaryAtlas;
		}
	}

	private static readonly LocaleFontProfile[] Profiles =
	{
		new LocaleFontProfile("zh-Hant", "SourceHanSansTC-Regular SDF", "NotoSansTC-Regular SDF", "SourceHanSansTC-Bold SDF", "SourceHanSansTC-Regular SDF", "SourceHanSansTC-Bold", "SourceHanSansTC-Regular", true),
		new LocaleFontProfile("zh", "SourceHanSansSC-Regular SDF", "NotoSansSC-Regular SDF", "SourceHanSansSC-Bold-2 SDF", "SourceHanSansSC-Regular SDF", "SourceHanSansSC-Bold-2", "SourceHanSansSC-Regular", true),
		// The official Japanese asset table points every role at
		// SourceHanSansJP-Bold. Mod role selection therefore uses the game's
		// original NotoSansJP-Regular source for body text and the official Bold
		// source for title, heading and button roles.
		new LocaleFontProfile("ja", "NotoSansJP-Regular SDF", "SourceHanSansJP-Regular SDF", "SourceHanSansJP-Bold SDF", "NotoSansJP-Regular SDF", "SourceHanSansJP-Bold", "NotoSansJP-Regular", true),
		new LocaleFontProfile("ko", "NotoSansKR-Regular SDF", null, "NotoSansKR-Bold SDF", "NotoSansKR-Regular SDF", "NotoSansKR-Bold", "NotoSansKR-Regular", true),
		new LocaleFontProfile("ar", "NotoSansArabic-Regular SDF", null, "NotoSansArabic-Bold SDF", "NotoSansArabic-Regular SDF", "NotoSansArabic-Bold", "NotoSansArabic-Regular", true),
		new LocaleFontProfile("", "Helvetica SDF", null, "Helvetica-Bold SDF", "Helvetica SDF", "Helvetica-Bold", "Helvetica", false)
	};

	private static TMP_FontAsset[] fontAssets_;
	private static int snapshotRevision_ = -1;
	private static string snapshotLocale_;
	private static TMP_FontAsset hudFont_;
	private static TMP_FontAsset hudBoldFont_;
	private static readonly HashSet<int> initializedFonts_ = new HashSet<int>();
	// 缓存键只含字体来源与角色，不含语言：同一官方字体（如 Helvetica）服务的所有
	// 拉丁语言共享同一张动态图集。此前按语言分键并在每次切换后退役，导致每个来回
	// 都重新分配/释放 2048×2048 图集（约 32MB 原生抖动），多轮循环后碎片化增长。
	private static readonly Dictionary<string, TMP_FontAsset> runtimeFonts_ = new Dictionary<string, TMP_FontAsset>(StringComparer.Ordinal);
	private static readonly Dictionary<string, int> runtimeFontUsedFrame_ = new Dictionary<string, int>(StringComparer.Ordinal);
	private static readonly HashSet<int> registeredFonts_ = new HashSet<int>();
	private static readonly HashSet<string> warningKeys_ = new HashSet<string>(StringComparer.Ordinal);

	// 退役上限：未被活动文本引用的运行时字体中，保留最近使用的 N 个。2 个字体约
	// 覆盖一个语言的 Regular+Bold 双角色，往返切换两到三个语言都能全量命中缓存。
	private const int MaxIdleRuntimeFonts = 4;

	internal static void PrepareLocale(string localeCode)
	{
		EnsureSnapshot();
		LocaleFontProfile profile = GetProfile();
		string warmupText = ChineseTypography.StripRichText(ModLocalization.GetTypographyWarmupText());
		if (ModLocalization.IsRtl && !string.IsNullOrEmpty(warmupText))
		{
			warmupText = ArabicTextFormatter.ShapeForFont(warmupText);
		}

		TMP_FontAsset regular = PrepareRoleFont(profile.RegularName, profile.SourceRegularName, warmupText, profile.RequiresRuntimePrimaryAtlas);
		TMP_FontAsset bold = PrepareRoleFont(profile.BoldName, profile.SourceBoldName, warmupText, profile.RequiresRuntimePrimaryAtlas);
		LinkBoldTypeface(regular, bold);
		if (regular != null && string.Equals(profile.HudRegularName, profile.RegularName, StringComparison.OrdinalIgnoreCase))
		{
			hudFont_ = regular;
		}
		Plugin.Log?.LogDebug("Prepared locale font roles for " + localeCode + ".");
	}

	private static TMP_FontAsset PrepareRoleFont(string expectedName, string sourceName, string visibleText, bool forceRuntime)
	{
		TMP_FontAsset staticFont = ResolveLoadedFont(expectedName, visibleText, null);
		if (!forceRuntime && staticFont != null && Supports(staticFont, visibleText))
		{
			RegisterFont(staticFont);
			return staticFont;
		}
		return GetOrCreateRuntimeFont(sourceName, expectedName, visibleText, staticFont);
	}

	private static void LinkBoldTypeface(TMP_FontAsset regular, TMP_FontAsset bold)
	{
		if (regular == null || bold == null || regular == bold || !IsRuntimeFont(regular))
		{
			return;
		}
		try
		{
			TMP_FontWeightPair[] table = regular.fontWeightTable;
			int boldIndex = (int)FontWeight.Bold / 100;
			if (table == null || boldIndex < 0 || boldIndex >= table.Length)
			{
				WarnOnce("weight-table-missing-" + regular.name, "Runtime font weight table is unavailable for " + regular.name + ".");
				return;
			}
			table[boldIndex].regularTypeface = bold;
		}
		catch (Exception exception)
		{
			WarnOnce("weight-link-error-" + regular.name, "Could not link official Bold typeface for " + regular.name + ": " + exception.GetBaseException().Message);
		}
	}

	private static bool IsRuntimeFont(TMP_FontAsset font)
	{
		return font != null && font.name.StartsWith("MiniRealisticAirways.", StringComparison.Ordinal);
	}

	internal static TMP_FontAsset ApplyHud(TMP_Text label, string visibleText, string context, bool bold = false)
	{
		if (label == null)
		{
			return null;
		}

		TMP_FontAsset font = GetHudFont(bold);
		string text = visibleText ?? string.Empty;
		LocaleFontProfile profile = GetProfile();
		if (font == null || !Supports(font, text))
		{
			// Several official HUD assets are dynamic and start with an empty
			// primary table. Do not leave a box on screen and do not add a second
			// typeface as a fallback: build the same-face primary atlas from the
			// game's raw Regular font and cache it for this locale.
			string sourceName = bold ? profile.SourceBoldName : profile.SourceRegularName;
			string roleName = bold ? profile.BoldName : profile.HudRegularName;
			TMP_FontAsset repaired = GetOrCreateRuntimeFont(sourceName, roleName, text, font);
			if (repaired != null)
			{
				font = repaired;
				if (bold)
				{
					hudBoldFont_ = repaired;
				}
				else
				{
					hudFont_ = repaired;
				}
			}
		}
		if (font == null)
		{
			Audit(label.font, text, context);
			return null;
		}

		if (label.font != font)
		{
			label.font = font;
		}
		if (font.material != null && label.fontSharedMaterial != font.material)
		{
			label.fontSharedMaterial = font.material;
		}
		label.fontStyle = FontStyles.Normal;
		label.fontWeight = FontWeight.Regular;
		Audit(font, text, context);
		return font;
	}

	/// <summary>
	/// Applies one Mod-owned UI role. Typeface and weight are explicit role
	/// properties; the current label font is never used to infer the target
	/// language or weight because it may still belong to the previous locale.
	/// </summary>
	internal static void ApplyStock(TMP_Text label, string visibleText, ChineseTypography.StockTextRole role)
	{
		if (label == null)
		{
			return;
		}

		string text = visibleText ?? string.Empty;
		LocaleFontProfile profile = GetProfile();
		bool boldRole = IsBoldRole(role);
		string expectedName = boldRole ? profile.BoldName : profile.RegularName;
		string sourceName = boldRole ? profile.SourceBoldName : profile.SourceRegularName;

		TMP_FontAsset materialTemplate = ResolveLoadedFont(expectedName, string.Empty, label.font);
		TMP_FontAsset resolved = ResolveLoadedFont(expectedName, text, label.font);
		if (profile.RequiresRuntimePrimaryAtlas || resolved == null || !Supports(resolved, text))
		{
			resolved = GetOrCreateRuntimeFont(sourceName, expectedName, text, materialTemplate ?? label.font);
		}

		if (resolved != null && label.font != resolved)
		{
			label.font = resolved;
			if (resolved.material != null)
			{
				label.fontSharedMaterial = resolved.material;
			}
		}

		if (resolved != null)
		{
			// The physical face already represents the requested weight. Keeping
			// TMP's Bold flag would apply the shader's synthetic dilation a second
			// time, which is especially visible on Japanese text.
			label.fontStyle &= ~FontStyles.Bold;
			label.fontWeight = FontWeight.Regular;
		}

		Audit(label.font, text, RoleContext(role));
	}

	private static bool IsBoldRole(ChineseTypography.StockTextRole role)
	{
		if (role == ChineseTypography.StockTextRole.QrhButton || role == ChineseTypography.StockTextRole.ModalTitle || role == ChineseTypography.StockTextRole.ModalButton || role == ChineseTypography.StockTextRole.SettingsLabel)
		{
			return true;
		}
		if (role == ChineseTypography.StockTextRole.ModalHeading)
		{
			return true;
		}
		return false;
	}

	private static string RoleContext(ChineseTypography.StockTextRole role)
	{
		switch (role)
		{
			case ChineseTypography.StockTextRole.QrhButton:
				return "QRH button";
			case ChineseTypography.StockTextRole.ModalTitle:
				return "tutorial modal title";
			case ChineseTypography.StockTextRole.ModalHeading:
				return "tutorial modal heading";
			case ChineseTypography.StockTextRole.ModalDescription:
				return "tutorial modal description";
			case ChineseTypography.StockTextRole.ModalButton:
				return "tutorial modal button";
			case ChineseTypography.StockTextRole.SettingsLabel:
				return "settings label";
			default:
				return "stock UI text";
		}
	}

	internal static bool Audit(TMP_FontAsset font, string visibleText, string context)
	{
		if (font == null || string.IsNullOrEmpty(visibleText))
		{
			return true;
		}

		if (Supports(font, visibleText))
		{
			return true;
		}

		try
		{
			if (font.HasCharacters(visibleText, out uint[] missing, searchFallbacks: false, tryAddCharacter: false))
			{
				return true;
			}
			LogMissing(font, missing, context);
		}
		catch (Exception exception)
		{
			WarnOnce("audit-error-" + font.name + "-" + context, "Font glyph audit failed for " + font.name + " (" + context + "): " + exception.GetBaseException().Message);
		}
		return false;
	}

	private static bool Supports(TMP_FontAsset font, string visibleText)
	{
		if (font == null || string.IsNullOrEmpty(visibleText))
		{
			return true;
		}
		try
		{
			PrepareFont(font);
			return font.HasCharacters(visibleText, out _, searchFallbacks: false, tryAddCharacter: false);
		}
		catch
		{
			return false;
		}
	}

	private static TMP_FontAsset ResolveLoadedFont(string expectedName, string text, TMP_FontAsset current)
	{
		EnsureSnapshot();
		TMP_FontAsset best = null;
		int bestCharacterCount = -1;
		for (int i = 0; i < fontAssets_.Length; i++)
		{
			TMP_FontAsset candidate = fontAssets_[i];
			if (candidate == null || !string.Equals(candidate.name, expectedName, StringComparison.OrdinalIgnoreCase) || candidate.material == null || candidate.material.shader == null)
			{
				continue;
			}
			PrepareFont(candidate);
			int count = candidate.characterLookupTable == null ? 0 : candidate.characterLookupTable.Count;
			if (Supports(candidate, text) && count >= bestCharacterCount)
			{
				best = candidate;
				bestCharacterCount = count;
			}
			else if (best == null && count > bestCharacterCount)
			{
				best = candidate;
				bestCharacterCount = count;
			}
		}

		if (best != null)
		{
			return best;
		}
		if (current != null && string.Equals(current.name, expectedName, StringComparison.OrdinalIgnoreCase))
		{
			return current;
		}
		return null;
	}

	private static TMP_FontAsset GetOrCreateRuntimeFont(string sourceName, string expectedName, string visibleText, TMP_FontAsset template)
	{
		if (string.IsNullOrEmpty(sourceName))
		{
			return null;
		}
		string key = sourceName + "|" + expectedName;
		if (!runtimeFonts_.TryGetValue(key, out TMP_FontAsset runtime) || runtime == null)
		{
			Font source = FindSourceFont(sourceName);
			if (source == null)
			{
				WarnOnce("source-font-missing-" + sourceName, "Official source font is unavailable; no fallback will be used: " + sourceName);
				return null;
			}
			try
			{
				// The atlas is generated from the game's original Font at runtime;
				// only current Mod text is inserted, so no multi-megabyte font file
				// is embedded in the DLL.
				runtime = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
				if (runtime == null)
				{
					WarnOnce("runtime-font-create-" + sourceName, "Could not create an official runtime TMP atlas: " + sourceName);
					return null;
				}
				runtime.name = "MiniRealisticAirways." + expectedName + ".Runtime";
				runtime.hideFlags = HideFlags.HideAndDontSave;
				try
				{
					runtime.fallbackFontAssetTable = new List<TMP_FontAsset>();
				}
				catch
				{
					// A null fallback table is also acceptable; do not block the
					// primary same-face atlas if this Unity version rejects the set.
				}
				CopyMaterial(template, runtime);
				PrepareFont(runtime);
				RegisterFont(runtime);
				runtimeFonts_[key] = runtime;
				Plugin.Log?.LogInfo("Created same-face runtime TMP atlas for " + sourceName + " (role font " + expectedName + ").");
			}
			catch (Exception exception)
			{
				WarnOnce("runtime-font-create-error-" + sourceName, "Official runtime TMP atlas creation failed for " + sourceName + ": " + exception.GetBaseException().Message);
				return null;
			}
		}
		runtimeFontUsedFrame_[key] = Time.frameCount;

		// 已支持全文时跳过 TryAddCharacters，避免 CJK 等永远走运行时图集的语言
		// 在每次应用文本时重复做逐字符原生检查。
		bool supported = Supports(runtime, visibleText);
		if (!string.IsNullOrEmpty(visibleText) && !supported)
		{
			try
			{
				// Mutates only the same-face primary atlas; it is not a fallback
				// lookup and does not combine two typefaces in one label.
				runtime.TryAddCharacters(visibleText, includeFontFeatures: true);
				PrepareFont(runtime);
				supported = Supports(runtime, visibleText);
			}
			catch (Exception exception)
			{
				WarnOnce("runtime-font-add-error-" + runtime.name, "Could not add glyphs to same-face runtime atlas " + runtime.name + ": " + exception.GetBaseException().Message);
			}
		}
		if (!supported)
		{
			Audit(runtime, visibleText, "same-face runtime atlas");
		}
		return runtime;
	}

	private static void CopyMaterial(TMP_FontAsset template, TMP_FontAsset runtime)
	{
		if (template == null || template.material == null || runtime == null || runtime.material == null)
		{
			return;
		}
		try
		{
			Texture atlas = runtime.atlasTextures != null && runtime.atlasTextures.Length > 0 ? runtime.atlasTextures[0] : null;
			runtime.material.CopyPropertiesFromMaterial(template.material);
			if (atlas != null)
			{
				runtime.material.mainTexture = atlas;
			}
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Stock TMP material copy was skipped: " + exception.GetBaseException().Message);
		}
	}

	private static Font FindSourceFont(string sourceName)
	{
		Font source = Resources.Load<Font>("fonts & materials/fonts/" + sourceName);
		if (source != null)
		{
			return source;
		}
		Font[] sources = Resources.FindObjectsOfTypeAll<Font>();
		for (int i = 0; i < sources.Length; i++)
		{
			if (sources[i] != null && string.Equals(sources[i].name, sourceName, StringComparison.OrdinalIgnoreCase))
			{
				return sources[i];
			}
		}
		return null;
	}

	private static void PrepareFont(TMP_FontAsset font)
	{
		if (font == null || !initializedFonts_.Add(font.GetInstanceID()))
		{
			return;
		}
		try
		{
			font.ReadFontAssetDefinition();
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("TMP font lookup initialization failed for " + font.name + ": " + exception.GetBaseException().Message);
		}
	}

	private static void RegisterFont(TMP_FontAsset font)
	{
		if (font == null || !registeredFonts_.Add(font.GetInstanceID()))
		{
			return;
		}
		try
		{
			MaterialReferenceManager.AddFontAsset(font);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("TMP material reference registration failed for " + font.name + ": " + exception.GetBaseException().Message);
		}
	}

	internal static TMP_FontAsset GetHudFont(bool bold = false)
	{
		EnsureSnapshot();
		if (bold && hudBoldFont_ != null)
		{
			return hudBoldFont_;
		}
		if (!bold && hudFont_ != null)
		{
			return hudFont_;
		}

		LocaleFontProfile profile = GetProfile();
		string expectedName = bold ? profile.BoldName : profile.HudRegularName;
		TMP_FontAsset resolved = FindUsableByName(expectedName);
		if (!bold && resolved == null && !string.IsNullOrEmpty(profile.HudRegularAlternateName))
		{
			resolved = FindUsableByName(profile.HudRegularAlternateName);
		}
		if (resolved != null)
		{
			RegisterFont(resolved);
			if (bold)
			{
				hudBoldFont_ = resolved;
			}
			else
			{
				hudFont_ = resolved;
			}
			return resolved;
		}

		WarnOnce("missing-hud-font-" + ModLocalization.CurrentLocaleCode + (bold ? "-bold" : "-regular"), "Game " + (bold ? "Bold" : "Regular") + " HUD TMP font is not loaded: " + expectedName);
		return null;
	}

	private static TMP_FontAsset FindUsableByName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		TMP_FontAsset best = null;
		int bestCount = -1;
		for (int i = 0; i < fontAssets_.Length; i++)
		{
			TMP_FontAsset candidate = fontAssets_[i];
			if (candidate == null || !string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase) || candidate.material == null || candidate.material.shader == null)
			{
				continue;
			}
			PrepareFont(candidate);
			int count = candidate.characterLookupTable == null ? 0 : candidate.characterLookupTable.Count;
			if (count > bestCount)
			{
				best = candidate;
				bestCount = count;
			}
		}
		return best;
	}

	private static LocaleFontProfile GetProfile()
	{
		string locale = ModLocalization.CurrentLocaleCode ?? string.Empty;
		for (int i = 0; i < Profiles.Length; i++)
		{
			if (Profiles[i].LocalePrefix.Length != 0 && locale.StartsWith(Profiles[i].LocalePrefix, StringComparison.OrdinalIgnoreCase))
			{
				return Profiles[i];
			}
		}
		return Profiles[Profiles.Length - 1];
	}

	private static void EnsureSnapshot()
	{
		string locale = ModLocalization.CurrentLocaleCode ?? string.Empty;
		int revision = ModLocalization.Revision;
		if (fontAssets_ != null && snapshotRevision_ == revision && string.Equals(snapshotLocale_, locale, StringComparison.Ordinal))
		{
			return;
		}
		// Do not destroy the previous locale's runtime atlas here. Labels may
		// still reference it while Unity's own localization callbacks are settling.
		// The coordinator retires unreferenced old atlases only after the new
		// locale has been committed and all Mod labels have been rebound.
		fontAssets_ = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
		snapshotRevision_ = revision;
		snapshotLocale_ = locale;
		hudFont_ = null;
		hudBoldFont_ = null;
	}

	/// <summary>
	/// 提交一次语言后回收不再使用的运行时图集：仍被活动文本引用的一律保留；
	/// 未被引用的按最近使用排序保留 <see cref="MaxIdleRuntimeFonts"/> 个，其余销毁。
	/// 往返切换语言的场景因此全部命中缓存，不再反复重建图集。
	/// </summary>
	internal static void RetireUnreferencedRuntimeFonts()
	{
		if (runtimeFonts_.Count == 0)
		{
			return;
		}

		TMP_Text[] labels = Resources.FindObjectsOfTypeAll<TMP_Text>();
		List<string> idleKeys = new List<string>();
		foreach (KeyValuePair<string, TMP_FontAsset> pair in runtimeFonts_)
		{
			if (pair.Value == null || !IsFontReferencedByLiveText(pair.Value, labels))
			{
				idleKeys.Add(pair.Key);
			}
		}
		if (idleKeys.Count <= MaxIdleRuntimeFonts)
		{
			return;
		}
		// 最近使用的排后面，销毁最旧的一批。
		idleKeys.Sort((a, b) =>
		{
			int frameA = runtimeFontUsedFrame_.TryGetValue(a, out int valueA) ? valueA : -1;
			int frameB = runtimeFontUsedFrame_.TryGetValue(b, out int valueB) ? valueB : -1;
			return frameA.CompareTo(frameB);
		});
		int removeCount = idleKeys.Count - MaxIdleRuntimeFonts;
		List<string> removedNames = new List<string>(removeCount);
		for (int i = 0; i < removeCount; i++)
		{
			string key = idleKeys[i];
			if (runtimeFonts_.TryGetValue(key, out TMP_FontAsset runtime))
			{
				DestroyRuntimeFont(runtime);
				runtimeFonts_.Remove(key);
				runtimeFontUsedFrame_.Remove(key);
				removedNames.Add(runtime == null ? key : runtime.name);
			}
		}
		if (removedNames.Count > 0)
		{
			fontAssets_ = null;
			hudFont_ = null;
			hudBoldFont_ = null;
			Plugin.Log?.LogInfo("Retired " + removedNames.Count + " unused runtime font atlas(es): " + string.Join(", ", removedNames) + ".");
		}
	}

	private static bool IsFontReferencedByLiveText(TMP_FontAsset font, TMP_Text[] labels)
	{
		for (int i = 0; i < labels.Length; i++)
		{
			TMP_Text label = labels[i];
			if (label != null && (label.font == font || label.fontSharedMaterial == font.material))
			{
				return true;
			}
		}
		return false;
	}

	private static void DestroyRuntimeFonts()
	{
		foreach (TMP_FontAsset runtime in runtimeFonts_.Values)
		{
			DestroyRuntimeFont(runtime);
		}
		runtimeFonts_.Clear();
		runtimeFontUsedFrame_.Clear();
	}

	private static void DestroyRuntimeFont(TMP_FontAsset runtime)
	{
		if (runtime == null)
		{
			return;
		}
		int instanceId = runtime.GetInstanceID();
		initializedFonts_.Remove(instanceId);
		registeredFonts_.Remove(instanceId);
		try
		{
			// CreateFontAsset 生成的图集纹理与材质是独立原生资源；只销毁字体壳
			// 会在每次切换语言时各泄漏数 MB 原生内存。
			if (runtime.atlasTextures != null)
			{
				foreach (Texture2D atlasTexture in runtime.atlasTextures)
				{
					if (atlasTexture != null)
					{
						UnityEngine.Object.Destroy(atlasTexture);
					}
				}
			}
			if (runtime.material != null)
			{
				UnityEngine.Object.Destroy(runtime.material);
			}
			UnityEngine.Object.Destroy(runtime);
		}
		catch
		{
			// Locale changes can coincide with scene teardown; the object may
			// already have been destroyed by Unity.
		}
	}

	private static void LogMissing(TMP_FontAsset font, uint[] missing, string context)
	{
		if (missing == null || missing.Length == 0)
		{
			WarnOnce("missing-empty-" + font.name + "-" + context, "Font " + font.name + " reported missing glyphs without code points (" + context + ").");
			return;
		}

		List<string> values = new List<string>(missing.Length);
		for (int i = 0; i < missing.Length; i++)
		{
			uint codePoint = missing[i];
			string character = codePoint <= 0x10FFFF ? char.ConvertFromUtf32((int)codePoint) : "?";
			values.Add(character + " U+" + codePoint.ToString("X4"));
		}
		WarnOnce("missing-" + font.name + "-" + context + "-" + string.Join(",", missing), "Font " + font.name + " is missing " + missing.Length + " glyph(s) for " + context + ": " + string.Join(", ", values));
	}

	private static void WarnOnce(string key, string message)
	{
		if (warningKeys_.Add(key))
		{
			Plugin.Log?.LogWarning(message);
		}
	}

	internal static void Reset()
	{
		DestroyRuntimeFonts();
		fontAssets_ = null;
		snapshotRevision_ = -1;
		snapshotLocale_ = null;
		hudFont_ = null;
		hudBoldFont_ = null;
		initializedFonts_.Clear();
		registeredFonts_.Clear();
		warningKeys_.Clear();
	}

	internal static void ResetSceneState()
	{
		// Menu controls can survive a Single scene transition. Keep runtime faces
		// that are still referenced by live TMP labels, while retiring idle
		// atlases beyond the recent-use cap. The next locale commit rebuilds
		// the Resources snapshot without forcing a surviving atlas to be created
		// again.
		RetireUnreferencedRuntimeFonts();
		fontAssets_ = null;
		snapshotRevision_ = -1;
		snapshotLocale_ = null;
		hudFont_ = null;
		hudBoldFont_ = null;
	}
}
