using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

[DataContract]
internal sealed class ServiceSnapshotFile
{
    [DataMember] public int Version { get; set; } = 1;
    [DataMember] public string? CreatedAt { get; set; }
    [DataMember] public string? Os { get; set; }
    [DataMember] public string? Machine { get; set; }
    /// <summary>manual | before-apply | export</summary>
    [DataMember] public string? Reason { get; set; }
    [DataMember] public string? Note { get; set; }
    [DataMember] public List<ServiceSnapshotEntry>? Services { get; set; }
}

[DataContract]
internal sealed class ServiceSnapshotEntry
{
    [DataMember] public string? Name { get; set; }
    [DataMember] public string? DisplayName { get; set; }
    /// <summary>Automatic / Manual / Disabled / Boot / System / Unknown</summary>
    [DataMember] public string? StartType { get; set; }
}

internal sealed class ServiceSnapshotInfo
{
    public string Path { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Reason { get; set; } = "";
    public string Os { get; set; } = "";
    public string Machine { get; set; } = "";
    public string Note { get; set; } = "";
    public int ServiceCount { get; set; }

    public string ReasonLabel => Reason switch
    {
        "before-apply" => AppLang.L("改前自动备份", "Auto before change"),
        "manual" => AppLang.L("手动备份", "Manual"),
        "export" => AppLang.L("导出", "Export"),
        _ => string.IsNullOrEmpty(Reason) ? "—" : Reason,
    };

    public string ListLabel =>
        $"{CreatedAt:yyyy-MM-dd HH:mm:ss}  ·  {ReasonLabel}  ·  {ServiceCount}"
        + AppLang.L(" 项", " items");
}

internal sealed class ServiceSnapshotDiff
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ServiceStartTypeKind BackupStart { get; set; }
    public ServiceStartTypeKind CurrentStart { get; set; }
    public bool MissingNow { get; set; }
    public bool CanRestore =>
        !MissingNow
        && BackupStart is ServiceStartTypeKind.Automatic
            or ServiceStartTypeKind.Manual
            or ServiceStartTypeKind.Disabled
        && BackupStart != CurrentStart;
}

internal sealed class ServiceSnapshotRestoreResult
{
    public int Applied { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; } = [];
}

/// <summary>
/// 服务启动类型快照：%DataRoot%\service-snapshots\*.json
/// </summary>
internal static class ServiceSnapshotStore
{
    public const int MaxKeep = 20;
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private static DateTime _lastAutoUtc = DateTime.MinValue;
    private static string? _lastAutoPath;

