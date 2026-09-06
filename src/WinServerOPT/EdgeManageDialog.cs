namespace WinOpt;

/// <summary>MSEdge 管理：状态、禁用更新、卸载 Edge / WebView2 / Edge Core。</summary>
internal sealed class EdgeManageDialog : Form
{
    private readonly Label _edgeStatus = MakeStatus();
    private readonly Label _edgeVer = MakeVersion();
    private readonly Label _wvStatus = MakeStatus();
    private readonly Label _wvVer = MakeVersion();
    private readonly Label _coreStatus = MakeStatus();
    private readonly Label _coreVer = MakeVersion();
    private readonly CheckBox _disableUpdate = new();
    private bool _loading;

    public EdgeManageDialog()
    {
        Text = "MSEdge 管理";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 340);

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
            Text = "提示：卸载 WebView2 可能导致部分应用甚至系统组件打不开。",
            AutoSize = false,
            Width = 460,
            Height = 28,
            ForeColor = AppTheme.TextMute,
            Margin = new Padding(0, 2, 0, 12),
        };
        stack.Controls.Add(hint);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0),
        };
        buttons.Controls.Add(MkBtn("卸载 Edge", () => UninstallOne(EdgeComponentKind.Edge)));
        buttons.Controls.Add(MkBtn("卸载 WebView2", () => UninstallOne(EdgeComponentKind.WebView2)));
        buttons.Controls.Add(MkBtn("卸载 Edge Core", () => UninstallOne(EdgeComponentKind.EdgeCore)));
        buttons.Controls.Add(MkBtn("卸载所有", UninstallAll));
        stack.Controls.Add(buttons);

        body.Controls.Add(stack);

        ThemedSettingsChrome.MountModal(
            this,
            "MSEdge 管理",
            "卸载 Edge 组件 · 禁用更新",
            body,
            "",
            showHeader: false);

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
        var name = kind switch
        {
            EdgeComponentKind.WebView2 => "Edge WebView2",
            EdgeComponentKind.EdgeCore => "Edge Core",
            _ => "Microsoft Edge",
        };
        var tip = kind == EdgeComponentKind.WebView2
            ? "卸载 WebView2 可能导致部分应用打不开。是否继续？"
            : $"确定卸载 {name}？";
        if (MessageBox.Show(this, tip, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            Cursor = Cursors.WaitCursor;
            var msg = EdgeManageHelper.Uninstall(kind);
            RefreshStatus();
            MessageBox.Show(this, msg, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshStatus();
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void UninstallAll()
    {
        if (!EnsureAdmin()) return;
        if (MessageBox.Show(this,
                "将依次卸载 Edge、WebView2、Edge Core。\r\nWebView2 卸载可能导致部分应用打不开。是否继续？",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            Cursor = Cursors.WaitCursor;
            var msg = EdgeManageHelper.UninstallAll();
            RefreshStatus();
            MessageBox.Show(this, msg, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshStatus();
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

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
        var panel = new Panel { Width = 460, Height = 32, Margin = new Padding(0, 0, 0, 2) };
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
