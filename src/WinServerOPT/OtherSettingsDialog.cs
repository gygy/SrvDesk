namespace WinOpt;

internal sealed class OtherSettingsDialog : Form
{
    private readonly InstantToggleRow _hibernate = new("禁用系统休眠");
    private readonly InstantToggleRow _fast = new("禁用快速启动");
    private readonly InstantToggleRow _rdp = new("启用远程桌面");
    private readonly InstantToggleRow _ra = new("禁用远程协助");
    private readonly InstantToggleRow _sysmain = new("禁用 SysMain 服务");
    private readonly InstantToggleRow _memComp = new("禁用内存压缩");
    private readonly InstantToggleRow _prelaunch = new("禁用应用预启动");
    private readonly InstantToggleRow _page = new("禁用内存页面合并");
    private readonly InstantToggleRow _ucpd = new("禁止微软 UCPD 驱动");
    private readonly NumericUpDown _port = new();
    private readonly NumericUpDown _prefetch = new();
    private readonly InstantToggleRow _prefetchRead = new("应用启动预取（只读）");

    public OtherSettingsDialog()
    {
        Text = "其他设置";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 620);
        MinimumSize = new Size(780, 520);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        var header = ThemedSettingsChrome.CreateHeader("其他设置", "系统功能调节 · 防火墙 / 日志 / 远程端口");
        var footer = ThemedSettingsChrome.CreateFooter(this, "开关立即生效。UCPD 为微软用户选择保护驱动，禁用后可改默认浏览器等关联。", LoadValues);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = AppTheme.Surface };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
        };
        for (var i = 0; i < 4; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, i == 3 ? 28 : 24));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33));

        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Width = 80;
        var portBtn = ThemedSettingsChrome.CreateButton("更改端口", false);
        portBtn.AutoSize = true;
        portBtn.Click += (_, _) =>
        {
            try
            {
                EasySettingsTweaks.SetRdpPort((int)_port.Value);
                MessageBox.Show("已修改 RDP 端口。请同步检查防火墙。", "远程桌面", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "远程桌面", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        var rdpExtra = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        rdpExtra.Controls.Add(new Label { Text = "端口", AutoSize = true, Padding = new Padding(0, 8, 6, 0) });
        rdpExtra.Controls.Add(_port);
        rdpExtra.Controls.Add(portBtn);

        _prefetch.Minimum = 32;
        _prefetch.Maximum = 4096;
        _prefetch.Width = 80;
        var pfBtn = ThemedSettingsChrome.CreateButton("应用", true);
        pfBtn.AutoSize = true;
        pfBtn.Click += (_, _) =>
        {
            EasySettingsTweaks.SetMaxPrefetchFiles((int)_prefetch.Value);
            MessageBox.Show("已写入最大预取文件数。", "预取", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var pfRow = new FlowLayoutPanel { AutoSize = true };
        pfRow.Controls.Add(_prefetch);
        pfRow.Controls.Add(pfBtn);

        _prefetchRead.Enabled = false;

        grid.Controls.Add(Tile("系统休眠", _hibernate), 0, 0);
        grid.Controls.Add(Tile("快速启动", _fast), 1, 0);
        grid.Controls.Add(Tile("远程设置", _rdp, _ra, rdpExtra), 2, 0);
        var side = BuildSideButtons();
        grid.Controls.Add(side, 3, 0);
        grid.SetRowSpan(side, 3);

        grid.Controls.Add(Tile("SysMain 服务", _sysmain), 0, 1);
        grid.Controls.Add(Tile("内存压缩", _memComp), 1, 1);
        grid.Controls.Add(Tile("应用预启动", _prelaunch), 2, 1);

        grid.Controls.Add(Tile("最大预取文件数", pfRow), 0, 2);
        grid.Controls.Add(Tile("内存页面合并", _page), 1, 2);
        grid.Controls.Add(Tile("应用启动预取", _prefetchRead), 2, 2);

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = AppTheme.Surface };
        _ucpd.Dock = DockStyle.Left;
        _ucpd.Width = 280;
        bottomBar.Controls.Add(_ucpd);

        body.Controls.Add(grid);
        body.Controls.Add(bottomBar);

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        Load += (_, _) => LoadValues();
    }

    private void LoadValues()
    {
        var s = Optimizer.Read(fullScan: false);
        _hibernate.Bind(s.DisableHibernate, EasySettingsTweaks.SetHibernate);
        _fast.Bind(s.DisableFastStartup, EasySettingsTweaks.SetFastStartup);
        _rdp.Bind(s.EnableRdp, EasySettingsTweaks.SetRdpEnabled);
        _ra.Bind(s.DisableRemoteAssistance, EasySettingsTweaks.SetRemoteAssistanceDisabled);
        _sysmain.Bind(s.DisableSysMain, EasySettingsTweaks.SetSysMain);
        _memComp.Bind(s.DisableMemoryCompression, EasySettingsTweaks.SetMemoryCompressionDisabled);
        _prelaunch.Bind(s.DisableAppPrelaunch, EasySettingsTweaks.SetAppPrelaunchDisabled);
        _page.Bind(s.DisablePageCombining, EasySettingsTweaks.SetPageCombiningDisabled);
        _ucpd.Bind(s.DisableUcpdDriver, EasySettingsTweaks.SetUcpdDisabled);
        _prefetchRead.Bind(EasySettingsTweaks.IsAppLaunchPrefetchOn(), _ => { });
        _port.Value = Math.Min(_port.Maximum, Math.Max(_port.Minimum, EasySettingsTweaks.GetRdpPort()));
        _prefetch.Value = Math.Min(_prefetch.Maximum, Math.Max(_prefetch.Minimum, EasySettingsTweaks.GetMaxPrefetchFiles()));
    }

    private Control BuildSideButtons()
    {
        var p = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(6),
            BackColor = AppTheme.SurfaceCard,
        };
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        p.Controls.Add(Side("防火墙状态...", EasySettingsTweaks.OpenFirewallStatus));
        p.Controls.Add(Side("防火墙规则设置...", EasySettingsTweaks.OpenFirewallRules));
        p.Controls.Add(Side("刷新 DNS 解析缓存", () => HostsFileHelper.FlushDns()));
        p.Controls.Add(Side("修改 HOSTS 文件", () =>
        {
            using var dlg = new HostsEditorDialog();
            dlg.ShowDialog(this);
        }));
        p.Controls.Add(Side("事件查看器...", () =>
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = "eventvwr.msc", UseShellExecute = true })));
        var clear = Side("清除系统日志", () =>
        {
            if (MessageBox.Show(this, "将清除 Application / System / Setup 日志，是否继续？",
                    "清除系统日志", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            EasySettingsTweaks.ClearEventLogs();
        });
        clear.ForeColor = Color.FromArgb(176, 42, 42);
        p.Controls.Add(clear);
        return p;
    }

    private static Panel Tile(string title, params Control[] children)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(8),
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
            Height = 24,
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            ForeColor = AppTheme.PrimaryDark,
        };
        var host = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        foreach (var c in children)
        {
            c.Dock = DockStyle.None;
            host.Controls.Add(c);
        }
        card.Controls.Add(host);
        card.Controls.Add(cap);
        return card;
    }

    private static Button Side(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.Width = 210;
        b.Height = 34;
        b.Margin = new Padding(4);
        b.Click += (_, _) =>
        {
            try { click(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        return b;
    }
}
