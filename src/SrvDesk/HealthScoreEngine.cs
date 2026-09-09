using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceProcess;
using System.Text;
using Microsoft.Win32;

namespace SrvDesk;

internal sealed class HealthIssue
{
    public string Area { get; set; } = "";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public OptimizeRisk Risk { get; set; }
    public string Hint { get; set; } = "";
}

internal sealed class HealthScoreReport
{
    public int Total { get; set; }
    public int Performance { get; set; }
    public int Stability { get; set; }
    public int Security { get; set; }
    public int Network { get; set; }
    public int Storage { get; set; }
    public int System { get; set; }
    public List<HealthIssue> Issues { get; } = new();
    public string ProfileSummary { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

internal static class HealthScoreEngine
{
    public static HealthScoreReport Evaluate()
    {
        var r = new HealthScoreReport
        {
            ProfileSummary = ServerProfile.Summary(),
            Performance = 90,
            Stability = 92,
            Security = 88,
            Network = 90,
            Storage = 90,
            System = 90,
        };

        try { EvalStorage(r); } catch { /* ignore */ }
        try { EvalSecurity(r); } catch { /* ignore */ }
        try { EvalNetwork(r); } catch { /* ignore */ }
        try { EvalPerformance(r); } catch { /* ignore */ }
        try { EvalSystem(r); } catch { /* ignore */ }
        try { EvalStability(r); } catch { /* ignore */ }

        r.Performance = Clamp(r.Performance);
        r.Stability = Clamp(r.Stability);
        r.Security = Clamp(r.Security);
        r.Network = Clamp(r.Network);
        r.Storage = Clamp(r.Storage);
        r.System = Clamp(r.System);
        r.Total = Clamp((r.Performance + r.Stability + r.Security + r.Network + r.Storage + r.System) / 6);
        return r;
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(100, v));

    private static void Add(HealthScoreReport r, string area, string title, string detail, OptimizeRisk risk, string hint, ref int score, int penalty)
    {
        score -= penalty;
        r.Issues.Add(new HealthIssue
        {
            Area = area,
            Title = title,
            Detail = detail,
            Risk = risk,
            Hint = hint,
        });
    }

    private static void EvalStorage(HealthScoreReport r)
    {
        var score = r.Storage;
        foreach (var d in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
        {
            if (d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
            {
                var freePct = d.TotalSize > 0 ? (double)d.AvailableFreeSpace / d.TotalSize * 100 : 100;
                if (freePct < 10)
                    Add(r, AppLang.L("存储", "Storage"), AppLang.L("系统盘空间不足", "System disk low"),
                        $"{d.Name} {freePct:0.0}% " + AppLang.L("可用", "free"),
                        OptimizeRisk.High,
                        AppLang.L("建议至少保留 15～20% 空间。", "Keep at least 15–20% free."),
                        ref score, 25);
                else if (freePct < 20)
                    Add(r, AppLang.L("存储", "Storage"), AppLang.L("系统盘空间偏低", "System disk getting full"),
                        $"{d.Name} {freePct:0.0}% " + AppLang.L("可用", "free"),
                        OptimizeRisk.Medium,
                        AppLang.L("建议清理更新缓存/临时文件。", "Clean update cache / temp files."),
                        ref score, 10);
            }
        }
        r.Storage = score;
    }

    private static void EvalSecurity(HealthScoreReport r)
    {
        var score = r.Security;
        if (IsSmb1Enabled())
            Add(r, AppLang.L("安全", "Security"), "SMBv1",
                AppLang.L("检测到 SMBv1 可能仍启用。", "SMBv1 may still be enabled."),
                OptimizeRisk.High,
                AppLang.L("立即禁用 SMBv1。", "Disable SMBv1 now."),
                ref score, 20);

        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp");
            var nla = k?.GetValue("UserAuthentication");
            if (nla is int i && i == 0)
                Add(r, "RDP", AppLang.L("NLA 未启用", "NLA off"),
                    AppLang.L("远程桌面未要求网络级身份验证。", "RDP does not require NLA."),
                    OptimizeRisk.Medium,
                    AppLang.L("启用 NLA 降低暴力破解风险。", "Enable NLA."),
                    ref score, 12);
        }
        catch { /* ignore */ }

        foreach (var p in PortExposureHelper.Scan().Where(x => x.IsSensitive))
        {
            if (p.ListensOnAllInterfaces)
                Add(r, AppLang.L("安全", "Security"),
                    AppLang.Lf("端口 {0} 全网卡监听", "Port {0} on all interfaces", p.Port),
                    p.ProcessName + " · " + p.LocalAddress,
                    p.Port is 445 or 3389 ? OptimizeRisk.High : OptimizeRisk.Medium,
                    AppLang.L("确认是否需对公网/全部网卡开放。", "Confirm if Internet/all-NIC exposure is intended."),
                    ref score, p.Port == 445 ? 18 : 10);
        }

        r.Security = score;
    }

    private static void EvalNetwork(HealthScoreReport r)
    {
        var score = r.Network;
        var up = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .ToList();
        if (up.Count == 0)
            Add(r, AppLang.L("网络", "Network"), AppLang.L("无活动网卡", "No active NIC"),
                "", OptimizeRisk.High, AppLang.L("检查网线/驱动。", "Check cable/drivers."), ref score, 30);
        else
        {
            var speed = up.Max(n => n.Speed);
            if (speed > 0 && speed < 1_000_000_000)
                Add(r, AppLang.L("网络", "Network"), AppLang.L("链路低于 1Gbps", "Link below 1Gbps"),
                    (speed / 1_000_000) + " Mbps", OptimizeRisk.Low,
                    AppLang.L("NAS/大文件场景可检查协商速率。", "For NAS/large files check link negotiation."),
                    ref score, 5);
        }
        r.Network = score;
    }

    private static void EvalPerformance(HealthScoreReport r)
    {
        var score = r.Performance;
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
            // 电源计划：高性能略加分，不强制
            var plan = PowerPlanName();
            if (plan.IndexOf("均衡", StringComparison.OrdinalIgnoreCase) >= 0 ||
                plan.IndexOf("Balanced", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (ServerProfile.Has(ServerRoleFlags.Docker) || ServerProfile.Has(ServerRoleFlags.HyperV) ||
                    ServerProfile.Has(ServerRoleFlags.Database))
                    Add(r, AppLang.L("性能", "Performance"), AppLang.L("电源计划为均衡", "Balanced power plan"),
                        plan, OptimizeRisk.Low,
                        AppLang.L("虚拟化/数据库场景可考虑高性能。", "Consider High Performance for virt/DB."),
                        ref score, 4);
            }
        }
        catch { /* ignore */ }
        r.Performance = score;
    }

    private static void EvalSystem(HealthScoreReport r)
    {
        var score = r.System;
        if (!ServerProfile.Load().ProfileConfigured)
            Add(r, AppLang.L("系统", "System"), AppLang.L("未配置服务器用途", "Server profile not set"),
                AppLang.L("用途画像可避免误关服务。", "Profile avoids disabling needed services."),
                OptimizeRisk.Medium,
                AppLang.L("打开「服务器用途」勾选角色。", "Open Server profile and select roles."),
                ref score, 8);
        r.System = score;
    }

    private static void EvalStability(HealthScoreReport r)
    {
        var score = r.Stability;
        foreach (var name in new[] { "RpcSs", "EventLog", "LanmanServer", "BFE" })
        {
            try
            {
                using var sc = new ServiceController(name);
                if (sc.Status != ServiceControllerStatus.Running)
                    Add(r, AppLang.L("稳定", "Stability"),
                        AppLang.Lf("关键服务未运行：{0}", "Critical service not running: {0}", name),
                        sc.DisplayName, OptimizeRisk.High,
                        AppLang.L("检查服务管理器。", "Check Services MMC."),
                        ref score, 15);
            }
            catch { /* missing ok on some SKUs */ }
        }
        r.Stability = score;
    }

    private static bool IsSmb1Enabled()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters");
            var v = k?.GetValue("SMB1");
            if (v is int i) return i != 0;
            // 缺省旧系统可能仍开；现代默认关。再看功能状态较重，这里用服务依赖粗检
            return ServiceController.GetServices().Any(s =>
            {
                using (s)
                    return s.ServiceName.Equals("mrxsmb10", StringComparison.OrdinalIgnoreCase);
            });
        }
        catch { return false; }
    }

    private static string PowerPlanName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\cimv2\power", "SELECT ElementName FROM Win32_PowerPlan WHERE IsActive=True");
            foreach (ManagementObject o in searcher.Get())
            {
                using (o)
                    return Convert.ToString(o["ElementName"]) ?? "";
            }
        }
        catch { /* ignore */ }
        return "";
    }
}

