using System.Diagnostics;
using Microsoft.Win32;

namespace SrvDesk;

/// <summary>对齐 ZyperWin++ 且适合 Server 桌面的 12 项（注册表 / 服务 / 环境变量）。</summary>
internal static class CommunityTweaks
{
    private const string ExplorerPolCu = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
    private const string ExplorerPolLm = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
    private const string ExplorerAdv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string HideIcons = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
    private const string HideIconsClassic = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu";
    private const string SpotlightClsid = "{2cc5ca98-6485-489a-920e-b3e88a6ccce3}";
    private const string DupDriveKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}";
    private const string RunMru = @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";
    private const string SrvDeskTweaks = @"Software\SrvDesk\Tweaks";
    private const string Winlogon = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
    private const string CrashControl = @"SYSTEM\CurrentControlSet\Control\CrashControl";
    private const string SessionEnv = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
    private const int DwordMax = unchecked((int)0xFFFFFFFF);

    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Take(string field, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return false;
            ApplyLog.DebugField(field, baseline is null ? null : f(baseline), f(s));
            return true;
        }

        if (Take("DisableBrokenShortcutTracking", x => x.DisableBrokenShortcutTracking))
            SetBrokenShortcutTracking(!s.DisableBrokenShortcutTracking);
        if (Take("ExplorerSeparateProcess", x => x.ExplorerSeparateProcess))
            SetDword(Hive.HkCu, ExplorerAdv, "SeparateProcess", s.ExplorerSeparateProcess ? 1 : 0);
        if (Take("AutoRestartExplorer", x => x.AutoRestartExplorer))
            SetDword(Hive.HkLm, Winlogon, "AutoRestartShell", s.AutoRestartExplorer ? 1 : 0);
        if (Take("HideDesktopSpotlight", x => x.HideDesktopSpotlight))
            SetDesktopSpotlightHidden(s.HideDesktopSpotlight);
        if (Take("HideDuplicateRemovableDrives", x => x.HideDuplicateRemovableDrives))
            SetDuplicateDriveHidden(s.HideDuplicateRemovableDrives);
        if (Take("DisableRunDialogHistory", x => x.DisableRunDialogHistory))
            SetRunDialogHistory(!s.DisableRunDialogHistory);
        if (Take("MergeSvchostProcesses", x => x.MergeSvchostProcesses))
            SetSvchostMerged(s.MergeSvchostProcesses);
        if (Take("DisableDistributedLinkTracking", x => x.DisableDistributedLinkTracking))
            SetTrkWks(!s.DisableDistributedLinkTracking);
        if (Take("DisableLowDiskSpaceChecks", x => x.DisableLowDiskSpaceChecks))
            SetLowDiskChecks(!s.DisableLowDiskSpaceChecks);
        if (Take("UsbFullPowerOff", x => x.UsbFullPowerOff))
            SetUsbFullPowerOff(s.UsbFullPowerOff);
        if (Take("AutoRebootOnCrash", x => x.AutoRebootOnCrash))
            SetDword(Hive.HkLm, CrashControl, "AutoReboot", s.AutoRebootOnCrash ? 1 : 0);
        if (Take("DisableDotNetPowerShellTelemetry", x => x.DisableDotNetPowerShellTelemetry))
            SetCliTelemetryOptOut(s.DisableDotNetPowerShellTelemetry);

        if (NeedsExplorerRestart(baseline, s))
            DesktopQuickActions.RestartExplorer();
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.DisableBrokenShortcutTracking = DwordEquals(Hive.HkCu, ExplorerPolCu, "NoResolveTrack", 1);
        s.ExplorerSeparateProcess = DwordEquals(Hive.HkCu, ExplorerAdv, "SeparateProcess", 1);
        s.AutoRestartExplorer = DwordEquals(Hive.HkLm, Winlogon, "AutoRestartShell", 1);
        s.HideDesktopSpotlight = DwordEquals(Hive.HkCu, HideIcons, SpotlightClsid, 1);
        s.HideDuplicateRemovableDrives = !KeyExists(Hive.HkLm, DupDriveKey);
        s.DisableRunDialogHistory = DwordEquals(Hive.HkCu, SrvDeskTweaks, "DisableRunDialogHistory", 1);
        s.MergeSvchostProcesses = IsMaxDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB");
        s.DisableDistributedLinkTracking = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\TrkWks", "Start", 4);
        s.DisableLowDiskSpaceChecks = DwordEquals(Hive.HkCu, ExplorerPolCu, "NoLowDiskSpaceChecks", 1);
        s.UsbFullPowerOff = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\USB", "DisableSelectiveSuspend", 1);
        s.AutoRebootOnCrash = DwordEquals(Hive.HkLm, CrashControl, "AutoReboot", 1);
        s.DisableDotNetPowerShellTelemetry = IsEnvOptOut("DOTNET_CLI_TELEMETRY_OPTOUT")
            && IsEnvOptOut("POWERSHELL_TELEMETRY_OPTOUT");
    }

    public static bool AnyChanged(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.DisableBrokenShortcutTracking != s.DisableBrokenShortcutTracking
            || b.ExplorerSeparateProcess != s.ExplorerSeparateProcess
            || b.AutoRestartExplorer != s.AutoRestartExplorer
            || b.HideDesktopSpotlight != s.HideDesktopSpotlight
            || b.HideDuplicateRemovableDrives != s.HideDuplicateRemovableDrives
            || b.DisableRunDialogHistory != s.DisableRunDialogHistory
            || b.MergeSvchostProcesses != s.MergeSvchostProcesses
            || b.DisableDistributedLinkTracking != s.DisableDistributedLinkTracking
            || b.DisableLowDiskSpaceChecks != s.DisableLowDiskSpaceChecks
            || b.UsbFullPowerOff != s.UsbFullPowerOff
            || b.AutoRebootOnCrash != s.AutoRebootOnCrash
            || b.DisableDotNetPowerShellTelemetry != s.DisableDotNetPowerShellTelemetry;
    }

    public static bool NeedsExplorerRestart(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.DisableBrokenShortcutTracking != s.DisableBrokenShortcutTracking
            || b.ExplorerSeparateProcess != s.ExplorerSeparateProcess
            || b.HideDesktopSpotlight != s.HideDesktopSpotlight
            || b.HideDuplicateRemovableDrives != s.HideDuplicateRemovableDrives
            || b.DisableRunDialogHistory != s.DisableRunDialogHistory
            || b.DisableLowDiskSpaceChecks != s.DisableLowDiskSpaceChecks;
    }

    private static void SetBrokenShortcutTracking(bool track)
    {
        var v = track ? 0 : 1;
        foreach (var hive in new[] { Hive.HkCu, Hive.HkLm })
        {
            var key = hive == Hive.HkCu ? ExplorerPolCu : ExplorerPolLm;
            SetDword(hive, key, "NoResolveTrack", v);
            SetDword(hive, key, "NoResolveSearch", v);
            SetDword(hive, key, "LinkResolveIgnoreLinkInfo", v);
        }
    }

    private static void SetDesktopSpotlightHidden(bool hide)
    {
        SetDword(Hive.HkCu, HideIcons, SpotlightClsid, hide ? 1 : 0);
        SetDword(Hive.HkCu, HideIconsClassic, SpotlightClsid, hide ? 1 : 0);
    }

    private static void SetDuplicateDriveHidden(bool hide)
    {
        if (hide)
            DeleteKeyTree(Hive.HkLm, DupDriveKey);
        else
            CreateKey(Hive.HkLm, DupDriveKey);
    }

    private static void SetRunDialogHistory(bool keep)
    {
        // 不写 Start_TrackProgs：那一项归「关闭应用启动跟踪」，两开关必须独立。
        SetDword(Hive.HkCu, SrvDeskTweaks, "DisableRunDialogHistory", keep ? 0 : 1);
        if (keep) return;
        using var baseKey = OpenBase(Hive.HkCu);
        using var k = baseKey.OpenSubKey(RunMru, writable: true);
        if (k is null) return;
        foreach (var name in k.GetValueNames())
        {
            var old = k.GetValue(name);
            ApplyLog.RegistryDelete("HKCU", RunMru, name, old);
            k.DeleteValue(name, throwOnMissingValue: false);
        }
    }

    private static void SetSvchostMerged(bool merge)
    {
        if (merge)
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", DwordMax);
        else
            DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB");
    }

    private static void SetTrkWks(bool enable)
    {
        if (!KeyExists(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\TrkWks")) return;
        Run("sc.exe", enable ? "config TrkWks start= auto" : "config TrkWks start= disabled");
        if (!enable) Run("sc.exe", "stop TrkWks");
        else Run("sc.exe", "start TrkWks");
    }

    private static void SetLowDiskChecks(bool enable)
    {
        SetDword(Hive.HkCu, ExplorerPolCu, "NoLowDiskSpaceChecks", enable ? 0 : 1);
        SetDword(Hive.HkLm, ExplorerPolLm, "NoLowDiskSpaceChecks", enable ? 0 : 1);
    }

    private static void SetUsbFullPowerOff(bool on)
    {
        SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\USB", "DisableSelectiveSuspend", on ? 1 : 0);
        var val = on ? "0" : "1";
        Run("powercfg.exe",
            "/SETACVALUEINDEX SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 " + val);
        Run("powercfg.exe",
            "/SETDCVALUEINDEX SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 " + val);
        Run("powercfg.exe", "/SETACTIVE SCHEME_CURRENT");
    }

    private static void SetCliTelemetryOptOut(bool optOut)
    {
        if (optOut)
        {
            SetString(Hive.HkLm, SessionEnv, "DOTNET_CLI_TELEMETRY_OPTOUT", "1");
            SetString(Hive.HkLm, SessionEnv, "POWERSHELL_TELEMETRY_OPTOUT", "1");
            Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", "1", EnvironmentVariableTarget.Machine);
        }
        else
        {
            DeleteValue(Hive.HkLm, SessionEnv, "DOTNET_CLI_TELEMETRY_OPTOUT");
            DeleteValue(Hive.HkLm, SessionEnv, "POWERSHELL_TELEMETRY_OPTOUT");
            Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", null, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", null, EnvironmentVariableTarget.Machine);
        }
    }

    private static bool IsEnvOptOut(string name)
    {
        var machine = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
        if (string.Equals(machine, "1", StringComparison.Ordinal)) return true;
        return DwordOrSzIsOne(Hive.HkLm, SessionEnv, name);
    }

    private static bool DwordOrSzIsOne(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        var v = k?.GetValue(name);
        if (v is int i) return i == 1;
        return string.Equals(Convert.ToString(v), "1", StringComparison.Ordinal);
    }

    private enum Hive { HkLm, HkCu }

    private static RegistryKey OpenBase(Hive hive) =>
        RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);

    private static bool DwordEquals(Hive hive, string key, string name, int expected)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) is int i && i == expected;
    }

    private static bool IsMaxDword(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) is int i && i == DwordMax;
    }

    private static bool KeyExists(Hive hive, string key)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k is not null;
    }

    private static object? GetValue(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name);
    }

    private static void SetDword(Hive hive, string key, string name, int value)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDword(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old, value);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true);
        k?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(Hive hive, string key, string name, string value)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryString(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old, value);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true);
        k?.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDelete(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void DeleteKeyTree(Hive hive, string key)
    {
        var existed = KeyExists(hive, key);
        ApplyLog.RegistryDeleteTree(hive == Hive.HkLm ? "HKLM" : "HKCU", key, existed);
        if (!existed) return;
        using var baseKey = OpenBase(hive);
        try { baseKey.DeleteSubKeyTree(key, throwOnMissingSubKey: false); }
        catch { /* 无权限或已被删 */ }
    }

    private static void CreateKey(Hive hive, string key)
    {
        if (KeyExists(hive, key)) return;
        ApplyLog.RegistryString(hive == Hive.HkLm ? "HKLM" : "HKCU", key, "(default)", null, "");
        using var baseKey = OpenBase(hive);
        baseKey.CreateSubKey(key, writable: true)?.Dispose();
    }

    private static void Run(string file, string args)
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
            p?.WaitForExit(20_000);
        }
        catch (Exception ex)
        {
            ApplyLog.Debug($"{file} {args} 失败：{ex.Message}");
        }
    }
}
