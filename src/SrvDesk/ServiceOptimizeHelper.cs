using System.ServiceProcess;
using Microsoft.Win32;

namespace SrvDesk;

internal enum ServiceStartTypeKind
{
    Boot = 0,
    System = 1,
    Automatic = 2,
    Manual = 3,
    Disabled = 4,
    Missing = -1,
    Unknown = -2,
}

internal sealed class ServiceOptimizeRow
{
    public ServiceOptimizeEntry Entry { get; }
    public string ActualServiceName { get; }
    public string DisplayName { get; }
    public ServiceStartTypeKind StartType { get; }
    public ServiceStartTypeKind SystemDefault { get; }
    public bool Running { get; }
    public ServiceRecommend Recommend { get; }
    /// <summary>处置标签：应禁用 / 应保留 / 按需 / 更新时开。</summary>
    public string AdviceTag { get; }
    /// <summary>简洁说明，如【应禁用】超级抓取，建议禁。</summary>
    public string AdviceNote { get; }

    public ServiceOptimizeRow(
        ServiceOptimizeEntry entry,
        string actualServiceName,
        string displayName,
        ServiceStartTypeKind startType,
        ServiceStartTypeKind systemDefault,
        bool running,
        ServiceRecommend recommend,
        string adviceTag,
        string adviceNote)
    {
        Entry = entry;
        ActualServiceName = actualServiceName;
        DisplayName = displayName;
        StartType = startType;
        SystemDefault = systemDefault;
        Running = running;
        Recommend = recommend;
        AdviceTag = adviceTag;
        AdviceNote = adviceNote;
    }

    public bool Installed => StartType != ServiceStartTypeKind.Missing;

    /// <summary>推荐值：越高越优先优化（禁用建议最高）。</summary>
    public int RecommendScore => Recommend switch
    {
        ServiceRecommend.Disable => 90,
        ServiceRecommend.Manual => 60,
        ServiceRecommend.Auto => 40,
        _ => 0,
    };

    /// <summary>与桌面优化「推荐值」同一套五星等级：表示仍需优化的程度。</summary>
    public RecommendLevel OptimizeLevel
    {
        get
        {
            if (!CanOptimize)
                return RecommendLevel.Optional;
            return Recommend switch
            {
                ServiceRecommend.Disable => RecommendLevel.Must,
                ServiceRecommend.Manual => RecommendLevel.Strong,
                ServiceRecommend.Auto => RecommendLevel.Suggested,
                _ => RecommendLevel.Optional,
            };
        }
    }

    public bool MatchesRecommend
    {
        get
        {
            if (!Installed) return true;
            return Recommend switch
            {
                ServiceRecommend.Disable => StartType == ServiceStartTypeKind.Disabled,
                ServiceRecommend.Manual => StartType == ServiceStartTypeKind.Manual,
                ServiceRecommend.Auto => StartType == ServiceStartTypeKind.Automatic,
                _ => true,
            };
        }
    }

    public bool CanOptimize =>
        Installed && Recommend != ServiceRecommend.Keep && !MatchesRecommend;

    public bool CanRestoreDefault =>
        Installed
        && SystemDefault is ServiceStartTypeKind.Automatic
            or ServiceStartTypeKind.Manual
            or ServiceStartTypeKind.Disabled
        && SystemDefault != StartType;
}

internal static class ServiceOptimizeHelper
{
    public static ServiceOsTarget DetectOsTarget()
    {
        if (Optimizer.IsWindowsServer())
            return ServiceOsTarget.Server;

        var build = GetCurrentBuild();
        return build >= 22000 ? ServiceOsTarget.Win11 : ServiceOsTarget.Win10;
    }

    public static string OsLabel(ServiceOsTarget os) => os switch
    {
        ServiceOsTarget.Server => AppLang.L("Windows Server", "Windows Server"),
        ServiceOsTarget.Win11 => AppLang.L("Windows 11", "Windows 11"),
        ServiceOsTarget.Win10 => AppLang.L("Windows 10", "Windows 10"),
        _ => AppLang.L("未知", "Unknown"),
    };

