using System.Diagnostics;
using Microsoft.Win32;

namespace WinOpt;

/// <summary>VB/社区 Server 桌面脚本中的注册表与 DISM 优化（原 Form 未覆盖部分）。</summary>
internal static class ServerDesktopTweaks
{
    internal const string ClsidControlPanel = "{21EC2020-3AEA-1069-A2D8-08002B30309D}";
    internal const string ClsidRecycleBin = "{6459FF20-5081-101B-9F08-00AA002F954E}";
    internal const string SamPasswordKey = @"SYSTEM\CurrentControlSet\Control\SAM\Domain\Registration";
    internal const string HideDesktopIcons = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";

    private static readonly string[] MediaFeaturesToEnable =
    [
        "Server-Media-Foundation",
        "DirectPlay",
        "Wireless-Networking",
        "MediaFoundation",
    ];

    private static readonly string[] BloatFeaturesToDisable =
    [
        "SystemDataArchiver",
        "WindowsAdminCenterSetup",
        "AzureArcSetup",
        "SearchEngine",
    ];

    private static readonly string[] RsatFeaturePrefixes = ["RSAT-", "Rsat"];

    // --- Read ---

    public static bool IsSmartScreenOff() =>
        DwordEquals(@"Software\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", 0);

    public static bool IsOpenFileWarningOff() =>
        DwordEquals(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation", 1);

    public static bool IsControlPanelIconShown() =>
        DwordEquals(HideDesktopIcons, ClsidControlPanel, 0);

    public static bool IsRecycleBinIconShown() =>
        DwordEquals(HideDesktopIcons, ClsidRecycleBin, 0);

    public static bool IsLargeSystemCacheOn() =>
        DwordEquals(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1)
        && DwordEquals(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1)
        && DwordEquals(@"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMemoryUsage", 2);

    public static bool IsReservedStorageOff() =>
        DwordEquals(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves", 0);

    public static bool IsSrvSplitDisabled() =>
        GetDword(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SrvSplitThreshold") == unchecked((int)0xFFFFFFFF);

    public static bool IsGpuHwSchedulingOn() =>
        GetDword(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode") == 2;

    public static bool IsLoginKeyboardFilterOff() =>
        GetAccessibilityFlags(@"Control Panel\Accessibility\Keyboard Response") == "126";

    public static bool IsBackgroundAppsOff() =>
        DwordEquals(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2);

    public static bool IsClassicSearchOn() =>
        DwordEquals(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0)
        && DwordEquals(@"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0);

    public static bool IsDefenderOff() =>
        DwordEquals(@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1);

    public static bool IsSearchEngineFeatureOff() =>
        !IsDismFeatureEnabled("SearchEngine") && ServiceStartEquals("WSearch", 4);

    public static bool IsDesktopMediaFeaturesOn()
    {
        foreach (var f in MediaFeaturesToEnable)
        {
            if (DismFeatureExists(f) && !IsDismFeatureEnabled(f)) return false;
        }
        return true;
    }

    public static bool IsServerBloatFeaturesOff()
    {
        foreach (var f in BloatFeaturesToDisable)
        {
            if (IsDismFeatureEnabled(f)) return false;
        }
        return !HasInstalledRsat();
    }

    // --- Apply ---

    public static void ApplySamPasswordComplexity(bool disable)
    {
        if (disable)
            SetDword(SamPasswordKey, "PasswordComplexity", 0);
        else
            DeleteValue(SamPasswordKey, "PasswordComplexity");
    }

    public static void ApplySmartScreenAndOpenWarning(bool disable)
    {
        if (disable)
        {
            SetDword(@"Software\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", 0);
            SetDword(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation", 1);
        }
        else
        {
            DeleteValue(@"Software\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled");
            DeleteValue(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation");
        }
    }

    public static void ApplyDesktopIcons(bool controlPanel, bool recycleBin)
    {
        SetDword(HideDesktopIcons, ClsidControlPanel, controlPanel ? 0 : 1);
        SetDword(HideDesktopIcons, ClsidRecycleBin, recycleBin ? 0 : 1);
    }

    public static void ApplyLargeSystemCache(bool enable)
    {
        if (enable)
        {
            SetDword(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1);
            SetDword(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1);
            SetDword(@"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMemoryUsage", 2);
        }
        else
        {
            SetDword(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
            DeleteValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive");
            DeleteValue(@"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMemoryUsage");
        }
    }

    public static void ApplyReservedStorage(bool disable)
    {
        if (disable)
            SetDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves", 0);
        else
            DeleteValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves");
    }

    public static void ApplySrvSplitThreshold(bool disableSplit)
    {
        if (disableSplit)
            SetDword(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SrvSplitThreshold", unchecked((int)0xFFFFFFFF));
        else
            DeleteValue(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SrvSplitThreshold");
    }

    public static void ApplyGpuHwScheduling(bool enable)
    {
        if (enable)
            SetDword(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
        else
            DeleteValue(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
    }

    public static void ApplyLoginKeyboardFilters(bool disable)
    {
        var paths = new[]
        {
            @"Control Panel\Accessibility\Keyboard Response",
            @"Control Panel\Accessibility\StickyKeys",
            @"Control Panel\Accessibility\ToggleKeys",
            @"Control Panel\Accessibility\MouseKeys",
        };
        if (disable)
        {
            SetString(Hive.HkCu, paths[0], "Flags", "126");
            SetString(Hive.HkCu, paths[1], "Flags", "506");
            SetString(Hive.HkCu, paths[2], "Flags", "58");
            SetString(Hive.HkCu, paths[3], "Flags", "0");
            SetDefaultUserAccessibility(paths[0], "126");
            SetDefaultUserAccessibility(paths[1], "506");
        }
        else
        {
            foreach (var p in paths) DeleteValue(Hive.HkCu, p, "Flags");
        }
    }

    public static void ApplyBackgroundApps(bool disable)
    {
        if (disable)
        {
            SetDword(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2);
            SetDword(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
        }
        else
        {
            DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground");
            DeleteValue(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled");
        }
    }

    public static void ApplyClassicSearch(bool enable)
    {
        if (enable)
        {
            SetDword(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
            SetDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0);
            SetDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "AllowSearchToUseLocation", 0);
        }
        else
        {
            DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
            DeleteValue(@"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode");
            DeleteValue(@"Software\Microsoft\Windows\CurrentVersion\Search", "AllowSearchToUseLocation");
        }
    }

    public static void ApplyDefender(bool disable)
    {
        if (disable)
        {
            SetDword(@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1);
            RunSc("stop WinDefend");
            RunSc("config WinDefend start= disabled");
        }
        else
        {
            DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
            RunSc("config WinDefend start= auto");
            RunSc("start WinDefend");
        }
    }

    public static void ApplySearchEngineFeature(bool disable)
    {
        if (disable)
        {
            DismDisable("SearchEngine");
            RunSc("stop WSearch");
            RunSc("config WSearch start= disabled");
        }
        else
        {
            DismEnable("SearchEngine");
            RunSc("config WSearch start= auto");
            RunSc("start WSearch");
        }
    }

    public static void ApplyDesktopMediaFeatures(bool enable)
    {
        foreach (var f in MediaFeaturesToEnable)
        {
            if (!DismFeatureExists(f)) continue;
            if (enable) DismEnable(f);
            else DismDisable(f);
        }
    }

    public static void ApplyServerBloatFeatures(bool disable)
    {
        if (!disable) return;
        foreach (var f in BloatFeaturesToDisable)
        {
            if (IsDismFeatureEnabled(f)) DismDisable(f);
        }
        DisableInstalledRsat();
    }

    // --- helpers ---

    private static void SetDefaultUserAccessibility(string subKey, string flags)
    {
        try
        {
            using var users = Registry.Users;
            using var def = users.OpenSubKey(@".DEFAULT", writable: true);
            using var key = def?.OpenSubKey(subKey, writable: true);
            key?.SetValue("Flags", flags, RegistryValueKind.String);
        }
        catch
        {
            // .DEFAULT 不可写时忽略
        }
    }

    private static string? GetAccessibilityFlags(string subKey)
    {
        using var baseKey = Registry.CurrentUser;
        using var k = baseKey.OpenSubKey(subKey);
        return k?.GetValue("Flags") as string;
    }

    private static bool DismFeatureExists(string name)
    {
        try
        {
            var output = RunCapture("dism.exe", $"/online /Get-FeatureInfo /FeatureName:{name}");
            return output.IndexOf("Error", StringComparison.OrdinalIgnoreCase) < 0
                && output.IndexOf("not found", StringComparison.OrdinalIgnoreCase) < 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDismFeatureEnabled(string name)
    {
        try
        {
            var output = RunCapture("dism.exe", $"/online /Get-FeatureInfo /FeatureName:{name}");
            return output.IndexOf("State : Enabled", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static void DismEnable(string name) =>
        Run("dism.exe", $"/online /Enable-Feature /FeatureName:{name} /All /NoRestart");

    private static void DismDisable(string name) =>
        Run("dism.exe", $"/online /Disable-Feature /FeatureName:{name} /NoRestart");

    private static bool HasInstalledRsat()
    {
        try
        {
            var output = RunCapture("dism.exe", "/online /Get-Features /Format:List");
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.StartsWith("Feature Name : ", StringComparison.OrdinalIgnoreCase)) continue;
                var name = line.Substring("Feature Name : ".Length).Trim();
                if (!IsRsatFeature(name)) continue;
                if (output.IndexOf($"Feature Name : {name}", StringComparison.OrdinalIgnoreCase) >= 0
                    && output.IndexOf("State : Enabled", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    private static void DisableInstalledRsat()
    {
        try
        {
            var output = RunCapture("dism.exe", "/online /Get-Features /Format:List");
            string? current = null;
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("Feature Name : ", StringComparison.OrdinalIgnoreCase))
                    current = line.Substring("Feature Name : ".Length).Trim();
                else if (line.StartsWith("State : ", StringComparison.OrdinalIgnoreCase)
                    && current != null
                    && IsRsatFeature(current)
                    && line.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0)
                    DismDisable(current);
            }
        }
        catch { /* ignore */ }
    }

    private static bool IsRsatFeature(string name)
    {
        foreach (var p in RsatFeaturePrefixes)
        {
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool DwordEquals(string key, string name, int expected) => GetDword(key, name) == expected;

    private static bool ServiceStartEquals(string service, int expected) =>
        GetDword($@"SYSTEM\CurrentControlSet\Services\{service}", "Start") == expected;

    private static int? GetDword(string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) switch
        {
            int i => i,
            byte b => b,
            _ => null,
        };
    }

    private enum Hive { HkLm, HkCu }

    private static void SetDword(string key, string name, int value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(Hive hive, string key, string name, string value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            RegistryView.Default);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.OpenSubKey(key, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            RegistryView.Default);
        using var k = baseKey.OpenSubKey(key, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void RunSc(string args) => Run("sc.exe", args);

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
        p.WaitForExit(600_000);
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
        }) ?? throw new InvalidOperationException("无法启动 " + fileName);
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(600_000);
        return output;
    }
}