    public static string SnapshotDir
    {
        get
        {
            var dir = AppPaths.Combine("service-snapshots");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string StartTypeToText(ServiceStartTypeKind kind) => kind switch
    {
        ServiceStartTypeKind.Automatic => "Automatic",
        ServiceStartTypeKind.Manual => "Manual",
        ServiceStartTypeKind.Disabled => "Disabled",
        ServiceStartTypeKind.Boot => "Boot",
        ServiceStartTypeKind.System => "System",
        ServiceStartTypeKind.Missing => "Missing",
        _ => "Unknown",
    };

    public static ServiceStartTypeKind ParseStartType(string? text) => (text ?? "").Trim() switch
    {
        "Automatic" or "Auto" or "2" => ServiceStartTypeKind.Automatic,
        "Manual" or "Demand" or "3" => ServiceStartTypeKind.Manual,
        "Disabled" or "4" => ServiceStartTypeKind.Disabled,
        "Boot" or "0" => ServiceStartTypeKind.Boot,
        "System" or "1" => ServiceStartTypeKind.System,
        "Missing" or "-1" => ServiceStartTypeKind.Missing,
        _ => ServiceStartTypeKind.Unknown,
    };

    /// <summary>捕获本机全部 Win32 服务启动类型并落盘。</summary>
    public static ServiceSnapshotInfo Capture(string reason, string? note = null)
    {
        var file = new ServiceSnapshotFile
        {
            Version = 1,
            CreatedAt = DateTime.Now.ToString("o"),
            Os = ServiceOptimizeHelper.OsLabel(ServiceOptimizeHelper.DetectOsTarget()),
            Machine = Environment.MachineName,
            Reason = reason,
            Note = note,
            Services = CaptureEntries(),
        };

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var prefix = reason == "before-apply" ? "auto" : "manual";
        var path = Path.Combine(SnapshotDir, $"{prefix}-{stamp}.json");
        // 同秒冲突时加序号
        for (var i = 0; i < 20 && File.Exists(path); i++)
            path = Path.Combine(SnapshotDir, $"{prefix}-{stamp}-{i + 1}.json");

        File.WriteAllText(path, Serialize(file), Utf8);
        TrimOld();

        if (reason == "before-apply")
        {
            _lastAutoUtc = DateTime.UtcNow;
            _lastAutoPath = path;
        }

        ApplyLog.Write(AppLang.Lf(
            "服务快照已保存（{0}，{1} 项）：{2}",
            "Service snapshot saved ({0}, {1} items): {2}",
            reason, file.Services?.Count ?? 0, path));

        return ToInfo(path, file);
    }

    /// <summary>
    /// 批量改之前自动备份。距上次自动备份不足 2 分钟则复用，避免连点刷盘。
    /// </summary>
    public static ServiceSnapshotInfo? EnsureAutoBackupBeforeChange()
    {
        if (_lastAutoPath is not null
            && File.Exists(_lastAutoPath)
            && (DateTime.UtcNow - _lastAutoUtc).TotalMinutes < 2)
            return TryReadInfo(_lastAutoPath);

        try
        {
            return Capture("before-apply",
                AppLang.L("批量修改服务启动类型前自动备份", "Auto backup before changing service start types"));
        }
        catch (Exception ex)
        {
            ApplyLog.Write(AppLang.L("服务自动备份失败：", "Service auto-backup failed: ") + ex.Message);
            return null;
        }
    }

    public static List<ServiceSnapshotInfo> ListSnapshots()
    {
        var list = new List<ServiceSnapshotInfo>();
        foreach (var path in Directory.EnumerateFiles(SnapshotDir, "*.json"))
        {
            var info = TryReadInfo(path);
            if (info is not null)
                list.Add(info);
        }

        list.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        return list;
    }

    public static ServiceSnapshotFile? Load(string path)
    {
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return Deserialize(json);
        }
        catch
        {
            return null;
        }
    }

    public static ServiceSnapshotInfo? TryReadInfo(string path)
    {
        var file = Load(path);
        return file is null ? null : ToInfo(path, file);
    }

    public static void ExportCopy(string sourcePath, string destPath)
    {
        File.Copy(sourcePath, destPath, overwrite: true);
    }

    public static void OpenSnapshotFolder()
    {
        Directory.CreateDirectory(SnapshotDir);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = SnapshotDir,
            UseShellExecute = true,
        });
    }

    public static List<ServiceSnapshotDiff> Diff(ServiceSnapshotFile snap)
    {
        var result = new List<ServiceSnapshotDiff>();
        if (snap.Services is null) return result;

        foreach (var e in snap.Services)
        {
            if (string.IsNullOrWhiteSpace(e.Name)) continue;
            var name = e.Name!;
            var backup = ParseStartType(e.StartType);
            var current = ServiceOptimizeHelper.ReadStartTypePublic(name);
            var missing = current == ServiceStartTypeKind.Missing;
            if (missing || backup != current)
            {
                result.Add(new ServiceSnapshotDiff
                {
                    Name = name,
                    DisplayName = string.IsNullOrWhiteSpace(e.DisplayName) ? name : e.DisplayName!,
                    BackupStart = backup,
                    CurrentStart = current,
                    MissingNow = missing,
                });
            }
        }

        result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public static ServiceSnapshotRestoreResult Restore(
        ServiceSnapshotFile snap,
        IEnumerable<string>? onlyNames = null)
    {
        var result = new ServiceSnapshotRestoreResult();
        var filter = onlyNames is null
            ? null
            : new HashSet<string>(onlyNames, StringComparer.OrdinalIgnoreCase);

        if (snap.Services is null) return result;

        foreach (var e in snap.Services)
        {
            if (string.IsNullOrWhiteSpace(e.Name)) continue;
            var name = e.Name!;
            if (filter is not null && !filter.Contains(name)) continue;

            var target = ParseStartType(e.StartType);
            if (target is not (ServiceStartTypeKind.Automatic
                or ServiceStartTypeKind.Manual
                or ServiceStartTypeKind.Disabled))
            {
                result.Skipped++;
                continue;
            }

            var current = ServiceOptimizeHelper.ReadStartTypePublic(name);
            if (current == ServiceStartTypeKind.Missing)
            {
                result.Skipped++;
                continue;
            }

            if (current == target)
            {
                result.Skipped++;
                continue;
            }

            try
            {
                using (ApplyLog.PushContext(name))
                    ServiceOptimizeHelper.SetStartType(name, target);
                result.Applied++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"{name}: {ex.Message}");
            }
        }

        ApplyLog.Write(AppLang.Lf(
            "服务快照还原：成功 {0}，跳过 {1}，失败 {2}",
            "Service snapshot restore: applied {0}, skipped {1}, failed {2}",
            result.Applied, result.Skipped, result.Failed));
        return result;
    }

    private static List<ServiceSnapshotEntry> CaptureEntries()
    {
        var list = new List<ServiceSnapshotEntry>();
        System.ServiceProcess.ServiceController[] services;
        try { services = System.ServiceProcess.ServiceController.GetServices(); }
        catch { return list; }

        foreach (var sc in services)
        {
            try
            {
                var name = sc.ServiceName;
                if (string.IsNullOrWhiteSpace(name)) continue;
                var display = string.IsNullOrWhiteSpace(sc.DisplayName) ? name : sc.DisplayName;
                var start = ServiceOptimizeHelper.ReadStartTypePublic(name);
                list.Add(new ServiceSnapshotEntry
                {
                    Name = name,
                    DisplayName = display,
                    StartType = StartTypeToText(start),
                });
            }
            finally
            {
                sc.Dispose();
            }
        }

        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    private static void TrimOld()
    {
        try
        {
            var files = Directory.GetFiles(SnapshotDir, "*.json")
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();
            for (var i = MaxKeep; i < files.Count; i++)
            {
                try { files[i].Delete(); }
                catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }
    }

    private static ServiceSnapshotInfo ToInfo(string path, ServiceSnapshotFile file)
    {
        var created = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(file.CreatedAt)
            && DateTime.TryParse(file.CreatedAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            created = parsed.ToLocalTime();
        else
        {
            try { created = File.GetCreationTime(path); }
            catch { /* ignore */ }
        }

        return new ServiceSnapshotInfo
        {
            Path = path,
            CreatedAt = created,
            Reason = file.Reason ?? "",
            Os = file.Os ?? "",
            Machine = file.Machine ?? "",
            Note = file.Note ?? "",
            ServiceCount = file.Services?.Count ?? 0,
        };
    }

    private static string Serialize(ServiceSnapshotFile file)
    {
        using var ms = new MemoryStream();
        var settings = new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true,
        };
        new DataContractJsonSerializer(typeof(ServiceSnapshotFile), settings).WriteObject(ms, file);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static ServiceSnapshotFile? Deserialize(string json)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new DataContractJsonSerializer(typeof(ServiceSnapshotFile)).ReadObject(ms) as ServiceSnapshotFile;
    }
}
