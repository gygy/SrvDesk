namespace WinOpt;

internal sealed class OtherSettingsDialog : Form, IEmbeddedSettingsPage
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
    private readonly InstantToggleRow _prefetchRead = new("应用启动预取（只读）");
    private readonly NumericUpDown _port = new();
    private readonly NumericUpDown _prefetch = new();
    private bool _loaded;

    public OtherSettingsDialog()
    {
        Text = "系统服务";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 620);
        MinimumSize = new Size(720, 480);

        var body = ThemedSettingsChrome.CreateBodyPanel();

        var power = ThemedSettingsChrome.CreateSection("电源与休眠", [_hibernate, _fast]);
        power.Dock = DockStyle.Top;
        var remote = BuildRemoteSection();
        remote.Dock = DockStyle.Top;
        var svc = ThemedSettingsChrome.CreateSection("后台服务与内存", [_sysmain, _memComp, _prelaunch, _page, _ucpd]);
        svc.Dock = DockStyle.Top;
        var prefetch = BuildPrefetchSection();
        prefetch.Dock = DockStyle.Top;
        var search = BuildSearchTools();
        search.Dock = DockStyle.Top;

        body.Controls.Add(search);
        body.Controls.Add(prefetch);
        body.Controls.Add(svc);
        body.Controls.Add(remote);
        body.Controls.Add(power);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "系统服务",
            "休眠 · 远程桌面 · SysMain · 内存与预取 · 开关立即生效",
            body,
            "UCPD 为微软用户选择保护驱动，禁用后可修改默认浏览器等关联。",
            LoadValues);

        Shown += (_, _) =>
        {
            if (_loaded) return;
            _loaded = true;
            BeginInvoke(new Action(LoadValues));
        };
    }

    public void RefreshFromSystem() => LoadValues();

    private Panel BuildRemoteSection()
    {
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Width = 90;
        var portBtn = ThemedSettingsChrome.CreateButton("更改端口", false);
        portBtn.Size = new Size(96, 30);
        portBtn.Click += (_, _) =>
        {
            try
            {
                EasySettingsTweaks.SetRdpPort((int)_port.Value);
                MessageBox.Show("已修改 RDP 端口。请同步检查防火墙。", "远程桌面", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "远程桌面", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };

        var portRow = new FlowLayoutPanel
        {
            Height = 38,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 4, 0, 0),
        };
        portRow.Controls.Add(new Label
        {
            Text = "RDP 端口",
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0),
            ForeColor = AppTheme.TextMain,
        });
        portRow.Controls.Add(_port);
        portRow.Controls.Add(portBtn);

        var (card, host) = ThemedSettingsChrome.CreateSectionShell("远程桌面", 34 + 3 * 38 + 16);
        _ra.Dock = DockStyle.Top;
        _rdp.Dock = DockStyle.Top;
        host.Controls.Add(portRow);
        host.Controls.Add(_ra);
        host.Controls.Add(_rdp);
        return card;
    }

    private Panel BuildPrefetchSection()
    {
        _prefetch.Minimum = 32;
        _prefetch.Maximum = 4096;
        _prefetch.Width = 90;
        var pfBtn = ThemedSettingsChrome.CreateButton("应用", true);
        pfBtn.Size = new Size(72, 30);
        pfBtn.Click += (_, _) =>
        {
            EasySettingsTweaks.SetMaxPrefetchFiles((int)_prefetch.Value);
            MessageBox.Show("已写入最大预取文件数。", "预取", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var pfRow = new FlowLayoutPanel
        {
            Height = 38,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 4, 0, 0),
        };
        pfRow.Controls.Add(new Label
        {
            Text = "最大预取文件数",
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0),
        });
        pfRow.Controls.Add(_prefetch);
        pfRow.Controls.Add(pfBtn);

        _prefetchRead.Enabled = false;
        var (card, host) = ThemedSettingsChrome.CreateSectionShell("预取设置", 34 + 2 * 38 + 16);
        _prefetchRead.Dock = DockStyle.Top;
        host.Controls.Add(pfRow);
        host.Controls.Add(_prefetchRead);
        return card;
    }

    private Panel BuildSearchTools()
    {
        var (card, body) = ThemedSettingsChrome.CreateSectionShell("搜索服务与防火墙", 120);
        var host = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4, 4, 4, 4),
        };
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
        row.Controls.Add(MkBtn("停止并禁止 Windows Search", () => EasySettingsTweaks.SetWindowsSearchEnabled(false)));
        row.Controls.Add(MkBtn("恢复 Windows Search", () => EasySettingsTweaks.SetWindowsSearchEnabled(true)));
        row.Controls.Add(MkBtn("添加搜索防火墙规则", EasySettingsTweaks.AddSearchFirewallRules));
        row.Controls.Add(MkBtn("移除搜索防火墙规则", EasySettingsTweaks.RemoveSearchFirewallRules));
        host.Controls.Add(row);
        body.Controls.Add(host);
        return card;
    }

    private static Button MkBtn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.AutoSize = true;
        b.Height = 32;
        b.Margin = new Padding(0, 0, 8, 8);
        b.Click += (_, _) =>
        {
            try
            {
                click();
                MessageBox.Show("已完成。", text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        return b;
    }

    private void LoadValues()
    {
        _hibernate.Bind(EasySettingsTweaks.IsHibernateDisabled(), EasySettingsTweaks.SetHibernate);
        _fast.Bind(EasySettingsTweaks.IsFastStartupDisabled(), EasySettingsTweaks.SetFastStartup);
        _rdp.Bind(EasySettingsTweaks.IsRdpEnabled(), EasySettingsTweaks.SetRdpEnabled);
        _ra.Bind(EasySettingsTweaks.IsRemoteAssistanceDisabled(), EasySettingsTweaks.SetRemoteAssistanceDisabled);
        _sysmain.Bind(EasySettingsTweaks.IsSysMainDisabled(), EasySettingsTweaks.SetSysMain);
        _memComp.Bind(EasySettingsTweaks.IsMemoryCompressionDisabled(), EasySettingsTweaks.SetMemoryCompressionDisabled);
        _prelaunch.Bind(EasySettingsTweaks.IsAppPrelaunchDisabled(), EasySettingsTweaks.SetAppPrelaunchDisabled);
        _page.Bind(EasySettingsTweaks.IsPageCombiningDisabled(), EasySettingsTweaks.SetPageCombiningDisabled);
        _ucpd.Bind(EasySettingsTweaks.IsUcpdDisabled(), EasySettingsTweaks.SetUcpdDisabled);
        _prefetchRead.Bind(EasySettingsTweaks.IsAppLaunchPrefetchOn(), _ => { });
        _port.Value = Math.Min(_port.Maximum, Math.Max(_port.Minimum, EasySettingsTweaks.GetRdpPort()));
        _prefetch.Value = Math.Min(_prefetch.Maximum, Math.Max(_prefetch.Minimum, EasySettingsTweaks.GetMaxPrefetchFiles()));
    }
}
