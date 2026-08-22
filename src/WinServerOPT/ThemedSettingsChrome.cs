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
        MinimumSize = new Size(200, 36);
        Margin = new Padding(0, 0, 0, 4);
        Padding = new Padding(4, 0, 8, 0);
        BackColor = Color.Transparent;

        // Dock 布局：右侧固定槽放开关，文字填满左侧；行高恒定，不在 Resize 里改尺寸
        var right = new Panel
        {
            Dock = DockStyle.Right,
            Width = 64,
            Padding = new Padding(4, 5, 0, 5),
            BackColor = Color.Transparent,
        };
        _toggle.Dock = DockStyle.Fill;
        right.Controls.Add(_toggle);

        _label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent,
            AutoEllipsis = false,
            Padding = new Padding(0, 0, 8, 0),
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
        Controls.Add(right);
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
        if (Parent is not FlowLayoutPanel flp || flp.FlowDirection != FlowDirection.TopDown)
            return;
        var w = Math.Max(MinimumSize.Width, flp.ClientSize.Width - Margin.Horizontal - 4);
        if (Width != w)
            Width = w;
        if (Height != 36)
            Height = 36;
    }
}

internal static class ThemedSettingsChrome
{
    public static Panel CreateHeader(string title, string subtitle)
    {
        var header = new Panel { Height = 56, Dock = DockStyle.Top, BackColor = AppTheme.PrimaryDeep };
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
            Location = new Point(14, 10),
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
            Size = new Size(520, 24),
            ForeColor = AppTheme.TextOnPrimary,
            Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            AutoEllipsis = false,
        };

        var sub = new Label
        {
            Text = subtitle,
            AutoSize = false,
            Location = new Point(54, 30),
            Height = 22,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = AppTheme.TextOnPrimarySoft,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            AutoEllipsis = false,
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
            Location = new Point(16, 4),
            Size = new Size(Math.Max(120, form.ClientSize.Width - 24 - right), 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            ForeColor = AppTheme.TextMute,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false,
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

    /// <summary>带标题分区：高度随正文自适应，标题完整显示。</summary>
    public static (Panel Card, FlowLayoutPanel Body) CreateSectionShell(string title, int minHeight = 0)
    {
        var card = new Panel
        {
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(10, 8, 10, 10),
            Margin = new Padding(0, 0, 0, 8),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
        };
        if (minHeight > 0)
            card.MinimumSize = new Size(0, minHeight);

        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var cap = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = AppTheme.TextHeader,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false,
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(0, 2, 0, 2),
        };
        body.Resize += (_, _) => StretchStackChildren(body);

        // 先 body 后 cap：cap 停靠在最上，body 在其下并随内容增高
        card.Controls.Add(body);
        card.Controls.Add(cap);
        card.Tag = body;
        return (card, body);
    }

    public static Panel CreateSection(string title, Control[] rows)
    {
        var (card, body) = CreateSectionShell(title);
        body.SuspendLayout();
        foreach (var row in rows)
            body.Controls.Add(row);
        StretchStackChildren(body);
        body.ResumeLayout(true);
        return card;
    }

    public static Panel CreateSectionCard(string title, int height = 0)
    {
        var (card, body) = CreateSectionShell(title, height);
        card.Tag = body;
        return card;
    }

    public static Panel SectionBody(Panel card) =>
        card.Tag as Panel ?? card;

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

    public static FlowLayoutPanel CreateToggleStack()
    {
        var p = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoScroll = false,
            BackColor = Color.Transparent,
        };
        p.Resize += (_, _) => StretchStackChildren(p);
        return p;
    }

    public static void StretchStackChildren(FlowLayoutPanel p)
    {
        var w = Math.Max(160, p.ClientSize.Width - 4);
        foreach (Control c in p.Controls)
        {
            if (c.Width != w)
                c.Width = w;
        }
    }

    public static Panel CreateComboRow(string label, ComboBox box, string[] items)
    {
        var row = new Panel
        {
            Height = 36,
            Margin = new Padding(0, 0, 0, 4),
            MinimumSize = new Size(200, 36),
            Padding = new Padding(4, 0, 8, 0),
            BackColor = Color.Transparent,
        };
        var l = new Label
        {
            Text = label,
            Dock = DockStyle.Left,
            Width = 88,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false,
            BackColor = Color.Transparent,
        };
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.Items.Clear();
        box.Items.AddRange(items);
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 5, 0, 5),
            BackColor = Color.Transparent,
        };
        box.Dock = DockStyle.Fill;
        host.Controls.Add(box);
        row.Controls.Add(host);
        row.Controls.Add(l);
        row.ParentChanged += (_, _) =>
        {
            if (row.Parent is not FlowLayoutPanel flp || flp.FlowDirection != FlowDirection.TopDown)
                return;
            void Sync(object? s, EventArgs e)
            {
                var w = Math.Max(row.MinimumSize.Width, flp.ClientSize.Width - row.Margin.Horizontal - 4);
                if (row.Width != w) row.Width = w;
            }
            flp.Resize -= Sync;
            flp.Resize += Sync;
            Sync(null, EventArgs.Empty);
        };
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
