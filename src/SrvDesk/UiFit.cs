namespace SrvDesk;

/// <summary>按实际文字宽度计算控件尺寸，避免中文被裁成半个字。</summary>
internal static class UiFit
{
    public static readonly Font UiFont = new("Microsoft YaHei UI", 9F);
    public static readonly Font UiFontSmall = new("Microsoft YaHei UI", 8.5F);

    public static int TextWidth(string text, Font? font = null) =>
        TextRenderer.MeasureText(text ?? "", font ?? UiFont).Width;

    /// <summary>按钮宽度：文字宽 + 内边距，且不小于 minWidth。</summary>
    public static int ButtonWidth(string text, Font? font = null, int minWidth = 72, int padding = 24) =>
        Math.Max(minWidth, TextWidth(text, font ?? UiFont) + padding);

    public static Size ButtonSize(string text, int height = 34, Font? font = null, int minWidth = 72, int padding = 24) =>
        new(ButtonWidth(text, font, minWidth, padding), height);

    /// <summary>把按钮收紧到刚好能完整显示文字（保留调用方指定的高度）。</summary>
    public static void FitButton(Button b, int? height = null, int minWidth = 72, int padding = 24)
    {
        var h = height ?? (b.Height > 0 ? b.Height : 34);
        b.Size = ButtonSize(b.Text, h, b.Font, minWidth, padding);
    }
}
