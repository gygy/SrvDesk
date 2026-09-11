namespace SrvDesk;

/// <summary>优化推荐强度（列表「推荐值」列 / 优化顾问）。</summary>
internal enum RecommendLevel
{
    /// <summary>不推荐作常规优化（或仅按需），顾问默认不展示。</summary>
    Optional = 0,
    /// <summary>高级：有收益但有副作用，不进一键/顾问核心。</summary>
    Suggested = 1,
    /// <summary>推荐：有明确场景与收益。</summary>
    Strong = 2,
    /// <summary>强烈推荐：低风险、可逆、体验收益明显。</summary>
    Must = 3,
}

internal static class RecommendLevelUi
{
    /// <summary>实心星：偏暖琥珀金，对比清晰。</summary>
    public static readonly Color StarOn = Color.FromArgb(230, 145, 12);
    /// <summary>未亮星：同形灰色（几何星，不用☆以免宽窄不一）。</summary>
    public static readonly Color StarOff = Color.FromArgb(168, 176, 188);

    /// <summary>单星外接直径（像素）。用几何星绘制，避免字体 ★ 在窄列里叠成变形块。</summary>
    public static int StarSize => Math.Max(11, UiScale.S(12));

    /// <summary>相邻星中心距（直径 + 间隙，互不重叠）。</summary>
    public static int StarStep => StarSize + Math.Max(2, UiScale.S(2));

    /// <summary>兼容旧调用：几何星高度约等于字号观感。</summary>
    public static float StarFontSize => StarSize;

    public static string Title(RecommendLevel level) => level switch
    {
        RecommendLevel.Must => AppLang.L("强烈推荐", "Strongly recommended"),
        RecommendLevel.Strong => AppLang.L("推荐", "Recommended"),
        RecommendLevel.Suggested => AppLang.L("高级", "Advanced"),
        _ => AppLang.L("不推荐", "Not recommended"),
    };

    /// <summary>亮星：5=强烈推荐，4=推荐，2=高级，0=不推荐。</summary>
    public static int StarsOn(RecommendLevel level) => level switch
    {
        RecommendLevel.Must => 5,
        RecommendLevel.Strong => 4,
        RecommendLevel.Suggested => 2,
        _ => 0,
    };

    public static int StarsBlockWidth => StarStep * 4 + StarSize;

    /// <summary>五星纯文本（提示/日志）；列表用自绘着色。</summary>
    public static string Icon(RecommendLevel level)
    {
        var on = StarsOn(level);
        return new string('★', on) + new string('☆', 5 - on);
    }

    public static Color ForeColorOf(RecommendLevel level) =>
        level == RecommendLevel.Optional ? StarOff : StarOn;

    public static string Tip(RecommendLevel level) =>
        $"{Icon(level)} {Title(level)}（{StarsOn(level)}/5） · " + level switch
        {
            RecommendLevel.Must => AppLang.L(
                "低风险、可逆，体验收益明显；适合作为默认/顾问优化。",
                "Low risk, reversible, clear UX gain — default/advisor optimize."),
            RecommendLevel.Strong => AppLang.L(
                "有明确使用场景与收益；建议按本机用途开启。",
                "Clear scenario and benefit; enable for your use case."),
            RecommendLevel.Suggested => AppLang.L(
                "高级项：可能有性能收益，但有副作用；勿一键全开。",
                "Advanced: possible gains with side effects; not for one-click."),
            _ => AppLang.L(
                "不推荐作常规优化（过时、收益小或降低安全/稳定性）。",
                "Not for routine optimize (outdated, low gain, or hurts security/stability)."),
        };

    public static string LegendShort => AppLang.L(
        "★★★★★ 强烈推荐 · ★★★★☆ 推荐 · ★★☆☆☆ 高级 · ☆☆☆☆☆ 不推荐",
        "★★★★★ Strongly recommended · ★★★★☆ Recommended · ★★☆☆☆ Advanced · ☆☆☆☆☆ Not recommended");

