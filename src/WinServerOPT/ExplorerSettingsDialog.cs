using Microsoft.Win32;

namespace WinOpt;

/// <summary>资源管理器即时页：单列分区布局（与「系统服务」等页一致），避免多列 TableLayout 切换重排。</summary>
internal sealed class ExplorerSettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly InstantToggleRow _ext = new("显示文件扩展名");
    private readonly InstantToggleRow _fullPath = new("标题栏显示完整路径");
    private readonly InstantToggleRow _hidden = new("显示隐藏的文件和文件夹");
    private readonly InstantToggleRow _osFiles = new("隐藏受保护的系统文件");
    private readonly InstantToggleRow _iconsOnly = new("始终显示图标（无缩略图）");
    private readonly InstantToggleRow _emptyDrives = new("显示空驱动器");
    private readonly InstantToggleRow _recent = new("显示最近使用的文件");
    private readonly InstantToggleRow _frequent = new("显示常用文件夹");
    private readonly InstantToggleRow _office = new("隐藏 office.com 云文件");
    private readonly InstantToggleRow _arrow = new("去掉快捷方式箭头");
    private readonly InstantToggleRow _suffix = new("快捷方式不加「快捷方式」后缀");
    private readonly InstantToggleRow _shield = new("去掉管理员盾牌图标");
    private readonly InstantToggleRow _win10Explorer = new("紧凑 / Win10 间距");
    private readonly InstantToggleRow _classicMenu = new("Win10 经典右键菜单");
    private readonly InstantToggleRow _onedrive = new("禁止 OneDrive");
    private readonly InstantToggleRow _autohide = new("自动隐藏任务栏");
    private readonly InstantToggleRow _taskView = new("显示任务视图按钮");
    private readonly InstantToggleRow _chat = new("隐藏任务栏聊天");
    private readonly InstantToggleRow _copilot = new("隐藏任务栏 Copilot");
    private readonly InstantToggleRow _widgets = new("关闭任务栏小组件");
    private readonly InstantToggleRow _seconds = new("托盘时钟显示秒");
    private readonly ComboBox _launchTo = new();
    private readonly ComboBox _searchMode = new();
    private readonly ComboBox _align = new();
    private readonly ComboBox _glom = new();
    private bool _loading;
    private bool _loaded;
    private bool _warmLoadSkip;

    public ExplorerSettingsDialog()
    {
        Text = "资源管理器";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 640);
        MinimumSize = new Size(720, 480);

        var body = ThemedSettingsChrome.CreateBodyPanel();

        var files = ThemedSettingsChrome.CreateSection("文件与显示", [
            _ext, _fullPath, _hidden, _osFiles, _iconsOnly, _emptyDrives, _recent, _frequent, _office,
        ]);
        var desktop = ThemedSettingsChrome.CreateSection("桌面与快捷方式", [
            BuildLaunchRow(),
            _arrow, _suffix, _shield, _win10Explorer, _classicMenu, _onedrive,
        ]);
        var taskbar = ThemedSettingsChrome.CreateSection("任务栏", [
            BuildSearchRow(),
            _autohide, _taskView, _chat, _copilot, _widgets,
            BuildAlignRow(),
            _seconds,
            BuildGlomRow(),
        ]);
        var tools = BuildToolsSection();

        // Dock.Top 后添加的在上：先加工具，再任务栏…最终「文件与显示」在最上
        body.Controls.Add(tools);
        body.Controls.Add(taskbar);
        body.Controls.Add(desktop);
        body.Controls.Add(files);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "资源管理器",
            "资源管理器 · 任务栏 · 开关立即生效",
            body,
            "部分项需点「重启资源管理器」后可见。",
            LoadValues);

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

    private Control BuildLaunchRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow("打开至", _launchTo, ["此电脑", "快速访问"]);
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
        var row = ThemedSettingsChrome.CreateComboRow("搜索", _searchMode, ["隐藏", "仅图标", "搜索框"]);
        _searchMode.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            EasySettingsTweaks.SetSearchboxMode(_searchMode.SelectedIndex);
        };
        return row;
    }

    private Control BuildAlignRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow("对齐", _align, ["靠左", "居中"]);
        _align.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl",
                _align.SelectedIndex == 0 ? 0 : 1);
        };
        return row;
    }

    private Control BuildGlomRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow("合并", _glom, ["始终合并", "已满时合并", "从不合并"]);
        _glom.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            EasySettingsTweaks.SetTaskbarGlomLevel(_glom.SelectedIndex);
        };
        return row;
    }

    private Panel BuildToolsSection()
    {
        var (card, host) = ThemedSettingsChrome.CreateSectionShell("快捷操作");
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 4, 0, 0),
            BackColor = Color.Transparent,
        };
        row.Controls.Add(ActionBtn("刷新图标缓存", () => DesktopQuickActions.RefreshIconCache(this)));
        row.Controls.Add(ActionBtn("重启资源管理器", DesktopQuickActions.RestartExplorer, accent: true));
        row.Controls.Add(ActionBtn("清空回收站", () => DesktopQuickActions.EmptyRecycleBin(this)));
        row.Controls.Add(ActionBtn("性能选项...", () => DesktopQuickActions.OpenPerformanceOptions(this)));
        row.Controls.Add(ActionBtn("桌面图标...", () => DesktopQuickActions.OpenDesktopIconSettings(this)));
        host.Controls.Add(row);
        return card;
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
        _chat.Bind(bits.HideTaskbarChat, v => { bits.HideTaskbarChat = v; EasySettingsTweaks.ApplyExplorerBits(bits); });
        _copilot.Bind(bits.HideTaskbarCopilot, v => { bits.HideTaskbarCopilot = v; EasySettingsTweaks.ApplyExplorerBits(bits); });

        _arrow.Bind(IsShortcutArrowHidden(), ApplyArrow);
        _suffix.Bind(Win11DesktopTweaks.IsNoShortcutSuffixOn(), Win11DesktopTweaks.SetNoShortcutSuffix);
        _shield.Bind(Win11DesktopTweaks.IsRemoveAdminShieldOn(), Win11DesktopTweaks.SetRemoveAdminShield);
        _win10Explorer.Bind(!Win11DesktopTweaks.IsWin11ExplorerStyleOn(), Win11DesktopTweaks.SetCompactExplorerSpacing);
        _classicMenu.Bind(Win11DesktopTweaks.IsWin10ClassicContextMenuOn(), Win11DesktopTweaks.SetWin10ClassicContextMenu);
        _autohide.Bind(Win11DesktopTweaks.IsTaskbarAutoHideOn(), Win11DesktopTweaks.SetTaskbarAutoHideEnabled);
        _taskView.Bind(Win11DesktopTweaks.IsShowTaskViewButtonOn(), Win11DesktopTweaks.SetShowTaskViewButton);
        _widgets.Bind(Win11DesktopTweaks.IsDisableWidgetsOn(), Win11DesktopTweaks.SetDisableWidgets);
        _seconds.Bind(seconds == 1,
            v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", v ? 1 : 0));

        _launchTo.SelectedIndex = launchTo == 1 ? 0 : 1;
        var mode = EasySettingsTweaks.GetSearchboxMode();
        _searchMode.SelectedIndex = mode is 0 or 1 or 2 ? mode : 1;
        _align.SelectedIndex = Win11DesktopTweaks.IsTaskbarAlignLeftOn() ? 0 : 1;
        var glom = EasySettingsTweaks.GetTaskbarGlomLevel();
        _glom.SelectedIndex = glom is 0 or 1 or 2 ? glom : 0;
        _loading = false;
        _loaded = true;
    }

    private static bool IsShortcutArrowHidden()
    {
        using var k = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
        return k?.GetValue("29") is not null;
    }

    private static void ApplyArrow(bool hide)
    {
        using var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
        if (hide) k?.SetValue("29", "", RegistryValueKind.String);
        else k?.DeleteValue("29", throwOnMissingValue: false);
    }

    private static void SetDwordCu(string key, string name, int value)
    {
        using var k = Registry.CurrentUser.CreateSubKey(key);
        k?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static Control ActionBtn(string text, Action click, bool accent = false)
    {
        var b = ThemedSettingsChrome.CreateButton(text, accent);
        b.AutoSize = true;
        b.Height = 32;
        b.Margin = new Padding(0, 0, 8, 8);
        b.Click += (_, _) => click();
        return b;
    }
}
