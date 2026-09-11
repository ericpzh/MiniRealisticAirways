using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MiniRealisticAirways;

/// <summary>
/// Owns the mod's player-facing text while using the game's selected locale.
/// The catalog is kept in the DLL so the mod does not modify Addressables or
/// depend on MapPort-owned bundles. Lookups are cheap; locale work is event
/// driven and never performed by a scene-wide scan.
/// </summary>
internal static partial class ModLocalization
{
	private const string DefaultLocale = "en";

	private static readonly string[] SupportedLocaleCodes =
	{
		"en", "zh-Hans", "zh-Hant", "ja", "ko", "ar", "nl", "fr", "de", "pl", "pt", "ru", "es", "tr", "uk"
	};

	private static readonly Dictionary<string, Dictionary<string, string>> Catalog = BuildCatalog();

	private static bool initialized_;

	private static string currentLocaleCode_ = DefaultLocale;

	private static int revision_;

	private static readonly HashSet<string> MissingKeyWarnings = new HashSet<string>(StringComparer.Ordinal);

	internal static event Action<string> LocaleChanged;

	internal static string CurrentLocaleCode => currentLocaleCode_;

	internal static int Revision => revision_;

	internal static IReadOnlyList<string> SupportedLocales => SupportedLocaleCodes;

	internal static bool IsRtl => string.Equals(currentLocaleCode_, "ar", StringComparison.OrdinalIgnoreCase);

	internal static void Initialize()
	{
		if (initialized_)
		{
			return;
		}
		initialized_ = true;
		try
		{
			LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogWarning("Mod localization subscription failed; using English fallback: " + exception.GetBaseException().Message);
		}
		try
		{
			UpdateLocale(LocalizationSettings.SelectedLocale, notify: false);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogWarning("Mod locale lookup failed; using English fallback: " + exception.GetBaseException().Message);
		}
	}

	internal static string Get(string key)
	{
		Initialize();
		if (string.IsNullOrEmpty(key))
		{
			return string.Empty;
		}
		if (TryGet(currentLocaleCode_, key, out string value) || TryGet(DefaultLocale, key, out value))
		{
			return value;
		}
		if (MissingKeyWarnings.Add(key))
		{
			Plugin.Log?.LogWarning("Missing MiniRealisticAirways localization key: " + key);
		}
		return key;
	}

	internal static string Format(string key, params object[] arguments)
	{
		string template = Get(key);
		try
		{
			return string.Format(CultureInfo.CurrentCulture, template, arguments);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogWarning("Localization format failed for " + key + ": " + exception.GetBaseException().Message);
			return template;
		}
	}

	internal static string GetAltitudeName(AltitudeLevel level)
	{
		return Get("altitude." + level.ToString().ToLowerInvariant());
	}

	internal static string GetWeightName(Weight weight)
	{
		return Get("weight." + weight.ToString().ToLowerInvariant());
	}

	internal static string GetFuelString(int percent)
	{
		return percent >= 0 ? Format("hud.fuel", percent) : Get("hud.fuelInfinite");
	}

	internal static void RefreshSubscribers()
	{
		revision_++;
		LocalizedUiRefreshCoordinator.Request(currentLocaleCode_, revision_);
	}

	/// <summary>
	/// Returns the current locale's visible Mod strings for one-time TMP atlas
	/// preparation. Documentation URLs are deliberately excluded because they
	/// are not rendered as text and would introduce unrelated Latin glyphs into
	/// the Arabic atlas.
	/// </summary>
	internal static string GetTypographyWarmupText()
	{
		Initialize();
		if (!Catalog.TryGetValue(currentLocaleCode_, out Dictionary<string, string> values))
		{
			values = Catalog[DefaultLocale];
		}
		StringBuilder result = new StringBuilder(1024);
		foreach (KeyValuePair<string, string> pair in values)
		{
			if (string.Equals(pair.Key, "tutorial.docsUrl", StringComparison.Ordinal))
			{
				continue;
			}
			// Composite format placeholders are replaced with live values before a
			// label reaches TMP. They are not visible glyphs and some official RTL
			// faces intentionally omit ASCII braces; excluding them keeps the
			// runtime atlas audit focused on text that can actually be rendered.
			result.Append(StripFormatPlaceholders(pair.Value));
			result.Append('\n');
		}
		return result.ToString();
	}

