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
        Text = "系统服务";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 560);
        MinimumSize = new Size(720, 480);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 3,
            Padding = new Padding(0, 0, 0, 8),
        };
        for (var i = 0; i < 3; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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

        grid.Controls.Add(Tile("电源与休眠", _hibernate, _fast), 0, 0);
        grid.Controls.Add(Tile("远程桌面", _rdp, _ra, rdpExtra), 1, 0);
        grid.Controls.Add(Tile("后台服务", _sysmain, _memComp), 2, 0);
        grid.Controls.Add(Tile("内存优化", _prelaunch, _page), 0, 1);
        grid.Controls.Add(Tile("预取设置", pfRow, _prefetchRead), 1, 1);
        grid.Controls.Add(Tile("系统驱动", _ucpd), 2, 1);

        body.Controls.Add(grid);
        ThemedSettingsChrome.MountEmbedded(
            this,
            "系统服务",
            "休眠 · 远程桌面 · SysMain · 内存与预取 · 开关立即生效",
            body,
            "UCPD 为微软用户选择保护驱动，禁用后可修改默认浏览器等关联。",
            LoadValues);
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

    private static Panel Tile(string title, params Control[] children)
    {
        var card = ThemedSettingsChrome.CreateSectionCard(title);
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(4);
        card.AutoSize = true;
        card.Padding = new Padding(8, 32, 8, 10);

        var host = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
        };
        foreach (var c in children)
            host.Controls.Add(c);
        card.Controls.Add(host);
        return card;
    }
}
