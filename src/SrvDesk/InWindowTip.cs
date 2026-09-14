using System.Runtime.CompilerServices;

namespace SrvDesk;

/// <summary>
/// 窗体内悬浮说明：位置与尺寸钳制在宿主 Form 客户区，避免系统 ToolTip 画出窗口外。
/// </summary>
internal sealed class InWindowTip
{
    private static readonly ConditionalWeakTable<Form, InWindowTip> ByForm = new();

    private readonly Form _form;
    private readonly Panel _panel;
    private readonly Label _label;
    private readonly System.Windows.Forms.Timer _showDelay = new() { Interval = 380 };
    private readonly System.Windows.Forms.Timer _hideDelay = new() { Interval = 180 };
    private Control? _pendingAnchor;
    private Control? _activeAnchor;
    private Func<string>? _pendingText;
    private bool _overPanel;

    private InWindowTip(Form form)
    {
        _form = form;
        _label = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
            ForeColor = AppTheme.TextMain,
            Font = UiFit.UiFontSmall,
            Padding = new Padding(UiScale.S(8), UiScale.S(6), UiScale.S(8), UiScale.S(6)),
            UseMnemonic = false,
        };
        _panel = new BufferedPanel
        {
            Visible = false,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(1),
            AutoScroll = true,
        };
        _panel.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, _panel.Width - 1, _panel.Height - 1);
        };
        _panel.Controls.Add(_label);
        _panel.MouseEnter += (_, _) =>
        {
            _overPanel = true;
            _hideDelay.Stop();
        };
        _panel.MouseLeave += (_, _) =>
        {
            _overPanel = false;
            ScheduleHide();
        };
        _label.MouseEnter += (_, _) =>
        {
            _overPanel = true;
            _hideDelay.Stop();
        };
        _label.MouseLeave += (_, _) =>
        {
            _overPanel = false;
            ScheduleHide();
        };

        _showDelay.Tick += (_, _) =>
        {
            _showDelay.Stop();
            if (_pendingAnchor is { IsDisposed: false } a && _pendingText is not null)
                ShowNow(a, _pendingText());
        };
        _hideDelay.Tick += (_, _) =>
        {
            _hideDelay.Stop();
            if (!_overPanel && (_activeAnchor is null || !_activeAnchor.ClientRectangle.Contains(
                    _activeAnchor.PointToClient(Control.MousePosition))))
                Hide();
        };

        form.Controls.Add(_panel);
        _panel.BringToFront();
        form.Resize += (_, _) => Hide();
        form.Deactivate += (_, _) => Hide();
        form.Move += (_, _) => Hide();
    }

    public static InWindowTip For(Form form)
    {
        if (ByForm.TryGetValue(form, out var tip))
            return tip;
        tip = new InWindowTip(form);
        ByForm.Add(form, tip);
        return tip;
    }

    /// <summary>绑定控件：悬停后在窗体内弹出说明（不再使用系统 ToolTip）。</summary>
    public static void Attach(Control anchor, Func<string> getText)
    {
        if (anchor is null) return;
        void OnEnter(object? _, EventArgs __)
        {
            var form = anchor.FindForm();
            if (form is null) return;
            var tip = For(form);
            tip.ScheduleShow(anchor, getText);
        }
        void OnLeave(object? _, EventArgs __)
        {
            var form = anchor.FindForm();
            if (form is null) return;
            if (ByForm.TryGetValue(form, out var tip))
                tip.ScheduleHide();
        }
        void OnDown(object? _, MouseEventArgs __)
        {
            var form = anchor.FindForm();
            if (form is null) return;
            if (ByForm.TryGetValue(form, out var tip))
                tip.Hide();
        }

        anchor.MouseEnter += OnEnter;
        anchor.MouseLeave += OnLeave;
        anchor.MouseDown += OnDown;
    }

    private void ScheduleShow(Control anchor, Func<string> getText)
    {
        _hideDelay.Stop();
        _pendingAnchor = anchor;
        _pendingText = getText;
        _showDelay.Stop();
        _showDelay.Start();
    }

    private void ScheduleHide()
    {
        _showDelay.Stop();
        _pendingAnchor = null;
        _pendingText = null;
        _hideDelay.Stop();
        _hideDelay.Start();
    }

    private void ShowNow(Control anchor, string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0 || _form.IsDisposed || !anchor.IsHandleCreated)
        {
            Hide();
            return;
        }

        _activeAnchor = anchor;
        var margin = UiScale.S(8);
        var pad = UiScale.S(8);
        var maxW = Math.Max(UiScale.S(200), _form.ClientSize.Width - margin * 2);
        var tipW = Math.Min(maxW, UiScale.S(380));
        var textW = Math.Max(UiScale.S(160), tipW - pad * 2 - 2);
        var measured = TextRenderer.MeasureText(
            text,
            _label.Font,
            new Size(textW, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.Left);
        var contentH = measured.Height + pad * 2;
        var maxH = Math.Max(UiScale.S(48), _form.ClientSize.Height - margin * 2);
        var tipH = Math.Min(contentH, maxH);

        _label.Text = text;
        _label.AutoSize = false;
        // 内容超出时靠面板滚动，仍不超出窗口
        _panel.AutoScrollMinSize = contentH > tipH
            ? new Size(0, contentH)
            : Size.Empty;
        _panel.Size = new Size(tipW, tipH);

        var below = _form.PointToClient(anchor.PointToScreen(new Point(0, anchor.Height)));
        var above = _form.PointToClient(anchor.PointToScreen(Point.Empty));
        var x = below.X;
        var y = below.Y + UiScale.S(2);
        if (y + tipH > _form.ClientSize.Height - margin)
            y = above.Y - tipH - UiScale.S(2);
        if (y < margin)
            y = margin;
        if (y + tipH > _form.ClientSize.Height - margin)
            y = Math.Max(margin, _form.ClientSize.Height - margin - tipH);
        if (x + tipW > _form.ClientSize.Width - margin)
            x = _form.ClientSize.Width - margin - tipW;
        if (x < margin)
            x = margin;

        _panel.Location = new Point(x, y);
        _panel.Visible = true;
        _panel.BringToFront();
    }

    public void Hide()
    {
        _showDelay.Stop();
        _hideDelay.Stop();
        _pendingAnchor = null;
        _pendingText = null;
        _activeAnchor = null;
        _overPanel = false;
        if (!_form.IsDisposed && _panel.Visible)
            _panel.Visible = false;
    }
}
