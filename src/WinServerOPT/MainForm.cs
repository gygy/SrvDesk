namespace WinOpt;

internal sealed class MainForm : Form
{
    private readonly SettingRow _cpu = Row("CPU资源分配程序优先", "后台服务优先");
    private readonly SettingRow _dep = Row("数据执行保护DEP（T）", "按系统策略");
    private readonly SettingRow _uac = Row("禁用用户账户控制UAC", "启用");
    private readonly SettingRow _ie = Row("关闭IE增强安全配置", "开启");
    private readonly SettingRow _thisPc = Row("桌面此电脑图标", "不显示");
    private readonly SettingRow _taskbar = Row("使用小按钮任务栏", "标准大小");
    private readonly SettingRow _confirmDel = Row("显示删除确认对话框", "不提示");
    private readonly SettingRow _audio = Row("启动音频服务", "不启动");
    private readonly SettingRow _svrMgr = Row("登录不启动服务管理器", "自动打开");
    private readonly SettingRow _azure = Row("禁止启动Azure Arc", "允许启动");
    private readonly SettingRow _pwd = Row("禁用密码符合复杂性要求", "必须符合");
    private readonly SettingRow _shutdownLogon = Row("允许未登录时关机", "不允许");
    private readonly SettingRow _shutdownReason = Row("关闭显示事件跟踪程序", "显示");
    private readonly SettingRow _noCad = Row("无需Ctrl+Alt+Del登录", "需要按键");

    private readonly ListBox _menu = new();
    private readonly TabControl _tabs = new();
    private readonly TabPage[] _pages = new TabPage[4];
    private readonly Label _status = new();
    private readonly Button _apply = new();
    private int _menuHover = -1;

    private static readonly Color Navy = Color.FromArgb(11, 31, 58);
    private static readonly Color NavySoft = Color.FromArgb(18, 44, 76);
    private static readonly Color Gold = Color.FromArgb(201, 162, 39);
    private static readonly Color Canvas = Color.FromArgb(241, 243, 246);
    private static readonly Color Line = Color.FromArgb(226, 230, 236);
    private static readonly Color TextMain = Color.FromArgb(28, 37, 48);
    private static readonly Color TextMute = Color.FromArgb(107, 118, 132);
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
        MinimumSize = new Size(1000, 640);
        ClientSize = new Size(1040, 660);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Canvas;
        ForeColor = TextMain;

        var header = BuildHeader();
        var sidebar = BuildSidebar();
        var bottom = BuildBottom();
        BuildTabs();

        _tabs.Dock = DockStyle.Fill;
        sidebar.Dock = DockStyle.Left;
        header.Dock = DockStyle.Top;
        bottom.Dock = DockStyle.Bottom;

        Controls.Add(_tabs);
        Controls.Add(sidebar);
        Controls.Add(bottom);
        Controls.Add(header);