internal sealed class PortListenInfo
{
    public int Port { get; set; }
    public string Protocol { get; set; } = "TCP";
    public string LocalAddress { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
    public bool ListensOnAllInterfaces =>
        LocalAddress == "0.0.0.0" || LocalAddress == "::" || LocalAddress == "[::]";
    public bool IsSensitive => Port is 22 or 23 or 80 or 443 or 445 or 3389 or 5985 or 5986 or 1433 or 3306 or 5432;
}

internal static class PortExposureHelper
{
    public static IReadOnlyList<PortListenInfo> Scan()
    {
        var list = new List<PortListenInfo>();
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            foreach (var ep in props.GetActiveTcpListeners())
            {
                list.Add(new PortListenInfo
                {
                    Port = ep.Port,
                    Protocol = "TCP",
                    LocalAddress = ep.Address.ToString(),
                    ProcessName = "—",
                    Pid = 0,
                });
            }
        }
        catch { /* ignore */ }

        // 补充 PID（netstat）
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netstat.exe",
                Arguments = "-ano -p tcp",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return list.OrderBy(x => x.Port).ToList();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(8000);
            var map = new Dictionary<(string addr, int port), int>();
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5 || !parts[0].StartsWith("TCP", StringComparison.OrdinalIgnoreCase)) continue;
                if (!parts[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase)) continue;
                var local = parts[1];
                var colon = local.LastIndexOf(':');
                if (colon < 0) continue;
                if (!int.TryParse(local.Substring(colon + 1), out var port)) continue;
                if (!int.TryParse(parts[4], out var pid)) continue;
                var addr = local.Substring(0, colon).Trim('[', ']');
                map[(addr, port)] = pid;
            }

            foreach (var item in list)
            {
                if (map.TryGetValue((item.LocalAddress, item.Port), out var pid) ||
                    map.TryGetValue(("0.0.0.0", item.Port), out pid) ||
                    map.TryGetValue(("::", item.Port), out pid))
                {
                    item.Pid = pid;
                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        item.ProcessName = proc.ProcessName;
                    }
                    catch { item.ProcessName = "pid=" + pid; }
                }
            }
        }
        catch { /* ignore */ }

        return list
            .GroupBy(x => (x.Port, x.LocalAddress))
            .Select(g => g.First())
            .OrderByDescending(x => x.IsSensitive)
            .ThenBy(x => x.Port)
            .ToList();
    }
}

