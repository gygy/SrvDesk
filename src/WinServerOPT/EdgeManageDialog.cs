namespace WinOpt;

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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(540, 420);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 14, 20, 8),
            BackColor = AppTheme.Surface,
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };

        stack.Controls.Add(MakeComponentRow("Microsoft Edge", _edgeStatus, _edgeVer));
        stack.Controls.Add(MakeComponentRow("Edge WebView2", _wvStatus, _wvVer));
        stack.Controls.Add(MakeComponentRow("Edge Core", _coreStatus, _coreVer));

        _disableUpdate.Text = "禁用 Edge 更新";
        _disableUpdate.AutoSize = true;
        _disableUpdate.Margin = new Padding(0, 12, 0, 4);
        _disableUpdate.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            if (!EnsureAdmin()) { _loading = true; _disableUpdate.Checked = !_disableUpdate.Checked; _loading = false; return; }
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

        var hint = new Label
        {
            Text = "提示：卸载 WebView2 可能导致部分应用打不开。安装优先用 winget，失败则下载官方包。取消勾选「禁用更新」即可恢复更新。",
            AutoSize = false,
            Width = 490,
            Height = 40,
            ForeColor = AppTheme.TextMute,
            Margin = new Padding(0, 2, 0, 10),
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

        stack.Controls.Add(MakeButtonRow("卸载", _btnUnEdge, _btnUnWv, _btnUnCore, _btnUnAll));
        stack.Controls.Add(MakeButtonRow("安装 / 恢复", _btnInEdge, _btnInWv, _btnInCore, _btnInMissing));

        body.Controls.Add(stack);

        ThemedSettingsChrome.MountModal(
            this,
            "MSEdge 管理",
            "卸载 / 安装恢复 · 禁用更新",
            body,
            "",
            showHeader: false,
            onRefresh: RefreshStatus);

        Load += (_, _) => RefreshStatus();
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
        // Edge Core：未安装时可点；若 Edge 也未装，会先装 Edge
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
        var name = KindName(kind);
        var tip = kind == EdgeComponentKind.EdgeCore
            ? "Edge Core 无独立安装包时，将尝试通过安装 Microsoft Edge 来恢复。是否继续？"
            : $"确定安装/恢复 {name}？\r\n（优先 winget，失败则下载官方安装包，可能需要几分钟）";
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
        AutoSize = true,
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        ForeColor = Color.FromArgb(40, 140, 70),
    };

    private static Label MakeVersion() => new()
    {
        AutoSize = true,
        Font = new Font("Microsoft YaHei UI", 9F),
        ForeColor = AppTheme.PrimaryDeep,
    };

    private static Control MakeComponentRow(string title, Label status, Label version)
    {
        var panel = new Panel { Width = 490, Height = 32, Margin = new Padding(0, 0, 0, 2) };
        panel.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Location = new Point(0, 6),
            ForeColor = AppTheme.TextHeader,
            Width = 160,
        });
        status.Location = new Point(170, 6);
        version.Location = new Point(250, 6);
        panel.Controls.Add(status);
        panel.Controls.Add(version);
        return panel;
    }

    private static Control MakeButtonRow(string caption, params Button[] buttons)
    {
        var wrap = new Panel { Width = 490, AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
        var label = new Label
        {
            Text = caption,
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
            Location = new Point(0, 0),
        };
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Location = new Point(0, 20),
            Width = 490,
            Margin = new Padding(0),
        };
        foreach (var b in buttons)
            flow.Controls.Add(b);
        wrap.Controls.Add(label);
        wrap.Controls.Add(flow);
        wrap.Height = 20 + 48;
        return wrap;
    }

    private Button MkBtn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.AutoSize = true;
        b.MinimumSize = new Size(108, 34);
        b.Margin = new Padding(0, 0, 8, 8);
        b.Click += (_, _) => click();
        return b;
    }
}
