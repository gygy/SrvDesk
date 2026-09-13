namespace SrvDesk;

/// <summary>MSEdge 管理：状态、禁用/恢复更新、卸载与安装恢复 Edge / WebView2 / Edge Core。</summary>
internal sealed class EdgeManageDialog : Form
{
    private readonly Label _edgeStatus = MakeStatus();
    private readonly Label _edgeVer = MakeVersion();
    private readonly Label _wvStatus = MakeStatus();
    private readonly Label _wvVer = MakeVersion();
    private readonly Label _coreStatus = MakeStatus();
    private readonly Label _coreVer = MakeVersion();
    private readonly CheckBox _disableUpdate = new();
    private readonly Button _btnUnEdge;
    private readonly Button _btnUnWv;
    private readonly Button _btnUnCore;
    private readonly Button _btnUnAll;
    private readonly Button _btnInEdge;
    private readonly Button _btnInWv;
    private readonly Button _btnInCore;
    private readonly Button _btnInMissing;
    private bool _loading;

    public EdgeManageDialog()
    {
        Text = "MSEdge 管理";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiFit.UiFont;
        ClientSize = UiScale.Size(780, 520);
        MinimumSize = UiScale.Size(780, 520);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.AutoScroll = true;
        body.Padding = new Padding(UiScale.S(20), UiScale.S(14), UiScale.S(20), UiScale.S(10));

        // Dock Fill 吃 Padding；禁止 Location(0,y) 贴左缘
        var stack = ThemedSettingsChrome.CreateToggleStack();
        stack.Dock = DockStyle.Top;
        stack.AutoSize = true;
        stack.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var rowH = Math.Max(UiScale.S(32), UiFit.ControlHeight(UiFit.UiFontBold()) + UiScale.S(4));
        stack.Controls.Add(MakeComponentRow("Microsoft Edge", _edgeStatus, _edgeVer, rowH));
        stack.Controls.Add(MakeComponentRow("Edge WebView2", _wvStatus, _wvVer, rowH));
        stack.Controls.Add(MakeComponentRow("Edge Core", _coreStatus, _coreVer, rowH));

        _disableUpdate.Text = "禁用 Edge 更新（取消勾选即恢复更新）";
        _disableUpdate.AutoSize = false;
        _disableUpdate.Height = rowH;
        _disableUpdate.Font = UiFit.UiFont;
        _disableUpdate.TextAlign = ContentAlignment.MiddleLeft;
        _disableUpdate.Margin = new Padding(0, UiScale.S(10), 0, UiScale.S(4));
        _disableUpdate.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            if (!EnsureAdmin())
            {
                _loading = true;
                _disableUpdate.Checked = !_disableUpdate.Checked;
                _loading = false;
                return;
            }
            try
            {
                EdgeManageHelper.SetUpdatesDisabled(_disableUpdate.Checked);
                RefreshStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshStatus();
            }
        };
        stack.Controls.Add(_disableUpdate);

        var hint = new SingleLineLabel
        {
            Text = "卸载 WebView2 可能导致部分应用打不开。",
            ForeColor = AppTheme.TextMute,
            Font = UiFit.UiFontSmall,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = Math.Max(UiScale.S(24), UiFit.ControlHeight(UiFit.UiFontSmall)),
            Margin = new Padding(0, 0, 0, UiScale.S(10)),
        };
        stack.Controls.Add(hint);

        _btnUnEdge = MkBtn("卸载 Edge", () => UninstallOne(EdgeComponentKind.Edge));
        _btnUnWv = MkBtn("卸载 WebView2", () => UninstallOne(EdgeComponentKind.WebView2));
        _btnUnCore = MkBtn("卸载 Edge Core", () => UninstallOne(EdgeComponentKind.EdgeCore));
        _btnUnAll = MkBtn("卸载所有", UninstallAll);

        _btnInEdge = MkBtn("安装 Edge", () => InstallOne(EdgeComponentKind.Edge));
        _btnInWv = MkBtn("安装 WebView2", () => InstallOne(EdgeComponentKind.WebView2));
        _btnInCore = MkBtn("恢复 Edge Core", () => InstallOne(EdgeComponentKind.EdgeCore));
        _btnInMissing = MkBtn("恢复缺失项", InstallMissing);

        stack.Controls.Add(MakeSection("卸载", _btnUnEdge, _btnUnWv, _btnUnCore, _btnUnAll));
        stack.Controls.Add(MakeSection("安装 / 恢复", _btnInEdge, _btnInWv, _btnInCore, _btnInMissing));

        body.Controls.Add(stack);
        body.Resize += (_, _) => ThemedSettingsChrome.StretchStackChildren(stack);

        ThemedSettingsChrome.MountModal(
            this,
            "MSEdge 管理",
            "",
            body,
            "",
            showHeader: false,
            onRefresh: RefreshStatus);