internal static class DiskMemoryNetworkInsights
{
    public static string BuildReportText()
    {
        var sb = new StringBuilder();
        sb.AppendLine(AppLang.L("【存储】", "[Storage]"));
        foreach (var d in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
        {
            var freePct = d.TotalSize > 0 ? (double)d.AvailableFreeSpace / d.TotalSize * 100 : 0;
            sb.AppendLine($"  {d.Name} {d.DriveFormat}  " +
                          AppLang.Lf("可用 {0:0.0}% / {1:0.0} GB", "free {0:0.0}% / {1:0.0} GB",
                              freePct, d.AvailableFreeSpace / 1024.0 / 1024 / 1024));
        }
        sb.AppendLine(AppLang.L("提示：缓存不是内存泄漏；SSD 请保持 TRIM，勿对 SSD 做传统碎片整理。",
            "Note: cache is not a leak; keep TRIM on SSD; avoid classic defrag on SSD."));
        sb.AppendLine();

        sb.AppendLine(AppLang.L("【内存】", "[Memory]"));
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject o in searcher.Get())
            {
                using (o)
                {
                    var totalKb = Convert.ToDouble(o["TotalVisibleMemorySize"]);
                    var freeKb = Convert.ToDouble(o["FreePhysicalMemory"]);
                    var used = (totalKb - freeKb) / 1024 / 1024;
                    var total = totalKb / 1024 / 1024;
                    sb.AppendLine(AppLang.Lf("  物理内存约 {0:0.0} GB，已用约 {1:0.0} GB",
                        "  RAM ~{0:0.0} GB, used ~{1:0.0} GB", total, used));
                }
            }
        }
        catch
        {
            sb.AppendLine("  —");
        }
        sb.AppendLine(AppLang.L("  不做「一键释放内存」（清空缓存往往更慢）。",
            "  No “one-click free RAM” (purging cache often hurts)."));
        sb.AppendLine();

