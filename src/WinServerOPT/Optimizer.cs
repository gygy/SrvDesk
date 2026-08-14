using System.Diagnostics;
using Microsoft.Win32;

namespace WinOpt;

internal static class Optimizer
{
    internal const string IeEscAdmin = "{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string IeEscUser = "{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string ClsidMyComputer = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
    internal const string AzureArcCommand = @"%windir%\AzureArcSetup\Systray\AzureArcSysTray.exe";

    internal sealed class State
    {
        public bool CpuProgramPriority;
        public bool Dep;
        public bool DisableUac;
        public bool DisableIeEsc;
        public bool ShowThisPcIcon;
        public bool SmallTaskbar;
        public bool ConfirmDelete;
        public bool EnableAudio;
        public bool SkipServerManager;
        public bool DisableAzureArc;
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
            || name.Contains("Windows Server", StringComparison.OrdinalIgnoreCase);
    }

    public static State Read()
    {
        return new State
        {
            CpuProgramPriority = GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation") == 38,
            Dep = GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DataExecutionPrevention_S4UEnable") == 1,
            DisableUac = GetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA") == 0,
            DisableIeEsc = GetDword(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscAdmin}", "IsInstalled") == 0,
            ShowThisPcIcon = GetDword(Hive.HkCu, $@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", ClsidMyComputer) == 0,
            SmallTaskbar = GetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons") == 1,
            ConfirmDelete = GetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete") == 1,
            EnableAudio = GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\AudioSrv", "Start") == 2,
            SkipServerManager = GetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon") == 1,
            DisableAzureArc = GetValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup") is null,
            DisablePasswordComplexity = ReadPasswordComplexityDisabled(),
            ShutdownWithoutLogon = GetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon") == 1,
            DisableShutdownReason = GetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonOn") == 0,
            DisableCad = GetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD") == 1,
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
        Try(errors, "桌面此电脑", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", ClsidMyComputer, s.ShowThisPcIcon ? 0 : 1));
        Try(errors, "小按钮任务栏", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", s.SmallTaskbar ? 1 : 0));
        Try(errors, "删除确认", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete", s.ConfirmDelete ? 1 : 0));
        Try(errors, "音频服务", () => SetAudio(s.EnableAudio));
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
        Try(errors, "密码复杂性", () => SetPasswordComplexity(s.DisablePasswordComplexity));
        Try(errors, "未登录关机", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", s.ShutdownWithoutLogon ? 1 : 0));
        Try(errors, "关机事件跟踪", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonOn", s.DisableShutdownReason ? 0 : 1));
        Try(errors, "Ctrl+Alt+Del", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", s.DisableCad ? 1 : 0));
        return errors;
    }

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
        return k?.GetValue(name) is int i ? i : null;
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
            Run("sc.exe", "config AudioSrv start= Disabled");
            Run("sc.exe", "config AudioEndpointBuilder start= Disabled");
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

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* ignore */ }
    }
}
