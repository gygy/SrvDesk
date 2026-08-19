using System.Reflection;

namespace WinOpt;

internal sealed class MainForm : Form
{
    private readonly SettingRow _cpu = Row("CPU 资源分配（程序优先）", "后台服务优先");
    private readonly SettingRow _dep = Row("数据执行保护 DEP（T）", "按系统策略");
    private readonly SettingRow _uac = Row("禁用用户账户控制 UAC", "启用");
    private readonly SettingRow _ie = Row("关闭 IE 增强安全配置", "开启");
    private readonly SettingRow _thisPc = Row("显示桌面「此电脑」图标", "不显示");
    private readonly SettingRow _taskbar = Row("使用小按钮任务栏", "标准大小");
    private readonly SettingRow _confirmDel = Row("显示删除确认对话框", "不提示");
    private readonly SettingRow _audio = Row("启动音频服务", "不启动");
    private readonly SettingRow _svrMgr = Row("登录不启动服务管理器", "自动打开");
    private readonly SettingRow _azure = Row("禁止启动 Azure Arc 托盘", "允许启动");
    private readonly SettingRow _pwd = Row("禁用密码复杂性要求", "必须符合");
    private readonly SettingRow _shutdownLogon = Row("允许未登录时关机", "不允许");
    private readonly SettingRow _shutdownReason = Row("关闭关机事件跟踪", "显示");
    private readonly SettingRow _noCad = Row("无需 Ctrl+Alt+Del 登录", "需要按键");

    private readonly Panel _contentHost = new();
    private readonly Label _status = new();
    private readonly Button _apply = new();
    private readonly List<(string Title, SettingRow[] Rows)> _groups = [];
    private readonly ListBox _menu = new();
    private int _menuHover = -1;

    private static readonly string[] MenuItems =
    [
        "性能及安全",
        "个性化设置",
        "启动项",
        "账户策略",
    ];

    private SettingRow[] AllRows =>
    [
        _cpu, _dep, _uac, _ie, _thisPc, _taskbar, _confirmDel, _audio,
        _svrMgr, _azure, _pwd, _shutdownLogon, _shutdownReason, _noCad
    ];

