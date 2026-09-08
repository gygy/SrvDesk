using Microsoft.Win32;

namespace SrvDesk;

/// <summary>文件夹选项（查看）+ 全文件夹默认「分组依据 / 排序方式」。</summary>
internal static class FolderViewTweaks
{
    private const string Adv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string FolderTypes = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FolderTypes";
    private const string AllFoldersShell =
        @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell";
    private const string BagsRoot =
        @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags";
    private const string TopViewId = "{00000000-0000-0000-0000-000000000000}";

    private static readonly string[] FolderTypeGuids =
    {
        "{5c4f28b5-f869-4e84-8e60-f11db97c5cc7}", // Generic
        "{7d49d726-3c21-4f05-99aa-fdc2c9474656}", // Documents
        "{b3690e58-e961-423b-b687-386ebfd83239}", // Pictures
        "{94d6ddcc-4a68-4175-a374-bd584a510b78}", // Music
        "{5fa96489-d550-4084-a223-ebc58c59cbbb}", // Videos
        "{885a186e-a440-4ada-812b-db871b942259}", // Downloads
    };

    private static readonly string DownloadsGuid = FolderTypeGuids[5];

    public static string[] GroupByLabels =>
    [
        AppLang.L("不分组", "No grouping"),
        AppLang.L("按名称", "Name"),
        AppLang.L("按修改日期", "Date modified"),
        AppLang.L("按类型", "Type"),
        AppLang.L("按大小", "Size"),
    ];
    public static string[] SortByLabels =>
    [
        AppLang.L("名称升序", "Name A–Z"),
        AppLang.L("名称降序", "Name Z–A"),
        AppLang.L("日期新到旧", "Newest first"),
        AppLang.L("日期旧到新", "Oldest first"),
        AppLang.L("按类型", "Type"),
        AppLang.L("按大小", "Size"),
    ];
    public static string[] DriveLetterLabels =>
    [
        AppLang.L("卷标后面", "After label"),
        AppLang.L("卷标前面", "Before label"),
        AppLang.L("隐藏盘符", "Hide letter"),
    ];

    private static readonly string[] GroupByProps =
    {
        "System.Null",
        "System.ItemNameDisplay",
        "System.DateModified",
        "System.ItemType",
        "System.Size",
    };

    private static readonly string[] SortByLists =
    {
        "prop:System.ItemNameDisplay",
        "prop:-System.ItemNameDisplay",
        "prop:-System.DateModified",
        "prop:System.DateModified",
        "prop:System.ItemType",
        "prop:-System.Size",
    };

    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Di(Func<Optimizer.State, int> f) => baseline is null || f(baseline) != f(s);
        bool Take(string field, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return false;
            ApplyLog.DebugField(field, baseline is null ? null : f(baseline), f(s));
            return true;
        }

        if (Take("AlwaysShowMenus", x => x.AlwaysShowMenus))
            SetAdv("AlwaysShowMenus", s.AlwaysShowMenus ? 1 : 0);
        if (Take("HideMergeConflicts", x => x.HideMergeConflicts))
            SetAdv("HideMergeConflicts", s.HideMergeConflicts ? 1 : 0);
        if (Take("ShowCompColor", x => x.ShowCompColor))
            SetAdv("ShowCompColor", s.ShowCompColor ? 1 : 0);
        if (Take("ShowInfoTip", x => x.ShowInfoTip))
            SetAdv("ShowInfoTip", s.ShowInfoTip ? 1 : 0);
        if (Take("ShowStatusBar", x => x.ShowStatusBar))
            SetAdv("ShowStatusBar", s.ShowStatusBar ? 1 : 0);
        if (Take("DisablePersistBrowsers", x => x.DisablePersistBrowsers))
            SetAdv("PersistBrowsers", s.DisablePersistBrowsers ? 0 : 1);
        if (Take("NavPaneExpandCurrent", x => x.NavPaneExpandCurrent))
            SetAdv("NavPaneExpandToCurrentFolder", s.NavPaneExpandCurrent ? 1 : 0);
        if (Take("DisableSharingWizard", x => x.DisableSharingWizard))
            SetAdv("SharingWizardOn", s.DisableSharingWizard ? 0 : 1);
        if (Di(x => x.ShowDriveLettersMode))
        {
            ApplyLog.DebugField("ShowDriveLettersMode", baseline?.ShowDriveLettersMode, s.ShowDriveLettersMode);
            SetAdv("ShowDriveLettersFirst", Clamp(s.ShowDriveLettersMode, 0, 2));
        }

