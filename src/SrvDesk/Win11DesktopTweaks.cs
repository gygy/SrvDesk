using System.Diagnostics;
using Microsoft.Win32;

namespace SrvDesk;

/// <summary>借鉴 Win11 桌面优化工具的资源管理器、任务栏与系统体验项（注册表实现）。</summary>
internal static class Win11DesktopTweaks
{
    private const string ExplorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ShellIcons = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons";
    /// <summary>透明空图标（与常见「删除快捷方式箭头.reg」一致），避免自建 blank.ico 失效。</summary>
    private const string BlankOverlayIcon = @"%systemroot%\system32\imageres.dll,197";
    private const string ClassicMenuClsid = @"Software\Classes\CLSID\{86ca1aa0-3389-4ff8-b098-4136676466e2}\InprocServer32";
    private const string StuckRects3 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3";
    private const string StuckRects2 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects2";
    /// <summary>与 SHAppBarMessage / APPBARDATA 一致：bit0=自动隐藏，bit1=总在最前。</summary>
    private const byte AbsAutoHide = 0x01;
    private const byte AbsAlwaysOnTop = 0x02;
    private const uint AbmGetState = 0x00000004;
    private const uint AbmSetState = 0x0000000A;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Di(Func<Optimizer.State, int> f) => baseline is null || f(baseline) != f(s);
        void Field(string name, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return;
            ApplyLog.DebugField(name, baseline is null ? null : f(baseline), f(s));
        }

        void FieldI(string name, Func<Optimizer.State, int> f)
        {
            if (!Di(f)) return;
            ApplyLog.DebugField(name, baseline is null ? null : f(baseline), f(s));
        }

