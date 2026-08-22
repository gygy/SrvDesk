namespace WinOpt;

internal sealed class InstantToggleRow : Panel
{
    private readonly ToggleSwitch _toggle = new();
    private readonly Label _label;
    private bool _suppress;
    private Action<bool>? _apply;

    public InstantToggleRow(string title)
    {
        Title = title;
        Height = 36;
        Dock = DockStyle.Top;
        MinimumSize = new Size(240, 36);
        Margin = new Padding(0, 0, 0, 2);
        BackColor = Color.Transparent;

        _toggle.Location = new Point(4, 5);
        _label = new Label
        {
            Text = title,
            AutoSize = false,
            Location = new Point(68, 0),
            Size = new Size(400, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent,
            AutoEllipsis = true,
        };
        _label.Click += (_, _) => _toggle.Checked = !_toggle.Checked;
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
        Controls.Add(_label);
        Controls.Add(_toggle);
        Resize += (_, _) => LayoutLabel();
        ParentChanged += (_, _) =>
        {
            if (Parent is null) return;
            Parent.Resize -= OnParentResize;
            Parent.Resize += OnParentResize;
            SyncWidthToParent();
        };
    }

    public string Title { get; }

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

    private void OnParentResize(object? sender, EventArgs e) => SyncWidthToParent();

    private void SyncWidthToParent()
    {
        // FlowLayoutPanel 会忽略 Dock，需手动拉满宽度，否则长标题被裁切
        if (Parent is FlowLayoutPanel flp && flp.FlowDirection == FlowDirection.TopDown)
        {
            var w = Math.Max(MinimumSize.Width, flp.ClientSize.Width - Margin.Horizontal - 4);
            if (Width != w) Width = w;
        }
        LayoutLabel();
    }

    private void LayoutLabel() =>
        _label.Width = Math.Max(120, ClientSize.Width - 76);
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
            AutoEllipsis = true,
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
            AutoEllipsis = true,
        };

        header.Controls.Add(sub);
        header.Controls.Add(titleLabel);
        header.Controls.Add(logo);
        header.Resize += (_, _) =>
        {
            var w = Math.Max(200, header.Width - 68);
            titleLabel.Width = w;
            sub.Width = w;
        };
        return header;
    }

    public static Panel CreateFooter(Form form, string hint, Action? onRefresh = null, bool showClose = true)
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

        var right = (showClose ? 104 : 16) + (onRefresh is not null ? 100 : 0);
        var label = new Label
        {
            Text = hint,
            AutoSize = false,
            Location = new Point(16, 8),
            Size = new Size(Math.Max(120, form.ClientSize.Width - 24 - right), 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = AppTheme.TextMute,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        };

        if (onRefresh is not null)
        {
            var refresh = CreateButton("刷新", false);
            refresh.Size = new Size(88, 34);
            refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            refresh.Location = new Point(form.ClientSize.Width - (showClose ? 204 : 104), 9);
            refresh.Click += (_, _) => onRefresh();
            footer.Controls.Add(refresh);
        }

        if (showClose)
        {
            var close = CreateButton("关闭", true);
            close.Size = new Size(88, 34);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            close.Location = new Point(form.ClientSize.Width - 104, 9);
            close.DialogResult = DialogResult.Cancel;
            form.CancelButton = close;
            footer.Controls.Add(close);
        }

        footer.Controls.Add(label);
        footer.Resize += (_, _) =>
            label.Width = Math.Max(80, footer.ClientSize.Width - 24 - right);
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

    /// <summary>带标题分区：标题与正文分栏，避免互相遮挡。</summary>
    public static (Panel Card, Panel Body) CreateSectionShell(string title, int height = 0)
    {
        var card = new Panel
        {
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(10, 8, 10, 10),
            Margin = new Padding(0, 0, 0, 8),
        };
        if (height > 0) card.Height = height;

        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.SurfaceCard,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var cap = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = AppTheme.TextHeader,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        };
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
            AutoScroll = false,
        };
        layout.Controls.Add(cap, 0, 0);
        layout.Controls.Add(body, 0, 1);
        card.Controls.Add(layout);
        return (card, body);
    }

    public static Panel CreateSection(string title, Control[] rows)
    {
        var (card, body) = CreateSectionShell(title, 36 + rows.Length * 38 + 12);
        for (var i = rows.Length - 1; i >= 0; i--)
        {
            rows[i].Dock = DockStyle.Top;
            body.Controls.Add(rows[i]);
        }
        return card;
    }

    /// <summary>兼容旧调用：正文面板放在 Tag。</summary>
    public static Panel CreateSectionCard(string title, int height = 0)
    {
        var (card, body) = CreateSectionShell(title, height);
        card.Tag = body;
        return card;
    }

    public static Panel SectionBody(Panel card) =>
        card.Tag as Panel
        ?? card.Controls.OfType<TableLayoutPanel>().FirstOrDefault()?.GetControlFromPosition(0, 1) as Panel
        ?? card;

    public static Panel CreateBodyPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            AutoScroll = true,
            BackColor = AppTheme.Surface,
        };
    }

    /// <summary>纵向开关列表容器（自动拉满宽度）。</summary>
    public static FlowLayoutPanel CreateToggleStack()
    {
        var p = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
        };
        p.Resize += (_, _) =>
        {
            foreach (Control c in p.Controls)
            {
                if (c is InstantToggleRow or Panel)
                    c.Width = Math.Max(200, p.ClientSize.Width - 4);
            }
        };
        return p;
    }

    public static Panel CreateComboRow(string label, ComboBox box, string[] items)
    {
        var row = new Panel { Height = 36, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 2) };
        var l = new Label
        {
            Text = label,
            AutoSize = false,
            Location = new Point(4, 0),
            Size = new Size(160, 36),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        };
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.Items.Clear();
        box.Items.AddRange(items);
        box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        box.Location = new Point(168, 5);
        box.Width = 200;
        row.Controls.Add(box);
        row.Controls.Add(l);
        row.Resize += (_, _) => box.Width = Math.Max(120, row.ClientSize.Width - 176);
        return row;
    }

    public static void MountModal(
        Form form,
        string title,
        string subtitle,
        Control body,
        string footerHint,
        Action? onRefresh = null)
    {
        form.BackColor = AppTheme.Surface;
        form.Font = new Font("Microsoft YaHei UI", 9F);
        body.Dock = DockStyle.Fill;
        var header = CreateHeader(title, subtitle);
        var footer = CreateFooter(form, footerHint, onRefresh, showClose: true);
        form.Controls.Add(body);
        form.Controls.Add(footer);
        form.Controls.Add(header);
    }

    public static void MountEmbedded(
        Form form,
        string title,
        string subtitle,
        Control body,
        string footerHint,
        Action? onRefresh = null)
    {
        form.BackColor = AppTheme.Surface;
        form.Font = new Font("Microsoft YaHei UI", 9F);
        body.Dock = DockStyle.Fill;
        var footer = CreateFooter(form, footerHint, onRefresh, showClose: false);
        form.Controls.Add(body);
        form.Controls.Add(footer);
    }
}
