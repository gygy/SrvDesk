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
        ClientSize = new Size(460, 268);
        BackColor = AppTheme.SurfaceCard;
        Font = new Font("Microsoft YaHei UI", 9F);
        ShowInTaskbar = false;

        var body = new Label
        {
            Text = AppLang.L(
                "本程序是 Windows Server 优化助手：先识别用途，再给建议，确认后执行，并可回滚。\r\n" +
                "请以管理员身份运行；重要环境请先备份或创建还原点。\r\n" +
                "入口：工具 → 优化顾问。完整条款见「帮助」。\r\n\r\n" +
                "部分杀软可能误报。如遇误报，请将本程序加入信任/白名单；" +
                "若不放心，可通过在线查杀核对：",
                "SrvDesk is a Windows Server optimization assistant: profile → advice → confirm → apply → rollback.\r\n" +
                "Run as Administrator; back up or create a restore point on important machines.\r\n" +
                "Start at Tools → Optimization advisor. See Help for legal terms.\r\n\r\n" +
                "Some antivirus products may false-positive. If so, add this app to trust/whitelist; " +
                "or verify online at:"),
            ForeColor = AppTheme.TextMain,
            AutoSize = false,
            Location = new Point(24, 18),
            Size = new Size(412, 130),
        };

        var linkVt = new LinkLabel
        {
            Text = "https://www.virustotal.com",
            AutoSize = true,
            Location = new Point(24, 152),
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
        var linkDisclaimer = MakeLink(disclaimerTitle, 24, 182);
        linkDisclaimer.LinkClicked += (_, _) =>
            LegalDocumentDialog.Show(this, disclaimerTitle, "SrvDesk.DISCLAIMER.md");

        var linkPrivacy = MakeLink(privacyTitle, 100, 182);
        linkPrivacy.LinkClicked += (_, _) =>
            LegalDocumentDialog.Show(this, privacyTitle, "SrvDesk.PRIVACY.md");

        var ok = ThemedSettingsChrome.CreateButton(AppLang.L("知道了", "Got it"), true);
        ok.Size = new Size(88, 32);
        ok.Location = new Point(ClientSize.Width - 24 - 88, 214);
        ok.DialogResult = DialogResult.OK;
        AcceptButton = ok;
        CancelButton = ok;

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