        if (D(x => x.ShowItemCheckboxes))
        {
            Field("ShowItemCheckboxes", x => x.ShowItemCheckboxes);
            SetDword(Hive.HkCu, ExplorerAdvanced, "AutoCheckSelect", s.ShowItemCheckboxes ? 1 : 0);
        }
        if (D(x => x.ShowCommonFolders))
        {
            Field("ShowCommonFolders", x => x.ShowCommonFolders);
            SetDword(Hive.HkCu, ExplorerAdvanced, "NavPaneShowAllFolders", s.ShowCommonFolders ? 1 : 0);
        }
        if (D(x => x.RemoveAdminShield))
        {
            Field("RemoveAdminShield", x => x.RemoveAdminShield);
            SetShellIconBlank(77, s.RemoveAdminShield);
        }
        if (D(x => x.NoShortcutSuffix))
        {
            Field("NoShortcutSuffix", x => x.NoShortcutSuffix);
            SetShortcutSuffixOff(s.NoShortcutSuffix);
        }
        if (D(x => x.Win11ExplorerStyle))
        {
            Field("Win11ExplorerStyle", x => x.Win11ExplorerStyle);
            SetDword(Hive.HkCu, ExplorerAdvanced, "UseCompactMode", s.Win11ExplorerStyle ? 0 : 1);
        }
        if (D(x => x.Win10ClassicContextMenu))
        {
            Field("Win10ClassicContextMenu", x => x.Win10ClassicContextMenu);
            SetClassicContextMenu(s.Win10ClassicContextMenu);
        }
        if (Di(x => x.TaskbarSearchMode) || D(x => x.TaskbarSearchBox))
        {
            FieldI("TaskbarSearchMode", x => x.TaskbarSearchMode);
            Field("TaskbarSearchBox", x => x.TaskbarSearchBox);
            SetTaskbarSearchMode(
                s.TaskbarSearchMode is 0 or 1 or 2 ? s.TaskbarSearchMode : (s.TaskbarSearchBox ? 2 : 1));
        }
        if (D(x => x.TaskbarAlignLeft))
        {
            Field("TaskbarAlignLeft", x => x.TaskbarAlignLeft);
            SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarAl", s.TaskbarAlignLeft ? 0 : 1);
        }
        if (D(x => x.TaskbarCombineAlways))
        {
            Field("TaskbarCombineAlways", x => x.TaskbarCombineAlways);
            SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarGlomLevel", s.TaskbarCombineAlways ? 0 : 2);
        }
        if (D(x => x.TaskbarAutoHide))
        {
            Field("TaskbarAutoHide", x => x.TaskbarAutoHide);
            SetTaskbarAutoHide(s.TaskbarAutoHide);
        }
        if (D(x => x.ShowTaskViewButton))
        {
            Field("ShowTaskViewButton", x => x.ShowTaskViewButton);
            SetDword(Hive.HkCu, ExplorerAdvanced, "ShowTaskViewButton", s.ShowTaskViewButton ? 1 : 0);
        }
        if (D(x => x.TaskbarEndTask))
        {
            Field("TaskbarEndTask", x => x.TaskbarEndTask);
            SetDword(Hive.HkCu, ExplorerAdvanced, "EndTask", s.TaskbarEndTask ? 1 : 0);
        }
        if (D(x => x.DisableWidgets))
        {
            Field("DisableWidgets", x => x.DisableWidgets);
            SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarDa", s.DisableWidgets ? 0 : 1);
        }
        if (D(x => x.DisableSearchHighlights))
        {
            Field("DisableSearchHighlights", x => x.DisableSearchHighlights);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", s.DisableSearchHighlights ? 0 : 1);
        }
        if (D(x => x.DisableRecommendedItems))
        {
            Field("DisableRecommendedItems", x => x.DisableRecommendedItems);
            SetDword(Hive.HkCu, ExplorerAdvanced, "Start_ShowRecentRecommendations", s.DisableRecommendedItems ? 0 : 1);
        }
        if (D(x => x.DisableAdTracking))
        {
            Field("DisableAdTracking", x => x.DisableAdTracking);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", s.DisableAdTracking ? 0 : 1);
        }
        if (D(x => x.DisableSearchHistory))
        {
            Field("DisableSearchHistory", x => x.DisableSearchHistory);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "HistoryViewEnabled", s.DisableSearchHistory ? 0 : 1);
        }
        if (D(x => x.DisableStickyKeys))
        {
            Field("DisableStickyKeys", x => x.DisableStickyKeys);
            // StickyKeys=506 保留本软件既有语义；并写入 FilterKeys/ToggleKeys，对齐 Duck DisableAccessibilityKeyboardHotkeys
            SetString(Hive.HkCu, @"Control Panel\Accessibility\StickyKeys", "Flags",
                s.DisableStickyKeys ? "506" : "510");
            SetString(Hive.HkCu, @"Control Panel\Accessibility\Keyboard Response", "Flags",
                s.DisableStickyKeys ? "2" : "126");
            SetString(Hive.HkCu, @"Control Panel\Accessibility\ToggleKeys", "Flags",
                s.DisableStickyKeys ? "34" : "62");
        }
    }

    /// <summary>仅任务栏 / 资源管理器界面相关变更才需要重启 explorer；隐私类开关不必。</summary>
    public static bool NeedsExplorerRestart(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.ShowItemCheckboxes != s.ShowItemCheckboxes
            || b.ShowCommonFolders != s.ShowCommonFolders
            || b.RemoveAdminShield != s.RemoveAdminShield
            || b.NoShortcutSuffix != s.NoShortcutSuffix
            || b.Win11ExplorerStyle != s.Win11ExplorerStyle
            || b.Win10ClassicContextMenu != s.Win10ClassicContextMenu
            || b.TaskbarSearchBox != s.TaskbarSearchBox
            || b.TaskbarSearchMode != s.TaskbarSearchMode
            || b.TaskbarAlignLeft != s.TaskbarAlignLeft
            || b.TaskbarCombineAlways != s.TaskbarCombineAlways
            || b.TaskbarAutoHide != s.TaskbarAutoHide
            || b.ShowTaskViewButton != s.ShowTaskViewButton
            || b.TaskbarEndTask != s.TaskbarEndTask
            || b.DisableWidgets != s.DisableWidgets
            || b.DisableSearchHighlights != s.DisableSearchHighlights
            || b.DisableSearchBoxSuggestions != s.DisableSearchBoxSuggestions
            || b.DisableRecommendedItems != s.DisableRecommendedItems;
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

    public static bool IsTaskbarSearchBoxOn() => GetTaskbarSearchMode() == 2;

    /// <summary>
    /// 0=隐藏 1=仅图标 2=搜索框。
    /// 必须同时写 Search + Advanced，并设置 SearchboxTaskbarModeCache，
    /// 否则 Taskbar.dll 会在资源管理器启动时把模式“迁移”回默认搜索框。
    /// </summary>
    public static int GetTaskbarSearchMode()
    {
        const string searchKey = @"Software\Microsoft\Windows\CurrentVersion\Search";
        var search = GetDword(Hive.HkCu, searchKey, "SearchboxTaskbarMode");
        if (search is 0 or 1 or 2) return search;
        var adv = GetDword(Hive.HkCu, ExplorerAdvanced, "SearchboxTaskbarMode");
        return adv is 0 or 1 or 2 ? adv : -1;
    }

    public static void SetTaskbarSearchMode(int mode)
    {
        if (mode is not (0 or 1 or 2))
            mode = 1;

        const string searchKey = @"Software\Microsoft\Windows\CurrentVersion\Search";
        SetDword(Hive.HkCu, searchKey, "SearchboxTaskbarMode", mode);
        // Cache=1：标记为用户已设定，阻止开机/重启资源管理器时被重置为搜索框(2)
        SetDword(Hive.HkCu, searchKey, "SearchboxTaskbarModeCache", 1);
        SetDword(Hive.HkCu, ExplorerAdvanced, "SearchboxTaskbarMode", mode);

        // 可选策略加固（需管理员）；非隐藏时删除策略，避免把界面选项锁死
        const string policy = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search";
        try
        {
            if (mode == 0)
            {
                // 0=隐藏 1=仅图标 2=图标+标签 3=搜索框
                SetDword(Hive.HkLm, policy, "ConfigureSearchOnTaskbarMode", 0);
            }
            else
            {
                DeleteValue(Hive.HkLm, policy, "ConfigureSearchOnTaskbarMode");
            }
        }
        catch
        {
            /* 无管理员权限时跳过策略 */
        }
    }

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

    public static bool IsPauseWindowsUpdatesUxOn() => IsWindowsUpdateUxPausedLong();

    public static void SetShortcutArrowHidden(bool hide)
    {
        if (hide)
        {
            // 对齐「删除快捷方式箭头.reg」
            SetString(Hive.HkLm, ShellIcons, "29", BlankOverlayIcon);
        }
        else
        {
            // 对齐「恢复快捷方式箭头.reg」：删除整个 Shell Icons 键
            DeleteKeyTree(Hive.HkLm, ShellIcons);
        }

        DesktopQuickActions.RestartExplorer();
    }

    public static bool IsShortcutArrowHidden() => IsShellIconBlank(29);

    public static void SetNoShortcutSuffix(bool on)
    {
        SetShortcutSuffixOff(on);
        // Link 旧写法会导致桌面图标空白，改完后重启资源管理器以立刻恢复
        DesktopQuickActions.RestartExplorer();
    }

    public static void SetRemoveAdminShield(bool on)
    {
        SetShellIconBlank(77, on);
        DesktopQuickActions.RestartExplorer();
    }
    public static void SetCompactExplorerSpacing(bool compactWin10) =>
        SetDword(Hive.HkCu, ExplorerAdvanced, "UseCompactMode", compactWin10 ? 1 : 0);
    public static void SetWin10ClassicContextMenu(bool classic) => SetClassicContextMenu(classic);
    public static void SetTaskbarAutoHideEnabled(bool hide) => SetTaskbarAutoHide(hide);
    public static void SetShowTaskViewButton(bool show) =>
        SetDword(Hive.HkCu, ExplorerAdvanced, "ShowTaskViewButton", show ? 1 : 0);
    public static void SetDisableWidgets(bool disable) =>
        SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarDa", disable ? 0 : 1);
    public static void SetTaskbarAlignLeft(bool left) =>
        SetDword(Hive.HkCu, ExplorerAdvanced, "TaskbarAl", left ? 0 : 1);

    private static bool IsShellIconBlank(int index)
    {
        var val = GetValue(Hive.HkLm, ShellIcons, index.ToString()) as string;
        if (val is null) return false;
        if (val.Length == 0) return true; // 旧空字符串写法
        if (val.IndexOf("imageres.dll", StringComparison.OrdinalIgnoreCase) >= 0 &&
            val.IndexOf(",197", StringComparison.Ordinal) >= 0)
            return true;
        // 兼容本工具旧版 blank.ico 方案
        return val.IndexOf("blank.ico", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetShellIconBlank(int index, bool blank)
    {
        if (blank)
        {
            // 对齐 Win11 常用优化：指向 imageres 透明图标，不要写空串或自建 ico
            SetString(Hive.HkLm, ShellIcons, index.ToString(), BlankOverlayIcon);
        }
        else
            DeleteValue(Hive.HkLm, ShellIcons, index.ToString());
    }
    private static bool IsShortcutSuffixOff()
    {
        using (var naming = Registry.CurrentUser.OpenSubKey(
                   @"Software\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates"))
        {
            var template = naming?.GetValue("ShortcutNameTemplate") as string ?? "";
            if (template.IndexOf("%s", StringComparison.Ordinal) >= 0)
                return true;
        }

        // 兼容旧检测（会在写入时清掉）
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer");
        var link = k?.GetValue("Link") as byte[];
        return link is { Length: 4 } && link[0] == 0 && link[1] == 0 && link[2] == 0 && link[3] == 0;
    }

    private static void SetShortcutSuffixOff(bool disable)
    {
        const string explorerKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
        const string namingKey = explorerKey + @"\NamingTemplates";

        // 旧方案 Link=00-00-00-00 在 Server/部分 Win10+ 会导致桌面图标全部空白，必须清除
        using (var ex = Registry.CurrentUser.OpenSubKey(explorerKey, writable: true))
        {
            if (ex?.GetValue("Link") is not null)
            {
                ApplyLog.RegistryDelete("HKCU", explorerKey, "Link", ex.GetValue("Link"));
                ex.DeleteValue("Link", throwOnMissingValue: false);
            }
        }

        if (disable)
        {
            object? old;
            using (var r = Registry.CurrentUser.OpenSubKey(namingKey))
                old = r?.GetValue("ShortcutNameTemplate");
            using var k = Registry.CurrentUser.CreateSubKey(namingKey);
            // "%s.lnk"：新建快捷方式只用目标名，不加「快捷方式」后缀
            const string template = "\"%s.lnk\"";
            ApplyLog.SystemChange($"HKCU\\{namingKey}\\ShortcutNameTemplate",
                "去掉「快捷方式」后缀（安全写法）",
                ApplyLog.FormatValue(old), template);
            k?.SetValue("ShortcutNameTemplate", template, RegistryValueKind.String);
        }
        else
        {
            object? old;
            using (var r = Registry.CurrentUser.OpenSubKey(namingKey))
                old = r?.GetValue("ShortcutNameTemplate");
            ApplyLog.RegistryDelete("HKCU", namingKey, "ShortcutNameTemplate", old);
            using var k = Registry.CurrentUser.OpenSubKey(namingKey, writable: true);
            k?.DeleteValue("ShortcutNameTemplate", throwOnMissingValue: false);
        }
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
            ApplyLog.RegistryKeyWrite("HKCU", ClassicMenuClsid, "创建键（默认值空字符串）启用经典右键菜单");
            using var k = Registry.CurrentUser.CreateSubKey(ClassicMenuClsid);
            k?.SetValue(null, "", RegistryValueKind.String);
        }
        else
        {
            var existed = Registry.CurrentUser.OpenSubKey(ClassicMenuClsid) is not null;
            ApplyLog.RegistryDeleteTree("HKCU", ClassicMenuClsid, existed);
            try { Registry.CurrentUser.DeleteSubKeyTree(ClassicMenuClsid, throwOnMissingSubKey: false); }
            catch { /* ignore */ }
        }
    }

    private static bool ReadTaskbarAutoHide()
    {
        try
        {
            var abd = NewAppBarData();
            var state = (int)SHAppBarMessage(AbmGetState, ref abd);
            return (state & AbsAutoHide) != 0;
        }
        catch
        {
            /* 回退注册表 */
        }

        foreach (var key in new[] { StuckRects3, StuckRects2 })
        {
            using var k = Registry.CurrentUser.OpenSubKey(key);
            if (k?.GetValue("Settings") is byte[] settings && settings.Length >= 9)
                return (settings[8] & AbsAutoHide) != 0;
        }

        return false;
    }

    private static void SetTaskbarAutoHide(bool hide)
    {
        WriteStuckRectsAutoHide(StuckRects3, hide);
        WriteStuckRectsAutoHide(StuckRects2, hide);

        try
        {
            var abd = NewAppBarData();
            var current = (int)SHAppBarMessage(AbmGetState, ref abd);
            var next = hide
                ? (current | AbsAutoHide | AbsAlwaysOnTop)
                : ((current | AbsAlwaysOnTop) & ~AbsAutoHide);
            abd.lParam = (IntPtr)next;
            SHAppBarMessage(AbmSetState, ref abd);
            ApplyLog.SystemChange(
                "SHAppBarMessage(ABM_SETSTATE)",
                "任务栏自动隐藏（立即通知外壳）",
                current.ToString(),
                next.ToString());
        }
        catch (Exception ex)
        {
            ApplyLog.Debug("SHAppBarMessage 设置任务栏自动隐藏失败：" + ex.Message);
        }
    }

    private static void WriteStuckRectsAutoHide(string keyPath, bool hide)
    {
        using var k = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (k?.GetValue("Settings") is not byte[] settings || settings.Length < 9)
            return;

        var oldByte = settings[8];
        var newByte = hide
            ? (byte)(oldByte | AbsAutoHide | AbsAlwaysOnTop)
            : (byte)((oldByte | AbsAlwaysOnTop) & ~AbsAutoHide);
        if (oldByte == newByte)
            return;

        ApplyLog.SystemChange(
            $"HKCU\\{keyPath}\\Settings[8]",
            "任务栏自动隐藏（StuckRects 第 9 字节 bit0）",
            "0x" + oldByte.ToString("X2"),
            "0x" + newByte.ToString("X2"));
        settings[8] = newByte;
        k.SetValue("Settings", settings, RegistryValueKind.Binary);
    }

    private static APPBARDATA NewAppBarData()
    {
        var abd = new APPBARDATA();
        abd.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(APPBARDATA));
        return abd;
    }

    public static void SetFeatureUpdatePause(bool pause) => SetFeatureUpdatePause2035(pause);

    public static void SetWindowsUpdateUxPause(bool pause)
    {
        const string key = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        // 与常见「延迟更新至 2099」.reg 对齐：暂停功能更新 + 质量更新
        const string start = "1990-11-22T15:09:05Z";
        const string endFeature = "2099-05-28T11:11:11Z";
        const string endExpiry = "2099-05-28T16:38:59Z";
        if (pause)
        {
            SetString(Hive.HkLm, key, "PauseUpdatesStartTime", start);
            SetString(Hive.HkLm, key, "PauseFeatureUpdatesStartTime", start);
            SetString(Hive.HkLm, key, "PauseQualityUpdatesStartTime", start);
            SetString(Hive.HkLm, key, "PauseFeatureUpdatesEndTime", endFeature);
            SetString(Hive.HkLm, key, "PauseQualityUpdatesEndTime", endFeature);
            SetString(Hive.HkLm, key, "PauseUpdatesExpiryTime", endExpiry);
        }
        else
        {
            DeleteValue(Hive.HkLm, key, "PauseUpdatesStartTime");
            DeleteValue(Hive.HkLm, key, "PauseFeatureUpdatesStartTime");
            DeleteValue(Hive.HkLm, key, "PauseQualityUpdatesStartTime");
            DeleteValue(Hive.HkLm, key, "PauseFeatureUpdatesEndTime");
            DeleteValue(Hive.HkLm, key, "PauseQualityUpdatesEndTime");
            DeleteValue(Hive.HkLm, key, "PauseUpdatesExpiryTime");
        }
    }

    private static bool IsFeatureUpdatePausedUntil2035()
    {
        if (!DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "PauseFeatureUpdates", 1))
            return false;
        var end = GetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "PauseFeatureUpdatesEndTime");
        return end >= 2051222400;
    }

    private static bool IsWindowsUpdateUxPausedLong()
    {
        const string key = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        if (TryParseUxUtc(GetString(Hive.HkLm, key, "PauseUpdatesExpiryTime"), out var expiry) && expiry.Year >= 2090)
            return true;
        if (TryParseUxUtc(GetString(Hive.HkLm, key, "PauseFeatureUpdatesEndTime"), out var featureEnd) && featureEnd.Year >= 2090)
            return true;
        if (TryParseUxUtc(GetString(Hive.HkLm, key, "PauseQualityUpdatesEndTime"), out var qualityEnd) && qualityEnd.Year >= 2090)
            return true;
        return false;
    }

    private static bool TryParseUxUtc(string? text, out DateTimeOffset dto)
    {
        dto = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return DateTimeOffset.TryParse(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out dto);
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
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDword(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old, value);
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(Hive hive, string key, string name, string value)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryString(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old, value);
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDelete(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old);
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key, true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void DeleteKeyTree(Hive hive, string key)
    {
        ApplyLog.RegistryDelete(hive == Hive.HkLm ? "HKLM" : "HKCU", key, "(key)", "(tree)");
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        try
        {
            baseKey.DeleteSubKeyTree(key, throwOnMissingSubKey: false);
        }
        catch
        {
            /* 键不存在或无权限 */
        }
    }
}

