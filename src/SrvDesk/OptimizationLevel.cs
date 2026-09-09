namespace SrvDesk;

/// <summary>优化等级：控制干跑/可执行风险范围。</summary>
internal enum OptimizationLevel
{
    /// <summary>仅检测与建议，不写入系统。</summary>
    DetectOnly = 0,
    /// <summary>仅低风险。</summary>
    Safe = 1,
    /// <summary>低 + 中风险。</summary>
    Standard = 2,
    /// <summary>含深度项，须逐项确认。</summary>
    Deep = 3,
}

internal enum OptimizeRisk
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}

internal static class OptimizationLevelUi
{
    public static string Title(OptimizationLevel level) => level switch
    {
        OptimizationLevel.DetectOnly => AppLang.L("仅检测", "Detect only"),
        OptimizationLevel.Safe => AppLang.L("安全优化", "Safe"),
        OptimizationLevel.Standard => AppLang.L("标准优化", "Standard"),
        OptimizationLevel.Deep => AppLang.L("深度优化", "Deep"),
        _ => level.ToString(),
    };

    public static string Describe(OptimizationLevel level) => level switch
    {
        OptimizationLevel.DetectOnly => AppLang.L("只扫描与生成建议，不修改系统。", "Scan and suggest only; no system changes."),
        OptimizationLevel.Safe => AppLang.L("仅执行低风险项。", "Apply low-risk items only."),
        OptimizationLevel.Standard => AppLang.L("执行低/中风险项。", "Apply low and medium risk items."),
        OptimizationLevel.Deep => AppLang.L("含系统级调整，执行前需确认。", "Includes deep changes; confirm before apply."),
        _ => "",
    };

    public static string RiskText(OptimizeRisk risk) => risk switch
    {
        OptimizeRisk.Low => AppLang.L("低", "Low"),
        OptimizeRisk.Medium => AppLang.L("中", "Medium"),
        OptimizeRisk.High => AppLang.L("高", "High"),
        OptimizeRisk.Critical => AppLang.L("禁止", "Critical"),
        _ => "?",
    };

    public static Color RiskColor(OptimizeRisk risk) => risk switch
    {
        OptimizeRisk.Low => Color.FromArgb(46, 160, 67),
        OptimizeRisk.Medium => Color.FromArgb(200, 140, 20),
        OptimizeRisk.High => Color.FromArgb(200, 80, 60),
        OptimizeRisk.Critical => Color.FromArgb(160, 40, 40),
        _ => AppTheme.TextMute,
    };

    public static bool AllowsRisk(OptimizationLevel level, OptimizeRisk risk)
    {
        if (risk == OptimizeRisk.Critical) return false;
        return level switch
        {
            OptimizationLevel.DetectOnly => false,
            OptimizationLevel.Safe => risk == OptimizeRisk.Low,
            OptimizationLevel.Standard => risk <= OptimizeRisk.Medium,
            OptimizationLevel.Deep => risk <= OptimizeRisk.High,
            _ => false,
        };
    }
}
