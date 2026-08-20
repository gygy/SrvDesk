namespace WinOpt;

internal sealed class PrivacySettingsDialog : Form
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

        var searchCard = ThemedSettingsChrome.CreateSectionCard("搜索与云内容", 210);
        var searchHost = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 28, 0, 0) };
        searchHost.Controls.Add(_cloud);
        searchHost.Controls.Add(_web);
        var fw = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        fw.Controls.Add(Btn("添加防火墙规则", EasySettingsTweaks.AddSearchFirewallRules));
        fw.Controls.Add(Btn("移除防火墙规则", EasySettingsTweaks.RemoveSearchFirewallRules));
        searchHost.Controls.Add(fw);
        var warn = new Label
        {
            Text = "搜索框输入可能上传至微软。勾选上方项并添加防火墙规则可减少上传，一般不影响 Edge 浏览器搜索。",
            AutoSize = false,
            Size = new Size(700, 36),
            ForeColor = AppTheme.TextMute,
        };
        searchHost.Controls.Add(warn);
        searchCard.Controls.Add(searchHost);

        var svcCard = Section("Windows Search 服务", 90);
        var svcHost = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 32, 8, 8) };
        var stop = ThemedSettingsChrome.CreateButton("停止并禁止 Windows Search 服务", false);
        stop.Size = new Size(320, 36);
        stop.Click += (_, _) => { EasySettingsTweaks.SetWindowsSearchEnabled(false); LoadValues(); };
        var start = ThemedSettingsChrome.CreateButton("恢复并允许 Windows Search 服务", false);
        start.Size = new Size(320, 36);
        start.Click += (_, _) => { EasySettingsTweaks.SetWindowsSearchEnabled(true); LoadValues(); };
        svcHost.Controls.Add(stop);
        svcHost.Controls.Add(start);
        svcCard.Controls.Add(svcHost);

        var leftCard = Section("隐私和安全（仅当前用户）", 280);
        var leftHost = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 28, 0, 0) };
        leftHost.Controls.Add(_history);
        var tip = new Label { Text = "以下为系统默认开启项，隐私场景建议关闭：", AutoSize = true, ForeColor = AppTheme.TextMute };
        leftHost.Controls.Add(tip);
        leftHost.Controls.Add(_ad);
        leftHost.Controls.Add(_lang);
        leftHost.Controls.Add(_track);
        leftHost.Controls.Add(_suggest);
        leftHost.Controls.Add(_ink);
        leftCard.Controls.Add(leftHost);

        var rightCard = Section("更新与其它", 180);
        var rightHost = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 28, 0, 0) };
        rightHost.Controls.Add(_delivery);
        rightHost.Controls.Add(_msrt);
        rightHost.Controls.Add(_major);
        rightCard.Controls.Add(rightHost);

        var cols = new TableLayoutPanel { Dock = DockStyle.Top, Height = 300, ColumnCount = 2 };
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        cols.Controls.Add(leftCard, 0, 0);
        cols.Controls.Add(rightCard, 1, 0);

        rightCard.Dock = DockStyle.Fill;
        leftCard.Dock = DockStyle.Fill;
        searchCard.Dock = DockStyle.Top;
        svcCard.Dock = DockStyle.Top;
        cols.Dock = DockStyle.Top;

        body.Controls.Add(cols);
        body.Controls.Add(svcCard);
        body.Controls.Add(searchCard);

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        Load += (_, _) => LoadValues();
    }

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

    private static Panel Section(string title, int height)
    {
        var card = new Panel
        {
            Height = height,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(10, 6, 10, 8),
            Margin = new Padding(0, 0, 8, 8),
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
            Height = 26,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = AppTheme.TextHeader,
        };
        card.Controls.Add(cap);
        return card;
    }

    private static Control Btn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.AutoSize = true;
        b.Height = 32;
        b.Margin = new Padding(4);
        b.Click += (_, _) =>
        {
            try { click(); MessageBox.Show("已完成。", text, MessageBoxButtons.OK, MessageBoxIcon.Information); }
            catch (Exception ex) { MessageBox.Show(ex.Message, text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        return b;
    }
}
