namespace SrvDesk;

/// <summary>首次打开时的简短说明，少打扰。</summary>
internal sealed class FirstRunNoticeDialog : Form
{
    public FirstRunNoticeDialog()
    {
        Text = AppLang.L("使用说明", "Getting started");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.SurfaceCard;
        Font = UiFit.UiFont;
        ShowInTaskbar = false;

        var pad = UiScale.S(24);
        var contentW = UiScale.S(420);

        var bodyText = AppLang.L(
            "本程序是 Windows Server 优化助手：先识别用途，再给建议，确认后执行，并可回滚。\r\n" +
            "请以管理员身份运行；重要环境请先备份或创建还原点。\r\n" +
            "入口：工具 → 优化顾问。\r\n\r\n" +
            "部分杀软可能误报。如遇误报，请将本程序加入信任/白名单；" +
            "若不放心，可通过在线查杀核对：",
            "SrvDesk is a Windows Server optimization assistant: profile → advice → confirm → apply → rollback.\r\n" +
            "Run as Administrator; back up or create a restore point on important machines.\r\n" +
            "Start at Tools → Optimization advisor.\r\n\r\n" +
            "Some antivirus products may false-positive. If so, add this app to trust/whitelist; " +
            "or verify online at:");

        var bodyH = TextRenderer.MeasureText(
            bodyText, Font, new Size(contentW, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + UiScale.S(8);

        var body = new Label
        {
            Text = bodyText,
            ForeColor = AppTheme.TextMain,
            AutoSize = false,
            Location = new Point(pad, UiScale.S(18)),
            Size = new Size(contentW, Math.Max(UiScale.S(120), bodyH)),
        };

        var linkVt = new LinkLabel
        {
            Text = "https://www.virustotal.com",
            AutoSize = true,
            Location = new Point(pad, body.Bottom + UiScale.S(6)),
            LinkColor = AppTheme.Primary,
            ActiveLinkColor = AppTheme.PrimaryDark,
            VisitedLinkColor = AppTheme.Primary,
        };
        linkVt.LinkClicked += (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.virustotal.com",
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var disclaimerTitle = AppLang.L("免责声明", "Disclaimer");
        var privacyTitle = AppLang.L("隐私说明", "Privacy");
        var linkDisclaimer = MakeLink(disclaimerTitle, pad, linkVt.Bottom + UiScale.S(12));
        linkDisclaimer.LinkClicked += (_, _) =>
            LegalDocumentDialog.Show(this, disclaimerTitle, "SrvDesk.DISCLAIMER.md");

        var linkPrivacy = MakeLink(privacyTitle, pad + UiScale.S(88), linkVt.Bottom + UiScale.S(12));
        linkPrivacy.LinkClicked += (_, _) =>
            LegalDocumentDialog.Show(this, privacyTitle, "SrvDesk.PRIVACY.md");

        var ok = ThemedSettingsChrome.CreateButton(AppLang.L("知道了", "Got it"), true);
        UiFit.FitButton(ok, padding: 28);
        AcceptButton = ok;
        CancelButton = ok;
        ok.DialogResult = DialogResult.OK;

        var bottomY = Math.Max(linkDisclaimer.Bottom, linkPrivacy.Bottom) + UiScale.S(16);
        ClientSize = new Size(pad + contentW + pad, bottomY + ok.Height + pad);
        ok.Location = new Point(ClientSize.Width - pad - ok.Width, ClientSize.Height - pad - ok.Height);

        Controls.AddRange([body, linkVt, linkDisclaimer, linkPrivacy, ok]);
    }

    private static LinkLabel MakeLink(string text, int x, int y) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y),
            LinkColor = AppTheme.Primary,
            ActiveLinkColor = AppTheme.PrimaryDark,
            VisitedLinkColor = AppTheme.Primary,
        };
}
