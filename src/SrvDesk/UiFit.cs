using System.Runtime.CompilerServices;

namespace SrvDesk;

/// <summary>
/// 界面度量与字体层级（Windows 桌面管理软件）：
/// 基础 14px / Segoe UI（中文回退 YaHei UI）；勿对逻辑 pt 再乘 DPI（PerMonitorV2）。
/// </summary>
internal static class UiFit
{
    private static Font? _ui;
    private static Font? _uiSmall;
    private static Font? _uiScope;
    private static Font? _menu;
    private static Font? _nav;
    private static Font? _navSecondary;
    private static Font? _section;
    private static Font? _pageTitle;
    private static Font? _button;
    private static Font? _tableHeader;
    private static string? _family;
    private static string? _semiboldFamily;
    private static readonly ConditionalWeakTable<Button, object> CenteredPaintHooked = new();

    /// <summary>
    /// 字体栈：Segoe UI → Microsoft YaHei UI → Microsoft YaHei。
    /// </summary>
    public static string UiFontFamily
    {
        get
        {
            if (_family is not null) return _family;
            foreach (var name in new[] { "Segoe UI", "Microsoft YaHei UI", "Microsoft YaHei", "Tahoma" })
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

    /// <summary>代码/脚本编辑器字体，与 UI 分离。</summary>
    public static string CodeFontFamily
    {
        get
        {
            foreach (var name in new[] { "Cascadia Code", "Cascadia Mono", "Consolas", "Courier New" })
            {
                if (FontFamilyExists(name)) return name;
            }
            return "Consolas";
        }
    }

    private static string? SemiboldFamily
    {
        get
        {
            if (_semiboldFamily is not null)
                return _semiboldFamily.Length == 0 ? null : _semiboldFamily;
            if (string.Equals(UiFontFamily, "Segoe UI", StringComparison.OrdinalIgnoreCase)
                && FontFamilyExists("Segoe UI Semibold"))
            {
                _semiboldFamily = "Segoe UI Semibold";
                return _semiboldFamily;
            }
            _semiboldFamily = "";
            return null;
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

    /// <summary>设计稿 px → 逻辑 pt（96DPI：pt = px × 72/96）。</summary>
    public static float PxToPt(float px) =>
        (float)Math.Round(px * 72f / 96f, 2);

    // 14px→10.5 / 13px→9.75 / 12px→9 / 16px→12 / 20px→15
    public const float DesignFontPt = 10.5F;
    public const float DesignFontSmallPt = 9.75F;
    public const float DesignFontScopePt = 9.0F;
    public const float DesignFontSectionPt = 12.0F;
    public const float DesignFontPageTitlePt = 15.0F;

    /// <summary>正文 14 Regular。</summary>
    public static Font UiFont => _ui ??= Make(DesignFontPt, FontStyle.Regular);
    /// <summary>描述 / 二级 13 Regular。</summary>
    public static Font UiFontSmall => _uiSmall ??= Make(DesignFontSmallPt, FontStyle.Regular);
    /// <summary>辅助 / Badge 12 Regular。</summary>
    public static Font UiFontScope => _uiScope ??= Make(DesignFontScopePt, FontStyle.Regular);
    /// <summary>顶栏菜单 14 Regular。</summary>
    public static Font UiFontMenu => _menu ??= Make(DesignFontPt, FontStyle.Regular);
    /// <summary>左侧一级 14 Medium；选中靠背景条，不再加粗。</summary>
    public static Font UiFontNav => _nav ??= MakeMedium(DesignFontPt);
    /// <summary>左侧二级 13 Regular。</summary>
    public static Font UiFontNavSecondary => _navSecondary ??= Make(DesignFontSmallPt, FontStyle.Regular);
    /// <summary>分区标题 16 Semibold。</summary>
    public static Font UiFontSection => _section ??= MakeSemibold(DesignFontSectionPt);
    /// <summary>页面标题 20 Semibold。</summary>
    public static Font UiFontPageTitle => _pageTitle ??= MakeSemibold(DesignFontPageTitlePt);
    /// <summary>按钮 / Tab 14 Medium。</summary>
    public static Font UiFontButton => _button ??= MakeMedium(DesignFontPt);
    /// <summary>表格表头 13 Medium。</summary>
    public static Font UiFontTableHeader => _tableHeader ??= MakeMedium(DesignFontSmallPt);

    /// <summary>兼容旧调用：指定 pt 的 SemiBold/Bold。</summary>
    public static Font UiFontBold(float size = DesignFontPt) => MakeSemibold(size);

    public static Font UiFontCode(float pt = 9.75f) =>
        new(CodeFontFamily, pt, FontStyle.Regular);

    private static Font Make(float pt, FontStyle style) => new(UiFontFamily, pt, style);

    private static Font MakeMedium(float pt) =>
        SemiboldFamily is { } sb ? new Font(sb, pt, FontStyle.Regular) : Make(pt, FontStyle.Regular);

    private static Font MakeSemibold(float pt) =>
        SemiboldFamily is { } sb ? new Font(sb, pt, FontStyle.Regular) : Make(pt, FontStyle.Bold);

    /// <summary>DPI / 换屏后丢弃缓存字体。</summary>
    public static void ResetCachedFonts()
    {
        DisposeFont(ref _ui);
        DisposeFont(ref _uiSmall);
        DisposeFont(ref _uiScope);
        DisposeFont(ref _menu);
        DisposeFont(ref _nav);
        DisposeFont(ref _navSecondary);
        DisposeFont(ref _section);
        DisposeFont(ref _pageTitle);
        DisposeFont(ref _button);
        DisposeFont(ref _tableHeader);
        _family = null;
        _semiboldFamily = null;
    }

    private static void DisposeFont(ref Font? f)
    {
        try { f?.Dispose(); } catch { /* ignore */ }
        f = null;
    }

    /// <summary>标签单行省略（按钮请用 PaintFlatButtonFace，勿带 EndEllipsis）。</summary>
    public static readonly TextFormatFlags SingleLineFlags =
        TextFormatFlags.SingleLine
        | TextFormatFlags.EndEllipsis
        | TextFormatFlags.NoPrefix
        | TextFormatFlags.NoPadding
        | TextFormatFlags.PreserveGraphicsClipping;

    /// <summary>测量用：保留字墨溢出边距，避免高 DPI 量偏窄。</summary>
    private static readonly TextFormatFlags MeasureFlags =
        TextFormatFlags.SingleLine
        | TextFormatFlags.NoPrefix
        | TextFormatFlags.GlyphOverhangPadding;

    /// <summary>舒适行高（12→18 / 13→20 / 14→22 / 16→24 / 20→28），不低于实测字形。</summary>
    public static int LineHeight(Font? font = null)
    {
        var f = font ?? UiFont;
        var measured = TextRenderer.MeasureText("国Agyp", f, new Size(1024, 256), MeasureFlags).Height;
        var comfort = ComfortLineHeightDesignPx(f.SizeInPoints);
        return Math.Max(f.Height, Math.Max(measured, UiScale.S(comfort)));
    }

    public static int ComfortLineHeightDesignPx(float sizeInPoints)
    {
        var px = sizeInPoints * 96f / 72f;
        if (px <= 12.5f) return 18;
        if (px <= 13.5f) return 20;
        if (px <= 15.0f) return 22;
        if (px <= 17.0f) return 24;
        if (px <= 21.0f) return 28;
        return 32;
    }

    /// <summary>
    /// 按钮 / 下拉 / 单行输入的最小可视高度。
    /// Flat 中文字体在偏矮高度时常裁掉字脚；副屏 DPI 变化后更明显。
    /// </summary>
    public static int ControlHeight(Font? font = null, int designMin = 34) =>
        Math.Max(UiScale.S(designMin), LineHeight(font ?? UiFont) + UiScale.S(12));

    public static int TextWidth(string text, Font? font = null) =>
        TextRenderer.MeasureText(text ?? "", font ?? UiFont, new Size(int.MaxValue, 256), MeasureFlags).Width;

    /// <summary>按钮宽度：文字宽 + 内边距 + 安全边，且不小于 minWidth。</summary>
    public static int ButtonWidth(string text, Font? font = null, int minWidth = 72, int padding = 28)
    {
        var f = font ?? UiFontButton;
        var slack = Math.Max(UiScale.S(8), LineHeight(f) / 3);
        return Math.Max(UiScale.S(minWidth), TextWidth(text, f) + UiScale.S(padding) + slack);
    }

    public static Size ButtonSize(string text, int height = 34, Font? font = null, int minWidth = 72, int padding = 28)
    {
        var f = font ?? UiFontButton;
        var h = Math.Max(height, ControlHeight(f));
        return new(ButtonWidth(text, f, minWidth, padding), h);
    }

    /// <summary>把按钮收到刚好能完整显示文字（高度不低于 ControlHeight）。</summary>
    public static void FitButton(Button b, int? height = null, int minWidth = 72, int padding = 28)
    {
        if (b is null || string.IsNullOrEmpty(b.Text)) return;
        if (b.Font is null || ReferenceEquals(b.Font, SystemFonts.DefaultFont) || ReferenceEquals(b.Font, UiFont))
            b.Font = UiFontButton;
        var f = b.Font ?? UiFontButton;
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

    private static readonly ConditionalWeakTable<Form, object> FormFitHooked = new();

    /// <summary>
    /// 按当前显示器工作区收小窗体，避免底栏「应用到系统」等被挤出屏幕。
    /// 子窗体额外不超过主窗（或 Owner）外框。
    /// </summary>
    public static void FitFormToWorkingArea(Form form, Form? sizeCapRelativeTo = null)
    {
        if (form is null || form.IsDisposed) return;
        // 嵌入主窗的页（TopLevel=false）不要改尺寸
        if (!form.TopLevel || form.FormBorderStyle == FormBorderStyle.None)
            return;

        Rectangle wa;
        try
        {
            Screen? scr = null;
            if (form.IsHandleCreated)
                scr = Screen.FromControl(form);
            else if (sizeCapRelativeTo is { IsHandleCreated: true })
                scr = Screen.FromControl(sizeCapRelativeTo);
            scr ??= Screen.FromPoint(Cursor.Position) ?? Screen.PrimaryScreen;
            if (scr is null) return;
            wa = scr.WorkingArea;
        }
        catch
        {
            return;
        }

        const float maxFrac = 0.92f;
        var maxOuterW = Math.Max(360, (int)(wa.Width * maxFrac));
        var maxOuterH = Math.Max(280, (int)(wa.Height * maxFrac));

        var cap = sizeCapRelativeTo;
        if (cap is null || cap.IsDisposed || ReferenceEquals(cap, form))
            cap = FindMainForm(form);
        if (cap is { IsHandleCreated: true, IsDisposed: false } && !ReferenceEquals(cap, form))
        {
            // 子窗不超过主窗，并略留边
            maxOuterW = Math.Min(maxOuterW, Math.Max(360, cap.Width - UiScale.S(24)));
            maxOuterH = Math.Min(maxOuterH, Math.Max(280, cap.Height - UiScale.S(24)));
        }

        var ncW = 16;
        var ncH = 39;
        try
        {
            if (form.IsHandleCreated)
            {
                ncW = Math.Max(0, form.Width - form.ClientSize.Width);
                ncH = Math.Max(0, form.Height - form.ClientSize.Height);
            }
        }
        catch { /* ignore */ }

        var maxClientW = Math.Max(320, maxOuterW - ncW);
        var maxClientH = Math.Max(240, maxOuterH - ncH);

        var minOuter = form.MinimumSize;
        if (minOuter.Width > 0 || minOuter.Height > 0)
        {
            form.MinimumSize = new Size(
                minOuter.Width > 0 ? Math.Min(Math.Max(320, minOuter.Width), maxOuterW) : 0,
                minOuter.Height > 0 ? Math.Min(Math.Max(240, minOuter.Height), maxOuterH) : 0);
            minOuter = form.MinimumSize;
        }

        var cs = form.ClientSize;
        var w = Math.Min(cs.Width, maxClientW);
        var h = Math.Min(cs.Height, maxClientH);
        if (minOuter.Width > 0)
            w = Math.Max(w, Math.Max(280, minOuter.Width - ncW));
        if (minOuter.Height > 0)
            h = Math.Max(h, Math.Max(200, minOuter.Height - ncH));
        w = Math.Min(w, maxClientW);
        h = Math.Min(h, maxClientH);

        if (w != cs.Width || h != cs.Height)
            form.ClientSize = new Size(w, h);

        // 保证整窗落在工作区内（底栏可见）
        try
        {
            if (!form.IsHandleCreated) return;
            var b = form.Bounds;
            var x = Math.Min(Math.Max(b.X, wa.Left), Math.Max(wa.Left, wa.Right - b.Width));
            var y = Math.Min(Math.Max(b.Y, wa.Top), Math.Max(wa.Top, wa.Bottom - b.Height));
            if (x != b.X || y != b.Y)
                form.Location = new Point(x, y);
        }
        catch { /* ignore */ }
    }

    /// <summary>在 Load/Shown 时自动 Fit（每个窗体只挂一次）。</summary>
    public static void HookAutoFitToWorkingArea(Form form)
    {
        if (form is null || form.IsDisposed) return;
        try
        {
            if (FormFitHooked.TryGetValue(form, out _))
                return;
            FormFitHooked.Add(form, new object());
        }
        catch (ArgumentException)
        {
            return;
        }

        void Fit() => FitFormToWorkingArea(form, form.Owner);
        form.Load += (_, _) => Fit();
        form.Shown += (_, _) => Fit();
        form.DpiChanged += (_, _) =>
        {
            UiScale.OnHostDpiChanged(form);
            Fit();
        };
    }

    private static Form? FindMainForm(Form? exclude)
    {
        try
        {
            foreach (Form f in Application.OpenForms)
            {
                if (f is MainForm && !f.IsDisposed && !ReferenceEquals(f, exclude))
                    return f;
            }
        }
        catch { /* ignore */ }
        return null;
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

    /// <summary>工具栏按钮可关 Selectable，避免抢 ListView 焦点导致丢选中。</summary>
    public void SetSelectable(bool selectable)
    {
        SetStyle(ControlStyles.Selectable, selectable);
        TabStop = selectable;
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
