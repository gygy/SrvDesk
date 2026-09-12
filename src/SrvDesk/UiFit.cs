using System.Runtime.CompilerServices;

namespace SrvDesk;

/// <summary>按实际文字宽度计算控件尺寸，避免中文被裁成半个字。</summary>
internal static class UiFit
{
    private static Font? _ui;
    private static Font? _uiSmall;
    private static Font? _uiScope;
    private static string? _family;
    private static readonly ConditionalWeakTable<Button, object> CenteredPaintHooked = new();

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

    /// <summary>测量用：保留字墨溢出边距，避免雅黑/高 DPI 量偏窄。</summary>
    private static readonly TextFormatFlags MeasureFlags =
        TextFormatFlags.SingleLine
        | TextFormatFlags.NoPrefix
        | TextFormatFlags.GlyphOverhangPadding;

    public static int LineHeight(Font? font = null)
    {
        var f = font ?? UiFont;
        var h = TextRenderer.MeasureText("国Agyp", f, new Size(1024, 256), MeasureFlags).Height;
        return Math.Max(f.Height, Math.Max(14, h));
    }

    /// <summary>
    /// 按钮 / 下拉 / 单行输入的最小可视高度。
    /// Flat + 雅黑在偏矮高度时常裁掉字脚；副屏 DPI 变化后更明显。
    /// </summary>
    public static int ControlHeight(Font? font = null, int designMin = 34) =>
        Math.Max(UiScale.S(designMin), LineHeight(font ?? UiFont) + UiScale.S(20));

    public static int TextWidth(string text, Font? font = null) =>
        TextRenderer.MeasureText(text ?? "", font ?? UiFont, new Size(int.MaxValue, 256), MeasureFlags).Width;

    /// <summary>按钮宽度：文字宽 + 内边距 + 安全边，且不小于 minWidth。</summary>
    public static int ButtonWidth(string text, Font? font = null, int minWidth = 72, int padding = 28)
    {
        var f = font ?? UiFont;
        // LineHeight/3：ClearType / 副屏 DPI 下 Measure 仍可能略窄
        var slack = Math.Max(UiScale.S(8), LineHeight(f) / 3);
        return Math.Max(UiScale.S(minWidth), TextWidth(text, f) + UiScale.S(padding) + slack);
    }

    public static Size ButtonSize(string text, int height = 34, Font? font = null, int minWidth = 72, int padding = 28)
    {
        var f = font ?? UiFont;
        var h = Math.Max(height, ControlHeight(f));
        return new(ButtonWidth(text, f, minWidth, padding), h);
    }

    /// <summary>把按钮收到刚好能完整显示文字（高度不低于 ControlHeight）。</summary>
    public static void FitButton(Button b, int? height = null, int minWidth = 72, int padding = 28)
    {
        if (b is null || string.IsNullOrEmpty(b.Text)) return;
        var f = b.Font ?? UiFont;
        var h = Math.Max(height ?? 0, ControlHeight(f));
        b.Size = ButtonSize(b.Text, h, f, minWidth, padding);
        b.TextAlign = ContentAlignment.MiddleCenter;
        b.UseCompatibleTextRendering = false;
        b.Padding = Padding.Empty;
        b.AutoSize = false;
    }

    /// <summary>DPI 变化后重算窗体内 Flat 文字按钮尺寸（跳过无文字/仅图标）。</summary>
    public static void RefitFlatTextButtons(Control root)
    {
        if (root is null) return;
        foreach (Control c in root.Controls)
        {
            if (c is Button b
                && b.FlatStyle == FlatStyle.Flat
                && !string.IsNullOrWhiteSpace(b.Text)
                && b.Image is null
                && b.BackgroundImage is null)
            {
                FitButton(b);
            }
            if (c.HasChildren)
                RefitFlatTextButtons(c);
        }
    }

    /// <summary>下拉框高度与 ItemHeight，避免选中项文字被裁。</summary>
    public static void FitCombo(ComboBox box, Font? font = null)
    {
        if (font is not null) box.Font = font;
        var f = box.Font ?? UiFont;
        box.IntegralHeight = false;
        try { box.ItemHeight = Math.Max(18, LineHeight(f) + 2); } catch { /* DropDownList 外偶发 */ }
        box.Height = ControlHeight(f, 30);
    }

    /// <summary>Flat 按钮用 TextRenderer 居中绘制，避免雅黑默认偏上/偏下裁字。仅在创建时挂一次。</summary>
    public static void EnableCenteredFlatText(Button b)
    {
        // FlatChromeButton 已在 OnPaint 自绘，勿再叠一层
        if (b is FlatChromeButton) return;
        if (CenteredPaintHooked.TryGetValue(b, out _))
            return;
        try { CenteredPaintHooked.Add(b, new object()); }
        catch (ArgumentException) { return; }
        b.TextAlign = ContentAlignment.MiddleCenter;
        b.UseCompatibleTextRendering = false;
        b.Padding = Padding.Empty;
        b.Paint += (_, e) => PaintFlatButtonFace(b, e.Graphics);
    }

    /// <summary>自绘 Flat 按钮：背景 + 边框 + 居中文字（供 FlatChromeButton / Paint 共用）。</summary>
    public static void PaintFlatButtonFace(Button b, Graphics g)
    {
        var bg = b.Enabled ? b.BackColor : ControlPaint.LightLight(b.BackColor);
        using (var brush = new SolidBrush(bg))
            g.FillRectangle(brush, b.ClientRectangle);
        if (b.FlatAppearance.BorderSize > 0)
        {
            using var pen = new Pen(b.FlatAppearance.BorderColor);
            g.DrawRectangle(pen, 0, 0, b.Width - 1, b.Height - 1);
        }

        if (string.IsNullOrEmpty(b.Text)) return;

        var color = b.Enabled ? b.ForeColor : SystemColors.GrayText;
        var font = b.Font ?? UiFont;
        const TextFormatFlags flags =
            TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding
            | TextFormatFlags.SingleLine;

        // 内缩 2px，避免边框吃字；用 NoPadding + 实测高度垂直居中，纠正雅黑光学偏上
        var bounds = Rectangle.Inflate(b.ClientRectangle, -2, -2);
        if (bounds.Width < 4 || bounds.Height < 4) return;

        var measured = TextRenderer.MeasureText(g, b.Text, font, new Size(bounds.Width, int.MaxValue), flags);
        var y = bounds.Y + Math.Max(0, (bounds.Height - measured.Height) / 2);
        // 雅黑在 GDI 下视觉偏上约 1px，略下移
        y += Math.Max(1, UiScale.S(1));
        if (y + measured.Height > bounds.Bottom)
            y = Math.Max(bounds.Y, bounds.Bottom - measured.Height);

        var textRect = new Rectangle(bounds.X, y, bounds.Width, measured.Height);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        TextRenderer.DrawText(g, b.Text, font, textRect, color, flags);
    }
}

/// <summary>
/// 完全自绘的 Flat 按钮：避开 Button 默认绘制在 Paint 之后又画一遍裁切文字。
/// </summary>
internal class FlatChromeButton : Button
{
    public FlatChromeButton()
    {
        FlatStyle = FlatStyle.Flat;
        UseCompatibleTextRendering = false;
        TextAlign = ContentAlignment.MiddleCenter;
        Padding = Padding.Empty;
        AutoSize = false;
        Cursor = Cursors.Hand;
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.Selectable,
            true);
        SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
        UpdateStyles();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // 由 OnPaint 统一填底，避免闪烁与双重绘制
    }

    protected override void OnPaint(PaintEventArgs e) =>
        UiFit.PaintFlatButtonFace(this, e.Graphics);

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
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
