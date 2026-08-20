using System.Diagnostics;

namespace WinOpt;

internal sealed class MainForm : Form
{
    private readonly SettingRow _cpu = Row("CPU 资源分配（程序优先）", "后台服务优先", SettingCatalog.CpuProgramPriority);
    private readonly SettingRow _dep = Row("数据执行保护 DEP（T）", "按系统策略", SettingCatalog.Dep);
    private readonly SettingRow _uac = Row("禁用用户账户控制 UAC", "启用", SettingCatalog.DisableUac);
    private readonly SettingRow _ie = Row("关闭 IE 增强安全配置", "开启", SettingCatalog.DisableIeEsc);
    private readonly SettingRow _highPerf = Row("高性能电源计划", "平衡", SettingCatalog.HighPerfPower);
    private readonly SettingRow _telemetry = Row("关闭遥测与 DiagTrack", "开启", SettingCatalog.DisableTelemetry);
    private readonly SettingRow _noUpdateReboot = Row("更新时不自动重启", "允许重启", SettingCatalog.NoUpdateReboot);
    private readonly SettingRow _deliveryOpt = Row("关闭更新传递优化（P2P）", "开启", SettingCatalog.DisableDeliveryOpt);
    private readonly SettingRow _wuNotify = Row("Windows 更新仅通知下载", "自动安装", SettingCatalog.WuNotifyOnly);
    private readonly SettingRow _sysMain = Row("禁用 SysMain 超级预读", "自动", SettingCatalog.DisableSysMain);
    private readonly SettingRow _visualPerf = Row("视觉效果调整为最佳性能", "系统自选", SettingCatalog.VisualBestPerf);
    private readonly SettingRow _powerThrottle = Row("关闭 CPU 电源节流", "开启", SettingCatalog.PowerThrottlingOff);
    private readonly SettingRow _hibernate = Row("关闭休眠释放磁盘空间", "开启", SettingCatalog.DisableHibernate);
    private readonly SettingRow _tcp = Row("TCP 参数优化（对齐 Win10）", "默认", SettingCatalog.TcpOptimized);
    private readonly SettingRow _qosSpeed = Row("QoS 网速优化（零保留+入站TCP级别3）", "系统默认", SettingCatalog.QosSpeedOptimize);
    private readonly SettingRow _errorReport = Row("关闭 Windows 错误报告", "开启", SettingCatalog.DisableErrorReport);
    private readonly SettingRow _longPaths = Row("启用 NTFS 长路径支持", "关闭", SettingCatalog.LongPathsEnabled);
    private readonly SettingRow _fastStartup = Row("关闭快速启动（稳定双系统）", "开启", SettingCatalog.DisableFastStartup);
    private readonly SettingRow _autoMaint = Row("禁用自动维护计划", "开启", SettingCatalog.DisableAutoMaintenance);
    private readonly SettingRow _noDriverWu = Row("Windows 更新不含驱动", "含驱动", SettingCatalog.ExcludeDriverUpdates);
    private readonly SettingRow _smb1 = Row("禁用 SMB 1.0 协议", "允许", SettingCatalog.DisableSmb1);
    private readonly SettingRow _remoteReg = Row("禁用 Remote Registry 服务", "手动", SettingCatalog.DisableRemoteRegistry);
    private readonly SettingRow _spooler = Row("禁用打印后台处理（无打印机）", "自动", SettingCatalog.DisablePrintSpooler);
    private readonly SettingRow _largeCache = Row("大系统缓存与 NTFS 缓冲优化", "默认", SettingCatalog.LargeSystemCacheOptimize);
    private readonly SettingRow _reservedStorage = Row("关闭系统保留存储", "开启", SettingCatalog.DisableReservedStorage);
    private readonly SettingRow _srvSplit = Row("关闭 LanmanServer 服务拆分", "默认", SettingCatalog.DisableSrvSplit);
    private readonly SettingRow _gpuSched = Row("启用 GPU 硬件加速计划", "关闭", SettingCatalog.EnableGpuHwScheduling);
    private readonly SettingRow _defender = Row("关闭 Windows Defender", "开启", SettingCatalog.DisableDefenderAntivirus);

