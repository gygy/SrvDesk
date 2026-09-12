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
    private readonly Panel _body;
    private bool _loading;

    public EdgeManageDialog()
    {
        Text = "MSEdge 管理";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiFit.UiFont;

        _body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiScale.S(16), UiScale.S(12), UiScale.S(16), UiScale.S(8)),
            BackColor = AppTheme.Surface,
            AutoScroll = true,
        };

        var rowH = Math.Max(UiScale.S(28), UiFit.ControlHeight(UiFit.UiFontBold()));
        var stack = new List<(Control Control, int GapAfter)>();
        void Add(Control c, int height, int gapAfter = 4)
        {
            c.Width = Math.Max(200, _body.ClientSize.Width - _body.Padding.Horizontal);
            c.Height = height;
            c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _body.Controls.Add(c);
            stack.Add((c, gapAfter));
        }

        Add(MakeComponentRow("Microsoft Edge", _edgeStatus, _edgeVer, rowH), rowH, UiScale.S(2));
        Add(MakeComponentRow("Edge WebView2", _wvStatus, _wvVer, rowH), rowH, UiScale.S(2));
        Add(MakeComponentRow("Edge Core", _coreStatus, _coreVer, rowH), rowH, UiScale.S(10));

        _disableUpdate.Text = "禁用 Edge 更新（取消勾选即恢复更新）";
        _disableUpdate.AutoSize = false;
        _disableUpdate.Font = UiFit.UiFont;
        _disableUpdate.TextAlign = ContentAlignment.MiddleLeft;
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
        Add(_disableUpdate, rowH, UiScale.S(6));

        var hint = new SingleLineLabel
        {
            Text = "卸载 WebView2 可能导致部分应用打不开。",
            ForeColor = AppTheme.TextMute,
            Font = UiFit.UiFontSmall,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Add(hint, Math.Max(UiScale.S(22), UiFit.ControlHeight(UiFit.UiFontSmall)), UiScale.S(10));

        _btnUnEdge = MkBtn("卸载 Edge", () => UninstallOne(EdgeComponentKind.Edge));
        _btnUnWv = MkBtn("卸载 WebView2", () => UninstallOne(EdgeComponentKind.WebView2));
        _btnUnCore = MkBtn("卸载 Edge Core", () => UninstallOne(EdgeComponentKind.EdgeCore));
        _btnUnAll = MkBtn("卸载所有", UninstallAll);

        _btnInEdge = MkBtn("安装 Edge", () => InstallOne(EdgeComponentKind.Edge));
        _btnInWv = MkBtn("安装 WebView2", () => InstallOne(EdgeComponentKind.WebView2));
        _btnInCore = MkBtn("恢复 Edge Core", () => InstallOne(EdgeComponentKind.EdgeCore));
        _btnInMissing = MkBtn("恢复缺失项", InstallMissing);

        var unSection = MakeSection("卸载", _btnUnEdge, _btnUnWv, _btnUnCore, _btnUnAll);
        Add(unSection, unSection.Height, UiScale.S(10));
        var inSection = MakeSection("安装 / 恢复", _btnInEdge, _btnInWv, _btnInCore, _btnInMissing);
        Add(inSection, inSection.Height, UiScale.S(4));

        void RelayoutBody()
        {
            var w = Math.Max(200, _body.ClientSize.Width - _body.Padding.Horizontal);
            foreach (var (c, _) in stack)
                c.Width = w;
            LayoutSectionButtons(unSection);
            LayoutSectionButtons(inSection);

            var y = 0;
            foreach (var (c, gap) in stack)
            {
                c.Location = new Point(0, y);
                y += c.Height + gap;
            }
        }

        // 先按内容量宽高，再挂底栏，避免底栏盖住「安装 / 恢复」
        RelayoutBody();
        var footerReserve = UiScale.S(64);
        var contentBottom = stack.Count == 0 ? 0 : stack[stack.Count - 1].Control.Bottom;
        var contentW = MeasureContentWidth(unSection, inSection);
        var clientW = Math.Max(UiScale.S(720), contentW + _body.Padding.Horizontal + UiScale.S(24));
        var clientH = contentBottom + _body.Padding.Vertical + footerReserve + UiScale.S(16);
        ClientSize = new Size(clientW, Math.Max(UiScale.S(480), clientH));
        MinimumSize = Size;

        _body.Resize += (_, _) => RelayoutBody();

        ThemedSettingsChrome.MountModal(
            this,
            "MSEdge 管理",
            "",
            _body,
            "",
            showHeader: false,
            onRefresh: RefreshStatus);

        Load += (_, _) =>
        {
            RelayoutBody();
            // 若 DPI/底栏使内容仍溢出，再略增高（FixedDialog）
            var need = stack[stack.Count - 1].Control.Bottom + _body.Padding.Vertical + footerReserve + UiScale.S(8);
            if (ClientSize.Height < need)
                ClientSize = new Size(ClientSize.Width, need);
            RefreshStatus();
        };
    }

    private static int MeasureContentWidth(params Control[] sections)
    {
        var max = UiScale.S(640);
        foreach (var section in sections)
        {
            foreach (Control c in section.Controls)
            {
                if (c is Button b)
                    max = Math.Max(max, b.Right + UiScale.S(8));
            }
        }
        return max;
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
        Font = UiFit.UiFontBold(),
        ForeColor = Color.FromArgb(40, 140, 70),
    };

    private static Label MakeVersion() => new()
    {
        AutoSize = true,
        Font = UiFit.UiFont,
        ForeColor = AppTheme.PrimaryDeep,
    };

    private static Control MakeComponentRow(string title, Label status, Label version, int height)
    {
        var panel = new Panel { Height = height };
        var titleLbl = new SingleLineLabel
        {
            Text = title,
            Location = new Point(0, 0),
            Size = new Size(UiScale.S(150), height),
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFont,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        status.Location = new Point(UiScale.S(160), Math.Max(0, (height - status.PreferredHeight) / 2));
        version.Location = new Point(UiScale.S(240), Math.Max(0, (height - version.PreferredHeight) / 2));
        panel.Controls.Add(titleLbl);
        panel.Controls.Add(status);
        panel.Controls.Add(version);
        return panel;
    }

    private static Control MakeSection(string caption, params Button[] buttons)
    {
        var btnH = 0;
        foreach (var b in buttons)
            btnH = Math.Max(btnH, b.Height);
        if (btnH <= 0) btnH = UiFit.ControlHeight();

        var captionH = Math.Max(UiScale.S(20), UiFit.ControlHeight(UiFit.UiFontSmall) - UiScale.S(4));
        var gap = UiScale.S(8);
        var wrap = new Panel
        {
            Height = captionH + btnH + UiScale.S(10),
            Tag = buttons,
        };
        wrap.Controls.Add(new SingleLineLabel
        {
            Text = caption,
            ForeColor = AppTheme.TextMute,
            Font = UiFit.UiFontSmall,
            Location = new Point(0, 0),
            Size = new Size(UiScale.S(200), captionH),
            TextAlign = ContentAlignment.MiddleLeft,
        });

        var x = 0;
        var top = captionH + UiScale.S(2);
        foreach (var b in buttons)
        {
            b.Location = new Point(x, top);
            b.Margin = Padding.Empty;
            wrap.Controls.Add(b);
            x += b.Width + gap;
        }
        return wrap;
    }

    private static void LayoutSectionButtons(Control section)
    {
        if (section.Tag is not Button[] buttons || buttons.Length == 0) return;

        var captionH = Math.Max(UiScale.S(20), UiFit.ControlHeight(UiFit.UiFontSmall) - UiScale.S(4));
        var gap = UiScale.S(8);
        var btnH = 0;
        foreach (var b in buttons)
        {
            UiFit.FitButton(b, padding: 28);
            btnH = Math.Max(btnH, b.Height);
        }

        var avail = Math.Max(200, section.ClientSize.Width);
        var x = 0;
        var y = captionH + UiScale.S(2);
        var rowH = btnH;
        foreach (var b in buttons)
        {
            if (x > 0 && x + b.Width > avail)
            {
                x = 0;
                y += rowH + gap;
            }
            b.Location = new Point(x, y);
            x += b.Width + gap;
        }

        section.Height = y + rowH + UiScale.S(8);
    }

    private static Button MkBtn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        UiFit.FitButton(b, padding: 28);
        b.Click += (_, _) => click();
        return b;
    }
}
