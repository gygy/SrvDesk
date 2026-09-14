using System.Runtime.CompilerServices;

namespace SrvDesk;

/// <summary>
/// 窗体内悬浮说明：位置与尺寸钳制在宿主 Form 客户区；推荐星用与主界面相同的彩色几何星。
/// 推荐说明可换行完整显示，不在星旁单行省略。
/// </summary>
internal sealed class InWindowTip
{
    private static readonly ConditionalWeakTable<Form, InWindowTip> ByForm = new();

    private readonly Form _form;
    private readonly Panel _panel;
    private readonly Panel _recommendHost;
    private readonly Panel _starRow;
    private readonly Label _prefix;
    private readonly RecommendStarsRow _stars;
    private readonly Label _recommendText;
    private readonly Panel _bodyHost;
    private readonly Label _label;
    private readonly System.Windows.Forms.Timer _showDelay = new() { Interval = 380 };
    private readonly System.Windows.Forms.Timer _hideDelay = new() { Interval = 180 };
    private Control? _pendingAnchor;
    private Control? _activeAnchor;
    private Func<(RecommendLevel Level, string Body)>? _pendingContent;
    private bool _overPanel;

    private InWindowTip(Form form)
    {
        _form = form;
        _prefix = new Label
        {
            AutoSize = false,
            Text = AppLang.L("【推荐】", "[Recommend] "),
            Font = UiFit.UiFontSmall,
            ForeColor = AppTheme.TextMain,
            BackColor = AppTheme.SurfaceCard,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
        };
        _stars = new RecommendStarsRow { BackColor = AppTheme.SurfaceCard };
        _starRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = Math.Max(UiScale.S(22), RecommendLevelUi.StarSize + UiScale.S(4)),
            BackColor = AppTheme.SurfaceCard,
        };
        _starRow.Controls.Add(_stars);
        _starRow.Controls.Add(_prefix);
        _starRow.Resize += (_, _) => LayoutStarRow();

