using System.Management;

namespace WinOpt;

internal sealed class DnsSwitcherDialog : Form, IEmbeddedSettingsPage
{
    private readonly ComboBox _preset = new();
    private readonly TextBox _primary = new();
    private readonly TextBox _secondary = new();
    private readonly CheckedListBox _adapters = new();
    private readonly Label _hint = new();

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
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(600, 440);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(16),
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 120,
            ColumnCount = 2,
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

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
        form.Controls.Add(new Label { Text = "预设", AutoSize = true, Padding = new Padding(0, 8, 0, 0), ForeColor = AppTheme.TextHeader }, 0, 0);
        form.Controls.Add(_preset, 1, 0);
        form.Controls.Add(new Label { Text = "首选", AutoSize = true, Padding = new Padding(0, 8, 0, 0), ForeColor = AppTheme.TextHeader }, 0, 1);
        form.Controls.Add(_primary, 1, 1);
        form.Controls.Add(new Label { Text = "备选", AutoSize = true, Padding = new Padding(0, 8, 0, 0), ForeColor = AppTheme.TextHeader }, 0, 2);
        form.Controls.Add(_secondary, 1, 2);

        _hint.Dock = DockStyle.Top;
        _hint.Height = 40;
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Text = "勾选要修改的网卡。默认勾选「已连接」的物理网卡；虚拟网卡默认不勾选。";

        var listCap = new Label
        {
            Text = "选择网卡",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            ForeColor = AppTheme.TextHeader,
        };

        _adapters.Dock = DockStyle.Fill;
        _adapters.CheckOnClick = true;
        _adapters.IntegralHeight = false;
        _adapters.BorderStyle = BorderStyle.FixedSingle;
        _adapters.BackColor = AppTheme.Surface;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
        };
        var selectUp = ThemedSettingsChrome.CreateButton("仅勾选已连接", false);
        selectUp.Size = new Size(120, 34);
        selectUp.Click += (_, _) => SelectConnectedOnly();
        var selectAll = ThemedSettingsChrome.CreateButton("全选", false);
        selectAll.Size = new Size(72, 34);
        selectAll.Margin = new Padding(8, 0, 0, 0);
        selectAll.Click += (_, _) => SetAllChecked(true);
        var clear = ThemedSettingsChrome.CreateButton("全不选", false);
        clear.Size = new Size(80, 34);
        clear.Margin = new Padding(8, 0, 0, 0);
        clear.Click += (_, _) => SetAllChecked(false);
        var apply = ThemedSettingsChrome.CreateButton("应用到勾选网卡", true);
        apply.Size = new Size(140, 34);
        apply.Margin = new Padding(16, 0, 0, 0);
        apply.Click += (_, _) => ApplyDns();
        var flush = ThemedSettingsChrome.CreateButton("仅刷新缓存", false);
        flush.Size = new Size(110, 34);
        flush.Margin = new Padding(8, 0, 0, 0);
        flush.Click += (_, _) =>
        {
            HostsFileHelper.FlushDns();
            MessageBox.Show(this, "已刷新 DNS 缓存。", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        actions.Controls.AddRange([selectUp, selectAll, clear, apply, flush]);

        card.Controls.Add(_adapters);
        card.Controls.Add(actions);
        card.Controls.Add(listCap);
        card.Controls.Add(_hint);
        card.Controls.Add(form);
        body.Controls.Add(card);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "DNS 设置",
            "仅修改勾选的网卡 · 虚拟网卡默认不选",
            body,
            "DHCP 模式会对勾选网卡恢复自动获取 DNS。",
            RefreshAdapters);

        Shown += (_, _) =>
        {
            if (_adapters.Items.Count == 0) RefreshAdapters();
        };
    }

    public void RefreshFromSystem() => RefreshAdapters();

    private void RefreshAdapters()
    {
        _adapters.BeginUpdate();
        _adapters.Items.Clear();
        try
        {
            foreach (var a in DnsAdapterHelper.ListIpEnabledAdapters())
            {
                var i = _adapters.Items.Add(a);
                // 默认只勾选已连接、已启用 IP、且不像虚拟网卡的项
                _adapters.SetItemChecked(i, a.IsUp && a.IpEnabled && !a.LikelyVirtual);
            }
            if (_adapters.Items.Count == 0)
                _hint.Text = "未找到已启用 IP 的网卡。";
            else
                _hint.Text = $"共 {_adapters.Items.Count} 块网卡。默认勾选已连接的物理网卡；可改选后点「应用到勾选网卡」。";
        }
        catch (Exception ex)
        {
            _hint.Text = "读取网卡失败：" + ex.Message;
        }
        finally
        {
            _adapters.EndUpdate();
        }
    }

    private void SelectConnectedOnly()
    {
        for (var i = 0; i < _adapters.Items.Count; i++)
        {
            if (_adapters.Items[i] is DnsAdapterInfo a)
                _adapters.SetItemChecked(i, a.IsUp && a.IpEnabled && !a.LikelyVirtual);
        }
    }

    private void SetAllChecked(bool on)
    {
        for (var i = 0; i < _adapters.Items.Count; i++)
            _adapters.SetItemChecked(i, on);
    }

    private void ApplyDns()
    {
        try
        {
            var indexes = new List<int>();
            var settingIds = new List<string>();
            var names = new List<string>();
            for (var i = 0; i < _adapters.Items.Count; i++)
            {
                if (!_adapters.GetItemChecked(i)) continue;
                if (_adapters.Items[i] is not DnsAdapterInfo a) continue;
                if (!a.IpEnabled)
                    throw new InvalidOperationException($"「{a.Name}」未启用 IP，无法设置 DNS。请先在网络连接中启用该网卡。");
                indexes.Add(a.ConfigIndex);
                if (a.SettingId.Length > 0) settingIds.Add(a.SettingId);
                names.Add(a.Name);
            }

            if (indexes.Count == 0 && settingIds.Count == 0)
                throw new InvalidOperationException("请先勾选要修改的网卡。");

            string[]? servers = null;
            if (_preset.SelectedIndex != 0)
            {
                servers = new[] { _primary.Text.Trim(), _secondary.Text.Trim() }
                    .Where(x => x.Length > 0)
                    .ToArray();
                if (servers.Length == 0) throw new InvalidOperationException("请填写 DNS 地址。");
            }

            var count = DnsAdapterHelper.ApplyDns(indexes, settingIds, servers);
            HostsFileHelper.FlushDns();
            RefreshAdapters();
            ApplyLog.Write($"DNS → {string.Join(", ", names)} / {_preset.Text}");
            MessageBox.Show(this,
                $"已应用到 {count} 块网卡：\r\n" + string.Join("\r\n", names),
                "DNS 切换", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (ManagementException ex)
        {
            MessageBox.Show(this, "WMI 操作失败：" + ex.Message, "DNS 切换", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "DNS 切换", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
