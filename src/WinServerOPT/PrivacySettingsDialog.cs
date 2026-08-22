namespace WinOpt;

internal sealed class PrivacySettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly InstantToggleRow _cloud = new("搜索界面禁止云内容搜索（OneDrive / SharePoint / Outlook / 必应）");
    private readonly InstantToggleRow _web = new("搜索界面禁止 Web 搜索（仅当前用户）");
    private readonly InstantToggleRow _history = new("禁止本地存储搜索历史记录（仅当前用户）");
    private readonly InstantToggleRow _ad = new("允许应用使用广告 ID 展示个性化广告");
    private readonly InstantToggleRow _lang = new("允许网站通过访问语言列表显示本地相关内容");
    private readonly InstantToggleRow _track = new("允许 Windows 跟踪应用启动以改进搜索结果");
    private readonly InstantToggleRow _suggest = new("在设置应用中为我显示建议的内容");
    private readonly InstantToggleRow _ink = new("自定义墨迹书写和键入词典");
    private readonly InstantToggleRow _delivery = new("禁止 Windows 更新传递优化");
    private readonly InstantToggleRow _msrt = new("Windows 更新不包括恶意软件删除工具");
    private readonly InstantToggleRow _major = new("禁止 Win 大版本更新（暂停功能更新至 2035）");

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

        var (searchCard, searchHost) = ThemedSettingsChrome.CreateSectionShell("搜索与云内容", 210);
        var searchStack = ThemedSettingsChrome.CreateToggleStack();
        searchStack.Controls.Add(_cloud);
        searchStack.Controls.Add(_web);
        var fw = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Width = 700 };
        fw.Controls.Add(Btn("添加防火墙规则", EasySettingsTweaks.AddSearchFirewallRules));
        fw.Controls.Add(Btn("移除防火墙规则", EasySettingsTweaks.RemoveSearchFirewallRules));
        searchStack.Controls.Add(fw);
        var warn = new Label
        {
            Text = "搜索框输入可能上传至微软。勾选上方项并添加防火墙规则可减少上传，一般不影响 Edge 浏览器搜索。",
            AutoSize = false,
            Size = new Size(700, 40),
            ForeColor = AppTheme.TextMute,
        };
        searchStack.Controls.Add(warn);
        searchHost.Controls.Add(searchStack);

        var (svcCard, svcBody) = ThemedSettingsChrome.CreateSectionShell("Windows Search 服务", 100);
        var svcHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4),
        };
        var stop = ThemedSettingsChrome.CreateButton("停止并禁止 Windows Search 服务", false);
        stop.Size = new Size(320, 36);
        stop.Click += (_, _) => { EasySettingsTweaks.SetWindowsSearchEnabled(false); LoadValues(); };
        var start = ThemedSettingsChrome.CreateButton("恢复并允许 Windows Search 服务", false);
        start.Size = new Size(320, 36);
        start.Click += (_, _) => { EasySettingsTweaks.SetWindowsSearchEnabled(true); LoadValues(); };
        svcHost.Controls.Add(stop);
        svcHost.Controls.Add(start);
        svcBody.Controls.Add(svcHost);

        var (leftCard, leftBody) = ThemedSettingsChrome.CreateSectionShell("隐私和安全（仅当前用户）", 300);
        var leftStack = ThemedSettingsChrome.CreateToggleStack();
        leftStack.Controls.Add(_history);
        var tip = new Label
        {
            Text = "以下为系统默认开启项，隐私场景建议关闭：",
            AutoSize = false,
            Height = 28,
            Width = 400,
            ForeColor = AppTheme.TextMute,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        leftStack.Controls.Add(tip);
        leftStack.Controls.Add(_ad);
        leftStack.Controls.Add(_lang);
        leftStack.Controls.Add(_track);
        leftStack.Controls.Add(_suggest);
        leftStack.Controls.Add(_ink);
        leftBody.Controls.Add(leftStack);

        var (rightCard, rightBody) = ThemedSettingsChrome.CreateSectionShell("更新与其它", 180);
        var rightStack = ThemedSettingsChrome.CreateToggleStack();
        rightStack.Controls.Add(_delivery);
        rightStack.Controls.Add(_msrt);
        rightStack.Controls.Add(_major);
        rightBody.Controls.Add(rightStack);

        var cols = new TableLayoutPanel { Dock = DockStyle.Top, Height = 320, ColumnCount = 2 };
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        leftCard.Dock = DockStyle.Fill;
        rightCard.Dock = DockStyle.Fill;
        cols.Controls.Add(leftCard, 0, 0);
        cols.Controls.Add(rightCard, 1, 0);

        searchCard.Dock = DockStyle.Top;
        svcCard.Dock = DockStyle.Top;
        cols.Dock = DockStyle.Top;

        body.Controls.Add(cols);
        body.Controls.Add(svcCard);
        body.Controls.Add(searchCard);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "隐私与搜索",
            "搜索隐私 · 广告跟踪 · 更新传递 · 开关立即生效",
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
