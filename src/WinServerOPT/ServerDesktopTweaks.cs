using System.Diagnostics;
using Microsoft.Win32;

namespace WinOpt;

/// <summary>社区 Server 桌面脚本中的注册表与 DISM 优化（原工具未覆盖部分）。</summary>
internal static class ServerDesktopTweaks
{
    internal const string ClsidControlPanel = "{21EC2020-3AEA-1069-A2D8-08002B30309D}";
    internal const string ClsidRecycleBin = "{6459FF20-5081-101B-9F08-00AA002F954E}";
    internal const string SamPasswordKey = @"SYSTEM\CurrentControlSet\Control\SAM\Domain\Registration";
    internal const string HideDesktopIcons = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";

    private static readonly string[] MediaFeaturesToEnable =
    {
        "Server-Media-Foundation",
        "DirectPlay",
        "Wireless-Networking",
        "MediaFoundation",
    };

    private static readonly string[] BloatFeaturesToDisable =
    {
        "SystemDataArchiver",
        "WindowsAdminCenterSetup",
        "AzureArcSetup",
    };

    private static readonly string[] RsatFeaturePrefixes = { "RSAT-", "Rsat" };

    private static readonly Dictionary<string, bool> DismEnabledCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, bool> DismExistsCache = new(StringComparer.OrdinalIgnoreCase);

    public static void ResetDismCache()
    {
        DismEnabledCache.Clear();
        DismExistsCache.Clear();
    }

    // --- Read ---

    public static bool IsSamPasswordComplexityOff() =>
        GetDword(Hive.HkLm, SamPasswordKey, "PasswordComplexity") == 0;

    public static bool IsSmartScreenOff() =>
        GetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled") == 0;

    public static bool IsOpenFileWarningOff() =>
        GetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation") == 1;

    public static bool IsControlPanelIconShown() =>
        GetDword(Hive.HkCu, HideDesktopIcons, ClsidControlPanel) == 0;

    public static bool IsRecycleBinIconShown() =>
        GetDword(Hive.HkCu, HideDesktopIcons, ClsidRecycleBin) == 0;

    public static bool IsLargeSystemCacheOn() =>
        GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache") == 1
        && GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive") == 1
        && GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMemoryUsage") == 2;

    public static bool IsReservedStorageOff() =>
        GetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves") == 0;

    public static bool IsSrvSplitDisabled() =>
        GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SrvSplitThreshold") == unchecked((int)0xFFFFFFFF);

    public static bool IsGpuHwSchedulingOn() =>
        GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode") == 2;

    public static bool IsLoginKeyboardFilterOff() =>
        GetAccessibilityFlags(@"Control Panel\Accessibility\Keyboard Response") == "126";

    public static bool IsBackgroundAppsOff() =>
        GetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground") == 2;

    public static bool IsClassicSearchOn() =>
        GetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana") == 0
        && GetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode") == 0;

    public static bool IsSearchEngineFeatureOff() =>
        !IsDismFeatureEnabled("SearchEngine") && GetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\WSearch", "Start") == 4;

    public static bool IsDesktopMediaFeaturesOn()
    {
        var any = false;
        foreach (var f in MediaFeaturesToEnable)
        {
            if (!DismFeatureExists(f)) continue;
            any = true;
            if (!IsDismFeatureEnabled(f)) return false;
        }
        return any;
    }

    public static bool IsServerBloatFeaturesOff(bool includeRsatScan = true)
    {
        foreach (var f in BloatFeaturesToDisable)
        {
            if (IsDismFeatureEnabled(f)) return false;
        }
        if (!includeRsatScan) return true;
        return !HasInstalledRsat();
    }

    // --- Apply ---

    public static void ApplySamPasswordComplexity(bool disable)
    {
        if (disable)
            SetDword(Hive.HkLm, SamPasswordKey, "PasswordComplexity", 0);
        else
            DeleteValue(Hive.HkLm, SamPasswordKey, "PasswordComplexity");
    }

