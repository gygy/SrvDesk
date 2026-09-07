using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace SrvDesk;

internal sealed class SecurityCenterStatus
{
    public string WscText { get; set; } = "未知";
    public string DefenderText { get; set; } = "未知";
    public string PolicyText { get; set; } = "未知";
    public bool LooksDisabled { get; set; }
    public string Summary { get; set; } = "";
}

/// <summary>Windows 安全中心 / Defender 相关启停（需管理员）。</summary>
internal static class SecurityCenterHelper
{
    private const string DefenderPolicy = @"SOFTWARE\Policies\Microsoft\Windows Defender";
    private const string RealtimePolicy = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection";

    private static readonly string[] RelatedServices =
    [
        "wscsvc",                 // Security Center
        "SecurityHealthService",  // Windows Security Health
        "WinDefend",              // Microsoft Defender Antivirus
        "WdNisSvc",               // Defender Network Inspection
        "Sense",                  // Defender ATP（可能不存在）
    ];

    public static SecurityCenterStatus Query()
    {
        var wsc = DescribeService("wscsvc");
        var defender = DescribeService("WinDefend");
        var policyOff = IsPolicyDisabled();
        var servicesOff = IsServiceDisabledOrMissing("wscsvc") && IsServiceDisabledOrMissing("WinDefend");
        var looksDisabled = policyOff || servicesOff;

        return new SecurityCenterStatus
        {
            WscText = wsc,
            DefenderText = defender,
            PolicyText = policyOff ? "已通过策略禁用" : "未禁用（策略默认）",
            LooksDisabled = looksDisabled,
            Summary = looksDisabled ? "当前偏向已禁用" : "当前偏向已启用",
        };
    }

    public static void Disable()
    {
        ApplyLog.Write("禁用 Windows 安全中心 / Defender");

        SetDword(RegistryHive.LocalMachine, DefenderPolicy, "DisableAntiSpyware", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableRealtimeMonitoring", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableBehaviorMonitoring", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableOnAccessProtection", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableScanOnRealtimeEnable", 1);

        foreach (var svc in RelatedServices)
            TrySetService(svc, enable: false);

        TryDisableSecurityHealthRun();
        ApplyLog.Write("已写入禁用安全中心相关策略与服务");
    }

    public static void Enable()
    {
        ApplyLog.Write("启用 Windows 安全中心 / Defender");

        DeleteValue(RegistryHive.LocalMachine, DefenderPolicy, "DisableAntiSpyware");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableRealtimeMonitoring");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableBehaviorMonitoring");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableOnAccessProtection");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableScanOnRealtimeEnable");

        // 安全中心 / Health：自动；Defender：自动；网络检测：手动
        TrySetService("wscsvc", enable: true, autoStart: true);
        TrySetService("SecurityHealthService", enable: true, autoStart: true);
        TrySetService("WinDefend", enable: true, autoStart: true);
        TrySetService("WdNisSvc", enable: true, autoStart: false);
        TrySetService("Sense", enable: true, autoStart: false);

        ApplyLog.Write("已恢复安全中心相关策略与服务");
    }

    public static void OpenWindowsSecurity()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "windowsdefender:",
                UseShellExecute = true,
            });
        }
        catch
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "control.exe"),
                Arguments = "/name Microsoft.WindowsDefender",
                UseShellExecute = true,
            });
        }
    }

    private static string DescribeService(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            var start = ReadStartType(name);
            var startText = start switch
            {
                2 => "自动",
                3 => "手动",
                4 => "已禁用",
                _ => "启动类型 " + start,
            };
            var state = sc.Status switch
            {
                ServiceControllerStatus.Running => "运行中",
                ServiceControllerStatus.Stopped => "已停止",
                ServiceControllerStatus.StartPending => "正在启动",
                ServiceControllerStatus.StopPending => "正在停止",
                _ => sc.Status.ToString(),
            };
            return $"{state} · {startText}";
        }
        catch
        {
            return "未安装或不存在";
        }
    }

    private static bool IsPolicyDisabled()
    {
        using var k = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey(DefenderPolicy);
        return k?.GetValue("DisableAntiSpyware") is int i && i == 1;
    }

    private static bool IsServiceDisabledOrMissing(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            return ReadStartType(name) == 4;
        }
        catch
        {
            return true; // 不存在视为「不在工作」
        }
    }

    private static int ReadStartType(string name)
    {
        using var k = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{name}");
        return k?.GetValue("Start") is int i ? i : -1;
    }

    private static void TrySetService(string name, bool enable, bool autoStart = true)
    {
        try
        {
            using var sc = new ServiceController(name);
            _ = sc.DisplayName; // 探测是否存在
        }
        catch
        {
            return;
        }

        var startArg = enable ? (autoStart ? "auto" : "demand") : "disabled";
        RunSc($"config {name} start= {startArg}");
        if (enable)
            RunSc($"start {name}");
        else
            RunSc($"stop {name}");
    }

    private static void TryDisableSecurityHealthRun()
    {
        try
        {
            using var run = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (run?.GetValue("SecurityHealth") is not null)
            {
                ApplyLog.RegistryDelete("HKLM",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth",
                    run.GetValue("SecurityHealth"));
                run.DeleteValue("SecurityHealth", throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            ApplyLog.Write("移除 SecurityHealth 启动项：" + ex.Message);
        }
    }

    private static void SetDword(RegistryHive hive, string key, string name, int value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入：" + key);
        var old = k.GetValue(name);
        ApplyLog.RegistryDword("HKLM", key, name, old, value);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void DeleteValue(RegistryHive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var k = baseKey.OpenSubKey(key, writable: true);
        if (k is null) return;
        var old = k.GetValue(name);
        if (old is null) return;
        ApplyLog.RegistryDelete("HKLM", key, name, old);
        k.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void RunSc(string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (p is null) return;
        _ = p.StandardOutput.ReadToEnd();
        _ = p.StandardError.ReadToEnd();
        p.WaitForExit(30_000);
        // 1056 已运行、1062 未启动、1060 不存在：忽略
    }
}
