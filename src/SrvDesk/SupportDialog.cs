using System.Diagnostics;

namespace SrvDesk;

/// <summary>帮助 → 支持：作者与问题反馈入口（内容精简）。</summary>
internal sealed class SupportDialog : Form
{
    public SupportDialog()
    {
        Text = AppBrand.SupportDialogTitle;
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 220);
        BackColor = AppTheme.SurfaceCard;
        Font = new Font("Microsoft YaHei UI", 9F);
        ShowInTaskbar = false;

        var title = new Label
        {
            Text = $"{AppBrand.ShortName}  v{AppBrand.VersionText}",
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
            ForeColor = AppTheme.PrimaryDeep,
            AutoSize = true,
            Location = new Point(28, 28),
        };

        var subtitle = new Label
        {
            Text = AppLang.L("Windows Server 桌面优化助手", "Windows Server desktop optimizer"),
            ForeColor = AppTheme.TextMute,
            AutoSize = true,
            Location = new Point(28, 58),
        };

        var author = new Label
        {
            Text = AppLang.L("作者", "Author"),
            ForeColor = AppTheme.TextMute,
            AutoSize = true,
            Location = new Point(28, 100),
        };
        var authorValue = new Label
        {
            Text = AppBrand.Author,
            ForeColor = AppTheme.TextMain,
            Font = new Font("Microsoft YaHei UI", 9.75F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(88, 99),
        };

        var feedback = new Label
        {
            Text = AppLang.L("反馈", "Feedback"),
            ForeColor = AppTheme.TextMute,
            AutoSize = true,
            Location = new Point(28, 132),
        };
        var link = new LinkLabel
        {
            Text = AppBrand.FeedbackUrl,
            AutoSize = true,
            Location = new Point(88, 132),
            LinkColor = AppTheme.Primary,
            ActiveLinkColor = AppTheme.PrimaryDark,
            VisitedLinkColor = AppTheme.Primary,
        };
        link.LinkClicked += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppBrand.FeedbackUrl,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, AppBrand.SupportDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var ok = ThemedSettingsChrome.CreateButton(AppLang.L("关闭", "Close"), true);
        ok.Size = new Size(88, 32);
        ok.Location = new Point(ClientSize.Width - 28 - 88, 168);
        ok.DialogResult = DialogResult.OK;
        AcceptButton = ok;
        CancelButton = ok;

        Controls.AddRange([title, subtitle, author, authorValue, feedback, link, ok]);
    }
}