        Load += (_, _) =>
        {
            ThemedSettingsChrome.StretchStackChildren(stack);
            RefreshStatus();
        };
    }

    private void RefreshStatus()
    {
        var s = EdgeManageHelper.Query();
        Bind(s.Edge, _edgeStatus, _edgeVer);
        Bind(s.WebView2, _wvStatus, _wvVer);
        Bind(s.EdgeCore, _coreStatus, _coreVer);
        _loading = true;
        _disableUpdate.Checked = s.UpdatesDisabled;
        _loading = false;

        _btnUnEdge.Enabled = s.Edge.Installed;
        _btnUnWv.Enabled = s.WebView2.Installed;
        _btnUnCore.Enabled = s.EdgeCore.Installed;
        _btnUnAll.Enabled = s.Edge.Installed || s.WebView2.Installed || s.EdgeCore.Installed;

        _btnInEdge.Enabled = !s.Edge.Installed;
        _btnInWv.Enabled = !s.WebView2.Installed;
        _btnInCore.Enabled = !s.EdgeCore.Installed;
        _btnInMissing.Enabled = !s.Edge.Installed || !s.WebView2.Installed || !s.EdgeCore.Installed;
    }

    private static void Bind(EdgeComponentStatus c, Label status, Label ver)
    {
        status.Text = c.Installed ? "已安装" : "未安装";
        status.ForeColor = c.Installed ? Color.FromArgb(40, 140, 70) : AppTheme.TextMute;
        ver.Text = c.Installed ? c.Version : "";
        ver.Visible = c.Installed;
    }

    private void UninstallOne(EdgeComponentKind kind)
    {
        if (!EnsureAdmin()) return;
        var name = KindName(kind);
        var tip = kind == EdgeComponentKind.WebView2
            ? "卸载 WebView2 可能导致部分应用打不开。是否继续？"
            : $"确定卸载 {name}？";
        if (MessageBox.Show(this, tip, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        RunAction(() => EdgeManageHelper.Uninstall(kind), "卸载失败");
    }

    private void UninstallAll()
    {
        if (!EnsureAdmin()) return;
        if (MessageBox.Show(this,
                "将依次卸载 Edge、WebView2、Edge Core。\r\nWebView2 卸载可能导致部分应用打不开。是否继续？",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        RunAction(EdgeManageHelper.UninstallAll, "卸载失败");
    }

    private void InstallOne(EdgeComponentKind kind)
    {
        if (!EnsureAdmin()) return;
        var tip = kind == EdgeComponentKind.EdgeCore
            ? "Edge Core 无独立安装包时，将尝试通过安装 Microsoft Edge 来恢复。是否继续？"
            : $"确定安装/恢复 {KindName(kind)}？\r\n（优先 winget，失败则下载官方安装包，可能需要几分钟）";
        if (MessageBox.Show(this, tip, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        RunAction(() => EdgeManageHelper.Install(kind), "安装失败");
    }

    private void InstallMissing()
    {
        if (!EnsureAdmin()) return;
        if (MessageBox.Show(this,
                "将安装当前未安装的 Edge 相关组件（优先 winget，失败则下载官方包）。是否继续？",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        RunAction(EdgeManageHelper.InstallMissing, "安装失败");
    }

    private void RunAction(Func<string> action, string failTitle)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            var msg = action();
            RefreshStatus();
            MessageBox.Show(this, msg, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, failTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshStatus();
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private static string KindName(EdgeComponentKind kind) => kind switch
    {
        EdgeComponentKind.WebView2 => "Edge WebView2",
        EdgeComponentKind.EdgeCore => "Edge Core",
        _ => "Microsoft Edge",
    };

    private bool EnsureAdmin()
    {
        if (AdminHelper.IsRunningAsAdministrator()) return true;
        MessageBox.Show(this, "请以管理员身份运行后再操作。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private static Label MakeStatus() => new()
    {
        AutoSize = false,
        Font = UiFit.UiFontBold(),
        ForeColor = Color.FromArgb(40, 140, 70),
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static Label MakeVersion() => new()
    {
        AutoSize = false,
        Font = UiFit.UiFont,
        ForeColor = AppTheme.PrimaryDeep,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static Control MakeComponentRow(string title, Label status, Label version, int height)
    {
        var panel = new Panel
        {
            Height = height,
            Margin = new Padding(0, 0, 0, UiScale.S(4)),
        };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleLbl = new SingleLineLabel
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFont,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        status.Dock = DockStyle.Fill;
        version.Dock = DockStyle.Fill;
        grid.Controls.Add(titleLbl, 0, 0);
        grid.Controls.Add(status, 1, 0);
        grid.Controls.Add(version, 2, 0);
        panel.Controls.Add(grid);
        return panel;
    }

    private static Control MakeSection(string caption, params Button[] buttons)
    {
        var btnH = 0;
        foreach (var b in buttons)
        {
            UiFit.FitButton(b, padding: 28);
            btnH = Math.Max(btnH, b.Height);
        }
        if (btnH <= 0) btnH = UiFit.ControlHeight();

        var captionH = Math.Max(UiScale.S(22), UiFit.ControlHeight(UiFit.UiFontSmall));
        var wrap = new Panel
        {
            Height = captionH + btnH + UiScale.S(14),
            Margin = new Padding(0, 0, 0, UiScale.S(10)),
        };

        var captionLbl = new SingleLineLabel
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = captionH,
            ForeColor = AppTheme.TextMute,
            Font = UiFit.UiFontSmall,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = buttons.Length,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(0, UiScale.S(4), 0, 0),
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (var i = 0; i < buttons.Length; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / buttons.Length));
            buttons[i].Dock = DockStyle.Fill;
            buttons[i].Margin = new Padding(i == 0 ? 0 : UiScale.S(6), 0, 0, 0);
            grid.Controls.Add(buttons[i], i, 0);
        }

        wrap.Controls.Add(grid);
        wrap.Controls.Add(captionLbl);
        return wrap;
    }

    private static Button MkBtn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        UiFit.FitButton(b, padding: 28);
        b.Click += (_, _) => click();
        return b;
    }
}
