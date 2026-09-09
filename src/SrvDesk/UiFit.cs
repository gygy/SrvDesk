namespace SrvDesk;

/// <summary>按实际文字宽度计算控件尺寸，避免中文被裁成半个字。</summary>
internal static class UiFit
{
    private static Font? _ui;
    private static Font? _uiSmall;
    private static Font? _uiScope;
    private static string? _family;

    /// <summary>优先雅黑 UI；Server 精简环境可能缺失，依次回退。</summary>
    public static string UiFontFamily
    {
        get
        {
            if (_family is not null) return _family;
            foreach (var name in new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Tahoma" })
            {
                if (FontFamilyExists(name))
                {
                    _family = name;
                    return _family;
                }
            }
            _family = SystemFonts.MessageBoxFont?.FontFamily.Name ?? "Microsoft Sans Serif";
            return _family;
        }
    }

    private static bool FontFamilyExists(string name)
    {
        try
        {
            foreach (var ff in FontFamily.Families)
            {
                if (string.Equals(ff.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    public static Font UiFont => _ui ??= new Font(UiFontFamily, 9F);
    public static Font UiFontSmall => _uiSmall ??= new Font(UiFontFamily, 8.5F);
    public static Font UiFontScope => _uiScope ??= new Font(UiFontFamily, 8F);

    public static Font UiFontBold(float size = 9F) => new(UiFontFamily, size, FontStyle.Bold);

    public static readonly TextFormatFlags SingleLineFlags =
        TextFormatFlags.SingleLine
        | TextFormatFlags.EndEllipsis
        | TextFormatFlags.NoPrefix
        | TextFormatFlags.NoPadding
        | TextFormatFlags.PreserveGraphicsClipping;

    public static int LineHeight(Font? font = null)
    {
        var f = font ?? UiFont;
        var h = TextRenderer.MeasureText("国Agyp", f, new Size(1024, 256),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Height;
        return Math.Max(f.Height, Math.Max(14, h));
    }

    /// <summary>
    /// 按钮 / 下拉 / 单行输入的最小可视高度。
    /// Flat + 雅黑在 26–28px 时常裁掉字脚；至少 LineHeight + 上下内边距。
    /// </summary>
    public static int ControlHeight(Font? font = null, int designMin = 30) =>
        Math.Max(UiScale.S(designMin), LineHeight(font ?? UiFont) + UiScale.S(14));

    public static int TextWidth(string text, Font? font = null) =>
        TextRenderer.MeasureText(text ?? "", font ?? UiFont,
            new Size(int.MaxValue, 256),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;

    /// <summary>按钮宽度：文字宽 + 内边距，且不小于 minWidth。</summary>
    public static int ButtonWidth(string text, Font? font = null, int minWidth = 72, int padding = 24) =>
        Math.Max(UiScale.S(minWidth), TextWidth(text, font ?? UiFont) + UiScale.S(padding));

    public static Size ButtonSize(string text, int height = 34, Font? font = null, int minWidth = 72, int padding = 24)
    {
        var f = font ?? UiFont;
        var h = Math.Max(height, ControlHeight(f));
        return new(ButtonWidth(text, f, minWidth, padding), h);
    }

    /// <summary>把按钮收紧到刚好能完整显示文字（高度不低于 ControlHeight）。</summary>
    public static void FitButton(Button b, int? height = null, int minWidth = 72, int padding = 24)
    {
        var h = height ?? (b.Height > 0 ? b.Height : ControlHeight(b.Font));
        b.Size = ButtonSize(b.Text, h, b.Font, minWidth, padding);
        b.TextAlign = ContentAlignment.MiddleCenter;
        b.UseCompatibleTextRendering = false;
        b.Padding = Padding.Empty;
    }

    /// <summary>下拉框高度与 ItemHeight，避免选中项文字被裁。</summary>
    public static void FitCombo(ComboBox box, Font? font = null)
    {
        if (font is not null) box.Font = font;
        var f = box.Font ?? UiFont;
        box.IntegralHeight = false;
        try { box.ItemHeight = Math.Max(18, LineHeight(f) + 2); } catch { /* DropDownList 外偶发 */ }
        box.Height = ControlHeight(f, 28);
    }

    /// <summary>Flat 按钮用 TextRenderer 居中绘制，避免雅黑默认偏上/偏下裁字。仅在创建时挂一次。</summary>
    public static void EnableCenteredFlatText(Button b)
    {
        b.TextAlign = ContentAlignment.MiddleCenter;
        b.UseCompatibleTextRendering = false;
        b.Padding = Padding.Empty;
        b.Paint += (_, e) =>
        {
            if (b.FlatStyle != FlatStyle.Flat) return;
            var g = e.Graphics;
            var bg = b.Enabled ? b.BackColor : ControlPaint.LightLight(b.BackColor);
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, b.ClientRectangle);
            if (b.FlatAppearance.BorderSize > 0)
            {
                using var pen = new Pen(b.FlatAppearance.BorderColor);
                g.DrawRectangle(pen, 0, 0, b.Width - 1, b.Height - 1);
            }
            var color = b.Enabled ? b.ForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(
                g,
                b.Text,
                b.Font,
                b.ClientRectangle,
                color,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.NoPadding);
        };
    }
}

/// <summary>
/// 单行省略号标签。WinForms 默认 Label 在 AutoSize=false 时仍会换行，
/// 行高不够时第二行会叠在标题或相邻标签上。
/// </summary>
internal sealed class SingleLineLabel : Label
{
    public SingleLineLabel()
    {
        AutoSize = false;
        UseMnemonic = false;
        AutoEllipsis = true;
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var bg = BackColor.A == 255
            ? BackColor
            : (Parent?.BackColor ?? SystemColors.Control);
        using (var brush = new SolidBrush(bg))
            g.FillRectangle(brush, ClientRectangle);

        if (string.IsNullOrEmpty(Text)) return;

        var flags = UiFit.SingleLineFlags;
        var align = TextAlign;
        if (align == ContentAlignment.MiddleCenter || align == ContentAlignment.TopCenter || align == ContentAlignment.BottomCenter)
            flags |= TextFormatFlags.HorizontalCenter;
        else if (align == ContentAlignment.MiddleRight || align == ContentAlignment.TopRight || align == ContentAlignment.BottomRight)
            flags |= TextFormatFlags.Right;
        else
            flags |= TextFormatFlags.Left;

        if (align == ContentAlignment.TopLeft || align == ContentAlignment.TopCenter || align == ContentAlignment.TopRight)
            flags |= TextFormatFlags.Top;
        else if (align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomCenter || align == ContentAlignment.BottomRight)
            flags |= TextFormatFlags.Bottom;
        else
            flags |= TextFormatFlags.VerticalCenter;

        var rect = ClientRectangle;
        if (rect.Width > 4)
            rect = new Rectangle(rect.X + 1, rect.Y, rect.Width - 2, rect.Height);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        TextRenderer.DrawText(g, Text, Font, rect, ForeColor, flags);
    }
}