internal static class DesktopQuickActions
{
    private static readonly object Sync = new();
    private static int _deferDepth;
    private static bool _pendingRestart;
    private static DateTime _lastRestartUtc = DateTime.MinValue;

    /// <summary>
    /// 批量应用期间合并多次「重启资源管理器」请求，结束时最多执行一次，避免桌面反复抖动。
    /// </summary>
    public static IDisposable DeferRestarts() => new RestartDeferScope();

    /// <summary>请求重启资源管理器；若在 DeferRestarts 内则只记标志，出口时统一执行。</summary>
    public static void RestartExplorer()
    {
        lock (Sync)
        {
            if (_deferDepth > 0)
            {
                _pendingRestart = true;
                ApplyLog.Debug("资源管理器重启已延后（批量写入中）");
                return;
            }
        }

        RestartExplorerCore();
    }

    /// <summary>广播外壳变更（图标/策略），比杀进程更轻；任务栏类改动仍可能需 RestartExplorer。</summary>
    public static void NotifyShellChanged()
    {
        try
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            _ = SendMessageTimeout(
                new IntPtr(-1), WM_SETTINGCHANGE, IntPtr.Zero, "Policy",
                SMTO_ABORTIFHUNG, 1000, out _);
            _ = SendMessageTimeout(
                new IntPtr(-1), WM_SETTINGCHANGE, IntPtr.Zero, "TraySettings",
                SMTO_ABORTIFHUNG, 1000, out _);
            ApplyLog.Debug("已广播外壳刷新（SHChangeNotify / WM_SETTINGCHANGE）");
        }
        catch (Exception ex)
        {
            ApplyLog.Debug("外壳刷新失败：" + ex.Message);
        }
    }

    private static void FlushPendingRestart()
    {
        bool need;
        lock (Sync)
        {
            need = _pendingRestart;
            _pendingRestart = false;
        }

        if (!need) return;
        // 先轻量刷新，再必要时杀进程一次
        NotifyShellChanged();
        RestartExplorerCore();
    }

    private static void RestartExplorerCore()
    {
        lock (Sync)
        {
            // 短时间防抖：系统常会自动拉起 explorer，连杀会反复闪任务栏
            if ((DateTime.UtcNow - _lastRestartUtc).TotalSeconds < 2.5)
            {
                ApplyLog.Debug("跳过重复重启资源管理器（防抖）");
                return;
            }

            _lastRestartUtc = DateTime.UtcNow;
        }

        try
        {
            foreach (var proc in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(4000);
                }
                catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }

        // 等系统自动拉起外壳；已存在则不再 Start，避免双实例抖动
        for (var i = 0; i < 20; i++)
        {
            Thread.Sleep(100);
            if (Process.GetProcessesByName("explorer").Length > 0)
            {
                ApplyLog.Write("重启资源管理器（系统已自动拉起）");
                ApplyLog.Debug("explorer 已由系统恢复，跳过二次启动");
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                UseShellExecute = true,
            });
            ApplyLog.Write("重启资源管理器");
        }
        catch (Exception ex)
        {
            ApplyLog.Write("重启资源管理器失败：" + ex.Message);
        }
    }

    private sealed class RestartDeferScope : IDisposable
    {
        private bool _disposed;

        public RestartDeferScope()
        {
            lock (Sync) _deferDepth++;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            bool flush;
            lock (Sync)
            {
                _deferDepth = Math.Max(0, _deferDepth - 1);
                flush = _deferDepth == 0 && _pendingRestart;
            }

            if (flush)
                FlushPendingRestart();
        }
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

    public static void EmptyRecycleBin(IWin32Window? owner, bool notify = true)
    {
        try
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            ApplyLog.Write("清空回收站");
            if (notify)
                MessageBox.Show(owner, "回收站已清空。", "清空回收站", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            if (notify)
                MessageBox.Show(owner, ex.Message, "清空回收站", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                throw;
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
    private const uint SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;
    private const uint SHCNF_FLUSH = 0x1000;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int SMTO_ABORTIFHUNG = 0x0002;

    [System.Runtime.InteropServices.DllImport("Shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, int dwFlags);

    [System.Runtime.InteropServices.DllImport("Shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, int msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);
}
