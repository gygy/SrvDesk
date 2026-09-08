namespace SrvDesk;

/// <summary>按系统 DPI 缩放设计稿像素（96 DPI = 1.0）。</summary>
internal static class UiScale
{
    private static float? _factor;

    public static float Factor
    {
        get
        {
            if (_factor is float f && f > 0) return f;
            try
            {
                using var g = Graphics.FromHwnd(IntPtr.Zero);
                _factor = Math.Max(1f, g.DpiX / 96f);
            }
            catch
            {
                _factor = 1f;
            }
            return _factor.Value;
        }
    }

    /// <summary>主显示器 DPI 变化后清缓存（少见；下次访问重算）。</summary>
    public static void Reset() => _factor = null;

    public static int S(int designPx) =>
        (int)Math.Round(designPx * Factor, MidpointRounding.AwayFromZero);

    public static Size Size(int w, int h) => new(S(w), S(h));

    public static Padding Pad(int all) => new(S(all));

    public static Padding Pad(int left, int top, int right, int bottom) =>
        new(S(left), S(top), S(right), S(bottom));
}
