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
    public static string Title(RecommendLevel level) => level switch
    {
        RecommendLevel.Must => "必优化",
        RecommendLevel.Strong => "强烈推荐",
        RecommendLevel.Suggested => "建议优化",
        _ => "可选",
    };

    /// <summary>用实心/空心圆表示强度，兼容无彩色 emoji 的 Server 字体。</summary>
    public static string Icon(RecommendLevel level) => level switch
    {
        RecommendLevel.Must => "●●●",
        RecommendLevel.Strong => "●●○",
        RecommendLevel.Suggested => "●○○",
        _ => "○○○",
    };

    public static Color Color(RecommendLevel level) => level switch
    {
        RecommendLevel.Must => Color.FromArgb(198, 40, 40),
        RecommendLevel.Strong => Color.FromArgb(230, 126, 34),
        RecommendLevel.Suggested => Color.FromArgb(41, 98, 163),
        _ => AppTheme.TextMute,
    };

    public static string Tip(RecommendLevel level) =>
        Title(level) + " · " + level switch
        {
            RecommendLevel.Must => "Server 当桌面几乎必做，否则基础体验明显受限。",
            RecommendLevel.Strong => "个人/内网桌面强烈建议开启，收益高、风险可控。",
            RecommendLevel.Suggested => "多数场景值得开启，可按习惯取舍。",
            _ => "按需开启；有兼容性、安全或业务依赖时请谨慎。",
        };
}
