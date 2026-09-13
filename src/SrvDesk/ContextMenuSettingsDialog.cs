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
    private readonly Label _count = new();
    private readonly Label _detail = new();
    private readonly CheckBox _multi = new();
    private List<ContextMenuEntry> _items = [];
    private bool _scanLoaded;

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

        _hint.AutoSize = true;
        _hint.MaximumSize = new Size(640, 0);
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Margin = new Padding(4, 8, 4, 4);
        if (!ContextMenuTweaks.TerminalAvailable())
        {
            _hint.Text = AppLang.L(
                "未检测到 Windows 终端（wt.exe），相关项开启前请先安装。",
                "Windows Terminal (wt.exe) not found — install it before enabling those items.");
            body.Controls.Add(_hint);
        }

        body.Controls.Add(other);
        body.Controls.Add(edit);
        body.Controls.Add(terminal);
        body.Controls.Add(common);
        _quickPage.Controls.Add(body);

        Resize += (_, _) =>
            _hint.MaximumSize = new Size(Math.Max(280, ClientSize.Width - 80), 0);
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
        _list.SelectedIndexChanged += (_, _) => UpdateDetail();
        _list.DoubleClick += (_, _) => ToggleSelected();
        _list.HandleCreated += (_, _) => UiBuffer.EnableListView(_list);

        _scanPage.Controls.Add(_list);
        _scanPage.Controls.Add(tools);
        UiBuffer.BindListViewColumnFit(_list, 6, 160);
    }

    private Panel BuildScanTools()
    {
        var btnH = UiFit.ControlHeight();
        var row1 = btnH + UiScale.S(12);
        var bar = new Panel
        {
            Height = row1 + UiScale.S(28),
            BackColor = AppTheme.Surface,
        };

        var sceneLbl = new Label
        {
            Text = AppLang.L("场景", "Scene"),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _sceneFilter.DropDownStyle = ComboBoxStyle.DropDownList;
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

        var adviceLbl = new Label
        {
            Text = AppLang.L("建议", "Advice"),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _adviceFilter.DropDownStyle = ComboBoxStyle.DropDownList;
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

        var searchLbl = new Label
        {
            Text = AppLang.L("搜索", "Search"),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.Font = UiFit.UiFont;
        _search.TextChanged += (_, _) => ApplyFilter();

        _multi.Text = AppLang.L("多选", "Multi");
        _multi.AutoSize = true;
        _multi.ForeColor = AppTheme.TextMute;
        _multi.Checked = true;
        _multi.CheckedChanged += (_, _) => { _list.MultiSelect = _multi.Checked; };

        _count.AutoSize = true;
        _count.ForeColor = AppTheme.TextMute;

        _detail.AutoSize = false;
        _detail.AutoEllipsis = true;
        _detail.ForeColor = AppTheme.TextMute;
        _detail.TextAlign = ContentAlignment.MiddleLeft;
        _detail.Text = AppLang.L("双击切换；Server 精简只动「可精简」项。", "Double-click to toggle; Server slim only touches “Can slim”.");

        var buttons = new[]
        {
            ToolBtn(AppLang.L("启用", "Enable"), () => SetSelected(true), btnH),
            ToolBtn(AppLang.L("禁用", "Disable"), () => SetSelected(false), btnH),
            ToolBtn(AppLang.L("Server 精简", "Server slim"), ApplyServerSlim, btnH),
            ToolBtn(AppLang.L("打开位置", "Open key"), OpenSelected, btnH),
            ToolBtn(AppLang.L("刷新", "Refresh"), RefreshScan, btnH),
        };

        bar.Controls.Add(sceneLbl);
        bar.Controls.Add(_sceneFilter);
        bar.Controls.Add(adviceLbl);
        bar.Controls.Add(_adviceFilter);
        bar.Controls.Add(searchLbl);
        bar.Controls.Add(_search);
        bar.Controls.Add(_multi);
        bar.Controls.Add(_count);
        bar.Controls.Add(_detail);
        foreach (var b in buttons)
            bar.Controls.Add(b);

        void LayoutTools()
        {
            var gap = UiScale.S(6);
            var pad = UiScale.S(4);
            foreach (var b in buttons)
                UiFit.FitButton(b, btnH, minWidth: 64, padding: 22);

            var x = bar.ClientSize.Width - pad;
            for (var i = buttons.Length - 1; i >= 0; i--)
            {
                var b = buttons[i];
                x -= b.Width;
                b.Location = new Point(Math.Max(pad, x), UiScale.S(4));
                x -= gap;
            }

            var btnLeft = buttons[0].Left;
            var y1 = UiScale.S(8);
            sceneLbl.Location = new Point(0, y1);
            _sceneFilter.SetBounds(sceneLbl.Right + UiScale.S(4), UiScale.S(4), UiScale.S(110), UiScale.S(26));
            adviceLbl.Location = new Point(_sceneFilter.Right + UiScale.S(10), y1);
            _adviceFilter.SetBounds(adviceLbl.Right + UiScale.S(4), UiScale.S(4), UiScale.S(100), UiScale.S(26));
            searchLbl.Location = new Point(_adviceFilter.Right + UiScale.S(10), y1);

            _multi.Location = new Point(Math.Max(searchLbl.Right + UiScale.S(4), btnLeft - UiScale.S(70)), y1);
            var countW = _count.PreferredSize.Width;
            var countX = _multi.Left - countW - UiScale.S(10);
            _count.Visible = countX > searchLbl.Right + UiScale.S(90);
            if (_count.Visible)
                _count.Location = new Point(countX, y1);

            var searchRight = _count.Visible ? _count.Left - UiScale.S(8) : _multi.Left - UiScale.S(8);
            var searchW = Math.Max(UiScale.S(80), searchRight - (searchLbl.Right + UiScale.S(4)));
            _search.SetBounds(searchLbl.Right + UiScale.S(4), UiScale.S(4), searchW, UiScale.S(26));

            _detail.SetBounds(0, row1, Math.Max(80, bar.ClientSize.Width - pad), UiScale.S(24));
        }

        bar.Resize += (_, _) => LayoutTools();
        LayoutTools();
        return bar;
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
        var shown = 0;
        var slim = 0;
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
            if (ContextMenuScanHelper.IsSlimCandidate(item) && item.Enabled)
                slim++;
            _list.Items.Add(row);
            shown++;
        }

        _list.EndUpdate();
        _count.Text = AppLang.Lf("共 {0} · 显示 {1} · 可精简 {2}",
            "{0} total · {1} shown · {2} slimable", _items.Count, shown, slim);
        UpdateDetail();
    }

    private IEnumerable<ContextMenuEntry> SelectedEntries()
    {
        foreach (ListViewItem row in _list.SelectedItems)
        {
            if (row.Tag is ContextMenuEntry e)
                yield return e;
        }
    }

    private void UpdateDetail()
    {
        var sel = SelectedEntries().ToList();
        if (sel.Count == 0)
        {
            _detail.Text = AppLang.L(
                "双击切换；Server 精简只动「可精简」项，不删注册表。",
                "Double-click to toggle; Server slim only “Can slim”, no delete.");
            return;
        }

        if (sel.Count == 1)
        {
            var e = sel[0];
            _detail.Text = $"{e.Name}  ·  {e.Scene}  ·  {e.Source}  ·  {(e.Enabled ? AppLang.L("启用", "On") : AppLang.L("禁用", "Off"))}  ·  {e.RegistryPath}";
            return;
        }

        _detail.Text = AppLang.Lf("已选 {0} 项", "{0} selected", sel.Count);
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
            _detail.Text = AppLang.Lf("已精简 {0} 项。重新打开资源管理器窗口后生效更彻底。",
                "Slimmed {0} items. Reopen Explorer windows for full effect.", n);
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