    public static string StartTypeLabel(ServiceStartTypeKind kind) => kind switch
    {
        ServiceStartTypeKind.Automatic => AppLang.L("自动", "Automatic"),
        ServiceStartTypeKind.Manual => AppLang.L("手动", "Manual"),
        ServiceStartTypeKind.Disabled => AppLang.L("禁用", "Disabled"),
        ServiceStartTypeKind.Boot => AppLang.L("引导", "Boot"),
        ServiceStartTypeKind.System => AppLang.L("系统", "System"),
        ServiceStartTypeKind.Missing => AppLang.L("未安装", "Not installed"),
        _ => AppLang.L("未知", "Unknown"),
    };

    public static string RecommendLabel(ServiceRecommend r) => r switch
    {
        ServiceRecommend.Disable => AppLang.L("建议禁用", "Suggest disable"),
        ServiceRecommend.Manual => AppLang.L("建议手动", "Suggest manual"),
        ServiceRecommend.Auto => AppLang.L("建议自动", "Suggest automatic"),
        _ => AppLang.L("保持", "Keep"),
    };

    public static string RecommendShort(ServiceRecommend r) => r switch
    {
        ServiceRecommend.Disable => AppLang.L("禁用", "Disable"),
        ServiceRecommend.Manual => AppLang.L("手动", "Manual"),
        ServiceRecommend.Auto => AppLang.L("自动", "Automatic"),
        _ => "—",
    };

    public static string RecommendScoreLabel(int score, ServiceRecommend r) =>
        score <= 0 ? "—" : $"{score} · {RecommendShort(r)}";

    public static string RecommendStarsText(ServiceOptimizeRow row) =>
        RecommendLevelUi.Icon(row.OptimizeLevel);

    public static string OsScopeLabel(ServiceOsTarget os)
    {
        var parts = new List<string>(3);
        if ((os & ServiceOsTarget.Win10) != 0) parts.Add("Win10");
        if ((os & ServiceOsTarget.Win11) != 0) parts.Add("Win11");
        if ((os & ServiceOsTarget.Server) != 0) parts.Add("Server");
        return parts.Count == 0 ? "-" : string.Join("/", parts);
    }

    public static List<ServiceOptimizeRow> LoadApplicable(bool installedOnly = true)
    {
        var currentOs = DetectOsTarget();
        var isServer = currentOs == ServiceOsTarget.Server;
        var rows = new List<ServiceOptimizeRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 本机 SCM 动态枚举全部 Win32 服务（与 services.msc 同源）
        ServiceController[] services;
        try { services = ServiceController.GetServices(); }
        catch { services = []; }

        foreach (var sc in services)
        {
            try
            {
                var name = sc.ServiceName;
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                    continue;

                var display = string.IsNullOrWhiteSpace(sc.DisplayName) ? name : sc.DisplayName;
                var running = sc.Status is ServiceControllerStatus.Running
                    or ServiceControllerStatus.StartPending;
                var start = ReadStartType(name);
                var catalog = ServiceOptimizeCatalog.Find(name);
                var hint = ServiceOptimizeLtscHints.Find(name, display);
                var entry = catalog ?? MakeDiscoveredEntry(name, display, currentOs, hint);
                var recommend = ResolveRecommend(catalog, hint, currentOs, isServer);
                var (tag, note) = ResolveAdvice(entry, recommend, hint);
                var sysDefault = ServiceOptimizeCatalog.SystemDefaultFor(name, currentOs);

                rows.Add(new ServiceOptimizeRow(
                    entry, name, display, start, sysDefault, running, recommend, tag, note));
            }
            finally
            {
                sc.Dispose();
            }
        }

        // 可选：目录里对本机有建议、但 SCM 中不存在的项
        if (!installedOnly)
        {
            foreach (var entry in ServiceOptimizeCatalog.All)
            {
                if (!entry.AppliesTo(currentOs)) continue;
                if (entry.IsPrefix)
                {
                    var any = false;
                    foreach (var n in seen)
                    {
                        if (n.Equals(entry.ServiceName, StringComparison.OrdinalIgnoreCase)
                            || n.StartsWith(entry.ServiceName + "_", StringComparison.OrdinalIgnoreCase))
                        {
                            any = true;
                            break;
                        }
                    }
                    if (!any)
                        rows.Add(BuildMissing(entry, entry.ServiceName + "_*", currentOs, isServer));
                    continue;
                }

                if (!seen.Contains(entry.ServiceName))
                    rows.Add(BuildMissing(entry, entry.ServiceName, currentOs, isServer));
            }
        }

        rows.Sort(CompareRows);
        return rows;
    }

