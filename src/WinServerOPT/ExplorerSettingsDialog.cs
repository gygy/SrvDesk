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
    private readonly InstantToggleRow _recent = new("开始屏幕显示最近文件");
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

        // 分区：常用 → 快速访问 → 快捷方式/云 → 任务栏 → 工具
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
            _autohide, _taskView, _seconds, BuildGlomRow(),
        ]);
        var tools = BuildToolsSection();

        body.Controls.Add(tools);
        body.Controls.Add(taskbar);
        body.Controls.Add(shortcuts);
        body.Controls.Add(quickAccess);
        body.Controls.Add(common);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "资源管理器",
            "资源管理器即时生效 · 任务栏改完后点「应用到系统」",
            body,
            "任务栏搜索/对齐等需点「应用到系统」（会重启资源管理器）。",
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
        // 任务栏项：改动后需「应用到系统」才写入并重启资源管理器
        _searchMode.SelectedIndexChanged += (_, _) => { /* deferred */ };
        return row;
    }

    private Control BuildAlignRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow("对齐", _align, ["靠左", "居中"]);
        _align.SelectedIndexChanged += (_, _) => { /* deferred */ };
        return row;
    }

    private Control BuildGlomRow()
    {
        var row = ThemedSettingsChrome.CreateComboRow("合并", _glom, ["始终合并", "已满时合并", "从不合并"]);
        _glom.SelectedIndexChanged += (_, _) => { /* deferred */ };
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

            Win11DesktopTweaks.SetTaskbarAutoHideEnabled(_autohide.Checked);
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
        _autohide.Bind(Win11DesktopTweaks.IsTaskbarAutoHideOn(), _ => { });
        _taskView.Bind(Win11DesktopTweaks.IsShowTaskViewButtonOn(), _ => { });
        _widgets.Bind(Win11DesktopTweaks.IsDisableWidgetsOn(), _ => { });
        _seconds.Bind(seconds == 1, _ => { });

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
