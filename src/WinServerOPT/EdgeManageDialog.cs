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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        // 内容紧凑，避免中间留白
        ClientSize = new Size(520, 360);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 4),
            BackColor = AppTheme.Surface,
            AutoScroll = false,
        };

        // 用 Top 堆叠 + 固定高度，避免 Fill 的 FlowLayoutPanel 把某行撑出大块空白
        var y = 0;
        void Add(Control c, int height, int gapAfter = 4)
        {
            c.Location = new Point(0, y);
            c.Width = body.ClientSize.Width - body.Padding.Horizontal;
            c.Height = height;
            c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            body.Controls.Add(c);
            y += height + gapAfter;
        }

        Add(MakeComponentRow("Microsoft Edge", _edgeStatus, _edgeVer), 28, 2);
        Add(MakeComponentRow("Edge WebView2", _wvStatus, _wvVer), 28, 2);
        Add(MakeComponentRow("Edge Core", _coreStatus, _coreVer), 28, 8);

        _disableUpdate.Text = "禁用 Edge 更新（取消勾选即恢复更新）";
        _disableUpdate.AutoSize = false;
        _disableUpdate.TextAlign = ContentAlignment.MiddleLeft;
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
        Add(_disableUpdate, 26, 4);

        var hint = new Label
        {
            Text = "提示：卸载 WebView2 可能导致部分应用打不开。安装优先 winget，失败则下载官方包。",
            AutoSize = false,
            ForeColor = AppTheme.TextMute,
        };
        Add(hint, 32, 8);

        _btnUnEdge = MkBtn("卸载 Edge", () => UninstallOne(EdgeComponentKind.Edge));
        _btnUnWv = MkBtn("卸载 WebView2", () => UninstallOne(EdgeComponentKind.WebView2));
        _btnUnCore = MkBtn("卸载 Edge Core", () => UninstallOne(EdgeComponentKind.EdgeCore));
        _btnUnAll = MkBtn("卸载所有", UninstallAll);

        _btnInEdge = MkBtn("安装 Edge", () => InstallOne(EdgeComponentKind.Edge));
        _btnInWv = MkBtn("安装 WebView2", () => InstallOne(EdgeComponentKind.WebView2));
        _btnInCore = MkBtn("恢复 Edge Core", () => InstallOne(EdgeComponentKind.EdgeCore));
        _btnInMissing = MkBtn("恢复缺失项", InstallMissing);

        Add(MakeSection("卸载", _btnUnEdge, _btnUnWv, _btnUnCore, _btnUnAll), 64, 6);
        Add(MakeSection("安装 / 恢复", _btnInEdge, _btnInWv, _btnInCore, _btnInMissing), 64, 0);

        body.Resize += (_, _) =>
        {
            var w = body.ClientSize.Width - body.Padding.Horizontal;
            foreach (Control c in body.Controls)
                c.Width = Math.Max(200, w);
        };

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
        var panel = new Panel { Height = 28 };
        var titleLbl = new Label
        {
            Text = title,
            AutoSize = false,
            Location = new Point(0, 4),
            Size = new Size(150, 22),
            ForeColor = AppTheme.TextHeader,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        status.Location = new Point(160, 4);
        version.Location = new Point(240, 4);
        panel.Controls.Add(titleLbl);
        panel.Controls.Add(status);
        panel.Controls.Add(version);
        return panel;
    }

    private static Control MakeSection(string caption, params Button[] buttons)
    {
        var wrap = new Panel { Height = 64 };
        wrap.Controls.Add(new Label
        {
            Text = caption,
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
            Location = new Point(0, 0),
        });

        var x = 0;
        const int top = 22;
        foreach (var b in buttons)
        {
            b.Location = new Point(x, top);
            b.Margin = Padding.Empty;
            wrap.Controls.Add(b);
            x += b.Width + 8;
        }
        return wrap;
    }

    private Button MkBtn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.AutoSize = false;
        b.Size = new Size(112, 32);
        b.Click += (_, _) => click();
        return b;
    }
}