    /// <summary>推荐值高优先；当前已禁用的排后面；同组内可优化优先。</summary>
    public static int CompareRows(ServiceOptimizeRow a, ServiceOptimizeRow b)
    {
        var aDis = a.StartType == ServiceStartTypeKind.Disabled ? 1 : 0;
        var bDis = b.StartType == ServiceStartTypeKind.Disabled ? 1 : 0;
        if (aDis != bDis) return aDis.CompareTo(bDis);

        var score = b.RecommendScore.CompareTo(a.RecommendScore);
        if (score != 0) return score;

        var ao = a.CanOptimize ? 0 : 1;
        var bo = b.CanOptimize ? 0 : 1;
        if (ao != bo) return ao.CompareTo(bo);

        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>目录未收录的本机服务：仅展示；若有 LTSC 备注则带上处置说明。</summary>
    private static ServiceOptimizeEntry MakeDiscoveredEntry(
        string serviceName,
        string displayName,
        ServiceOsTarget currentOs,
        ServiceOptimizeLtscHints.Hint? hint)
    {
        var noteZh = hint?.Note
            ?? "本机已安装；暂无专项优化建议，请按业务需要调整。";
        var noteEn = hint is not null
            ? hint.Value.Note
            : "Installed on this PC; no curated advice — adjust as needed.";
        var rec = hint?.Recommend ?? ServiceRecommend.Keep;
        return new(
            serviceName,
            displayName,
            displayName,
            hint is null ? "其他服务" : "LTSC 参考",
            hint is null ? "Other" : "LTSC tips",
            currentOs,
            rec,
            rec,
            noteZh,
            noteEn);
    }

    private static ServiceRecommend ResolveRecommend(
        ServiceOptimizeEntry? catalog,
        ServiceOptimizeLtscHints.Hint? hint,
        ServiceOsTarget currentOs,
        bool isServer)
    {
        // 桌面/LTSC：Excel 备注优先；Server：目录 Server 建议优先，避免误禁 RDP 等
        if (!isServer && hint is not null)
            return hint.Value.Recommend;

        if (catalog is not null && catalog.AppliesTo(currentOs))
            return catalog.Recommend(isServer);

        if (hint is not null)
        {
            // Server 上把「应禁用」降为手动，避免远程场景误伤
            return hint.Value.Recommend == ServiceRecommend.Disable
                ? ServiceRecommend.Manual
                : hint.Value.Recommend;
        }

        return ServiceRecommend.Keep;
    }

    private static (string Tag, string Note) ResolveAdvice(
        ServiceOptimizeEntry entry,
        ServiceRecommend recommend,
        ServiceOptimizeLtscHints.Hint? hint)
    {
        if (hint is not null)
            return (hint.Value.Tag, hint.Value.Note);

        var tag = recommend switch
        {
            ServiceRecommend.Disable => AppLang.L("应禁用", "Disable"),
            ServiceRecommend.Auto => AppLang.L("应启用", "Enable"),
            ServiceRecommend.Manual => AppLang.L("按需", "As needed"),
            _ => AppLang.L("应保留", "Keep"),
        };
        var note = entry.Note;
        if (!note.StartsWith("【", StringComparison.Ordinal))
            note = "【" + tag + "】" + note;
        return (tag, note);
    }

    public static void SetStartType(string serviceName, ServiceStartTypeKind target)
    {
        if (target is not (ServiceStartTypeKind.Automatic or ServiceStartTypeKind.Manual or ServiceStartTypeKind.Disabled))
            throw new InvalidOperationException(AppLang.L("仅支持自动 / 手动 / 禁用。", "Only Automatic / Manual / Disabled are supported."));

        var scType = target switch
        {
            ServiceStartTypeKind.Automatic => "auto",
            ServiceStartTypeKind.Manual => "demand",
            _ => "disabled",
        };

        if (target == ServiceStartTypeKind.Disabled)
            RunScAllowBenign($"stop {serviceName}");

        RunSc($"config {serviceName} start= {scType}");

        if (target == ServiceStartTypeKind.Automatic)
            RunScAllowBenign($"start {serviceName}");
    }

    public static void ApplyRecommend(ServiceOptimizeRow row)
    {
        if (!row.Installed || row.Recommend == ServiceRecommend.Keep)
            return;
        var target = row.Recommend switch
        {
            ServiceRecommend.Disable => ServiceStartTypeKind.Disabled,
            ServiceRecommend.Manual => ServiceStartTypeKind.Manual,
            ServiceRecommend.Auto => ServiceStartTypeKind.Automatic,
            _ => row.StartType,
        };
        if (target == row.StartType) return;
        SetStartType(row.ActualServiceName, target);
    }

    public static void RestoreSystemDefault(ServiceOptimizeRow row)
    {
        if (!row.CanRestoreDefault) return;
        SetStartType(row.ActualServiceName, row.SystemDefault);
    }

    private static ServiceOptimizeRow BuildMissing(
        ServiceOptimizeEntry entry, string name, ServiceOsTarget os, bool isServer)
    {
        var hint = ServiceOptimizeLtscHints.Find(entry.ServiceName, entry.Title);
        var recommend = ResolveRecommend(entry, hint, os, isServer);
        var (tag, note) = ResolveAdvice(entry, recommend, hint);
        return new(
            entry,
            name,
            entry.Title,
            ServiceStartTypeKind.Missing,
            ServiceOptimizeCatalog.SystemDefaultFor(entry.ServiceName, os),
            false,
            recommend,
            tag,
            note);
    }

    private static ServiceStartTypeKind ReadStartType(string serviceName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName);
            if (key is null) return ServiceStartTypeKind.Missing;
            if (key.GetValue("Start") is int i && i is >= 0 and <= 4)
                return (ServiceStartTypeKind)i;
            return ServiceStartTypeKind.Unknown;
        }
        catch
        {
            return ServiceStartTypeKind.Unknown;
        }
    }

    /// <summary>供快照 Diff/还原读取当前启动类型。</summary>
    public static ServiceStartTypeKind ReadStartTypePublic(string serviceName) =>
        ReadStartType(serviceName);

    private static int GetCurrentBuild()
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var build = key?.GetValue("CurrentBuildNumber") as string;
            return int.TryParse(build, out var n) ? n : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static void RunSc(string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("sc.exe");
        p.WaitForExit(15000);
        if (p.ExitCode != 0)
        {
            var err = (p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd()).Trim();
            throw new InvalidOperationException(
                string.IsNullOrEmpty(err)
                    ? AppLang.Lf("sc.exe 失败（{0}）：{1}", "sc.exe failed ({0}): {1}", p.ExitCode, args)
                    : err);
        }
    }

    private static void RunScAllowBenign(string args)
    {
        try { RunSc(args); }
        catch { /* 已停止 / 已运行等 */ }
    }
}
