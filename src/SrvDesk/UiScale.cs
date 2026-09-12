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

    /// <summary>按控件所在显示器 DPI（副屏与主屏不一致时用这个）。</summary>
    public static float FactorFor(Control? c)
    {
        try
        {
            if (c is not null && c.IsHandleCreated)
            {
                var dpi = c.DeviceDpi;
                if (dpi > 0) return Math.Max(1f, dpi / 96f);
            }
        }
        catch { /* ignore */ }
        return Factor;
    }

    /// <summary>DPI / 换显示器后清缓存，下次访问重算。</summary>
    public static void Reset() => _factor = null;

    /// <summary>换屏后：清缩放缓存并重算窗体内 Flat 文字按钮，减轻裁字。</summary>
    public static void OnHostDpiChanged(Control host)
    {
        Reset();
        try
        {
            if (host is not null && host.IsHandleCreated && host.DeviceDpi > 0)
                _factor = Math.Max(1f, host.DeviceDpi / 96f);
        }
        catch { /* ignore */ }
        UiFit.RefitFlatTextButtons(host);
    }

    public static int S(int designPx) =>
        (int)Math.Round(designPx * Factor, MidpointRounding.AwayFromZero);

    public static Size Size(int w, int h) => new(S(w), S(h));

    public static Padding Pad(int all) => new(S(all));

    public static Padding Pad(int left, int top, int right, int bottom) =>
        new(S(left), S(top), S(right), S(bottom));
}