	private static string StripFormatPlaceholders(string text)
	{
		if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0)
		{
			return text ?? string.Empty;
		}
		StringBuilder result = new StringBuilder(text.Length);
		bool insidePlaceholder = false;
		for (int i = 0; i < text.Length; i++)
		{
			char character = text[i];
			if (character == '{')
			{
				insidePlaceholder = true;
				continue;
			}
			if (insidePlaceholder)
			{
				if (character == '}')
				{
					insidePlaceholder = false;
				}
				continue;
			}
			result.Append(character);
		}
		return result.ToString();
	}

	/// <summary>Checks that every shipped locale has the same complete key set as English.</summary>
	internal static bool ValidateCatalog(out string error)
	{
		error = null;
		if (!Catalog.TryGetValue(DefaultLocale, out Dictionary<string, string> english))
		{
			error = "English catalog is missing.";
			return false;
		}
		for (int i = 0; i < SupportedLocaleCodes.Length; i++)
		{
			string code = SupportedLocaleCodes[i];
			if (!Catalog.TryGetValue(code, out Dictionary<string, string> values))
			{
				error = "Locale catalog is missing: " + code;
				return false;
			}
			if (values.Count != english.Count)
			{
				error = "Locale catalog key count differs for " + code + ": " + values.Count + " vs " + english.Count + ".";
				return false;
			}
			foreach (string key in english.Keys)
			{
				if (!values.ContainsKey(key))
				{
					error = "Locale " + code + " is missing key " + key + ".";
					return false;
				}
			}
		}
		return true;
	}

	private static void OnSelectedLocaleChanged(Locale locale)
	{
		try
		{
			// Unity Localization 的事件实测会迟到数秒并成批补发（语言循环探针中
			// 触发 92 次偏差告警）。事件参数可能是早已过时的语言，这里一律改读
			// LocalizationSettings.SelectedLocale 的当前真实值；与现有语言一致时
			// 直接跳过，避免过时事件把 Mod 文本拖回上一个语言。
			Locale actual = LocalizationSettings.SelectedLocale;
			if (actual == null)
			{
				return;
			}
			string nextCode = NormalizeLocaleCode(actual);
			if (string.Equals(nextCode, currentLocaleCode_, StringComparison.Ordinal))
			{
				return;
			}
			UpdateLocale(actual, notify: true);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogWarning("Mod locale change handling failed; retaining " + currentLocaleCode_ + ": " + exception.GetBaseException().Message);
		}
	}

	private static void UpdateLocale(Locale locale, bool notify)
	{
		string nextCode = NormalizeLocaleCode(locale);
		bool changed = !string.Equals(currentLocaleCode_, nextCode, StringComparison.Ordinal);
		currentLocaleCode_ = nextCode;
		if (notify || changed)
		{
			revision_++;
			// Unity's own GameObjectLocalizer callbacks run after the selected-locale
			// event. Defer the Mod event until the coordinator can commit one stable
			// locale, otherwise a new font is asked to render the previous language.
			LocalizedUiRefreshCoordinator.Request(currentLocaleCode_, revision_);
		}
	}

	internal static bool CommitPendingLocale(string localeCode, int revision)
	{
		if (revision != revision_ || !string.Equals(localeCode, currentLocaleCode_, StringComparison.Ordinal))
		{
			return false;
		}
		try
		{
			LocalizedFontRegistry.PrepareLocale(localeCode);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogWarning("Locale font preparation failed for " + localeCode + ": " + exception.GetBaseException().Message);
		}
		NotifyLocaleChanged(localeCode);
		return true;
	}

	/// <summary>
	/// 兜底对账：Unity 冷加载新语言时 SelectedLocaleChanged 可能迟到或成对触发，
	/// 造成 Mod 文本停留在上一语言（实测语言循环下 QRH 按钮宽度/字体滞后的根因）。
	/// 周期调用；发现偏差时按游戏当前真实语言重新走一次提交流程。
	/// </summary>
	internal static bool EnsureMatchesSelectedLocale()
	{
		if (!initialized_)
		{
			return false;
		}
		try
		{
			Locale selected = LocalizationSettings.SelectedLocale;
			if (selected == null)
			{
				return false;
			}
			string actual = NormalizeLocaleCode(selected);
			if (string.Equals(actual, currentLocaleCode_, StringComparison.Ordinal))
			{
				return false;
			}
			Plugin.Log?.LogWarning("Mod locale " + currentLocaleCode_ + " diverged from the game's selected locale " + actual + "; re-syncing.");
			UpdateLocale(selected, notify: true);
			return true;
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Locale re-sync check skipped: " + exception.GetBaseException().Message);
			return false;
		}
	}

	private static void NotifyLocaleChanged(string code)
	{
		Delegate[] subscribers = LocaleChanged?.GetInvocationList();
		if (subscribers == null)
		{
			return;
		}
		for (int i = 0; i < subscribers.Length; i++)
		{
			try
			{
				((Action<string>)subscribers[i])(code);
			}
			catch (Exception exception)
			{
				Action<string> subscriber = (Action<string>)subscribers[i];
				string subscriberName = subscriber.Method == null ? "unknown" : subscriber.Method.DeclaringType + "." + subscriber.Method.Name;
				Plugin.Log?.LogWarning("Mod localization subscriber failed (" + subscriberName + "): " + exception.GetBaseException());
			}
		}
	}

	private static string NormalizeLocaleCode(Locale locale)
	{
		string code = locale == null ? string.Empty : locale.Identifier.Code;
		if (string.IsNullOrEmpty(code) && locale != null)
		{
			code = locale.LocaleName;
		}
		if (string.IsNullOrEmpty(code))
		{
			return DefaultLocale;
		}
		if (code.IndexOf("traditional", StringComparison.OrdinalIgnoreCase) >= 0 || code.IndexOf("繁", StringComparison.Ordinal) >= 0)
		{
			return "zh-Hant";
		}
		if (code.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) >= 0 || code.IndexOf("中文", StringComparison.Ordinal) >= 0)
		{
			return "zh-Hans";
		}
		code = code.Replace('_', '-');
		if (code.StartsWith("zh-hant", StringComparison.OrdinalIgnoreCase) ||
			code.StartsWith("zh-tw", StringComparison.OrdinalIgnoreCase) ||
			code.StartsWith("zh-hk", StringComparison.OrdinalIgnoreCase) ||
			code.StartsWith("zh-mo", StringComparison.OrdinalIgnoreCase))
		{
			return "zh-Hant";
		}
		if (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return "zh-Hans";
		}
		int separator = code.IndexOf('-');
		return separator < 0 ? code.ToLowerInvariant() : code.Substring(0, separator).ToLowerInvariant();
	}

	private static bool TryGet(string localeCode, string key, out string value)
	{
		value = null;
		if (string.IsNullOrEmpty(localeCode) || !Catalog.TryGetValue(localeCode, out Dictionary<string, string> locale))
		{
			return false;
		}
		return locale.TryGetValue(key, out value);
	}

	private static void RegisterLocale(Dictionary<string, Dictionary<string, string>> catalog, string code, params (string Key, string Value)[] entries)
	{
		Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
		for (int i = 0; i < entries.Length; i++)
		{
			values[entries[i].Key] = entries[i].Value;
		}
		catalog[code] = values;
	}

	private static Dictionary<string, Dictionary<string, string>> BuildCatalog()
	{
		Dictionary<string, Dictionary<string, string>> catalog = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		RegisterEnglish(catalog);
		RegisterSimplifiedChinese(catalog);
		RegisterTraditionalChinese(catalog);
		RegisterJapanese(catalog);
		RegisterKorean(catalog);
		RegisterArabic(catalog);
		RegisterDutch(catalog);
		RegisterFrench(catalog);
		RegisterGerman(catalog);
		RegisterPolish(catalog);
		RegisterPortuguese(catalog);
		RegisterRussian(catalog);
		RegisterSpanish(catalog);
		RegisterTurkish(catalog);
		RegisterUkrainian(catalog);
		return catalog;
	}

	private static void RegisterEnglish(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "en",
			("tutorial.title", "Mini Realistic Airways"),
			("tutorial.qrh", "Quick Reference Handbook"),
			("tutorial.next", "Next"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Enable Wind"), ("settings.events", "Enable Events"), ("settings.tcas", "Enable TCAS & GPWS"),
			("hud.wind", "Wind: {0}°"), ("hud.altitudePrefix", "ALT: "), ("hud.speedPrefix", "SPD: "),
			("hud.fuel", "Fuel: {0}%"), ("hud.fuelInfinite", "Fuel: ∞"), ("hud.weight", "Weight: {0}"),
			("altitude.ground", "Ground"), ("altitude.low", "Low"), ("altitude.normal", "Normal"), ("altitude.high", "High"),
			("weight.light", "Light"), ("weight.medium", "Medium"), ("weight.heavy", "Heavy"),
			("engine.mayday", "Mayday, Mayday, Mayday!"),
			("engine.failure", "{0}. We have one engine failure."),
			("engine.return", "We need to return to the field immediately."),
			("tutorial.page.altitude.heading", "Altitude"),
			("tutorial.page.altitude.description", "Aircraft operates in <b><u>low</u></b>, <b><u>normal</u></b>, or <b><u>high</u></b> altitude. The current altitude of an aircraft is displayed in the bottom-left corner of the aircraft.\nAircraft arrives at <b><u>high</u></b> altitude and can only land with <b><u>low</u></b> altitude.\nAircraft takes off at <b><u>low</u></b> altitude and can only depart in <b><u>normal</u></b> or <b><u>high</u></b> altitude.\nPress <b><u>W</u></b> or <b><u>Scroll Up</u></b> to increase the altitude of aircraft/waypoint.\nPress <b><u>S</u></b> or <b><u>Scroll Down</u></b> to decrease the altitude of aircraft/waypoint.\nTerrain will not affect aircraft in <b><u>high</u></b> altitude."),
			("tutorial.page.speed.heading", "Speed"),
			("tutorial.page.speed.description", "Aircraft operates in <b><u>slow</u></b>, <b><u>normal</u></b>, or <b><u>fast</u></b> speed. The current speed of an aircraft is displayed in the bottom-right corner of the aircraft.\nAircraft arrives at <b><u>normal</u></b> speed and can only land at <b><u>slow</u></b> or <b><u>normal</u></b> speed.\nAircraft lifts off at <b><u>normal</u></b> speed.\nPress <b><u>D</u></b> or hold <b><u>Left Shift</u></b> while <b><u>Scroll Up</u></b> to increase speed.\nPress <b><u>A</u></b> or hold <b><u>Left Shift</u></b> while <b><u>Scroll Down</u></b> to decrease speed."),
			("tutorial.page.type.heading", "Type"),
			("tutorial.page.type.description", "Aircraft can be <b><u>Light</u></b>, <b><u>Medium</u></b>, or <b><u>Heavy</u></b> type. Arrival aircraft carry limited fuel, shown in the top-right corner.\n<b><u>Light</u></b> aircraft are small, have a maximum <b><u>normal</u></b> speed, and can land only at <b><u>slow</u></b> speed. They turn 50% faster and carry 3 days of fuel.\n<b><u>Medium</u></b> aircraft carry 3.5 days of fuel.\n<b><u>Heavy</u></b> aircraft are large, carry 4 days of fuel, and account for 30% of aircraft."),
			("tutorial.page.events.heading", "Events"),
			("tutorial.page.events.description", "Sometimes, accidents do happen.\nA runway excursion can close a runway.\nAircraft may arrive with emergency fuel and need to land immediately.\nAn aircraft may suffer an engine failure and need to return immediately.\nWeather can turn <b><u>low</u></b> and <b><u>normal</u></b> airspace into a restricted area."),
			("tutorial.page.wind.heading", "Wind"),
			("tutorial.page.wind.description", "Wind affects aircraft takeoff and landing performance. Wind direction is shown in the top-left corner.\nWith a full tailwind, the go-around or rejected-takeoff probability is high.\nThe probability drops to 0% when the wind direction is at or below 90 degrees of the runway."),
			("tutorial.page.last.heading", "Last"),
			("tutorial.page.last.description", "\nUse <b><u>Tab</u></b> to hide or show in-game text.\nTCAS commands aircraft to climb or descend before a collision.\nGPWS commands aircraft to climb before terrain impact.\nYou start with 3 waiting-area upgrades.\nUpgrades arrive twice as fast.\nAircraft flying out of bounds count as restricted-area violations.\nPress <b><u>Space</u></b> while placing a waypoint to name it."),
			("tutorial.page.thanks.heading", "Thanks for playing \"Mini Realistic Airways\"!"),
			("tutorial.page.thanks.description", "For more information, refer to the <b><u><link=\"ENG\">Quick Reference Handbook</link></u></b>"));
	}

	private static void RegisterSimplifiedChinese(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "zh-Hans",
			("tutorial.title", "真实迷你空管"), ("tutorial.qrh", "快速检查单"), ("tutorial.next", "下一页"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#%E8%BF%B7%E4%BD%A0%E7%9C%9F%E5%AE%9E%E7%A9%BA%E7%AE%A1"),
			("settings.wind", "启用风向"), ("settings.events", "启用特情"), ("settings.tcas", "启用TCAS与GPWS"),
			("hud.wind", "风向：{0}°"), ("hud.altitudePrefix", "高度："), ("hud.speedPrefix", "速度："),
			("hud.fuel", "燃油：{0}%"), ("hud.fuelInfinite", "燃油：∞"), ("hud.weight", "机型：{0}"),
			("altitude.ground", "地面"), ("altitude.low", "低"), ("altitude.normal", "中"), ("altitude.high", "高"),
			("weight.light", "轻型"), ("weight.medium", "中型"), ("weight.heavy", "重型"),
			("engine.mayday", "Mayday，Mayday，Mayday！"), ("engine.failure", "{0}。我们的一台发动机发生故障。"), ("engine.return", "我们需要立即返场。"),
			("tutorial.page.altitude.heading", "高度"), ("tutorial.page.altitude.description", "飞机会处于<b><u>低</u></b>、<b><u>中</u></b>、<b><u>高</u></b>三种高度，当前高度显示在飞机图标左下角。\n飞机以<b><u>高</u></b>高度进场，只有<b><u>低</u></b>高度才能降落。\n飞机从<b><u>低</u></b>高度起飞，必须以<b><u>中</u></b>或<b><u>高</u></b>高度离场。\n按<b><u>W</u></b>或<b><u>滚轮上</u></b>升高，按<b><u>S</u></b>或<b><u>滚轮下</u></b>降低。\n<b><u>高</u></b>空飞机不受地形影响。"),
			("tutorial.page.speed.heading", "速度"), ("tutorial.page.speed.description", "飞机有<b><u>慢</u></b>、<b><u>中</u></b>、<b><u>快</u></b>三种速度，当前速度显示在飞机图标右下角。\n飞机以<b><u>中</u></b>速进场，只有<b><u>慢</u></b>或<b><u>中</u></b>速才能降落。\n飞机以<b><u>中</u></b>速起飞。\n按<b><u>D</u></b>或按住<b><u>左SHIFT</u></b><b><u>滚轮上</u></b>调速，按<b><u>A</u></b>或按住<b><u>左SHIFT</u></b><b><u>滚轮下</u></b>减速。"),
			("tutorial.page.type.heading", "机型"), ("tutorial.page.type.description", "飞机分为<b><u>轻型</u></b>、<b><u>中型</u></b>和<b><u>重型</u></b>，进场飞机的<b>燃油</b>显示在右上角。\n<b><u>轻型</u></b>飞机体积小，最高<b><u>中</u></b>速，只能以<b><u>慢</u></b>速降落，转弯快50%，携带3天<b>燃油</b>。\n<b><u>中型</u></b>飞机携带3.5天<b>燃油</b>。\n<b><u>重型</u></b>飞机体积大，携带4天<b>燃油</b>，占飞机总数30%。"),
			("tutorial.page.events.heading", "特情"), ("tutorial.page.events.description", "意外随时可能发生。\n跑道事故可能导致跑道关闭。\n低油量飞机可能需要立即降落。\n发动机故障飞机需要立即返场。\n恶劣天气会把<b><u>低</u></b>、<b><u>中</u></b>空域变成禁飞区。"),
			("tutorial.page.wind.heading", "风向"), ("tutorial.page.wind.description", "风向会影响飞机起降性能，当前风向显示在左上角。\n完全顺风起降时，复飞或中断起飞概率较高。\n风向与跑道夹角不超过90度时，概率为0%。"),
			("tutorial.page.last.heading", "最后"), ("tutorial.page.last.description", "\n按<b><u>Tab</u></b>隐藏或显示游戏文字。\nTCAS会在碰撞前命令飞机爬升或下降。\nGPWS会在撞山前命令飞机爬升。\n开局获得3个等待区升级，升级速度加倍。\n飞出边界算作进入禁飞区。\n放置航点时按<b><u>空格</u></b>可以命名。"),
			("tutorial.page.thanks.heading", "感谢您游玩“真实迷你空管”！"), ("tutorial.page.thanks.description", "更多信息请参阅<b><u><link=\"CHS\">快速检查单</link></u></b>"));
	}

	private static void RegisterTraditionalChinese(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "zh-Hant",
			("tutorial.title", "真實迷你空管"), ("tutorial.qrh", "快速檢查單"), ("tutorial.next", "下一頁"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#%E8%BF%B7%E4%BD%A0%E7%9C%9F%E5%AF%A6%E7%A9%BA%E7%AE%A1"),
			("settings.wind", "啟用風向"), ("settings.events", "啟用特情"), ("settings.tcas", "啟用TCAS與GPWS"),
			("hud.wind", "風向：{0}°"), ("hud.altitudePrefix", "高度："), ("hud.speedPrefix", "速度："),
			("hud.fuel", "燃油：{0}%"), ("hud.fuelInfinite", "燃油：∞"), ("hud.weight", "機型：{0}"),
			("altitude.ground", "地面"), ("altitude.low", "低"), ("altitude.normal", "中"), ("altitude.high", "高"),
			("weight.light", "輕型"), ("weight.medium", "中型"), ("weight.heavy", "重型"),
			("engine.mayday", "Mayday，Mayday，Mayday！"), ("engine.failure", "{0}。我們的一具引擎發生故障。"), ("engine.return", "我們需要立即返場。"),
			("tutorial.page.altitude.heading", "高度"), ("tutorial.page.altitude.description", "飛機有<b><u>低</u></b>、<b><u>中</u></b>、<b><u>高</u></b>三種高度，目前高度顯示於飛機圖示左下角。\n飛機以<b><u>高</u></b>高度進場，只有<b><u>低</u></b>高度才能降落。\n飛機由<b><u>低</u></b>高度起飛，必須以<b><u>中</u></b>或<b><u>高</u></b>高度離場。\n按<b><u>W</u></b>或<b><u>滾輪上</u></b>上升，按<b><u>S</u></b>或<b><u>滾輪下</u></b>下降。\n<b><u>高</u></b>空飛機不受地形影響。"),
			("tutorial.page.speed.heading", "速度"), ("tutorial.page.speed.description", "飛機有<b><u>慢</u></b>、<b><u>中</u></b>、<b><u>快</u></b>三種速度，目前速度顯示於飛機圖示右下角。\n飛機以<b><u>中</u></b>速進場，只有<b><u>慢</u></b>或<b><u>中</u></b>速才能降落。\n飛機以<b><u>中</u></b>速起飛。\n按<b><u>D</u></b>或按住<b><u>左SHIFT</u></b><b><u>滾輪上</u></b>調速，按<b><u>A</u></b>或按住<b><u>左SHIFT</u></b><b><u>滾輪下</u></b>減速。"),
			("tutorial.page.type.heading", "機型"), ("tutorial.page.type.description", "飛機分為<b><u>輕型</u></b>、<b><u>中型</u></b>和<b><u>重型</u></b>，進場飛機<b>燃油</b>顯示於右上角。\n<b><u>輕型</u></b>飛機體積小，最高<b><u>中</u></b>速，只能以<b><u>慢</u></b>速降落，轉彎快50%，攜帶3天<b>燃油</b>。\n<b><u>中型</u></b>飛機攜帶3.5天<b>燃油</b>。\n<b><u>重型</u></b>飛機體積大，攜帶4天<b>燃油</b>，占飛機總數30%。"),
			("tutorial.page.events.heading", "特情"), ("tutorial.page.events.description", "意外隨時可能發生。\n跑道事故可能導致跑道關閉。\n低油量飛機可能需要立即降落。\n引擎故障飛機需要立即返場。\n惡劣天氣會把<b><u>低</u></b>、<b><u>中</u></b>空域變成禁飛區。"),
			("tutorial.page.wind.heading", "風向"), ("tutorial.page.wind.description", "風向會影響飛機起降性能，目前風向顯示於左上角。\n完全順風起降時，重飛或中斷起飛機率較高。\n風向與跑道夾角不超過90度時，機率為0%。"),
			("tutorial.page.last.heading", "最後"), ("tutorial.page.last.description", "\n按<b><u>Tab</u></b>隱藏或顯示遊戲文字。\nTCAS會在碰撞前命令飛機爬升或下降。\nGPWS會在撞山前命令飛機爬升。\n開局獲得3個等待區升級，升級速度加倍。\n飛出邊界算作進入禁飛區。\n放置航點時按<b><u>空白鍵</u></b>可以命名。"),
			("tutorial.page.thanks.heading", "感謝您遊玩「真實迷你空管」！"), ("tutorial.page.thanks.description", "更多資訊請參閱<b><u><link=\"CHS\">快速檢查單</link></u></b>"));
	}

	private static void RegisterJapanese(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "ja",
			("tutorial.title", "リアルミニ航空管制"), ("tutorial.qrh", "クイックリファレンス"), ("tutorial.next", "次へ"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "風向を有効化"), ("settings.events", "イベントを有効化"), ("settings.tcas", "TCAS/GPWSを有効化"),
			("hud.wind", "風向：{0}°"), ("hud.altitudePrefix", "高度："), ("hud.speedPrefix", "速度："),
			("hud.fuel", "燃料：{0}%"), ("hud.fuelInfinite", "燃料：∞"), ("hud.weight", "機種：{0}"),
			("altitude.ground", "地上"), ("altitude.low", "低"), ("altitude.normal", "中"), ("altitude.high", "高"),
			("weight.light", "小型"), ("weight.medium", "中型"), ("weight.heavy", "大型"),
			("engine.mayday", "Mayday、Mayday、Mayday！"), ("engine.failure", "{0}。エンジンが1基故障しました。"), ("engine.return", "直ちに空港へ戻る必要があります。"),
			("tutorial.page.altitude.heading", "高度"), ("tutorial.page.altitude.description", "航空機には<b><u>低</u></b>、<b><u>中</u></b>、<b><u>高</u></b>の3高度があります。現在の高度は機体左下に表示されます。\n航空機は<b><u>高</u></b>高度で到着し、<b><u>低</u></b>高度でのみ着陸できます。\n離陸は<b><u>低</u></b>高度で行い、<b><u>中</u></b>または<b><u>高</u></b>高度で出発します。\n<b><u>W</u></b>または<b><u>ホイール上</u></b>で上昇、<b><u>S</u></b>または<b><u>ホイール下</u></b>で下降します。\n<b><u>高</u></b>高度の航空機は地形の影響を受けません。"),
			("tutorial.page.speed.heading", "速度"), ("tutorial.page.speed.description", "速度は<b><u>低速</u></b>、<b><u>中速</u></b>、<b><u>高速</u></b>の3段階です。現在の速度は機体右下に表示されます。\n到着時は<b><u>中速</u></b>で、<b><u>低速</u></b>または<b><u>中速</u></b>で着陸できます。\n離陸速度は<b><u>中速</u></b>です。\n<b><u>D</u></b>または<b><u>左Shift</u></b>＋<b><u>ホイール上</u></b>で加速、<b><u>A</u></b>または<b><u>左Shift</u></b>＋<b><u>ホイール下</u></b>で減速します。"),
			("tutorial.page.type.heading", "機種"), ("tutorial.page.type.description", "航空機は<b><u>小型</u></b>、<b><u>中型</u></b>、<b><u>大型</u></b>です。到着機の燃料は右上に表示されます。\n<b><u>小型</u></b>機は小さく、最高<b><u>中速</u></b>、<b><u>低速</u></b>でのみ着陸し、旋回が50%速く、3日分の燃料を搭載します。\n<b><u>中型</u></b>機は3.5日分、<b><u>大型</u></b>機は4日分の燃料を搭載します。大型機は全体の30%です。"),
			("tutorial.page.events.heading", "イベント"), ("tutorial.page.events.description", "事故は時々起こります。\n滑走路事故で滑走路が閉鎖されることがあります。\n燃料の少ない航空機は直ちに着陸する必要があります。\nエンジン故障機は直ちに帰投します。\n悪天候は<b><u>低</u></b>・<b><u>中</u></b>空域を制限区域にします。"),
			("tutorial.page.wind.heading", "風"), ("tutorial.page.wind.description", "風は離着陸性能に影響します。風向は左上に表示されます。\n完全な追い風では復行・離陸中止の確率が高くなります。\n滑走路との角度が90度以下なら確率は0%です。"),
			("tutorial.page.last.heading", "最後に"), ("tutorial.page.last.description", "\n<b><u>Tab</u></b>でゲーム内テキストを表示・非表示にします。\n衝突前にはTCASが上昇・下降を指示します。\n地形衝突前にはGPWSが上昇を指示します。\n待機エリアのアップグレードを3個所持し、獲得速度が2倍になります。\n画面外へ出た航空機は制限区域違反です。\nウェイポイント設置中に<b><u>Space</u></b>で名前を付けられます。"),
			("tutorial.page.thanks.heading", "リアルミニ航空管制を遊んでくれてありがとう！"), ("tutorial.page.thanks.description", "詳しくは<b><u><link=\"ENG\">クイックリファレンス</link></u></b>を参照してください。"));
	}

	private static void RegisterKorean(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "ko",
			("tutorial.title", "리얼 미니 항공 관제"), ("tutorial.qrh", "빠른 매뉴얼"), ("tutorial.next", "다음"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "바람 사용"), ("settings.events", "이벤트 사용"), ("settings.tcas", "TCAS/GPWS 사용"),
			("hud.wind", "바람: {0}°"), ("hud.altitudePrefix", "고도: "), ("hud.speedPrefix", "속도: "),
			("hud.fuel", "연료: {0}%"), ("hud.fuelInfinite", "연료: ∞"), ("hud.weight", "기종: {0}"),
			("altitude.ground", "지상"), ("altitude.low", "저고도"), ("altitude.normal", "중고도"), ("altitude.high", "고고도"),
			("weight.light", "경량"), ("weight.medium", "중형"), ("weight.heavy", "중량"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. 엔진 하나가 고장 났습니다."), ("engine.return", "즉시 공항으로 돌아가야 합니다."),
			("tutorial.page.altitude.heading", "고도"), ("tutorial.page.altitude.description", "항공기는<b><u>저</u></b>·<b><u>중</u></b>·<b><u>고</u></b>고도를 사용하며 현재 고도는 기체 왼쪽 아래에 표시됩니다.\n항공기는 <b><u>고</u></b>고도로 도착하고 <b><u>저</u></b>고도에서만 착륙할 수 있습니다.\n<b><u>저</u></b>고도로 이륙하며 <b><u>중</u></b> 또는 <b><u>고</u></b>고도로 출발해야 합니다.\n<b><u>W</u></b>/<b><u>스크롤 위</u></b>로 상승, <b><u>S</u></b>/<b><u>스크롤 아래</u></b>로 하강합니다.\n<b><u>고</u></b>고도 항공기는 지형의 영향을 받지 않습니다."),
			("tutorial.page.speed.heading", "속도"), ("tutorial.page.speed.description", "속도는<b><u>느림</u></b>·<b><u>보통</u></b>·<b><u>빠름</u></b> 세 단계이며 현재 속도는 기체 오른쪽 아래에 표시됩니다.\n도착 시 <b><u>보통</u></b> 속도이고 <b><u>느림</u></b> 또는 <b><u>보통</u></b>에서만 착륙할 수 있습니다.\n이륙 속도는 <b><u>보통</u></b>입니다.\n<b><u>D</u></b>/<b><u>왼쪽 Shift</u></b>+<b><u>스크롤 위</u></b>로 가속, <b><u>A</u></b>/<b><u>왼쪽 Shift</u></b>+<b><u>스크롤 아래</u></b>로 감속합니다."),
			("tutorial.page.type.heading", "기종"), ("tutorial.page.type.description", "항공기는<b><u>경량</u></b>·<b><u>중형</u></b>·<b><u>중량</u></b>입니다. 도착기의 연료는 오른쪽 위에 표시됩니다.\n<b><u>경량</u></b>기는 작고 최대 속도가 <b><u>보통</u></b>이며 <b><u>느림</u></b>에서만 착륙하고 선회가 50% 빠릅니다.\n<b><u>중형</u></b>기는 3.5일, <b><u>중량</u></b>기는 4일의 연료를 탑재합니다."),
			("tutorial.page.events.heading", "이벤트"), ("tutorial.page.events.description", "사고가 발생할 수 있습니다.\n활주로 사고로 활주로가 폐쇄될 수 있습니다.\n연료가 부족한 항공기는 즉시 착륙해야 합니다.\n엔진 고장 항공기는 즉시 귀환해야 합니다.\n악천후는<b><u>저</u></b>·<b><u>중</u></b>고도를 제한 구역으로 만듭니다."),
			("tutorial.page.wind.heading", "바람"), ("tutorial.page.wind.description", "바람은 이착륙 성능에 영향을 주며 방향은 왼쪽 위에 표시됩니다.\n순풍으로 이착륙하면 복행·이륙 중단 확률이 높습니다.\n활주로와의 각도가 90도 이하이면 확률은 0%입니다."),
			("tutorial.page.last.heading", "마지막"), ("tutorial.page.last.description", "\n<b><u>Tab</u></b>으로 게임 텍스트를 표시하거나 숨깁니다.\n충돌 전 TCAS가 상승 또는 하강을 지시합니다.\n지형 충돌 전 GPWS가 상승을 지시합니다.\n대기 구역 업그레이드 3개로 시작하며 업그레이드 속도가 2배입니다.\n화면 밖으로 나가면 제한 구역 위반입니다.\n웨이포인트 배치 중 <b><u>Space</u></b>로 이름을 지정합니다."),
			("tutorial.page.thanks.heading", "리얼 미니 항공 관제를 플레이해 주셔서 감사합니다!"), ("tutorial.page.thanks.description", "자세한 내용은 <b><u><link=\"ENG\">빠른 매뉴얼</link></u></b>을 확인하세요."));
	}

	private static void RegisterArabic(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "ar",
			("tutorial.title", "المراقبة الجوية المصغرة الواقعية"), ("tutorial.qrh", "الدليل السريع"), ("tutorial.next", "التالي"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "تفعيل الرياح"), ("settings.events", "تفعيل الأحداث"), ("settings.tcas", "تفعيل نظامي TCAS وGPWS"),
			("hud.wind", "الرياح: {0} درجة"), ("hud.altitudePrefix", "الارتفاع: "), ("hud.speedPrefix", "السرعة: "),
			("hud.fuel", "الوقود: {0}٪"), ("hud.fuelInfinite", "الوقود: غير محدود"), ("hud.weight", "النوع: {0}"),
			("altitude.ground", "على الأرض"), ("altitude.low", "منخفض"), ("altitude.normal", "متوسط"), ("altitude.high", "مرتفع"),
			("weight.light", "خفيف"), ("weight.medium", "متوسط"), ("weight.heavy", "ثقيل"),
			("engine.mayday", "نداء استغاثة، نداء استغاثة، نداء استغاثة!"), ("engine.failure", "{0}. تعطل أحد المحركات."), ("engine.return", "نحتاج إلى العودة إلى المطار فوراً."),
			("tutorial.page.altitude.heading", "الارتفاع"), ("tutorial.page.altitude.description", "تعمل الطائرات على ارتفاع <b><u>منخفض</u></b> أو <b><u>متوسط</u></b> أو <b><u>مرتفع</u></b>، ويظهر الارتفاع الحالي أسفل يسار الطائرة.\nتصل الطائرة بارتفاع <b><u>مرتفع</u></b> ولا تهبط إلا بارتفاع <b><u>منخفض</u></b>.\nتقلع بارتفاع <b><u>منخفض</u></b> وتغادر بارتفاع <b><u>متوسط</u></b> أو <b><u>مرتفع</u></b>.\nاستخدم <b><u>عجلة الفأرة للأعلى</u></b> للصعود، و<b><u>عجلة الفأرة للأسفل</u></b> للهبوط.\nالطائرة ذات الارتفاع <b><u>المرتفع</u></b> لا تتأثر بالتضاريس."),
			("tutorial.page.speed.heading", "السرعة"), ("tutorial.page.speed.description", "للطائرة سرعات <b><u>بطيئة</u></b> و<b><u>متوسطة</u></b> و<b><u>سريعة</u></b>، وتظهر السرعة الحالية أسفل يمينها.\nتصل بسرعة <b><u>متوسطة</u></b> وتهبط بسرعة <b><u>بطيئة</u></b> أو <b><u>متوسطة</u></b> فقط.\nتقلع بسرعة <b><u>متوسطة</u></b>.\nاستخدم <b><u>مفتاح التحويل الأيسر</u></b> مع <b><u>عجلة الفأرة للأعلى</u></b> للتسريع، ومع <b><u>عجلة الفأرة للأسفل</u></b> للإبطاء."),
			("tutorial.page.type.heading", "النوع"), ("tutorial.page.type.description", "الطائرات <b><u>خفيفة</u></b> أو <b><u>متوسطة</u></b> أو <b><u>ثقيلة</u></b>، ويظهر وقود الطائرة الوافدة أعلى اليمين.\nالطائرة <b><u>الخفيفة</u></b> صغيرة، سرعتها القصوى <b><u>متوسطة</u></b>، وتهبط بسرعة <b><u>بطيئة</u></b> فقط وتنعطف أسرع 50٪.\nتحمل الطائرة <b><u>المتوسطة</u></b> وقود 3.5 أيام، والثقيلة وقود 4 أيام."),
			("tutorial.page.events.heading", "الأحداث"), ("tutorial.page.events.description", "قد تحدث حوادث أحياناً.\nقد يؤدي خروج طائرة عن المدرج إلى إغلاقه.\nقد تحتاج طائرة قليلة الوقود إلى الهبوط فوراً.\nتحتاج الطائرة المتعطلة المحرك إلى العودة فوراً.\nقد يحول الطقس السيئ المجالين <b><u>المنخفض</u></b> و<b><u>المتوسط</u></b> إلى منطقة محظورة."),
			("tutorial.page.wind.heading", "الرياح"), ("tutorial.page.wind.description", "تؤثر الرياح في أداء الإقلاع والهبوط، ويظهر اتجاهها أعلى اليسار.\nيزداد احتمال الدوران أو إلغاء الإقلاع مع الرياح الخلفية الكاملة.\nيصبح الاحتمال 0٪ عندما تكون الزاوية مع المدرج 90 درجة أو أقل."),
			("tutorial.page.last.heading", "أخيراً"), ("tutorial.page.last.description", "\nاستخدم لوحة المفاتيح لإظهار نصوص اللعبة أو إخفائها.\nيأمر نظام تجنب التصادم الطائرات بالصعود أو الهبوط قبل التصادم.\nيأمر نظام التحذير من التضاريس بالصعود قبل الاصطدام بها.\nتبدأ بثلاث ترقيات لمنطقة الانتظار وتتضاعف سرعة الترقيات.\nالخروج من حدود الشاشة يُعد مخالفة لمنطقة محظورة.\nاستخدم لوحة المفاتيح أثناء وضع نقطة الطريق لتسميتها."),
			("tutorial.page.thanks.heading", "شكراً للعبك المراقبة الجوية المصغرة الواقعية!"), ("tutorial.page.thanks.description", "لمزيد من المعلومات راجع <b><u><link=\"ENG\">الدليل السريع</link></u></b>."));
	}

	private static void RegisterDutch(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "nl",
			("tutorial.title", "Mini Realistische Luchtverkeersleiding"), ("tutorial.qrh", "Snelle handleiding"), ("tutorial.next", "Volgende"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Wind inschakelen"), ("settings.events", "Gebeurtenissen inschakelen"), ("settings.tcas", "TCAS & GPWS inschakelen"),
			("hud.wind", "Wind: {0}°"), ("hud.altitudePrefix", "Hoogte: "), ("hud.speedPrefix", "Snelheid: "),
			("hud.fuel", "Brandstof: {0}%"), ("hud.fuelInfinite", "Brandstof: ∞"), ("hud.weight", "Type: {0}"),
			("altitude.ground", "Grond"), ("altitude.low", "Laag"), ("altitude.normal", "Normaal"), ("altitude.high", "Hoog"),
			("weight.light", "Licht"), ("weight.medium", "Middel"), ("weight.heavy", "Zwaar"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. We hebben motorproblemen."), ("engine.return", "We moeten onmiddellijk terugkeren naar het vliegveld."),
			("tutorial.page.altitude.heading", "Hoogte"), ("tutorial.page.altitude.description", "Vliegtuigen vliegen op <b><u>lage</u></b>, <b><u>normale</u></b> of <b><u>hoge</u></b> hoogte. De huidige hoogte staat linksonder bij het vliegtuig.\nAankomende vliegtuigen komen op <b><u>hoge</u></b> hoogte en landen alleen op <b><u>lage</u></b> hoogte.\nZe stijgen op vanaf <b><u>lage</u></b> hoogte en vertrekken op <b><u>normale</u></b> of <b><u>hoge</u></b> hoogte.\nGebruik <b><u>W</u></b> of <b><u>scroll omhoog</u></b> om te stijgen, en <b><u>S</u></b> of <b><u>scroll omlaag</u></b> om te dalen.\n<b><u>Hoge</u></b> vliegtuigen worden niet door terrein beïnvloed."),
			("tutorial.page.speed.heading", "Snelheid"), ("tutorial.page.speed.description", "Vliegtuigen hebben <b><u>lage</u></b>, <b><u>normale</u></b> of <b><u>hoge</u></b> snelheid; de huidige snelheid staat rechtsonder.\nAankomst is op <b><u>normale</u></b> snelheid en landen kan op <b><u>lage</u></b> of <b><u>normale</u></b> snelheid.\nVertrekken gebeurt op <b><u>normale</u></b> snelheid.\nGebruik <b><u>D</u></b> of <b><u>Shift</u></b> met <b><u>scroll omhoog</u></b> om te versnellen, en <b><u>A</u></b> of <b><u>scroll omlaag</u></b> om te vertragen."),
			("tutorial.page.type.heading", "Type"), ("tutorial.page.type.description", "Vliegtuigen zijn <b><u>licht</u></b>, <b><u>middel</u></b> of <b><u>zwaar</u></b>. Brandstof van aankomende vliegtuigen staat rechtsboven.\n<b><u>Lichte</u></b> vliegtuigen zijn klein, hebben maximaal <b><u>normale</u></b> snelheid, landen alleen langzaam en draaien 50% sneller.\n<b><u>Middelzware</u></b> vliegtuigen hebben 3,5 dagen brandstof; <b><u>zware</u></b> 4 dagen."),
			("tutorial.page.events.heading", "Gebeurtenissen"), ("tutorial.page.events.description", "Soms gebeuren er ongelukken.\nEen runway-excursie kan een baan sluiten.\nEen vliegtuig met weinig brandstof moet soms direct landen.\nEen motorstoring vereist een onmiddellijke terugkeer.\nSlecht weer maakt <b><u>lage</u></b> en <b><u>normale</u></b> lucht verboden."),
			("tutorial.page.wind.heading", "Wind"), ("tutorial.page.wind.description", "Wind beïnvloedt opstijgen en landen; de richting staat linksboven.\nBij volledige rugwind is de kans op doorstart of afgebroken start groot.\nBij een hoek van maximaal 90 graden met de baan is die kans 0%."),
			("tutorial.page.last.heading", "Tot slot"), ("tutorial.page.last.description", "\nGebruik <b><u>Tab</u></b> om speltekst te tonen of verbergen.\nTCAS laat vliegtuigen klimmen of dalen voor een botsing.\nGPWS laat ze klimmen voor terrein.\nJe start met 3 wachtgebied-upgrades en upgrades komen tweemaal zo snel.\nBuiten beeld vliegen telt als overtreding.\nGebruik <b><u>Space</u></b> bij het plaatsen van een waypoint om het te benoemen."),
			("tutorial.page.thanks.heading", "Bedankt voor het spelen van Mini Realistische Luchtverkeersleiding!"), ("tutorial.page.thanks.description", "Meer informatie staat in de <b><u><link=\"ENG\">snelle handleiding</link></u></b>."));
	}

	private static void RegisterFrench(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "fr",
			("tutorial.title", "Mini contrôle aérien réaliste"), ("tutorial.qrh", "Guide rapide"), ("tutorial.next", "Suivant"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Activer le vent"), ("settings.events", "Activer les événements"), ("settings.tcas", "Activer TCAS et GPWS"),
			("hud.wind", "Vent : {0}°"), ("hud.altitudePrefix", "ALT : "), ("hud.speedPrefix", "VIT : "),
			("hud.fuel", "Carburant : {0}%"), ("hud.fuelInfinite", "Carburant : ∞"), ("hud.weight", "Type : {0}"),
			("altitude.ground", "Sol"), ("altitude.low", "Basse"), ("altitude.normal", "Normale"), ("altitude.high", "Haute"),
			("weight.light", "Léger"), ("weight.medium", "Moyen"), ("weight.heavy", "Lourd"),
			("engine.mayday", "Mayday, Mayday, Mayday !"), ("engine.failure", "{0}. Nous avons une panne moteur."), ("engine.return", "Nous devons revenir immédiatement au terrain."),
			("tutorial.page.altitude.heading", "Altitude"), ("tutorial.page.altitude.description", "Les avions utilisent une altitude <b><u>basse</u></b>, <b><u>normale</u></b> ou <b><u>haute</u></b>. L'altitude actuelle est affichée en bas à gauche de l'avion.\nLes avions arrivent à <b><u>haute</u></b> altitude et ne peuvent atterrir qu'à <b><u>basse</u></b> altitude.\nIls décollent à <b><u>basse</u></b> altitude et partent à altitude <b><u>normale</u></b> ou <b><u>haute</u></b>.\nUtilisez <b><u>W</u></b>/<b><u>molette haut</u></b> pour monter et <b><u>S</u></b>/<b><u>molette bas</u></b> pour descendre.\nLe relief n'affecte pas les avions à <b><u>haute</u></b> altitude."),
			("tutorial.page.speed.heading", "Vitesse"), ("tutorial.page.speed.description", "Les avions ont une vitesse <b><u>lente</u></b>, <b><u>normale</u></b> ou <b><u>rapide</u></b>. La vitesse actuelle est affichée en bas à droite.\nLes arrivées sont à vitesse <b><u>normale</u></b> et atterrissent à vitesse <b><u>lente</u></b> ou <b><u>normale</u></b>.\nLe décollage se fait à vitesse <b><u>normale</u></b>.\n<b><u>D</u></b>/<b><u>molette haut</u></b> accélère ; <b><u>A</u></b>/<b><u>molette bas</u></b> ralentit (avec <b><u>Shift gauche</u></b>)."),
			("tutorial.page.type.heading", "Type"), ("tutorial.page.type.description", "Les avions sont <b><u>légers</u></b>, <b><u>moyens</u></b> ou <b><u>lourds</u></b>. Le carburant des arrivées est affiché en haut à droite.\nLes avions <b><u>légers</u></b> sont petits, limités à la vitesse <b><u>normale</u></b>, atterrissent seulement à vitesse <b><u>lente</u></b> et tournent 50 % plus vite.\nLes avions <b><u>moyens</u></b> emportent 3,5 jours de carburant ; les <b><u>lourds</u></b>, 4 jours."),
			("tutorial.page.events.heading", "Événements"), ("tutorial.page.events.description", "Des accidents peuvent arriver.\nUne sortie de piste peut fermer une piste.\nUn avion à court de carburant peut devoir atterrir immédiatement.\nUne panne moteur exige un retour immédiat.\nLa météo peut transformer les espaces <b><u>bas</u></b> et <b><u>normaux</u></b> en zone interdite."),
			("tutorial.page.wind.heading", "Vent"), ("tutorial.page.wind.description", "Le vent influence les performances au décollage et à l'atterrissage. Sa direction est affichée en haut à gauche.\nAvec un vent arrière complet, la probabilité de remise des gaz ou d'interruption du décollage est élevée.\nElle tombe à 0 % lorsque l'angle avec la piste est inférieur ou égal à 90 degrés."),
			("tutorial.page.last.heading", "Dernier rappel"), ("tutorial.page.last.description", "\nUtilisez <b><u>Tab</u></b> pour afficher ou masquer les textes.\nLe TCAS ordonne de monter ou descendre avant une collision.\nLe GPWS ordonne de monter avant un impact avec le relief.\nVous commencez avec 3 améliorations de zone d'attente, obtenues deux fois plus vite.\nSortir des limites compte comme une infraction de zone interdite.\nAppuyez sur <b><u>Espace</u></b> pour nommer un waypoint."),
			("tutorial.page.thanks.heading", "Merci d'avoir joué à Mini contrôle aérien réaliste !"), ("tutorial.page.thanks.description", "Pour plus d'informations, consultez le <b><u><link=\"ENG\">guide rapide</link></u></b>."));
	}

	private static void RegisterGerman(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "de",
			("tutorial.title", "Mini Realistische Flugverkehrskontrolle"), ("tutorial.qrh", "Kurzanleitung"), ("tutorial.next", "Weiter"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Wind aktivieren"), ("settings.events", "Ereignisse aktivieren"), ("settings.tcas", "TCAS & GPWS aktivieren"),
			("hud.wind", "Wind: {0}°"), ("hud.altitudePrefix", "HÖHE: "), ("hud.speedPrefix", "GESCHW: "),
			("hud.fuel", "Treibstoff: {0}%"), ("hud.fuelInfinite", "Treibstoff: ∞"), ("hud.weight", "Typ: {0}"),
			("altitude.ground", "Boden"), ("altitude.low", "Niedrig"), ("altitude.normal", "Normal"), ("altitude.high", "Hoch"),
			("weight.light", "Leicht"), ("weight.medium", "Mittel"), ("weight.heavy", "Schwer"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. Wir haben einen Triebwerksausfall."), ("engine.return", "Wir müssen sofort zum Flugplatz zurückkehren."),
			("tutorial.page.altitude.heading", "Höhe"), ("tutorial.page.altitude.description", "Flugzeuge fliegen in <b><u>niedriger</u></b>, <b><u>normaler</u></b> oder <b><u>hoher</u></b> Höhe. Die aktuelle Höhe steht unten links am Flugzeug.\nAnkommende Flugzeuge kommen in <b><u>hoher</u></b> Höhe und landen nur in <b><u>niedriger</u></b> Höhe.\nSie starten in <b><u>niedriger</u></b> Höhe und verlassen den Bereich in <b><u>normaler</u></b> oder <b><u>hoher</u></b> Höhe.\nMit <b><u>W</u></b>/<b><u>Mausrad hoch</u></b> steigen, mit <b><u>S</u></b>/<b><u>Mausrad runter</u></b> sinken.\n<b><u>Hohe</u></b> Flugzeuge werden vom Gelände nicht beeinflusst."),
			("tutorial.page.speed.heading", "Geschwindigkeit"), ("tutorial.page.speed.description", "Flugzeuge haben <b><u>langsame</u></b>, <b><u>normale</u></b> oder <b><u>schnelle</u></b> Geschwindigkeit. Sie steht unten rechts.\nAnkünfte sind <b><u>normal</u></b> schnell und landen langsam oder normal.\nDer Start erfolgt mit <b><u>normaler</u></b> Geschwindigkeit.\n<b><u>D</u></b>/<b><u>Mausrad hoch</u></b> beschleunigt, <b><u>A</u></b>/<b><u>Mausrad runter</u></b> bremst (mit <b><u>linker Shift-Taste</u></b>)."),
			("tutorial.page.type.heading", "Typ"), ("tutorial.page.type.description", "Flugzeuge sind <b><u>leicht</u></b>, <b><u>mittel</u></b> oder <b><u>schwer</u></b>. Der Treibstoff ankommender Flugzeuge steht oben rechts.\n<b><u>Leichte</u></b> Flugzeuge sind klein, höchstens normal schnell, landen nur langsam und drehen 50 % schneller.\n<b><u>Mittlere</u></b> Flugzeuge haben 3,5 Tage Treibstoff, <b><u>schwere</u></b> 4 Tage."),
			("tutorial.page.events.heading", "Ereignisse"), ("tutorial.page.events.description", "Manchmal passieren Unfälle.\nEin Ausflug von der Startbahn kann sie schließen.\nFlugzeuge mit wenig Treibstoff müssen eventuell sofort landen.\nBei Triebwerksausfall ist eine sofortige Rückkehr nötig.\nWetter kann niedrige und normale Lufträume zu Sperrgebieten machen."),
			("tutorial.page.wind.heading", "Wind"), ("tutorial.page.wind.description", "Wind beeinflusst Start und Landung; seine Richtung steht oben links.\nBei vollständigem Rückenwind ist die Wahrscheinlichkeit für Durchstarten oder Startabbruch hoch.\nBei höchstens 90 Grad zur Bahn beträgt sie 0 %."),
			("tutorial.page.last.heading", "Zum Schluss"), ("tutorial.page.last.description", "\nMit <b><u>Tab</u></b> blendest du Spieltexte ein oder aus.\nTCAS lässt Flugzeuge vor einer Kollision steigen oder sinken.\nGPWS lässt sie vor Geländekontakt steigen.\nDu startest mit 3 Wartebereich-Upgrades, die doppelt so schnell kommen.\nAußerhalb der Grenzen zählt als Sperrgebietsverletzung.\nMit <b><u>Leertaste</u></b> benennst du einen Wegpunkt."),
			("tutorial.page.thanks.heading", "Danke fürs Spielen von Mini Realistische Flugverkehrskontrolle!"), ("tutorial.page.thanks.description", "Weitere Informationen findest du in der <b><u><link=\"ENG\">Kurzanleitung</link></u></b>."));
	}

	private static void RegisterPolish(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "pl",
			("tutorial.title", "Mini Realistyczna Kontrola Ruchu Lotniczego"), ("tutorial.qrh", "Szybki podręcznik"), ("tutorial.next", "Dalej"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Włącz wiatr"), ("settings.events", "Włącz zdarzenia"), ("settings.tcas", "Włącz TCAS i GPWS"),
			("hud.wind", "Wiatr: {0}°"), ("hud.altitudePrefix", "WYS: "), ("hud.speedPrefix", "PRĘD: "),
			("hud.fuel", "Paliwo: {0}%"), ("hud.fuelInfinite", "Paliwo: ∞"), ("hud.weight", "Typ: {0}"),
			("altitude.ground", "Ziemia"), ("altitude.low", "Niska"), ("altitude.normal", "Normalna"), ("altitude.high", "Wysoka"),
			("weight.light", "Lekki"), ("weight.medium", "Średni"), ("weight.heavy", "Ciężki"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. Mamy awarię jednego silnika."), ("engine.return", "Musimy natychmiast wrócić na lotnisko."),
			("tutorial.page.altitude.heading", "Wysokość"), ("tutorial.page.altitude.description", "Samoloty latają na wysokości <b><u>niskiej</u></b>, <b><u>normalnej</u></b> lub <b><u>wysokiej</u></b>. Bieżąca wysokość jest wyświetlana w lewym dolnym rogu samolotu.\nPrzyloty nadlatują na <b><u>wysokiej</u></b> wysokości i lądują tylko na <b><u>niskiej</u></b>.\nStartują z <b><u>niskiej</u></b> i odlatują na <b><u>normalnej</u></b> lub <b><u>wysokiej</u></b>.\n<b><u>W</u></b>/<b><u>kółko w górę</u></b> zwiększa wysokość, <b><u>S</u></b>/<b><u>w dół</u></b> ją zmniejsza.\nTeren nie wpływa na samoloty na <b><u>wysokiej</u></b> wysokości."),
			("tutorial.page.speed.heading", "Prędkość"), ("tutorial.page.speed.description", "Samoloty mają prędkość <b><u>małą</u></b>, <b><u>normalną</u></b> lub <b><u>dużą</u></b>. Bieżąca prędkość jest w prawym dolnym rogu.\nPrzyloty mają prędkość <b><u>normalną</u></b> i lądują przy małej lub normalnej.\nStartują z prędkością <b><u>normalną</u></b>.\n<b><u>D</u></b>/<b><u>kółko w górę</u></b> przyspiesza, <b><u>A</u></b>/<b><u>w dół</u></b> zwalnia (z <b><u>lewym Shift</u></b>)."),
			("tutorial.page.type.heading", "Typ"), ("tutorial.page.type.description", "Samoloty są <b><u>lekkie</u></b>, <b><u>średnie</u></b> lub <b><u>ciężkie</u></b>. Paliwo przylotów jest w prawym górnym rogu.\nSamoloty <b><u>lekkie</u></b> są małe, osiągają najwyżej normalną prędkość, lądują tylko wolno i skręcają o 50% szybciej.\n<b><u>Średnie</u></b> mają 3,5 dnia paliwa, a <b><u>ciężkie</u></b> 4 dni."),
			("tutorial.page.events.heading", "Zdarzenia"), ("tutorial.page.events.description", "Czasem zdarzają się wypadki.\nWypadnięcie z pasa może go zamknąć.\nSamolot z małą ilością paliwa może wymagać natychmiastowego lądowania.\nAwaria silnika wymaga natychmiastowego powrotu.\nPogoda może zamienić niską i normalną przestrzeń w strefę zakazaną."),
			("tutorial.page.wind.heading", "Wiatr"), ("tutorial.page.wind.description", "Wiatr wpływa na start i lądowanie; kierunek jest w lewym górnym rogu.\nPrzy pełnym wietrze tylnym prawdopodobieństwo odejścia lub przerwania startu jest wysokie.\nPrzy kącie do pasa nie większym niż 90 stopni wynosi 0%."),
			("tutorial.page.last.heading", "Na koniec"), ("tutorial.page.last.description", "\n<b><u>Tab</u></b> pokazuje lub ukrywa tekst gry.\nTCAS nakazuje wznoszenie lub zniżanie przed kolizją.\nGPWS nakazuje wznoszenie przed zderzeniem z terenem.\nZaczynasz z 3 ulepszeniami strefy oczekiwania, zdobywanymi dwa razy szybciej.\nWylot poza granice to naruszenie strefy zakazanej.\nNaciśnij <b><u>Spację</u></b>, aby nazwać punkt."),
			("tutorial.page.thanks.heading", "Dziękujemy za grę w Mini Realistyczną Kontrolę Ruchu Lotniczego!"), ("tutorial.page.thanks.description", "Więcej informacji znajdziesz w <b><u><link=\"ENG\">szybkim podręczniku</link></u></b>."));
	}

	private static void RegisterPortuguese(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "pt",
			("tutorial.title", "Mini Controle de Tráfego Aéreo Realista"), ("tutorial.qrh", "Manual rápido"), ("tutorial.next", "Próximo"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Ativar vento"), ("settings.events", "Ativar eventos"), ("settings.tcas", "Ativar TCAS e GPWS"),
			("hud.wind", "Vento: {0}°"), ("hud.altitudePrefix", "ALT: "), ("hud.speedPrefix", "VEL: "),
			("hud.fuel", "Combustível: {0}%"), ("hud.fuelInfinite", "Combustível: ∞"), ("hud.weight", "Tipo: {0}"),
			("altitude.ground", "Solo"), ("altitude.low", "Baixa"), ("altitude.normal", "Normal"), ("altitude.high", "Alta"),
			("weight.light", "Leve"), ("weight.medium", "Médio"), ("weight.heavy", "Pesado"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. Temos uma falha em um motor."), ("engine.return", "Precisamos retornar imediatamente ao aeroporto."),
			("tutorial.page.altitude.heading", "Altitude"), ("tutorial.page.altitude.description", "As aeronaves usam altitude <b><u>baixa</u></b>, <b><u>normal</u></b> ou <b><u>alta</u></b>. A altitude atual aparece no canto inferior esquerdo.\nAs chegadas vêm em altitude <b><u>alta</u></b> e só pousam em altitude <b><u>baixa</u></b>.\nDecolam em <b><u>baixa</u></b> e partem em altitude <b><u>normal</u></b> ou <b><u>alta</u></b>.\nUse <b><u>W</u></b>/<b><u>rolagem para cima</u></b> para subir e <b><u>S</u></b>/<b><u>para baixo</u></b> para descer.\nO terreno não afeta aeronaves em altitude <b><u>alta</u></b>."),
			("tutorial.page.speed.heading", "Velocidade"), ("tutorial.page.speed.description", "As aeronaves têm velocidade <b><u>lenta</u></b>, <b><u>normal</u></b> ou <b><u>rápida</u></b>. A atual aparece no canto inferior direito.\nChegadas vêm em velocidade <b><u>normal</u></b> e pousam em lenta ou normal.\nA decolagem usa velocidade <b><u>normal</u></b>.\n<b><u>D</u></b>/<b><u>rolagem para cima</u></b> acelera; <b><u>A</u></b>/<b><u>para baixo</u></b> desacelera (com <b><u>Shift esquerdo</u></b>)."),
			("tutorial.page.type.heading", "Tipo"), ("tutorial.page.type.description", "As aeronaves podem ser <b><u>leves</u></b>, <b><u>médias</u></b> ou <b><u>pesadas</u></b>. O combustível das chegadas aparece no canto superior direito.\nAeronaves <b><u>leves</u></b> são pequenas, têm velocidade máxima normal, pousam apenas devagar e fazem curvas 50% mais rápidas.\nAs <b><u>médias</u></b> levam 3,5 dias de combustível; as <b><u>pesadas</u></b>, 4 dias."),
			("tutorial.page.events.heading", "Eventos"), ("tutorial.page.events.description", "Às vezes acidentes acontecem.\nUma saída de pista pode fechá-la.\nUma aeronave com pouco combustível pode precisar pousar imediatamente.\nUma falha de motor exige retorno imediato.\nO clima pode transformar os espaços baixos e normais em área restrita."),
			("tutorial.page.wind.heading", "Vento"), ("tutorial.page.wind.description", "O vento afeta a decolagem e o pouso; sua direção aparece no canto superior esquerdo.\nCom vento de cauda total, a chance de arremetida ou rejeição de decolagem é alta.\nEla cai para 0% quando o ângulo com a pista é de até 90 graus."),
			("tutorial.page.last.heading", "Por fim"), ("tutorial.page.last.description", "\nUse <b><u>Tab</u></b> para mostrar ou ocultar textos.\nO TCAS ordena subir ou descer antes de uma colisão.\nO GPWS ordena subir antes do impacto com o terreno.\nVocê começa com 3 melhorias da área de espera, obtidas duas vezes mais rápido.\nSair dos limites conta como infração de área restrita.\nPressione <b><u>Espaço</u></b> para nomear um ponto."),
			("tutorial.page.thanks.heading", "Obrigado por jogar Mini Controle de Tráfego Aéreo Realista!"), ("tutorial.page.thanks.description", "Para mais informações, consulte o <b><u><link=\"ENG\">manual rápido</link></u></b>."));
	}

	private static void RegisterRussian(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "ru",
			("tutorial.title", "Мини-реалистичный авиадиспетчер"), ("tutorial.qrh", "Краткое руководство"), ("tutorial.next", "Далее"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Включить ветер"), ("settings.events", "Включить события"), ("settings.tcas", "Включить TCAS и GPWS"),
			("hud.wind", "Ветер: {0}°"), ("hud.altitudePrefix", "ВЫС: "), ("hud.speedPrefix", "СКОР: "),
			("hud.fuel", "Топливо: {0}%"), ("hud.fuelInfinite", "Топливо: ∞"), ("hud.weight", "Тип: {0}"),
			("altitude.ground", "Земля"), ("altitude.low", "Низкая"), ("altitude.normal", "Обычная"), ("altitude.high", "Высокая"),
			("weight.light", "Лёгкий"), ("weight.medium", "Средний"), ("weight.heavy", "Тяжёлый"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. Отказал один двигатель."), ("engine.return", "Нам нужно немедленно вернуться на аэродром."),
			("tutorial.page.altitude.heading", "Высота"), ("tutorial.page.altitude.description", "Самолёты летают на <b><u>низкой</u></b>, <b><u>обычной</u></b> или <b><u>высокой</u></b> высоте. Текущая высота показана слева внизу самолёта.\nПрибывающие самолёты входят на <b><u>высокой</u></b> высоте и садятся только на <b><u>низкой</u></b>.\nВзлёт выполняется с <b><u>низкой</u></b>, а вылет — на <b><u>обычной</u></b> или <b><u>высокой</u></b>.\n<b><u>W</u></b>/<b><u>колесо вверх</u></b> повышает высоту, <b><u>S</u></b>/<b><u>вниз</u></b> понижает.\nСамолёты на <b><u>высокой</u></b> высоте не зависят от рельефа."),
			("tutorial.page.speed.heading", "Скорость"), ("tutorial.page.speed.description", "Скорость бывает <b><u>низкой</u></b>, <b><u>обычной</u></b> или <b><u>высокой</u></b>; она показана справа внизу.\nПрибытие происходит с <b><u>обычной</u></b> скоростью, посадка — с низкой или обычной.\nВзлёт — с <b><u>обычной</u></b> скоростью.\n<b><u>D</u></b>/<b><u>колесо вверх</u></b> ускоряет, <b><u>A</u></b>/<b><u>вниз</u></b> замедляет (с <b><u>левым Shift</u></b>)."),
			("tutorial.page.type.heading", "Тип"), ("tutorial.page.type.description", "Самолёты бывают <b><u>лёгкими</u></b>, <b><u>средними</u></b> и <b><u>тяжёлыми</u></b>. Топливо прибывающего самолёта показано справа вверху.\n<b><u>Лёгкие</u></b> самолёты малы, летают максимум с обычной скоростью, садятся только медленно и поворачивают на 50% быстрее.\n<b><u>Средние</u></b> имеют 3,5 дня топлива, <b><u>тяжёлые</u></b> — 4 дня."),
			("tutorial.page.events.heading", "События"), ("tutorial.page.events.description", "Иногда происходят аварии.\nВыкатывание с полосы может закрыть её.\nСамолёту с малым запасом топлива может потребоваться немедленная посадка.\nОтказ двигателя требует немедленного возврата.\nПогода может превратить низкое и обычное воздушное пространство в запретную зону."),
			("tutorial.page.wind.heading", "Ветер"), ("tutorial.page.wind.description", "Ветер влияет на взлёт и посадку; направление показано слева вверху.\nПри полном попутном ветре вероятность ухода на второй круг или прекращения взлёта высока.\nПри угле к полосе не более 90 градусов она равна 0%."),
			("tutorial.page.last.heading", "Напоследок"), ("tutorial.page.last.description", "\n<b><u>Tab</u></b> показывает или скрывает текст игры.\nTCAS даёт команду набрать или снизить высоту перед столкновением.\nGPWS даёт команду набрать высоту перед столкновением с рельефом.\nВ начале доступно 3 улучшения зоны ожидания, они приходят вдвое быстрее.\nВылет за границы считается нарушением запретной зоны.\nНажмите <b><u>Пробел</u></b>, чтобы назвать точку."),
			("tutorial.page.thanks.heading", "Спасибо за игру в Мини-реалистичный авиадиспетчер!"), ("tutorial.page.thanks.description", "Подробнее см. в <b><u><link=\"ENG\">кратком руководстве</link></u></b>."));
	}

	private static void RegisterSpanish(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "es",
			("tutorial.title", "Mini Control Aéreo Realista"), ("tutorial.qrh", "Guía rápida"), ("tutorial.next", "Siguiente"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Activar viento"), ("settings.events", "Activar eventos"), ("settings.tcas", "Activar TCAS y GPWS"),
			("hud.wind", "Viento: {0}°"), ("hud.altitudePrefix", "ALT: "), ("hud.speedPrefix", "VEL: "),
			("hud.fuel", "Combustible: {0}%"), ("hud.fuelInfinite", "Combustible: ∞"), ("hud.weight", "Tipo: {0}"),
			("altitude.ground", "Suelo"), ("altitude.low", "Baja"), ("altitude.normal", "Normal"), ("altitude.high", "Alta"),
			("weight.light", "Ligero"), ("weight.medium", "Medio"), ("weight.heavy", "Pesado"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. Tenemos un fallo de motor."), ("engine.return", "Debemos volver al aeropuerto inmediatamente."),
			("tutorial.page.altitude.heading", "Altitud"), ("tutorial.page.altitude.description", "Los aviones operan a altitud <b><u>baja</u></b>, <b><u>normal</u></b> o <b><u>alta</u></b>. La actual se muestra abajo a la izquierda.\nLos aviones llegan a altitud <b><u>alta</u></b> y solo aterrizan a altitud <b><u>baja</u></b>.\nDespegan a <b><u>baja</u></b> y salen a altitud <b><u>normal</u></b> o <b><u>alta</u></b>.\n<b><u>W</u></b>/<b><u>rueda arriba</u></b> sube; <b><u>S</u></b>/<b><u>abajo</u></b> baja.\nEl terreno no afecta a aviones en altitud <b><u>alta</u></b>."),
			("tutorial.page.speed.heading", "Velocidad"), ("tutorial.page.speed.description", "La velocidad puede ser <b><u>lenta</u></b>, <b><u>normal</u></b> o <b><u>rápida</u></b>; aparece abajo a la derecha.\nLas llegadas usan velocidad <b><u>normal</u></b> y aterrizan lenta o normalmente.\nEl despegue usa velocidad <b><u>normal</u></b>.\n<b><u>D</u></b>/<b><u>rueda arriba</u></b> acelera y <b><u>A</u></b>/<b><u>abajo</u></b> frena (con <b><u>Shift izquierdo</u></b>)."),
			("tutorial.page.type.heading", "Tipo"), ("tutorial.page.type.description", "Los aviones son <b><u>ligeros</u></b>, <b><u>medios</u></b> o <b><u>pesados</u></b>. El combustible de las llegadas aparece arriba a la derecha.\nLos <b><u>ligeros</u></b> son pequeños, alcanzan velocidad normal, solo aterrizan despacio y giran un 50% más rápido.\nLos <b><u>medios</u></b> llevan 3,5 días de combustible y los <b><u>pesados</u></b>, 4 días."),
			("tutorial.page.events.heading", "Eventos"), ("tutorial.page.events.description", "A veces ocurren accidentes.\nUna salida de pista puede cerrarla.\nUn avión con poco combustible puede necesitar aterrizar de inmediato.\nUn fallo de motor exige volver inmediatamente.\nEl tiempo puede convertir los espacios bajos y normales en zona restringida."),
			("tutorial.page.wind.heading", "Viento"), ("tutorial.page.wind.description", "El viento afecta al despegue y aterrizaje; su dirección aparece arriba a la izquierda.\nCon viento de cola completo, la probabilidad de frustrada o despegue abortado es alta.\nBaja al 0% cuando el ángulo con la pista es de 90 grados o menos."),
			("tutorial.page.last.heading", "Por último"), ("tutorial.page.last.description", "\nUsa <b><u>Tab</u></b> para mostrar u ocultar el texto.\nTCAS ordena subir o bajar antes de una colisión.\nGPWS ordena subir antes de impactar con el terreno.\nEmpiezas con 3 mejoras del área de espera, que llegan el doble de rápido.\nSalir de los límites cuenta como infracción.\nPulsa <b><u>Espacio</u></b> para nombrar un punto."),
			("tutorial.page.thanks.heading", "¡Gracias por jugar a Mini Control Aéreo Realista!"), ("tutorial.page.thanks.description", "Para más información, consulta la <b><u><link=\"ENG\">guía rápida</link></u></b>."));
	}

	private static void RegisterTurkish(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "tr",
			("tutorial.title", "Mini Gerçekçi Hava Trafik Kontrolü"), ("tutorial.qrh", "Hızlı başvuru"), ("tutorial.next", "İleri"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Rüzgârı etkinleştir"), ("settings.events", "Olayları etkinleştir"), ("settings.tcas", "TCAS ve GPWS'ı etkinleştir"),
			("hud.wind", "Rüzgâr: {0}°"), ("hud.altitudePrefix", "İRT: "), ("hud.speedPrefix", "HIZ: "),
			("hud.fuel", "Yakıt: %{0}"), ("hud.fuelInfinite", "Yakıt: ∞"), ("hud.weight", "Tip: {0}"),
			("altitude.ground", "Yer"), ("altitude.low", "Alçak"), ("altitude.normal", "Normal"), ("altitude.high", "Yüksek"),
			("weight.light", "Hafif"), ("weight.medium", "Orta"), ("weight.heavy", "Ağır"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. Bir motorumuz arızalandı."), ("engine.return", "Derhal meydana dönmemiz gerekiyor."),
			("tutorial.page.altitude.heading", "İrtifa"), ("tutorial.page.altitude.description", "Uçaklar <b><u>alçak</u></b>, <b><u>normal</u></b> veya <b><u>yüksek</u></b> irtifada çalışır. Güncel irtifa uçağın sol altında görünür.\nGelen uçaklar <b><u>yüksek</u></b> irtifada gelir ve yalnızca <b><u>alçak</u></b> irtifada iner.\n<b><u>Alçak</u></b> irtifadan kalkar, <b><u>normal</u></b> veya <b><u>yüksek</u></b> irtifada ayrılır.\n<b><u>W</u></b>/<b><u>tekerlek yukarı</u></b> yükseltir, <b><u>S</u></b>/<b><u>aşağı</u></b> alçaltır.\n<b><u>Yüksek</u></b> irtifadaki uçaklar araziden etkilenmez."),
			("tutorial.page.speed.heading", "Hız"), ("tutorial.page.speed.description", "Hız <b><u>yavaş</u></b>, <b><u>normal</u></b> veya <b><u>hızlı</u></b> olabilir; güncel hız sağ altta görünür.\nGelen uçaklar <b><u>normal</u></b> hızdadır ve yavaş ya da normal hızda iner.\nKalkış hızı <b><u>normal</u></b>dir.\n<b><u>D</u></b>/<b><u>tekerlek yukarı</u></b> hızlandırır, <b><u>A</u></b>/<b><u>aşağı</u></b> yavaşlatır (<b><u>sol Shift</u></b> ile)."),
			("tutorial.page.type.heading", "Tip"), ("tutorial.page.type.description", "Uçaklar <b><u>hafif</u></b>, <b><u>orta</u></b> veya <b><u>ağır</u></b> olabilir. Gelen uçakların yakıtı sağ üstte görünür.\n<b><u>Hafif</u></b> uçaklar küçüktür, en fazla normal hızda uçar, yalnızca yavaş iner ve %50 daha hızlı döner.\n<b><u>Orta</u></b> uçaklar 3,5 gün, <b><u>ağır</u></b> uçaklar 4 gün yakıt taşır."),
			("tutorial.page.events.heading", "Olaylar"), ("tutorial.page.events.description", "Bazen kazalar olur.\nPistten çıkma pisti kapatabilir.\nYakıtı az olan uçak hemen inmek zorunda kalabilir.\nMotor arızası acil dönüş gerektirir.\nHava durumu alçak ve normal hava sahasını kısıtlı bölgeye çevirebilir."),
			("tutorial.page.wind.heading", "Rüzgâr"), ("tutorial.page.wind.description", "Rüzgâr kalkış ve inişi etkiler; yönü sol üstte görünür.\nTam kuyruk rüzgârında pas geçme veya kalkış iptali olasılığı yüksektir.\nPist ile açı 90 derece veya altındaysa olasılık %0'dır."),
			("tutorial.page.last.heading", "Son olarak"), ("tutorial.page.last.description", "\n<b><u>Tab</u></b> ile oyun metnini gösterip gizleyebilirsin.\nTCAS çarpışmadan önce yükselme veya alçalma emri verir.\nGPWS arazi çarpışmasından önce yükselme emri verir.\n3 bekleme alanı yükseltmesiyle başlarsın ve yükseltmeler iki kat hızlı gelir.\nSınır dışına çıkmak kısıtlı bölge ihlalidir.\nBir noktayı adlandırmak için yerleştirirken <b><u>Boşluk</u></b>'a bas."),
			("tutorial.page.thanks.heading", "Mini Gerçekçi Hava Trafik Kontrolü'nü oynadığın için teşekkürler!"), ("tutorial.page.thanks.description", "Daha fazla bilgi için <b><u><link=\"ENG\">hızlı başvuruya</link></u></b> bak."));
	}

	private static void RegisterUkrainian(Dictionary<string, Dictionary<string, string>> c)
	{
		RegisterLocale(c, "uk",
			("tutorial.title", "Міні-реалістичне керування повітряним рухом"), ("tutorial.qrh", "Короткий довідник"), ("tutorial.next", "Далі"),
			("tutorial.docsUrl", "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways"),
			("settings.wind", "Увімкнути вітер"), ("settings.events", "Увімкнути події"), ("settings.tcas", "Увімкнути TCAS і GPWS"),
			("hud.wind", "Вітер: {0}°"), ("hud.altitudePrefix", "ВИС: "), ("hud.speedPrefix", "ШВИД: "),
			("hud.fuel", "Пальне: {0}%"), ("hud.fuelInfinite", "Пальне: ∞"), ("hud.weight", "Тип: {0}"),
			("altitude.ground", "Земля"), ("altitude.low", "Низька"), ("altitude.normal", "Нормальна"), ("altitude.high", "Висока"),
			("weight.light", "Легкий"), ("weight.medium", "Середній"), ("weight.heavy", "Важкий"),
			("engine.mayday", "Mayday, Mayday, Mayday!"), ("engine.failure", "{0}. У нас відмова одного двигуна."), ("engine.return", "Нам потрібно негайно повернутися на аеродром."),
			("tutorial.page.altitude.heading", "Висота"), ("tutorial.page.altitude.description", "Літаки працюють на <b><u>низькій</u></b>, <b><u>нормальній</u></b> або <b><u>високій</u></b> висоті. Поточна висота показана внизу ліворуч.\nПрибуття входять на <b><u>високій</u></b> висоті й сідають лише на <b><u>низькій</u></b>.\nЗлітають із <b><u>низької</u></b> та вилітають на <b><u>нормальній</u></b> або <b><u>високій</u></b>.\n<b><u>W</u></b>/<b><u>колесо вгору</u></b> підвищує висоту, <b><u>S</u></b>/<b><u>вниз</u></b> знижує.\nРельєф не впливає на літаки на <b><u>високій</u></b> висоті."),
			("tutorial.page.speed.heading", "Швидкість"), ("tutorial.page.speed.description", "Швидкість буває <b><u>низькою</u></b>, <b><u>нормальною</u></b> або <b><u>високою</u></b>; вона показана внизу праворуч.\nПрибуття мають <b><u>нормальну</u></b> швидкість і сідають на низькій або нормальній.\nЗлітають із <b><u>нормальною</u></b> швидкістю.\n<b><u>D</u></b>/<b><u>колесо вгору</u></b> прискорює, <b><u>A</u></b>/<b><u>вниз</u></b> сповільнює (з <b><u>лівим Shift</u></b>)."),
			("tutorial.page.type.heading", "Тип"), ("tutorial.page.type.description", "Літаки бувають <b><u>легкі</u></b>, <b><u>середні</u></b> або <b><u>важкі</u></b>. Пальне прибуття показано вгорі праворуч.\n<b><u>Легкі</u></b> літаки малі, мають максимум нормальну швидкість, сідають лише повільно й повертають на 50% швидше.\n<b><u>Середні</u></b> мають 3,5 дня пального, <b><u>важкі</u></b> — 4 дні."),
			("tutorial.page.events.heading", "Події"), ("tutorial.page.events.description", "Іноді трапляються аварії.\nВикочування зі смуги може закрити її.\nЛітаку з малим запасом пального може знадобитися негайна посадка.\nВідмова двигуна вимагає негайного повернення.\nПогода може перетворити низький і нормальний простір на заборонену зону."),
			("tutorial.page.wind.heading", "Вітер"), ("tutorial.page.wind.description", "Вітер впливає на зліт і посадку; напрямок показано вгорі ліворуч.\nЗа повного попутного вітру ймовірність відходу на друге коло або припинення зльоту висока.\nЗа кута до смуги не більшого за 90 градусів вона дорівнює 0%."),
			("tutorial.page.last.heading", "Наостанок"), ("tutorial.page.last.description", "\n<b><u>Tab</u></b> показує або ховає текст гри.\nTCAS наказує набрати або знизити висоту перед зіткненням.\nGPWS наказує набрати висоту перед ударом об рельєф.\nНа початку є 3 покращення зони очікування, що надходять удвічі швидше.\nВиліт за межі є порушенням забороненої зони.\nНатисніть <b><u>Пробіл</u></b>, щоб назвати точку."),
			("tutorial.page.thanks.heading", "Дякуємо за гру в Міні-реалістичне керування повітряним рухом!"), ("tutorial.page.thanks.description", "Докладніше дивіться в <b><u><link=\"ENG\">короткому довіднику</link></u></b>."));
	}
}