        var viewChanged = Di(x => x.FolderGroupByMode) || Di(x => x.FolderSortByMode);
        if (viewChanged)
        {
            ApplyLog.DebugField("FolderGroupByMode", baseline?.FolderGroupByMode, s.FolderGroupByMode);
            ApplyLog.DebugField("FolderSortByMode", baseline?.FolderSortByMode, s.FolderSortByMode);
            ApplyFolderView(Clamp(s.FolderGroupByMode, 0, 4), Clamp(s.FolderSortByMode, 0, 5));
        }

        if (viewChanged || D(x => x.AlwaysShowMenus) || D(x => x.NavPaneExpandCurrent)
            || D(x => x.ShowStatusBar) || Di(x => x.ShowDriveLettersMode))
            DesktopQuickActions.RestartExplorer();
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.AlwaysShowMenus = DwordEquals("AlwaysShowMenus", 1);
        s.HideMergeConflicts = DwordEquals("HideMergeConflicts", 1);
        s.ShowCompColor = DwordEquals("ShowCompColor", 1);
        s.ShowInfoTip = !DwordEquals("ShowInfoTip", 0);
        s.ShowStatusBar = !DwordEquals("ShowStatusBar", 0);
        s.DisablePersistBrowsers = !DwordEquals("PersistBrowsers", 1);
        s.NavPaneExpandCurrent = DwordEquals("NavPaneExpandToCurrentFolder", 1);
        s.DisableSharingWizard = DwordEquals("SharingWizardOn", 0);
        s.ShowDriveLettersMode = GetAdv("ShowDriveLettersFirst", 0);
        if (s.ShowDriveLettersMode is < 0 or > 2) s.ShowDriveLettersMode = 0;
        s.FolderGroupByMode = ReadGroupBy();
        s.FolderSortByMode = ReadSortBy();
    }

    public static bool AnyChanged(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.AlwaysShowMenus != s.AlwaysShowMenus
            || b.HideMergeConflicts != s.HideMergeConflicts
            || b.ShowCompColor != s.ShowCompColor
            || b.ShowInfoTip != s.ShowInfoTip
            || b.ShowStatusBar != s.ShowStatusBar
            || b.DisablePersistBrowsers != s.DisablePersistBrowsers
            || b.NavPaneExpandCurrent != s.NavPaneExpandCurrent
            || b.DisableSharingWizard != s.DisableSharingWizard
            || b.ShowDriveLettersMode != s.ShowDriveLettersMode
            || b.FolderGroupByMode != s.FolderGroupByMode
            || b.FolderSortByMode != s.FolderSortByMode;
    }

    public static void SetAdv(string name, int value)
    {
        object? old;
        using (var r = Registry.CurrentUser.OpenSubKey(Adv))
            old = r?.GetValue(name);
        ApplyLog.RegistryDword("HKCU", Adv, name, old, value);
        using var k = Registry.CurrentUser.CreateSubKey(Adv);
        k?.SetValue(name, value, RegistryValueKind.DWord);
    }

    public static int GetAdv(string name, int fallback)
    {
        using var k = Registry.CurrentUser.OpenSubKey(Adv);
        return k?.GetValue(name) is int i ? i : fallback;
    }

    public static void ApplyGroupBy(int mode) =>
        ApplyFolderView(Clamp(mode, 0, 4), ReadSortBy());

    public static void ApplySortBy(int mode) =>
        ApplyFolderView(ReadGroupBy(), Clamp(mode, 0, 5));

    public static void ApplyFolderView(int groupMode, int sortMode)
    {
        var group = GroupByProps[Clamp(groupMode, 0, 4)];
        var sort = SortByLists[Clamp(sortMode, 0, 5)];
        foreach (var guid in FolderTypeGuids)
            WriteTopView(guid, group, sort);

        SetDwordKey(AllFoldersShell, "GroupView", groupMode == 0 ? 0 : 1);
        PatchExistingBags(groupMode == 0 ? 0 : 1);
        DesktopQuickActions.NotifyShellChanged();
    }

    public static int ReadGroupBy()
    {
        var g = GetSz(TopViewPath(DownloadsGuid), "GroupBy");
        var mapped = IndexOfProp(GroupByProps, g);
        if (mapped >= 0) return mapped;
        return GetDwordKey(AllFoldersShell, "GroupView") == 0 ? 0 : 2;
    }

    public static int ReadSortBy()
    {
        var s = GetSz(TopViewPath(DownloadsGuid), "SortByList");
        var mapped = IndexOfProp(SortByLists, s);
        return mapped >= 0 ? mapped : 2;
    }

    private static void WriteTopView(string folderType, string groupBy, string sortBy)
    {
        var path = TopViewPath(folderType);
        SetSzKey(path, "GroupBy", groupBy);
        SetSzKey(path, "SortByList", sortBy);
        SetSzKey(path, "PrimaryProperty", "System.ItemNameDisplay");
        SetSzKey(path, "Name", "NoName");
        SetSzKey(path, "ColumnList", "System.Null");
        SetDwordKey(path, "LogicalViewMode", 1);
        SetDwordKey(path, "Order", 0);
    }

    private static void PatchExistingBags(int groupView)
    {
        using var root = Registry.CurrentUser.OpenSubKey(BagsRoot, writable: true);
        if (root is null) return;
        foreach (var name in root.GetSubKeyNames())
        {
            if (name.Equals("AllFolders", StringComparison.OrdinalIgnoreCase)) continue;
            using var bag = root.OpenSubKey(name + @"\Shell", writable: true);
            if (bag is null) continue;
            try { bag.SetValue("GroupView", groupView, RegistryValueKind.DWord); }
            catch { /* 个别包只读 */ }
            foreach (var child in bag.GetSubKeyNames())
            {
                using var sub = bag.OpenSubKey(child, writable: true);
                try { sub?.SetValue("GroupView", groupView, RegistryValueKind.DWord); }
                catch { /* ignore */ }
            }
        }
    }

    private static string TopViewPath(string guid) =>
        FolderTypes + @"\" + guid + @"\TopViews\" + TopViewId;

    private static int IndexOfProp(string[] list, string? value)
    {
        if (string.IsNullOrEmpty(value)) return -1;
        for (var i = 0; i < list.Length; i++)
        {
            if (string.Equals(list[i], value, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static bool DwordEquals(string name, int expected) => GetAdv(name, -1) == expected;

    private static int GetDwordKey(string key, string name)
    {
        using var k = Registry.CurrentUser.OpenSubKey(key);
        return k?.GetValue(name) is int i ? i : -1;
    }

    private static string? GetSz(string key, string name)
    {
        using var k = Registry.CurrentUser.OpenSubKey(key);
        return k?.GetValue(name) as string;
    }

    private static void SetDwordKey(string key, string name, int value)
    {
        object? old;
        using (var r = Registry.CurrentUser.OpenSubKey(key))
            old = r?.GetValue(name);
        ApplyLog.RegistryDword("HKCU", key, name, old, value);
        using var k = Registry.CurrentUser.CreateSubKey(key);
        k?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetSzKey(string key, string name, string value)
    {
        object? old;
        using (var r = Registry.CurrentUser.OpenSubKey(key))
            old = r?.GetValue(name);
        ApplyLog.RegistryString("HKCU", key, name, old, value);
        using var k = Registry.CurrentUser.CreateSubKey(key);
        k?.SetValue(name, value, RegistryValueKind.String);
    }

    private static int Clamp(int value, int min, int max) =>
        value < min ? min : (value > max ? max : value);
}
