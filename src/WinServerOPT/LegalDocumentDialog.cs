using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SrvDesk;

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
            ScrollBars = ScrollBars.Vertical,
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
        var raw = LoadEmbeddedText(resourceLogicalName);
        var body = ToReadableText(raw, windowTitle: title);
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

    /// <summary>
    /// 将仓库 Markdown / 许可证原文转为对话框可读纯文本（去掉 #、**、链接语法等）。
    /// </summary>
    internal static string ToReadableText(string source, string? windowTitle = null)
    {
        if (string.IsNullOrWhiteSpace(source))
            return source;

        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var sb = new StringBuilder(source.Length);
        var skipNextBlank = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            // ATX 标题：# 标题 → 标题（窗口标题已有时跳过同名一级标题）
            var heading = Regex.Match(line, @"^(#{1,6})\s+(.*)$");
            if (heading.Success)
            {
                var level = heading.Groups[1].Value.Length;
                var text = heading.Groups[2].Value.Trim();
                text = StripInlineMarkdown(text);
                if (level == 1 &&
                    windowTitle is { Length: > 0 } title &&
                    string.Equals(text, title.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    skipNextBlank = true;
                    continue;
                }

                if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
                    sb.AppendLine();
                sb.AppendLine(text);
                if (level <= 2)
                    sb.AppendLine();
                skipNextBlank = false;
                continue;
            }

            if (skipNextBlank && string.IsNullOrWhiteSpace(line))
            {
                skipNextBlank = false;
                continue;
            }
            skipNextBlank = false;

            // 无序列表
            var bullet = Regex.Match(line, @"^(\s*)[-*+]\s+(.*)$");
            if (bullet.Success)
            {
                var indent = bullet.Groups[1].Value.Length >= 2 ? "  " : "";
                sb.Append(indent).Append("· ").AppendLine(StripInlineMarkdown(bullet.Groups[2].Value));
                continue;
            }

            // 水平线
            if (Regex.IsMatch(line, @"^\s*(-{3,}|\*{3,}|_{3,})\s*$"))
            {
                sb.AppendLine();
                continue;
            }

            sb.AppendLine(StripInlineMarkdown(line));
        }

        var textOut = sb.ToString().Replace("\n", "\r\n").Trim();
        textOut = Regex.Replace(textOut, @"(?:\r\n){3,}", "\r\n\r\n");
        return textOut;
    }

    private static string StripInlineMarkdown(string line)
    {
        if (string.IsNullOrEmpty(line))
            return line;

        // [文字](url) → 文字（绝对链接时附 URL）
        line = Regex.Replace(line, @"\[([^\]]+)\]\(([^)]+)\)", m =>
        {
            var label = m.Groups[1].Value.Trim();
            var url = m.Groups[2].Value.Trim();
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return $"{label}（{url}）";
            return label;
        });

        line = Regex.Replace(line, @"\*\*(.+?)\*\*", "$1");
        line = Regex.Replace(line, @"__(.+?)__", "$1");
        line = Regex.Replace(line, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "$1");
        line = Regex.Replace(line, @"`([^`]+)`", "$1");
        return line;
    }
}