    /// <summary>与列表「推荐值」列相同的五星着色绘制（几何星，比例固定）。</summary>
    public static void DrawStars(Graphics g, RecommendLevel level, int x, int y)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        var on = StarsOn(level);
        var size = StarSize;
        var step = StarStep;
        var r = size / 2f;
        for (var i = 0; i < 5; i++)
        {
            var filled = i < on;
            using var brush = new SolidBrush(filled ? StarOn : StarOff);
            var cx = x + i * step + r;
            var cy = y + r;
            FillStar(g, brush, cx, cy, r);
        }
    }

    /// <summary>在指定矩形内绘制五星（裁剪 + 居中，避免溢出邻列或被盖住）。</summary>
    public static void DrawStarsInBounds(Graphics g, RecommendLevel level, Rectangle bounds, int paddingLeft = 4)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var state = g.Save();
        try
        {
            // 略内缩裁剪，防止抗锯齿像素渗到邻列
            var clip = Rectangle.Inflate(bounds, -1, 0);
            if (clip.Width <= 0 || clip.Height <= 0) clip = bounds;
            g.SetClip(clip);

            var blockW = StarsBlockWidth;
            var blockH = StarSize;
            var padL = Math.Max(2, paddingLeft);
            var x = bounds.X + padL;
            // 列够宽时水平居中，避免贴边看起来「压住」邻列
            if (bounds.Width > blockW + padL * 2)
                x = bounds.X + (bounds.Width - blockW) / 2;
            var y = bounds.Y + Math.Max(0, (bounds.Height - blockH) / 2);
            DrawStars(g, level, x, y);
        }
        finally
        {
            g.Restore(state);
        }
    }

    /// <summary>推荐值列建议宽度（五星完整 + 边距，且不窄于表头）。</summary>
    public static int PreferredColumnWidth
    {
        get
        {
            var stars = StarsBlockWidth + UiScale.S(16);
            var header = TextRenderer.MeasureText(
                AppLang.L("推荐值", "Recommend"),
                UiFit.UiFont,
                new Size(int.MaxValue, 64),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width
                + UiScale.S(16);
            return Math.Max(stars, header);
        }
    }

    public static Size MeasureStars() => new(StarsBlockWidth + 2, StarSize + 4);

    /// <summary>正五角星（尖朝上），外接半径 r，内半径约 0.4r。</summary>
    private static void FillStar(Graphics g, Brush brush, float cx, float cy, float outerR)
    {
        if (outerR < 2f) return;
        var innerR = outerR * 0.42f;
        var pts = new PointF[10];
        for (var i = 0; i < 10; i++)
        {
            var angle = -Math.PI / 2 + i * Math.PI / 5;
            var r = (i % 2 == 0) ? outerR : innerR;
            pts[i] = new PointF(
                cx + (float)(r * Math.Cos(angle)),
                cy + (float)(r * Math.Sin(angle)));
        }

        g.FillPolygon(brush, pts);
    }
}

/// <summary>彩色五星 + 右侧说明文字（与列表推荐值同色）。</summary>
internal sealed class RecommendStarsRow : Control
{
    private RecommendLevel _level = RecommendLevel.Suggested;
    private string _text = "";

    public RecommendStarsRow()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        Height = 22;
        BackColor = Color.Transparent;
    }

    public RecommendLevel Level
    {
        get => _level;
        set { _level = value; Invalidate(); }
    }

    public string TrailingText
    {
        get => _text;
        set { _text = value ?? ""; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var bg = Parent?.BackColor ?? AppTheme.SurfaceCard;
        if (bg.A == 255)
        {
            using var b = new SolidBrush(bg);
            g.FillRectangle(b, ClientRectangle);
        }

        var starY = Math.Max(0, (Height - RecommendLevelUi.StarSize) / 2);
        RecommendLevelUi.DrawStars(g, _level, 0, starY);

        if (_text.Length == 0) return;
        var textX = RecommendLevelUi.StarsBlockWidth + 6;
        using var font = new Font(UiFit.UiFontFamily, 8.5F);
        var color = AppTheme.TextMain;
        TextRenderer.DrawText(g, _text, font,
            new Rectangle(textX, 0, Math.Max(20, Width - textX), Height),
            color,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
    }
}
