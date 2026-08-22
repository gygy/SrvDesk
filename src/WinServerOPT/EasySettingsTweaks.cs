using System.Diagnostics;
using Microsoft.Win32;

namespace WinOpt;

/// <summary>资源管理器 / 隐私 / 其他设置（对齐轻松设置功能，注册表与服务实现）。</summary>
internal static class EasySettingsTweaks
{
    private const string ExplorerAdv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
    private const string DeviceGuard = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
    private const string Hvci = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
    private const string MemMgmt = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
    private const string Prefetch = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters";
    private const string SearchPol = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search";
    private const string TermServices = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services";
    private const string RdpTcp = @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";

    public static void ApplyExplorerBits(Optimizer.State s)
    {
        SetDword(Hive.HkCu, ExplorerAdv, "ShowSuperHidden", s.HideProtectedOsFiles ? 0 : 1);
        SetDword(Hive.HkCu, ExplorerAdv, "IconsOnly", s.AlwaysShowIconsNeverThumbnails ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdv, "HideDrivesWithNoMedia", s.ShowEmptyDrives ? 0 : 1);
        SetDword(Hive.HkCu, Explorer, "ShowRecent", s.ShowRecentFiles ? 1 : 0);
        SetDword(Hive.HkCu, Explorer, "ShowFrequent", s.ShowFrequentPlaces ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdv, "ShowCloudFilesInQuickAccess", s.HideOfficeCloudFiles ? 0 : 1);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", s.DisableOneDrive ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdv, "TaskbarMn", s.HideTaskbarChat ? 0 : 1);
        SetDword(Hive.HkCu, ExplorerAdv, "TaskbarCo", s.HideTaskbarCopilot ? 0 : 1);
    }

    public static void ApplyPrivacyBits(Optimizer.State s)
    {
        SetDword(Hive.HkLm, SearchPol, "AllowCloudSearch", s.DisableCloudSearch ? 0 : 1);
        SetDword(Hive.HkLm, SearchPol, "DisableWebSearch", s.DisableWebSearch ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", s.DisableWebSearch ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "HistoryViewEnabled", s.DisableSearchHistory ? 0 : 1);
        SetDword(Hive.HkCu, @"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", s.DisableWebsiteLangList ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdv, "Start_TrackProgs", s.DisableAppLaunchTracking ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338393Enabled", s.DisableSettingsSuggestions ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", s.DisableSettingsSuggestions ? 0 : 1);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", s.DisableInkingPersonalization ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", s.DisableInkingPersonalization ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", s.DisableAdTracking ? 0 : 1);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", s.DisableDeliveryOpt ? 100 : 1);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\MRT", "DontOfferThroughWUAU", s.ExcludeMsrtFromWu ? 1 : 0);
        Win11DesktopTweaks.SetFeatureUpdatePause(s.PauseFeatureUpdatesUntil2035);
    }