        _menu.SelectedIndex = 0;
        Load += (_, _) => LoadState();
    }

    private Panel BuildHeader()
    {
        var header = new Panel
        {
            Height = 58,
            BackColor = Navy,
            Padding = new Padding(18, 0, 20, 0),
        };
        var brand = new Label
        {
            Text = "Win一键优化",
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 220,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var sub = new Label
        {
            Text = "Windows Server  系统优化",
            AutoSize = false,
            Dock = DockStyle.Right,
            Width = 220,
            ForeColor = Color.FromArgb(196, 176, 122),
            Font = new Font("Microsoft YaHei UI", 9F),
            TextAlign = ContentAlignment.MiddleRight,
        };
        header.Controls.Add(sub);
        header.Controls.Add(brand);
        return header;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 196, BackColor = Navy };
        var cap = new Label
        {
            Text = "  功能导航",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = Color.FromArgb(168, 178, 190),
            Font = new Font("Microsoft YaHei UI", 8.5F),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _menu.Dock = DockStyle.Fill;
        _menu.BorderStyle = BorderStyle.None;
        _menu.BackColor = Navy;
        _menu.ForeColor = Color.White;
        _menu.IntegralHeight = false;
        _menu.DrawMode = DrawMode.OwnerDrawFixed;
        _menu.ItemHeight = 46;
        _menu.Items.AddRange(MenuItems);
        _menu.DrawItem += DrawMenuItem;
        _menu.SelectedIndexChanged += (_, _) => ShowSelectedPage();
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

    private void BuildTabs()
    {
        _tabs.Padding = new Point(14, 6);
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.ItemSize = new Size(132, 34);
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.DrawItem += DrawTab;
        _pages[0] = NewPage("性能及安全", "处理器调度、内存防护、UAC 与 IE 增强安全。", _cpu, _dep, _uac, _ie);
        _pages[1] = NewPage("个性化设置", "桌面图标、任务栏、删除确认与音频服务。", _thisPc, _taskbar, _confirmDel, _audio);
        _pages[2] = NewPage("启动项", "登录后的服务管理器与 Azure Arc。", _svrMgr, _azure);
        _pages[3] = NewPage("账户策略", "密码复杂性、关机行为与安全登录提示。", _pwd, _shutdownLogon, _shutdownReason, _noCad);
    }

    private Panel BuildBottom()
    {
        var bottom = new Panel { Height = 78, BackColor = Color.White };
        var rule = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Line };

        _status.AutoSize = false;
        _status.SetBounds(20, 14, 500, 50);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _status.ForeColor = TextMute;
        _status.Text = Optimizer.IsWindowsServer()
            ? "勾选表示采用推荐设置。右侧「系统默认值」仅供对照，不需要选择。"
            : "当前系统可能不是 Windows Server。本工具面向 Server 日常使用优化。";

        var allOn = GhostButton("全部选择", 548, 22, 92, () => SetAll(true));
        var allOff = GhostButton("全部取消", 648, 22, 92, () => SetAll(false));
        var about = GhostButton("关于", 748, 22, 72, ShowAbout);
        allOn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        allOff.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        about.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _apply.Text = "一键优化";
        _apply.SetBounds(832, 18, 116, 40);
        _apply.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _apply.FlatStyle = FlatStyle.Flat;
        _apply.FlatAppearance.BorderSize = 0;
        _apply.BackColor = Navy;
        _apply.ForeColor = Color.White;
        _apply.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _apply.Cursor = Cursors.Hand;
        _apply.Click += (_, _) => Apply();
        _apply.MouseEnter += (_, _) => _apply.BackColor = NavySoft;
        _apply.MouseLeave += (_, _) => _apply.BackColor = Navy;

        bottom.Controls.AddRange([rule, _status, allOn, allOff, about, _apply]);
        return bottom;
    }

    private void ShowSelectedPage()
    {
        var i = _menu.SelectedIndex;
        if (i < 0 || i >= _pages.Length || _pages[i] is null) return;
        if (_tabs.TabPages.Count == 1 && ReferenceEquals(_tabs.TabPages[0], _pages[i])) return;
        _tabs.TabPages.Clear();
        _tabs.TabPages.Add(_pages[i]);
    }

    private void DrawMenuItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        var hover = e.Index == _menuHover && !selected;
        using var back = new SolidBrush(selected ? NavySoft : hover ? Color.FromArgb(16, 38, 68) : Navy);
        e.Graphics.FillRectangle(back, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Gold);
            e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 10, 3, e.Bounds.Height - 20);
        }
        TextRenderer.DrawText(
            e.Graphics,
            MenuItems[e.Index],
            selected ? new Font(Font, FontStyle.Bold) : Font,
            new Rectangle(e.Bounds.X + 22, e.Bounds.Y, e.Bounds.Width - 28, e.Bounds.Height),
            selected ? Color.White : Color.FromArgb(214, 220, 228),
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    private void DrawTab(object sender, DrawItemEventArgs e)
    {
        var selected = e.Index == _tabs.SelectedIndex;
        using var back = new SolidBrush(selected ? Color.White : Canvas);
        e.Graphics.FillRectangle(back, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Gold);
            e.Graphics.FillRectangle(accent, e.Bounds.X + 10, e.Bounds.Bottom - 3, e.Bounds.Width - 20, 3);
        }
        var text = e.Index >= 0 && e.Index < _tabs.TabPages.Count ? _tabs.TabPages[e.Index].Text : "";
        TextRenderer.DrawText(
            e.Graphics,
            text,
            selected ? new Font(Font, FontStyle.Bold) : Font,
            e.Bounds,
            selected ? Navy : TextMute,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static TabPage NewPage(string title, string hint, params SettingRow[] rows)
    {
        var page = new TabPage(title)
        {
            BackColor = Canvas,
            UseVisualStyleBackColor = false,
        };
        var hintLabel = new Label
        {
            Text = hint + "  勾选采用推荐；右侧为系统默认值，仅展示。",
            AutoSize = false,
            ForeColor = TextMute,
            Location = new Point(20, 12),
            Size = new Size(760, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        page.Controls.Add(hintLabel);

        const int headerH = 36;
        const int rowH = 44;
        var card = new Panel
        {
            Location = new Point(20, 40),
            Size = new Size(760, headerH + rows.Length * rowH),
            BackColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Line);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        card.Controls.Add(ColHeader("推荐设置", 16, 420, TextMain));
        card.Controls.Add(ColHeader("系统默认值", 500, 240, TextMute));
        var headLine = new Panel
        {
            BackColor = Line,
            Location = new Point(0, headerH - 1),
            Size = new Size(760, 1),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        card.Controls.Add(headLine);

        for (var i = 0; i < rows.Length; i++)
        {
            var y = headerH + i * rowH;
            rows[i].Mount(card, y, rowH);
            if (i < rows.Length - 1)
            {
                card.Controls.Add(new Panel
                {
                    BackColor = Line,
                    Location = new Point(16, y + rowH - 1),
                    Size = new Size(728, 1),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                });
            }
        }
        page.Controls.Add(card);
        return page;
    }

    private static Label ColHeader(string text, int x, int w, Color color) => new()
    {
        Text = text,
        Location = new Point(x, 8),
        Size = new Size(w, 22),
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        ForeColor = color,
    };

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
                ? "已按勾选项应用推荐设置；未勾选的项已恢复为系统默认。部分项目可能需要注销或重启后生效。"
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
            "Windows Server 日常使用优化工具。\r\n勾选采用推荐设置；右侧「系统默认值」仅供对照。未勾选的项在应用时恢复为系统默认。",
            "关于 Win一键优化",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static SettingRow Row(string recommend, string systemDefault) =>
        new(recommend, systemDefault);

    private static Button GhostButton(string text, int x, int y, int w, Action click)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Navy,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(196, 204, 214);
        b.Click += (_, _) => click();
        return b;
    }

    private sealed class SettingRow
    {
        private readonly CheckBox _check;
        private readonly Label _def;

        public SettingRow(string recommend, string systemDefault)
        {
            _check = new CheckBox
            {
                Text = recommend,
                AutoSize = false,
                Size = new Size(460, 22),
                ForeColor = TextMain,
                BackColor = Color.White,
            };
            _def = new Label
            {
                Text = systemDefault,
                AutoSize = false,
                Size = new Size(240, 22),
                ForeColor = TextMute,
                TextAlign = ContentAlignment.MiddleLeft,
            };
        }

        public bool Checked
        {
            get => _check.Checked;
            set => _check.Checked = value;
        }

        public void Mount(Control parent, int y, int h)
        {
            var wrap = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(parent.Width, h),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            var mid = (h - 22) / 2;
            _check.Location = new Point(16, mid);
            _def.Location = new Point(500, mid);
            wrap.Controls.Add(_check);
            wrap.Controls.Add(_def);
            parent.Controls.Add(wrap);
        }
    }
}
