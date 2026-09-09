using System.Diagnostics;

namespace SrvDesk;

internal enum ScheduledTaskBucket
{
    Core = 0,
    Security = 1,
    Update = 2,
    Maintenance = 3,
    Telemetry = 4,
    ThirdParty = 5,
}

internal sealed class ScheduledTaskAdvice
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public ScheduledTaskBucket Bucket { get; set; }
    public OptimizeRisk Risk { get; set; }
    public string Advice { get; set; } = "";
    public bool CanToggle { get; set; }
}

/// <summary>
/// 计划任务最小集：枚举常见遥测/诊断任务。优先 TaskScheduler 托管库；
/// 若运行时无程序集则回退 schtasks。
/// </summary>
internal static class ScheduledTaskHelper
{
    // 无 NuGet TaskScheduler 时用 COM / schtasks。本仓库可能没有 Microsoft.Win32.TaskScheduler —— 改用 schtasks 解析。

    private static readonly string[] TelemetryHints =
    [
        "Customer Experience Improvement", "Consolidator", "UsbCeip", "DiskDiagnostic",
        "ProgramDataUpdater", "Microsoft Compatibility Appraiser", "Proxy", "QueueReporting",
        "MareBackup", "PcaPatchDbTask", "StartupAppTask", "MapsToastTask", "MapsUpdateTask",
        "FamilySafety", "Windows Error Reporting", "QueueReporting", "XblGameSave",
    ];

    private static readonly string[] SecurityHints =
    [
        "Windows Defender", "Antivirus", "Exploitation", "BitLocker", "WindowsHello",
    ];

    private static readonly string[] UpdateHints =
    [
        "UpdateOrchestrator", "WindowsUpdate", "USO", "WaaSMedic", "Scheduled Start",
    ];

    public static IReadOnlyList<ScheduledTaskAdvice> ListMicrosoftTasks()
    {
        var list = new List<ScheduledTaskAdvice>();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = "/Query /FO LIST /V",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.GetEncoding(0),
            };
            using var p = Process.Start(psi);
            if (p is null) return list;
            var text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(20000);

            string? folder = null, name = null, status = null;
            void Flush()
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                var path = (folder ?? "\\") + (folder is null or "\\" ? "" : "\\") ;
                // schtasks LIST: TaskName often full path
                var full = name!.StartsWith("\\") ? name : "\\" + name;
                if (full.IndexOf("\\Microsoft\\Windows\\", StringComparison.OrdinalIgnoreCase) < 0 &&
                    full.IndexOf("\\Microsoft\\", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    // 仍收录第三方简表
                }
                var bucket = Classify(full);
                var risk = bucket switch
                {
                    ScheduledTaskBucket.Core or ScheduledTaskBucket.Security => OptimizeRisk.Critical,
                    ScheduledTaskBucket.Update => OptimizeRisk.High,
                    ScheduledTaskBucket.Maintenance => OptimizeRisk.Medium,
                    ScheduledTaskBucket.Telemetry => OptimizeRisk.Low,
                    _ => OptimizeRisk.Low,
                };
                list.Add(new ScheduledTaskAdvice
                {
                    Path = full,
                    Name = System.IO.Path.GetFileName(full.Replace('/', '\\')),
                    Enabled = !(status ?? "").Equals("Disabled", StringComparison.OrdinalIgnoreCase) &&
                              !(status ?? "").Contains("已禁用"),
                    Bucket = bucket,
                    Risk = risk,
                    Advice = AdviceText(bucket),
                    CanToggle = risk <= OptimizeRisk.Medium || bucket == ScheduledTaskBucket.Telemetry,
                });
                name = null; status = null;
            }

