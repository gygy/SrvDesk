using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

internal sealed class ChangePlanItem
{
    public string Module { get; set; } = "";
    public string Title { get; set; } = "";
    public string Action { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Impact { get; set; } = "";
    public OptimizeRisk Risk { get; set; }
    public bool Selected { get; set; } = true;
    public string Key { get; set; } = "";
}

internal sealed class ChangePlan
{
    public List<ChangePlanItem> Items { get; } = new();
    public OptimizationLevel Level { get; set; } = OptimizationLevel.Standard;
    public bool DryRunOnly { get; set; }

    public int CountLow => Items.Count(i => i.Risk == OptimizeRisk.Low);
    public int CountMedium => Items.Count(i => i.Risk == OptimizeRisk.Medium);
    public int CountHigh => Items.Count(i => i.Risk == OptimizeRisk.High);
    public int CountCritical => Items.Count(i => i.Risk == OptimizeRisk.Critical);

    public IEnumerable<ChangePlanItem> Executable =>
        Items.Where(i => i.Selected && OptimizationLevelUi.AllowsRisk(Level, i.Risk));

    public string SummaryText()
    {
        var n = Items.Count;
        return AppLang.Lf(
            "计划 {0} 项 · 低 {1} · 中 {2} · 高 {3} · 禁动 {4}",
            "Plan {0} · low {1} · med {2} · high {3} · blocked {4}",
            n, CountLow, CountMedium, CountHigh, CountCritical);
    }
}

[DataContract]
internal sealed class OptimizationHistoryEntry
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string TimeLocal { get; set; } = "";
    [DataMember] public string Title { get; set; } = "";
    [DataMember] public string Detail { get; set; } = "";
    [DataMember] public string ServiceSnapshotId { get; set; } = "";
    [DataMember] public string ProfileJson { get; set; } = "";
    [DataMember] public bool CanRollbackServices { get; set; }
}

[DataContract]
internal sealed class OptimizationHistoryFile
{
    [DataMember] public List<OptimizationHistoryEntry> Items { get; set; } = new();
}

internal static class OptimizationHistory
{
    private static string PathFile => AppPaths.Combine("optimization-history.json");

    public static IReadOnlyList<OptimizationHistoryEntry> List()
    {
        try
        {
            if (!File.Exists(PathFile)) return Array.Empty<OptimizationHistoryEntry>();
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(PathFile, Encoding.UTF8)));
            var file = new DataContractJsonSerializer(typeof(OptimizationHistoryFile)).ReadObject(ms) as OptimizationHistoryFile;
            return file?.Items?.OrderByDescending(i => i.TimeLocal).ToList()
                   ?? (IReadOnlyList<OptimizationHistoryEntry>)Array.Empty<OptimizationHistoryEntry>();
        }
        catch
        {
            return Array.Empty<OptimizationHistoryEntry>();
        }
    }

    public static void Add(string title, string detail, string? serviceSnapshotId = null, string? profileJson = null)
    {
        try
        {
            var file = new OptimizationHistoryFile { Items = List().ToList() };
            file.Items.Insert(0, new OptimizationHistoryEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                TimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Title = title,
                Detail = detail,
                ServiceSnapshotId = serviceSnapshotId ?? "",
                ProfileJson = profileJson ?? "",
                CanRollbackServices = !string.IsNullOrEmpty(serviceSnapshotId),
            });
            if (file.Items.Count > 50)
                file.Items = file.Items.Take(50).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(PathFile)!);
            using var ms = new MemoryStream();
            new DataContractJsonSerializer(typeof(OptimizationHistoryFile)).WriteObject(ms, file);
            File.WriteAllText(PathFile, Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("优化历史写入失败：" + ex.Message);
        }
    }
}

/// <summary>从开关差分与服务建议等生成变更计划。</summary>
internal static class ChangePlanBuilder
{
    public static ChangePlan FromToggleDiff(Optimizer.State target, Optimizer.State? baseline, OptimizationLevel level)
    {
        var plan = new ChangePlan { Level = level, DryRunOnly = level == OptimizationLevel.DetectOnly };
        try
        {
            var fields = typeof(Optimizer.State).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(bool)) continue;
                var tv = (bool)f.GetValue(target)!;
                var bv = baseline is null ? !tv : (bool)f.GetValue(baseline)!;
                if (tv == bv && baseline is not null) continue;
                if (baseline is null && !tv) continue;

                var risk = GuessToggleRisk(f.Name);
                plan.Items.Add(new ChangePlanItem
                {
                    Module = AppLang.L("系统开关", "Settings"),
                    Title = f.Name,
                    Action = tv
                        ? AppLang.L("开启", "Enable")
                        : AppLang.L("关闭", "Disable"),
                    Reason = AppLang.L("相对当前基线有差异。", "Differs from current baseline."),
                    Impact = AppLang.L("写入注册表/策略/服务相关设置。", "Writes registry/policy/service-related settings."),
                    Risk = risk,
                    Selected = OptimizationLevelUi.AllowsRisk(level, risk),
                    Key = "toggle:" + f.Name,
                });
            }
        }
        catch { /* ignore */ }
        return plan;
    }

    public static void AppendServiceSuggestions(ChangePlan plan, int max = 40)
    {
        try
        {
            var rows = ServiceOptimizeHelper.LoadApplicable(installedOnly: true);
            var added = 0;
            foreach (var row in rows)
            {
                if (added >= max) break;
                if (!row.CanOptimize) continue;
                if (ServerProfile.MustKeepService(row.ActualServiceName)) continue;
                var risk = ServiceRiskCatalog.GetRisk(row.ActualServiceName, row.Recommend);
                if (risk >= OptimizeRisk.High) continue;
                var want = row.Recommend switch
                {
                    ServiceRecommend.Disable => ServiceStartTypeKind.Disabled,
                    ServiceRecommend.Manual => ServiceStartTypeKind.Manual,
                    ServiceRecommend.Auto => ServiceStartTypeKind.Automatic,
                    _ => row.StartType,
                };
                plan.Items.Add(new ChangePlanItem
                {
                    Module = AppLang.L("服务", "Service"),
                    Title = row.DisplayName + " (" + row.ActualServiceName + ")",
                    Action = AppLang.L("启动类型 → ", "Start type → ") + ServiceOptimizeHelper.StartTypeLabel(want),
                    Reason = string.IsNullOrWhiteSpace(row.AdviceNote) ? row.Entry.Note : row.AdviceNote,
                    Impact = ServiceRiskCatalog.WhyKeep(row.ActualServiceName),
                    Risk = risk,
                    Selected = false,
                    Key = "svc:" + row.ActualServiceName,
                });
                added++;
            }
        }
        catch { /* ignore */ }
    }

    private static OptimizeRisk GuessToggleRisk(string fieldName)
    {
        var n = fieldName ?? "";
        if (n.IndexOf("Defender", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("Uac", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("Firewall", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("SmartScreen", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("Vbs", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("Wdac", StringComparison.OrdinalIgnoreCase) >= 0)
            return OptimizeRisk.High;
        if (n.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("Rdp", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("Smb", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("Remote", StringComparison.OrdinalIgnoreCase) >= 0)
            return OptimizeRisk.Medium;
        return OptimizeRisk.Low;
    }
}