    public static void ApplySmartScreenAndOpenWarning(bool disable)
    {
        if (disable)
        {
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", 0);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation", 1);
        }
        else
        {
            DeleteValue(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled");
            DeleteValue(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation");
        }
    }

    public static void ApplyDesktopIcons(bool controlPanelAndRecycleBin)
    {
        var hide = controlPanelAndRecycleBin ? 0 : 1;
        SetDword(Hive.HkCu, HideDesktopIcons, ClsidControlPanel, hide);
        SetDword(Hive.HkCu, HideDesktopIcons, ClsidRecycleBin, hide);
    }

    public static void ApplyLargeSystemCache(bool enable)
    {
        if (enable)
        {
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1);
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1);
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMemoryUsage", 2);
        }
        else
        {
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
            DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive");
            DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMemoryUsage");
        }
    }

    public static void ApplyReservedStorage(bool disable)
    {
        if (disable)
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves", 0);
        else
            DeleteValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves");
    }

    public static void ApplySrvSplitThreshold(bool disableSplit)
    {
        if (disableSplit)
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SrvSplitThreshold", unchecked((int)0xFFFFFFFF));
        else
            DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SrvSplitThreshold");
    }

    public static void ApplyGpuHwScheduling(bool enable)
    {
        if (enable)
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
        else
            DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
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
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
        }
        else
        {
            DeleteValue(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground");
            DeleteValue(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled");
        }
    }

    public static void ApplyClassicSearch(bool enable)
    {
        if (enable)
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "AllowSearchToUseLocation", 0);
        }
        else
        {
            DeleteValue(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
            DeleteValue(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode");
            DeleteValue(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "AllowSearchToUseLocation");
        }
    }

    public static void ApplySearchEngineFeature(bool disable)
    {
        if (disable)
        {
            if (IsDismFeatureEnabled("SearchEngine")) DismDisable("SearchEngine");
            RunSc("stop WSearch");
            RunSc("config WSearch start= disabled");
        }
        else
        {
            if (DismFeatureExists("SearchEngine")) DismEnable("SearchEngine");
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
            using var def = Registry.Users.OpenSubKey(@".DEFAULT", writable: true);
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
        using var k = Registry.CurrentUser.OpenSubKey(subKey);
        return k?.GetValue("Flags") as string;
    }

    private static bool DismFeatureExists(string name)
    {
        if (DismExistsCache.TryGetValue(name, out var cached))
            return cached;

        try
        {
            var output = RunCapture("dism.exe", $"/online /Get-FeatureInfo /FeatureName:{name}");
            var exists = output.IndexOf("Error", StringComparison.OrdinalIgnoreCase) < 0
                && output.IndexOf("not found", StringComparison.OrdinalIgnoreCase) < 0;
            DismExistsCache[name] = exists;
            if (exists && output.IndexOf("State : Enabled", StringComparison.OrdinalIgnoreCase) >= 0)
                DismEnabledCache[name] = true;
            else if (exists)
                DismEnabledCache[name] = false;
            return exists;
        }
        catch
        {
            DismExistsCache[name] = false;
            return false;
        }
    }

    private static bool IsDismFeatureEnabled(string name)
    {
        if (DismEnabledCache.TryGetValue(name, out var cached))
            return cached;

        try
        {
            var output = RunCapture("dism.exe", $"/online /Get-FeatureInfo /FeatureName:{name}");
            var enabled = output.IndexOf("State : Enabled", StringComparison.OrdinalIgnoreCase) >= 0;
            DismEnabledCache[name] = enabled;
            DismExistsCache[name] = output.IndexOf("not found", StringComparison.OrdinalIgnoreCase) < 0;
            return enabled;
        }
        catch
        {
            DismEnabledCache[name] = false;
            return false;
        }
    }

    private static void DismEnable(string name) =>
        Run("dism.exe", $"/online /Enable-Feature /FeatureName:{name} /All /NoRestart");

    private static void DismDisable(string name) =>
        Run("dism.exe", $"/online /Disable-Feature /FeatureName:{name} /NoRestart");

    private static bool HasInstalledRsat()
    {
        foreach (var (name, state) in EnumerateDismFeatures())
        {
            if (IsRsatFeature(name) && state.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static void DisableInstalledRsat()
    {
        foreach (var (name, state) in EnumerateDismFeatures())
        {
            if (IsRsatFeature(name) && state.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0)
                DismDisable(name);
        }
    }

    private static IEnumerable<(string Name, string State)> EnumerateDismFeatures()
    {
        string output;
        try
        {
            output = RunCapture("dism.exe", "/online /Get-Features /Format:List");
        }
        catch
        {
            yield break;
        }

        string? current = null;
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("Feature Name : ", StringComparison.OrdinalIgnoreCase))
                current = line.Substring("Feature Name : ".Length).Trim();
            else if (line.StartsWith("State : ", StringComparison.OrdinalIgnoreCase) && current != null)
            {
                yield return (current, line.Substring("State : ".Length).Trim());
                current = null;
            }
        }
    }

    private static bool IsRsatFeature(string name)
    {
        foreach (var p in RsatFeaturePrefixes)
        {
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private enum Hive { HkLm, HkCu }

    private static int? GetDword(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) switch
        {
            int i => i,
            byte b => b,
            _ => null,
        };
    }

    private static void SetDword(Hive hive, string key, string name, int value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
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

    private static void DeleteValue(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
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
