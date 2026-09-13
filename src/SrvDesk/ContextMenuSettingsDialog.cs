namespace SrvDesk;

/// <summary>
/// 右键菜单：快捷增强 + 已安装菜单扫描（安全启停、Server 精简）。
/// </summary>
internal sealed class ContextMenuSettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly InstantToggleRow _takeOwn = new("取得所有权");
    private readonly InstantToggleRow _openCmd = new("在此处打开 CMD");
    private readonly InstantToggleRow _openPs = new("在此处打开 PowerShell");
    private readonly InstantToggleRow _openPsAdmin = new("PowerShell（管理员）");
    private readonly InstantToggleRow _openWt = new("在此处打开 Windows Terminal");
    private readonly InstantToggleRow _openWtAdmin = new("Terminal（管理员）");
    private readonly InstantToggleRow _copyPath = new("复制完整路径");
    private readonly InstantToggleRow _copyMoveTo = new("复制到 / 移动到文件夹");
    private readonly InstantToggleRow _quickOps = new("空白处「快捷操作组」");
    private readonly InstantToggleRow _paint = new("用画图编辑图片");
    private readonly InstantToggleRow _notepad = new("用记事本编辑文件");
    private readonly InstantToggleRow _blockShare = new("屏蔽「授予访问权限」");
    private readonly InstantToggleRow _classicMenu = new(AppLang.L("Win10 经典右键菜单", "Win10 classic context menu"));
    private readonly Label _hint = new();
    private readonly Action? _onChanged;

    private readonly FlatChromeButton _tabQuick = new();
    private readonly FlatChromeButton _tabScan = new();
    private readonly Panel _tabBar = new();
    private readonly Panel _pageHost = new();
    private readonly Panel _quickPage = new();
    private readonly Panel _scanPage = new();

    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _sceneFilter = new();
    private readonly ComboBox _adviceFilter = new();
    private List<ContextMenuEntry> _items = [];
    private bool _scanLoaded;
    private Button[] _scanButtons = [];

    public ContextMenuSettingsDialog(Action? onChanged = null)
    {
        _onChanged = onChanged;
        Text = AppLang.L("右键菜单", "Context menu");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(880, 620);
        MinimumSize = UiScale.Size(820, 560);

        BuildTabs();
        BuildQuickPage();
        BuildScanPage();

        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = AppTheme.Surface;
        _pageHost.Controls.Add(_scanPage);
        _pageHost.Controls.Add(_quickPage);

        var shell = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        shell.Controls.Add(_pageHost);
        shell.Controls.Add(_tabBar);

        ThemedSettingsChrome.MountEmbedded(
            this,
            AppLang.L("右键菜单", "Context menu"),
            AppLang.L("增强 · 精简已安装项", "Enhance · Slim installed"),
            shell,
            "",
            RefreshFromSystem);

        ShowPage(quick: true);
        Load += (_, _) => RefreshFromSystem();
    }

    public bool SupportsApplyToSystem => false;
    public void ApplyToSystem() { }
    public bool ConsumeWarmLoadSkip() => false;

    public void RefreshFromSystem()
    {
        LoadQuickValues();
        if (_scanLoaded || _scanPage.Visible)
            RefreshScan();
    }

    private void BuildTabs()
    {
        var h = UiFit.ControlHeight();
        _tabBar.Dock = DockStyle.Top;
        _tabBar.Height = h + UiScale.S(16);
        _tabBar.BackColor = AppTheme.SurfaceCard;
        _tabBar.Padding = new Padding(UiScale.S(12), UiScale.S(8), UiScale.S(12), UiScale.S(4));
        _tabBar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, 0, _tabBar.Height - 1, _tabBar.Width, _tabBar.Height - 1);
        };

        StyleTab(_tabQuick, AppLang.L("快捷增强", "Quick enhance"));
        StyleTab(_tabScan, AppLang.L("已安装菜单", "Installed menus"));
        _tabQuick.Click += (_, _) => ShowPage(quick: true);
        _tabScan.Click += (_, _) => ShowPage(quick: false);

        _tabBar.Controls.Add(_tabQuick);
        _tabBar.Controls.Add(_tabScan);
        _tabBar.Resize += (_, _) => LayoutTabs();
        LayoutTabs();
    }

    private void LayoutTabs()
    {
        var h = UiFit.ControlHeight();
        UiFit.FitButton(_tabQuick, h, minWidth: 96, padding: 28);
        UiFit.FitButton(_tabScan, h, minWidth: 96, padding: 28);
        var y = Math.Max(0, (_tabBar.ClientSize.Height - h - 1) / 2);
        _tabQuick.Location = new Point(UiScale.S(12), y);
        _tabScan.Location = new Point(_tabQuick.Right + UiScale.S(8), y);
    }

    private static void StyleTab(FlatChromeButton b, string text)
    {
        b.Text = text;
        b.FlatStyle = FlatStyle.Flat;
        b.Cursor = Cursors.Hand;
        b.Font = UiFit.UiFont;
        b.UseVisualStyleBackColor = false;
        b.UseCompatibleTextRendering = false;
        b.AutoSize = false;
        b.FlatAppearance.BorderSize = 1;
    }

    private void ApplyTabVisual(bool quick)
    {
        void One(FlatChromeButton b, bool on)
        {
            if (on)
            {
                b.BackColor = AppTheme.Primary;
                b.ForeColor = AppTheme.TextOnPrimary;
                b.FlatAppearance.BorderSize = 0;
            }
            else
            {
                b.BackColor = Color.White;
                b.ForeColor = AppTheme.TextMain;
                b.FlatAppearance.BorderColor = AppTheme.Border;
                b.FlatAppearance.BorderSize = 1;
            }
        }

        One(_tabQuick, quick);
        One(_tabScan, !quick);
    }

    private void ShowPage(bool quick)
    {
        ApplyTabVisual(quick);
        _quickPage.Visible = quick;
        _scanPage.Visible = !quick;
        if (quick)
            _quickPage.BringToFront();
        else
        {
            _scanPage.BringToFront();
            if (!_scanLoaded)
                RefreshScan();
        }
    }

    private void BuildQuickPage()
    {
        _quickPage.Dock = DockStyle.Fill;
        _quickPage.BackColor = AppTheme.Surface;
        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.Dock = DockStyle.Fill;
        body.Padding = new Padding(UiScale.S(12), UiScale.S(10), UiScale.S(12), UiScale.S(12));

        var common = ThemedSettingsChrome.CreateSection(
            AppLang.L("常用", "Common"),
            [_takeOwn, _openCmd, _copyPath, _copyMoveTo, _quickOps, _classicMenu]);
        var terminal = ThemedSettingsChrome.CreateSection(
            AppLang.L("终端", "Terminal"),
            [_openPs, _openPsAdmin, _openWt, _openWtAdmin]);
        var edit = ThemedSettingsChrome.CreateSection(
            AppLang.L("用…打开", "Open with…"),
            [_paint, _notepad]);
        var other = ThemedSettingsChrome.CreateSection(
            AppLang.L("其它", "Other"),
            [_blockShare]);

        _hint.AutoSize = false;
        _hint.Dock = DockStyle.Top;
        _hint.Height = Math.Max(UiScale.S(28), UiFit.LineHeight(UiFit.UiFontSmall) + UiScale.S(12));
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Font = UiFit.UiFontSmall;
        _hint.TextAlign = ContentAlignment.MiddleLeft;
        _hint.AutoEllipsis = true;
        _hint.Padding = new Padding(UiScale.S(4), 0, UiScale.S(4), UiScale.S(6));
        _hint.Margin = Padding.Empty;
        if (!ContextMenuTweaks.TerminalAvailable())
        {
            _hint.Text = AppLang.L(
                "未检测到 Windows 终端（wt.exe），相关项开启前请先安装。",
                "Windows Terminal (wt.exe) not found — install it before enabling those items.");
        }
        else
        {
            _hint.Visible = false;
            _hint.Height = 0;
        }

        // Dock.Top：后加的在上 → common 最上，其次终端…；提示再压在分区之上
        body.Controls.Add(other);
        body.Controls.Add(edit);
        body.Controls.Add(terminal);
        body.Controls.Add(common);
        if (_hint.Visible)
            body.Controls.Add(_hint);
        _quickPage.Controls.Add(body);
    }

    private void BuildScanPage()
    {
        _scanPage.Dock = DockStyle.Fill;
        _scanPage.BackColor = AppTheme.Surface;
        _scanPage.Padding = new Padding(UiScale.S(12), UiScale.S(8), UiScale.S(12), UiScale.S(8));

        var tools = BuildScanTools();
        tools.Dock = DockStyle.Top;

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.MultiSelect = true;
        _list.CheckBoxes = false;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.ForeColor = AppTheme.TextMain;
        _list.Font = UiFit.UiFont;
        _list.Columns.Add(AppLang.L("名称", "Name"), 180);
        _list.Columns.Add(AppLang.L("状态", "Status"), 56);
        _list.Columns.Add(AppLang.L("建议", "Advice"), 72);
        _list.Columns.Add(AppLang.L("场景", "Scene"), 90);
        _list.Columns.Add(AppLang.L("类型", "Kind"), 56);
        _list.Columns.Add(AppLang.L("来源", "Source"), 72);
        _list.Columns.Add(AppLang.L("注册表", "Registry"), 260);
        _list.DoubleClick += (_, _) => ToggleSelected();
        _list.HandleCreated += (_, _) => UiBuffer.EnableListView(_list);

        _scanPage.Controls.Add(_list);
        _scanPage.Controls.Add(tools);
        UiBuffer.BindListViewColumnFit(_list, 6, 160);
    }

    private Panel BuildScanTools()
    {
        var btnH = UiFit.ControlHeight();
        var rowH = btnH + UiScale.S(10);
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = rowH * 2 + UiScale.S(8),
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 0, 0, UiScale.S(2)),
        };

        // 第 1 行：筛选（不与按钮抢同一行宽度）
        var filterRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = rowH,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, UiScale.S(4), 0, 0),
            Margin = new Padding(0),
        };
        UiBuffer.ConfigureNoScrollRow(filterRow);

        var sceneLbl = BarToolLabel(AppLang.L("场景", "Scene"));
        StyleToolCombo(_sceneFilter, btnH, UiScale.S(120));
        _sceneFilter.Items.AddRange([
            AppLang.L("全部场景", "All scenes"),
            AppLang.L("文件", "File"),
            AppLang.L("文件夹", "Folder"),
            AppLang.L("空白处", "Background"),
            AppLang.L("驱动器", "Drive"),
            AppLang.L("所有对象", "All objects"),
            AppLang.L("桌面空白", "Desktop bg"),
            AppLang.L("库空白处", "Library bg"),
            AppLang.L("文件夹(Folder)", "Folder key"),
        ]);
        _sceneFilter.SelectedIndex = 0;
        _sceneFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var adviceLbl = BarToolLabel(AppLang.L("建议", "Advice"));
        StyleToolCombo(_adviceFilter, btnH, UiScale.S(110));
        _adviceFilter.Items.AddRange([
            AppLang.L("全部建议", "All advice"),
            AppLang.L("可精简", "Can slim"),
            AppLang.L("建议保留", "Keep"),
            AppLang.L("按需", "Optional"),
            AppLang.L("勿动", "Keep"),
            AppLang.L("仅已禁用", "Disabled only"),
        ]);
        _adviceFilter.SelectedIndex = 0;
        _adviceFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var searchLbl = BarToolLabel(AppLang.L("搜索", "Search"));
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.Font = UiFit.UiFont;
        _search.Width = UiScale.S(180);
        _search.Height = btnH;
        _search.Margin = new Padding(0, 0, UiScale.S(12), 0);
        _search.TextChanged += (_, _) => ApplyFilter();

        filterRow.Controls.Add(sceneLbl);
        filterRow.Controls.Add(_sceneFilter);
        filterRow.Controls.Add(adviceLbl);
        filterRow.Controls.Add(_adviceFilter);
        filterRow.Controls.Add(searchLbl);
        filterRow.Controls.Add(_search);

        // 第 2 行：动作按钮（同高）
        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = rowH,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, UiScale.S(2), 0, 0),
            Margin = new Padding(0),
        };
        UiBuffer.ConfigureNoScrollRow(actionRow);

        _scanButtons =
        [
            ToolBtn(AppLang.L("启用", "Enable"), () => SetSelected(true), btnH),
            ToolBtn(AppLang.L("禁用", "Disable"), () => SetSelected(false), btnH),
            ToolBtn(AppLang.L("Server 精简", "Server slim"), ApplyServerSlim, btnH),
            ToolBtn(AppLang.L("打开位置", "Open key"), OpenSelected, btnH),
            ToolBtn(AppLang.L("刷新", "Refresh"), RefreshScan, btnH),
        ];
        foreach (var b in _scanButtons)
        {
            b.Margin = new Padding(0, 0, UiScale.S(8), 0);
            actionRow.Controls.Add(b);
        }

        // 后 Add 的 Dock.Top 靠上：filter → action
        bar.Controls.Add(actionRow);
        bar.Controls.Add(filterRow);

        void SyncHeights()
        {
            var h = UiFit.ControlHeight();
            var rh = h + UiScale.S(10);
            StyleToolCombo(_sceneFilter, h, _sceneFilter.Width > 0 ? _sceneFilter.Width : UiScale.S(120));
            StyleToolCombo(_adviceFilter, h, _adviceFilter.Width > 0 ? _adviceFilter.Width : UiScale.S(110));
            _search.Height = h;
            foreach (var b in _scanButtons)
                UiFit.FitButton(b, h, minWidth: 64, padding: 22);
            filterRow.Height = rh;
            actionRow.Height = rh;
            bar.Height = rh * 2 + UiScale.S(8);
        }

        bar.HandleCreated += (_, _) => SyncHeights();
        SyncHeights();
        return bar;
    }

    private static Label BarToolLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = AppTheme.TextHeader,
        Margin = new Padding(0, UiScale.S(8), UiScale.S(4), 0),
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static void StyleToolCombo(ComboBox box, int height, int width)
    {
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.Font = UiFit.UiFont;
        box.FlatStyle = FlatStyle.Flat;
        box.IntegralHeight = false;
        box.BackColor = Color.White;
        box.ForeColor = AppTheme.TextMain;
        box.Width = width;
        box.Height = height;
        box.Margin = new Padding(0, 0, UiScale.S(12), 0);
    }

    private Button ToolBtn(string text, Action click, int height)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        UiFit.FitButton(b, height, minWidth: 64, padding: 22);
        b.Click += (_, _) => click();
        return b;
    }

    private void LoadQuickValues()
    {
        Bind(_takeOwn, ContextMenuTweaks.IsTakeOwnershipOn(), ContextMenuTweaks.SetTakeOwnership);
        Bind(_openCmd, ContextMenuTweaks.IsOpenCmdOn(), ContextMenuTweaks.SetOpenCmd);
        Bind(_openPs, ContextMenuTweaks.IsOpenPowerShellOn(), ContextMenuTweaks.SetOpenPowerShell);
        Bind(_openPsAdmin, ContextMenuTweaks.IsOpenPowerShellAdminOn(), ContextMenuTweaks.SetOpenPowerShellAdmin);
        Bind(_openWt, ContextMenuTweaks.IsOpenTerminalOn(), ContextMenuTweaks.SetOpenTerminal);
        Bind(_openWtAdmin, ContextMenuTweaks.IsOpenTerminalAdminOn(), ContextMenuTweaks.SetOpenTerminalAdmin);
        Bind(_copyPath, ContextMenuTweaks.IsCopyPathOn(), ContextMenuTweaks.SetCopyPath);
        Bind(_copyMoveTo, ContextMenuTweaks.IsCopyMoveToOn(), ContextMenuTweaks.SetCopyMoveTo);
        Bind(_quickOps, ContextMenuTweaks.IsQuickOpsMenuOn(), ContextMenuTweaks.SetQuickOpsMenu);
        Bind(_paint, ContextMenuTweaks.IsEditWithPaintOn(), ContextMenuTweaks.SetEditWithPaint);
        Bind(_notepad, ContextMenuTweaks.IsEditWithNotepadOn(), ContextMenuTweaks.SetEditWithNotepad);
        Bind(_blockShare, ContextMenuTweaks.IsBlockAccessMenuOn(), ContextMenuTweaks.SetBlockAccessMenu);
        Bind(_classicMenu, Win11DesktopTweaks.IsWin10ClassicContextMenuOn(), Win11DesktopTweaks.SetWin10ClassicContextMenu);
    }

    private void Bind(InstantToggleRow row, bool on, Action<bool> apply)
    {
        row.Bind(on, value =>
        {
            apply(value);
            _onChanged?.Invoke();
        });
    }

    private void RefreshScan()
    {
        UseWaitCursor = true;
        try
        {
            _items = ContextMenuScanHelper.ScanAll().ToList();
            _scanLoaded = true;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("右键菜单", "Context menu"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void ApplyFilter()
    {
        var q = _search.Text.Trim();
        var scene = _sceneFilter.SelectedItem as string ?? "";
        var advice = _adviceFilter.SelectedItem as string ?? "";
        var allScene = AppLang.L("全部场景", "All scenes");
        var allAdvice = AppLang.L("全部建议", "All advice");
        var disabledOnly = AppLang.L("仅已禁用", "Disabled only");

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in _items)
        {
            if (scene != allScene
                && !string.Equals(item.Scene, scene, StringComparison.OrdinalIgnoreCase))
                continue;

            if (advice == disabledOnly)
            {
                if (item.Enabled) continue;
            }
            else if (advice != allAdvice && !string.Equals(item.Advice, advice, StringComparison.OrdinalIgnoreCase))
                continue;

            if (q.Length > 0
                && item.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && item.RegistryPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && (item.Clsid?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
                continue;

            var row = new ListViewItem(item.Name) { Tag = item };
            row.SubItems.Add(item.Enabled
                ? AppLang.L("启用", "On")
                : AppLang.L("禁用", "Off"));
            row.SubItems.Add(item.Advice);
            row.SubItems.Add(item.Scene);
            row.SubItems.Add(item.KindText);
            row.SubItems.Add(item.Source);
            row.SubItems.Add(item.RegistryPath);
            if (!item.Enabled) row.ForeColor = AppTheme.TextMute;
            if (item.Protected) row.ForeColor = AppTheme.TextMute;
            _list.Items.Add(row);
        }

        _list.EndUpdate();
    }

    private IEnumerable<ContextMenuEntry> SelectedEntries()
    {
        foreach (ListViewItem row in _list.SelectedItems)
        {
            if (row.Tag is ContextMenuEntry e)
                yield return e;
        }
    }

    private void ToggleSelected()
    {
        var e = SelectedEntries().FirstOrDefault();
        if (e is null) return;
        SetEnabledOne(e, !e.Enabled);
    }

    private void SetSelected(bool enable)
    {
        var list = SelectedEntries().ToList();
        if (list.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择菜单项。", "Select items first."),
                AppLang.L("右键菜单", "Context menu"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ok = 0;
        string? err = null;
        foreach (var e in list)
        {
            try
            {
                if (e.Protected) continue;
                ContextMenuScanHelper.SetEnabled(e, enable);
                e.Enabled = enable;
                ok++;
            }
            catch (Exception ex)
            {
                err ??= ex.Message;
            }
        }

        ApplyFilter();
        _onChanged?.Invoke();
        if (err is not null && ok == 0)
            MessageBox.Show(this, err, AppLang.L("右键菜单", "Context menu"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void SetEnabledOne(ContextMenuEntry e, bool enable)
    {
        try
        {
            ContextMenuScanHelper.SetEnabled(e, enable);
            e.Enabled = enable;
            ApplyFilter();
            _onChanged?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("右键菜单", "Context menu"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyServerSlim()
    {
        var candidates = _items.Where(ContextMenuScanHelper.IsSlimCandidate).Where(e => e.Enabled).ToList();
        if (candidates.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("当前没有可精简的已启用项。", "No slimable enabled items."),
                AppLang.L("右键菜单", "Context menu"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var msg = AppLang.Lf(
            "将禁用 {0} 个「可精简」菜单项（第三方/干扰项）。\r\n使用 LegacyDisable / Blocked，不删除注册表。继续？",
            "Disable {0} “Can slim” items (third-party/clutter).\r\nUses LegacyDisable / Blocked — no delete. Continue?",
            candidates.Count);
        if (MessageBox.Show(this, msg, AppLang.L("Server 精简", "Server slim"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        UseWaitCursor = true;
        try
        {
            var n = ContextMenuScanHelper.ApplyServerSlim(candidates);
            RefreshScan();
            _onChanged?.Invoke();
            if (n > 0)
            {
                MessageBox.Show(this,
                    AppLang.Lf("已精简 {0} 项。", "Slimmed {0} items.", n),
                    AppLang.L("Server 精简", "Server slim"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("Server 精简", "Server slim"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void OpenSelected()
    {
        var e = SelectedEntries().FirstOrDefault();
        if (e is null) return;
        try { ContextMenuScanHelper.OpenInRegedit(e); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("右键菜单", "Context menu"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
