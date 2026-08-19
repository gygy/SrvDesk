using System.Diagnostics;
using System.Text;
using Microsoft.Win32;

namespace WinOpt;

internal static class Optimizer
{
    internal const string IeEscAdmin = "{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string IeEscUser = "{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string ClsidMyComputer = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
    internal const string AzureArcCommand = @"%windir%\AzureArcSetup\Systray\AzureArcSysTray.exe";
    internal const string PowerPlanHighPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    internal const string PowerPlanBalanced = "381b4222-f694-41f0-9685-ff5bb260df2e";

    internal sealed class State
    {
        public bool CpuProgramPriority;
        public bool Dep;
        public bool DisableUac;
        public bool DisableIeEsc;
        public bool HighPerfPower;
        public bool DisableTelemetry;
        public bool NoUpdateReboot;
        public bool DisableDeliveryOpt;

        public bool ShowThisPcIcon;
        public bool SmallTaskbar;
        public bool ConfirmDelete;
        public bool EnableAudio;
        public bool ShowFileExtensions;
        public bool EnableThemes;
        public bool EnableSearch;

        public bool EnableRdp;
        public bool EnableNetworkDiscovery;

        public bool SkipServerManager;
        public bool DisableAzureArc;
        public bool DisableErrorReport;

        public bool DisablePasswordComplexity;
        public bool ShutdownWithoutLogon;
        public bool DisableShutdownReason;
        public bool DisableCad;
    }

    public static bool IsWindowsServer()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key is null) return false;
        var type = key.GetValue("InstallationType") as string ?? "";
        var name = key.GetValue("ProductName") as string ?? "";
        return type.Equals("Server", StringComparison.OrdinalIgnoreCase)
            || name.IndexOf("Windows Server", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static State Read()
    {
        return new State
        {
            CpuProgramPriority = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38),
            Dep = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DataExecutionPrevention_S4UEnable", 1),
            DisableUac = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 0),
            DisableIeEsc = DwordEquals(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscAdmin}", "IsInstalled", 0),
            HighPerfPower = IsActivePowerPlan(PowerPlanHighPerf),
            DisableTelemetry = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0),
            NoUpdateReboot = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1),
            DisableDeliveryOpt = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 100),

            ShowThisPcIcon = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", ClsidMyComputer, 0),
            SmallTaskbar = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", 1),
            ConfirmDelete = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete", 1),
            EnableAudio = ServiceStartEquals("AudioSrv", 2),
            ShowFileExtensions = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0),
            EnableThemes = ServiceStartEquals("Themes", 2),
            EnableSearch = ServiceStartEquals("WSearch", 2),

            EnableRdp = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0),
            EnableNetworkDiscovery = ServiceStartEquals("fdPHost", 2) && ServiceStartEquals("FDResPub", 2),

            SkipServerManager = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", 1),
            DisableAzureArc = GetValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup") is null,
            DisableErrorReport = ServiceStartEquals("WerSvc", 4),

            DisablePasswordComplexity = ReadPasswordComplexityDisabled(),
            ShutdownWithoutLogon = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", 1),
            DisableShutdownReason = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonOn", 0),
            DisableCad = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", 1),
        };
    }

    public static List<string> Apply(State s)
    {
        var errors = new List<string>();
        Try(errors, "CPU资源分配", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", s.CpuProgramPriority ? 38 : 2));
        Try(errors, "DEP", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DataExecutionPrevention_S4UEnable", s.Dep ? 1 : 0));
        Try(errors, "UAC", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", s.DisableUac ? 0 : 1));
        Try(errors, "IE增强安全", () =>
        {
            SetDword(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscAdmin}", "IsInstalled", s.DisableIeEsc ? 0 : 1);
            SetDword(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscUser}", "IsInstalled", s.DisableIeEsc ? 0 : 1);
        });
        Try(errors, "电源计划", () => SetPowerPlan(s.HighPerfPower ? PowerPlanHighPerf : PowerPlanBalanced));
        Try(errors, "遥测", () => SetTelemetry(!s.DisableTelemetry));
        Try(errors, "更新重启", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", s.NoUpdateReboot ? 1 : 0));
        Try(errors, "传递优化", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", s.DisableDeliveryOpt ? 100 : 1));

        Try(errors, "桌面此电脑", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", ClsidMyComputer, s.ShowThisPcIcon ? 0 : 1));
        Try(errors, "小按钮任务栏", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", s.SmallTaskbar ? 1 : 0));
        Try(errors, "删除确认", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete", s.ConfirmDelete ? 1 : 0));
        Try(errors, "音频服务", () => SetAudio(s.EnableAudio));
        Try(errors, "文件扩展名", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", s.ShowFileExtensions ? 0 : 1));
        Try(errors, "主题服务", () => SetService("Themes", s.EnableThemes, disableWhenOff: false));
        Try(errors, "Windows搜索", () => SetService("WSearch", s.EnableSearch, disableWhenOff: true));

        Try(errors, "远程桌面", () => SetRdp(s.EnableRdp));
        Try(errors, "网络发现", () => SetNetworkDiscovery(s.EnableNetworkDiscovery));

        Try(errors, "服务管理器", () =>
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", s.SkipServerManager ? 1 : 0);
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotPopWACConsoleAtSMLaunch", s.SkipServerManager ? 1 : 0);
        });
        Try(errors, "Azure Arc", () =>
        {
            if (s.DisableAzureArc)
                DeleteValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup");
            else
                SetString(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup", AzureArcCommand);
        });
        Try(errors, "错误报告", () => SetService("WerSvc", !s.DisableErrorReport, disableWhenOff: true));

        Try(errors, "密码复杂性", () => SetPasswordComplexity(s.DisablePasswordComplexity));
        Try(errors, "未登录关机", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", s.ShutdownWithoutLogon ? 1 : 0));
        Try(errors, "关机事件跟踪", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonOn", s.DisableShutdownReason ? 0 : 1));
        Try(errors, "Ctrl+Alt+Del", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", s.DisableCad ? 1 : 0));
        return errors;
    }

    private static void SetRdp(bool enable)
    {
        SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", enable ? 0 : 1);
        Run("netsh.exe", enable
            ? "advfirewall firewall set rule group=\"remote desktop\" new enable=Yes"
            : "advfirewall firewall set rule group=\"remote desktop\" new enable=No");
    }

    private static void SetNetworkDiscovery(bool enable)
    {
        SetService("fdPHost", enable, disableWhenOff: false);
        SetService("FDResPub", enable, disableWhenOff: false);
        if (enable)
        {
            Run("netsh.exe", "advfirewall firewall set rule group=\"network discovery\" new enable=Yes");
            Run("netsh.exe", "advfirewall firewall set rule group=\"file and printer sharing\" new enable=Yes");
        }
        else
        {
            Run("netsh.exe", "advfirewall firewall set rule group=\"network discovery\" new enable=No");
            Run("netsh.exe", "advfirewall firewall set rule group=\"file and printer sharing\" new enable=No");
        }
    }

    private static void SetTelemetry(bool enable)
    {
        if (enable)
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1);
            SetService("DiagTrack", true, disableWhenOff: false);
        }
        else
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            SetService("DiagTrack", false, disableWhenOff: true);
        }
    }

    private static void SetPowerPlan(string guid) => Run("powercfg.exe", "/setactive " + guid);

    private static bool IsActivePowerPlan(string guid)
    {
        try
        {
            var output = RunCapture("powercfg.exe", "/getactivescheme");
            return output.IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool DwordEquals(Hive hive, string key, string name, int expected) =>
        GetDword(hive, key, name) == expected;

    private static bool ServiceStartEquals(string service, int expected) =>
        GetDword(Hive.HkLm, $@"SYSTEM\CurrentControlSet\Services\{service}", "Start") == expected;

    private enum Hive { HkLm, HkCu }

    private static void Try(List<string> errors, string name, Action action)
    {
        try { action(); }
        catch (Exception ex) { errors.Add($"{name}：{ex.Message}"); }
    }

    private static RegistryKey OpenBase(Hive hive) =>
        RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);

    private static int? GetDword(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) switch
        {
            int i => i,
            byte b => b,
            _ => null,
        };
    }

    private static object? GetValue(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name);
    }

    private static void SetDword(Hive hive, string key, string name, int value)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(Hive hive, string key, string name, string value)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void SetService(string name, bool enable, bool disableWhenOff)
    {
        if (enable)
        {
            Run("sc.exe", $"config {name} start= auto");
            Run("sc.exe", $"start {name}");
        }
        else
        {
            Run("sc.exe", $"stop {name}");
            Run("sc.exe", $"config {name} start= {(disableWhenOff ? "disabled" : "demand")}");
        }
    }

    private static void SetAudio(bool enable)
    {
        if (enable)
        {
            Run("sc.exe", "config AudioSrv start= auto");
            Run("sc.exe", "config AudioEndpointBuilder start= auto");
            Run("sc.exe", "start AudioSrv");
        }
        else
        {
            Run("sc.exe", "stop AudioSrv");
            Run("sc.exe", "stop AudioEndpointBuilder");
            Run("sc.exe", "config AudioSrv start= disabled");
            Run("sc.exe", "config AudioEndpointBuilder start= disabled");
        }
    }

    private static bool ReadPasswordComplexityDisabled()
    {
        var cfg = Path.Combine(Path.GetTempPath(), "WinOpt-secpol.inf");
        try
        {
            Run("secedit.exe", $"/export /cfg \"{cfg}\"");
            if (!File.Exists(cfg)) return false;
            return File.ReadAllLines(cfg).Any(line => line.Contains("PasswordComplexity = 0"));
        }
        catch
        {
            return false;
        }
        finally
        {
            TryDelete(cfg);
        }
    }

    private static void SetPasswordComplexity(bool disable)
    {
        var cfg = Path.Combine(Path.GetTempPath(), "WinOpt-secpol.inf");
        Run("secedit.exe", $"/export /cfg \"{cfg}\"");
        if (!File.Exists(cfg)) throw new InvalidOperationException("secedit 导出失败");
        var text = File.ReadAllText(cfg);
        text = disable
            ? text.Replace("PasswordComplexity = 1", "PasswordComplexity = 0")
            : text.Replace("PasswordComplexity = 0", "PasswordComplexity = 1");
        File.WriteAllText(cfg, text);
        Run("secedit.exe", $"/configure /db C:\\Windows\\security\\local.sdb /cfg \"{cfg}\" /areas SECURITYPOLICY");
        TryDelete(cfg);
    }

    private static void Run(string fileName, string arguments)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + fileName);
        p.WaitForExit(60_000);
    }

    private static string RunCapture(string fileName, string arguments)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.Default,
        }) ?? throw new InvalidOperationException("无法启动 " + fileName);
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(60_000);
        return output;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* ignore */ }
    }
}
