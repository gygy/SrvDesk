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

    private static readonly Color MenuBack = Color.FromArgb(32, 46, 64);
    private static readonly Color MenuHot = Color.FromArgb(44, 64, 88);
    private static readonly Color MenuSel = Color.FromArgb(0, 140, 150);
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
        MinimumSize = new Size(820, 520);
        ClientSize = new Size(880, 540);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.White;

        var sidebar = BuildSidebar();
        var bottom = BuildBottom();
        BuildTabs();

        _tabs.Dock = DockStyle.Fill;
        sidebar.Dock = DockStyle.Left;
        bottom.Dock = DockStyle.Bottom;

        Controls.Add(_tabs);
        Controls.Add(sidebar);
        Controls.Add(bottom);

        _menu.SelectedIndex = 0;
        Load += (_, _) => LoadState();
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Width = 176,
            BackColor = MenuBack,
        };

        var title = new Label
        {
            Text = "  Win一键优化",
            Dock = DockStyle.Top,
            Height = 52,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _menu.Dock = DockStyle.Fill;
        _menu.BorderStyle = BorderStyle.None;
        _menu.BackColor = MenuBack;
        _menu.ForeColor = Color.White;
        _menu.IntegralHeight = false;
        _menu.DrawMode = DrawMode.OwnerDrawFixed;
        _menu.ItemHeight = 44;
        _menu.Items.AddRange(MenuItems);
        _menu.DrawItem += DrawMenuItem;
        _menu.SelectedIndexChanged += (_, _) => ShowSelectedPage();

        sidebar.Controls.Add(_menu);
        sidebar.Controls.Add(title);
        return sidebar;
    }

    private void BuildTabs()
    {
        _tabs.Padding = new Point(8, 4);
        _pages[0] = NewPage("性能及安全", "调整 CPU 调度、DEP、UAC 与 IE 增强安全。", _cpu, _dep, _uac, _ie);
        _pages[1] = NewPage("个性化设置", "桌面图标、任务栏、删除确认与音频服务。", _thisPc, _taskbar, _confirmDel, _audio);
        _pages[2] = NewPage("启动项", "登录后的服务器管理器与 Azure Arc 托盘。", _svrMgr, _azure);
        _pages[3] = NewPage("账户策略", "密码复杂性、关机与 Ctrl+Alt+Del 登录。", _pwd, _shutdownLogon, _shutdownReason, _noCad);
    }

    private Panel BuildBottom()
    {
        var bottom = new Panel
        {
            Height = 92,
            BackColor = Color.FromArgb(246, 248, 250),
            Padding = new Padding(12, 8, 12, 8),
        };

        _status.AutoSize = false;
        _status.SetBounds(12, 8, 856, 36);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        _status.Text = Optimizer.IsWindowsServer()
            ? "先勾选或取消配置项，再运行。勾选表示采用优化项，取消勾选表示恢复该项。"
            : "当前系统可能不是 Windows Server。本工具面向 Server 日常使用优化。";

        var selectAll = NewButton("全部选择", 12, 50, 96, () => SetAll(true));
        var selectNone = NewButton("全部取消", 116, 50, 96, () => SetAll(false));
        var about = NewButton("关于", 772, 50, 88, ShowAbout);
        about.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _apply.Text = "一键优化";
        _apply.SetBounds(620, 46, 140, 34);
        _apply.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _apply.Click += (_, _) => Apply();

        bottom.Controls.AddRange([_status, selectAll, selectNone, _apply, about]);
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
        using var back = new SolidBrush(selected ? MenuSel : MenuBack);
        e.Graphics.FillRectangle(back, e.Bounds);
        if (!selected && (e.State & DrawItemState.HotLight) != 0)
        {
            using var hot = new SolidBrush(MenuHot);
            e.Graphics.FillRectangle(hot, e.Bounds);
        }
        TextRenderer.DrawText(
            e.Graphics,
            MenuItems[e.Index],
            Font,
            new Rectangle(e.Bounds.X + 16, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height),
            Color.White,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    private static TabPage NewPage(string title, string hint, params CheckBox[] boxes)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White,
            UseVisualStyleBackColor = true,
        };
        var hintLabel = new Label
        {
            Text = hint,
            AutoSize = false,
            ForeColor = Color.FromArgb(96, 96, 96),
            Location = new Point(20, 16),
            Size = new Size(620, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        page.Controls.Add(hintLabel);
        for (var i = 0; i < boxes.Length; i++)
        {
            boxes[i].Location = new Point(24, 52 + i * 36);
            page.Controls.Add(boxes[i]);
        }
        return page;
    }

    private void LoadState()
    {
        try
        {
            Bind(Optimizer.Read());
        }
        catch (Exception ex)
        {
            _status.Text = "读取当前配置失败：" + ex.Message;
        }
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
            if (errors.Count == 0)
                _status.Text = "已应用。部分项目（UAC、IE 增强安全等）可能需要注销或重启后生效。";
            else
                _status.Text = "部分失败：\r\n" + string.Join("\r\n", errors);
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
            "关于",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static CheckBox NewCheck(string text) => new()
    {
        AutoSize = true,
        Text = text,
        UseVisualStyleBackColor = true,
    };

    private static Button NewButton(string text, int x, int y, int w, Action click)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, 28),
            UseVisualStyleBackColor = true,
        };
        b.Click += (_, _) => click();
        return b;
    }
}
