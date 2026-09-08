using System.Globalization;

namespace SrvDesk;

/// <summary>界面语言：自动跟随系统，或手动简体中文 / English。</summary>
internal enum AppLanguageMode
{
    Auto = 0,
    ZhHans = 1,
    En = 2,
}

/// <summary>双语文案与启动时文化设置。</summary>
internal static class AppLang
{
    public static bool IsEnglish { get; private set; }

    public static AppLanguageMode Mode { get; private set; } = AppLanguageMode.Auto;

    /// <summary>按偏好应用语言（须在创建任何窗体之前调用）。</summary>
    public static void Apply(UiPrefsData? prefs = null)
    {
        prefs ??= UiPrefs.Load();
        Mode = ParseMode(prefs.Language);
        IsEnglish = ResolveIsEnglish(Mode);
        var culture = IsEnglish
            ? CultureInfo.GetCultureInfo("en-US")
            : CultureInfo.GetCultureInfo("zh-CN");
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
    }

    public static string L(string zh, string en) =>
        IsEnglish ? (string.IsNullOrEmpty(en) ? zh : en) : zh;

    public static string Lf(string zh, string en, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, L(zh, en), args);

    public static AppLanguageMode ParseMode(string? raw)
    {
        var value = (raw ?? "").Trim();
        if (value.Length == 0 || value.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return AppLanguageMode.Auto;
        if (value.Equals("en", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
            return AppLanguageMode.En;
        if (value.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)
            || value.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)
            || value.Equals("zh", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("zh-", StringComparison.OrdinalIgnoreCase))
            return AppLanguageMode.ZhHans;
        return AppLanguageMode.Auto;
    }

    public static string ToPrefsValue(AppLanguageMode mode) => mode switch
    {
        AppLanguageMode.En => "en",
        AppLanguageMode.ZhHans => "zh-Hans",
        _ => "auto",
    };

    public static string ModeDisplayName(AppLanguageMode mode) => mode switch
    {
        AppLanguageMode.En => "English",
        AppLanguageMode.ZhHans => L("简体中文", "Simplified Chinese"),
        _ => L("自动（跟随系统）", "Auto (system)"),
    };

    static bool ResolveIsEnglish(AppLanguageMode mode) => mode switch
    {
        AppLanguageMode.En => true,
        AppLanguageMode.ZhHans => false,
        _ => !IsSystemChineseUi(),
    };

    /// <summary>系统 UI 语言是否为中文（含 zh-TW，本轮统一走简体词库）。</summary>
    public static bool IsSystemChineseUi()
    {
        try
        {
            var ui = CultureInfo.InstalledUICulture ?? CultureInfo.CurrentUICulture;
            if (ui.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                return true;
            var name = ui.Name ?? "";
            return name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}
