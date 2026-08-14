namespace WinOpt;

internal sealed class MainForm : Form
{
    private readonly CheckBox _cpu = NewCheck("CPU资源分配程序优先");
    private readonly CheckBox _dep = NewCheck("数据执行保护DEP（T）");
    private readonly CheckBox _uac = NewCheck("禁用用户账户控制UAC");
    private readonly CheckBox _ie = NewCheck("关闭IE增强安全配置");
    private readonly CheckBox _thisPc = NewCheck("桌面此电脑图标");
    private readonly CheckBox _taskbar = NewCheck("使用小按钮任务栏");
    private readonly CheckBox _confirmDel = NewCheck("显示删除确认对话框");
    private readonly CheckBox _audio = NewCheck("启动音频服务");
    private readonly CheckBox _svrMgr = NewCheck("登录不启动服务管理器");
    private readonly CheckBox _azure = NewCheck("禁止启动Azure Arc");
    private readonly CheckBox _pwd = NewCheck("禁用密码符合复杂性要求");
    private readonly CheckBox _shutdownLogon = NewCheck("允许未登录时关机");
    private readonly CheckBox _shutdownReason = NewCheck("关闭显示事件跟踪程序");
    private readonly CheckBox _noCad = NewCheck("无需Ctrl+Alt+Del登录");

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

    private CheckBox[] AllChecks =>
    [
        _cpu, _dep, _uac, _ie, _thisPc, _taskbar, _confirmDel, _audio,
        _svrMgr, _azure, _pwd, _shutdownLogon, _shutdownReason, _noCad
    ];

    public MainForm()
    {
        Text = "Win一键优化";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 580);
        ClientSize = new Size(960, 600);
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
        var sidebar = new Panel
        {
            Width = 196,
            BackColor = Navy,
        };
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
        _tabs.BackColor = Canvas;
        _pages[0] = NewPage("性能及安全", "调整处理器调度、内存防护、账户控制与 IE 增强安全。", _cpu, _dep, _uac, _ie);
        _pages[1] = NewPage("个性化设置", "桌面图标、任务栏样式、删除确认与音频服务。", _thisPc, _taskbar, _confirmDel, _audio);
        _pages[2] = NewPage("启动项", "控制登录后自动打开的管理控制台与 Azure Arc。", _svrMgr, _azure);
        _pages[3] = NewPage("账户策略", "密码复杂性、关机行为与安全登录提示。", _pwd, _shutdownLogon, _shutdownReason, _noCad);
    }

    private Panel BuildBottom()
    {
        var bottom = new Panel
        {
            Height = 78,
            BackColor = Color.White,
        };
        var rule = new Panel
        {
            Height = 1,
            Dock = DockStyle.Top,
            BackColor = Line,
        };

        _status.AutoSize = false;
        _status.SetBounds(20, 14, 480, 50);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _status.ForeColor = TextMute;
        _status.Text = Optimizer.IsWindowsServer()
            ? "勾选表示采用优化项，取消勾选表示恢复该项。部分设置需注销或重启后生效。"
            : "当前系统可能不是 Windows Server。本工具面向 Server 日常使用优化。";

        var selectAll = GhostButton("全部选择", 540, 22, 92, () => SetAll(true));
        var selectNone = GhostButton("全部取消", 640, 22, 92, () => SetAll(false));
        var about = GhostButton("关于", 740, 22, 72, ShowAbout);
        selectAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        selectNone.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        about.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _apply.Text = "一键优化";
        _apply.SetBounds(824, 18, 116, 40);
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

        bottom.Controls.AddRange([rule, _status, selectAll, selectNone, about, _apply]);
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

    private static TabPage NewPage(string title, string hint, params CheckBox[] boxes)
    {
        var page = new TabPage(title)
        {
            BackColor = Canvas,
            UseVisualStyleBackColor = false,
            Padding = new Padding(20, 16, 20, 16),
        };
        var hintLabel = new Label
        {
            Text = hint,
            AutoSize = false,
            ForeColor = TextMute,
            Location = new Point(20, 14),
            Size = new Size(700, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        page.Controls.Add(hintLabel);

        var card = new Panel
        {
            Location = new Point(20, 44),
            Size = new Size(700, 16 + boxes.Length * 48),
            BackColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Line);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        for (var i = 0; i < boxes.Length; i++)
        {
            boxes[i].Location = new Point(20, 14 + i * 48);
            boxes[i].ForeColor = TextMain;
            boxes[i].Font = new Font("Microsoft YaHei UI", 10F);
            card.Controls.Add(boxes[i]);
            if (i < boxes.Length - 1)
            {
                var divider = new Panel
                {
                    BackColor = Line,
                    Location = new Point(16, 56 + i * 48),
                    Size = new Size(668, 1),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                };
                card.Controls.Add(divider);
            }
        }
        page.Controls.Add(card);
        return page;
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

    private void SetAll(bool value)
    {
        foreach (var box in AllChecks) box.Checked = value;
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
                ? "已应用。部分项目（UAC、IE 增强安全等）可能需要注销或重启后生效。"
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
            "Windows Server 日常使用优化工具。",
            "关于 Win一键优化",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static CheckBox NewCheck(string text) => new()
    {
        AutoSize = true,
        Text = text,
        BackColor = Color.White,
        UseVisualStyleBackColor = false,
    };

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
}