    private readonly SettingRow _thisPc = Row("显示桌面「此电脑」图标", "不显示", SettingCatalog.ShowThisPcIcon);
    private readonly SettingRow _launchThisPc = Row("资源管理器打开到「此电脑」", "快速访问", SettingCatalog.LaunchExplorerThisPc);
    private readonly SettingRow _taskbar = Row("使用小按钮任务栏", "标准大小", SettingCatalog.SmallTaskbar);
    private readonly SettingRow _confirmDel = Row("显示删除确认对话框", "不提示", SettingCatalog.ConfirmDelete);
    private readonly SettingRow _audio = Row("启动音频服务", "不启动", SettingCatalog.EnableAudio);
    private readonly SettingRow _fileExt = Row("显示已知文件扩展名", "隐藏", SettingCatalog.ShowFileExtensions);
    private readonly SettingRow _themes = Row("启用主题服务（完整桌面外观）", "手动", SettingCatalog.EnableThemes);
    private readonly SettingRow _search = Row("启用 Windows 搜索", "手动", SettingCatalog.EnableSearch);
    private readonly SettingRow _webSearch = Row("关闭开始菜单 Bing 网络搜索", "开启", SettingCatalog.DisableWebSearch);
    private readonly SettingRow _feedback = Row("关闭 Windows 体验反馈提示", "开启", SettingCatalog.DisableFeedback);
    private readonly SettingRow _noLockScreen = Row("禁用锁屏界面", "显示", SettingCatalog.NoLockScreen);
    private readonly SettingRow _hiddenFiles = Row("显示隐藏文件", "不显示", SettingCatalog.ShowHiddenFiles);
    private readonly SettingRow _noArrow = Row("隐藏快捷方式小箭头", "显示", SettingCatalog.NoShortcutArrow);
    private readonly SettingRow _fullPath = Row("标题栏显示完整路径", "仅文件夹名", SettingCatalog.ExplorerFullPath);
    private readonly SettingRow _allTrayIcons = Row("任务栏显示全部图标", "自动隐藏", SettingCatalog.TaskbarAllIcons);
    private readonly SettingRow _taskbarClock = Row("任务栏时钟显示星期与秒", "无星期/无秒", SettingCatalog.TaskbarClockWeekdaySeconds);
    private readonly SettingRow _desktopIcons = Row("显示控制面板与回收站图标", "不显示", SettingCatalog.ShowControlPanelRecycleBin);
    private readonly SettingRow _smartScreen = Row("关闭 SmartScreen 与打开文件警告", "开启", SettingCatalog.DisableSmartScreenWarning);
    private readonly SettingRow _classicSearch = Row("搜索退回传统模式", "现代搜索", SettingCatalog.ClassicFileSearch);
    private readonly SettingRow _searchEngine = Row("禁用 SearchEngine 功能包", "已安装", SettingCatalog.DisableSearchEngineFeature);

    private readonly SettingRow _animations = Row("禁用窗口与任务栏动画", "开启", SettingCatalog.DisableAnimations);
    private readonly SettingRow _transparency = Row("禁用透明效果", "开启", SettingCatalog.DisableTransparency);
    private readonly SettingRow _tips = Row("关闭 Windows 提示与建议", "开启", SettingCatalog.DisableTips);
    private readonly SettingRow _autoplay = Row("禁用所有驱动器自动播放", "开启", SettingCatalog.DisableAutoplay);
    private readonly SettingRow _activityHist = Row("禁用活动历史记录", "开启", SettingCatalog.DisableActivityHistory);
    private readonly SettingRow _storageSense = Row("禁用存储感知", "开启", SettingCatalog.DisableStorageSense);
    private readonly SettingRow _backgroundApps = Row("禁止应用在后台运行", "允许", SettingCatalog.DisableBackgroundApps);

    private readonly SettingRow _rdp = Row("启用远程桌面（RDP）", "禁用", SettingCatalog.EnableRdp);
    private readonly SettingRow _rdpGpu = Row("RDP 硬件图形加速", "关闭", SettingCatalog.RdpGpuAccel);
    private readonly SettingRow _rdpFps = Row("RDP 提高远程帧率", "默认", SettingCatalog.RdpHighRefresh);
    private readonly SettingRow _rdpNla = Row("RDP 关闭 NLA（内网/Linux 客户端）", "开启", SettingCatalog.RdpDisableNla);
    private readonly SettingRow _netDiscovery = Row("启用网络发现与文件共享", "关闭", SettingCatalog.EnableNetworkDiscovery);
    private readonly SettingRow _smRemoting = Row("关闭 Server Manager 远程管理", "开启", SettingCatalog.DisableSmRemoting);

    private readonly SettingRow _svrMgr = Row("登录不启动服务管理器", "自动打开", SettingCatalog.SkipServerManager);
    private readonly SettingRow _azure = Row("禁止启动 Azure Arc 托盘", "允许启动", SettingCatalog.DisableAzureArc);
    private readonly SettingRow _installer = Row("Windows Installer 自动启动", "手动", SettingCatalog.EnableInstaller);
    private readonly SettingRow _wia = Row("启用 WIA（摄像头/扫描仪）", "手动", SettingCatalog.EnableWia);
    private readonly SettingRow _mediaFeatures = Row("开启桌面媒体组件（DISM）", "未安装", SettingCatalog.EnableDesktopMediaFeatures);
    private readonly SettingRow _bloatFeatures = Row("关闭 Server 冗余组件（DISM）", "已安装", SettingCatalog.DisableServerBloatFeatures);

    private readonly SettingRow _pwd = Row("禁用密码复杂性要求", "必须符合", SettingCatalog.DisablePasswordComplexity);
    private readonly SettingRow _pwdExpire = Row("密码永不过期", "42 天", SettingCatalog.PasswordNeverExpire);
    private readonly SettingRow _shutdownLogon = Row("允许未登录时关机", "不允许", SettingCatalog.ShutdownWithoutLogon);
    private readonly SettingRow _shutdownReason = Row("关闭关机事件跟踪", "显示", SettingCatalog.DisableShutdownReason);
    private readonly SettingRow _noCad = Row("无需 Ctrl+Alt+Del 登录", "需要按键", SettingCatalog.DisableCad);
    private readonly SettingRow _autologon = Row("启用 Windows 自动登录（Autologon）", "未启用", SettingCatalog.EnableAutologon);
    private readonly SettingRow _keyboardFilter = Row("取消登录粘滞键/筛选键提示", "显示", SettingCatalog.DisableLoginKeyboardFilters);
    private AutologonSettings? _autologonSettings;

