using System.Management;
using System.Net.NetworkInformation;

namespace WinOpt;

internal sealed class DnsSwitcherDialog : Form
{
    private readonly ComboBox _preset = new();
    private readonly TextBox _primary = new();
    private readonly TextBox _secondary = new();
    private readonly Label _current = new();

    private static readonly (string Name, string Dns1, string Dns2)[] Presets =
    [
        ("DHCP（自动获取）", "", ""),
        ("Cloudflare", "1.1.1.1", "1.0.0.1"),
        ("Google", "8.8.8.8", "8.8.4.4"),
        ("Quad9", "9.9.9.9", "149.112.112.112"),
        ("阿里 DNS", "223.5.5.5", "223.6.6.6"),
        ("腾讯 DNS", "119.29.29.29", "182.254.116.116"),
        ("114 DNS", "114.114.114.114", "114.114.115.115"),
        ("AdGuard", "94.140.14.14", "94.140.15.15"),
    ];

    public DnsSwitcherDialog()
    {
        Text = "DNS 设置";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 360);
        MinimumSize = new Size(480, 320);

        var card = ThemedSettingsChrome.CreateSectionCard("网卡 DNS");
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Padding = new Padding(16, 36, 16, 16);

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0),
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _preset.DropDownStyle = ComboBoxStyle.DropDownList;
        _preset.Dock = DockStyle.Fill;
        foreach (var p in Presets) _preset.Items.Add(p.Name);
        _preset.SelectedIndex = 0;
        _preset.SelectedIndexChanged += (_, _) =>
        {
            var p = Presets[_preset.SelectedIndex];
            _primary.Text = p.Dns1;
            _secondary.Text = p.Dns2;
            _primary.Enabled = _preset.SelectedIndex != 0;
            _secondary.Enabled = _preset.SelectedIndex != 0;
        };

        _primary.Dock = DockStyle.Fill;
        _secondary.Dock = DockStyle.Fill;

        AddRow(form, "预设", _preset);
        AddRow(form, "首选", _primary);
        AddRow(form, "备选", _secondary);

        _current.AutoSize = false;
        _current.Height = 48;
        _current.Dock = DockStyle.Top;
        _current.ForeColor = AppTheme.TextMute;
        _current.Text = CurrentSummary();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0),
        };
        var apply = ThemedSettingsChrome.CreateButton("应用到网卡", true);
        apply.Size = new Size(120, 34);
        apply.Click += (_, _) => ApplyDns();
        var flush = ThemedSettingsChrome.CreateButton("仅刷新缓存", false);
        flush.Size = new Size(120, 34);
        flush.Click += (_, _) =>
        {
            HostsFileHelper.FlushDns();
            MessageBox.Show(this, "已刷新 DNS 缓存。", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        actions.Controls.Add(flush);
        actions.Controls.Add(apply);

        card.Controls.Add(actions);
        card.Controls.Add(_current);
        card.Controls.Add(form);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.Controls.Add(card);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "DNS 设置",
            "应用到已连接的以太网/无线网卡 · 修改后立即刷新缓存",
            body,
            "DHCP 模式会恢复为自动获取 DNS。");
    }

    private static void AddRow(TableLayoutPanel grid, string label, Control control)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
            Padding = new Padding(0, 6, 0, 0),
        }, 0, row);
        grid.Controls.Add(control, 1, row);
    }

    private static string CurrentSummary()
    {
        try
        {
            var lines = new List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;
                var dns = nic.GetIPProperties().DnsAddresses;
                if (dns.Count == 0) continue;
                lines.Add(nic.Name + "：" + string.Join(", ", dns.Take(3).Select(a => a.ToString())));
            }
            return lines.Count == 0 ? "未检测到已连接网卡的 DNS。" : string.Join("\r\n", lines.Take(3));
        }
        catch { return "无法读取当前 DNS。"; }
    }

    private void ApplyDns()
    {
        try
        {
            if (_preset.SelectedIndex == 0)
                SetAdapterDns(null);
            else
            {
                var servers = new[] { _primary.Text.Trim(), _secondary.Text.Trim() }
                    .Where(x => x.Length > 0)
                    .ToArray();
                if (servers.Length == 0) throw new InvalidOperationException("请填写 DNS 地址。");
                SetAdapterDns(servers);
            }
            HostsFileHelper.FlushDns();
            _current.Text = CurrentSummary();
            ApplyLog.Write("已切换 DNS：" + _preset.Text);
            MessageBox.Show(this, "已应用到已连接网卡。", "DNS 切换", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "DNS 切换", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void SetAdapterDns(string[]? servers)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");
        var any = false;
        foreach (ManagementObject adapter in searcher.Get())
        {
            using (adapter)
            {
                any = true;
                var result = (uint)adapter.InvokeMethod("SetDNSServerSearchOrder", new object?[] { servers });
                if (result is not 0 and not 1)
                    throw new InvalidOperationException("设置 DNS 失败，WMI 返回 " + result + "。");
            }
        }
        if (!any) throw new InvalidOperationException("未找到已启用 IP 的网卡。");
    }
}
