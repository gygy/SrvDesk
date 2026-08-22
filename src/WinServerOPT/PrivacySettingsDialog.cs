namespace WinOpt;

internal sealed class PrivacySettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly InstantToggleRow _cloud = new("禁止搜索云内容");
    private readonly InstantToggleRow _web = new("禁止搜索 Web（当前用户）");
    private readonly InstantToggleRow _history = new("禁止本地搜索历史");
    private readonly InstantToggleRow _ad = new("允许广告 ID 个性化");
    private readonly InstantToggleRow _lang = new("允许网站读取语言列表");
    private readonly InstantToggleRow _track = new("允许应用启动跟踪");
    private readonly InstantToggleRow _suggest = new("设置中显示建议内容");
    private readonly InstantToggleRow _ink = new("墨迹与键入个性化");
    private readonly InstantToggleRow _delivery = new("禁止更新传递优化");
    private readonly InstantToggleRow _msrt = new("更新不含恶意软件删除工具");
    private readonly InstantToggleRow _major = new("暂停功能更新至 2035");

    public PrivacySettingsDialog()
    {
        Text = "隐私与搜索";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(780, 640);
        MinimumSize = new Size(680, 520);

        var body = ThemedSettingsChrome.CreateBodyPanel();

        var (searchCard, searchHost) = ThemedSettingsChrome.CreateSectionShell("搜索与云内容");
        searchHost.Controls.Add(_cloud);
        searchHost.Controls.Add(_web);
        var fw = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        fw.Controls.Add(Btn("添加防火墙规则", EasySettingsTweaks.AddSearchFirewallRules));
        fw.Controls.Add(Btn("移除防火墙规则", EasySettingsTweaks.RemoveSearchFirewallRules));
        searchHost.Controls.Add(fw);
        var warn = new Label
        {
            Text = "搜索框输入可能上传至微软。勾选上方项并添加防火墙规则可减少上传。",
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            ForeColor = AppTheme.TextMute,
            Margin = new Padding(4, 8, 4, 4),
        };
        searchHost.Controls.Add(warn);

        var (svcCard, svcBody) = ThemedSettingsChrome.CreateSectionShell("Windows Search");
        svcBody.Controls.Add(Btn("停止并禁止 Windows Search", () =>
        {
            EasySettingsTweaks.SetWindowsSearchEnabled(false);
            LoadValues();
        }));
        svcBody.Controls.Add(Btn("恢复并允许 Windows Search", () =>
        {
            EasySettingsTweaks.SetWindowsSearchEnabled(true);
            LoadValues();
        }));

        var (leftCard, leftBody) = ThemedSettingsChrome.CreateSectionShell("隐私（当前用户）");
        leftBody.Controls.Add(_history);
        leftBody.Controls.Add(new Label
        {
            Text = "以下默认开启，隐私场景建议关闭：",
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
            Margin = new Padding(4, 4, 4, 4),
        });
        leftBody.Controls.Add(_ad);
        leftBody.Controls.Add(_lang);
        leftBody.Controls.Add(_track);
        leftBody.Controls.Add(_suggest);
        leftBody.Controls.Add(_ink);

        var (rightCard, rightBody) = ThemedSettingsChrome.CreateSectionShell("更新与其它");
        rightBody.Controls.Add(_delivery);
        rightBody.Controls.Add(_msrt);
        rightBody.Controls.Add(_major);

        var cols = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 0, 0, 8),
        };
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        leftCard.Dock = DockStyle.Fill;
        rightCard.Dock = DockStyle.Fill;
        cols.Controls.Add(leftCard, 0, 0);
        cols.Controls.Add(rightCard, 1, 0);

        body.Controls.Add(cols);
        body.Controls.Add(svcCard);
        body.Controls.Add(searchCard);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "隐私与搜索",
            "搜索隐私 · 广告跟踪 · 更新传递",
            body,
            "建议同时添加防火墙规则以拦截搜索上传。",
            LoadValues);
        Load += (_, _) => LoadValues();
    }

    public void RefreshFromSystem() => LoadValues();

    private void LoadValues()
    {
        var s = Optimizer.Read(fullScan: false);
        _cloud.Bind(s.DisableCloudSearch, v => { s.DisableCloudSearch = v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _web.Bind(s.DisableWebSearch, v => { s.DisableWebSearch = v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _history.Bind(s.DisableSearchHistory, v => { s.DisableSearchHistory = v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _ad.Bind(!s.DisableAdTracking, v => { s.DisableAdTracking = !v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _lang.Bind(!s.DisableWebsiteLangList, v => { s.DisableWebsiteLangList = !v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _track.Bind(!s.DisableAppLaunchTracking, v => { s.DisableAppLaunchTracking = !v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _suggest.Bind(!s.DisableSettingsSuggestions, v => { s.DisableSettingsSuggestions = !v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _ink.Bind(!s.DisableInkingPersonalization, v => { s.DisableInkingPersonalization = !v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _delivery.Bind(s.DisableDeliveryOpt, v => { s.DisableDeliveryOpt = v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _msrt.Bind(s.ExcludeMsrtFromWu, v => { s.ExcludeMsrtFromWu = v; EasySettingsTweaks.ApplyPrivacyBits(s); });
        _major.Bind(s.PauseFeatureUpdatesUntil2035, v => { s.PauseFeatureUpdatesUntil2035 = v; EasySettingsTweaks.ApplyPrivacyBits(s); });
    }

    private static Button Btn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.AutoSize = true;
        b.Height = 30;
        b.Margin = new Padding(0, 4, 8, 4);
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
}
