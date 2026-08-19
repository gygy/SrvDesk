namespace WinOpt;

internal sealed class MainForm : Form
{
    private readonly SettingRow _cpu = Row("CPU 资源分配（程序优先）", "后台服务优先");
    private readonly SettingRow _dep = Row("数据执行保护 DEP（T）", "按系统策略");
    private readonly SettingRow _uac = Row("禁用用户账户控制 UAC", "启用");
    private readonly SettingRow _ie = Row("关闭 IE 增强安全配置", "开启");
    private readonly SettingRow _highPerf = Row("高性能电源计划", "平衡");
    private readonly SettingRow _telemetry = Row("关闭遥测与 DiagTrack", "开启");
    private readonly SettingRow _noUpdateReboot = Row("更新时不自动重启", "允许重启");
    private readonly SettingRow _deliveryOpt = Row("关闭更新传递优化（P2P）", "开启");
    private readonly SettingRow _wuNotify = Row("Windows 更新仅通知下载", "自动安装");
    private readonly SettingRow _sysMain = Row("禁用 SysMain 超级预读", "自动");
    private readonly SettingRow _visualPerf = Row("视觉效果调整为最佳性能", "系统自选");
    private readonly SettingRow _powerThrottle = Row("关闭 CPU 电源节流", "开启");
    private readonly SettingRow _hibernate = Row("关闭休眠释放磁盘空间", "开启");
    private readonly SettingRow _tcp = Row("TCP 参数优化（对齐 Win10）", "默认");
    private readonly SettingRow _errorReport = Row("关闭 Windows 错误报告", "开启");
    private readonly SettingRow _longPaths = Row("启用 NTFS 长路径支持", "关闭");
    private readonly SettingRow _fastStartup = Row("关闭快速启动（稳定双系统）", "开启");
    private readonly SettingRow _autoMaint = Row("禁用自动维护计划", "开启");
    private readonly SettingRow _noDriverWu = Row("Windows 更新不含驱动", "含驱动");
    private readonly SettingRow _smb1 = Row("禁用 SMB 1.0 协议", "允许");
    private readonly SettingRow _remoteReg = Row("禁用 Remote Registry 服务", "手动");
    private readonly SettingRow _spooler = Row("禁用打印后台处理（无打印机）", "自动");

    private readonly SettingRow _thisPc = Row("显示桌面「此电脑」图标", "不显示");
    private readonly SettingRow _launchThisPc = Row("资源管理器打开到「此电脑」", "快速访问");
    private readonly SettingRow _taskbar = Row("使用小按钮任务栏", "标准大小");
    private readonly SettingRow _confirmDel = Row("显示删除确认对话框", "不提示");
    private readonly SettingRow _audio = Row("启动音频服务", "不启动");
    private readonly SettingRow _fileExt = Row("显示已知文件扩展名", "隐藏");
    private readonly SettingRow _themes = Row("启用主题服务（完整桌面外观）", "手动");
    private readonly SettingRow _search = Row("启用 Windows 搜索", "手动");
    private readonly SettingRow _webSearch = Row("关闭开始菜单 Bing 网络搜索", "开启");
    private readonly SettingRow _feedback = Row("关闭 Windows 体验反馈提示", "开启");
    private readonly SettingRow _noLockScreen = Row("禁用锁屏界面", "显示");
    private readonly SettingRow _hiddenFiles = Row("显示隐藏文件", "不显示");
    private readonly SettingRow _noArrow = Row("隐藏快捷方式小箭头", "显示");
    private readonly SettingRow _fullPath = Row("标题栏显示完整路径", "仅文件夹名");
    private readonly SettingRow _allTrayIcons = Row("任务栏显示全部图标", "自动隐藏");

    private readonly SettingRow _animations = Row("禁用窗口与任务栏动画", "开启");
    private readonly SettingRow _transparency = Row("禁用透明效果", "开启");
    private readonly SettingRow _tips = Row("关闭 Windows 提示与建议", "开启");
    private readonly SettingRow _autoplay = Row("禁用所有驱动器自动播放", "开启");
    private readonly SettingRow _activityHist = Row("禁用活动历史记录", "开启");
    private readonly SettingRow _storageSense = Row("禁用存储感知", "开启");