            foreach (var raw in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (line.StartsWith("TaskName:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("任务名:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("任务名称:", StringComparison.OrdinalIgnoreCase))
                {
                    Flush();
                    name = line.Substring(line.IndexOf(':') + 1).Trim();
                }
                else if (line.StartsWith("Status:", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("状态:", StringComparison.OrdinalIgnoreCase))
                {
                    status = line.Substring(line.IndexOf(':') + 1).Trim();
                }
                else if (line.StartsWith("Folder:", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("文件夹:", StringComparison.OrdinalIgnoreCase))
                {
                    folder = line.Substring(line.IndexOf(':') + 1).Trim();
                }
            }
            Flush();
        }
        catch (Exception ex)
        {
            ApplyLog.Write("计划任务枚举失败：" + ex.Message);
        }

        return list
            .GroupBy(t => t.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t.Bucket)
            .ThenBy(t => t.Path, StringComparer.OrdinalIgnoreCase)
            .Take(400)
            .ToList();
    }

    public static void SetEnabled(string taskPath, bool enabled)
    {
        var arg = enabled
            ? $"/Change /TN \"{taskPath}\" /ENABLE"
            : $"/Change /TN \"{taskPath}\" /DISABLE";
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arg,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("schtasks failed to start");
        p.WaitForExit(15000);
        if (p.ExitCode != 0)
            throw new InvalidOperationException((p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd()).Trim());
        ApplyLog.Write((enabled ? "启用" : "禁用") + "计划任务：" + taskPath);
    }

    private static ScheduledTaskBucket Classify(string path)
    {
        if (ContainsAny(path, SecurityHints)) return ScheduledTaskBucket.Security;
        if (ContainsAny(path, UpdateHints)) return ScheduledTaskBucket.Update;
        if (ContainsAny(path, TelemetryHints) ||
            path.IndexOf("Application Experience", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("Customer Experience", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("Feedback", StringComparison.OrdinalIgnoreCase) >= 0)
            return ScheduledTaskBucket.Telemetry;
        if (path.IndexOf("\\Microsoft\\Windows\\Defrag\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("Maintenance", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("DiskCleanup", StringComparison.OrdinalIgnoreCase) >= 0)
            return ScheduledTaskBucket.Maintenance;
        if (path.IndexOf("\\Microsoft\\Windows\\", StringComparison.OrdinalIgnoreCase) >= 0)
            return ScheduledTaskBucket.Core;
        return ScheduledTaskBucket.ThirdParty;
    }

    private static bool ContainsAny(string path, string[] hints) =>
        hints.Any(h => path.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0);

    private static string AdviceText(ScheduledTaskBucket b) => b switch
    {
        ScheduledTaskBucket.Core => AppLang.L("核心系统任务，不允许关闭", "Core task — do not disable"),
        ScheduledTaskBucket.Security => AppLang.L("安全相关，不允许关闭", "Security — do not disable"),
        ScheduledTaskBucket.Update => AppLang.L("更新相关，请谨慎", "Update-related — be careful"),
        ScheduledTaskBucket.Maintenance => AppLang.L("维护任务，可按场景调整", "Maintenance — optional"),
        ScheduledTaskBucket.Telemetry => AppLang.L("遥测/体验，通常可禁用", "Telemetry — usually safe to disable"),
        _ => AppLang.L("第三方任务，按用途决定", "Third-party — depends on role"),
    };
}

internal static class StartupAdviceHelper
{
    private static readonly string[] LikelyOptional =
    [
        "OneDrive", "Teams", "Skype", "Spotify", "Discord", "Steam", "Epic", "Adobe", "iTunes",
        "CCXProcess", "AdobeGCClient", "Microsoft.Office", "Lync",
    ];

    public static string Tag(StartupEntry e)
    {
        var cmd = (e.Command ?? "") + " " + (e.Name ?? "");
        if (cmd.IndexOf("Windows Security", StringComparison.OrdinalIgnoreCase) >= 0 ||
            cmd.IndexOf("SecurityHealth", StringComparison.OrdinalIgnoreCase) >= 0 ||
            cmd.IndexOf("MsMpEng", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppLang.L("安全·保留", "Security·keep");
        if (cmd.IndexOf("Docker", StringComparison.OrdinalIgnoreCase) >= 0)
            return ServerProfile.Has(ServerRoleFlags.Docker)
                ? AppLang.L("Docker·保留", "Docker·keep")
                : AppLang.L("Docker·按需", "Docker·optional");
        foreach (var k in LikelyOptional)
        {
            if (cmd.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                return AppLang.L("可关", "Optional");
        }
        var path = ExtractPath(e.Command ?? "");
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                var dir = Path.GetDirectoryName(path) ?? "";
                if (dir.IndexOf(@"\Windows\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dir.IndexOf(@"\Microsoft\", StringComparison.OrdinalIgnoreCase) >= 0)
                    return AppLang.L("系统", "System");
            }
            catch { /* ignore */ }
            return AppLang.L("未知", "Unknown");
        }
        return AppLang.L("未知路径", "Bad path");
    }

    private static string ExtractPath(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return "";
        var c = command.Trim();
        if (c.StartsWith("\""))
        {
            var end = c.IndexOf('"', 1);
            if (end > 1) return c.Substring(1, end - 1);
        }
        var sp = c.IndexOf(' ');
        return sp > 0 ? c.Substring(0, sp) : c;
    }
}
