namespace WinOpt;

/// <summary>电源与服务高级工具：RDP 端口、预取文件数、Windows Search（开关项已并入主列表）。</summary>
internal sealed class OtherSettingsDialog : Form
{
    private readonly NumericUpDown _port = new();
    private readonly NumericUpDown _prefetch = new();

    public OtherSettingsDialog()
    {
        Text = "电源服务高级工具";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 420);
        MinimumSize = new Size(560, 360);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.Controls.Add(BuildSearchTools());
        body.Controls.Add(BuildPrefetchSection());
        body.Controls.Add(BuildRemoteSection());

        ThemedSettingsChrome.MountModal(
            this,
            "电源服务高级工具",
            "RDP 端口 · 预取 · Windows Search",
            body,
            "开关类项请在左侧「电源与服务」列表中勾选后点「应用到系统」。");

        Load += (_, _) =>
        {
            _port.Value = Math.Min(_port.Maximum, Math.Max(_port.Minimum, EasySettingsTweaks.GetRdpPort()));
            _prefetch.Value = Math.Min(_prefetch.Maximum, Math.Max(_prefetch.Minimum, EasySettingsTweaks.GetMaxPrefetchFiles()));
        };
    }

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
                MessageBox.Show(this, "已修改 RDP 端口。请同步检查防火墙。", "远程桌面",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "远程桌面", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var portRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 4, 0, 0),
            Margin = new Padding(0, 0, 0, 4),
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

        var (card, host) = ThemedSettingsChrome.CreateSectionShell("远程桌面端口");
        host.Controls.Add(portRow);
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
            MessageBox.Show(this, "已写入最大预取文件数。", "预取",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var pfRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 4, 0, 0),
            Margin = new Padding(0, 0, 0, 4),
        };
        pfRow.Controls.Add(new Label
        {
            Text = "最大预取文件数",
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0),
        });
        pfRow.Controls.Add(_prefetch);
        pfRow.Controls.Add(pfBtn);

        var tip = new Label
        {
            Text = "应用启动预取当前：" + (EasySettingsTweaks.IsAppLaunchPrefetchOn() ? "开启" : "关闭") + "（只读）",
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
            Margin = new Padding(4, 0, 0, 6),
        };

        var (card, host) = ThemedSettingsChrome.CreateSectionShell("预取设置");
        host.Controls.Add(tip);
        host.Controls.Add(pfRow);
        return card;
    }

    private Panel BuildSearchTools()
    {
        var (card, body) = ThemedSettingsChrome.CreateSectionShell("搜索服务与防火墙");
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 4, 0, 0) };
        row.Controls.Add(MkBtn("停止 Windows Search", () => EasySettingsTweaks.SetWindowsSearchEnabled(false)));
        row.Controls.Add(MkBtn("恢复 Windows Search", () => EasySettingsTweaks.SetWindowsSearchEnabled(true)));
        row.Controls.Add(MkBtn("添加搜索防火墙规则", EasySettingsTweaks.AddSearchFirewallRules));
        row.Controls.Add(MkBtn("移除搜索防火墙规则", EasySettingsTweaks.RemoveSearchFirewallRules));
        body.Controls.Add(row);
        return card;
    }

    private Button MkBtn(string text, Action click)
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
                MessageBox.Show(this, "已完成。", text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        return b;
    }
}