    private readonly SettingRow _rdp = Row("启用远程桌面（RDP）", "禁用");
    private readonly SettingRow _rdpGpu = Row("RDP 硬件图形加速", "关闭");
    private readonly SettingRow _rdpFps = Row("RDP 提高远程帧率", "默认");
    private readonly SettingRow _rdpNla = Row("RDP 关闭 NLA（内网/Linux 客户端）", "开启");
    private readonly SettingRow _netDiscovery = Row("启用网络发现与文件共享", "关闭");
    private readonly SettingRow _smRemoting = Row("关闭 Server Manager 远程管理", "开启");

    private readonly SettingRow _svrMgr = Row("登录不启动服务管理器", "自动打开");
    private readonly SettingRow _azure = Row("禁止启动 Azure Arc 托盘", "允许启动");
    private readonly SettingRow _installer = Row("Windows Installer 自动启动", "手动");
    private readonly SettingRow _wia = Row("启用 WIA（摄像头/扫描仪）", "手动");

    private readonly SettingRow _pwd = Row("禁用密码复杂性要求", "必须符合");
    private readonly SettingRow _pwdExpire = Row("密码永不过期", "42 天");
    private readonly SettingRow _shutdownLogon = Row("允许未登录时关机", "不允许");
    private readonly SettingRow _shutdownReason = Row("关闭关机事件跟踪", "显示");
    private readonly SettingRow _noCad = Row("无需 Ctrl+Alt+Del 登录", "需要按键");

    private readonly Panel _contentHost = new();
    private readonly Label _status = new();
    private readonly Button _apply = new();
    private readonly Button _restore = new();
    private readonly List<(string Title, SettingRow[] Rows)> _groups = [];
    private readonly ListBox _menu = new();
    private int _menuHover = -1;

    private static readonly string[] MenuItems =
    [
        "性能及安全",
        "个性化设置",
        "隐私与体验",
        "远程与网络",
        "启动项",
        "账户策略",
    ];

    private SettingRow[] AllRows =>
    [
        _cpu, _dep, _uac, _ie, _highPerf, _telemetry, _noUpdateReboot, _deliveryOpt, _wuNotify,
        _sysMain, _visualPerf, _powerThrottle, _hibernate, _tcp, _errorReport,
        _longPaths, _fastStartup, _autoMaint, _noDriverWu, _smb1, _remoteReg, _spooler,
        _thisPc, _launchThisPc, _taskbar, _confirmDel, _audio, _fileExt, _themes, _search,
        _webSearch, _feedback, _noLockScreen, _hiddenFiles, _noArrow, _fullPath, _allTrayIcons,
        _animations, _transparency, _tips, _autoplay, _activityHist, _storageSense,
        _rdp, _rdpGpu, _rdpFps, _rdpNla, _netDiscovery, _smRemoting,
        _svrMgr, _azure, _installer, _wia,
        _pwd, _pwdExpire, _shutdownLogon, _shutdownReason, _noCad
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
        Icon = AppBrand.ApplicationIcon;

        _groups.Add(("性能及安全", [
            _cpu, _dep, _uac, _ie, _highPerf, _telemetry, _noUpdateReboot, _deliveryOpt, _wuNotify,
            _sysMain, _visualPerf, _powerThrottle, _hibernate, _tcp, _errorReport,
            _longPaths, _fastStartup, _autoMaint, _noDriverWu, _smb1, _remoteReg, _spooler
        ]));
        _groups.Add(("个性化设置", [
            _thisPc, _launchThisPc, _taskbar, _confirmDel, _audio, _fileExt, _themes, _search,
            _webSearch, _feedback, _noLockScreen, _hiddenFiles, _noArrow, _fullPath, _allTrayIcons
        ]));
        _groups.Add(("隐私与体验", [
            _animations, _transparency, _tips, _autoplay, _activityHist, _storageSense
        ]));
        _groups.Add(("远程与网络", [_rdp, _rdpGpu, _rdpFps, _rdpNla, _netDiscovery, _smRemoting]));
        _groups.Add(("启动项", [_svrMgr, _azure, _installer, _wia]));
        _groups.Add(("账户策略", [_pwd, _pwdExpire, _shutdownLogon, _shutdownReason, _noCad]));

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
            Text = "Windows Server 2022 / 2025 个人优化",
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

    private static Image? LoadLogo() => AppBrand.LoadLogoImage();

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 184, BackColor = AppTheme.NavBg };
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
        header.Controls.Add(MakeHeaderLabel("推荐设置", 448, 160, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderLabel("系统默认", 628, 160, ContentAlignment.MiddleCenter));
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
            Size = new Size(section.Width - 120, headerH),
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

        var restoreGroup = new LinkLabel
        {
            Text = "恢复本组默认",
            AutoSize = true,
            Location = new Point(section.Width - 108, 8),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            LinkColor = AppTheme.PrimaryDark,
            ActiveLinkColor = AppTheme.Primary,
            VisitedLinkColor = AppTheme.PrimaryDark,
            BackColor = Color.Transparent,
        };
        restoreGroup.Click += (_, _) => RestoreGroup(title, rows);
        head.Controls.Add(restoreGroup);

        section.Controls.Add(body);
        section.Controls.Add(head);
        section.Resize += (_, _) =>
        {
            head.Width = section.Width;
            body.Width = section.Width;
            titleLabel.Width = section.Width - 120;
            restoreGroup.Location = new Point(section.Width - 108, 8);
        };
        return section;
    }

