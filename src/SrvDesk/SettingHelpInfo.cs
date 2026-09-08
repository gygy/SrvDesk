namespace SrvDesk;

internal sealed class SettingHelpInfo
{
    public string Summary { get; }
    public string Purpose { get; }
    public string Benefit { get; }
    public string Guide { get; }
    public string Effect { get; }
    public SettingScope Scope { get; }
    /// <summary>对应系统哪里（弹窗/菜单/设置页），列表「说明」列用。</summary>
    public string UiPlace { get; }
    /// <summary>何时建议优化，列表「说明」列用。</summary>
    public string WhenHint { get; }
    /// <summary>推荐强度（列表「推荐值」列）。</summary>
    public RecommendLevel Recommend { get; }

    public SettingHelpInfo(
        string summary,
        string purpose,
        string benefit,
        string guide,
        string effect,
        SettingScope? scope = null,
        string? uiPlace = null,
        string? whenHint = null,
        RecommendLevel recommend = RecommendLevel.Suggested)
    {
        Summary = summary;
        Purpose = purpose;
        Benefit = benefit;
        Guide = guide;
        Effect = effect;
        Scope = scope ?? SettingScope.Universal;
        UiPlace = uiPlace ?? "";
        WhenHint = whenHint ?? "";
        Recommend = recommend;
    }

    /// <summary>列表「说明」列：先短建议，再对应位置（窄列时建议仍可见）。</summary>
    public string ListNote
    {
        get
        {
            var when = WhenHint.Length > 0 ? WhenHint : Compact(Guide, 14);
            var place = UiPlace.Length > 0 ? Compact(UiPlace, 28) : Compact(Summary, 28);
            if (when.Length == 0) return place;
            if (place.Length == 0) return when;
            return when + " · " + place;
        }
    }

    public string FormatDetail() =>
        Scope.FormatHelpSection() +
        "\r\n" + AppLang.L("【推荐】", "[Recommend] ") + RecommendLevelUi.Tip(Recommend) +
        (UiPlace.Length > 0 ? "\r\n" + AppLang.L("【对应】", "[Where] ") + UiPlace : "") +
        (WhenHint.Length > 0 ? "\r\n" + AppLang.L("【建议】", "[When] ") + WhenHint : "") +
        "\r\n" + AppLang.L("【作用】", "[What] ") + Purpose +
        "\r\n" + AppLang.L("【好处】", "[Benefit] ") + Benefit +
        "\r\n" + AppLang.L("【指引】", "[Guide] ") + Guide +
        "\r\n" + AppLang.L("【生效】", "[Effect] ") + Effect;


    private static string Compact(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim();
        var cut = t.IndexOfAny(['。', '；', ';']);
        if (cut > 0 && cut < max) t = t.Substring(0, cut);
        if (t.Length > max) t = t.Substring(0, max - 1) + "…";
        return t;
    }
}
