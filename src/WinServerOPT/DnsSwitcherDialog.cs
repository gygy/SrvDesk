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
        Text = "DNS 切换";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 280);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        var header = ThemedSettingsChrome.CreateHeader("DNS 切换", "对齐 Optimizer Pinger · 应用到已连接的以太网/无线适配器");
        var footer = ThemedSettingsChrome.CreateFooter(this, "切换后会刷新 DNS 缓存。", showClose: false);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 8), BackColor = AppTheme.SurfaceCard };
        body.Controls.Add(new Label { Text = "预设", Location = new Point(8, 12), AutoSize = true });
        _preset.DropDownStyle = ComboBoxStyle.DropDownList;
        _preset.SetBounds(80, 8, 400, 26);
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

        body.Controls.Add(new Label { Text = "首选", Location = new Point(8, 52), AutoSize = true });
        _primary.SetBounds(80, 48, 400, 24);
        body.Controls.Add(new Label { Text = "备选", Location = new Point(8, 88), AutoSize = true });
        _secondary.SetBounds(80, 84, 400, 24);

        _current.SetBounds(8, 124, 480, 48);
        _current.ForeColor = AppTheme.TextMute;
        _current.Text = CurrentSummary();

        var apply = ThemedSettingsChrome.CreateButton("应用到网卡", true);
        apply.SetBounds(280, 180, 120, 34);
        apply.Click += (_, _) => ApplyDns();
        var flush = ThemedSettingsChrome.CreateButton("仅刷新缓存", false);
        flush.SetBounds(80, 180, 120, 34);
        flush.Click += (_, _) =>
        {
            HostsFileHelper.FlushDns();
            MessageBox.Show(this, "已刷新 DNS 缓存。", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        body.Controls.AddRange([_preset, _primary, _secondary, _current, apply, flush]);
        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
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
            {
                SetAdapterDns(null);
            }
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