        _recommendText = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Font = UiFit.UiFontSmall,
            ForeColor = AppTheme.TextMain,
            BackColor = AppTheme.SurfaceCard,
            UseMnemonic = false,
            Padding = new Padding(0, 0, 0, UiScale.S(4)),
        };

        _recommendHost = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(UiScale.S(8), UiScale.S(6), UiScale.S(8), UiScale.S(2)),
        };
        // Dock Top：后加的在上 → 先加文案再加星行，星行在上
        _recommendHost.Controls.Add(_recommendText);
        _recommendHost.Controls.Add(_starRow);

        _label = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            BackColor = AppTheme.SurfaceCard,
            ForeColor = AppTheme.TextMain,
            Font = UiFit.UiFontSmall,
            Padding = new Padding(UiScale.S(8), UiScale.S(2), UiScale.S(8), UiScale.S(6)),
            UseMnemonic = false,
        };
        _bodyHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
            AutoScroll = true,
        };
        _bodyHost.Controls.Add(_label);

        _panel = new BufferedPanel
        {
            Visible = false,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(1),
        };
        _panel.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, _panel.Width - 1, _panel.Height - 1);
        };
        _panel.Controls.Add(_bodyHost);
        _panel.Controls.Add(_recommendHost);

        foreach (Control c in new Control[] { _panel, _recommendHost, _starRow, _prefix, _stars, _recommendText, _bodyHost, _label })
            WireKeepOpen(c);

        _showDelay.Tick += (_, _) =>
        {
            _showDelay.Stop();
            if (_pendingAnchor is { IsDisposed: false } a && _pendingContent is not null)
            {
                var (level, body) = _pendingContent();
                ShowNow(a, level, body);
            }
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

    private void LayoutStarRow()
    {
        var prefixW = TextRenderer.MeasureText(
            _prefix.Text, _prefix.Font, new Size(int.MaxValue, _starRow.Height),
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + 4;
        _prefix.SetBounds(0, 0, prefixW, _starRow.ClientSize.Height);
        _stars.SetBounds(_prefix.Right, 0, RecommendLevelUi.StarsBlockWidth + 4, _starRow.ClientSize.Height);
        _stars.TrailingText = ""; // 文案改到下方换行 Label，避免单行省略
    }

    private void WireKeepOpen(Control c)
    {
        c.MouseEnter += (_, _) =>
        {
            _overPanel = true;
            _hideDelay.Stop();
        };
        c.MouseLeave += (_, _) =>
        {
            _overPanel = false;
            ScheduleHide();
        };
    }

    public static InWindowTip For(Form form)
    {
        if (ByForm.TryGetValue(form, out var tip))
            return tip;
        tip = new InWindowTip(form);
        ByForm.Add(form, tip);
        return tip;
    }

    public static void Attach(Control anchor, Func<(RecommendLevel Level, string Body)> getContent)
    {
        if (anchor is null) return;
        void OnEnter(object? _, EventArgs __)
        {
            var form = anchor.FindForm();
            if (form is null) return;
            For(form).ScheduleShow(anchor, getContent);
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

    private void ScheduleShow(Control anchor, Func<(RecommendLevel Level, string Body)> getContent)
    {
        _hideDelay.Stop();
        _pendingAnchor = anchor;
        _pendingContent = getContent;
        _showDelay.Stop();
        _showDelay.Start();
    }

    private void ScheduleHide()
    {
        _showDelay.Stop();
        _pendingAnchor = null;
        _pendingContent = null;
        _hideDelay.Stop();
        _hideDelay.Start();
    }

    private void ShowNow(Control anchor, RecommendLevel level, string body)
    {
        body = (body ?? "").Trim();
        if (body.Length == 0 || _form.IsDisposed || !anchor.IsHandleCreated)
        {
            Hide();
            return;
        }

        body = StripRecommendLine(body);

        _activeAnchor = anchor;
        _stars.Level = level;
        _stars.TrailingText = "";
        var tipBody = RecommendLevelUi.TipBody(level);
        _recommendText.Text = tipBody;

        var margin = UiScale.S(8);
        var pad = UiScale.S(8);
        var maxW = Math.Max(UiScale.S(200), _form.ClientSize.Width - margin * 2);
        var tipW = Math.Min(maxW, UiScale.S(420));
        var innerW = Math.Max(UiScale.S(160), tipW - pad * 2 - 2);

        LayoutStarRow();
        var starH = Math.Max(UiScale.S(22), RecommendLevelUi.StarSize + UiScale.S(4));
        _starRow.Height = starH;

        var recFlags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.Left;
        var recTextH = TextRenderer.MeasureText(tipBody, _recommendText.Font, new Size(innerW, int.MaxValue), recFlags).Height
                       + _recommendText.Padding.Vertical;
        _recommendText.Height = Math.Max(UiFit.LineHeight(_recommendText.Font), recTextH);
        _recommendHost.Height = _recommendHost.Padding.Vertical + starH + _recommendText.Height;

        var bodyFlags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.Left;
        var bodyTextH = body.Length == 0
            ? 0
            : TextRenderer.MeasureText(body, _label.Font, new Size(innerW, int.MaxValue), bodyFlags).Height
              + _label.Padding.Vertical;
        _label.Text = body;
        _label.Width = tipW - 2;
        _label.Height = Math.Max(bodyTextH, body.Length == 0 ? 0 : UiFit.LineHeight(_label.Font));

        var contentH = 2 + _recommendHost.Height + _label.Height;
        var maxH = Math.Max(UiScale.S(48), _form.ClientSize.Height - margin * 2);
        var tipH = Math.Min(contentH, maxH);
        _bodyHost.AutoScrollMinSize = contentH > tipH
            ? new Size(0, _label.Height)
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

    private static string StripRecommendLine(string body)
    {
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var kept = new List<string>();
        foreach (var line in lines)
        {
            var t = line.TrimStart();
            if (t.StartsWith("【推荐】", StringComparison.Ordinal)
                || t.StartsWith("[Recommend]", StringComparison.OrdinalIgnoreCase))
                continue;
            kept.Add(line);
        }
        return string.Join("\r\n", kept).Trim();
    }

    public void Hide()
    {
        _showDelay.Stop();
        _hideDelay.Stop();
        _pendingAnchor = null;
        _pendingContent = null;
        _activeAnchor = null;
        _overPanel = false;
        if (!_form.IsDisposed && _panel.Visible)
            _panel.Visible = false;
    }
}
