namespace WinOpt;

internal sealed class InstantToggleRow : Panel
{
    private readonly ToggleSwitch _toggle = new();
    private bool _suppress;
    private Action<bool>? _apply;

    public InstantToggleRow(string title)
    {
        Height = 38;
        Width = 520;
        BackColor = Color.Transparent;

        _toggle.Location = new Point(4, 6);
        var label = new Label
        {
            Text = title,
            AutoSize = false,
            Location = new Point(68, 2),
            Size = new Size(520, 34),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent,
        };
        label.Click += (_, _) => _toggle.Checked = !_toggle.Checked;
        _toggle.CheckedChanged += (_, _) =>
        {
            if (_suppress || _apply is null) return;
            try { _apply(_toggle.Checked); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _suppress = true;
                _toggle.Checked = !_toggle.Checked;
                _suppress = false;
            }
        };
        Controls.Add(label);
        Controls.Add(_toggle);
    }

    public bool Checked
    {
        get => _toggle.Checked;
        set
        {
            _suppress = true;
            _toggle.Checked = value;
            _suppress = false;
        }
    }

    public void Bind(bool on, Action<bool> apply)
    {
        _apply = apply;
        Checked = on;
    }
}

internal static class ThemedSettingsChrome
{
    public static Panel CreateHeader(string title, string subtitle)
    {
        var header = new Panel { Height = 52, Dock = DockStyle.Top, BackColor = AppTheme.PrimaryDeep };
        header.Paint += (_, e) =>
        {
            var r = header.ClientRectangle;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                r, AppTheme.HeaderBarTop, AppTheme.HeaderBarBottom, 90f);
            e.Graphics.FillRectangle(brush, r);
        };

        var logo = new PictureBox
        {
            Size = new Size(36, 36),
            Location = new Point(14, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };
        var logoImg = AppBrand.LoadLogoImage();
        if (logoImg is not null) logo.Image = logoImg;

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = false,
            Location = new Point(54, 6),
            Size = new Size(520, 28),
            ForeColor = AppTheme.TextOnPrimary,
            Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };

        var sub = new Label
        {
            Text = subtitle,
            AutoSize = false,
            Location = new Point(54, 32),
            Height = 18,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = AppTheme.TextOnPrimarySoft,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };

        header.Controls.Add(sub);
        header.Controls.Add(titleLabel);
        header.Controls.Add(logo);
        header.Resize += (_, _) => sub.Width = Math.Max(200, header.Width - 68);
        return header;
    }

    public static Panel CreateFooter(Form form, string hint, Action? onRefresh = null)
    {
        var footer = new Panel
        {
            Height = 52,
            Dock = DockStyle.Bottom,
            BackColor = AppTheme.SurfaceCard,
        };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var label = new Label
        {
            Text = hint,
            AutoSize = false,
            Location = new Point(16, 8),
            Size = new Size(Math.Max(120, form.ClientSize.Width - 240), 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = AppTheme.TextMute,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        if (onRefresh is not null)
        {
            var refresh = CreateButton("刷新", false);
            refresh.Size = new Size(88, 34);
            refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            refresh.Location = new Point(form.ClientSize.Width - 204, 9);
            refresh.Click += (_, _) => onRefresh();
            footer.Controls.Add(refresh);
        }

        var close = CreateButton("关闭", true);
        close.Size = new Size(88, 34);
        close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        close.Location = new Point(form.ClientSize.Width - 104, 9);
        close.DialogResult = DialogResult.Cancel;
        form.CancelButton = close;

        footer.Controls.Add(label);
        footer.Controls.Add(close);
        return footer;
    }

    public static Button CreateButton(string text, bool primary)
    {
        var b = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = primary ? AppTheme.Primary : AppTheme.SurfaceCard,
            ForeColor = primary ? AppTheme.TextOnPrimary : AppTheme.TextMain,
            Font = new Font("Microsoft YaHei UI", 9F),
        };
        if (primary) b.FlatAppearance.BorderSize = 0;
        else
        {
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.MouseEnter += (_, _) => b.BackColor = AppTheme.PrimaryPale;
            b.MouseLeave += (_, _) => b.BackColor = AppTheme.SurfaceCard;
        }
        return b;
    }

    public static Panel CreateSection(string title, Control[] rows)
    {
        var card = new Panel { BackColor = AppTheme.SurfaceCard, Padding = new Padding(10, 6, 10, 8) };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var host = new Panel { Dock = DockStyle.Fill, AutoScroll = false };
        for (var i = rows.Length - 1; i >= 0; i--)
            host.Controls.Add(rows[i]);

        var cap = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = AppTheme.TextHeader,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        card.Height = 34 + rows.Length * 38 + 12;
        card.Controls.Add(host);
        card.Controls.Add(cap);
        return card;
    }
}
