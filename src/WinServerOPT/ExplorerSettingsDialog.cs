using Microsoft.Win32;

namespace WinOpt;

internal sealed class ExplorerSettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly InstantToggleRow _ext = new("显示所有文件的文件扩展名");
    private readonly InstantToggleRow _fullPath = new("在标题栏中显示完整路径");
    private readonly InstantToggleRow _hidden = new("显示隐藏的文件、文件夹和驱动器");
    private readonly InstantToggleRow _osFiles = new("隐藏受保护的系统文件（推荐）");
    private readonly InstantToggleRow _iconsOnly = new("始终显示图标，从不显示缩略图");
    private readonly InstantToggleRow _emptyDrives = new("显示空的驱动器");
    private readonly InstantToggleRow _recent = new("显示最近使用的文件");
    private readonly InstantToggleRow _frequent = new("显示常用文件夹");
    private readonly InstantToggleRow _office = new("隐藏来自 office.com 的文件");
    private readonly InstantToggleRow _arrow = new("去除快捷方式的小箭头");
    private readonly InstantToggleRow _suffix = new("创建快捷方式时不加「快捷方式」文字");
    private readonly InstantToggleRow _shield = new("去除程序图标的盾牌标识");
    private readonly InstantToggleRow _win10Explorer = new("使用紧凑/Win10 风格间距");
    private readonly InstantToggleRow _classicMenu = new("Win10 经典右键菜单");
    private readonly InstantToggleRow _onedrive = new("禁止使用 OneDrive");
    private readonly InstantToggleRow _autohide = new("自动隐藏任务栏");
    private readonly InstantToggleRow _taskView = new("显示任务视图按钮");
    private readonly InstantToggleRow _chat = new("隐藏任务栏聊天");
    private readonly InstantToggleRow _copilot = new("隐藏任务栏 Copilot");
    private readonly InstantToggleRow _widgets = new("关闭任务栏小组件");
    private readonly InstantToggleRow _seconds = new("系统托盘时间显示秒");
    private readonly ComboBox _launchTo = new();
    private readonly ComboBox _searchMode = new();
    private readonly ComboBox _align = new();
    private readonly ComboBox _glom = new();
    private bool _loading;
    private bool _loaded;

    public ExplorerSettingsDialog()
    {
        Text = "资源管理器";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 680);
        MinimumSize = new Size(860, 560);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var explorerCard = BuildExplorerCard();
        explorerCard.Dock = DockStyle.Top;
        var taskbarCard = BuildTaskbarCard();
        taskbarCard.Dock = DockStyle.Top;
        body.Controls.Add(taskbarCard);
        body.Controls.Add(explorerCard);

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
            _loaded = true;
            BeginInvoke(new Action(LoadValues));
        };
    }

    private Panel BuildExplorerCard()
    {
        InstantToggleRow[] leftRows =
        [
            _ext, _fullPath, _hidden, _osFiles, _iconsOnly, _emptyDrives, _recent, _frequent, _office,
        ];
        InstantToggleRow[] rightRows =
        [
            _arrow, _suffix, _shield, _win10Explorer, _classicMenu, _onedrive,
        ];

        var left = Column(leftRows);
        var launchRow = ComboRow("打开资源管理器时打开：", _launchTo, ["此电脑", "快速访问"]);
        _launchTo.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo",
                _launchTo.SelectedIndex == 0 ? 1 : 2);
        };

        var rightHost = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Fill,
        };
        rightHost.Controls.Add(launchRow);
        foreach (var r in rightRows) rightHost.Controls.Add(r);

        var actions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Width = 190,
        };
        actions.Controls.Add(ActionBtn("刷新系统图标缓存", () => DesktopQuickActions.RefreshIconCache(this)));
        actions.Controls.Add(ActionBtn("重启文件资源管理器", DesktopQuickActions.RestartExplorer, accent: true));
        actions.Controls.Add(ActionBtn("清空回收站", () => DesktopQuickActions.EmptyRecycleBin(this)));
        actions.Controls.Add(ActionBtn("性能选项...", () => DesktopQuickActions.OpenPerformanceOptions(this)));
        actions.Controls.Add(ActionBtn("桌面图标设置...", () => DesktopQuickActions.OpenDesktopIconSettings(this)));

        var contentH = Math.Max(leftRows.Length, rightRows.Length + 1) * 38 + 48;
        var inner = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Top,
            Height = contentH,
            Padding = new Padding(0, 28, 0, 8),
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        inner.Controls.Add(left, 0, 0);
        inner.Controls.Add(rightHost, 1, 0);
        inner.Controls.Add(actions, 2, 0);

        var card = SectionShell("文件资源管理器", contentH + 36);
        card.Controls.Add(inner);
        return card;
    }

    private Panel BuildTaskbarCard()
    {
        var searchRow = ComboRow("搜索按钮：", _searchMode, ["隐藏", "仅图标", "搜索框"]);
        _searchMode.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            EasySettingsTweaks.SetSearchboxMode(_searchMode.SelectedIndex);
        };
        var alignRow = ComboRow("任务栏对齐：", _align, ["靠左", "居中"]);
        _align.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl",
                _align.SelectedIndex == 0 ? 0 : 1);
        };
        var glomRow = ComboRow("任务栏按钮合并：", _glom, ["始终合并", "任务栏已满时", "从不合并"]);
        _glom.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            EasySettingsTweaks.SetTaskbarGlomLevel(_glom.SelectedIndex);
        };

        Control[] rows =
        [
            searchRow, _autohide, _taskView, _chat, _copilot, _widgets, alignRow, _seconds, glomRow,
        ];
        var host = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(0, 28, 0, 8),
        };
        foreach (var c in rows) host.Controls.Add(c);

        var card = SectionShell("任务栏设置", rows.Length * 38 + 48);
        card.Controls.Add(host);
        return card;
    }

    public void RefreshFromSystem() => LoadValues();

    private void LoadValues()
    {
        _loading = true;
        // 仅读本页需要的注册表/服务，避免 Optimizer.Read 全量扫描导致卡顿
        _ext.Bind(DwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt") == 0,
            v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", v ? 0 : 1));
        _fullPath.Bind(DwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath") == 1,
            v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath", v ? 1 : 0));
        _hidden.Bind(DwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden") == 1,
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
        _suffix.Bind(Win11DesktopTweaks.IsNoShortcutSuffixOn(), v =>
        {
            bits.NoShortcutSuffix = v;
            bits.TaskbarSearchMode = _searchMode.SelectedIndex;
            Win11DesktopTweaks.Apply(bits);
        });
        _shield.Bind(Win11DesktopTweaks.IsRemoveAdminShieldOn(), v =>
        {
            bits.RemoveAdminShield = v;
            bits.TaskbarSearchMode = _searchMode.SelectedIndex;
            Win11DesktopTweaks.Apply(bits);
        });
        _win10Explorer.Bind(!Win11DesktopTweaks.IsWin11ExplorerStyleOn(), v =>
        {
            bits.Win11ExplorerStyle = !v;
            bits.TaskbarSearchMode = _searchMode.SelectedIndex;
            Win11DesktopTweaks.Apply(bits);
        });
        _classicMenu.Bind(Win11DesktopTweaks.IsWin10ClassicContextMenuOn(), v =>
        {
            bits.Win10ClassicContextMenu = v;
            bits.TaskbarSearchMode = _searchMode.SelectedIndex;
            Win11DesktopTweaks.Apply(bits);
        });
        _autohide.Bind(Win11DesktopTweaks.IsTaskbarAutoHideOn(), v =>
        {
            bits.TaskbarAutoHide = v;
            bits.TaskbarSearchMode = _searchMode.SelectedIndex;
            Win11DesktopTweaks.Apply(bits);
        });
        _taskView.Bind(Win11DesktopTweaks.IsShowTaskViewButtonOn(), v =>
        {
            bits.ShowTaskViewButton = v;
            bits.TaskbarSearchMode = _searchMode.SelectedIndex;
            Win11DesktopTweaks.Apply(bits);
        });
        _widgets.Bind(Win11DesktopTweaks.IsWidgetsDisabled(), v =>
        {
            bits.DisableWidgets = v;
            bits.TaskbarSearchMode = _searchMode.SelectedIndex;
            Win11DesktopTweaks.Apply(bits);
        });
        _seconds.Bind(DwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock") == 1,
            v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", v ? 1 : 0));

        _launchTo.SelectedIndex = DwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo") == 1 ? 0 : 1;
        var mode = EasySettingsTweaks.GetSearchboxMode();
        _searchMode.SelectedIndex = mode is 0 or 1 or 2 ? mode : 1;
        _align.SelectedIndex = Win11DesktopTweaks.IsTaskbarAlignLeftOn() ? 0 : 1;
        var glom = EasySettingsTweaks.GetTaskbarGlomLevel();
        _glom.SelectedIndex = glom is 0 or 1 or 2 ? glom : 0;
        _loading = false;
    }

    private static int DwordCu(string key, string name)
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(key);
        return k?.GetValue(name) is int i ? i : -1;
    }

    private static bool IsShortcutArrowHidden()
    {
        using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
        return k?.GetValue("29") is not null;
    }

    private static FlowLayoutPanel Column(InstantToggleRow[] rows)
    {
        var p = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Fill,
        };
        foreach (var r in rows) p.Controls.Add(r);
        return p;
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

    private static Panel SectionShell(string title, int height)
    {
        var card = new Panel
        {
            Height = height,
            Dock = DockStyle.Top,
            BackColor = AppTheme.SurfaceCard,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(10, 6, 10, 8),
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        var cap = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = AppTheme.TextHeader,
        };
        card.Controls.Add(cap);
        return card;
    }

    private static Panel ComboRow(string label, ComboBox box, string[] items)
    {
        var row = new Panel { Height = 36, Width = 420, Margin = new Padding(0, 0, 0, 2) };
        var l = new Label { Text = label, Location = new Point(4, 8), AutoSize = true };
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.Items.Clear();
        box.Items.AddRange(items);
        box.Location = new Point(200, 5);
        box.Width = 180;
        row.Controls.Add(l);
        row.Controls.Add(box);
        return row;
    }

    private static Control ActionBtn(string text, Action click, bool accent = false)
    {
        var b = ThemedSettingsChrome.CreateButton(text, accent);
        b.Width = 180;
        b.Height = 34;
        b.Margin = new Padding(4);
        b.Click += (_, _) => click();
        return b;
    }
}
