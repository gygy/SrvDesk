using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using Microsoft.Win32;

namespace SrvDesk;

internal sealed class SecurityCenterStatus
{
    public string WscText { get; set; } = "未知";
    public string DefenderText { get; set; } = "未知";
    public string PolicyText { get; set; } = "未知";
    public string TamperText { get; set; } = "未知";
    public bool LooksDisabled { get; set; }
    public bool TamperProtectionOn { get; set; }
    public string Summary { get; set; } = "";
}

/// <summary>Windows 安全中心 / Defender 相关启停（需管理员）。</summary>
internal static class SecurityCenterHelper
{
    private const string DefenderPolicy = @"SOFTWARE\Policies\Microsoft\Windows Defender";
    private const string DefenderFeaturesPolicy = @"SOFTWARE\Policies\Microsoft\Windows Defender\Features";
    private const string DefenderFeatures = @"SOFTWARE\Microsoft\Windows Defender\Features";
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
        var tamperOn = IsTamperProtectionOn();
        var servicesOff = IsServiceDisabledOrMissing("wscsvc") && IsServiceDisabledOrMissing("WinDefend");
        var looksDisabled = policyOff || servicesOff;

        return new SecurityCenterStatus
        {
            WscText = wsc,
            DefenderText = defender,
            PolicyText = policyOff ? "已通过策略禁用" : "未禁用（策略默认）",
            TamperText = DescribeTamperProtection(tamperOn),
            TamperProtectionOn = tamperOn,
            LooksDisabled = looksDisabled,
            Summary = looksDisabled
                ? (tamperOn
                    ? "当前偏向已禁用（篡改防护仍开，可能拦停服务；可再点禁用或重启）"
                    : "当前偏向已禁用")
                : (tamperOn
                    ? "当前偏向已启用（含篡改防护）"
                    : "当前偏向已启用"),
        };
    }

    public static void Disable()
    {
        ApplyLog.Write("禁用 Windows 安全中心 / Defender");

        // 先关篡改防护，否则后续停 WinDefend / 写策略常被拒绝
        TryDisableTamperProtection();
        Thread.Sleep(800);

        SetDword(RegistryHive.LocalMachine, DefenderPolicy, "DisableAntiSpyware", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableRealtimeMonitoring", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableBehaviorMonitoring", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableOnAccessProtection", 1);
        SetDword(RegistryHive.LocalMachine, RealtimePolicy, "DisableScanOnRealtimeEnable", 1);

        foreach (var svc in RelatedServices)
            TrySetService(svc, enable: false);

        TryDisableSecurityHealthRun();
        ApplyLog.Write("已写入禁用安全中心相关策略与服务；篡改防护="
            + (IsTamperProtectionOn() ? "仍开启" : "已关闭"));
    }

    public static void Enable()
    {
        ApplyLog.Write("启用 Windows 安全中心 / Defender");

        DeleteValue(RegistryHive.LocalMachine, DefenderPolicy, "DisableAntiSpyware");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableRealtimeMonitoring");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableBehaviorMonitoring");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableOnAccessProtection");
        DeleteValue(RegistryHive.LocalMachine, RealtimePolicy, "DisableScanOnRealtimeEnable");
        DeleteValue(RegistryHive.LocalMachine, DefenderFeaturesPolicy, "TamperProtection");

        // 安全中心 / Health：自动；Defender：自动；网络检测：手动
        TrySetService("wscsvc", enable: true, autoStart: true);
        TrySetService("SecurityHealthService", enable: true, autoStart: true);
        TrySetService("WinDefend", enable: true, autoStart: true);
        TrySetService("WdNisSvc", enable: true, autoStart: false);
        TrySetService("Sense", enable: true, autoStart: false);

        // 不强制重开篡改防护，避免再次锁死；需要时用户可在 Windows 安全中心打开
        ApplyLog.Write("已恢复安全中心相关策略与服务");
    }

    /// <summary>
    /// 尽量关闭篡改防护：组策略 + PowerShell + SYSTEM 写注册表（后者在部分版本会被拒）。
    /// </summary>
    public static bool TryDisableTamperProtection()
    {
        ApplyLog.Write("尝试关闭篡改防护 (Tamper Protection)");

        // 1) 组策略：可写，重启/刷新后通常生效
        try
        {
            SetDword(RegistryHive.LocalMachine, DefenderFeaturesPolicy, "TamperProtection", 0);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("篡改防护策略写入失败：" + ex.Message);
        }

        // 2) 官方偏好接口（部分 SKU 返回 E_NOTIMPL）
        TrySetMpPreferenceDisableTamper(true);

        // 3) SYSTEM 身份写 Features\TamperProtection（旧版可用；新版常拒绝）
        TryWriteTamperProtectionAsSystem(0);

        // 4) 当前进程再试一次（若已被允许）
        try
        {
            SetDword(RegistryHive.LocalMachine, DefenderFeatures, "TamperProtection", 0);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("直接写 Features\\TamperProtection：" + ex.Message);
        }

        Thread.Sleep(400);
        var stillOn = IsTamperProtectionOn();
        ApplyLog.Write(stillOn
            ? "篡改防护仍显示开启（已写策略；可重启后再禁用一次）"
            : "篡改防护已关闭");
        return !stillOn;
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

    private static string DescribeTamperProtection(bool on)
    {
        var policy = ReadDword(DefenderFeaturesPolicy, "TamperProtection");
        if (on)
            return policy is 0
                ? "开启（策略已关，重启后可能生效）"
                : "开启";
        return "已关闭";
    }

    private static bool IsPolicyDisabled() =>
        ReadDword(DefenderPolicy, "DisableAntiSpyware") == 1;

    /// <summary>Features\\TamperProtection：0/4=关，1/5=开（常见取值）。</summary>
    private static bool IsTamperProtectionOn()
    {
        var v = ReadDword(DefenderFeatures, "TamperProtection");
        if (v is null) return false;
        return v is not 0 and not 4;
    }

    private static int? ReadDword(string key, string name)
    {
        try
        {
            using var k = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(key);
            return k?.GetValue(name) is int i ? i : null;
        }
        catch
        {
            return null;
        }
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
            return true;
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
            _ = sc.DisplayName;
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

    private static void TrySetMpPreferenceDisableTamper(bool disable)
    {
        try
        {
            var flag = disable ? "$true" : "$false";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"try { Set-MpPreference -DisableTamperProtection "
                    + flag + " -ErrorAction Stop; exit 0 } catch { exit 1 }\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return;
            _ = p.StandardOutput.ReadToEnd();
            _ = p.StandardError.ReadToEnd();
            p.WaitForExit(45_000);
            ApplyLog.Write("Set-MpPreference -DisableTamperProtection 退出码 " + p.ExitCode);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("Set-MpPreference：" + ex.Message);
        }
    }

    private static void TryWriteTamperProtectionAsSystem(int value)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "SrvDeskTp");
            Directory.CreateDirectory(dir);
            var bat = Path.Combine(dir, "tp.cmd");
            var log = Path.Combine(dir, "tp.log");
            File.WriteAllText(bat,
                "@echo off\r\n"
                + "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows Defender\\Features\" /v TamperProtection /t REG_DWORD /d "
                + value + " /f > \"" + log + "\" 2>&1\r\n"
                + "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Features\" /v TamperProtection /t REG_DWORD /d "
                + value + " /f >> \"" + log + "\" 2>&1\r\n",
                Encoding.ASCII);

            const string taskName = "SrvDeskDisableTamperProtection";
            RunProcess("schtasks.exe", "/Delete /TN \"" + taskName + "\" /F", 15_000);
            var create = RunProcess("schtasks.exe",
                "/Create /TN \"" + taskName + "\" /RU SYSTEM /RL HIGHEST /SC ONCE /ST 23:59 /TR \"cmd /c \\\""
                + bat + "\\\"\" /F",
                20_000);
            ApplyLog.Write("计划任务创建篡改防护：" + create);
            RunProcess("schtasks.exe", "/Run /TN \"" + taskName + "\"", 15_000);
            Thread.Sleep(2_500);
            RunProcess("schtasks.exe", "/Delete /TN \"" + taskName + "\" /F", 15_000);
            if (File.Exists(log))
                ApplyLog.Write("SYSTEM 写篡改防护：" + File.ReadAllText(log).Trim());
        }
        catch (Exception ex)
        {
            ApplyLog.Write("SYSTEM 写篡改防护失败：" + ex.Message);
        }
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

    private static string RunProcess(string file, string args, int timeoutMs)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (p is null) return "start-failed";
            var o = p.StandardOutput.ReadToEnd();
            var e = p.StandardError.ReadToEnd();
            p.WaitForExit(timeoutMs);
            return (o + e).Trim();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
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
    }
}