    public MainForm()
    {
        Text = "Win一键优化";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 640);
        ClientSize = new Size(1040, 700);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextMain;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* 设计时 */ }

        _groups.Add(("性能及安全", [_cpu, _dep, _uac, _ie]));
        _groups.Add(("个性化设置", [_thisPc, _taskbar, _confirmDel, _audio]));
        _groups.Add(("启动项", [_svrMgr, _azure]));
        _groups.Add(("账户策略", [_pwd, _shutdownLogon, _shutdownReason, _noCad]));

        var header = BuildHeader();
        var sidebar = BuildSidebar();
        var bottom = BuildBottom();
        BuildContent();

        header.Dock = DockStyle.Top;
        sidebar.Dock = DockStyle.Left;
        _contentHost.Dock = DockStyle.Fill;
        bottom.Dock = DockStyle.Bottom;

        Controls.Add(_contentHost);
        Controls.Add(sidebar);
        Controls.Add(bottom);
        Controls.Add(header);

        _menu.SelectedIndex = 0;
        ShowGroup(0);
        Load += (_, _) => LoadState();
        Resize += (_, _) => LayoutContent();
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Height = 56, BackColor = AppTheme.PrimaryDeep };
        header.Paint += (_, e) =>
        {
            var r = header.ClientRectangle;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                r, AppTheme.HeaderBarTop, AppTheme.HeaderBarBottom, 90f);
            e.Graphics.FillRectangle(brush, r);
        };

        var logo = new PictureBox
        {
            Size = new Size(40, 40),
            Location = new Point(14, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };
        var logoImg = LoadLogo();
        if (logoImg is not null) logo.Image = logoImg;

        var brand = new Label
        {
            Text = "Win一键优化",
            AutoSize = false,
            Location = new Point(58, 0),
            Size = new Size(220, 56),
            ForeColor = AppTheme.TextOnPrimary,
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };
        var sub = new Label
        {
            Text = "Windows Server 系统优化",
            AutoSize = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(760, 0),
            Size = new Size(260, 56),
            ForeColor = AppTheme.TextOnPrimarySoft,
            Font = new Font("Microsoft YaHei UI", 9F),
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent,
        };

        header.Controls.Add(sub);
        header.Controls.Add(brand);
        header.Controls.Add(logo);
        return header;
    }

    private static Image? LoadLogo()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("WinOpt.app.png");
            if (stream is not null) return Image.FromStream(stream);
        }
        catch { /* ignore */ }

        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.png");
        return File.Exists(path) ? Image.FromFile(path) : null;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 176, BackColor = AppTheme.NavBg };
        var cap = new Label
        {
            Text = "  功能导航",
            Dock = DockStyle.Top,
            Height = 36,
            ForeColor = AppTheme.TextMute,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = AppTheme.NavBg,
        };
        _menu.Dock = DockStyle.Fill;
        _menu.BorderStyle = BorderStyle.None;
        _menu.BackColor = AppTheme.NavBg;
        _menu.ForeColor = AppTheme.TextMain;
        _menu.IntegralHeight = false;
        _menu.DrawMode = DrawMode.OwnerDrawFixed;
        _menu.ItemHeight = 44;
        _menu.Items.AddRange(MenuItems);
        _menu.DrawItem += DrawMenuItem;
        _menu.SelectedIndexChanged += (_, _) => ShowGroup(_menu.SelectedIndex);
        _menu.MouseMove += (_, e) =>
        {
            var i = _menu.IndexFromPoint(e.Location);
            if (i != _menuHover) { _menuHover = i; _menu.Invalidate(); }
        };
        _menu.MouseLeave += (_, _) => { _menuHover = -1; _menu.Invalidate(); };
        sidebar.Controls.Add(_menu);
        sidebar.Controls.Add(cap);
        return sidebar;
    }

    private void BuildContent()
    {
        _contentHost.BackColor = AppTheme.Surface;
        _contentHost.Padding = new Padding(12, 8, 12, 8);
        _contentHost.AutoScroll = true;
    }

    private void ShowGroup(int index)
    {
        if (index < 0 || index >= _groups.Count) return;
        _contentHost.SuspendLayout();
        _contentHost.Controls.Clear();

        var wrap = new Panel
        {
            Location = new Point(0, 0),
            Width = ContentWidth(),
            AutoSize = true,
            BackColor = AppTheme.SurfaceCard,
        };
        wrap.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, wrap.Width - 1, wrap.Height - 1);
        };

        var header = BuildTableHeader();
        wrap.Controls.Add(header);

        var group = _groups[index];
        var section = BuildGroupSection(group.Title, group.Rows);
        wrap.Controls.Add(section);
        section.Location = new Point(0, header.Bottom);

        wrap.Height = section.Bottom;
        _contentHost.Controls.Add(wrap);
        _contentHost.ResumeLayout(true);
    }

    private int ContentWidth() =>
        Math.Max(760, _contentHost.ClientSize.Width - _contentHost.Padding.Horizontal);

    private void LayoutContent()
    {
        if (_contentHost.Controls.Count == 0) return;
        if (_contentHost.Controls[0] is Panel wrap)
            wrap.Width = ContentWidth();
    }

    private Panel BuildTableHeader()
    {
        const int h = 36;
        var header = new Panel
        {
            Location = new Point(0, 0),
            Height = h,
            BackColor = AppTheme.PrimaryLight,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        header.Controls.Add(MakeHeaderLabel("项目", 16, 420));
        header.Controls.Add(MakeHeaderLabel("当前用户", 448, 160, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderLabel("系统", 628, 160, ContentAlignment.MiddleCenter));
        header.Resize += (_, _) => header.Width = ContentWidth();
        header.Width = ContentWidth();
        return header;
    }

    private static Label MakeHeaderLabel(string text, int x, int w, ContentAlignment align = ContentAlignment.MiddleLeft) => new()
    {
        Text = text,
        Location = new Point(x, 0),
        Size = new Size(w, 36),
        ForeColor = AppTheme.TextHeader,
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        TextAlign = align,
        BackColor = Color.Transparent,
    };

    private Panel BuildGroupSection(string title, SettingRow[] rows)
    {
        const int headerH = 34;
        const int rowH = 38;
        var expanded = true;
        var section = new Panel
        {
            Location = new Point(0, 0),
            Width = ContentWidth(),
            Height = headerH + rows.Length * rowH,
            BackColor = AppTheme.SurfaceCard,
        };

        var head = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(section.Width, headerH),
            BackColor = AppTheme.GroupBg,
            Cursor = Cursors.Hand,
        };
        var arrow = new Label
        {
            Text = "▼",
            Location = new Point(12, 8),
            AutoSize = true,
            ForeColor = AppTheme.PrimaryDark,
            Font = new Font("Segoe UI Symbol", 8F),
            BackColor = Color.Transparent,
        };
        var titleLabel = new Label
        {
            Text = title,
            Location = new Point(32, 0),
            Size = new Size(section.Width - 48, headerH),
            ForeColor = AppTheme.TextHeader,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };
        head.Controls.Add(arrow);
        head.Controls.Add(titleLabel);

        var body = new Panel
        {
            Location = new Point(0, headerH),
            Size = new Size(section.Width, rows.Length * rowH),
            BackColor = AppTheme.SurfaceCard,
        };

        for (var i = 0; i < rows.Length; i++)
        {
            var bg = i % 2 == 0 ? AppTheme.SurfaceCard : AppTheme.RowAlt;
            rows[i].Mount(body, i * rowH, rowH, bg, section.Width);
        }

        void Toggle(object? _, EventArgs __)
        {
            expanded = !expanded;
            arrow.Text = expanded ? "▼" : "▶";
            body.Visible = expanded;
            section.Height = expanded ? headerH + rows.Length * rowH : headerH;
        }

        head.Click += Toggle;
        arrow.Click += Toggle;
        titleLabel.Click += Toggle;

        section.Controls.Add(body);
        section.Controls.Add(head);
        section.Resize += (_, _) =>
        {
            head.Width = section.Width;
            body.Width = section.Width;
            titleLabel.Width = section.Width - 48;
        };
        return section;
    }

    private Panel BuildBottom()
    {
        var bottom = new Panel { Height = 54, BackColor = AppTheme.SurfaceCard };
        var rule = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = AppTheme.BorderLight };

        _status.AutoSize = false;
        _status.SetBounds(188, 16, 400, 22);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        _status.ForeColor = AppTheme.TextMute;
        _status.Text = Optimizer.IsWindowsServer()
            ? "中间开关为推荐设置；「系统」列为出厂默认值，仅展示。"
            : "当前系统可能不是 Windows Server。";

        var allOn = ToolButton("全部选择", 620, 10, 88, () => SetAll(true));
        var allOff = ToolButton("全部取消", 714, 10, 88, () => SetAll(false));
        var refresh = ToolButton("刷新", 808, 10, 72, LoadState);
        var about = ToolButton("关于", 884, 10, 72, ShowAbout);
        foreach (var b in new[] { allOn, allOff, refresh, about })
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _apply.Text = "一键优化";
        _apply.SetBounds(962, 8, 68, 36);
        _apply.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _apply.FlatStyle = FlatStyle.Flat;
        _apply.FlatAppearance.BorderSize = 0;
        _apply.BackColor = AppTheme.Primary;
        _apply.ForeColor = AppTheme.TextOnPrimary;
        _apply.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _apply.Cursor = Cursors.Hand;
        _apply.Click += (_, _) => Apply();
        _apply.MouseEnter += (_, _) => _apply.BackColor = AppTheme.PrimaryDark;
        _apply.MouseLeave += (_, _) => _apply.BackColor = AppTheme.Primary;

        bottom.Controls.AddRange([rule, _status, allOn, allOff, refresh, about, _apply]);
        return bottom;
    }

    private void DrawMenuItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        var hover = e.Index == _menuHover && !selected;
        using var back = new SolidBrush(selected ? AppTheme.Primary : hover ? AppTheme.NavHover : AppTheme.NavBg);
        e.Graphics.FillRectangle(back, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(AppTheme.PrimarySoft);
            e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 8, 3, e.Bounds.Height - 16);
        }
        TextRenderer.DrawText(
            e.Graphics,
            MenuItems[e.Index],
            selected ? new Font(Font, FontStyle.Bold) : Font,
            new Rectangle(e.Bounds.X + 18, e.Bounds.Y, e.Bounds.Width - 22, e.Bounds.Height),
            selected ? AppTheme.TextOnPrimary : AppTheme.TextMain,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    private void LoadState()
    {
        try { Bind(Optimizer.Read()); }
        catch (Exception ex) { _status.Text = "读取当前配置失败：" + ex.Message; }
    }

    private void Bind(Optimizer.State s)
    {
        _cpu.Checked = s.CpuProgramPriority;
        _dep.Checked = s.Dep;
        _uac.Checked = s.DisableUac;
        _ie.Checked = s.DisableIeEsc;
        _thisPc.Checked = s.ShowThisPcIcon;
        _taskbar.Checked = s.SmallTaskbar;
        _confirmDel.Checked = s.ConfirmDelete;
        _audio.Checked = s.EnableAudio;
        _svrMgr.Checked = s.SkipServerManager;
        _azure.Checked = s.DisableAzureArc;
        _pwd.Checked = s.DisablePasswordComplexity;
        _shutdownLogon.Checked = s.ShutdownWithoutLogon;
        _shutdownReason.Checked = s.DisableShutdownReason;
        _noCad.Checked = s.DisableCad;
    }

    private Optimizer.State CaptureState() => new()
    {
        CpuProgramPriority = _cpu.Checked,
        Dep = _dep.Checked,
        DisableUac = _uac.Checked,
        DisableIeEsc = _ie.Checked,
        ShowThisPcIcon = _thisPc.Checked,
        SmallTaskbar = _taskbar.Checked,
        ConfirmDelete = _confirmDel.Checked,
        EnableAudio = _audio.Checked,
        SkipServerManager = _svrMgr.Checked,
        DisableAzureArc = _azure.Checked,
        DisablePasswordComplexity = _pwd.Checked,
        ShutdownWithoutLogon = _shutdownLogon.Checked,
        DisableShutdownReason = _shutdownReason.Checked,
        DisableCad = _noCad.Checked,
    };

    private void SetAll(bool on)
    {
        foreach (var row in AllRows) row.Checked = on;
    }

    private void Apply()
    {
        _apply.Enabled = false;
        UseWaitCursor = true;
        _status.Text = "正在应用…";
        Application.DoEvents();
        try
        {
            var errors = Optimizer.Apply(CaptureState());
            LoadState();
            _status.Text = errors.Count == 0
                ? "已应用。开关开启为推荐设置，关闭则恢复系统默认。部分项目需注销或重启后生效。"
                : "部分失败：\r\n" + string.Join("\r\n", errors);
        }
        catch (Exception ex)
        {
            _status.Text = "应用失败：" + ex.Message;
        }
        finally
        {
            UseWaitCursor = false;
            _apply.Enabled = true;
        }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "Windows Server 日常使用优化工具。\r\n中间开关控制是否采用推荐设置；右侧「系统」列为出厂默认值。",
            "关于 Win一键优化",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static SettingRow Row(string item, string systemDefault) =>
        new(item, systemDefault);

    private static Button ToolButton(string text, int x, int y, int w, Action click)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.SurfaceCard,
            ForeColor = AppTheme.TextMain,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = AppTheme.Border;
        b.MouseEnter += (_, _) => b.BackColor = AppTheme.PrimaryPale;
        b.MouseLeave += (_, _) => b.BackColor = AppTheme.SurfaceCard;
        b.Click += (_, _) => click();
        return b;
    }

    private sealed class SettingRow
    {
        private readonly ToggleSwitch _toggle;
        private readonly Label _item;
        private readonly Label _system;

        public SettingRow(string item, string systemDefault)
        {
            _item = new Label
            {
                Text = item,
                AutoSize = false,
                ForeColor = AppTheme.TextMain,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            _toggle = new ToggleSwitch();
            _system = new Label
            {
                Text = systemDefault,
                AutoSize = false,
                ForeColor = AppTheme.TextMute,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
        }

        public bool Checked
        {
            get => _toggle.Checked;
            set => _toggle.Checked = value;
        }

        public void Mount(Control parent, int y, int h, Color bg, int width)
        {
            var wrap = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(width, h),
                BackColor = bg,
            };
            _item.SetBounds(16, 0, 420, h);
            _toggle.Location = new Point(500, (h - _toggle.Height) / 2);
            _system.SetBounds(628, 0, 160, h);
            wrap.Controls.Add(_item);
            wrap.Controls.Add(_toggle);
            wrap.Controls.Add(_system);
            wrap.Controls.Add(new Panel
            {
                BackColor = AppTheme.BorderLight,
                Dock = DockStyle.Bottom,
                Height = 1,
            });
            parent.Controls.Add(wrap);
        }
    }
}
