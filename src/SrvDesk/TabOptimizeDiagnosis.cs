namespace SrvDesk;

/// <summary>单条「未达推荐」的优化诊断。</summary>
internal sealed class TabOptimizeFinding
{
    public string TabTitle { get; set; } = "";
    public string SectionTitle { get; set; } = "";
    public string ItemTitle { get; set; } = "";
    public string CurrentValue { get; set; } = "";
    public string RecommendedValue { get; set; } = "";
    public RecommendLevel Level { get; set; }
    public string Hint { get; set; } = "";

    /// <summary>开关项：用主界面 SettingRow.ItemText 匹配。</summary>
    public bool IsService { get; set; }

    /// <summary>服务实际名称（仅 IsService）。</summary>
    public string ServiceName { get; set; } = "";

    /// <summary>服务推荐启动类型（仅 IsService）。</summary>
    public ServiceStartTypeKind ServiceTarget { get; set; } = ServiceStartTypeKind.Unknown;
}

/// <summary>按侧栏标签页归类的诊断组。</summary>
internal sealed class TabOptimizeGroup
{
    public string TabTitle { get; set; } = "";
    public IReadOnlyList<TabOptimizeFinding> Findings { get; set; } = Array.Empty<TabOptimizeFinding>();
}
