namespace WinOpt;

internal sealed class ToggleSwitch : Control
{
    private bool _checked;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Size = new Size(56, 26);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CheckedChanged;

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Checked = !Checked;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var track = ClientRectangle;
        track.Inflate(-1, -3);
        var radius = track.Height / 2;

        var trackColor = _checked ? AppTheme.ToggleOn : AppTheme.ToggleOff;
        using (var path = RoundedRect(track, radius))
        using (var brush = new SolidBrush(trackColor))
            g.FillPath(brush, path);

        if (_checked)
        {
            using var glow = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
            var glowRect = new Rectangle(track.X + 2, track.Y + 2, track.Width / 2, track.Height - 4);
            using var glowPath = RoundedRect(glowRect, radius - 2);
            g.FillPath(glow, glowPath);
        }

        var knobSize = track.Height - 4;
        var knobX = _checked ? track.Right - knobSize - 2 : track.X + 2;
        var knob = new Rectangle(knobX, track.Y + 2, knobSize, knobSize);
        using var knobBrush = new SolidBrush(AppTheme.ToggleKnob);
        g.FillEllipse(knobBrush, knob);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
