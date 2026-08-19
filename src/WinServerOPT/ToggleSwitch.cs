namespace WinOpt;

internal sealed class ToggleSwitch : Control
{
    private bool _checked;

    private static readonly Color OnLeft = Color.FromArgb(94, 53, 177);
    private static readonly Color OnRight = Color.FromArgb(55, 55, 55);
    private static readonly Color OffLeft = Color.FromArgb(55, 55, 55);
    private static readonly Color OffRight = Color.FromArgb(189, 189, 189);
    private static readonly Color Knob = Color.White;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Size = new Size(72, 28);
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
        var r = ClientRectangle;
        r.Inflate(-1, -1);
        var half = r.Width / 2;
        var left = new Rectangle(r.X, r.Y, half, r.Height);
        var right = new Rectangle(r.X + half, r.Y, r.Width - half, r.Height);

        using var leftBrush = new SolidBrush(_checked ? OnLeft : OffLeft);
        using var rightBrush = new SolidBrush(_checked ? OnRight : OffRight);
        g.FillRectangle(leftBrush, left);
        g.FillRectangle(rightBrush, right);

        var knobW = Math.Max(10, r.Width / 5);
        var knobX = _checked ? r.Right - knobW - 4 : r.X + 4;
        var knob = new Rectangle(knobX, r.Y + 3, knobW, r.Height - 6);
        using var knobBrush = new SolidBrush(Knob);
        g.FillRectangle(knobBrush, knob);
    }
}
