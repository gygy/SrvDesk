namespace SrvDesk;

/// <summary>主窗与子窗共用的左侧导航：宽度、行高、选中/悬停绘制一致。</summary>
internal static class NavMenuStyle
{
    public static int SidebarWidth => UiScale.S(220);
    /// <summary>一级菜单行高：14px 字 + 22px 行高配套。</summary>
    public static int ItemHeight => Math.Max(UiScale.S(40), UiFit.LineHeight(UiFit.UiFontNav) + UiScale.S(16));
    public static int IconSize => Math.Max(18, UiScale.S(18));

    /// <summary>
    /// 按全部标签完整文案（含一级 Medium、图标槽）计算侧栏最小宽度，
    /// 避免启动时只露出半截字或省略号。
    /// </summary>
    public static int PreferredWidth(IEnumerable<string> labels, bool withIcon = true)
    {
        var font = UiFit.UiFontNav;
        var maxText = 0;
        foreach (var raw in labels)
        {
            var t = raw?.Trim() ?? "";
            if (t.Length == 0) continue;
            maxText = Math.Max(maxText, UiFit.TextWidth(t, font));
        }

        if (maxText <= 0)
            maxText = UiFit.TextWidth("自定义配置", font);

        var left = UiScale.S(12);
        if (withIcon)
            left += IconSize + UiScale.S(8);
        var right = UiScale.S(16) + Math.Max(UiScale.S(10), UiFit.LineHeight(font) / 3)
                    + SystemInformation.VerticalScrollBarWidth;
        return Math.Max(UiScale.S(148), left + maxText + right);
    }

    public static Panel CreateSidebar(int? width = null) =>
        new() { Width = width ?? SidebarWidth, BackColor = AppTheme.NavBg };

    public static Panel CreateSidebar(IEnumerable<string> labels, bool withIcon = false) =>
        CreateSidebar(PreferredWidth(labels, withIcon));

    public static void Apply(ListBox menu)
    {
        menu.Dock = DockStyle.Fill;
        menu.BorderStyle = BorderStyle.None;
        menu.BackColor = AppTheme.NavBg;
        menu.ForeColor = AppTheme.TextMain;
        menu.Font = UiFit.UiFontNav;
        menu.IntegralHeight = false;
        menu.DrawMode = DrawMode.OwnerDrawFixed;
        menu.ItemHeight = ItemHeight;
    }

    public static void BindHover(ListBox menu, Func<int> getHover, Action<int> setHover)
    {
        menu.MouseMove += (_, e) =>
        {
            var i = menu.IndexFromPoint(e.Location);
            if (i != getHover())
            {
                setHover(i);
                menu.Invalidate();
            }
        };
        menu.MouseLeave += (_, _) =>
        {
            if (getHover() < 0) return;
            setHover(-1);
            menu.Invalidate();
        };
    }

    public static void DrawItem(
        DrawItemEventArgs e,
        string text,
        Font font,
        bool hover,
        bool separator = false,
        int matchCount = -1,
        Image? icon = null)
    {
        if (e.Index < 0) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        hover = hover && !selected;
        using var back = new SolidBrush(selected ? AppTheme.Primary : hover ? AppTheme.NavHover : AppTheme.NavBg);
        e.Graphics.FillRectangle(back, e.Bounds);

        if (separator)
        {
            using var sep = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(sep, e.Bounds.X + 12, e.Bounds.Y + 1, e.Bounds.Right - 12, e.Bounds.Y + 1);
        }

        if (selected)
        {
            // 3～4px 左侧指示条；选中态不靠加粗，保持 14 Medium + 背景
            using var accent = new SolidBrush(Color.FromArgb(255, 255, 255));
            var barW = Math.Max(3, UiScale.S(3));
            e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 10, barW, e.Bounds.Height - 20);
        }

        try
        {
            var use = font ?? UiFit.UiFontNav;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            var fore = selected ? AppTheme.TextOnPrimary : AppTheme.TextMain;
            var iconSz = IconSize;
            var left = e.Bounds.X + UiScale.S(12);
            if (icon is not null)
            {
                var iy = e.Bounds.Y + (e.Bounds.Height - iconSz) / 2;
                if (selected)
                {
                    // 选中时略提亮，避免深色图标压在主色底上发糊
                    var ia = new System.Drawing.Imaging.ImageAttributes();
                    var cm = new System.Drawing.Imaging.ColorMatrix(
                    [
                        [1.15f, 0, 0, 0, 0],
                        [0, 1.15f, 0, 0, 0],
                        [0, 0, 1.15f, 0, 0],
                        [0, 0, 0, 1, 0],
                        [0.08f, 0.08f, 0.08f, 0, 1],
                    ]);
                    ia.SetColorMatrix(cm);
                    e.Graphics.DrawImage(icon, new Rectangle(left, iy, iconSz, iconSz),
                        0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel, ia);
                    ia.Dispose();
                }
                else
                {
                    e.Graphics.DrawImage(icon, new Rectangle(left, iy, iconSz, iconSz));
                }

                left += iconSz + UiScale.S(8);
            }

            var badge = matchCount >= 0 ? matchCount.ToString() : "";
            var badgeW = 0;
            if (badge.Length > 0)
            {
                var badgeSize = TextRenderer.MeasureText(e.Graphics, badge, UiFit.UiFontScope, Size.Empty,
                    TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                badgeW = Math.Max(UiScale.S(18), badgeSize.Width + UiScale.S(8));
            }

            var textRightPad = badgeW > 0 ? badgeW + UiScale.S(14) : UiScale.S(12);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                use,
                new Rectangle(left, e.Bounds.Y, Math.Max(8, e.Bounds.Right - left - textRightPad), e.Bounds.Height),
                matchCount == 0 && !selected ? AppTheme.TextMute : fore,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            if (badgeW > 0)
            {
                var badgeRect = new Rectangle(
                    e.Bounds.Right - badgeW - UiScale.S(10),
                    e.Bounds.Y + (e.Bounds.Height - UiScale.S(20)) / 2,
                    badgeW,
                    UiScale.S(20));
                var badgeBack = selected
                    ? Color.FromArgb(40, 255, 255, 255)
                    : matchCount > 0 ? AppTheme.PrimaryPale : AppTheme.SurfaceCard;
                var badgeFore = selected
                    ? AppTheme.TextOnPrimary
                    : matchCount > 0 ? AppTheme.PrimaryDark : AppTheme.TextMute;
                using (var br = new SolidBrush(badgeBack))
                    e.Graphics.FillRectangle(br, badgeRect);
                TextRenderer.DrawText(
                    e.Graphics,
                    badge,
                    UiFit.UiFontScope,
                    badgeRect,
                    badgeFore,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
        }
        catch
        {
            /* ignore draw failures */
        }
    }
}
