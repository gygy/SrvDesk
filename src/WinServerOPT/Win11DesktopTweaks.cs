using System.Diagnostics;
using Microsoft.Win32;

namespace WinOpt;

/// <summary>借鉴 Win11 桌面优化工具的资源管理器、任务栏与系统体验项（注册表实现）。</summary>
internal static class Win11DesktopTweaks
{
    private const string ExplorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ShellIcons = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons";
    private const string ClassicMenuClsid = @"Software\Classes\CLSID\{86ca1aa0-3389-4ff8-b098-4136676466e2}\InprocServer32";
    private const string StuckRects = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3";

    public static void Apply(Optimizer.State s)
    {
        SetDword(Hive.HkCu, ExplorerAdvanced, "AutoCheckSelect", s.ShowItemCheckboxes ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdvanced, "NavPaneShowAllFolders", s.ShowCommonFolders ? 1 : 0);
        SetShellIconBlank(77, s.RemoveAdminShield);
        SetShortcutSuffixOff(s.NoShortcutSuffix);
        SetDword(Hive.HkCu, ExplorerAdvanced, "UseCompactMode", s.Win11ExplorerStyle ? 0 : 1);
        SetClassicContextMenu(s.Win10ClassicContextMenu);
        SetDword(Hive.HkCu, ExplorerAdvanced, "SearchboxTaskbarMode", s.TaskbarSearchBox ? 2 : 1);
        SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarAl", s.TaskbarAlignLeft ? 0 : 1);
        SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarGlomLevel", s.TaskbarCombineAlways ? 0 : 2);
        SetTaskbarAutoHide(s.TaskbarAutoHide);
        SetDword(Hive.HkCu, ExplorerAdvanced, "ShowTaskViewButton", s.ShowTaskViewButton ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdvanced, "EndTask", s.TaskbarEndTask ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarDa", s.DisableWidgets ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", s.DisableSearchHighlights ? 0 : 1);
        SetDword(Hive.HkCu, ExplorerAdvanced, "Start_ShowRecentRecommendations", s.DisableRecommendedItems ? 0 : 1);
        SetDword(Hive.HkCu, ExplorerAdvanced, "Start_TrackDocs", s.DisableRecommendedItems ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", s.DisableAdTracking ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "HistoryViewEnabled", s.DisableSearchHistory ? 0 : 1);
        SetString(Hive.HkCu, @"Control Panel\Accessibility\StickyKeys", "Flags", s.DisableStickyKeys ? "506" : "510");
        SetFeatureUpdatePause2035(s.PauseFeatureUpdatesUntil2035);
    }

    public static bool IsShowItemCheckboxesOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "AutoCheckSelect", 1);

