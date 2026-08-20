using Microsoft.Win32;

namespace WinOpt;

internal sealed class ExplorerSettingsDialog : Form
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

    public ExplorerSettingsDialog()
    {
        Text = "Explorer 设置";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 680);
        MinimumSize = new Size(860, 560);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        var header = ThemedSettingsChrome.CreateHeader("Explorer 设置", "资源管理器 · 任务栏 · 快速打开");
        var footer = ThemedSettingsChrome.CreateFooter(this, "开关立即写入注册表。部分项需点「重启资源管理器」后可见。", LoadValues, showClose: false);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true, BackColor = AppTheme.Surface };

        var explorerCard = BuildExplorerCard();
        explorerCard.Dock = DockStyle.Top;
        var taskbarCard = BuildTaskbarCard();
        taskbarCard.Dock = DockStyle.Top;
        var quickCard = BuildQuickCard();
        quickCard.Dock = DockStyle.Top;

        body.Controls.Add(quickCard);
        body.Controls.Add(taskbarCard);
        body.Controls.Add(explorerCard);

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        Load += (_, _) => LoadValues();
    }

    private Panel BuildExplorerCard()
    {
        var left = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Width = 430,
        };
        left.Controls.AddRange([_ext, _fullPath, _hidden, _osFiles, _iconsOnly, _emptyDrives, _recent, _frequent, _office]);

        var launchRow = ComboRow("打开资源管理器时打开：", _launchTo, ["此电脑", "快速访问"]);
        _launchTo.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo",
                _launchTo.SelectedIndex == 0 ? 1 : 2);
        };

        var right = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Width = 430,
        };
        right.Controls.Add(launchRow);
        right.Controls.AddRange([_arrow, _suffix, _shield, _win10Explorer, _classicMenu, _onedrive]);

        var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Width = 200 };
        actions.Controls.Add(ActionBtn("刷新系统图标缓存", () => DesktopQuickActions.RefreshIconCache(this)));
        actions.Controls.Add(ActionBtn("重启文件资源管理器", DesktopQuickActions.RestartExplorer, accent: true));
        actions.Controls.Add(ActionBtn("清空回收站", () => DesktopQuickActions.EmptyRecycleBin(this)));
        actions.Controls.Add(ActionBtn("性能选项...", () => DesktopQuickActions.OpenPerformanceOptions(this)));
        actions.Controls.Add(ActionBtn("桌面图标设置...", () => DesktopQuickActions.OpenDesktopIconSettings(this)));

        var inner = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, Padding = new Padding(0, 28, 0, 0) };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        inner.Controls.Add(left, 0, 0);
        inner.Controls.Add(right, 1, 0);
        inner.Controls.Add(actions, 2, 0);

        var card = SectionShell("文件资源管理器", 420);
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

        var grid = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 28, 0, 0) };
        grid.Controls.Add(searchRow);
        grid.Controls.Add(_autohide);
        grid.Controls.Add(_taskView);
        grid.Controls.Add(_chat);
        grid.Controls.Add(_copilot);
        grid.Controls.Add(_widgets);
        grid.Controls.Add(alignRow);
        grid.Controls.Add(_seconds);
        grid.Controls.Add(glomRow);

        var card = SectionShell("任务栏设置", 170);
        card.Controls.Add(grid);
        return card;
    }

    private Panel BuildQuickCard()
    {
        var grid = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 5,
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 32, 8, 8),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var i = 0; i < 5; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        AddQuick(grid, 0, 0, "系统设置", () => Launch("ms-settings:"));
        AddQuick(grid, 1, 0, "控制面板", () => DesktopQuickActions.OpenControlPanel(this));
        AddQuick(grid, 0, 1, "程序和功能", () => Launch("appwiz.cpl"));
        AddQuick(grid, 1, 1, "计算机管理", () => SystemToolLauncher.OpenComputerManagement(this));
        AddQuick(grid, 0, 2, "网络连接", () => Launch("ncpa.cpl"));
        AddQuick(grid, 1, 2, "电源选项", () => Launch("powercfg.cpl"));
        AddQuick(grid, 0, 3, "设备管理器", () => DesktopQuickActions.OpenDeviceManager(this));
        AddQuick(grid, 1, 3, "屏幕键盘", () => Launch("osk.exe"));
        AddQuick(grid, 0, 4, "磁盘管理", () => DesktopQuickActions.OpenDiskManagement(this));
        AddQuick(grid, 1, 4, "上帝模式", () => Launch("shell:::{ED7BA470-8E54-465E-825C-99712043E01C}"));

        var card = SectionShell("快速打开", 220);
        card.Controls.Add(grid);
        return card;
    }

    private void LoadValues()
    {
        _loading = true;
        var s = Optimizer.Read(fullScan: false);
        _ext.Bind(s.ShowFileExtensions, v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", v ? 0 : 1));
        _fullPath.Bind(s.ExplorerFullPath, v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath", v ? 1 : 0));
        _hidden.Bind(s.ShowHiddenFiles, v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", v ? 1 : 2));
        _osFiles.Bind(s.HideProtectedOsFiles, v =>
        {
            s.HideProtectedOsFiles = v;
            EasySettingsTweaks.ApplyExplorerBits(s);
        });
        _iconsOnly.Bind(s.AlwaysShowIconsNeverThumbnails, v => { s.AlwaysShowIconsNeverThumbnails = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _emptyDrives.Bind(s.ShowEmptyDrives, v => { s.ShowEmptyDrives = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _recent.Bind(s.ShowRecentFiles, v => { s.ShowRecentFiles = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _frequent.Bind(s.ShowFrequentPlaces, v => { s.ShowFrequentPlaces = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _office.Bind(s.HideOfficeCloudFiles, v => { s.HideOfficeCloudFiles = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _arrow.Bind(s.NoShortcutArrow, v => { /* applied via Optimizer path */ ApplyArrow(v); });
        _suffix.Bind(s.NoShortcutSuffix, v => { s.NoShortcutSuffix = v; s.TaskbarSearchMode = _searchMode.SelectedIndex; Win11DesktopTweaks.Apply(s); });
        _shield.Bind(s.RemoveAdminShield, v => { s.RemoveAdminShield = v; s.TaskbarSearchMode = _searchMode.SelectedIndex; Win11DesktopTweaks.Apply(s); });
        _win10Explorer.Bind(!s.Win11ExplorerStyle, v => { s.Win11ExplorerStyle = !v; s.TaskbarSearchMode = _searchMode.SelectedIndex; Win11DesktopTweaks.Apply(s); });
        _classicMenu.Bind(s.Win10ClassicContextMenu, v => { s.Win10ClassicContextMenu = v; s.TaskbarSearchMode = _searchMode.SelectedIndex; Win11DesktopTweaks.Apply(s); });
        _onedrive.Bind(s.DisableOneDrive, v => { s.DisableOneDrive = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _autohide.Bind(s.TaskbarAutoHide, v => { s.TaskbarAutoHide = v; s.TaskbarSearchMode = _searchMode.SelectedIndex; Win11DesktopTweaks.Apply(s); });
        _taskView.Bind(s.ShowTaskViewButton, v => { s.ShowTaskViewButton = v; s.TaskbarSearchMode = _searchMode.SelectedIndex; Win11DesktopTweaks.Apply(s); });
        _chat.Bind(s.HideTaskbarChat, v => { s.HideTaskbarChat = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _copilot.Bind(s.HideTaskbarCopilot, v => { s.HideTaskbarCopilot = v; EasySettingsTweaks.ApplyExplorerBits(s); });
        _widgets.Bind(s.DisableWidgets, v => { s.DisableWidgets = v; s.TaskbarSearchMode = _searchMode.SelectedIndex; Win11DesktopTweaks.Apply(s); });
        _seconds.Bind(s.TaskbarClockWeekdaySeconds, v => SetDwordCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", v ? 1 : 0));

        _launchTo.SelectedIndex = s.LaunchExplorerThisPc ? 0 : 1;
        var mode = EasySettingsTweaks.GetSearchboxMode();
        _searchMode.SelectedIndex = mode is 0 or 1 or 2 ? mode : 1;
        _align.SelectedIndex = s.TaskbarAlignLeft ? 0 : 1;
        var glom = EasySettingsTweaks.GetTaskbarGlomLevel();
        _glom.SelectedIndex = glom is 0 or 1 or 2 ? glom : 0;
        _loading = false;
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
        var row = new Panel { Height = 36, Width = 400 };
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

    private static void AddQuick(TableLayoutPanel grid, int col, int row, string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.Dock = DockStyle.Fill;
        b.Margin = new Padding(4);
        b.Click += (_, _) => click();
        grid.Controls.Add(b, col, row);
    }

    private static void Launch(string target)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true,
        });
    }
}
