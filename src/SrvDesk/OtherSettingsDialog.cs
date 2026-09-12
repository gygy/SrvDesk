namespace SrvDesk;

/// <summary>高级设置：RDP 端口、预取文件数、Windows Search（嵌入主窗标签页）。</summary>
internal sealed class OtherSettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly NumericUpDown _port = new();
    private readonly NumericUpDown _prefetch = new();
    private readonly Label _prefetchTip = new();

    public OtherSettingsDialog()
    {
        Text = AppLang.L("高级设置", "Advanced settings");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(560, 400);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.Controls.Add(BuildSearchTools());
        body.Controls.Add(BuildPrefetchSection());
        body.Controls.Add(BuildRemoteSection());

        ThemedSettingsChrome.MountEmbedded(
            this,
            AppLang.L("高级设置", "Advanced settings"),
            AppLang.L("RDP 端口 · 预取 · Windows Search", "RDP port · Prefetch · Windows Search"),
            body,
            "",
            RefreshFromSystem);

        Load += (_, _) => RefreshFromSystem();
    }

    public bool SupportsApplyToSystem => false;
    public void ApplyToSystem() { }

    public bool ConsumeWarmLoadSkip() => false;

    public void RefreshFromSystem()
    {
        try
        {
            _port.Value = Clamp(_port, EasySettingsTweaks.GetRdpPort());
            _prefetch.Value = Clamp(_prefetch, EasySettingsTweaks.GetMaxPrefetchFiles());
            _prefetchTip.Text = AppLang.L("应用启动预取当前：", "App launch prefetch: ")
                + (EasySettingsTweaks.IsAppLaunchPrefetchOn()
                    ? AppLang.L("开启（只读）", "On (read-only)")
                    : AppLang.L("关闭（只读）", "Off (read-only)"));
        }
        catch
        {
            /* 读取失败时保留界面现有值 */
        }
    }

    private static decimal Clamp(NumericUpDown box, int value) =>
        Math.Min(box.Maximum, Math.Max(box.Minimum, value));

    private Panel BuildRemoteSection()
    {
        _port.Minimum = 1;
        _port.Maximum = 65535;
        _port.Width = 90;
        var portBtn = ThemedSettingsChrome.CreateButton(AppLang.L("更改端口", "Change port"), false);
        portBtn.Height = 30;
        portBtn.Click += (_, _) =>
        {
            try
            {
                EasySettingsTweaks.SetRdpPort((int)_port.Value);
                MessageBox.Show(this,
                    AppLang.L("已修改 RDP 端口。请同步检查防火墙。", "RDP port changed. Check the firewall too."),
                    AppLang.L("远程桌面", "Remote Desktop"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, AppLang.L("远程桌面", "Remote Desktop"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var portRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(4, 4, 0, 0),
            Margin = new Padding(0, 0, 0, 4),
        };
        UiBuffer.ConfigureNoScrollRow(portRow);
        portRow.Controls.Add(new Label
        {
            Text = AppLang.L("RDP 端口", "RDP port"),
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0),
            ForeColor = AppTheme.TextMain,
        });
        portRow.Controls.Add(_port);
        portRow.Controls.Add(portBtn);

        var (card, host) = ThemedSettingsChrome.CreateSectionShell(AppLang.L("远程桌面端口", "Remote Desktop port"));
        host.Controls.Add(portRow);
        return card;
    }

    private Panel BuildPrefetchSection()
    {
        _prefetch.Minimum = 32;
        _prefetch.Maximum = 4096;
        _prefetch.Width = 90;
        var pfBtn = ThemedSettingsChrome.CreateButton(AppLang.L("应用", "Apply"), true);
        pfBtn.Height = 30;
        pfBtn.Click += (_, _) =>
        {
            EasySettingsTweaks.SetMaxPrefetchFiles((int)_prefetch.Value);
            MessageBox.Show(this,
                AppLang.L("已写入最大预取文件数。", "Max prefetch files saved."),
                AppLang.L("预取", "Prefetch"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var pfRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(4, 4, 0, 0),
            Margin = new Padding(0, 0, 0, 4),
        };
        UiBuffer.ConfigureNoScrollRow(pfRow);
        pfRow.Controls.Add(new Label
        {
            Text = AppLang.L("最大预取文件数", "Max prefetch files"),
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0),
        });
        pfRow.Controls.Add(_prefetch);
        pfRow.Controls.Add(pfBtn);

        _prefetchTip.AutoSize = true;
        _prefetchTip.ForeColor = AppTheme.TextMute;
        _prefetchTip.Margin = new Padding(4, 0, 0, 6);

        var (card, host) = ThemedSettingsChrome.CreateSectionShell(AppLang.L("预取设置", "Prefetch"));
        host.Controls.Add(_prefetchTip);
        host.Controls.Add(pfRow);
        return card;
    }

    private Panel BuildSearchTools()
    {
        var (card, body) = ThemedSettingsChrome.CreateSectionShell(
            AppLang.L("搜索服务与防火墙", "Search service & firewall"));
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 4, 0, 0) };
        row.Controls.Add(MkBtn(AppLang.L("停止 Windows Search", "Stop Windows Search"),
            () => EasySettingsTweaks.SetWindowsSearchEnabled(false)));
        row.Controls.Add(MkBtn(AppLang.L("恢复 Windows Search", "Restore Windows Search"),
            () => EasySettingsTweaks.SetWindowsSearchEnabled(true)));
        row.Controls.Add(MkBtn(AppLang.L("添加搜索防火墙规则", "Add search firewall rules"),
            EasySettingsTweaks.AddSearchFirewallRules));
        row.Controls.Add(MkBtn(AppLang.L("移除搜索防火墙规则", "Remove search firewall rules"),
            EasySettingsTweaks.RemoveSearchFirewallRules));
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
                MessageBox.Show(this, AppLang.L("已完成。", "Done."), text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        return b;
    }
}
