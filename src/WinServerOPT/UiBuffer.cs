using System.Reflection;

namespace WinOpt;

/// <summary>减少快速滚动时的残影/闪烁（WinForms 默认 Panel 不双缓冲）。</summary>
internal class BufferedPanel : Panel
{
    private readonly bool _composited;

    /// <param name="composited">
    /// 为 true 时启用 WS_EX_COMPOSITED，适合带大量子控件的 AutoScroll 容器；
    /// 行内小面板用 false 即可。
    /// </param>
    public BufferedPanel(bool composited = false)
    {
        _composited = composited;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);
        DoubleBuffered = true;
        UpdateStyles();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            if (_composited)
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }
}

/// <summary>FlowLayout 分区正文：双缓冲，减轻滚动残影。</summary>
internal sealed class BufferedFlowLayoutPanel : FlowLayoutPanel
{
    public BufferedFlowLayoutPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);
        DoubleBuffered = true;
        UpdateStyles();
    }
}

internal static class UiBuffer
{
    private static readonly PropertyInfo? DoubleBufferedProp =
        typeof(Control).GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>对已有控件开启双缓冲（无法改类型时用）。</summary>
    public static void Enable(Control control)
    {
        try { DoubleBufferedProp?.SetValue(control, true, null); }
        catch { /* ignore */ }
    }
}
