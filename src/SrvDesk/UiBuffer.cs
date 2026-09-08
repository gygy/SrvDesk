using System.Reflection;
using System.Runtime.InteropServices;

namespace SrvDesk;

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

/// <summary>横向单行工具栏：不换行、不出现滚动条（避免 1px 溢出挤出上下箭头）。</summary>
internal sealed class NoScrollFlowLayoutPanel : FlowLayoutPanel
{
    private const int WsHScroll = 0x00100000;
    private const int WsVScroll = 0x00200000;

    public NoScrollFlowLayoutPanel()
    {
        WrapContents = false;
        AutoScroll = false;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
        DoubleBuffered = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style &= ~(WsHScroll | WsVScroll);
            return cp;
        }
    }

    protected override Point ScrollToControl(Control activeControl) =>
        DisplayRectangle.Location;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UiBuffer.HideNativeScrollBars(this);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        UiBuffer.HideNativeScrollBars(this);
    }
}

internal static class UiBuffer
{
    private static readonly PropertyInfo? DoubleBufferedProp =
        typeof(Control).GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);

    private const int WmSetRedraw = 0x000B;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

    private const int SbHorz = 0;
    private const int SbVert = 1;

    /// <summary>
    /// 指定列铺满剩余宽度，并预留竖向滚动条。
    /// 否则列按满宽算，竖条一出现就挤出横条，最后一行会被挡住一半。
    /// </summary>
    public static void FitListViewColumn(ListView list, int fillColumnIndex, int minWidth = 80)
    {
        if (list.Columns.Count <= fillColumnIndex || !list.IsHandleCreated)
            return;
        var used = 0;
        for (var i = 0; i < list.Columns.Count; i++)
        {
            if (i == fillColumnIndex) continue;
            used += list.Columns[i].Width;
        }

        var avail = list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - used - 4;
        list.Columns[fillColumnIndex].Width = Math.Max(minWidth, avail);
        ShowScrollBar(list.Handle, SbHorz, false);
    }

    /// <summary>单行工具栏禁止出现滚动条（AutoScroll 在略溢出时会同时挤出竖条）。</summary>
    public static void ConfigureNoScrollRow(FlowLayoutPanel flow)
    {
        flow.WrapContents = false;
        flow.AutoScroll = false;
        void Hide(object? s, EventArgs e) => HideNativeScrollBars(flow);
        flow.HandleCreated += Hide;
        flow.Layout += Hide;
        flow.SizeChanged += Hide;
        if (flow.IsHandleCreated)
            HideNativeScrollBars(flow);
    }

    public static void HideNativeScrollBars(Control control)
    {
        if (control is null || !control.IsHandleCreated) return;
        ShowScrollBar(control.Handle, SbHorz, false);
        ShowScrollBar(control.Handle, SbVert, false);
    }

    public static void BindListViewColumnFit(ListView list, int fillColumnIndex, int minWidth = 80)
    {
        void Fit(object? sender, EventArgs e) => FitListViewColumn(list, fillColumnIndex, minWidth);
        list.HandleCreated += Fit;
        list.SizeChanged += Fit;
        if (list.IsHandleCreated)
            FitListViewColumn(list, fillColumnIndex, minWidth);
    }

    /// <summary>对已有控件开启双缓冲（无法改类型时用）。</summary>
    public static void Enable(Control control)
    {
        try { DoubleBufferedProp?.SetValue(control, true, null); }
        catch { /* ignore */ }
    }

    /// <summary>
    /// ListView 专用双缓冲：走 LVS_EX_DOUBLEBUFFER，避免反射 DoubleBuffered 破坏表头绘制。
    /// </summary>
    public static void EnableListView(ListView list)
    {
        if (!list.IsHandleCreated) return;
        const int lvmFirst = 0x1000;
        const int lvmSetExtendedListViewStyle = lvmFirst + 54;
        const int lvsExDoubleBuffer = 0x00010000;
        SendMessage(list.Handle, lvmSetExtendedListViewStyle, (IntPtr)lvsExDoubleBuffer, (IntPtr)lvsExDoubleBuffer);
        FitListViewRowHeight(list);
    }

    /// <summary>
    /// 按当前字体抬高报告行。注意：不存在可用的 LVM_SETITEMHEIGHT；
    /// 此前误用 LVM_FIRST+27（实为 LVM_INSERTCOLUMN）会导致原生访问冲突并直接退出。
    /// 正确做法是挂一个高度合适的 SmallImageList。
    /// </summary>
    public static void FitListViewRowHeight(ListView list)
    {
        if (list is null) return;
        var h = Math.Max(22, UiFit.LineHeight(list.Font) + 10);
        var cur = list.SmallImageList;
        if (cur is not null && cur.ImageSize.Width == 1 && cur.ImageSize.Height == h)
            return;

        var imgs = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(1, h),
        };
        list.SmallImageList = imgs;
        try { cur?.Dispose(); }
        catch { /* ignore */ }
    }

    /// <summary>切换标签时暂停重绘，减轻内容区高度跳动带来的闪烁。</summary>
    public static IDisposable SuspendRedraw(Control control) => new RedrawScope(control);

    private sealed class RedrawScope : IDisposable
    {
        private readonly Control _control;
        private readonly bool _hadHandle;

        public RedrawScope(Control control)
        {
            _control = control;
            _hadHandle = control.IsHandleCreated;
            if (_hadHandle)
                SendMessage(control.Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
            control.SuspendLayout();
        }

        public void Dispose()
        {
            _control.ResumeLayout(true);
            if (_hadHandle && _control.IsHandleCreated)
            {
                SendMessage(_control.Handle, WmSetRedraw, (IntPtr)1, IntPtr.Zero);
                _control.Invalidate(true);
                _control.Update();
            }
        }
    }
}