    public static bool IsShowCommonFoldersOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "NavPaneShowAllFolders", 1);

    public static bool IsRemoveAdminShieldOn() => IsShellIconBlank(77);

    public static bool IsNoShortcutSuffixOn() => IsShortcutSuffixOff();

    public static bool IsWin11ExplorerStyleOn() =>
        !DwordEquals(Hive.HkCu, ExplorerAdvanced, "UseCompactMode", 1);

    public static bool IsWin10ClassicContextMenuOn() => IsClassicContextMenuOn();

    public static bool IsTaskbarSearchBoxOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "SearchboxTaskbarMode", 2);

    public static bool IsTaskbarAlignLeftOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "TaskbarAl", 0);

    public static bool IsTaskbarCombineAlwaysOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "TaskbarGlomLevel", 0);

    public static bool IsTaskbarAutoHideOn() => ReadTaskbarAutoHide();

    public static bool IsShowTaskViewButtonOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "ShowTaskViewButton", 1);

    public static bool IsTaskbarEndTaskOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "EndTask", 1);

    public static bool IsDisableWidgetsOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "TaskbarDa", 0);

    public static bool IsDisableSearchHighlightsOn() =>
        DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0);

    public static bool IsDisableRecommendedItemsOn() =>
        DwordEquals(Hive.HkCu, ExplorerAdvanced, "Start_ShowRecentRecommendations", 0);

    public static bool IsDisableAdTrackingOn() =>
        DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0);

    public static bool IsDisableSearchHistoryOn() =>
        DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "HistoryViewEnabled", 0);

    public static bool IsDisableStickyKeysOn() =>
        GetString(Hive.HkCu, @"Control Panel\Accessibility\StickyKeys", "Flags") == "506";

    public static bool IsPauseFeatureUpdatesUntil2035On() => IsFeatureUpdatePausedUntil2035();

    private static bool IsShellIconBlank(int index)
    {
        var val = GetValue(Hive.HkLm, ShellIcons, index.ToString()) as string;
        return val is not null && val.Length == 0;
    }

    private static void SetShellIconBlank(int index, bool blank)
    {
        if (blank)
            SetString(Hive.HkLm, ShellIcons, index.ToString(), "");
        else
            DeleteValue(Hive.HkLm, ShellIcons, index.ToString());
    }

    private static bool IsShortcutSuffixOff()
    {
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer");
        var link = k?.GetValue("Link") as byte[];
        return link is { Length: 4 } && link[0] == 0 && link[1] == 0 && link[2] == 0 && link[3] == 0;
    }

    private static void SetShortcutSuffixOff(bool disable)
    {
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer", writable: true);
        if (k is null) return;
        if (disable)
            k.SetValue("Link", new byte[] { 0, 0, 0, 0 }, RegistryValueKind.Binary);
        else
            k.DeleteValue("Link", throwOnMissingValue: false);
    }

    private static bool IsClassicContextMenuOn()
    {
        using var k = Registry.CurrentUser.OpenSubKey(ClassicMenuClsid);
        return (k?.GetValue(null) as string)?.Length == 0;
    }

    private static void SetClassicContextMenu(bool classic)
    {
        if (classic)
        {
            using var k = Registry.CurrentUser.CreateSubKey(ClassicMenuClsid);
            k?.SetValue(null, "", RegistryValueKind.String);
        }
        else
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(ClassicMenuClsid, throwOnMissingSubKey: false); }
            catch { /* ignore */ }
        }
    }

    private static bool ReadTaskbarAutoHide()
    {
        using var k = Registry.CurrentUser.OpenSubKey(StuckRects);
        if (k?.GetValue("Settings") is not byte[] settings || settings.Length < 9)
            return false;
        return settings[8] == 2;
    }

    private static void SetTaskbarAutoHide(bool hide)
    {
        using var k = Registry.CurrentUser.OpenSubKey(StuckRects, writable: true);
        if (k?.GetValue("Settings") is not byte[] settings || settings.Length < 9)
            return;
        settings[8] = (byte)(hide ? 2 : 3);
        k.SetValue("Settings", settings, RegistryValueKind.Binary);
    }

    private static bool IsFeatureUpdatePausedUntil2035()
    {
        if (!DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "PauseFeatureUpdates", 1))
            return false;
        var end = GetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "PauseFeatureUpdatesEndTime");
        return end >= 2051222400;
    }

    private static void SetFeatureUpdatePause2035(bool pause)
    {
        const string key = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
        if (pause)
        {
            SetDword(Hive.HkLm, key, "PauseFeatureUpdates", 1);
            SetDword(Hive.HkLm, key, "PauseFeatureUpdatesStartTime", (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            SetDword(Hive.HkLm, key, "PauseFeatureUpdatesEndTime", 2051222400);
        }
        else
        {
            DeleteValue(Hive.HkLm, key, "PauseFeatureUpdates");
            DeleteValue(Hive.HkLm, key, "PauseFeatureUpdatesStartTime");
            DeleteValue(Hive.HkLm, key, "PauseFeatureUpdatesEndTime");
        }
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
            byte b => b,
            _ => -1,
        };
    }

    private static string? GetString(Hive hive, string subKey, string name) =>
        GetValue(hive, subKey, name) as string;

    private static object? GetValue(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name);
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
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key, true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }
}

internal static class DesktopQuickActions
{
    public static void RestartExplorer()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("explorer"))
            {
                proc.Kill();
                proc.WaitForExit(5000);
            }
        }
        catch { /* ignore */ }

        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
            UseShellExecute = true,
        });
        ApplyLog.Write("重启资源管理器");
    }

    public static void RefreshIconCache(IWin32Window? owner)
    {
        var ie4u = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "ie4uinit.exe");
        if (!File.Exists(ie4u))
        {
            MessageBox.Show(owner, "未找到 ie4uinit.exe。", "刷新图标缓存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Process.Start(new ProcessStartInfo { FileName = ie4u, Arguments = "-show", UseShellExecute = false, CreateNoWindow = true });
        ApplyLog.Write("刷新图标缓存");
    }

    public static void EmptyRecycleBin(IWin32Window? owner)
    {
        try
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            ApplyLog.Write("清空回收站");
            MessageBox.Show(owner, "回收站已清空。", "清空回收站", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, ex.Message, "清空回收站", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public static void OpenPerformanceOptions(IWin32Window? owner) =>
        Launch(owner, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SystemPropertiesPerformance.exe"), "性能选项");

    public static void OpenDesktopIconSettings(IWin32Window? owner)
    {
        var rundll = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "rundll32.exe");
        Launch(owner, rundll, "桌面图标设置", "shell32.dll,Control_RunDLL desk.cpl,,0");
    }

    public static void OpenControlPanel(IWin32Window? owner) =>
        Launch(owner, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "control.exe"), "控制面板");

    public static void OpenDiskManagement(IWin32Window? owner) =>
        Launch(owner, "diskmgmt.msc", "磁盘管理");

    public static void OpenDeviceManager(IWin32Window? owner) =>
        Launch(owner, "devmgmt.msc", "设备管理器");

    private static void Launch(IWin32Window? owner, string file, string title, string args = "")
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = true,
            });
            ApplyLog.Write("打开" + title);
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private const int SHERB_NOCONFIRMATION = 0x00000001;
    private const int SHERB_NOPROGRESSUI = 0x00000002;
    private const int SHERB_NOSOUND = 0x00000004;

    [System.Runtime.InteropServices.DllImport("Shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, int dwFlags);
}
