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
    /// <summary>统一金色实心星，靠「亮星数量」一眼区分（参考五星推荐）。</summary>
    public static readonly Color StarOn = Color.FromArgb(242, 169, 0);
    public static readonly Color StarOff = Color.FromArgb(210, 214, 220);

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

    /// <summary>五星字符串（实心★ + 空心☆），列表用 OwnerDraw 着色更清晰。</summary>
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
            RecommendLevel.Must => "Server 当桌面几乎必做，否则基础体验明显受限。",
            RecommendLevel.Strong => "个人/内网桌面强烈建议开启，收益高、风险可控。",
            RecommendLevel.Suggested => "多数场景值得开启，可按习惯取舍。",
            _ => "按需开启；有兼容性、安全或业务依赖时请谨慎。",
        };

    public static string LegendShort =>
        "★★★★★ 必优化 · ★★★★☆ 强烈推荐 · ★★★☆☆ 建议优化 · ★☆☆☆☆ 可选";
}
