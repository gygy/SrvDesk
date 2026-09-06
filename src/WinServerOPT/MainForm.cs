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
    private readonly SettingRow _pca = Row("禁用程序兼容性助手 PCA", "开启", SettingCatalog.DisablePca);
    private readonly SettingRow _wuPause2035 = Row("暂停功能更新至 2035", "不暂停", SettingCatalog.PauseFeatureUpdatesUntil2035);
    private readonly SettingRow _meltdown = Row("关闭 Meltdown/Spectre 缓解", "系统默认", SettingCatalog.DisableMeltdownSpectre);
    private readonly SettingRow _hvci = Row("关闭内存完整性 HVCI", "由系统决定", SettingCatalog.DisableMemoryIntegrity);
    private readonly SettingRow _wdac = Row("关闭 WDAC 应用控制", "系统默认", SettingCatalog.DisableWdac);
    private readonly SettingRow _vbs = Row("强制关闭 VBS 虚拟化安全", "由系统决定", SettingCatalog.DisableVbs);
    private readonly SettingRow _bbr2 = Row("TCP 拥塞控制开启 BBR2", "CUBIC 默认", SettingCatalog.EnableTcpBbr2);
    private readonly SettingRow _sysRestore = Row("禁用系统还原", "启用", SettingCatalog.DisableSystemRestore);
    private readonly SettingRow _ceip = Row("关闭客户体验改善计划", "启用", SettingCatalog.DisableCeip);
    private readonly SettingRow _dps = Row("禁用诊断策略服务 DPS", "自动", SettingCatalog.DisableDiagnosticPolicy);
    private readonly SettingRow _hideOs = Row("隐藏受保护的系统文件", "显示", SettingCatalog.HideProtectedOsFiles);
    private readonly SettingRow _iconsOnly = Row("始终显示图标从不缩略图", "允许缩略图", SettingCatalog.AlwaysShowIconsNeverThumbnails);
    private readonly SettingRow _emptyDrives = Row("显示空驱动器", "隐藏", SettingCatalog.ShowEmptyDrives);
    private readonly SettingRow _recentFiles = Row("显示最近使用的文件", "不显示", SettingCatalog.ShowRecentFiles);
    private readonly SettingRow _frequent = Row("显示快速访问常用文件夹", "不显示", SettingCatalog.ShowFrequentPlaces);
    private readonly SettingRow _officeCloud = Row("隐藏 office.com 云文件", "显示", SettingCatalog.HideOfficeCloudFiles);
    private readonly SettingRow _onedrive = Row("禁止 OneDrive 同步", "允许", SettingCatalog.DisableOneDrive);
    private readonly SettingRow _tbChat = Row("隐藏任务栏聊天", "显示", SettingCatalog.HideTaskbarChat);
    private readonly SettingRow _tbCopilot = Row("隐藏任务栏 Copilot", "显示", SettingCatalog.HideTaskbarCopilot);
    private readonly SettingRow _cloudSearch = Row("禁止搜索云内容", "允许", SettingCatalog.DisableCloudSearch);
    private readonly SettingRow _langList = Row("禁止网站读取语言列表", "允许", SettingCatalog.DisableWebsiteLangList);
    private readonly SettingRow _trackApps = Row("关闭应用启动跟踪", "开启", SettingCatalog.DisableAppLaunchTracking);
    private readonly SettingRow _settingsSuggest = Row("关闭设置应用建议内容", "开启", SettingCatalog.DisableSettingsSuggestions);
    private readonly SettingRow _inking = Row("关闭墨迹与键入个性化", "开启", SettingCatalog.DisableInkingPersonalization);
    private readonly SettingRow _msrt = Row("更新不含恶意软件删除工具", "包含", SettingCatalog.ExcludeMsrtFromWu);
    private readonly SettingRow _ra = Row("禁用远程协助", "允许", SettingCatalog.DisableRemoteAssistance);
    private readonly SettingRow _memComp = Row("禁用内存压缩", "启用", SettingCatalog.DisableMemoryCompression);
    private readonly SettingRow _prelaunch = Row("禁用应用预启动", "启用", SettingCatalog.DisableAppPrelaunch);
    private readonly SettingRow _pageCombine = Row("禁用内存页面合并", "启用", SettingCatalog.DisablePageCombining);
    private readonly SettingRow _ucpd = Row("禁用微软 UCPD 驱动", "启用", SettingCatalog.DisableUcpdDriver);
    private readonly SettingRow _cortana = Row("关闭 Cortana", "开启", SettingCatalog.DisableCortana);
    private readonly SettingRow _copilotAi = Row("关闭 Copilot（系统+Edge）", "开启", SettingCatalog.DisableCopilotAi);
    private readonly SettingRow _officeTel = Row("关闭 Office 遥测", "开启", SettingCatalog.DisableOfficeTelemetry);
    private readonly SettingRow _utc = Row("硬件时钟使用 UTC（双系统）", "本地时间", SettingCatalog.EnableUtcTime);
    private readonly SettingRow _hpet = Row("关闭 HPET 高精度计时器", "开启", SettingCatalog.DisableHpet);
    private readonly SettingRow _loginVerbose = Row("登录显示详细状态", "简洁", SettingCatalog.EnableLoginVerbose);
    private readonly SettingRow _netThrottle = Row("关闭多媒体网络节流", "开启", SettingCatalog.DisableNetworkThrottling);
    private readonly SettingRow _gameDvr = Row("关闭游戏栏 / Game DVR", "开启", SettingCatalog.DisableGameDvr);
    private readonly SettingRow _location = Row("禁止定位服务", "允许", SettingCatalog.DisableLocationTracking);
    private readonly SettingRow _consumer = Row("关闭消费者体验推送", "开启", SettingCatalog.DisableConsumerFeatures);
    private readonly SettingRow _edgePre = Row("禁止 Edge 预启动与后台", "允许", SettingCatalog.DisableEdgePreload);
    private readonly SettingRow _teredo = Row("禁用 Teredo 隧道", "允许", SettingCatalog.DisableTeredo);
    private readonly SettingRow _clipCloud = Row("关闭剪贴板云同步", "允许", SettingCatalog.DisableClipboardCloud);
    private readonly SettingRow _ntfsStamp = Row("关闭 NTFS 最后访问时间戳", "记录", SettingCatalog.DisableNtfsLastAccess);
    private readonly SettingRow _xbox = Row("禁用 Xbox Live 服务", "手动", SettingCatalog.DisableXboxServices);
    private readonly SettingRow _fax = Row("禁用传真服务", "手动", SettingCatalog.DisableFaxService);
    private readonly SettingRow _f8 = Row("启用 F8 高级启动菜单", "标准", SettingCatalog.EnableF8BootMenu);
    private readonly SettingRow _takeOwn = Row("右键菜单：取得所有权", "无", SettingCatalog.ContextMenuTakeOwnership);
    private readonly SettingRow _openCmd = Row("右键菜单：在此处打开 CMD", "无", SettingCatalog.ContextMenuOpenCmd);
    private readonly SettingRow _wmpShare = Row("禁用媒体播放器网络共享", "手动", SettingCatalog.DisableMediaPlayerSharing);
    private readonly SettingRow _insider = Row("禁用 Windows Insider 服务", "手动", SettingCatalog.DisableInsiderService);
    private readonly SettingRow _storeUpd = Row("禁止商店自动更新应用", "自动", SettingCatalog.DisableStoreAutoUpdate);
    private readonly SettingRow _news = Row("关闭资讯与兴趣", "开启", SettingCatalog.DisableNewsInterests);

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

    private readonly SettingRow _itemCheckboxes = Row("显示项目复选框", "不显示", SettingCatalog.ShowItemCheckboxes);
    private readonly SettingRow _commonFolders = Row("显示常用文件夹", "不显示", SettingCatalog.ShowCommonFolders);
    private readonly SettingRow _noShield = Row("去除快捷方式管理员盾牌", "显示", SettingCatalog.RemoveAdminShield);
    private readonly SettingRow _noSuffix = Row("快捷方式不加「快捷方式」后缀", "添加", SettingCatalog.NoShortcutSuffix);
    private readonly SettingRow _win11Explorer = Row("Win11 资源管理器布局", "紧凑", SettingCatalog.Win11ExplorerStyle);
    private readonly SettingRow _classicMenu = Row("Win10 经典右键菜单", "Win11 现代", SettingCatalog.Win10ClassicContextMenu);
    private readonly SettingRow _tbSearch = Row("隐藏任务栏搜索栏", "显示", SettingCatalog.HideTaskbarSearch);
    private readonly SettingRow _tbLeft = Row("任务栏靠左对齐", "居中", SettingCatalog.TaskbarAlignLeft);
    private readonly SettingRow _tbCombine = Row("任务栏按钮始终合并", "从不", SettingCatalog.TaskbarCombineAlways);
    private readonly SettingRow _tbAutohide = Row("自动隐藏任务栏", "不隐藏", SettingCatalog.TaskbarAutoHide);
    private readonly SettingRow _taskView = Row("显示任务视图按钮", "不显示", SettingCatalog.ShowTaskViewButton);
    private readonly SettingRow _tbEndTask = Row("任务栏右键结束任务", "关闭", SettingCatalog.TaskbarEndTask);
    private readonly SettingRow _widgets = Row("关闭任务栏小组件", "开启", SettingCatalog.DisableWidgets);

    private readonly SettingRow _animations = Row("禁用窗口与任务栏动画", "开启", SettingCatalog.DisableAnimations);
    private readonly SettingRow _transparency = Row("禁用透明效果", "开启", SettingCatalog.DisableTransparency);
    private readonly SettingRow _tips = Row("关闭 Windows 提示与建议", "开启", SettingCatalog.DisableTips);
    private readonly SettingRow _autoplay = Row("禁用所有驱动器自动播放", "开启", SettingCatalog.DisableAutoplay);
    private readonly SettingRow _activityHist = Row("禁用活动历史记录", "开启", SettingCatalog.DisableActivityHistory);
    private readonly SettingRow _storageSense = Row("禁用存储感知", "开启", SettingCatalog.DisableStorageSense);
    private readonly SettingRow _backgroundApps = Row("禁止应用在后台运行", "允许", SettingCatalog.DisableBackgroundApps);
    private readonly SettingRow _searchHighlights = Row("关闭搜索要点/亮点", "开启", SettingCatalog.DisableSearchHighlights);
    private readonly SettingRow _recommended = Row("关闭开始菜单推荐", "开启", SettingCatalog.DisableRecommendedItems);
    private readonly SettingRow _adTracking = Row("关闭广告标识符跟踪", "开启", SettingCatalog.DisableAdTracking);
    private readonly SettingRow _searchHistory = Row("关闭搜索历史记录", "开启", SettingCatalog.DisableSearchHistory);
    private readonly SettingRow _stickyKeys = Row("禁用粘滞键提示", "开启", SettingCatalog.DisableStickyKeys);

    private readonly SettingRow _rdp = Row("启用远程桌面（RDP）", "禁用", SettingCatalog.EnableRdp);
    private readonly SettingRow _rdpGpu = Row("RDP 硬件图形加速", "关闭", SettingCatalog.RdpGpuAccel);
    private readonly SettingRow _rdpFps = Row("RDP 提高远程帧率", "默认", SettingCatalog.RdpHighRefresh);
    private readonly SettingRow _rdpNla = Row("RDP 关闭 NLA（内网/Linux 客户端）", "开启", SettingCatalog.RdpDisableNla);
    private readonly SettingRow _netDiscovery = Row("启用网络发现与文件共享", "关闭", SettingCatalog.EnableNetworkDiscovery);
    private readonly SettingRow _smRemoting = Row("关闭 Server Manager 远程管理", "开启", SettingCatalog.DisableSmRemoting);

    private readonly SettingRow _svrMgr = Row("登录时不自动启动服务器管理器", "登录时启动", SettingCatalog.SkipServerManager);
    private readonly SettingRow _wacPrompt = Row("不再显示「立即尝试 WAC/Azure Arc」弹窗", "每次弹出", SettingCatalog.HideServerManagerWacPrompt);
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
    private readonly AppMenuStrip _appMenu = new();
    private readonly Panel _commandBar = new();
    private readonly Panel _workArea = new();
    private readonly Label _headerSubtitle = new();
    private HeaderResourceMeter? _headerMeter;
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 200 };
    private readonly Panel _contentHost = new BufferedPanel(composited: true);
    private readonly Label _status = new();
    private Optimizer.State? _baselineState;
    private readonly Button _apply = new();
    private readonly Button _restore = new();
    private Button? _refreshBottom;
    private readonly List<(string Title, (string Section, SettingRow[] Rows)[] Sections)> _groups = [];
    private readonly ListBox _menu = new();
    private int _menuHover = -1;
    private SettingRow? _selectedRow;
    private readonly SystemFacts _systemFacts = SystemInfoHelper.Detect();
    private readonly TextBox _searchBox = new();
    private readonly CheckBox _hideIncompatible = new();
    private readonly ComboBox _categoryFilter = new();
    private readonly FlowLayoutPanel _commandFlow = new();
    private SettingRow[] _activeRows = [];
    private ActiveSection[] _activeSections = [];
    private Panel? _activeWrap;
    private readonly Dictionary<string, CachedBatchPage> _batchPageCache = new(StringComparer.Ordinal);
    private bool _inBatchMode = true;
    private readonly Panel _bottomPanel = new();
    private FlowLayoutPanel? _bottomActions;
    private string _defaultStatusText = "";

    private sealed class CachedBatchPage
    {
        public Panel Wrap = null!;
        public ActiveSection[] Sections = [];
        public SettingRow[] Rows = [];
    }

    private sealed class ActiveSection
    {
        public Panel Panel = null!;
        public Panel Body = null!;
        public SettingRow[] Rows = [];
        public bool Expanded = true;
    }

    private enum RowCategoryFilter
    {
        All,
        ServerRecommended,
        OptRecommended,
        Optimized,
        NotOptimized,
    }

    private Form? _embeddedPage;
    private readonly Dictionary<string, Form> _pageCache = new(StringComparer.Ordinal);

    private static readonly string[] EmbeddedPageTitles =
    [
        "登录启动项", "DNS 设置",
    ];

    private static readonly string[] MenuItems =
    [
        "Server专属",
        "账户策略",
        "资源管理器",
        "桌面外观",
        "远程与网络",
        "隐私与体验",
        "性能及安全",
        "登录启动项",
        "电源与服务",
        "DNS 设置",
    ];

    private SettingRow[] AllRows =>
    [
        _cpu, _dep, _uac, _ie, _highPerf, _telemetry, _noUpdateReboot, _deliveryOpt, _wuNotify,
        _sysMain, _visualPerf, _powerThrottle, _hibernate, _tcp, _qosSpeed, _errorReport,
        _longPaths, _fastStartup, _autoMaint, _noDriverWu, _smb1, _remoteReg, _spooler,
        _largeCache, _reservedStorage, _srvSplit, _gpuSched, _pca, _wuPause2035,
        _meltdown, _hvci, _wdac, _vbs, _bbr2, _sysRestore, _ceip, _dps,
        _memComp, _prelaunch, _pageCombine, _ucpd,
        _netThrottle, _hpet, _ntfsStamp, _utc, _loginVerbose, _f8, _xbox, _fax, _wmpShare,
        _thisPc, _launchThisPc, _taskbar, _confirmDel, _audio, _fileExt, _themes, _search,
        _webSearch, _feedback, _noLockScreen, _hiddenFiles, _noArrow, _fullPath, _allTrayIcons,
        _taskbarClock, _desktopIcons, _smartScreen, _classicSearch, _searchEngine,
        _itemCheckboxes, _commonFolders, _noShield, _noSuffix, _win11Explorer, _classicMenu,
        _tbSearch, _tbLeft, _tbCombine, _tbAutohide, _taskView, _tbEndTask, _widgets,
        _hideOs, _iconsOnly, _emptyDrives, _recentFiles, _frequent, _officeCloud, _onedrive, _tbChat, _tbCopilot,
        _takeOwn, _openCmd, _news,
        _animations, _transparency, _tips, _autoplay, _activityHist, _storageSense, _backgroundApps,
        _searchHighlights, _recommended, _adTracking, _searchHistory, _stickyKeys,
        _cloudSearch, _langList, _trackApps, _settingsSuggest, _inking, _msrt,
        _cortana, _copilotAi, _officeTel, _gameDvr, _location, _consumer, _edgePre, _teredo, _clipCloud,
        _insider, _storeUpd,
        _rdp, _rdpGpu, _rdpFps, _rdpNla, _netDiscovery, _smRemoting, _ra,
        _svrMgr, _wacPrompt, _azure, _installer, _wia, _mediaFeatures, _bloatFeatures,
        _pwd, _pwdExpire, _shutdownLogon, _shutdownReason, _noCad, _autologon, _keyboardFilter
    ];

    public MainForm()
    {
        Text = $"{AppBrand.ProductName} v{AppBrand.VersionText}";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 720);
        ClientSize = new Size(1280, 760);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextMain;
        AppBrand.ApplyWindowIcon(this);
        KeyPreview = true;
        MainMenuStrip = _appMenu;

        // 批量分组顺序与 MenuItems 中分组项一致；组内可再分可折叠分区
        _groups.Add(("性能及安全", [
            ("性能及安全", [
                // Server 桌面必备
                _ie, _uac, _highPerf,
                // 遥测与诊断
                _telemetry, _dps, _ceip, _errorReport,
                // 性能与显卡
                _visualPerf, _powerThrottle, _gpuSched, _largeCache, _pca,
                // Windows 更新
                _noUpdateReboot, _wuNotify, _noDriverWu, _wuPause2035,
                // 网络栈
                _tcp, _qosSpeed, _bbr2, _netThrottle,
                // 安全服务
                _smb1, _remoteReg, _spooler,
                // 进阶安全（低频）
                _dep, _cpu, _meltdown, _hvci, _wdac, _vbs, _sysRestore,
                // 存储与文件系统
                _longPaths, _ntfsStamp, _reservedStorage, _srvSplit,
                // 维护与启动
                _autoMaint, _utc, _hpet, _loginVerbose, _f8,
                // 少见服务
                _xbox, _fax, _wmpShare,
            ]),
        ]));
        _groups.Add(("桌面外观", [
            ("桌面图标", [
                _thisPc, _desktopIcons, _confirmDel,
            ]),
            ("任务栏", [
                _taskbar, _allTrayIcons, _tbEndTask, _news,
            ]),
            ("桌面服务", [
                _themes, _audio, _search,
            ]),
            ("安全与锁屏", [
                _smartScreen, _noLockScreen, _feedback,
            ]),
            ("搜索模式", [
                _classicSearch, _searchEngine,
            ]),
            ("右键菜单", [
                _takeOwn, _openCmd,
            ]),
        ]));
        _groups.Add(("资源管理器", [
            ("常用显示", [
                _fileExt, _hiddenFiles, _fullPath, _hideOs, _launchThisPc,
            ]),
            ("快速访问", [
                _recentFiles, _frequent, _officeCloud, _emptyDrives, _iconsOnly,
            ]),
            ("快捷方式与布局", [
                _noArrow, _noSuffix, _noShield, _win11Explorer, _classicMenu, _onedrive,
            ]),
            ("任务栏", [
                _tbSearch, _tbLeft, _tbCombine, _widgets, _tbChat, _tbCopilot,
                _tbAutohide, _taskView, _taskbarClock,
            ]),
        ]));
        _groups.Add(("远程与网络", [
            ("远程与网络", [
                _rdpGpu, _rdpFps, _rdpNla,
                _netDiscovery, _smRemoting,
            ]),
        ]));
        _groups.Add(("电源与服务", [
            ("远程桌面", [
                _rdp, _ra,
            ]),
            ("电源与休眠", [
                _hibernate, _fastStartup,
            ]),
            ("后台服务与内存", [
                _sysMain, _memComp, _prelaunch, _pageCombine, _ucpd,
            ]),
        ]));
        _groups.Add(("隐私与体验", [
            ("隐私与体验", [
                _tips, _recommended, _searchHighlights, _cortana, _copilotAi,
                _adTracking, _cloudSearch, _webSearch, _searchHistory,
                _trackApps, _langList, _location, _activityHist, _clipCloud,
                _animations, _transparency, _stickyKeys, _backgroundApps,
                _storageSense, _autoplay,
                _settingsSuggest, _inking, _consumer, _edgePre,
                _deliveryOpt, _msrt, _insider, _storeUpd,
                _gameDvr, _officeTel, _teredo,
            ]),
        ]));
        _groups.Add(("Server专属", [
            ("Server专属", [
                _svrMgr, _wacPrompt, _azure,
                _mediaFeatures, _bloatFeatures,
                _installer, _wia,
            ]),
        ]));
        _groups.Add(("账户策略", [
            ("账户策略", [
                _autologon, _pwd, _pwdExpire, _noCad,
                _shutdownLogon, _shutdownReason, _keyboardFilter,
            ]),
        ]));

        foreach (var row in AllRows)
        {
            row.OnCheckedChanged = _ =>
            {
                var cat = CurrentCategoryFilter();
                if (cat is RowCategoryFilter.Optimized or RowCategoryFilter.NotOptimized)
                    ApplySearchFilter();
            };
        }

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
        _bottomPanel.Dock = DockStyle.Bottom;

        Controls.Add(_workArea);
        Controls.Add(_bottomPanel);
        Controls.Add(_commandBar);
        Controls.Add(header);
        Controls.Add(_appMenu);

        _menu.SelectedIndex = 0;
        ShowHelpPlaceholder();
        Load += (_, _) => InitializeRuntime();
        FormClosed += (_, _) =>
        {
            ClearPageCache();
            _toolTip.Dispose();
        };
        Resize += (_, _) => LayoutContent();
        KeyDown += OnFormKeyDown;
    }

    private void WireAppMenu()
    {
        _appMenu.FileImport.Click += (_, _) => ImportProfile();
        _appMenu.FileExport.Click += (_, _) => ExportProfile();
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
        _appMenu.ToolCleanup.Click += (_, _) => { using var d = new CleanupDialog(); d.ShowDialog(this); };
        _appMenu.ToolDesktopMaintenance.Click += (_, _) => ShowDesktopMaintenance();
        _appMenu.ToolPowerExtras.Click += (_, _) =>
        {
            using var d = new OtherSettingsDialog();
            d.ShowDialog(this);
        };
        _appMenu.ToolWindowsFeatures.Click += (_, _) =>
        {
            using var d = new WindowsFeaturesDialog();
            d.ShowDialog(this);
        };
        _appMenu.ToolContextMenu.Click += (_, _) =>
        {
            using var d = new ContextMenuSettingsDialog();
            d.ShowDialog(this);
            // 对话框即时写入后，同步批量页所有相关开关，避免「应用到系统」覆盖
            SyncContextMenuRowsFromSystem();
        };
        _appMenu.ToolQuick.Click += (_, _) => ShowQuickToolsDialog();
        _appMenu.ToolRefresh.Click += (_, _) => LoadState(fullScan: true);
        _appMenu.ToolRestoreDefaults.Click += (_, _) => RestoreDefaults();
        _appMenu.ViewAllOn.Click += (_, _) => SetVisibleAll(true);
        _appMenu.ViewAllOff.Click += (_, _) => SetVisibleAll(false);
        _appMenu.HelpUsage.Click += (_, _) => _helpDetail.ShowUsageGuide();
        _appMenu.HelpLegend.Click += (_, _) => _helpDetail.ShowScopeLegend();
        _appMenu.HelpChangeLog.Click += (_, _) => OpenLogFile(ApplyLog.ChangeLogFilePath, "变更日志");
        _appMenu.HelpLog.Click += (_, _) => OpenLogFile(ApplyLog.LogFilePath, "操作日志");
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
            UiPrefs.SetShowHelpPanel(_appMenu.ViewHelpPanel.Checked);
            LayoutContent();
        };

    }

    private void OpenLogFile(string path, string title)
    {
        try
        {
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);

            var isChangeLog = string.Equals(path, ApplyLog.ChangeLogFilePath, StringComparison.OrdinalIgnoreCase);
            if (!File.Exists(path))
            {
                var tip = isChangeLog
                    ? "# 变更日志 — 仅记录优化时真正改动的值（原来从 xx 变成 yy）\r\n" +
                      "# 当前尚无变更记录。\r\n" +
                      "# 请先：勾选推荐项 → 点击底部「应用到系统」→ 再打开本文件。\r\n" +
                      "# 即时页（登录启动项/DNS 等）开关切换后也会写入。\r\n"
                    : $"# {title}\r\n# 尚无记录。\r\n";
                File.WriteAllText(path, tip, new System.Text.UTF8Encoding(true));
            }

            if (isChangeLog && !ApplyLog.HasRealChangeEntries())
            {
                MessageBox.Show(
                    "变更日志里还没有「原来从 xx 变成 yy」的记录。\r\n\r\n" +
                    "请先点击底部「应用到系统」（或以管理员运行新版 SrvDesk.exe），\r\n" +
                    "应用成功后再打开「帮助 → 打开变更日志」。\r\n\r\n" +
                    "路径：\r\n" + path,
                    "变更日志为空",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "无法打开" + title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1)
        {
            if (!_helpDetail.Visible)
            {
                _appMenu.ViewHelpPanel.Checked = true;
            }
            _helpDetail.ShowUsageGuide();
            e.Handled = true;
        }
    }

    private void BuildWorkArea(Panel sidebar)
    {
        UiBuffer.Enable(_workArea);
        _workArea.BackColor = AppTheme.Surface;
        sidebar.Dock = DockStyle.Left;
        _helpDetail.Dock = DockStyle.Right;
        _contentHost.Dock = DockStyle.Fill;
        var showHelp = UiPrefs.Load().ShowHelpPanel;
        _helpDetail.Visible = showHelp;
        _appMenu.ViewHelpPanel.Checked = showHelp;
        _workArea.Controls.Add(_contentHost);
        _workArea.Controls.Add(_helpDetail);
        _workArea.Controls.Add(sidebar);
    }

    private void InitializeRuntime()
    {
        if (!AdminHelper.IsRunningAsAdministrator())
        {
            _status.ForeColor = Color.FromArgb(163, 72, 0);
            _status.Text = "提示：当前进程未提升权限，部分系统级项可能写入失败（失败项会显示在状态栏）。";
            _headerSubtitle.Text = _systemFacts.Summary;
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
                        _status.Text = _systemFacts.Summary + " · " + identity;
                        _headerSubtitle.Text = _systemFacts.Summary + " · " + identity;
                    });
                });
        }

        ApplyLog.Write("启动 " + _systemFacts.Summary);
        UseWaitCursor = false;
        Cursor = Cursors.Default;
        // 后台预热常用软件状态，点击打开时尽量秒开
        CommonSoftwareHelper.WarmUpInBackground();
        System.Threading.Tasks.Task.Run(() =>
        {
            try { CommonSoftwareHelper.PrefetchStatuses(CommonSoftwareCatalog.All); }
            catch { /* ignore */ }
        });
        // 启动先快速读取；完整扫描放后台、不改鼠标样式，避免一直转圈
        LoadState(fullScan: false);
        BeginInvoke(new Action(StartWarmupInstantPages));
        BeginInvoke(new Action(() =>
        {
            // 空闲后再补全 DISM 等慢项，仍不使用等待光标
            LoadState(fullScan: true);
        }));
    }

    /// <summary>空闲时分帧预创建即时页，并预热 MMAgent，减轻首次点左侧菜单的卡顿。</summary>
    private void StartWarmupInstantPages()
    {
        System.Threading.Tasks.Task.Run(EasySettingsTweaks.WarmupMmAgentCache);

        var titles = new[] { "登录启动项", "DNS 设置" };
        var i = 0;
        var timer = new System.Windows.Forms.Timer { Interval = 40 };
        timer.Tick += (_, _) =>
        {
            if (i >= titles.Length)
            {
                timer.Stop();
                timer.Dispose();
                return;
            }

            var title = titles[i++];
            if (_pageCache.ContainsKey(title)) return;
            try
            {
                Form page = title switch
                {
                    "登录启动项" => new StartupManagerDialog(),
                    "DNS 设置" => new DnsSwitcherDialog(),
                    _ => throw new InvalidOperationException(title),
                };
                page.TopLevel = false;
                page.FormBorderStyle = FormBorderStyle.None;
                page.ControlBox = false;
                page.Dock = DockStyle.Fill;
                page.Visible = false;
                // 空闲时创建窗口句柄，避免首次点开时同步 CreateHandle
                page.CreateControl();
                if (page is IEmbeddedSettingsPage embedded)
                    embedded.RefreshFromSystem();
                _pageCache[title] = page;
            }
            catch
            {
                // 预热失败不影响主界面
            }
        };
        timer.Start();
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

        _commandFlow.Dock = DockStyle.Fill;
        _commandFlow.FlowDirection = FlowDirection.LeftToRight;
        _commandFlow.WrapContents = false;
        _commandFlow.AutoScroll = true;
        _commandFlow.BackColor = AppTheme.SurfaceCard;
        _commandFlow.Padding = new Padding(0);

        _commandFlow.Controls.Add(BarLabel("搜索"));
        _searchBox.Width = 200;
        _searchBox.Height = 26;
        _searchBox.Margin = new Padding(0, 2, 16, 0);
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.ForeColor = AppTheme.TextMain;
        _searchBox.TextChanged += (_, _) => ApplySearchFilter();
        _commandFlow.Controls.Add(_searchBox);

        _hideIncompatible.Text = "隐藏不适用项";
        _hideIncompatible.AutoSize = true;
        _hideIncompatible.Margin = new Padding(0, 6, 20, 0);
        _hideIncompatible.ForeColor = AppTheme.TextMute;
        _hideIncompatible.CheckedChanged += (_, _) => ApplySearchFilter();
        _commandFlow.Controls.Add(_hideIncompatible);

        _commandFlow.Controls.Add(BarLabel("分类"));
        _categoryFilter.Width = 130;
        _categoryFilter.Height = 26;
        _categoryFilter.Margin = new Padding(0, 2, 0, 0);
        _categoryFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryFilter.Items.AddRange([
            "全部",
            "Server 推荐",
            "优化推荐",
            "已优化",
            "未优化",
        ]);
        _categoryFilter.SelectedIndex = 0;
        _categoryFilter.SelectedIndexChanged += (_, _) => ApplySearchFilter();
        _toolTip.SetToolTip(_categoryFilter,
            "Server 推荐：Server 专属项\r\n优化推荐：通用桌面/性能/隐私项\r\n已优化 / 未优化：按当前开关状态筛选");
        _commandFlow.Controls.Add(_categoryFilter);

        // 即时页不再在此显示提示（统一走底部状态栏）
        _commandBar.Controls.Add(_commandFlow);
    }

    private static Label BarLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 8, 6, 0),
        ForeColor = AppTheme.TextMute,
        BackColor = Color.Transparent,
    };

    private RowCategoryFilter CurrentCategoryFilter() =>
        _categoryFilter.SelectedIndex switch
        {
            1 => RowCategoryFilter.ServerRecommended,
            2 => RowCategoryFilter.OptRecommended,
            3 => RowCategoryFilter.Optimized,
            4 => RowCategoryFilter.NotOptimized,
            _ => RowCategoryFilter.All,
        };

    private void ExportProfile()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "SrvDesk 配置 (*.json)|*.json",
            FileName = "SrvDesk-配置.json",
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
            Filter = "SrvDesk 配置 (*.json)|*.json",
            InitialDirectory = ProfileStore.DefaultProfileDir(),
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            var state = ProfileStore.Load(dlg.FileName);
            Bind(state);
            _status.Text = "已导入配置到界面：" + dlg.FileName + "（点击「应用到系统」生效）";
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
        _status.Text = $"Autologon 已配置：{_autologonSettings!.Username}（应用到系统后下次重启生效）";
    }

    private void ConfigureComputerIdentity() => PromptComputerIdentity();

    private CommonSoftwareDialog? _commonSoftwareDlg;

    private void ShowCommonSoftware()
    {
        CommonSoftwareHelper.WarmUpInBackground();
        if (_commonSoftwareDlg is { IsDisposed: false })
        {
            if (_commonSoftwareDlg.WindowState == FormWindowState.Minimized)
                _commonSoftwareDlg.WindowState = FormWindowState.Normal;
            _commonSoftwareDlg.BringToFront();
            _commonSoftwareDlg.Activate();
            return;
        }

        _commonSoftwareDlg = new CommonSoftwareDialog();
        // 不设 Owner，避免子窗安装/弹窗时连带卡住主窗
        _commonSoftwareDlg.ShowInTaskbar = true;
        _commonSoftwareDlg.MinimizeBox = true;
        _commonSoftwareDlg.FormClosed += (_, _) => _commonSoftwareDlg = null;
        _commonSoftwareDlg.Show();
        _commonSoftwareDlg.BringToFront();
    }

    private void ShowDesktopMaintenance()
    {
        using var dlg = new DesktopMaintenanceDialog();
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
        if (_activeSections.Length == 0 || _activeWrap is null) return;
        const int headerH = 34;
        const int rowH = 44;
        var query = _searchBox.Text;
        var hideDe = _hideIncompatible.Checked;
        var category = CurrentCategoryFilter();
        var totalVisible = 0;
        foreach (var sec in _activeSections)
        {
            var visible = 0;
            foreach (var row in sec.Rows)
            {
                var show = row.MatchesFilter(query, _systemFacts, hideDe)
                    && row.MatchesCategory(category);
                row.SetVisible(show);
                if (!show) continue;
                row.SetLocationY(visible * rowH);
                visible++;
            }

            totalVisible += visible;
            sec.Body.Height = Math.Max(visible, 1) * rowH;
            sec.Panel.Visible = visible > 0;
            if (!sec.Panel.Visible) continue;
            sec.Panel.Height = sec.Expanded ? headerH + sec.Body.Height : headerH;
            sec.Body.Visible = sec.Expanded;
        }

        RelayoutActiveSections();
        if (totalVisible == 0 && (!string.IsNullOrWhiteSpace(query) || category != RowCategoryFilter.All))
            _status.Text = "无匹配项，请调整搜索或分类筛选。";
        else if (_status.Text.StartsWith("无匹配项", StringComparison.Ordinal))
            _status.Text = _defaultStatusText;
    }

    private void RelayoutActiveWrap() => RelayoutActiveSections();

    private void RelayoutActiveSections()
    {
        if (_activeWrap is null) return;
        var w = ContentWidth();
        _activeWrap.Width = w;
        var y = 0;
        foreach (Control c in _activeWrap.Controls)
        {
            if (c.Tag as string != "table-header") continue;
            c.Width = w;
            foreach (Control h in c.Controls)
            {
                if (h.Tag as string == "note-header")
                    h.Width = SettingListLayout.NoteWidthFor(w);
            }
            y = c.Bottom;
        }

        foreach (var sec in _activeSections)
        {
            if (!sec.Panel.Visible) continue;
            sec.Panel.Width = w;
            sec.Panel.Top = y;
            sec.Body.Width = w;
            foreach (var row in sec.Rows)
                row.ApplyLayoutWidth(w);
            y = sec.Panel.Bottom;
        }

        _activeWrap.Height = Math.Max(y, 1);
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Height = 48, BackColor = AppTheme.PrimaryDeep };
        header.Paint += (_, e) =>
        {
            var r = header.ClientRectangle;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                r, AppTheme.HeaderBarTop, AppTheme.HeaderBarBottom, 90f);
            e.Graphics.FillRectangle(brush, r);
        };

        var logo = new PictureBox
        {
            Size = new Size(32, 32),
            Location = new Point(14, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };
        var logoImg = LoadLogo();
        if (logoImg is not null) logo.Image = logoImg;

        _headerMeter = new HeaderResourceMeter
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(header.Width - 312, 0),
        };

        // 蓝色顶栏左侧系统信息，右侧资源占用
        _headerSubtitle.AutoSize = false;
        _headerSubtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _headerSubtitle.Location = new Point(54, 0);
        _headerSubtitle.Height = 48;
        _headerSubtitle.ForeColor = AppTheme.TextOnPrimarySoft;
        _headerSubtitle.Font = new Font("Microsoft YaHei UI", 9F);
        _headerSubtitle.TextAlign = ContentAlignment.MiddleLeft;
        _headerSubtitle.BackColor = Color.Transparent;
        _headerSubtitle.Text = "Windows Server 桌面优化 · 菜单栏访问文件/工具/帮助";

        header.Controls.Add(_headerSubtitle);
        header.Controls.Add(_headerMeter);
        header.Controls.Add(logo);
        void LayoutHeader()
        {
            if (_headerMeter is null) return;
            _headerMeter.Left = Math.Max(200, header.Width - _headerMeter.Width - 12);
            _headerSubtitle.Width = Math.Max(120, _headerMeter.Left - _headerSubtitle.Left - 12);
        }
        header.Resize += (_, _) => LayoutHeader();
        LayoutHeader();
        return header;
    }

    private static Image? LoadLogo() => AppBrand.LoadLogoImage();

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 196, BackColor = AppTheme.NavBg };
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
        return sidebar;
    }

    private void BuildContent()
    {
        _contentHost.BackColor = AppTheme.Surface;
        _contentHost.Padding = new Padding(12, 8, 12, 8);
        _contentHost.AutoScroll = true;
    }

    private static bool IsEmbeddedMenuTitle(string title) =>
        Array.IndexOf(EmbeddedPageTitles, title) >= 0;

    private int FindBatchGroupIndex(string title)
    {
        for (var i = 0; i < _groups.Count; i++)
            if (_groups[i].Title == title) return i;
        return -1;
    }

    private void ShowGroup(int index)
    {
        if (index < 0 || index >= MenuItems.Length) return;

        var title = MenuItems[index];
        if (IsEmbeddedMenuTitle(title))
        {
            ShowEmbeddedPage(index);
            return;
        }

        var groupIndex = FindBatchGroupIndex(title);
        if (groupIndex < 0) return;

        using (UiBuffer.SuspendRedraw(_workArea))
        {
            SetBatchMode(batch: true);
            var fromPage = _embeddedPage is not null;
            DetachEmbeddedPage();
            DetachActiveBatchWrap();

            _contentHost.SuspendLayout();
            _contentHost.AutoScroll = false;
            _contentHost.Padding = new Padding(12, 8, 12, 8);

            if (_batchPageCache.TryGetValue(title, out var cached))
            {
                _activeRows = cached.Rows;
                _activeSections = cached.Sections;
                _activeWrap = cached.Wrap;
                if (!_contentHost.Controls.Contains(cached.Wrap))
                    _contentHost.Controls.Add(cached.Wrap);
                cached.Wrap.Visible = true;
                _contentHost.AutoScrollMinSize = Size.Empty;
                _contentHost.AutoScroll = true;
                _contentHost.ResumeLayout(true);
                try { _contentHost.AutoScrollPosition = Point.Empty; }
                catch { /* ignore */ }
                ShowHelpPlaceholder(title);
                ApplySearchFilter();
                if (fromPage) LoadState(fullScan: false);
                return;
            }

            var wrap = new BufferedPanel
            {
                Location = new Point(0, 0),
                Width = ContentWidth(),
                AutoSize = false,
                BackColor = AppTheme.SurfaceCard,
            };
            wrap.Paint += (_, e) =>
            {
                using var pen = new Pen(AppTheme.BorderLight);
                e.Graphics.DrawRectangle(pen, 0, 0, wrap.Width - 1, wrap.Height - 1);
            };

            var header = BuildTableHeader();
            wrap.Controls.Add(header);

            var group = _groups[groupIndex];
            var sections = new List<ActiveSection>();
            var allRows = new List<SettingRow>();
            var y = header.Height;
            foreach (var (sectionTitle, rows) in group.Sections)
            {
                var ordered = OrderRowsByRecommend(rows);
                var section = BuildGroupSection(sectionTitle, ordered);
                section.Location = new Point(0, y);
                wrap.Controls.Add(section);
                sections.Add(new ActiveSection
                {
                    Panel = section,
                    Body = (Panel)section.Tag!,
                    Rows = ordered,
                    Expanded = true,
                });
                allRows.AddRange(ordered);
                y = section.Bottom;
            }

            _activeRows = allRows.ToArray();
            _activeSections = sections.ToArray();
            _activeWrap = wrap;
            _batchPageCache[title] = new CachedBatchPage
            {
                Wrap = wrap,
                Sections = _activeSections,
                Rows = _activeRows,
            };

            wrap.Height = Math.Max(y, 1);
            _contentHost.Controls.Add(wrap);
            _contentHost.AutoScrollMinSize = Size.Empty;
            _contentHost.AutoScroll = true;
            _contentHost.ResumeLayout(true);
            try { _contentHost.AutoScrollPosition = Point.Empty; }
            catch { /* ignore */ }
            ShowHelpPlaceholder(group.Title);
            ApplySearchFilter();
            if (fromPage) LoadState(fullScan: false);
        }
    }

    private void ShowEmbeddedPage(int index)
    {
        var title = MenuItems[index];
        using (UiBuffer.SuspendRedraw(_workArea))
        {
            SetBatchMode(batch: false, embeddedTitle: title);
            DetachActiveBatchWrap();

            if (_embeddedPage is not null)
            {
                var switchingAway = !_pageCache.TryGetValue(title, out var existing) ||
                                    !ReferenceEquals(_embeddedPage, existing);
                if (switchingAway)
                    DetachEmbeddedPage();
            }

            if (!_pageCache.TryGetValue(title, out var page))
            {
                page = title switch
                {
                    "登录启动项" => new StartupManagerDialog(),
                    "DNS 设置" => new DnsSwitcherDialog(),
                    _ => throw new InvalidOperationException(title),
                };
                page.TopLevel = false;
                page.FormBorderStyle = FormBorderStyle.None;
                page.ControlBox = false;
                page.Dock = DockStyle.Fill;
                _pageCache[title] = page;
            }

            _activeRows = [];
            _activeSections = [];
            _activeWrap = null;
            _contentHost.Padding = new Padding(0);
            _contentHost.AutoScroll = false;
            _contentHost.SuspendLayout();
            if (!_contentHost.Controls.Contains(page))
                _contentHost.Controls.Add(page);
            _embeddedPage = page;
            page.Visible = true;
            if (!page.IsHandleCreated) page.Show();
            _contentHost.ResumeLayout(true);
            UpdateBottomActionEnablement(title);
            ShowHelpPlaceholder(title);
            if (page is IEmbeddedSettingsPage embedded && !embedded.ConsumeWarmLoadSkip())
                RunWhenHandleReady(embedded.RefreshFromSystem);
        }
    }

    private void RunWhenHandleReady(Action action)
    {
        if (IsHandleCreated)
        {
            BeginInvoke(action);
            return;
        }

        void OnLoad(object? sender, EventArgs e)
        {
            Load -= OnLoad;
            BeginInvoke(action);
        }

        Load += OnLoad;
    }

    private void RefreshEmbeddedPageIfVisible()
    {
        if (_embeddedPage is IEmbeddedSettingsPage page)
            page.RefreshFromSystem();
    }

    private void DetachEmbeddedPage()
    {
        if (_embeddedPage is null) return;
        _embeddedPage.Visible = false;
        _contentHost.Controls.Remove(_embeddedPage);
        _embeddedPage = null;
    }

    private void DetachActiveBatchWrap()
    {
        if (_activeWrap is null) return;
        _activeWrap.Visible = false;
        _contentHost.Controls.Remove(_activeWrap);
        _activeWrap = null;
        _activeRows = [];
        _activeSections = [];
    }

    private void DisposeEmbeddedPage() => DetachEmbeddedPage();

    private void ClearPageCache()
    {
        DetachEmbeddedPage();
        DetachActiveBatchWrap();
        foreach (var page in _pageCache.Values)
            page.Dispose();
        _pageCache.Clear();
        foreach (var cached in _batchPageCache.Values)
            cached.Wrap.Dispose();
        _batchPageCache.Clear();
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
        if (groupTitle is not null && Array.IndexOf(EmbeddedPageTitles, groupTitle) >= 0)
            _helpDetail.ShowEmbeddedGuide(groupTitle);
        else
            _helpDetail.ShowPlaceholder(groupTitle);
    }

    private void SetBatchMode(bool batch, string? embeddedTitle = null)
    {
        _inBatchMode = batch;
        // 命令栏高度始终保留，避免即时页/批量页切换时内容区上下跳动
        _commandBar.Visible = true;
        _commandBar.Height = 44;
        _commandFlow.Visible = batch;

        UpdateBottomActionEnablement(embeddedTitle);

        _appMenu.ViewAllOn.Enabled = batch;
        _appMenu.ViewAllOff.Enabled = batch;
        _appMenu.ToolRestoreDefaults.Enabled = batch;

        if (batch)
        {
            _status.Text = _defaultStatusText;
            return;
        }

        _status.Text = _defaultStatusText;
    }

    private void UpdateBottomActionEnablement(string? embeddedTitle = null)
    {
        // 底部按钮始终占位，仅改 Enabled，避免右侧按钮区宽度抖动
        if (_refreshBottom is not null)
        {
            _refreshBottom.Visible = true;
            _refreshBottom.Enabled = true;
        }

        _restore.Visible = true;
        _restore.Enabled = _inBatchMode;

        var canApplyEmbedded = !_inBatchMode
            && _embeddedPage is IEmbeddedSettingsPage page
            && page.SupportsApplyToSystem;
        _apply.Visible = true;
        _apply.Enabled = _inBatchMode || canApplyEmbedded;
    }

    private int ContentWidth() =>
        Math.Max(SettingListLayout.NoteX + 160, _contentHost.ClientSize.Width - _contentHost.Padding.Horizontal);

    private void LayoutContent()
    {
        if (_embeddedPage is not null) return;
        RelayoutActiveWrap();
        if (_activeWrap is not null)
        {
            foreach (Control c in _activeWrap.Controls)
            {
                if (c.Tag as string == "table-header")
                    c.Width = _activeWrap.Width;
            }
        }
    }

    private Panel BuildTableHeader()
    {
        const int h = 36;
        var header = new BufferedPanel
        {
            Location = new Point(0, 0),
            Size = new Size(ContentWidth(), h),
            BackColor = AppTheme.PrimaryLight,
            Tag = "table-header",
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        header.Controls.Add(MakeHeaderLabel("项目", SettingListLayout.InfoX, SettingListLayout.RecommendHeaderX - SettingListLayout.InfoX - 4));
        header.Controls.Add(MakeHeaderLabel("设置操作", SettingListLayout.RecommendHeaderX, SettingListLayout.RecommendHeaderW, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderLabel("系统默认值", SettingListLayout.SystemX, SettingListLayout.SystemW, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderLabel("系统当前值", SettingListLayout.CurrentX, SettingListLayout.CurrentW, ContentAlignment.MiddleCenter));
        var levelHeader = MakeHeaderLabel("推荐值", SettingListLayout.LevelX, SettingListLayout.LevelW, ContentAlignment.MiddleCenter);
        levelHeader.Tag = "level-header";
        _toolTip.SetToolTip(levelHeader, RecommendLevelUi.LegendShort);
        header.Controls.Add(levelHeader);
        var noteHeader = MakeHeaderLabel("说明", SettingListLayout.NoteX, SettingListLayout.NoteWidthFor(ContentWidth()));
        noteHeader.Tag = "note-header";
        header.Controls.Add(noteHeader);
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

    /// <summary>分区内按推荐强度降序：必优化 → 强烈推荐 → 建议优化 → 可选；同级按标题。</summary>
    private static SettingRow[] OrderRowsByRecommend(SettingRow[] rows)
    {
        var ordered = (SettingRow[])rows.Clone();
        Array.Sort(ordered, (a, b) =>
        {
            var byLevel = b.Help.Recommend.CompareTo(a.Help.Recommend);
            if (byLevel != 0)
                return byLevel;
            return string.Compare(a.ItemText, b.ItemText, StringComparison.CurrentCultureIgnoreCase);
        });
        return ordered;
    }

    private Panel BuildGroupSection(string title, SettingRow[] rows)
    {
        const int headerH = 34;
        const int rowH = 44;
        var expanded = true;
        var section = new BufferedPanel
        {
            Location = new Point(0, 0),
            Width = ContentWidth(),
            Height = headerH + rows.Length * rowH,
            BackColor = AppTheme.SurfaceCard,
        };

        var head = new BufferedPanel
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

        var body = new BufferedPanel
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
            var sec = Array.Find(_activeSections, s => ReferenceEquals(s.Panel, section));
            if (sec is not null)
            {
                sec.Expanded = !sec.Expanded;
                expanded = sec.Expanded;
            }
            else
            {
                expanded = !expanded;
            }

            arrow.Text = expanded ? "▼" : "▶";
            body.Visible = expanded;
            section.Height = expanded ? headerH + Math.Max(body.Height, rowH) : headerH;
            RelayoutActiveSections();
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
            foreach (var row in rows)
                row.ApplyLayoutWidth(section.Width);
        };
        return section;
    }

    private Panel BuildBottom()
    {
        _bottomPanel.Height = 58;
        _bottomPanel.BackColor = AppTheme.SurfaceCard;
        var rule = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = AppTheme.BorderLight };

        _status.AutoSize = false;
        _status.SetBounds(12, 10, 280, 38);
        _status.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
        _status.ForeColor = AppTheme.TextMute;
        _status.AutoEllipsis = true;
        _defaultStatusText = "";
        _status.Text = _defaultStatusText;

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
        _bottomActions = actions;

        // 底部三键始终占位：刷新 / 恢复默认 / 应用到系统（按页启用）
        _refreshBottom = ToolButton("刷新", () => LoadState(fullScan: true));

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

        _apply.Text = "应用到系统";
        _apply.AutoSize = false;
        _apply.Size = new Size(108, 36);
        _apply.Margin = new Padding(8, 0, 0, 0);
        _apply.FlatStyle = FlatStyle.Flat;
        _apply.FlatAppearance.BorderSize = 0;
        _apply.BackColor = AppTheme.Primary;
        _apply.ForeColor = AppTheme.TextOnPrimary;
        _apply.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _apply.Cursor = Cursors.Hand;
        _apply.Click += (_, _) => OnApplyClicked();
        _apply.MouseEnter += (_, _) => _apply.BackColor = AppTheme.PrimaryDark;
        _apply.MouseLeave += (_, _) => _apply.BackColor = AppTheme.Primary;

        actions.Controls.AddRange([_refreshBottom, _restore, _apply]);
        _bottomPanel.Controls.Add(actions);
        _bottomPanel.Controls.Add(_status);
        _bottomPanel.Controls.Add(rule);
        _bottomPanel.Resize += (_, _) =>
        {
            var right = actions.Width + 24;
            _status.Width = Math.Max(120, _bottomPanel.ClientSize.Width - right - 12);
        };
        return _bottomPanel;
    }

    private void DrawMenuItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        var hover = e.Index == _menuHover && !selected;
        using var back = new SolidBrush(selected ? AppTheme.Primary : hover ? AppTheme.NavHover : AppTheme.NavBg);
        e.Graphics.FillRectangle(back, e.Bounds);

        if (MenuItems[e.Index] == "性能及安全")
        {
            using var sep = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(sep, e.Bounds.X + 12, e.Bounds.Y + 1, e.Bounds.Right - 12, e.Bounds.Y + 1);
        }

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
            _status.Text = "正在完整扫描系统状态（含 DISM，可能需要数十秒）…";

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var state = Optimizer.Read(fullScan);
                BeginInvoke(new Action(() =>
                {
                    try { Bind(state, updateCurrentValues: true); }
                    catch (Exception ex) { _status.Text = "读取当前配置失败：" + ex.Message; }
                    finally
                    {
                        UseWaitCursor = false;
                        Cursor = Cursors.Default;
                        Application.UseWaitCursor = false;
                        RefreshEmbeddedPageIfVisible();
                        if (fullScan && !_status.Text.StartsWith("读取当前配置失败", StringComparison.Ordinal))
                            _status.Text = _systemFacts.Summary + " · 状态已刷新。";
                    }
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    _status.Text = "读取当前配置失败：" + ex.Message;
                    UseWaitCursor = false;
                    Cursor = Cursors.Default;
                    Application.UseWaitCursor = false;
                }));
            }
        });
    }

    private void SyncContextMenuRowsFromSystem()
    {
        _takeOwn.Checked = ContextMenuTweaks.IsTakeOwnershipOn();
        _openCmd.Checked = ContextMenuTweaks.IsOpenCmdOn();
        _takeOwn.SyncCurrentValueFromState();
        _openCmd.SyncCurrentValueFromState();
    }

    private void Bind(Optimizer.State s, bool updateCurrentValues = false)
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
        _itemCheckboxes.Checked = s.ShowItemCheckboxes;
        _commonFolders.Checked = s.ShowCommonFolders;
        _noShield.Checked = s.RemoveAdminShield;
        _noSuffix.Checked = s.NoShortcutSuffix;
        _win11Explorer.Checked = s.Win11ExplorerStyle;
        _classicMenu.Checked = s.Win10ClassicContextMenu;
        _tbSearch.Checked = s.TaskbarSearchMode == 0;
        _tbLeft.Checked = s.TaskbarAlignLeft;
        _tbCombine.Checked = s.TaskbarCombineAlways;
        _tbAutohide.Checked = s.TaskbarAutoHide;
        _taskView.Checked = s.ShowTaskViewButton;
        _tbEndTask.Checked = s.TaskbarEndTask;
        _widgets.Checked = s.DisableWidgets;
        _animations.Checked = s.DisableAnimations;
        _transparency.Checked = s.DisableTransparency;
        _tips.Checked = s.DisableTips;
        _autoplay.Checked = s.DisableAutoplay;
        _activityHist.Checked = s.DisableActivityHistory;
        _storageSense.Checked = s.DisableStorageSense;
        _backgroundApps.Checked = s.DisableBackgroundApps;
        _searchHighlights.Checked = s.DisableSearchHighlights;
        _recommended.Checked = s.DisableRecommendedItems;
        _adTracking.Checked = s.DisableAdTracking;
        _searchHistory.Checked = s.DisableSearchHistory;
        _stickyKeys.Checked = s.DisableStickyKeys;
        _pca.Checked = s.DisablePca;
        _wuPause2035.Checked = s.PauseFeatureUpdatesUntil2035;
        _meltdown.Checked = s.DisableMeltdownSpectre;
        _hvci.Checked = s.DisableMemoryIntegrity;
        _wdac.Checked = s.DisableWdac;
        _vbs.Checked = s.DisableVbs;
        _bbr2.Checked = s.EnableTcpBbr2;
        _sysRestore.Checked = s.DisableSystemRestore;
        _ceip.Checked = s.DisableCeip;
        _dps.Checked = s.DisableDiagnosticPolicy;
        _hideOs.Checked = s.HideProtectedOsFiles;
        _iconsOnly.Checked = s.AlwaysShowIconsNeverThumbnails;
        _emptyDrives.Checked = s.ShowEmptyDrives;
        _recentFiles.Checked = s.ShowRecentFiles;
        _frequent.Checked = s.ShowFrequentPlaces;
        _officeCloud.Checked = s.HideOfficeCloudFiles;
        _onedrive.Checked = s.DisableOneDrive;
        _tbChat.Checked = s.HideTaskbarChat;
        _tbCopilot.Checked = s.HideTaskbarCopilot;
        _cloudSearch.Checked = s.DisableCloudSearch;
        _langList.Checked = s.DisableWebsiteLangList;
        _trackApps.Checked = s.DisableAppLaunchTracking;
        _settingsSuggest.Checked = s.DisableSettingsSuggestions;
        _inking.Checked = s.DisableInkingPersonalization;
        _msrt.Checked = s.ExcludeMsrtFromWu;
        _ra.Checked = s.DisableRemoteAssistance;
        _memComp.Checked = s.DisableMemoryCompression;
        _prelaunch.Checked = s.DisableAppPrelaunch;
        _pageCombine.Checked = s.DisablePageCombining;
        _ucpd.Checked = s.DisableUcpdDriver;
        _cortana.Checked = s.DisableCortana;
        _copilotAi.Checked = s.DisableCopilotAi;
        _officeTel.Checked = s.DisableOfficeTelemetry;
        _utc.Checked = s.EnableUtcTime;
        _hpet.Checked = s.DisableHpet;
        _loginVerbose.Checked = s.EnableLoginVerbose;
        _netThrottle.Checked = s.DisableNetworkThrottling;
        _gameDvr.Checked = s.DisableGameDvr;
        _location.Checked = s.DisableLocationTracking;
        _consumer.Checked = s.DisableConsumerFeatures;
        _edgePre.Checked = s.DisableEdgePreload;
        _teredo.Checked = s.DisableTeredo;
        _clipCloud.Checked = s.DisableClipboardCloud;
        _ntfsStamp.Checked = s.DisableNtfsLastAccess;
        _xbox.Checked = s.DisableXboxServices;
        _fax.Checked = s.DisableFaxService;
        _f8.Checked = s.EnableF8BootMenu;
        _takeOwn.Checked = s.ContextMenuTakeOwnership;
        _openCmd.Checked = s.ContextMenuOpenCmd;
        _wmpShare.Checked = s.DisableMediaPlayerSharing;
        _insider.Checked = s.DisableInsiderService;
        _storeUpd.Checked = s.DisableStoreAutoUpdate;
        _news.Checked = s.DisableNewsInterests;
        _rdp.Checked = s.EnableRdp;
        _rdpGpu.Checked = s.RdpGpuAccel;
        _rdpFps.Checked = s.RdpHighRefresh;
        _rdpNla.Checked = s.RdpDisableNla;
        _netDiscovery.Checked = s.EnableNetworkDiscovery;
        _smRemoting.Checked = s.DisableSmRemoting;
        _svrMgr.Checked = s.SkipServerManager;
        _wacPrompt.Checked = s.HideServerManagerWacPrompt;
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
        if (updateCurrentValues)
        {
            foreach (var row in AllRows)
                row.SyncCurrentValueFromState();
            // 仅从系统读取时刷新基线；载入预设/导入配置不改基线，以便「应用到系统」能写出差异
            _baselineState = CaptureState();
        }
        ApplySearchFilter();
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
        DisablePca = _pca.Checked,
        PauseFeatureUpdatesUntil2035 = _wuPause2035.Checked,
        DisableMeltdownSpectre = _meltdown.Checked,
        DisableMemoryIntegrity = _hvci.Checked,
        DisableWdac = _wdac.Checked,
        DisableVbs = _vbs.Checked,
        EnableTcpBbr2 = _bbr2.Checked,
        DisableSystemRestore = _sysRestore.Checked,
        DisableCeip = _ceip.Checked,
        DisableDiagnosticPolicy = _dps.Checked,
        HideProtectedOsFiles = _hideOs.Checked,
        AlwaysShowIconsNeverThumbnails = _iconsOnly.Checked,
        ShowEmptyDrives = _emptyDrives.Checked,
        ShowRecentFiles = _recentFiles.Checked,
        ShowFrequentPlaces = _frequent.Checked,
        HideOfficeCloudFiles = _officeCloud.Checked,
        DisableOneDrive = _onedrive.Checked,
        HideTaskbarChat = _tbChat.Checked,
        HideTaskbarCopilot = _tbCopilot.Checked,
        DisableCloudSearch = _cloudSearch.Checked,
        DisableWebsiteLangList = _langList.Checked,
        DisableAppLaunchTracking = _trackApps.Checked,
        DisableSettingsSuggestions = _settingsSuggest.Checked,
        DisableInkingPersonalization = _inking.Checked,
        ExcludeMsrtFromWu = _msrt.Checked,
        DisableRemoteAssistance = _ra.Checked,
        DisableMemoryCompression = _memComp.Checked,
        DisableAppPrelaunch = _prelaunch.Checked,
        DisablePageCombining = _pageCombine.Checked,
        DisableUcpdDriver = _ucpd.Checked,
        DisableCortana = _cortana.Checked,
        DisableCopilotAi = _copilotAi.Checked,
        DisableOfficeTelemetry = _officeTel.Checked,
        EnableUtcTime = _utc.Checked,
        DisableHpet = _hpet.Checked,
        EnableLoginVerbose = _loginVerbose.Checked,
        DisableNetworkThrottling = _netThrottle.Checked,
        DisableGameDvr = _gameDvr.Checked,
        DisableLocationTracking = _location.Checked,
        DisableConsumerFeatures = _consumer.Checked,
        DisableEdgePreload = _edgePre.Checked,
        DisableTeredo = _teredo.Checked,
        DisableClipboardCloud = _clipCloud.Checked,
        DisableNtfsLastAccess = _ntfsStamp.Checked,
        DisableXboxServices = _xbox.Checked,
        DisableFaxService = _fax.Checked,
        EnableF8BootMenu = _f8.Checked,
        ContextMenuTakeOwnership = _takeOwn.Checked,
        ContextMenuOpenCmd = _openCmd.Checked,
        DisableMediaPlayerSharing = _wmpShare.Checked,
        DisableInsiderService = _insider.Checked,
        DisableStoreAutoUpdate = _storeUpd.Checked,
        DisableNewsInterests = _news.Checked,
        TaskbarSearchMode = _tbSearch.Checked ? 0 : 1,
        TaskbarSearchBox = false,
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
        ShowItemCheckboxes = _itemCheckboxes.Checked,
        ShowCommonFolders = _commonFolders.Checked,
        RemoveAdminShield = _noShield.Checked,
        NoShortcutSuffix = _noSuffix.Checked,
        Win11ExplorerStyle = _win11Explorer.Checked,
        Win10ClassicContextMenu = _classicMenu.Checked,
        TaskbarAlignLeft = _tbLeft.Checked,
        TaskbarCombineAlways = _tbCombine.Checked,
        TaskbarAutoHide = _tbAutohide.Checked,
        ShowTaskViewButton = _taskView.Checked,
        TaskbarEndTask = _tbEndTask.Checked,
        DisableWidgets = _widgets.Checked,
        DisableAnimations = _animations.Checked,
        DisableTransparency = _transparency.Checked,
        DisableTips = _tips.Checked,
        DisableAutoplay = _autoplay.Checked,
        DisableActivityHistory = _activityHist.Checked,
        DisableStorageSense = _storageSense.Checked,
        DisableBackgroundApps = _backgroundApps.Checked,
        DisableSearchHighlights = _searchHighlights.Checked,
        DisableRecommendedItems = _recommended.Checked,
        DisableAdTracking = _adTracking.Checked,
        DisableSearchHistory = _searchHistory.Checked,
        DisableStickyKeys = _stickyKeys.Checked,
        EnableRdp = _rdp.Checked,
        RdpGpuAccel = _rdpGpu.Checked,
        RdpHighRefresh = _rdpFps.Checked,
        RdpDisableNla = _rdpNla.Checked,
        EnableNetworkDiscovery = _netDiscovery.Checked,
        DisableSmRemoting = _smRemoting.Checked,
        SkipServerManager = _svrMgr.Checked,
        HideServerManagerWacPrompt = _wacPrompt.Checked,
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

    /// <summary>仅操作左侧分组中可见的项，避免改到即时页专属的「幽灵开关」。</summary>
    private void SetVisibleAll(bool on)
    {
        foreach (var (_, sections) in _groups)
            foreach (var (_, rows) in sections)
                foreach (var row in rows)
                    row.Checked = on;
    }

    private HashSet<SettingRow> VisibleRowSet()
    {
        var set = new HashSet<SettingRow>();
        foreach (var (_, sections) in _groups)
            foreach (var (_, rows) in sections)
                foreach (var row in rows)
                    set.Add(row);
        return set;
    }

    /// <summary>
    /// 应用前：把未出现在分组里的开关从系统重读，防止即时页已改、批量页旧勾选把系统改回去。
    /// </summary>
    private void SyncInvisibleRowsFromSystem()
    {
        Optimizer.State s;
        try { s = Optimizer.Read(fullScan: false); }
        catch { return; }

        var vis = VisibleRowSet();
        void Sync(SettingRow row, bool value)
        {
            if (!vis.Contains(row)) row.Checked = value;
        }

        Sync(_sysMain, s.DisableSysMain);
        Sync(_hibernate, s.DisableHibernate);
        Sync(_fastStartup, s.DisableFastStartup);
        Sync(_memComp, s.DisableMemoryCompression);
        Sync(_prelaunch, s.DisableAppPrelaunch);
        Sync(_pageCombine, s.DisablePageCombining);
        Sync(_ucpd, s.DisableUcpdDriver);
        Sync(_ra, s.DisableRemoteAssistance);
        Sync(_rdp, s.EnableRdp);
        Sync(_fileExt, s.ShowFileExtensions);
        Sync(_hiddenFiles, s.ShowHiddenFiles);
        Sync(_noArrow, s.NoShortcutArrow);
        Sync(_fullPath, s.ExplorerFullPath);
        Sync(_taskbarClock, s.TaskbarClockWeekdaySeconds);
        Sync(_launchThisPc, s.LaunchExplorerThisPc);
        Sync(_itemCheckboxes, s.ShowItemCheckboxes);
        Sync(_commonFolders, s.ShowCommonFolders);
        Sync(_noShield, s.RemoveAdminShield);
        Sync(_noSuffix, s.NoShortcutSuffix);
        Sync(_win11Explorer, s.Win11ExplorerStyle);
        Sync(_classicMenu, s.Win10ClassicContextMenu);
        Sync(_tbSearch, s.TaskbarSearchMode == 0);
        Sync(_tbLeft, s.TaskbarAlignLeft);
        Sync(_tbCombine, s.TaskbarCombineAlways);
        Sync(_tbAutohide, s.TaskbarAutoHide);
        Sync(_taskView, s.ShowTaskViewButton);
        Sync(_widgets, s.DisableWidgets);
        Sync(_hideOs, s.HideProtectedOsFiles);
        Sync(_iconsOnly, s.AlwaysShowIconsNeverThumbnails);
        Sync(_emptyDrives, s.ShowEmptyDrives);
        Sync(_recentFiles, s.ShowRecentFiles);
        Sync(_frequent, s.ShowFrequentPlaces);
        Sync(_officeCloud, s.HideOfficeCloudFiles);
        Sync(_onedrive, s.DisableOneDrive);
        Sync(_tbChat, s.HideTaskbarChat);
        Sync(_tbCopilot, s.HideTaskbarCopilot);
        Sync(_takeOwn, s.ContextMenuTakeOwnership);
        Sync(_openCmd, s.ContextMenuOpenCmd);
    }

    private void SetRowsChecked(IEnumerable<SettingRow> rows, bool on)
    {
        foreach (var row in rows) row.Checked = on;
    }

    private void RestoreDefaults()
    {
        var answer = MessageBox.Show(
            "将把全部设置项恢复为 Windows Server 出厂默认值（「系统默认值」列）。\n\n" +
            "所有优化建议开关将关闭并立即写入系统。部分项目需注销或重启后生效。\n\n是否继续？",
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

    private void OnApplyClicked()
    {
        if (!_inBatchMode && _embeddedPage is IEmbeddedSettingsPage page && page.SupportsApplyToSystem)
        {
            page.ApplyToSystem();
            return;
        }

        ApplyRecommended();
    }

    private void ApplyRecommended()
    {
        if (!RunApply("正在写入系统…", "已写入本次改动。仅同步有变化的开关。"))
            return;
        // 改名请从「工具 → 计算机名 / 工作组」单独打开，不再每次追问
    }

    /// <summary>修改计算机名/工作组。optional=true 时提供明显的「跳过」。</summary>
    private void PromptComputerIdentity(bool optional = false)
    {
        try
        {
            var info = ComputerIdentityHelper.Read();
            using var dlg = new ComputerIdentityDialog(info, optional);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var msg = dlg.RestartScheduled
                ? "计算机名/工作组已修改，系统将在 60 秒后重启（命令行执行 shutdown /a 可取消）。"
                : "计算机名/工作组已修改，请自行选择合适时间重启以完全生效。";
            _status.Text = msg;
            MessageBox.Show(this, msg, "修改成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "计算机名", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool RunApply(string working, string success)
    {
        _apply.Enabled = false;
        _restore.Enabled = false;
        UseWaitCursor = false;
        Cursor = Cursors.Default;
        Application.UseWaitCursor = false;
        _status.Text = working;
        Application.DoEvents();
        var ok = false;
        try
        {
            if (!EnsureAutologonReady())
            {
                _status.Text = "已取消：启用自动登录需先配置账户。";
                return false;
            }

            SyncInvisibleRowsFromSystem();
            ApplyLog.BeginBatch(working);
            var target = CaptureState();
            var errors = Optimizer.Apply(target, _baselineState);
            ApplyLog.WriteApply(working, errors);

            if (Optimizer.LastApplyActionCount == 0 && errors.Count == 0)
            {
                _status.Text = "没有需要写入的更改（开关相对当前系统未改动）。";
                ok = true;
                return true;
            }

            LoadState(fullScan: true);
            if (!_autologon.Checked) _autologonSettings = null;
            RefreshAutologonDisplay();
            var changed = ApplyLog.LastBatchRealChangeCount;
            var attempted = Optimizer.LastApplyActionCount;
            _status.Text = errors.Count == 0
                ? $"{success} 本次写入 {attempted} 项（实际变更 {changed} 条）→ 帮助「打开变更日志」。部分项需注销/重启。"
                : "部分失败：\r\n" + string.Join("\r\n", errors) +
                  $"\r\n（本次尝试 {attempted} 项，已写入变更 {changed} 条，见帮助 → 打开变更日志）";
            ok = true;
        }
        catch (Exception ex)
        {
            _status.Text = "操作失败：" + ex.Message;
            ok = false;
        }
        finally
        {
            UseWaitCursor = false;
            Cursor = Cursors.Default;
            Application.UseWaitCursor = false;
            UpdateBottomActionEnablement();
        }

        return ok;
    }

    private void ShowAboutDialog()
    {
        MessageBox.Show(
            $"{AppBrand.ProductName} v{AppBrand.VersionText}\r\n" +
            "用于 Windows Server 2022/2025 个人桌面。\r\n\r\n" +
            "系统：" + _systemFacts.Summary + "\r\n" +
            "计算机：" + ComputerIdentityHelper.Read().Summary + "\r\n" +
            "管理员：" + (AdminHelper.IsRunningAsAdministrator() ? "是" : "否") + "\r\n" +
            "操作日志：" + ApplyLog.LogFilePath + "\r\n" +
            "变更日志：" + ApplyLog.ChangeLogFilePath + "\r\n\r\n" +
            "配置 JSON 可导入导出。\r\n" +
            $"CLI：{AppBrand.ExeFileName} --apply-preset server-desktop",
            AppBrand.AboutDialogTitle,
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
        private readonly Label _level;
        private readonly Label _note;
        private readonly Label _system;
        private readonly Label _current;
        private Panel? _wrap;
        private Color _normalBg;

        public string ItemText { get; }
        public SettingHelpInfo Help { get; }
        public Action<SettingRow>? OnCheckedChanged { get; set; }

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
                AutoEllipsis = true,
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
            _level = new Label
            {
                Text = RecommendLevelUi.Icon(help.Recommend),
                AutoSize = false,
                ForeColor = RecommendLevelUi.StarOn,
                Font = new Font("Segoe UI Symbol", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            // 实心星金色、空心星灰色：自绘，保证五星对比一眼可读
            _level.Paint += DrawRecommendStars;
            _note = new Label
            {
                Text = help.ListNote,
                AutoSize = false,
                ForeColor = AppTheme.TextMute,
                Font = new Font("Microsoft YaHei UI", 8.25F),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Cursor = Cursors.Hand,
            };
            _toggle = new ToggleSwitch();
            _toggle.CheckedChanged += (_, _) => OnCheckedChanged?.Invoke(this);
            _system = new Label
            {
                Text = systemDefault,
                AutoSize = false,
                ForeColor = AppTheme.TextMute,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _current = new Label
            {
                Text = "—",
                AutoSize = false,
                ForeColor = AppTheme.TextMain,
                Font = new Font("Microsoft YaHei UI", 9F),
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

        /// <summary>
        /// 根据开关是否已是优化建议，刷新「系统当前值」列。
        /// Checked=true 表示系统当前已是优化建议状态。
        /// </summary>
        public void SyncCurrentValueFromState()
        {
            _current.Text = FormatCurrentValue(Checked, _system.Text);
            _current.ForeColor = Checked ? AppTheme.PrimaryDark : AppTheme.TextMute;
        }

        private static string FormatCurrentValue(bool matchesRecommended, string systemDefault)
        {
            if (!matchesRecommended)
                return string.IsNullOrWhiteSpace(systemDefault) ? "系统默认值" : systemDefault;

            if (string.Equals(systemDefault, "开启", StringComparison.Ordinal))
                return "关闭";
            if (string.Equals(systemDefault, "关闭", StringComparison.Ordinal))
                return "开启";
            return "已是优化建议";
        }

        public bool MatchesFilter(string query, SystemFacts facts, bool hideIncompatibleDesktop)
        {
            if (hideIncompatibleDesktop && !facts.HasDesktopExperience && Help.Scope.RequiresDesktopExperience)
                return false;
            if (string.IsNullOrWhiteSpace(query)) return true;
            var q = query.Trim();
            return ItemText.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || Help.Summary.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || Help.ListNote.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || Help.UiPlace.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || Help.Scope.FormatBadges().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || RecommendLevelUi.Title(Help.Recommend).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || _system.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || _current.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool MatchesCategory(RowCategoryFilter category) =>
            category switch
            {
                RowCategoryFilter.ServerRecommended => Help.Scope.ServerOnly,
                RowCategoryFilter.OptRecommended => !Help.Scope.ServerOnly,
                RowCategoryFilter.Optimized => Checked,
                RowCategoryFilter.NotOptimized => !Checked,
                _ => true,
            };

        public void SetVisible(bool visible)
        {
            if (_wrap is not null) _wrap.Visible = visible;
        }

        public void SetLocationY(int y)
        {
            if (_wrap is not null) _wrap.Top = y;
        }

        /// <summary>窗口变宽/变窄时同步行宽与说明列，避免说明被父级裁切。</summary>
        public void ApplyLayoutWidth(int width)
        {
            if (_wrap is null) return;
            width = Math.Max(SettingListLayout.NoteX + 80, width);
            if (_wrap.Width != width)
                _wrap.Width = width;
            var noteW = SettingListLayout.NoteWidthFor(width);
            if (_note.Width != noteW || _note.Left != SettingListLayout.NoteX)
                _note.SetBounds(SettingListLayout.NoteX, 0, noteW, _wrap.Height);
            if (_level.Left != SettingListLayout.LevelX || _level.Width != SettingListLayout.LevelW)
                _level.SetBounds(SettingListLayout.LevelX, 0, SettingListLayout.LevelW, _wrap.Height);
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
            var toggleX = SettingListLayout.ToggleX;
            var systemX = SettingListLayout.SystemX;
            var currentX = SettingListLayout.CurrentX;
            var levelX = SettingListLayout.LevelX;
            var noteX = SettingListLayout.NoteX;
            var itemX = SettingListLayout.ItemX;

            _normalBg = bg;
            var wrap = new BufferedPanel
            {
                Location = new Point(0, y),
                Size = new Size(width, h),
                BackColor = bg,
            };
            _wrap = wrap;
            _info.SetBounds(SettingListLayout.InfoX, (h - 18) / 2, 18, 18);

            var textW = Math.Max(160, toggleX - itemX - SettingListLayout.TextToggleGap);
            var hasScope = Help.Scope.HasBadge;
            if (hasScope)
            {
                _item.SetBounds(itemX, 4, textW, 20);
                _scope.SetBounds(itemX, 24, textW, 16);
            }
            else
            {
                _item.SetBounds(itemX, 0, textW, h);
            }

            _toggle.Size = new Size(SettingListLayout.ToggleW, 26);
            _toggle.Location = new Point(toggleX, (h - _toggle.Height) / 2);
            _system.SetBounds(systemX, 0, SettingListLayout.SystemW, h);
            _current.SetBounds(currentX, 0, SettingListLayout.CurrentW, h);
            _level.SetBounds(levelX, 0, SettingListLayout.LevelW, h);
            _level.Text = ""; // 由 Paint 画五星，实心金 / 空心灰
            var noteW = SettingListLayout.NoteWidthFor(width);
            _note.SetBounds(noteX, 0, noteW, h);
            _note.Text = Help.ListNote;

            var tip = Help.Summary;
            if (hasScope) tip += "\r\n[" + Help.Scope.FormatBadges() + "]";
            toolTip.SetToolTip(_item, tip);
            toolTip.SetToolTip(_info, "点击查看详细说明\r\n" + tip);
            toolTip.SetToolTip(_level, RecommendLevelUi.Tip(Help.Recommend));
            toolTip.SetToolTip(_note,
                (Help.WhenHint.Length > 0 ? "建议：" + Help.WhenHint + "\r\n" : "") +
                (Help.UiPlace.Length > 0 ? "对应：" + Help.UiPlace : Help.ListNote));
            if (hasScope) toolTip.SetToolTip(_scope, Help.Scope.FormatHelpSection());
            toolTip.SetToolTip(_system, "系统默认值（出厂）");
            toolTip.SetToolTip(_current, "系统当前值（读取自本机）");

            void Select(object? _, EventArgs __) => onSelectHelp(this);
            _item.Click += Select;
            _info.Click += Select;
            _level.Click += Select;
            _note.Click += Select;
            if (hasScope) _scope.Click += Select;

            wrap.Controls.Add(_current);
            wrap.Controls.Add(_system);
            wrap.Controls.Add(_level);
            wrap.Controls.Add(_note);
            wrap.Controls.Add(_info);
            wrap.Controls.Add(_item);
            if (hasScope) wrap.Controls.Add(_scope);
            wrap.Controls.Add(_toggle);
            _toggle.BringToFront();
            wrap.Controls.Add(new Panel
            {
                BackColor = AppTheme.BorderLight,
                Dock = DockStyle.Bottom,
                Height = 1,
            });
            parent.Controls.Add(wrap);
        }

        private void DrawRecommendStars(object? sender, PaintEventArgs e)
        {
            if (sender is not Label label) return;
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 透明标签需铺底，避免残影；并保证五星完整落在列宽内
            var bg = label.BackColor.A == 255
                ? label.BackColor
                : (label.Parent?.BackColor ?? AppTheme.SurfaceCard);
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, label.ClientRectangle);

            var on = RecommendLevelUi.StarsOn(Help.Recommend);
            var step = RecommendLevelUi.StarStep;
            var totalW = RecommendLevelUi.StarsBlockWidth;
            using var font = new Font("Segoe UI Symbol", RecommendLevelUi.StarFontSize, FontStyle.Regular);
            var x0 = Math.Max(2, (label.ClientSize.Width - totalW) / 2);
            var y0 = Math.Max(0, (label.ClientSize.Height - font.Height) / 2 - 1);

            for (var i = 0; i < 5; i++)
            {
                var filled = i < on;
                using var brush = new SolidBrush(filled ? RecommendLevelUi.StarOn : RecommendLevelUi.StarOff);
                // 统一用 ★，靠颜色区分亮/暗，间隔固定且紧凑
                g.DrawString("★", font, brush, x0 + i * step, y0);
            }
        }
    }
}