    public static void Apply(Optimizer.State s)
    {
        ApplyExplorerBits(s);
        ApplyPrivacyBits(s);
        SetMeltdownSpectre(s.DisableMeltdownSpectre);
        SetHvci(!s.DisableMemoryIntegrity);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "ConfigCIPolicyEnable", s.DisableWdac ? 0 : 1);
        SetVbs(!s.DisableVbs);
        SetTcpBbr2(s.EnableTcpBbr2);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableSR", s.DisableSystemRestore ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", s.DisableCeip ? 0 : 1);
        SetService("DPS", !s.DisableDiagnosticPolicy);

        SetDword(Hive.HkLm, TermServices, "fAllowToGetHelp", s.DisableRemoteAssistance ? 0 : 1);
        SetMmAgent("MemoryCompression", !s.DisableMemoryCompression);
        SetMmAgent("ApplicationPreLaunch", !s.DisableAppPrelaunch);
        SetMmAgent("PageCombining", !s.DisablePageCombining);
        SetService("UCPD", !s.DisableUcpdDriver);
    }

    /// <summary>仅读资源管理器页开关（纯注册表，无 PowerShell/netsh）。</summary>
    public static void ReadExplorerOnly(Optimizer.State s)
    {
        s.HideProtectedOsFiles = DwordEquals(Hive.HkCu, ExplorerAdv, "ShowSuperHidden", 0);
        s.AlwaysShowIconsNeverThumbnails = DwordEquals(Hive.HkCu, ExplorerAdv, "IconsOnly", 1);
        s.ShowEmptyDrives = !DwordEquals(Hive.HkCu, ExplorerAdv, "HideDrivesWithNoMedia", 1);
        s.ShowRecentFiles = !DwordEquals(Hive.HkCu, Explorer, "ShowRecent", 0);
        s.ShowFrequentPlaces = !DwordEquals(Hive.HkCu, Explorer, "ShowFrequent", 0);
        s.HideOfficeCloudFiles = DwordEquals(Hive.HkCu, ExplorerAdv, "ShowCloudFilesInQuickAccess", 0);
        s.DisableOneDrive = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1);
        s.HideTaskbarChat = DwordEquals(Hive.HkCu, ExplorerAdv, "TaskbarMn", 0);
        s.HideTaskbarCopilot = DwordEquals(Hive.HkCu, ExplorerAdv, "TaskbarCo", 0);
    }

    public static void ReadInto(Optimizer.State s)
    {
        ReadExplorerOnly(s);

        s.DisableCloudSearch = DwordEquals(Hive.HkLm, SearchPol, "AllowCloudSearch", 0);
        s.DisableWebSearch = DwordEquals(Hive.HkLm, SearchPol, "DisableWebSearch", 1)
            || DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
        s.DisableSearchHistory = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "HistoryViewEnabled", 0);
        s.DisableWebsiteLangList = DwordEquals(Hive.HkCu, @"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1);
        s.DisableAppLaunchTracking = DwordEquals(Hive.HkCu, ExplorerAdv, "Start_TrackProgs", 0);
        s.DisableSettingsSuggestions = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0);
        s.DisableInkingPersonalization = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1);
        s.DisableAdTracking = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0);
        s.DisableDeliveryOpt = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 100);
        s.ExcludeMsrtFromWu = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\MRT", "DontOfferThroughWUAU", 1);
        s.PauseFeatureUpdatesUntil2035 = Win11DesktopTweaks.IsPauseFeatureUpdatesUntil2035On();

        s.DisableMeltdownSpectre = IsMeltdownSpectreDisabled();
        s.DisableMemoryIntegrity = DwordEquals(Hive.HkLm, Hvci, "Enabled", 0);
        s.DisableWdac = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "ConfigCIPolicyEnable", 0);
        s.DisableVbs = DwordEquals(Hive.HkLm, DeviceGuard, "EnableVirtualizationBasedSecurity", 0);
        s.EnableTcpBbr2 = IsTcpBbr2On();
        s.DisableSystemRestore = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableSR", 1);
        s.DisableCeip = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0);
        s.DisableDiagnosticPolicy = ServiceDisabled("DPS");

        s.DisableRemoteAssistance = DwordEquals(Hive.HkLm, TermServices, "fAllowToGetHelp", 0);
        s.DisableMemoryCompression = !IsMmAgentOn("MemoryCompression");
        s.DisableAppPrelaunch = !IsMmAgentOn("ApplicationPreLaunch");
        s.DisablePageCombining = !IsMmAgentOn("PageCombining");
        s.DisableUcpdDriver = ServiceDisabled("UCPD");
    }

    public static bool IsMemoryCompressionDisabled() => !IsMmAgentOn("MemoryCompression");
    public static bool IsAppPrelaunchDisabled() => !IsMmAgentOn("ApplicationPreLaunch");
    public static bool IsPageCombiningDisabled() => !IsMmAgentOn("PageCombining");
    public static bool IsUcpdDisabled() => ServiceDisabled("UCPD");

    /// <summary>后台预热 Get-MMAgent，避免首次打开「系统服务」卡顿。</summary>
    public static void WarmupMmAgentCache() => EnsureMmAgentCache();

    public static void SetRemoteAssistanceDisabled(bool disable) =>
        SetDword(Hive.HkLm, TermServices, "fAllowToGetHelp", disable ? 0 : 1);

    public static void SetMemoryCompressionDisabled(bool disable) =>
        SetMmAgent("MemoryCompression", !disable);

    public static void SetAppPrelaunchDisabled(bool disable) =>
        SetMmAgent("ApplicationPreLaunch", !disable);

    public static void SetPageCombiningDisabled(bool disable) =>
        SetMmAgent("PageCombining", !disable);

    public static void SetUcpdDisabled(bool disable) =>
        SetService("UCPD", !disable);

    public static int GetSearchboxMode() =>
        GetDword(Hive.HkCu, ExplorerAdv, "SearchboxTaskbarMode");

    public static void SetSearchboxMode(int mode) =>
        SetDword(Hive.HkCu, ExplorerAdv, "SearchboxTaskbarMode", mode);

    public static int GetTaskbarGlomLevel() =>
        GetDword(Hive.HkCu, ExplorerAdv, "TaskbarGlomLevel");

    public static void SetTaskbarGlomLevel(int level) =>
        SetDword(Hive.HkCu, ExplorerAdv, "TaskbarGlomLevel", level);

    public static int GetRdpPort()
    {
        var n = GetDword(Hive.HkLm, RdpTcp, "PortNumber");
        return n > 0 ? n : 3389;
    }

    public static void SetRdpPort(int port)
    {
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        var old = GetRdpPort();
        SetDword(Hive.HkLm, RdpTcp, "PortNumber", port);
        Run("netsh", $"advfirewall firewall set rule name=\"Remote Desktop\" new localport={port}");
        ApplyLog.Write($"RDP 端口 {old} → {port}");
    }

    public static int GetMaxPrefetchFiles()
    {
        var n = GetDword(Hive.HkLm, Prefetch, "MaxPrefetchFiles");
        return n > 0 ? n : 256;
    }

    public static void SetMaxPrefetchFiles(int count)
    {
        if (count < 1) count = 1;
        SetDword(Hive.HkLm, Prefetch, "MaxPrefetchFiles", count);
        ApplyLog.Write("最大预取文件数：" + count);
    }

    public static bool IsAppLaunchPrefetchOn()
    {
        var v = GetDword(Hive.HkLm, Prefetch, "EnablePrefetcher");
        return v < 0 || (v & 1) != 0;
    }

    public static void SetWindowsSearchEnabled(bool enable)
    {
        SetService("WSearch", enable);
        ApplyLog.Write(enable ? "已恢复 Windows Search" : "已停止并禁用 Windows Search");
    }

    public static bool IsWindowsSearchEnabled() => !ServiceDisabled("WSearch");

    public static void AddSearchFirewallRules()
    {
        foreach (var (name, program) in SearchFirewallTargets())
        {
            Run("netsh",
                $"advfirewall firewall delete rule name=\"{name}\"");
            Run("netsh",
                $"advfirewall firewall add rule name=\"{name}\" dir=out action=block program=\"{program}\" enable=yes");
        }
        ApplyLog.Write("已添加搜索相关防火墙出站拦截规则");
    }

    public static void RemoveSearchFirewallRules()
    {
        foreach (var (name, _) in SearchFirewallTargets())
            Run("netsh", $"advfirewall firewall delete rule name=\"{name}\"");
        ApplyLog.Write("已移除搜索相关防火墙规则");
    }

    public static void OpenFirewallStatus() =>
        Process.Start(new ProcessStartInfo { FileName = "wf.msc", UseShellExecute = true });

    public static void OpenFirewallRules() =>
        Process.Start(new ProcessStartInfo
        {
            FileName = "control.exe",
            Arguments = "firewall.cpl",
            UseShellExecute = true,
        });

    public static void ClearEventLogs()
    {
        foreach (var log in new[] { "Application", "System", "Setup" })
            Run("wevtutil.exe", "cl " + log);
        ApplyLog.Write("已清除 Application/System/Setup 日志");
    }

    public static void SetHibernate(bool disable) =>
        Run("powercfg.exe", disable ? "-h off" : "-h on");

    public static void SetFastStartup(bool disable) =>
        SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", disable ? 0 : 1);

    public static void SetSysMain(bool disable) => SetService("SysMain", !disable);

    public static void SetRdpEnabled(bool enable) =>
        SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", enable ? 0 : 1);

    public static bool IsHibernateDisabled() =>
        DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0);

    public static bool IsFastStartupDisabled() =>
        DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0);

    public static bool IsSysMainDisabled() => ServiceDisabled("SysMain");

    public static bool IsRdpEnabled() =>
        DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0);

    public static bool IsRemoteAssistanceDisabled() =>
        DwordEquals(Hive.HkLm, TermServices, "fAllowToGetHelp", 0);

    private static IEnumerable<(string Name, string Program)> SearchFirewallTargets()
    {
        var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        yield return ("WinOpt-Block-SearchHost", Path.Combine(windir, @"SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\SearchHost.exe"));
        yield return ("WinOpt-Block-SearchApp", Path.Combine(windir, @"SystemApps\Microsoft.Windows.Search_cw5n1h2txyewy\SearchApp.exe"));
        yield return ("WinOpt-Block-Cortana", Path.Combine(windir, @"SystemApps\Microsoft.Windows.Cortana_cw5n1h2txyewy\SearchUI.exe"));
    }

    private static void SetMeltdownSpectre(bool disable)
    {
        if (disable)
        {
            SetDword(Hive.HkLm, MemMgmt, "FeatureSettingsOverride", 3);
            SetDword(Hive.HkLm, MemMgmt, "FeatureSettingsOverrideMask", 3);
        }
        else
        {
            DeleteValue(Hive.HkLm, MemMgmt, "FeatureSettingsOverride");
            DeleteValue(Hive.HkLm, MemMgmt, "FeatureSettingsOverrideMask");
        }
    }

    private static bool IsMeltdownSpectreDisabled() =>
        DwordEquals(Hive.HkLm, MemMgmt, "FeatureSettingsOverride", 3);

    private static void SetHvci(bool enable) =>
        SetDword(Hive.HkLm, Hvci, "Enabled", enable ? 1 : 0);

    private static void SetVbs(bool enable)
    {
        if (enable)
            DeleteValue(Hive.HkLm, DeviceGuard, "EnableVirtualizationBasedSecurity");
        else
            SetDword(Hive.HkLm, DeviceGuard, "EnableVirtualizationBasedSecurity", 0);
    }

    private static bool IsTcpBbr2On()
    {
        try
        {
            var output = RunCapture("netsh", "int tcp show supplemental");
            return output.IndexOf("bbr2", StringComparison.OrdinalIgnoreCase) >= 0
                || output.IndexOf("BBR2", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return false; }
    }

    private static void SetTcpBbr2(bool enable)
    {
        if (enable)
            Run("netsh", "int tcp set supplemental Template=Internet CongestionProvider=bbr2");
        else
            Run("netsh", "int tcp set supplemental Template=Internet CongestionProvider=cubic");
    }

    private static (bool MemoryCompression, bool ApplicationPreLaunch, bool PageCombining)? _mmAgentCache;
    private static DateTime _mmAgentCacheUtc;

    private static bool IsMmAgentOn(string name)
    {
        EnsureMmAgentCache();
        var c = _mmAgentCache!.Value;
        return name switch
        {
            "MemoryCompression" => c.MemoryCompression,
            "ApplicationPreLaunch" => c.ApplicationPreLaunch,
            "PageCombining" => c.PageCombining,
            _ => true,
        };
    }

    private static void EnsureMmAgentCache()
    {
        if (_mmAgentCache is not null && (DateTime.UtcNow - _mmAgentCacheUtc).TotalSeconds < 45)
            return;

        try
        {
            var output = RunCapture("powershell.exe",
                "-NoProfile -Command \"$m=Get-MMAgent; '{0},{1},{2}' -f $m.MemoryCompression,$m.ApplicationPreLaunch,$m.PageCombining\"").Trim();
            var parts = output.Split(',');
            if (parts.Length >= 3)
            {
                _mmAgentCache = (
                    parts[0].IndexOf("True", StringComparison.OrdinalIgnoreCase) >= 0,
                    parts[1].IndexOf("True", StringComparison.OrdinalIgnoreCase) >= 0,
                    parts[2].IndexOf("True", StringComparison.OrdinalIgnoreCase) >= 0);
                _mmAgentCacheUtc = DateTime.UtcNow;
                return;
            }
        }
        catch { /* fall through */ }

        _mmAgentCache = (true, true, true);
        _mmAgentCacheUtc = DateTime.UtcNow;
    }

    private static void SetMmAgent(string name, bool enable)
    {
        var cmd = enable ? $"Enable-MMAgent -{name}" : $"Disable-MMAgent -{name}";
        if (name == "ApplicationPreLaunch")
            cmd = enable ? "Enable-MMAgent -ApplicationPreLaunch" : "Disable-MMAgent -ApplicationPreLaunch";
        Run("powershell.exe", "-NoProfile -Command " + cmd);
        _mmAgentCache = null;
    }

    private static bool ServiceDisabled(string name) =>
        DwordEquals(Hive.HkLm, $@"SYSTEM\CurrentControlSet\Services\{name}", "Start", 4);

    private static void SetService(string name, bool enable)
    {
        Run("sc.exe", enable ? $"config {name} start= auto" : $"config {name} start= disabled");
        Run("sc.exe", enable ? $"start {name}" : $"stop {name}");
    }

    private enum Hive { HkLm, HkCu }

    private static bool DwordEquals(Hive hive, string key, string name, int expected) =>
        GetDword(hive, key, name) == expected;

    private static int GetDword(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) switch
        {
            int i => i,
            _ => -1,
        };
    }

    private static void SetDword(Hive hive, string key, string name, int value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入：" + key);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key, true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void Run(string file, string args)
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
        p?.WaitForExit(30_000);
    }

    private static string RunCapture(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + file);
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(20_000);
        return output;
    }
}
