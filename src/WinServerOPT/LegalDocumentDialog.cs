using System.Reflection;
using System.Text;

namespace WinOpt;

/// <summary>帮助菜单中的许可证 / 免责 / 隐私说明对话框。</summary>
internal sealed class LegalDocumentDialog : Form
{
    public LegalDocumentDialog(string title, string body)
    {
        Text = title;
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 480);
        MinimumSize = new Size(480, 360);
        BackColor = AppTheme.SurfaceCard;
        Font = new Font("Microsoft YaHei UI", 9F);
        ShowInTaskbar = false;

        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextMain,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            Text = body,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
        };

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = AppTheme.SurfaceCard,
        };

        var ok = ThemedSettingsChrome.CreateButton("关闭", true);
        ok.Size = new Size(88, 32);
        ok.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        ok.DialogResult = DialogResult.OK;
        bottom.Controls.Add(ok);
        bottom.Resize += (_, _) =>
        {
            ok.Location = new Point(bottom.ClientSize.Width - ok.Width - 16, 10);
        };
        ok.Location = new Point(bottom.ClientSize.Width - ok.Width - 16, 10);

        var bodyHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 16, 16, 8),
        };
        bodyHost.Controls.Add(box);

        AcceptButton = ok;
        CancelButton = ok;
        Controls.Add(bodyHost);
        Controls.Add(bottom);
    }

    public static void Show(IWin32Window? owner, string title, string resourceLogicalName)
    {
        var body = LoadEmbeddedText(resourceLogicalName);
        using var dlg = new LegalDocumentDialog(title, body);
        dlg.ShowDialog(owner);
    }

    private static string LoadEmbeddedText(string logicalName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(logicalName);
        if (stream is null)
            return $"（未能加载文档资源：{logicalName}）";

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd().Trim();
    }
}
