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
    /// <summary>推荐强度（列表「推荐值」列）——目录基准值；展示/顾问请用 <see cref="EffectiveRecommend"/>。</summary>
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

    /// <summary>按本机 OS/场景解析后的有效推荐强度。</summary>
    public RecommendLevel EffectiveRecommend(SystemFacts? facts) =>
        RecommendRules.Resolve(this, facts);

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

    public string FormatDetail() => FormatDetail(null);

    public string FormatDetail(SystemFacts? facts)
    {
        var level = EffectiveRecommend(facts);
        return FormatDetailBody(level) ;
    }

    /// <summary>悬浮说明内容：推荐星由界面彩色绘制，正文不含 ★ 字符。</summary>
    public (RecommendLevel Level, string Body) FormatDetailParts(SystemFacts? facts = null)
    {
        var level = EffectiveRecommend(facts);
        return (level, FormatDetailBody(level));
    }

    private string FormatDetailBody(RecommendLevel level)
    {
        var what = JoinSentences(Purpose, Benefit);
        return Scope.FormatHelpSection() +
        "\r\n" + AppLang.L("【推荐】", "[Recommend] ") + RecommendLevelUi.TipBody(level) +
        (UiPlace.Length > 0 ? "\r\n" + AppLang.L("【对应】", "[Where] ") + UiPlace : "") +
        (WhenHint.Length > 0 ? "\r\n" + AppLang.L("【建议】", "[When] ") + WhenHint : "") +
        (what.Length > 0 ? "\r\n" + AppLang.L("【作用】", "[What] ") + what : "") +
        "\r\n" + AppLang.L("【生效】", "[Effect] ") + Effect;
    }

    /// <summary>把作用与好处合成一句展示，避免悬浮说明重复分行。</summary>
    private static string JoinSentences(string a, string b)
    {
        a = (a ?? "").Trim();
        b = (b ?? "").Trim();
        if (a.Length == 0) return b;
        if (b.Length == 0) return a;
        if (a.EndsWith("。", StringComparison.Ordinal) || a.EndsWith(".", StringComparison.Ordinal)
            || a.EndsWith("；", StringComparison.Ordinal) || a.EndsWith(";", StringComparison.Ordinal))
            return a + b;
        return a + AppLang.L("。", ". ") + b;
    }

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