        sb.AppendLine(AppLang.L("【网络】", "[Network]"));
        foreach (var n in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(x => x.OperationalStatus == OperationalStatus.Up &&
                                 x.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var mbps = n.Speed > 0 ? n.Speed / 1_000_000 : 0;
            sb.AppendLine($"  {n.Name}: {mbps} Mbps · {n.NetworkInterfaceType}");
        }
        return sb.ToString();
    }
}

internal static class HealthInspectionService
{
    private static System.Windows.Forms.Timer? _timer;

    public static void StartIfEnabled(Form? owner)
    {
        Stop();
        if (!ServerProfile.Load().HealthInspectionEnabled) return;
        _timer = new System.Windows.Forms.Timer { Interval = 6 * 60 * 60 * 1000 }; // 6h
        _timer.Tick += (_, _) => RunOnce(silent: true);
        _timer.Start();
        // 启动后稍后跑一次
        var once = new System.Windows.Forms.Timer { Interval = 45_000 };
        once.Tick += (_, _) =>
        {
            once.Stop();
            once.Dispose();
            RunOnce(silent: true);
        };
        once.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public static string RunOnce(bool silent)
    {
        var report = HealthScoreEngine.Evaluate();
        var path = AppPaths.Combine("health-inspection-latest.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"SrvDesk Health Inspection {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Score {report.Total}/100  perf={report.Performance} stab={report.Stability} sec={report.Security} net={report.Network} stor={report.Storage} sys={report.System}");
        sb.AppendLine("Profile: " + report.ProfileSummary);
        sb.AppendLine("--- Issues ---");
        foreach (var i in report.Issues)
            sb.AppendLine($"[{OptimizationLevelUi.RiskText(i.Risk)}] {i.Area} · {i.Title} · {i.Detail}");
        sb.AppendLine("--- Insights ---");
        sb.AppendLine(DiskMemoryNetworkInsights.BuildReportText());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

        var data = ServerProfile.Load();
        data.LastInspectionUtc = DateTime.UtcNow.ToString("o");
        ServerProfile.Save(data);
        ApplyLog.Write(AppLang.Lf("健康巡检完成：{0}/100，问题 {1} 条 → {2}",
            "Health inspection: {0}/100, {1} issue(s) → {2}", report.Total, report.Issues.Count, path));
        return path;
    }
}
