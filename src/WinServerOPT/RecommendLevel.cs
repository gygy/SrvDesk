namespace WinOpt;

/// <summary>优化推荐强度（列表「推荐值」列）。</summary>
internal enum RecommendLevel
{
    /// <summary>按需开启，默认不必开。</summary>
    Optional = 0,
    /// <summary>多数桌面场景值得开。</summary>
    Suggested = 1,
    /// <summary>Server 当桌面时强烈建议开。</summary>
    Strong = 2,
    /// <summary>几乎必做，不开体验/可用性明显差。</summary>
    Must = 3,
}

internal static class RecommendLevelUi
{
    /// <summary>实心星：偏暖琥珀金，对比清晰。</summary>
    public static readonly Color StarOn = Color.FromArgb(230, 145, 12);
    /// <summary>未亮星：同字形灰色实心星（不用☆，避免字形宽窄不一）。</summary>
    public static readonly Color StarOff = Color.FromArgb(168, 176, 188);

    /// <summary>单星步进（像素），尽量紧凑仍可辨。</summary>
    public const int StarStep = 11;
    public const float StarFontSize = 10f;

    public static string Title(RecommendLevel level) => level switch
    {
        RecommendLevel.Must => "必优化",
        RecommendLevel.Strong => "强烈推荐",
        RecommendLevel.Suggested => "建议优化",
        _ => "可选",
    };

    /// <summary>亮星数量：5=必优化，4=强烈，3=建议，1=可选。</summary>
    public static int StarsOn(RecommendLevel level) => level switch
    {
        RecommendLevel.Must => 5,
        RecommendLevel.Strong => 4,
        RecommendLevel.Suggested => 3,
        _ => 1,
    };

    public static int StarsBlockWidth => StarStep * 5;

    /// <summary>五星纯文本（提示/日志）；列表用自绘着色。</summary>
    public static string Icon(RecommendLevel level)
    {
        var on = StarsOn(level);
        return new string('★', on) + new string('☆', 5 - on);
    }

    public static Color ForeColorOf(RecommendLevel level) =>
        level == RecommendLevel.Optional ? StarOff : StarOn;

    public static string Tip(RecommendLevel level) =>
        $"{Icon(level)} {Title(level)}（{StarsOn(level)}/5） · " + level switch
        {
            RecommendLevel.Must => "Server 当桌面几乎必做，否则基础体验会明显变差。",
            RecommendLevel.Strong => "个人/内网桌面建议开，改动面小、收益明确。",
            RecommendLevel.Suggested => "多数场景可开，按习惯取舍即可。",
            _ => "按需开启；有兼容性、安全或业务依赖时请谨慎。",
        };

    public static string LegendShort =>
        "★★★★★ 必优化 · ★★★★☆ 强烈推荐 · ★★★☆☆ 建议优化 · ★☆☆☆☆ 可选";
}
