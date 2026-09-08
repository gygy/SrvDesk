using Microsoft.Win32;

namespace SrvDesk;

/// <summary>资源管理器即时页：单列分区布局（与「系统服务」等页一致），避免多列 TableLayout 切换重排。</summary>
internal sealed class ExplorerSettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly InstantToggleRow _ext = new(AppLang.L("显示文件扩展名", "Show file extensions"));
    private readonly InstantToggleRow _fullPath = new(AppLang.L("标题栏显示完整路径", "Full path in title bar"));
    private readonly InstantToggleRow _hidden = new(AppLang.L("显示隐藏的文件和文件夹", "Show hidden files"));
    private readonly InstantToggleRow _osFiles = new(AppLang.L("隐藏受保护的系统文件", "Hide protected OS files"));
    private readonly InstantToggleRow _iconsOnly = new(AppLang.L("始终显示图标（无缩略图）", "Icons only (no thumbnails)"));
    private readonly InstantToggleRow _emptyDrives = new(AppLang.L("显示空驱动器", "Show empty drives"));
    private readonly InstantToggleRow _recent = new(AppLang.L("开始屏幕显示最近文件", "Show recent files"));
    private readonly InstantToggleRow _frequent = new(AppLang.L("显示常用文件夹", "Show frequent folders"));
    private readonly InstantToggleRow _office = new(AppLang.L("隐藏 office.com 云文件", "Hide office.com cloud files"));
    private readonly InstantToggleRow _arrow = new(AppLang.L("去掉快捷方式箭头", "Remove shortcut arrow"));
    private readonly InstantToggleRow _suffix = new(AppLang.L("快捷方式不加「快捷方式」后缀", "No \"Shortcut\" suffix"));
    private readonly InstantToggleRow _shield = new(AppLang.L("去掉管理员盾牌图标", "Remove admin shield icon"));
    private readonly InstantToggleRow _win10Explorer = new(AppLang.L("紧凑 / Win10 间距", "Compact / Win10 spacing"));
    private readonly InstantToggleRow _classicMenu = new(AppLang.L("Win10 经典右键菜单", "Win10 classic context menu"));
    private readonly InstantToggleRow _onedrive = new(AppLang.L("禁止 OneDrive", "Disable OneDrive"));
    private readonly InstantToggleRow _taskView = new(AppLang.L("显示任务视图按钮", "Show Task View button"));
    private readonly InstantToggleRow _chat = new(AppLang.L("隐藏任务栏聊天", "Hide taskbar chat"));
    private readonly InstantToggleRow _copilot = new(AppLang.L("隐藏任务栏 Copilot", "Hide taskbar Copilot"));
    private readonly InstantToggleRow _widgets = new(AppLang.L("关闭任务栏小组件", "Disable taskbar widgets"));
    private readonly InstantToggleRow _seconds = new(AppLang.L("托盘时钟显示秒", "Show seconds in tray clock"));
    private readonly ComboBox _launchTo = new();
    private readonly ComboBox _searchMode = new();
    private readonly ComboBox _align = new();
    private readonly ComboBox _glom = new();
    private readonly ComboBox _autohideMode = new();
    private readonly InstantToggleRow _alwaysMenu = new(AppLang.L("始终显示菜单栏", "Always show menu bar"));
    private readonly InstantToggleRow _hideMerge = new(AppLang.L("隐藏文件夹合并冲突", "Hide folder merge conflicts"));
    private readonly InstantToggleRow _compColor = new(AppLang.L("加密/压缩文件用颜色标识", "Color encrypted/compressed files"));
    private readonly InstantToggleRow _infoTip = new(AppLang.L("显示文件夹弹出说明", "Show folder info tips"));
    private readonly InstantToggleRow _statusBar = new(AppLang.L("显示状态栏", "Show status bar"));
    private readonly InstantToggleRow _noPersist = new(AppLang.L("登录时不还原上次文件夹窗口", "Don't restore folder windows at logon"));
    private readonly InstantToggleRow _navExpand = new(AppLang.L("导航窗格展开到当前文件夹", "Expand nav pane to current folder"));
    private readonly InstantToggleRow _noShareWiz = new(AppLang.L("不使用共享向导", "Don't use sharing wizard"));
    private readonly ComboBox _driveLetters = new();
    private readonly ComboBox _folderGroup = new();
    private readonly ComboBox _folderSort = new();
    private bool _loading;
    private bool _loaded;
    private bool _warmLoadSkip;

    public ExplorerSettingsDialog()
    {
        Text = AppLang.L("资源管理器", "File Explorer");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 640);
        MinimumSize = new Size(720, 480);

        var body = ThemedSettingsChrome.CreateBodyPanel();

        // 分区：常用 → 快速访问 → 快捷方式/云 → 任务栏 → 工具
        var folderOpts = ThemedSettingsChrome.CreateSection("文件夹选项", [
            _alwaysMenu, _hideMerge, _compColor, _infoTip, _statusBar,
            _noPersist, _navExpand, _noShareWiz,
            BuildDriveLetterRow(), BuildGroupByRow(), BuildSortByRow(),
        ]);
        var common = ThemedSettingsChrome.CreateSection("常用显示", [
            _ext, _hidden, _fullPath, _osFiles, BuildLaunchRow(),
        ]);
        var quickAccess = ThemedSettingsChrome.CreateSection("快速访问", [
            _recent, _frequent, _office, _emptyDrives, _iconsOnly,
        ]);
        var shortcuts = ThemedSettingsChrome.CreateSection("快捷方式与布局", [
            _arrow, _suffix, _shield, _win10Explorer, _classicMenu, _onedrive,
        ]);
        var taskbar = ThemedSettingsChrome.CreateSection("任务栏", [
            BuildSearchRow(), BuildAlignRow(),
            _widgets, _chat, _copilot,
            BuildAutohideRow(), _taskView, _seconds, BuildGlomRow(),
        ]);
        var tools = BuildToolsSection();

        body.Controls.Add(tools);
        body.Controls.Add(taskbar);
        body.Controls.Add(shortcuts);
        body.Controls.Add(quickAccess);
        body.Controls.Add(folderOpts);
        body.Controls.Add(common);

        ThemedSettingsChrome.MountEmbedded(
            this,
            AppLang.L("资源管理器", "File Explorer"),
            AppLang.L(
                "资源管理器即时生效 · 任务栏改完后点「应用到系统」",
                "Explorer applies instantly · use Apply for taskbar changes"),
            body,
            AppLang.L(
                "任务栏搜索/对齐等需点「应用到系统」（会重启资源管理器）。",
                "Taskbar search/alignment need Apply (restarts Explorer)."),
            LoadValues,
            ApplyTaskbarToSystem);

        Shown += (_, _) =>
        {
            if (_loaded) return;
            BeginInvoke(new Action(LoadValues));
        };
    }

    /// <summary>预热已加载过：首次挂到主界面时跳过立刻再刷一次。</summary>
    public bool ConsumeWarmLoadSkip()
    {
        if (!_warmLoadSkip) return false;
        _warmLoadSkip = false;
        return true;
    }

    public bool SupportsApplyToSystem => true;

    public void ApplyToSystem() => ApplyTaskbarToSystem();

    private Control BuildLaunchRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("打开至", "Open to"), _launchTo,
            [AppLang.L("此电脑", "This PC"), AppLang.L("快速访问", "Quick access")]);
        _launchTo.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo",
                _launchTo.SelectedIndex == 0 ? 1 : 2);
        };
        return row;
    }

    private Control BuildSearchRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("搜索", "Search"), _searchMode,
            [AppLang.L("隐藏", "Hidden"), AppLang.L("仅图标", "Icon only"), AppLang.L("搜索框", "Search box")]);
        // 任务栏项：改动后需「应用到系统」才写入并重启资源管理器
        _searchMode.SelectedIndexChanged += (_, _) => { /* deferred */ };
        return row;
    }

    private Control BuildAlignRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("对齐", "Alignment"), _align,
            [AppLang.L("靠左", "Left"), AppLang.L("居中", "Center")]);
        _align.SelectedIndexChanged += (_, _) => { /* deferred */ };
        return row;
    }

    private Control BuildGlomRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("合并", "Combine"), _glom,
            [AppLang.L("始终合并", "Always"), AppLang.L("已满时合并", "When full"), AppLang.L("从不合并", "Never")]);
        _glom.SelectedIndexChanged += (_, _) => { /* deferred */ };
        return row;
    }

    private Control BuildAutohideRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("任务栏显示", "Taskbar"), _autohideMode,
            [AppLang.L("一直显示", "Always show"), AppLang.L("自动隐藏", "Auto-hide")]);
        _autohideMode.SelectedIndexChanged += (_, _) => { /* deferred */ };
        return row;
    }

    private Control BuildDriveLetterRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("盘符位置", "Drive letter"), _driveLetters, FolderViewTweaks.DriveLetterLabels);
        _driveLetters.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            var i = _driveLetters.SelectedIndex is >= 0 and <= 2 ? _driveLetters.SelectedIndex : 0;
            FolderViewTweaks.SetAdv("ShowDriveLettersFirst", i);
            DesktopQuickActions.RestartExplorer();
        };
        return row;
    }

    private Control BuildGroupByRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("分组依据", "Group by"), _folderGroup, FolderViewTweaks.GroupByLabels);
        _folderGroup.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            FolderViewTweaks.ApplyGroupBy(_folderGroup.SelectedIndex);
            DesktopQuickActions.RestartExplorer();
        };
        return row;
    }

    private Control BuildSortByRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow(AppLang.L("排序方式", "Sort by"), _folderSort, FolderViewTweaks.SortByLabels);
        _folderSort.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            FolderViewTweaks.ApplySortBy(_folderSort.SelectedIndex);
            DesktopQuickActions.RestartExplorer();
        };
        return row;
    }

    private Panel BuildToolsSection()
    {
        var (card, host) = ThemedSettingsChrome.CreateSectionShell("快捷操作");
        var tip = new Label
        {
            Text = "任务栏相关请先改开关，再点底部「应用到系统」。其它：菜单「工具 → 桌面维护」。",
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
            Margin = new Padding(0, 4, 0, 0),
        };
        host.Controls.Add(tip);
        return card;
    }

    /// <summary>写入任务栏相关设置并重启资源管理器，使搜索隐藏/显示等立即可见。</summary>
    private void ApplyTaskbarToSystem()
    {
        try
        {
            var mode = _searchMode.SelectedIndex is >= 0 and <= 2 ? _searchMode.SelectedIndex : 1;
            EasySettingsTweaks.SetSearchboxMode(mode);
            SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl",
                _align.SelectedIndex == 0 ? 0 : 1);
            EasySettingsTweaks.SetTaskbarGlomLevel(_glom.SelectedIndex is >= 0 and <= 2 ? _glom.SelectedIndex : 0);

            Win11DesktopTweaks.SetTaskbarAutoHideEnabled(_autohideMode.SelectedIndex == 1);
            Win11DesktopTweaks.SetShowTaskViewButton(_taskView.Checked);
            Win11DesktopTweaks.SetDisableWidgets(_widgets.Checked);
            SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock",
                _seconds.Checked ? 1 : 0);

            var bits = new Optimizer.State();
            EasySettingsTweaks.ReadExplorerOnly(bits);
            bits.HideTaskbarChat = _chat.Checked;
            bits.HideTaskbarCopilot = _copilot.Checked;
            EasySettingsTweaks.ApplyExplorerBits(bits);

            DesktopQuickActions.RestartExplorer();
            ApplyLog.Write("资源管理器页：已应用任务栏设置并重启资源管理器");
            MessageBox.Show(this,
                "任务栏设置已写入，并已重启资源管理器。",
                "应用到系统", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "应用到系统", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public void RefreshFromSystem()
    {
        LoadValues();
        _warmLoadSkip = true;
    }

    private void LoadValues()
    {
        _loading = true;
        int hideExt = -1, fullPath = -1, hidden = -1, seconds = -1, launchTo = -1;
        using (var adv = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
        {
            if (adv is not null)
            {
                hideExt = adv.GetValue("HideFileExt") is int a ? a : -1;
                fullPath = adv.GetValue("FullPath") is int b ? b : -1;
                hidden = adv.GetValue("Hidden") is int c ? c : -1;
                seconds = adv.GetValue("ShowSecondsInSystemClock") is int d ? d : -1;
                launchTo = adv.GetValue("LaunchTo") is int e ? e : -1;
            }
        }

        _ext.Bind(hideExt == 0,
            v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", v ? 0 : 1));
        _fullPath.Bind(fullPath == 1,
            v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath", v ? 1 : 0));
        _hidden.Bind(hidden == 1,
            v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", v ? 1 : 2));

        var bits = new Optimizer.State();
        EasySettingsTweaks.ReadExplorerOnly(bits);
        _osFiles.Bind(bits.HideProtectedOsFiles, v => { bits.HideProtectedOsFiles = v; EasySettingsTweaks.ApplyExplorerBits(bits); });
        _iconsOnly.Bind(bits.AlwaysShowIconsNeverThumbnails, v => { bits.AlwaysShowIconsNeverThumbnails = v; EasySettingsTweaks.ApplyExplorerBits(bits); });
        _emptyDrives.Bind(bits.ShowEmptyDrives, v => { bits.ShowEmptyDrives = v; EasySettingsTweaks.ApplyExplorerBits(bits); });
        _recent.Bind(bits.ShowRecentFiles, v => { bits.ShowRecentFiles = v; EasySettingsTweaks.ApplyExplorerBits(bits); });
        _frequent.Bind(bits.ShowFrequentPlaces, v => { bits.ShowFrequentPlaces = v; EasySettingsTweaks.ApplyExplorerBits(bits); });
        _office.Bind(bits.HideOfficeCloudFiles, v => { bits.HideOfficeCloudFiles = v; EasySettingsTweaks.ApplyExplorerBits(bits); });
        _onedrive.Bind(bits.DisableOneDrive, v => { bits.DisableOneDrive = v; EasySettingsTweaks.ApplyExplorerBits(bits); });

        // 任务栏相关：只更新界面，真正写入在「应用到系统」
        _chat.Bind(bits.HideTaskbarChat, _ => { });
        _copilot.Bind(bits.HideTaskbarCopilot, _ => { });
        _arrow.Bind(Win11DesktopTweaks.IsShortcutArrowHidden(), Win11DesktopTweaks.SetShortcutArrowHidden);
        _suffix.Bind(Win11DesktopTweaks.IsNoShortcutSuffixOn(), Win11DesktopTweaks.SetNoShortcutSuffix);
        _shield.Bind(Win11DesktopTweaks.IsRemoveAdminShieldOn(), Win11DesktopTweaks.SetRemoveAdminShield);
        _win10Explorer.Bind(!Win11DesktopTweaks.IsWin11ExplorerStyleOn(), Win11DesktopTweaks.SetCompactExplorerSpacing);
        _classicMenu.Bind(Win11DesktopTweaks.IsWin10ClassicContextMenuOn(), Win11DesktopTweaks.SetWin10ClassicContextMenu);
        _autohideMode.SelectedIndex = Win11DesktopTweaks.IsTaskbarAutoHideOn() ? 1 : 0;
        _taskView.Bind(Win11DesktopTweaks.IsShowTaskViewButtonOn(), _ => { });
        _widgets.Bind(Win11DesktopTweaks.IsDisableWidgetsOn(), _ => { });
        _seconds.Bind(seconds == 1, _ => { });

        _alwaysMenu.Bind(FolderViewTweaks.GetAdv("AlwaysShowMenus", 0) == 1,
            v => FolderViewTweaks.SetAdv("AlwaysShowMenus", v ? 1 : 0));
        _hideMerge.Bind(FolderViewTweaks.GetAdv("HideMergeConflicts", 0) == 1,
            v => FolderViewTweaks.SetAdv("HideMergeConflicts", v ? 1 : 0));
        _compColor.Bind(FolderViewTweaks.GetAdv("ShowCompColor", 0) == 1,
            v => FolderViewTweaks.SetAdv("ShowCompColor", v ? 1 : 0));
        _infoTip.Bind(FolderViewTweaks.GetAdv("ShowInfoTip", 1) != 0,
            v => FolderViewTweaks.SetAdv("ShowInfoTip", v ? 1 : 0));
        _statusBar.Bind(FolderViewTweaks.GetAdv("ShowStatusBar", 1) != 0,
            v => FolderViewTweaks.SetAdv("ShowStatusBar", v ? 1 : 0));
        _noPersist.Bind(FolderViewTweaks.GetAdv("PersistBrowsers", 0) != 1,
            v => FolderViewTweaks.SetAdv("PersistBrowsers", v ? 0 : 1));
        _navExpand.Bind(FolderViewTweaks.GetAdv("NavPaneExpandToCurrentFolder", 0) == 1,
            v => FolderViewTweaks.SetAdv("NavPaneExpandToCurrentFolder", v ? 1 : 0));
        _noShareWiz.Bind(FolderViewTweaks.GetAdv("SharingWizardOn", 1) == 0,
            v => FolderViewTweaks.SetAdv("SharingWizardOn", v ? 0 : 1));
        var letters = FolderViewTweaks.GetAdv("ShowDriveLettersFirst", 0);
        _driveLetters.SelectedIndex = letters is >= 0 and <= 2 ? letters : 0;
        _folderGroup.SelectedIndex = FolderViewTweaks.ReadGroupBy();
        _folderSort.SelectedIndex = FolderViewTweaks.ReadSortBy();

        _launchTo.SelectedIndex = launchTo == 1 ? 0 : 1;
        var mode = EasySettingsTweaks.GetSearchboxMode();
        _searchMode.SelectedIndex = mode is 0 or 1 or 2 ? mode : 1;
        _align.SelectedIndex = Win11DesktopTweaks.IsTaskbarAlignLeftOn() ? 0 : 1;
        var glom = EasySettingsTweaks.GetTaskbarGlomLevel();
        _glom.SelectedIndex = glom is 0 or 1 or 2 ? glom : 0;
        _loading = false;
        _loaded = true;
    }

    private static void SetDwordCu(string key, string name, int value)
    {
        object? old;
        using (var r = Registry.CurrentUser.OpenSubKey(key))
            old = r?.GetValue(name);
        ApplyLog.RegistryDword("HKCU", key, name, old, value);
        using var k = Registry.CurrentUser.CreateSubKey(key);
        k?.SetValue(name, value, RegistryValueKind.DWord);
    }

}