    private Panel BuildBottom()
    {
        var bottom = new Panel { Height = 58, BackColor = AppTheme.SurfaceCard };
        var rule = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = AppTheme.BorderLight };

        _status.AutoSize = false;
        _status.SetBounds(188, 10, 380, 38);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
        _status.ForeColor = AppTheme.TextMute;
        _status.Text = Optimizer.IsWindowsServer()
            ? "开关=是否采用推荐。「应用推荐」写入推荐项；「恢复默认」将全部项还原为右侧系统默认值。"
            : "当前系统可能不是 Windows Server。";

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 10, 14, 0),
            BackColor = AppTheme.SurfaceCard,
        };

        var allOn = ToolButton("全部推荐", () => SetAll(true));
        var allOff = ToolButton("关闭全部推荐", () => SetAll(false));
        var refresh = ToolButton("刷新", LoadState);
        var about = ToolButton("关于", ShowAbout);

        _restore.Text = "恢复默认";
        _restore.AutoSize = false;
        _restore.Size = new Size(92, 36);
        _restore.Margin = new Padding(8, 0, 0, 0);
        _restore.FlatStyle = FlatStyle.Flat;
        _restore.BackColor = AppTheme.SurfaceCard;
        _restore.ForeColor = AppTheme.PrimaryDeep;
        _restore.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _restore.Cursor = Cursors.Hand;
        _restore.FlatAppearance.BorderColor = AppTheme.Border;
        _restore.Click += (_, _) => RestoreDefaults();
        _restore.MouseEnter += (_, _) => _restore.BackColor = AppTheme.PrimaryPale;
        _restore.MouseLeave += (_, _) => _restore.BackColor = AppTheme.SurfaceCard;

        _apply.Text = "应用推荐";
        _apply.AutoSize = false;
        _apply.Size = new Size(92, 36);
        _apply.Margin = new Padding(8, 0, 0, 0);
        _apply.FlatStyle = FlatStyle.Flat;
        _apply.FlatAppearance.BorderSize = 0;
        _apply.BackColor = AppTheme.Primary;
        _apply.ForeColor = AppTheme.TextOnPrimary;
        _apply.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _apply.Cursor = Cursors.Hand;
        _apply.Click += (_, _) => ApplyRecommended();
        _apply.MouseEnter += (_, _) => _apply.BackColor = AppTheme.PrimaryDark;
        _apply.MouseLeave += (_, _) => _apply.BackColor = AppTheme.Primary;

        actions.Controls.AddRange([allOn, allOff, refresh, about, _restore, _apply]);
        bottom.Controls.Add(actions);
        bottom.Controls.Add(_status);
        bottom.Controls.Add(rule);
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
        _highPerf.Checked = s.HighPerfPower;
        _telemetry.Checked = s.DisableTelemetry;
        _noUpdateReboot.Checked = s.NoUpdateReboot;
        _deliveryOpt.Checked = s.DisableDeliveryOpt;
        _wuNotify.Checked = s.WuNotifyOnly;
        _sysMain.Checked = s.DisableSysMain;
        _visualPerf.Checked = s.VisualBestPerf;
        _powerThrottle.Checked = s.PowerThrottlingOff;
        _hibernate.Checked = s.DisableHibernate;
        _tcp.Checked = s.TcpOptimized;
        _errorReport.Checked = s.DisableErrorReport;
        _longPaths.Checked = s.LongPathsEnabled;
        _fastStartup.Checked = s.DisableFastStartup;
        _autoMaint.Checked = s.DisableAutoMaintenance;
        _noDriverWu.Checked = s.ExcludeDriverUpdates;
        _smb1.Checked = s.DisableSmb1;
        _remoteReg.Checked = s.DisableRemoteRegistry;
        _spooler.Checked = s.DisablePrintSpooler;
        _thisPc.Checked = s.ShowThisPcIcon;
        _launchThisPc.Checked = s.LaunchExplorerThisPc;
        _taskbar.Checked = s.SmallTaskbar;
        _confirmDel.Checked = s.ConfirmDelete;
        _audio.Checked = s.EnableAudio;
        _fileExt.Checked = s.ShowFileExtensions;
        _themes.Checked = s.EnableThemes;
        _search.Checked = s.EnableSearch;
        _webSearch.Checked = s.DisableWebSearch;
        _feedback.Checked = s.DisableFeedback;
        _noLockScreen.Checked = s.NoLockScreen;
        _hiddenFiles.Checked = s.ShowHiddenFiles;
        _noArrow.Checked = s.NoShortcutArrow;
        _fullPath.Checked = s.ExplorerFullPath;
        _allTrayIcons.Checked = s.TaskbarAllIcons;
        _animations.Checked = s.DisableAnimations;
        _transparency.Checked = s.DisableTransparency;
        _tips.Checked = s.DisableTips;
        _autoplay.Checked = s.DisableAutoplay;
        _activityHist.Checked = s.DisableActivityHistory;
        _storageSense.Checked = s.DisableStorageSense;
        _rdp.Checked = s.EnableRdp;
        _rdpGpu.Checked = s.RdpGpuAccel;
        _rdpFps.Checked = s.RdpHighRefresh;
        _rdpNla.Checked = s.RdpDisableNla;
        _netDiscovery.Checked = s.EnableNetworkDiscovery;
        _smRemoting.Checked = s.DisableSmRemoting;
        _svrMgr.Checked = s.SkipServerManager;
        _azure.Checked = s.DisableAzureArc;
        _installer.Checked = s.EnableInstaller;
        _wia.Checked = s.EnableWia;
        _pwd.Checked = s.DisablePasswordComplexity;
        _pwdExpire.Checked = s.PasswordNeverExpire;
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
        HighPerfPower = _highPerf.Checked,
        DisableTelemetry = _telemetry.Checked,
        NoUpdateReboot = _noUpdateReboot.Checked,
        DisableDeliveryOpt = _deliveryOpt.Checked,
        WuNotifyOnly = _wuNotify.Checked,
        DisableSysMain = _sysMain.Checked,
        VisualBestPerf = _visualPerf.Checked,
        PowerThrottlingOff = _powerThrottle.Checked,
        DisableHibernate = _hibernate.Checked,
        TcpOptimized = _tcp.Checked,
        DisableErrorReport = _errorReport.Checked,
        LongPathsEnabled = _longPaths.Checked,
        DisableFastStartup = _fastStartup.Checked,
        DisableAutoMaintenance = _autoMaint.Checked,
        ExcludeDriverUpdates = _noDriverWu.Checked,
        DisableSmb1 = _smb1.Checked,
        DisableRemoteRegistry = _remoteReg.Checked,
        DisablePrintSpooler = _spooler.Checked,
        ShowThisPcIcon = _thisPc.Checked,
        LaunchExplorerThisPc = _launchThisPc.Checked,
        SmallTaskbar = _taskbar.Checked,
        ConfirmDelete = _confirmDel.Checked,
        EnableAudio = _audio.Checked,
        ShowFileExtensions = _fileExt.Checked,
        EnableThemes = _themes.Checked,
        EnableSearch = _search.Checked,
        DisableWebSearch = _webSearch.Checked,
        DisableFeedback = _feedback.Checked,
        NoLockScreen = _noLockScreen.Checked,
        ShowHiddenFiles = _hiddenFiles.Checked,
        NoShortcutArrow = _noArrow.Checked,
        ExplorerFullPath = _fullPath.Checked,
        TaskbarAllIcons = _allTrayIcons.Checked,
        DisableAnimations = _animations.Checked,
        DisableTransparency = _transparency.Checked,
        DisableTips = _tips.Checked,
        DisableAutoplay = _autoplay.Checked,
        DisableActivityHistory = _activityHist.Checked,
        DisableStorageSense = _storageSense.Checked,
        EnableRdp = _rdp.Checked,
        RdpGpuAccel = _rdpGpu.Checked,
        RdpHighRefresh = _rdpFps.Checked,
        RdpDisableNla = _rdpNla.Checked,
        EnableNetworkDiscovery = _netDiscovery.Checked,
        DisableSmRemoting = _smRemoting.Checked,
        SkipServerManager = _svrMgr.Checked,
        DisableAzureArc = _azure.Checked,
        EnableInstaller = _installer.Checked,
        EnableWia = _wia.Checked,
        DisablePasswordComplexity = _pwd.Checked,
        PasswordNeverExpire = _pwdExpire.Checked,
        ShutdownWithoutLogon = _shutdownLogon.Checked,
        DisableShutdownReason = _shutdownReason.Checked,
        DisableCad = _noCad.Checked,
    };

    private void SetAll(bool on)
    {
        foreach (var row in AllRows) row.Checked = on;
    }

    private void SetRowsChecked(IEnumerable<SettingRow> rows, bool on)
    {
        foreach (var row in rows) row.Checked = on;
    }

    private void RestoreDefaults()
    {
        var answer = MessageBox.Show(
            "将把全部设置项恢复为 Windows Server 出厂默认值（右侧「系统默认」列）。\n\n" +
            "所有推荐开关将关闭并立即写入系统。部分项目需注销或重启后生效。\n\n是否继续？",
            "恢复出厂默认",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        SetAll(false);
        RunApply("正在恢复出厂默认…", "已恢复为系统出厂默认。开关已全部关闭并与系统状态同步。");
    }

    private void RestoreGroup(string title, SettingRow[] rows)
    {
        var answer = MessageBox.Show(
            $"将「{title}」分组内的 {rows.Length} 项恢复为出厂默认。\n\n是否立即写入系统？",
            "恢复本组默认",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        SetRowsChecked(rows, false);
        RunApply($"正在恢复「{title}」…", $"「{title}」已恢复为出厂默认。");
    }

    private void ApplyRecommended() =>
        RunApply("正在应用推荐设置…", "已应用。开启项为推荐设置，关闭项保持系统默认。");

    private void RunApply(string working, string success)
    {
        _apply.Enabled = false;
        _restore.Enabled = false;
        UseWaitCursor = true;
        _status.Text = working;
        Application.DoEvents();
        try
        {
            var errors = Optimizer.Apply(CaptureState());
            LoadState();
            _status.Text = errors.Count == 0
                ? success + " 部分项目需注销或重启后生效。"
                : "部分失败：\r\n" + string.Join("\r\n", errors);
        }
        catch (Exception ex)
        {
            _status.Text = "操作失败：" + ex.Message;
        }
        finally
        {
            UseWaitCursor = false;
            _apply.Enabled = true;
            _restore.Enabled = true;
        }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "Windows Server 2022 / 2025 个人日常使用优化。\r\n" +
            "「应用推荐」写入已开启项；「恢复默认」一键还原全部出厂设置。",
            "关于 Win一键优化",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static SettingRow Row(string item, string systemDefault) =>
        new(item, systemDefault);

    private static Button ToolButton(string text, Action click)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = false,
            Size = new Size(text.Length > 4 ? 96 : 72, 36),
            Margin = new Padding(8, 0, 0, 0),
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
            _item.SetBounds(16, 0, 460, h);
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
