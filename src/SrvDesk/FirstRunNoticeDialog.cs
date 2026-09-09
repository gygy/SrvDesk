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
        ClientSize = new Size(420, 198);
        BackColor = AppTheme.SurfaceCard;
        Font = new Font("Microsoft YaHei UI", 9F);
        ShowInTaskbar = false;

        var body = new Label
        {
            Text = AppLang.L(
                "本程序是 Windows Server 优化与健康助手：先识别用途，再给建议，确认后执行，并可回滚。\r\n" +
                "请以管理员身份运行；重要环境请先备份或创建还原点。\r\n" +
                "入口：工具 → 健康总览。完整条款见「帮助」。",
                "SrvDesk is a Server optimization & health assistant: profile → advice → confirm → apply → rollback.\r\n" +
                "Run as Administrator; back up or create a restore point on important machines.\r\n" +
                "Start at Tools → Health overview. See Help for legal terms."),
            ForeColor = AppTheme.TextMain,
            AutoSize = false,
            Location = new Point(24, 22),
            Size = new Size(372, 72),
        };

        var disclaimerTitle = AppLang.L("免责声明", "Disclaimer");
        var privacyTitle = AppLang.L("隐私说明", "Privacy");
        var linkDisclaimer = MakeLink(disclaimerTitle, 24, 108);
        linkDisclaimer.LinkClicked += (_, _) =>
            LegalDocumentDialog.Show(this, disclaimerTitle, "SrvDesk.DISCLAIMER.md");

        var linkPrivacy = MakeLink(privacyTitle, 100, 108);
        linkPrivacy.LinkClicked += (_, _) =>
            LegalDocumentDialog.Show(this, privacyTitle, "SrvDesk.PRIVACY.md");

        var ok = ThemedSettingsChrome.CreateButton(AppLang.L("知道了", "Got it"), true);
        ok.Size = new Size(88, 32);
        ok.Location = new Point(ClientSize.Width - 24 - 88, 148);
        ok.DialogResult = DialogResult.OK;
        AcceptButton = ok;
        CancelButton = ok;

        Controls.AddRange([body, linkDisclaimer, linkPrivacy, ok]);
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