    private readonly HelpDetailPanel _helpDetail = new();
    private readonly AppMenuStrip _appMenu = new(OptPresets.All);
    private readonly Panel _commandBar = new();
    private readonly Panel _workArea = new();
    private readonly Label _headerSubtitle = new();
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 200 };
    private readonly Panel _contentHost = new();
    private readonly Label _status = new();
    private readonly Button _apply = new();
    private readonly Button _restore = new();
    private readonly List<(string Title, SettingRow[] Rows)> _groups = [];
    private readonly ListBox _menu = new();
    private int _menuHover = -1;
    private SettingRow? _selectedRow;
    private readonly SystemFacts _systemFacts = SystemInfoHelper.Detect();
    private readonly TextBox _searchBox = new();
    private readonly CheckBox _hideIncompatible = new();
    private readonly ComboBox _presetCombo = new();
    private SettingRow[] _activeRows = [];
    private Panel? _activeBody;
    private Panel? _activeSection;
    private int _activeGroupIndex;

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
        _largeCache, _reservedStorage, _srvSplit, _gpuSched, _defender,
        _thisPc, _launchThisPc, _taskbar, _confirmDel, _audio, _fileExt, _themes, _search,
        _webSearch, _feedback, _noLockScreen, _hiddenFiles, _noArrow, _fullPath, _allTrayIcons,
        _taskbarClock, _desktopIcons, _smartScreen, _classicSearch, _searchEngine,
        _animations, _transparency, _tips, _autoplay, _activityHist, _storageSense, _backgroundApps,
        _rdp, _rdpGpu, _rdpFps, _rdpNla, _netDiscovery, _smRemoting,
        _svrMgr, _azure, _installer, _wia, _mediaFeatures, _bloatFeatures,
        _pwd, _pwdExpire, _shutdownLogon, _shutdownReason, _noCad, _autologon, _keyboardFilter
    ];

    public MainForm()
    {
        Text = "Win一键优化";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 720);
        ClientSize = new Size(1080, 760);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextMain;
        AppBrand.ApplyWindowIcon(this);
        KeyPreview = true;
        MainMenuStrip = _appMenu;

        _groups.Add(("性能及安全", [
            _cpu, _dep, _uac, _ie, _highPerf, _telemetry, _noUpdateReboot, _deliveryOpt, _wuNotify,
            _sysMain, _visualPerf, _powerThrottle, _hibernate, _tcp, _qosSpeed, _errorReport,
            _longPaths, _fastStartup, _autoMaint, _noDriverWu, _smb1, _remoteReg, _spooler,
            _largeCache, _reservedStorage, _srvSplit, _gpuSched, _defender
        ]));
        _groups.Add(("个性化设置", [
            _thisPc, _launchThisPc, _taskbar, _confirmDel, _audio, _fileExt, _themes, _search,
            _webSearch, _feedback, _noLockScreen, _hiddenFiles, _noArrow, _fullPath, _allTrayIcons, _taskbarClock,
            _desktopIcons, _smartScreen, _classicSearch, _searchEngine
        ]));
        _groups.Add(("隐私与体验", [
            _animations, _transparency, _tips, _autoplay, _activityHist, _storageSense, _backgroundApps
        ]));
        _groups.Add(("远程与网络", [_rdp, _rdpGpu, _rdpFps, _rdpNla, _netDiscovery, _smRemoting]));
        _groups.Add(("启动项", [_svrMgr, _azure, _installer, _wia, _mediaFeatures, _bloatFeatures]));
        _groups.Add(("账户策略", [_pwd, _pwdExpire, _shutdownLogon, _shutdownReason, _noCad, _autologon, _keyboardFilter]));

        WireAppMenu();
        var header = BuildHeader();
        var sidebar = BuildSidebar();
        var bottom = BuildBottom();
        BuildCommandBar();
        BuildContent();
        BuildWorkArea(sidebar);

        _appMenu.Dock = DockStyle.Top;
        header.Dock = DockStyle.Top;
        _commandBar.Dock = DockStyle.Top;
        _workArea.Dock = DockStyle.Fill;
        bottom.Dock = DockStyle.Bottom;

        Controls.Add(_workArea);
        Controls.Add(bottom);
        Controls.Add(_commandBar);
        Controls.Add(header);
        Controls.Add(_appMenu);

        _menu.SelectedIndex = 0;
        ShowHelpPlaceholder();
        ShowGroup(0);
        Load += (_, _) => InitializeRuntime();
        FormClosed += (_, _) => _toolTip.Dispose();
        Resize += (_, _) => LayoutContent();
        KeyDown += OnFormKeyDown;
    }

    private void WireAppMenu()
    {
        _appMenu.FileImport.Click += (_, _) => ImportProfile();
        _appMenu.FileExport.Click += (_, _) => ExportProfile();
        _appMenu.PresetLoad.Click += (_, _) => ApplySelectedPreset();
        _appMenu.ToolAutologon.Click += (_, _) => ConfigureAutologon();
        _appMenu.ToolIdentity.Click += (_, _) => ConfigureComputerIdentity();
        _appMenu.ToolSystemInfo.Click += (_, _) => ShowSystemInfo();
        _appMenu.ToolHosts.Click += (_, _) => ShowHostsEditor();
        _appMenu.ToolEventViewer.Click += (_, _) => OpenEventViewer();
        _appMenu.ToolGroupPolicy.Click += (_, _) => ShowGroupPolicy();
        _appMenu.ToolCmd.Click += (_, _) => SystemToolLauncher.OpenCommandPrompt(this);
        _appMenu.ToolPowerShell.Click += (_, _) => SystemToolLauncher.OpenWindowsPowerShell(this);
        _appMenu.ToolTaskScheduler.Click += (_, _) => SystemToolLauncher.OpenTaskScheduler(this);
        _appMenu.ToolComputerMgmt.Click += (_, _) => SystemToolLauncher.OpenComputerManagement(this);
        _appMenu.ToolFlushDns.Click += (_, _) => FlushDnsCache();
        _appMenu.ToolCommonSoftware.Click += (_, _) => ShowCommonSoftware();
        _appMenu.ToolQuick.Click += (_, _) => ShowQuickToolsDialog();
        _appMenu.ToolRefresh.Click += (_, _) => LoadState(fullScan: true);
        _appMenu.HelpUsage.Click += (_, _) => _helpDetail.ShowUsageGuide();
        _appMenu.HelpLegend.Click += (_, _) => _helpDetail.ShowScopeLegend();
        _appMenu.HelpLog.Click += (_, _) => OpenApplyLog();
        _appMenu.HelpAbout.Click += (_, _) => ShowAboutDialog();

        _appMenu.ViewHideIncompatible.CheckedChanged += (_, _) =>
        {
            _hideIncompatible.Checked = _appMenu.ViewHideIncompatible.Checked;
        };
        _hideIncompatible.CheckedChanged += (_, _) =>
        {
            _appMenu.ViewHideIncompatible.Checked = _hideIncompatible.Checked;
        };

        _appMenu.ViewHelpPanel.CheckedChanged += (_, _) =>
        {
            _helpDetail.Visible = _appMenu.ViewHelpPanel.Checked;
            LayoutContent();
        };

        var presetMenu = _appMenu.Items[1] as ToolStripMenuItem;
        if (presetMenu is not null)
        {
            foreach (ToolStripItem item in presetMenu.DropDownItems)
            {
                if (item.Tag is OptPresets.PresetInfo preset)
                    item.Click += (_, _) => LoadPresetFromMenu(preset);
            }
        }
    }

    private void LoadPresetFromMenu(OptPresets.PresetInfo preset)
    {
        for (var i = 0; i < _presetCombo.Items.Count; i++)
        {
            if (_presetCombo.Items[i] is OptPresets.PresetInfo p && p.Id == preset.Id)
            {
                _presetCombo.SelectedIndex = i;
                break;
            }
        }
        ApplySelectedPreset();
    }

    private void OpenApplyLog()
    {
        try
        {
            var path = ApplyLog.LogFilePath;
            if (!File.Exists(path))
            {
                MessageBox.Show("日志文件尚不存在：" + path, "操作日志", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "无法打开日志", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1)
        {
            _helpDetail.ShowUsageGuide();
            e.Handled = true;
        }
    }

    private void BuildWorkArea(Panel sidebar)
    {
        _workArea.BackColor = AppTheme.Surface;
        sidebar.Dock = DockStyle.Left;
        _helpDetail.Dock = DockStyle.Right;
        _contentHost.Dock = DockStyle.Fill;
        _workArea.Controls.Add(_contentHost);
        _workArea.Controls.Add(_helpDetail);
        _workArea.Controls.Add(sidebar);
    }

    private void InitializeRuntime()
    {
        if (!AdminHelper.IsRunningAsAdministrator())
        {
            _status.ForeColor = Color.FromArgb(163, 72, 0);
            _status.Text = "警告：当前未以管理员运行，应用设置可能失败。请右键「以管理员身份运行」。";
            _headerSubtitle.Text = "未以管理员运行 · " + _systemFacts.Summary;
        }
        else if (!_systemFacts.IsServer)
        {
            _status.ForeColor = AppTheme.ScopeServer;
            _status.Text = "提示：当前不是 Windows Server（" + _systemFacts.Summary + "）。部分「Server 专属」项可能无效。";
            _headerSubtitle.Text = _systemFacts.Summary + " · 非 Server 环境";
        }
        else if (!_systemFacts.HasDesktopExperience)
        {
            _status.ForeColor = AppTheme.ScopeServer;
            _status.Text = "提示：检测到 Server Core（无桌面体验）。已默认隐藏「需桌面体验」项，可取消勾选过滤。";
            _headerSubtitle.Text = _systemFacts.Summary + " · Server Core";
            _hideIncompatible.Checked = true;
        }
        else
        {
            _status.Text = _systemFacts.Summary + " · 正在加载…";
            _headerSubtitle.Text = _systemFacts.Summary;
            System.Threading.Tasks.Task.Run(() => ComputerIdentityHelper.Read().Summary)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted) return;
                    BeginInvoke(() =>
                    {
                        var identity = t.Result;
                        _status.Text = _systemFacts.Summary + " · " + identity + "。开关=推荐；关闭=恢复系统默认。";
                        _headerSubtitle.Text = _systemFacts.Summary + " · " + identity;
                    });
                });
        }

        ApplyLog.Write("启动 " + _systemFacts.Summary);
        LoadState(fullScan: false);
    }

    private void BuildCommandBar()
    {
        _commandBar.Height = 44;
        _commandBar.BackColor = AppTheme.SurfaceCard;
        _commandBar.Padding = new Padding(12, 6, 12, 6);
        _commandBar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, 0, _commandBar.Height - 1, _commandBar.Width, _commandBar.Height - 1);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(0),
        };

        flow.Controls.Add(BarLabel("搜索"));
        _searchBox.Width = 200;
        _searchBox.Height = 26;
        _searchBox.Margin = new Padding(0, 2, 16, 0);
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.ForeColor = AppTheme.TextMain;
        _searchBox.TextChanged += (_, _) => ApplySearchFilter();
        flow.Controls.Add(_searchBox);

        _hideIncompatible.Text = "隐藏不适用项";
        _hideIncompatible.AutoSize = true;
        _hideIncompatible.Margin = new Padding(0, 6, 20, 0);
        _hideIncompatible.ForeColor = AppTheme.TextMute;
        _hideIncompatible.CheckedChanged += (_, _) => ApplySearchFilter();
        flow.Controls.Add(_hideIncompatible);

        flow.Controls.Add(BarLabel("预设"));
        _presetCombo.Width = 220;
        _presetCombo.Height = 26;
        _presetCombo.Margin = new Padding(0, 2, 8, 0);
        _presetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _presetCombo.IntegralHeight = false;
        foreach (var p in OptPresets.All) _presetCombo.Items.Add(p);
        if (_presetCombo.Items.Count > 0) _presetCombo.SelectedIndex = 0;
        AdjustPresetComboDropDownWidth();
        flow.Controls.Add(_presetCombo);

        var loadPreset = CompactButton("载入预设", ApplySelectedPreset);
        loadPreset.Margin = new Padding(0, 1, 0, 0);
        flow.Controls.Add(loadPreset);

        _commandBar.Controls.Add(flow);
    }

    private static Label BarLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 8, 6, 0),
        ForeColor = AppTheme.TextMute,
        BackColor = Color.Transparent,
    };

    private static Button CompactButton(string text, Action click)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 28,
            Padding = new Padding(10, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.SurfaceCard,
            ForeColor = AppTheme.PrimaryDeep,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 1, 0, 0),
        };
        b.FlatAppearance.BorderColor = AppTheme.Border;
        b.MouseEnter += (_, _) => b.BackColor = AppTheme.PrimaryPale;
        b.MouseLeave += (_, _) => b.BackColor = AppTheme.SurfaceCard;
        b.Click += (_, _) => click();
        return b;
    }

    private void AdjustPresetComboDropDownWidth()
    {
        var max = _presetCombo.Width;
        foreach (OptPresets.PresetInfo preset in _presetCombo.Items)
        {
            var w = TextRenderer.MeasureText(preset.Title, _presetCombo.Font).Width + 28;
            if (w > max) max = w;
        }

        _presetCombo.DropDownWidth = max;
    }

    private void ApplySelectedPreset()
    {
        if (_presetCombo.SelectedItem is not OptPresets.PresetInfo preset) return;
        var answer = MessageBox.Show(
            $"将载入预设「{preset.Title}」到界面开关（尚未写入系统）。\n\n{preset.Description}\n\n是否继续？",
            "载入预设",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        Bind(preset.Build());
        _status.Text = $"已载入预设「{preset.Title}」，点击「应用推荐」写入系统。";
        ApplyLog.Write("载入预设 " + preset.Title);
    }

    private void ExportProfile()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "WinOpt 配置 (*.json)|*.json",
            FileName = "WinOpt-配置.json",
            InitialDirectory = ProfileStore.DefaultProfileDir(),
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            ProfileStore.Save(dlg.FileName, CaptureState(), "用户导出");
            _status.Text = "已导出配置：" + dlg.FileName;
            ApplyLog.Write("导出配置 " + dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportProfile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "WinOpt 配置 (*.json)|*.json",
            InitialDirectory = ProfileStore.DefaultProfileDir(),
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            var state = ProfileStore.Load(dlg.FileName);
            Bind(state);
            _status.Text = "已导入配置到界面：" + dlg.FileName + "（点击「应用推荐」写入）";
            ApplyLog.Write("导入配置 " + dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ConfigureAutologon()
    {
        if (!ConfigureAutologonDialog()) return;
        _autologon.Checked = true;
        _status.Text = $"Autologon 已配置：{_autologonSettings!.Username}（应用推荐后下次重启生效）";
    }

    private void ConfigureComputerIdentity()
    {
        try
        {
            var info = ComputerIdentityHelper.Read();
            using var dlg = new ComputerIdentityDialog(info);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var msg = dlg.RestartScheduled
                ? "计算机名/工作组已修改，系统将在 60 秒后重启（shutdown /a 可取消）。"
                : "计算机名/工作组已修改，请尽快手动重启以完全生效。";
            _status.Text = msg;
            MessageBox.Show(msg, "修改成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "读取失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowCommonSoftware()
    {
        using var dlg = new CommonSoftwareDialog();
        dlg.ShowDialog(this);
    }

    private void ShowQuickToolsDialog()
    {
        using var dlg = new QuickToolsDialog(_systemFacts);
        dlg.ShowDialog(this);
    }

    private void ShowSystemInfo()
    {
        using var dlg = new SystemInfoDialog();
        dlg.ShowDialog(this);
    }

    private void ShowHostsEditor()
    {
        using var dlg = new HostsEditorDialog();
        dlg.ShowDialog(this);
    }

    private void FlushDnsCache()
    {
        try
        {
            if (HostsFileHelper.FlushDns())
            {
                _status.Text = "已刷新 DNS 缓存（ipconfig /flushdns）。";
                MessageBox.Show(this, "DNS 解析缓存已清空。\r\n之后的域名解析会重新向 DNS 服务器查询。",
                    "刷新 DNS 缓存", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "ipconfig /flushdns 未成功完成。",
                    "刷新 DNS 缓存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "无法刷新 DNS 缓存。\r\n\r\n" + ex.Message,
                "刷新 DNS 缓存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowGroupPolicy()
    {
        using var dlg = new GroupPolicyDialog();
        dlg.ShowDialog(this);
    }

    private void OpenEventViewer()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "eventvwr.msc",
                UseShellExecute = true,
            });
            ApplyLog.Write("打开事件查看器");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "无法打开事件查看器。\r\n\r\n" + ex.Message,
                "事件查看器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool EnsureAutologonReady()
    {
        if (!_autologon.Checked) return true;
        if (_autologonSettings is not null) return true;
        return ConfigureAutologonDialog();
    }

    private bool ConfigureAutologonDialog()
    {
        var status = AutologonHelper.Read();
        var initial = AutologonHelper.FromStatus(status);
        using var dlg = new AutologonDialog(initial, status.Enabled);
        if (dlg.ShowDialog(this) != DialogResult.OK) return false;
        _autologonSettings = dlg.Settings;
        return true;
    }

    private void RefreshAutologonDisplay()
    {
        _autologon.SetSystemDefault(AutologonHelper.Read().DisplayDefault());
    }

    private void ApplySearchFilter()
    {
        if (_activeRows.Length == 0 || _activeBody is null || _activeSection is null) return;
        const int headerH = 34;
        const int rowH = 44;
        var query = _searchBox.Text;
        var hideDe = _hideIncompatible.Checked;
        var visible = 0;
        foreach (var row in _activeRows)
        {
            var show = row.MatchesFilter(query, _systemFacts, hideDe);
            row.SetVisible(show);
            if (show) visible++;
        }
        _activeBody.Height = Math.Max(visible, 1) * rowH;
        _activeSection.Height = headerH + _activeBody.Height;
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Height = 52, BackColor = AppTheme.PrimaryDeep };
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
        var logoImg = LoadLogo();
        if (logoImg is not null) logo.Image = logoImg;

        var brand = new Label
        {
            Text = "Win一键优化",
            AutoSize = false,
            Location = new Point(54, 6),
            Size = new Size(200, 28),
            ForeColor = AppTheme.TextOnPrimary,
            Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };

        _headerSubtitle.AutoSize = false;
        _headerSubtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _headerSubtitle.Location = new Point(54, 32);
        _headerSubtitle.Height = 18;
        _headerSubtitle.ForeColor = AppTheme.TextOnPrimarySoft;
        _headerSubtitle.Font = new Font("Microsoft YaHei UI", 8.5F);
        _headerSubtitle.TextAlign = ContentAlignment.MiddleLeft;
        _headerSubtitle.BackColor = Color.Transparent;
        _headerSubtitle.Text = "Windows Server 桌面优化 · 菜单栏访问文件/工具/帮助";

        header.Controls.Add(_headerSubtitle);
        header.Controls.Add(brand);
        header.Controls.Add(logo);
        header.Resize += (_, _) => _headerSubtitle.Width = Math.Max(200, header.Width - 68);
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
        _activeGroupIndex = index;
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

        _activeRows = group.Rows;
        _activeSection = section;
        _activeBody = section.Tag as Panel;

        wrap.Height = section.Bottom;
        _contentHost.Controls.Add(wrap);
        _contentHost.ResumeLayout(true);
        ShowHelpPlaceholder(group.Title);
        ApplySearchFilter();
    }

    private void ShowHelp(SettingRow row)
    {
        _selectedRow?.SetSelected(false);
        _selectedRow = row;
        row.SetSelected(true);
        _helpDetail.ShowSetting(row.ItemText, row.Help);
    }

    private void ShowHelpPlaceholder(string? groupTitle = null)
    {
        _selectedRow?.SetSelected(false);
        _selectedRow = null;
        _helpDetail.ShowPlaceholder(groupTitle);
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
        const int rowH = 44;
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
            rows[i].Mount(body, i * rowH, rowH, bg, section.Width, _toolTip, ShowHelp);
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
        section.Tag = body;
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
        _status.SetBounds(12, 10, 420, 38);
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
        var quickTools = ToolButton("快速工具", ShowQuickToolsDialog);
        var commonSoftware = ToolButton("常用软件", ShowCommonSoftware);
        var refresh = ToolButton("刷新", () => LoadState(fullScan: true));

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

        actions.Controls.AddRange([allOn, allOff, quickTools, commonSoftware, refresh, _restore, _apply]);
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

    private void LoadState(bool fullScan = false)
    {
        if (fullScan)
        {
            _status.Text = "正在完整扫描系统状态（含 DISM，可能需要数十秒）…";
            UseWaitCursor = true;
        }

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var state = Optimizer.Read(fullScan);
                BeginInvoke(new Action(() =>
                {
                    try { Bind(state); }
                    catch (Exception ex) { _status.Text = "读取当前配置失败：" + ex.Message; }
                    finally
                    {
                        if (fullScan)
                        {
                            UseWaitCursor = false;
                            if (!_status.Text.StartsWith("读取当前配置失败", StringComparison.Ordinal))
                                _status.Text = _systemFacts.Summary + " · 状态已刷新。";
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    _status.Text = "读取当前配置失败：" + ex.Message;
                    if (fullScan) UseWaitCursor = false;
                }));
            }
        });
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
        _qosSpeed.Checked = s.QosSpeedOptimize;
        _errorReport.Checked = s.DisableErrorReport;
        _longPaths.Checked = s.LongPathsEnabled;
        _fastStartup.Checked = s.DisableFastStartup;
        _autoMaint.Checked = s.DisableAutoMaintenance;
        _noDriverWu.Checked = s.ExcludeDriverUpdates;
        _smb1.Checked = s.DisableSmb1;
        _remoteReg.Checked = s.DisableRemoteRegistry;
        _spooler.Checked = s.DisablePrintSpooler;
        _largeCache.Checked = s.LargeSystemCacheOptimize;
        _reservedStorage.Checked = s.DisableReservedStorage;
        _srvSplit.Checked = s.DisableSrvSplit;
        _gpuSched.Checked = s.EnableGpuHwScheduling;
        _defender.Checked = s.DisableDefenderAntivirus;
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
        _taskbarClock.Checked = s.TaskbarClockWeekdaySeconds;
        _desktopIcons.Checked = s.ShowControlPanelRecycleBin;
        _smartScreen.Checked = s.DisableSmartScreenWarning;
        _classicSearch.Checked = s.ClassicFileSearch;
        _searchEngine.Checked = s.DisableSearchEngineFeature;
        _animations.Checked = s.DisableAnimations;
        _transparency.Checked = s.DisableTransparency;
        _tips.Checked = s.DisableTips;
        _autoplay.Checked = s.DisableAutoplay;
        _activityHist.Checked = s.DisableActivityHistory;
        _storageSense.Checked = s.DisableStorageSense;
        _backgroundApps.Checked = s.DisableBackgroundApps;
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
        _mediaFeatures.Checked = s.EnableDesktopMediaFeatures;
        _bloatFeatures.Checked = s.DisableServerBloatFeatures;
        _pwd.Checked = s.DisablePasswordComplexity;
        _pwdExpire.Checked = s.PasswordNeverExpire;
        _shutdownLogon.Checked = s.ShutdownWithoutLogon;
        _shutdownReason.Checked = s.DisableShutdownReason;
        _noCad.Checked = s.DisableCad;
        _autologon.Checked = s.EnableAutologon;
        _keyboardFilter.Checked = s.DisableLoginKeyboardFilters;
        RefreshAutologonDisplay();
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
        QosSpeedOptimize = _qosSpeed.Checked,
        DisableErrorReport = _errorReport.Checked,
        LongPathsEnabled = _longPaths.Checked,
        DisableFastStartup = _fastStartup.Checked,
        DisableAutoMaintenance = _autoMaint.Checked,
        ExcludeDriverUpdates = _noDriverWu.Checked,
        DisableSmb1 = _smb1.Checked,
        DisableRemoteRegistry = _remoteReg.Checked,
        DisablePrintSpooler = _spooler.Checked,
        LargeSystemCacheOptimize = _largeCache.Checked,
        DisableReservedStorage = _reservedStorage.Checked,
        DisableSrvSplit = _srvSplit.Checked,
        EnableGpuHwScheduling = _gpuSched.Checked,
        DisableDefenderAntivirus = _defender.Checked,
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
        TaskbarClockWeekdaySeconds = _taskbarClock.Checked,
        ShowControlPanelRecycleBin = _desktopIcons.Checked,
        DisableSmartScreenWarning = _smartScreen.Checked,
        ClassicFileSearch = _classicSearch.Checked,
        DisableSearchEngineFeature = _searchEngine.Checked,
        DisableAnimations = _animations.Checked,
        DisableTransparency = _transparency.Checked,
        DisableTips = _tips.Checked,
        DisableAutoplay = _autoplay.Checked,
        DisableActivityHistory = _activityHist.Checked,
        DisableStorageSense = _storageSense.Checked,
        DisableBackgroundApps = _backgroundApps.Checked,
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
        EnableDesktopMediaFeatures = _mediaFeatures.Checked,
        DisableServerBloatFeatures = _bloatFeatures.Checked,
        DisablePasswordComplexity = _pwd.Checked,
        PasswordNeverExpire = _pwdExpire.Checked,
        ShutdownWithoutLogon = _shutdownLogon.Checked,
        DisableShutdownReason = _shutdownReason.Checked,
        DisableCad = _noCad.Checked,
        EnableAutologon = _autologon.Checked,
        DisableLoginKeyboardFilters = _keyboardFilter.Checked,
        AutologonDomain = _autologonSettings?.Domain ?? "",
        AutologonUser = _autologonSettings?.Username ?? "",
        AutologonPassword = _autologonSettings?.Password ?? "",
        AutologonUpdatePassword = _autologonSettings?.UpdatePassword ?? true,
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
            if (!EnsureAutologonReady())
            {
                _status.Text = "已取消：启用自动登录需先配置账户。";
                return;
            }

            var errors = Optimizer.Apply(CaptureState());
            ApplyLog.WriteApply(working, errors);
            LoadState(fullScan: true);
            if (!_autologon.Checked) _autologonSettings = null;
            RefreshAutologonDisplay();
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

    private void ShowAboutDialog()
    {
        MessageBox.Show(
            "Win一键优化 v1.0\r\n" +
            "针对 Windows Server 2022/2025 个人桌面场景。\r\n\r\n" +
            "系统：" + _systemFacts.Summary + "\r\n" +
            "计算机：" + ComputerIdentityHelper.Read().Summary + "\r\n" +
            "管理员：" + (AdminHelper.IsRunningAsAdministrator() ? "是" : "否") + "\r\n" +
            "操作日志：" + ApplyLog.LogFilePath + "\r\n\r\n" +
            "预设方案对标 WinUtil；配置 JSON 导入导出。\r\n" +
            "CLI：Win一键优化.exe --apply-preset server-desktop",
            "关于 Win一键优化",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static SettingRow Row(string item, string systemDefault, SettingHelpInfo help) =>
        new(item, systemDefault, help);

    private static Button ToolButton(string text, Action click)
    {
        var font = new Font("Microsoft YaHei UI", 9F);
        var textWidth = TextRenderer.MeasureText(text, font).Width;
        var b = new Button
        {
            Text = text,
            Font = font,
            AutoSize = false,
            Size = new Size(Math.Max(72, textWidth + 24), 36),
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
        private readonly Label _scope;
        private readonly Label _info;
        private readonly Label _system;
        private Panel? _wrap;
        private Color _normalBg;

        public string ItemText { get; }
        public SettingHelpInfo Help { get; }

        public SettingRow(string item, string systemDefault, SettingHelpInfo help)
        {
            ItemText = item;
            Help = help;
            _item = new Label
            {
                Text = item,
                AutoSize = false,
                ForeColor = AppTheme.TextMain,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            _scope = new Label
            {
                Text = help.Scope.FormatBadges(),
                AutoSize = false,
                ForeColor = help.Scope.ServerOnly ? AppTheme.ScopeServer : AppTheme.ScopeTag,
                Font = new Font("Microsoft YaHei UI", 7.5F),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                Visible = help.Scope.HasBadge,
                Cursor = Cursors.Hand,
            };
            _info = new Label
            {
                Text = "ⓘ",
                AutoSize = true,
                ForeColor = AppTheme.Primary,
                Font = new Font("Segoe UI Symbol", 9F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
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

        public void SetSystemDefault(string text) => _system.Text = text;

        public bool MatchesFilter(string query, SystemFacts facts, bool hideIncompatibleDesktop)
        {
            if (hideIncompatibleDesktop && !facts.HasDesktopExperience && Help.Scope.RequiresDesktopExperience)
                return false;
            if (string.IsNullOrWhiteSpace(query)) return true;
            var q = query.Trim();
            return ItemText.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || Help.Summary.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || Help.Scope.FormatBadges().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void SetVisible(bool visible)
        {
            if (_wrap is not null) _wrap.Visible = visible;
        }

        public void SetSelected(bool selected)
        {
            if (_wrap is null) return;
            _wrap.BackColor = selected ? AppTheme.PrimaryPale : _normalBg;
            _item.ForeColor = selected ? AppTheme.PrimaryDeep : AppTheme.TextMain;
        }

        public void Mount(
            Control parent,
            int y,
            int h,
            Color bg,
            int width,
            ToolTip toolTip,
            Action<SettingRow> onSelectHelp)
        {
            _normalBg = bg;
            var wrap = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(width, h),
                BackColor = bg,
            };
            _wrap = wrap;
            _info.SetBounds(16, (h - 18) / 2, 18, 18);
            var hasScope = Help.Scope.HasBadge;
            if (hasScope)
            {
                _item.SetBounds(36, 4, 384, 20);
                _scope.SetBounds(36, 24, 420, 16);
            }
            else
            {
                _item.SetBounds(36, 0, 384, h);
            }
            _toggle.Location = new Point(500, (h - _toggle.Height) / 2);
            _system.SetBounds(628, 0, 160, h);

            var tip = Help.Summary;
            if (hasScope) tip += "\r\n[" + Help.Scope.FormatBadges() + "]";
            toolTip.SetToolTip(_item, tip);
            toolTip.SetToolTip(_info, "点击查看详细说明\r\n" + tip);
            if (hasScope) toolTip.SetToolTip(_scope, Help.Scope.FormatHelpSection());

            void Select(object? _, EventArgs __) => onSelectHelp(this);
            _item.Click += Select;
            _info.Click += Select;
            if (hasScope) _scope.Click += Select;

            wrap.Controls.Add(_info);
            wrap.Controls.Add(_item);
            if (hasScope) wrap.Controls.Add(_scope);
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
