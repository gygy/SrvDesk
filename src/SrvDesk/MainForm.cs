using System.Diagnostics;

namespace SrvDesk;

internal sealed class MainForm : Form
{
    private readonly SettingRow _cpu = Choice(AppLang.L("CPU 资源分配", "CPU scheduling"), AppLang.L("后台服务优先", "Background first"), SettingCatalog.CpuProgramPriority,
        [AppLang.L("后台服务优先", "Background first"), AppLang.L("程序优先", "Programs first")], optimizedIndex: 1);
    private readonly SettingRow _dep = Row(AppLang.L("数据执行保护 DEP（T）", "DEP (OptOut)"), AppLang.L("按系统策略", "System policy"), SettingCatalog.Dep);
    private readonly SettingRow _uac = Choice(AppLang.L("UAC 设置", "UAC settings"), AppLang.L("默认通知", "Default notify"), SettingCatalog.DisableUac,
        [AppLang.L("默认通知", "Default notify"), AppLang.L("从不通知", "Never notify")], optimizedIndex: 1);
    private readonly SettingRow _ie = Row(AppLang.L("关闭 IE 增强安全配置", "Disable IE ESC"), AppLang.L("开启", "On"), SettingCatalog.DisableIeEsc);
    private readonly SettingRow _highPerf = Choice(AppLang.L("电源计划", "Power plan"), AppLang.L("平衡", "Balanced"), SettingCatalog.HighPerfPower,
        [AppLang.L("平衡", "Balanced"), AppLang.L("高性能", "High performance")], optimizedIndex: 1);
    private readonly SettingRow _telemetry = Row(AppLang.L("关闭遥测与 DiagTrack", "Disable telemetry & DiagTrack"), AppLang.L("开启", "On"), SettingCatalog.DisableTelemetry);
    private readonly SettingRow _noUpdateReboot = Choice(AppLang.L("更新后重启策略", "Post-update reboot"), AppLang.L("允许重启", "Allow reboot"), SettingCatalog.NoUpdateReboot,
        [AppLang.L("允许重启", "Allow reboot"), AppLang.L("不自动重启", "No auto-reboot")], optimizedIndex: 1);
    private readonly SettingRow _deliveryOpt = Row(AppLang.L("关闭更新传递优化（P2P）", "Disable Delivery Optimization"), AppLang.L("开启", "On"), SettingCatalog.DisableDeliveryOpt);
    private readonly SettingRow _wuNotify = Choice(AppLang.L("Windows 更新下载方式", "Windows Update download"), AppLang.L("自动安装", "Auto install"), SettingCatalog.WuNotifyOnly,
        [AppLang.L("自动安装", "Auto install"), AppLang.L("仅通知下载", "Notify only")], optimizedIndex: 1);
    private readonly SettingRow _sysMain = Row(AppLang.L("禁用 SysMain 超级预读", "Disable SysMain"), AppLang.L("自动", "Automatic"), SettingCatalog.DisableSysMain);
    private readonly SettingRow _visualPerf = Choice(AppLang.L("视觉效果", "Visual effects"), AppLang.L("系统自选", "Let Windows decide"), SettingCatalog.VisualBestPerf,
        [AppLang.L("系统自选", "Let Windows decide"), AppLang.L("最佳性能", "Best performance")], optimizedIndex: 1);
    private readonly SettingRow _powerThrottle = Row(AppLang.L("关闭 CPU 电源节流", "Disable CPU power throttling"), AppLang.L("开启", "On"), SettingCatalog.PowerThrottlingOff);
    private readonly SettingRow _boostMode = Row(AppLang.L("显示处理器性能提升模式", "Show processor boost mode"), AppLang.L("隐藏", "Hidden"), SettingCatalog.ShowProcessorBoostMode);
    private readonly SettingRow _hibernate = Row(AppLang.L("关闭休眠释放磁盘空间", "Disable hibernation"), AppLang.L("开启", "On"), SettingCatalog.DisableHibernate);
    private readonly SettingRow _neverSleep = Row(AppLang.L("关闭关屏与睡眠超时", "Never sleep / screen off"), AppLang.L("开启", "On"), SettingCatalog.NeverSleepOrScreenOff);
    private readonly SettingRow _diskPerf = Row(AppLang.L("任务管理器显示硬盘", "Show Disk in Task Manager"), AppLang.L("开启", "On"), SettingCatalog.EnableDiskPerfCounters);
    private readonly SettingRow _tcp = Row(AppLang.L("TCP 参数优化（对齐 Win10）", "TCP tweak (Win10-like)"), AppLang.L("默认", "Default"), SettingCatalog.TcpOptimized);
    private readonly SettingRow _qosSpeed = Row(AppLang.L("QoS 网速优化（零保留+入站TCP级别3）", "QoS speed (0 reserved + inbound L3)"), AppLang.L("系统默认", "System default"), SettingCatalog.QosSpeedOptimize);
    private readonly SettingRow _errorReport = Row(AppLang.L("关闭 Windows 错误报告", "Disable Windows Error Reporting"), AppLang.L("开启", "On"), SettingCatalog.DisableErrorReport);
    private readonly SettingRow _longPaths = Row(AppLang.L("启用 NTFS 长路径支持", "Enable NTFS long paths"), AppLang.L("关闭", "Off"), SettingCatalog.LongPathsEnabled);
    private readonly SettingRow _fastStartup = Row(AppLang.L("关闭快速启动（稳定双系统）", "Disable Fast Startup"), AppLang.L("开启", "On"), SettingCatalog.DisableFastStartup);
    private readonly SettingRow _autoMaint = Row(AppLang.L("禁用自动维护计划", "Disable automatic maintenance"), AppLang.L("开启", "On"), SettingCatalog.DisableAutoMaintenance);
    private readonly SettingRow _noDriverWu = Row(AppLang.L("Windows 更新不含驱动", "Exclude drivers from WU"), AppLang.L("含驱动", "Include drivers"), SettingCatalog.ExcludeDriverUpdates);
    private readonly SettingRow _smb1 = Row(AppLang.L("禁用 SMB 1.0 协议", "Disable SMB 1.0"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableSmb1);
    private readonly SettingRow _remoteReg = Row(AppLang.L("禁用 Remote Registry 服务", "Disable Remote Registry"), AppLang.L("手动", "Manual"), SettingCatalog.DisableRemoteRegistry);
    private readonly SettingRow _spooler = Row(AppLang.L("禁用打印后台处理（无打印机）", "Disable Print Spooler"), AppLang.L("自动", "Automatic"), SettingCatalog.DisablePrintSpooler);
    private readonly SettingRow _largeCache = Row(AppLang.L("大系统缓存与 NTFS 缓冲优化", "Large system cache + NTFS"), AppLang.L("默认", "Default"), SettingCatalog.LargeSystemCacheOptimize);
    private readonly SettingRow _reservedStorage = Row(AppLang.L("关闭系统保留存储", "Disable reserved storage"), AppLang.L("开启", "On"), SettingCatalog.DisableReservedStorage);
    private readonly SettingRow _srvSplit = Row(AppLang.L("关闭 LanmanServer 服务拆分", "Disable LanmanServer split"), AppLang.L("默认", "Default"), SettingCatalog.DisableSrvSplit);
    private readonly SettingRow _gpuSched = Row(AppLang.L("启用 GPU 硬件加速计划", "Enable GPU HW scheduling"), AppLang.L("关闭", "Off"), SettingCatalog.EnableGpuHwScheduling);
    private readonly SettingRow _pca = Row(AppLang.L("禁用程序兼容性助手 PCA", "Disable PCA"), AppLang.L("开启", "On"), SettingCatalog.DisablePca);
    private readonly SettingRow _wuPause2035 = Row(AppLang.L("暂停功能更新至 2035", "Pause feature updates to 2035"), AppLang.L("不暂停", "Not paused"), SettingCatalog.PauseFeatureUpdatesUntil2035);
    private readonly SettingRow _wuPauseUx = Row(AppLang.L("延迟 Windows 更新至 2099", "Defer Windows Update to 2099"), AppLang.L("不延迟", "Not delayed"), SettingCatalog.PauseWindowsUpdatesUx);
    private readonly SettingRow _meltdown = Row(AppLang.L("关闭 Meltdown/Spectre 缓解", "Disable Meltdown/Spectre mitigations"), AppLang.L("系统默认", "System default"), SettingCatalog.DisableMeltdownSpectre);
    private readonly SettingRow _hvci = Row(AppLang.L("关闭内存完整性 HVCI", "Disable memory integrity (HVCI)"), AppLang.L("由系统决定", "System decides"), SettingCatalog.DisableMemoryIntegrity);
    private readonly SettingRow _wdac = Row(AppLang.L("关闭 WDAC 应用控制", "Disable WDAC"), AppLang.L("系统默认", "System default"), SettingCatalog.DisableWdac);
    private readonly SettingRow _vbs = Row(AppLang.L("强制关闭 VBS 虚拟化安全", "Force-disable VBS"), AppLang.L("由系统决定", "System decides"), SettingCatalog.DisableVbs);
    private readonly SettingRow _bbr2 = Row(AppLang.L("TCP 拥塞控制开启 BBR2", "Enable TCP BBR2"), AppLang.L("CUBIC 默认", "CUBIC default"), SettingCatalog.EnableTcpBbr2);
    private readonly SettingRow _ctcp = Row(AppLang.L("TCP 拥塞控制改用 CTCP", "Enable TCP CTCP"), AppLang.L("CUBIC 默认", "CUBIC default"), SettingCatalog.EnableTcpCtcp);
    private readonly SettingRow _sysRestore = Row(AppLang.L("禁用系统还原", "Disable System Restore"), AppLang.L("启用", "Enabled"), SettingCatalog.DisableSystemRestore);
    private readonly SettingRow _ceip = Row(AppLang.L("关闭客户体验改善计划", "Disable CEIP"), AppLang.L("启用", "Enabled"), SettingCatalog.DisableCeip);
    private readonly SettingRow _dps = Row(AppLang.L("禁用诊断策略服务 DPS", "Disable DPS"), AppLang.L("自动", "Automatic"), SettingCatalog.DisableDiagnosticPolicy);
    private readonly SettingRow _hideOs = Row(AppLang.L("隐藏受保护的系统文件", "Hide protected OS files"), AppLang.L("显示", "Shown"), SettingCatalog.HideProtectedOsFiles);
    private readonly SettingRow _iconsOnly = Row(AppLang.L("始终显示图标从不缩略图", "Icons only, never thumbnails"), AppLang.L("允许缩略图", "Allow thumbnails"), SettingCatalog.AlwaysShowIconsNeverThumbnails);
    private readonly SettingRow _emptyDrives = Row(AppLang.L("显示空驱动器", "Show empty drives"), AppLang.L("隐藏", "Hidden"), SettingCatalog.ShowEmptyDrives);
    private readonly SettingRow _recentFiles = Row(AppLang.L("开始屏幕显示最近文件", "Show recent files"), AppLang.L("显示", "Shown"), SettingCatalog.ShowRecentFiles);
    private readonly SettingRow _frequent = Row(AppLang.L("显示快速访问常用文件夹", "Show frequent folders"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowFrequentPlaces);
    private readonly SettingRow _officeCloud = Row(AppLang.L("隐藏 office.com 云文件", "Hide office.com cloud files"), AppLang.L("显示", "Shown"), SettingCatalog.HideOfficeCloudFiles);
    private readonly SettingRow _onedrive = Row(AppLang.L("禁止 OneDrive 同步", "Disable OneDrive sync"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableOneDrive);
    private readonly SettingRow _tbChat = Row(AppLang.L("隐藏任务栏聊天", "Hide taskbar Chat"), AppLang.L("显示", "Shown"), SettingCatalog.HideTaskbarChat);
    private readonly SettingRow _tbCopilot = Row(AppLang.L("隐藏任务栏 Copilot", "Hide taskbar Copilot"), AppLang.L("显示", "Shown"), SettingCatalog.HideTaskbarCopilot);
    private readonly SettingRow _notepadWrap = Row(AppLang.L("记事本默认自动换行", "Notepad word wrap"), AppLang.L("不换行", "No wrap"), SettingCatalog.NotepadWordWrap);
    private readonly SettingRow _notepadStatus = Row(AppLang.L("记事本显示状态栏", "Notepad status bar"), AppLang.L("不显示", "Hidden"), SettingCatalog.NotepadStatusBar);
    private readonly SettingRow _cloudSearch = Row(AppLang.L("禁止搜索云内容", "Disable cloud search"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableCloudSearch);
    private readonly SettingRow _langList = Row(AppLang.L("禁止网站读取语言列表", "Block sites reading language list"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableWebsiteLangList);
    private readonly SettingRow _trackApps = Row(AppLang.L("关闭应用启动跟踪", "Disable app launch tracking"), AppLang.L("开启", "On"), SettingCatalog.DisableAppLaunchTracking);
    private readonly SettingRow _settingsSuggest = Row(AppLang.L("关闭设置应用建议内容", "Disable Settings suggestions"), AppLang.L("开启", "On"), SettingCatalog.DisableSettingsSuggestions);
    private readonly SettingRow _inking = Row(AppLang.L("关闭墨迹与键入个性化", "Disable inking personalization"), AppLang.L("开启", "On"), SettingCatalog.DisableInkingPersonalization);
    private readonly SettingRow _msPinyinEn = Row(AppLang.L("微软拼音默认英文", "MS Pinyin default English"), AppLang.L("默认中文", "Chinese default"), SettingCatalog.MsPinyinDefaultEnglish);
    private readonly SettingRow _msPinyinCloud = Row(AppLang.L("关闭微软拼音云候选与输入见解", "Disable MS Pinyin cloud/insights"), AppLang.L("开启", "On"), SettingCatalog.DisableMsPinyinCloudAndInsights);
    private readonly SettingRow _msPinyinBar = Row(AppLang.L("关闭拼音工具条与帮助按钮", "Hide Pinyin toolbar/help"), AppLang.L("显示", "Shown"), SettingCatalog.DisableMsPinyinToolbar);
    private readonly SettingRow _msrt = Row(AppLang.L("更新不含恶意软件删除工具", "Exclude MSRT from updates"), AppLang.L("包含", "Included"), SettingCatalog.ExcludeMsrtFromWu);
    private readonly SettingRow _ra = Row(AppLang.L("禁用远程协助", "Disable Remote Assistance"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableRemoteAssistance);
    private readonly SettingRow _memComp = Row(AppLang.L("禁用内存压缩", "Disable memory compression"), AppLang.L("启用", "Enabled"), SettingCatalog.DisableMemoryCompression);
    private readonly SettingRow _prelaunch = Row(AppLang.L("禁用应用预启动", "Disable app prelaunch"), AppLang.L("启用", "Enabled"), SettingCatalog.DisableAppPrelaunch);
    private readonly SettingRow _pageCombine = Row(AppLang.L("禁用内存页面合并", "Disable page combining"), AppLang.L("启用", "Enabled"), SettingCatalog.DisablePageCombining);
    private readonly SettingRow _ucpd = Row(AppLang.L("禁用微软 UCPD 驱动", "Disable UCPD driver"), AppLang.L("启用", "Enabled"), SettingCatalog.DisableUcpdDriver);
    private readonly SettingRow _cortana = Row(AppLang.L("关闭 Cortana", "Disable Cortana"), AppLang.L("开启", "On"), SettingCatalog.DisableCortana);
    private readonly SettingRow _copilotAi = Row(AppLang.L("关闭 Copilot（系统+Edge）", "Disable Copilot (OS + Edge)"), AppLang.L("开启", "On"), SettingCatalog.DisableCopilotAi);
    private readonly SettingRow _officeTel = Row(AppLang.L("关闭 Office 遥测", "Disable Office telemetry"), AppLang.L("开启", "On"), SettingCatalog.DisableOfficeTelemetry);
    private readonly SettingRow _utc = Row(AppLang.L("硬件时钟使用 UTC（双系统）", "Hardware clock UTC (dual-boot)"), AppLang.L("本地时间", "Local time"), SettingCatalog.EnableUtcTime);
    private readonly SettingRow _hpet = Row(AppLang.L("关闭 HPET 高精度计时器", "Disable HPET"), AppLang.L("开启", "On"), SettingCatalog.DisableHpet);
    private readonly SettingRow _loginVerbose = Row(AppLang.L("登录显示详细状态", "Verbose login status"), AppLang.L("简洁", "Brief"), SettingCatalog.EnableLoginVerbose);
    private readonly SettingRow _netThrottle = Row(AppLang.L("关闭多媒体网络节流", "Disable multimedia network throttle"), AppLang.L("开启", "On"), SettingCatalog.DisableNetworkThrottling);
    private readonly SettingRow _mmcss = Row(AppLang.L("优化多媒体调度 (MMCSS)", "Optimize MMCSS"), AppLang.L("默认", "Default"), SettingCatalog.OptimizeMultimediaScheduler);
    private readonly SettingRow _keyboardLatency = Row(AppLang.L("优化键盘重复延迟", "Optimize keyboard repeat delay"), AppLang.L("默认", "Default"), SettingCatalog.OptimizeKeyboardLatency);
    private readonly SettingRow _webDavLimit = Row(AppLang.L("放开 WebDAV 文件大小限制", "Lift WebDAV size limit"), AppLang.L("默认约 50MB", "~50 MB default"), SettingCatalog.LiftWebDavFileSizeLimit);
    private readonly SettingRow _gameDvr = Row(AppLang.L("关闭游戏栏 / Game DVR", "Disable Game Bar / Game DVR"), AppLang.L("开启", "On"), SettingCatalog.DisableGameDvr);
    private readonly SettingRow _location = Row(AppLang.L("禁止定位服务", "Disable location"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableLocationTracking);
    private readonly SettingRow _consumer = Row(AppLang.L("关闭消费者体验推送", "Disable consumer features"), AppLang.L("开启", "On"), SettingCatalog.DisableConsumerFeatures);
    private readonly SettingRow _edgePre = Row(AppLang.L("禁止 Edge 预启动与后台", "Disable Edge preload"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableEdgePreload);
    private readonly SettingRow _teredo = Row(AppLang.L("禁用 Teredo 隧道", "Disable Teredo"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableTeredo);
    private readonly SettingRow _clipCloud = Row(AppLang.L("关闭剪贴板云同步", "Disable clipboard cloud sync"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableClipboardCloud);
    private readonly SettingRow _ntfsStamp = Row(AppLang.L("关闭 NTFS 最后访问时间戳", "Disable NTFS last-access stamp"), AppLang.L("记录", "Recorded"), SettingCatalog.DisableNtfsLastAccess);
    private readonly SettingRow _xbox = Row(AppLang.L("禁用 Xbox Live 服务", "Disable Xbox Live services"), AppLang.L("手动", "Manual"), SettingCatalog.DisableXboxServices);
    private readonly SettingRow _fax = Row(AppLang.L("禁用传真服务", "Disable Fax service"), AppLang.L("手动", "Manual"), SettingCatalog.DisableFaxService);
    private readonly SettingRow _f8 = Row(AppLang.L("启用 F8 高级启动菜单", "Enable F8 advanced boot menu"), AppLang.L("标准", "Standard"), SettingCatalog.EnableF8BootMenu);
    private readonly SettingRow _takeOwn = Row(AppLang.L("右键菜单：取得所有权", "Context menu: Take ownership"), AppLang.L("无", "None"), SettingCatalog.ContextMenuTakeOwnership);
    private readonly SettingRow _openCmd = Row(AppLang.L("右键菜单：在此处打开 CMD", "Context menu: Open CMD here"), AppLang.L("无", "None"), SettingCatalog.ContextMenuOpenCmd);
    private readonly SettingRow _copyMoveTo = Row(AppLang.L("右键菜单：复制到 / 移动到", "Context menu: Copy/Move to"), AppLang.L("无", "None"), SettingCatalog.ContextMenuCopyMoveTo);
    private readonly SettingRow _quickOps = Row(AppLang.L("右键菜单：快捷操作组", "Context menu: Quick ops"), AppLang.L("无", "None"), SettingCatalog.ContextMenuQuickOps);
    private readonly SettingRow _wmpShare = Row(AppLang.L("禁用媒体播放器网络共享", "Disable Media Player sharing"), AppLang.L("手动", "Manual"), SettingCatalog.DisableMediaPlayerSharing);
    private readonly SettingRow _insider = Row(AppLang.L("禁用 Windows Insider 服务", "Disable Windows Insider service"), AppLang.L("手动", "Manual"), SettingCatalog.DisableInsiderService);
    private readonly SettingRow _storeUpd = Row(AppLang.L("禁止商店自动更新应用", "Disable Store auto-update"), AppLang.L("自动", "Automatic"), SettingCatalog.DisableStoreAutoUpdate);
    private readonly SettingRow _news = Row(AppLang.L("关闭资讯与兴趣", "Disable News and interests"), AppLang.L("开启", "On"), SettingCatalog.DisableNewsInterests);
    private readonly SettingRow _noBrokenLnk = Row(AppLang.L("禁止跟踪损坏快捷方式", "Disable broken shortcut tracking"), AppLang.L("跟踪", "Track"), SettingCatalog.DisableBrokenShortcutTracking);
    private readonly SettingRow _sepProcess = Row(AppLang.L("单独进程打开文件夹", "Separate process per folder"), AppLang.L("同一进程", "Same process"), SettingCatalog.ExplorerSeparateProcess);
    private readonly SettingRow _autoRestartShell = Row(AppLang.L("资源管理器崩溃自动重启", "Auto-restart Explorer"), AppLang.L("不重启", "No restart"), SettingCatalog.AutoRestartExplorer);
    private readonly SettingRow _hideSpotlight = Row(AppLang.L("隐藏桌面「了解此图片」", "Hide desktop Spotlight tip"), AppLang.L("显示", "Shown"), SettingCatalog.HideDesktopSpotlight);
    private readonly SettingRow _noDupDrives = Row(AppLang.L("去除本地磁盘重复显示", "Hide duplicate drive entries"), AppLang.L("保留", "Keep"), SettingCatalog.HideDuplicateRemovableDrives);
    private readonly SettingRow _noRunMru = Row(AppLang.L("「运行」对话框不显示历史", "Clear Run dialog history"), AppLang.L("保留", "Keep"), SettingCatalog.DisableRunDialogHistory);
    private readonly SettingRow _mergeSvchost = Row(AppLang.L("合并 svchost 进程", "Merge svchost processes"), AppLang.L("默认拆分", "Split (default)"), SettingCatalog.MergeSvchostProcesses);
    private readonly SettingRow _trkWks = Row(AppLang.L("禁用 NTFS 分布式链接跟踪", "Disable distributed link tracking"), AppLang.L("启用", "Enabled"), SettingCatalog.DisableDistributedLinkTracking);
    private readonly SettingRow _noLowDisk = Row(AppLang.L("禁用磁盘空间不足警告", "Disable low disk space warnings"), AppLang.L("提示", "Warn"), SettingCatalog.DisableLowDiskSpaceChecks);
    private readonly SettingRow _usbPowerOff = Row(AppLang.L("弹出 USB 后彻底断电", "Full power-off after USB eject"), AppLang.L("保持供电", "Keep powered"), SettingCatalog.UsbFullPowerOff);
    private readonly SettingRow _autoReboot = Row(AppLang.L("蓝屏时自动重启", "Auto-reboot on BSOD"), AppLang.L("停留蓝屏", "Stay on BSOD"), SettingCatalog.AutoRebootOnCrash);
    private readonly SettingRow _cliTelemetry = Row(AppLang.L("关闭 .NET / PowerShell 遥测", "Disable .NET/PowerShell telemetry"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableDotNetPowerShellTelemetry);
    private readonly SettingRow _diagMinimal = Row(AppLang.L("诊断数据设为最小（官方级别）", "Diagnostic data: Required"), AppLang.L("完整", "Full"), SettingCatalog.DiagnosticDataMinimal);
    private readonly SettingRow _noSigninReopen = Row(AppLang.L("更新后不自动重开应用", "Don't reopen apps after update"), AppLang.L("允许重开", "Allow reopen"), SettingCatalog.DisableSigninReopen);
    private readonly SettingRow _noSilentApps = Row(AppLang.L("禁止静默安装建议应用", "Block silent suggested apps"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableSilentAppInstall);
    private readonly SettingRow _hideHomeGallery = Row(AppLang.L("隐藏资源管理器主页与图库", "Hide Explorer Home & Gallery"), AppLang.L("显示", "Shown"), SettingCatalog.HideExplorerHomeGallery);
    private readonly SettingRow _noSnapAssist = Row(AppLang.L("关闭窗口贴靠建议", "Disable Snap Assist"), AppLang.L("开启", "On"), SettingCatalog.DisableSnapAssist);
    private readonly SettingRow _darkMode = Row(AppLang.L("使用深色模式", "Dark mode"), AppLang.L("浅色", "Light"), SettingCatalog.EnableDarkMode);
    private readonly SettingRow _noBitlockerAuto = Row(AppLang.L("禁止 BitLocker 自动加密", "Disable BitLocker auto-encrypt"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableBitLockerAutoEncrypt);
    private readonly SettingRow _noCompanionApps = Row(AppLang.L("禁止外设配套应用自动安装", "Block companion app installs"), AppLang.L("允许", "Allowed"), SettingCatalog.PreventDeviceCompanionApps);
    private readonly SettingRow _noUpdateAsap = Row(AppLang.L("关闭「尽快获取最新更新」", "Disable 'Get the latest updates'"), AppLang.L("开启", "On"), SettingCatalog.DisableUpdateAsap);
    private readonly SettingRow _hideSettingsHome = Row(AppLang.L("隐藏设置首页与 365 广告", "Hide Settings home & 365 ads"), AppLang.L("显示", "Shown"), SettingCatalog.HideSettingsHomeAds);
    private readonly SettingRow _extraAi = Row(AppLang.L("关闭 Recall / Click to Do / 记事本画图 AI", "Disable Recall / Click to Do / AI"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableWin11ExtraAi);
    private readonly SettingRow _nullSess = Row(AppLang.L("限制匿名访问命名管道与共享", "Restrict null session shares"), AppLang.L("允许", "Allowed"), SettingCatalog.RestrictNullSessionShares);
    private readonly SettingRow _anonEnum = Row(AppLang.L("限制匿名枚举共享", "Restrict anonymous share enum"), AppLang.L("允许", "Allowed"), SettingCatalog.RestrictAnonymousEnum);
    private readonly SettingRow _smbThrottle = Row(AppLang.L("关闭 SMB 带宽节流", "Disable SMB bandwidth throttling"), AppLang.L("节流", "Throttled"), SettingCatalog.DisableSmbBandwidthThrottling);
    private readonly SettingRow _fastShutdown = Row(AppLang.L("缩短关机等待时间", "Faster shutdown"), AppLang.L("默认", "Default"), SettingCatalog.FasterShutdown);
    private readonly SettingRow _startupDelay = Row(AppLang.L("取消开机启动项延迟", "Disable startup app delay"), AppLang.L("延迟", "Delayed"), SettingCatalog.DisableStartupAppDelay);
    private readonly SettingRow _menuDelay = Row(AppLang.L("菜单悬停立即展开", "Instant menu show"), AppLang.L("默认延迟", "Default delay"), SettingCatalog.InstantMenuShow);
    private readonly SettingRow _aeroShake = Row(AppLang.L("禁用 Aero Shake 摇窗最小化", "Disable Aero Shake"), AppLang.L("开启", "On"), SettingCatalog.DisableAeroShake);
    private readonly SettingRow _netLocWizard = Row(AppLang.L("关闭网络位置向导弹窗", "Disable network location wizard"), AppLang.L("弹窗", "Prompt"), SettingCatalog.DisableNetworkLocationWizard);
    private readonly SettingRow _settingSync = Row(AppLang.L("关闭设置同步（Windows 备份）", "Disable Settings Sync"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableSettingSync);
    private readonly SettingRow _finishSetup = Row(AppLang.L("关闭「完成设备设置」建议", "Disable finish-setup suggestions"), AppLang.L("提示", "Prompt"), SettingCatalog.DisableFinishSetupSuggestions);
    private readonly SettingRow _xferDetails = Row(AppLang.L("复制文件默认显示更多详细信息", "Transfer dialog: more details"), AppLang.L("精简", "Simple"), SettingCatalog.ExplorerTransferDetails);
    private readonly SettingRow _telemTasks = Row(AppLang.L("禁用常见遥测计划任务", "Disable telemetry scheduled tasks"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableTelemetryScheduledTasks);
    private readonly SettingRow _alwaysMenu = Row(AppLang.L("始终显示菜单栏", "Always show menu bar"), AppLang.L("按 Alt 才显示", "Show with Alt"), SettingCatalog.AlwaysShowMenus);
    private readonly SettingRow _hideMerge = Row(AppLang.L("隐藏文件夹合并冲突", "Hide folder merge conflicts"), AppLang.L("每次确认", "Ask each time"), SettingCatalog.HideMergeConflicts);
    private readonly SettingRow _compColor = Row(AppLang.L("加密/压缩文件用颜色标识", "Color encrypted/compressed files"), AppLang.L("不着色", "No color"), SettingCatalog.ShowCompColor);
    private readonly SettingRow _infoTip = Row(AppLang.L("显示文件夹弹出说明", "Show folder info tips"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowInfoTip);
    private readonly SettingRow _statusBar = Row(AppLang.L("显示资源管理器状态栏", "Show Explorer status bar"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowStatusBar);
    private readonly SettingRow _noPersistFold = Row(AppLang.L("登录时不还原上次文件夹窗口", "Don't restore folders at logon"), AppLang.L("还原", "Restore"), SettingCatalog.DisablePersistBrowsers);
    private readonly SettingRow _navExpand = Row(AppLang.L("导航窗格展开到当前文件夹", "Expand nav pane to current"), AppLang.L("不展开", "Collapsed"), SettingCatalog.NavPaneExpandCurrent);
    private readonly SettingRow _noShareWiz = Row(AppLang.L("不使用共享向导", "Don't use sharing wizard"), AppLang.L("使用向导", "Use wizard"), SettingCatalog.DisableSharingWizard);
    private readonly SettingRow _driveLetters = Choice(AppLang.L("盘符显示位置", "Drive letter position"), AppLang.L("卷标后面", "After label"), SettingCatalog.ShowDriveLettersMode,
        FolderViewTweaks.DriveLetterLabels, optimizedIndex: 0);
    private readonly SettingRow _folderGroup = Choice(AppLang.L("分组依据", "Group by"), AppLang.L("按修改日期", "By modified date"), SettingCatalog.FolderGroupByMode,
        FolderViewTweaks.GroupByLabels, optimizedIndex: 0);
    private readonly SettingRow _folderSort = Choice(AppLang.L("排序方式", "Sort by"), AppLang.L("日期新到旧", "Newest first"), SettingCatalog.FolderSortByMode,
        FolderViewTweaks.SortByLabels, optimizedIndex: 0);

    private readonly SettingRow _thisPc = Row(AppLang.L("显示桌面「此电脑」图标", "Show This PC on desktop"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowThisPcIcon);
    private readonly SettingRow _launchThisPc = Row(AppLang.L("资源管理器打开到「此电脑」", "Explorer opens to This PC"), AppLang.L("快速访问", "Quick access"), SettingCatalog.LaunchExplorerThisPc);
    private readonly SettingRow _taskbar = Row(AppLang.L("使用小按钮任务栏", "Small taskbar buttons"), AppLang.L("标准大小", "Standard size"), SettingCatalog.SmallTaskbar);
    private readonly SettingRow _confirmDel = Row(AppLang.L("显示删除确认对话框", "Confirm delete dialog"), AppLang.L("不提示", "No prompt"), SettingCatalog.ConfirmDelete);
    private readonly SettingRow _audio = Row(AppLang.L("启动音频服务", "Enable audio service"), AppLang.L("不启动", "Not started"), SettingCatalog.EnableAudio);
    private readonly SettingRow _fileExt = Row(AppLang.L("显示已知文件扩展名", "Show file extensions"), AppLang.L("隐藏", "Hidden"), SettingCatalog.ShowFileExtensions);
    private readonly SettingRow _themes = Row(AppLang.L("启用主题服务（完整桌面外观）", "Enable Themes service"), AppLang.L("手动", "Manual"), SettingCatalog.EnableThemes);
    private readonly SettingRow _search = Row(AppLang.L("启用 Windows 搜索", "Enable Windows Search"), AppLang.L("手动", "Manual"), SettingCatalog.EnableSearch);
    private readonly SettingRow _webSearch = Row(AppLang.L("关闭开始菜单 Bing 网络搜索", "Disable Bing web search"), AppLang.L("开启", "On"), SettingCatalog.DisableWebSearch);
    private readonly SettingRow _feedback = Row(AppLang.L("关闭 Windows 体验反馈提示", "Disable feedback prompts"), AppLang.L("开启", "On"), SettingCatalog.DisableFeedback);
    private readonly SettingRow _noLockScreen = Row(AppLang.L("禁用锁屏界面", "Disable lock screen"), AppLang.L("显示", "Shown"), SettingCatalog.NoLockScreen);
    private readonly SettingRow _hiddenFiles = Row(AppLang.L("显示隐藏文件", "Show hidden files"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowHiddenFiles);
    private readonly SettingRow _noArrow = Row(AppLang.L("隐藏快捷方式小箭头", "Hide shortcut arrows"), AppLang.L("显示", "Shown"), SettingCatalog.NoShortcutArrow);
    private readonly SettingRow _fullPath = Row(AppLang.L("标题栏显示完整路径", "Full path in title bar"), AppLang.L("仅文件夹名", "Folder name only"), SettingCatalog.ExplorerFullPath);
    private readonly SettingRow _allTrayIcons = Row(AppLang.L("任务栏显示全部图标", "Show all tray icons"), AppLang.L("自动隐藏", "Auto-hide"), SettingCatalog.TaskbarAllIcons);
    private readonly SettingRow _taskbarClock = Row(AppLang.L("任务栏时钟显示星期与秒", "Taskbar clock: weekday & seconds"), AppLang.L("无星期/无秒", "No weekday/sec"), SettingCatalog.TaskbarClockWeekdaySeconds);
    private readonly SettingRow _desktopIcons = Row(AppLang.L("显示控制面板与回收站图标", "Show Control Panel & Recycle Bin"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowControlPanelRecycleBin);
    private readonly SettingRow _smartScreen = Row(AppLang.L("关闭 SmartScreen 与打开文件警告", "Disable SmartScreen warnings"), AppLang.L("开启", "On"), SettingCatalog.DisableSmartScreenWarning);
    private readonly SettingRow _classicSearch = Row(AppLang.L("搜索退回传统模式", "Classic file search"), AppLang.L("现代搜索", "Modern search"), SettingCatalog.ClassicFileSearch);
    private readonly SettingRow _searchEngine = Row(AppLang.L("禁用 SearchEngine 功能包", "Disable SearchEngine feature"), AppLang.L("已安装", "Installed"), SettingCatalog.DisableSearchEngineFeature);

    private readonly SettingRow _itemCheckboxes = Row(AppLang.L("显示项目复选框", "Show item checkboxes"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowItemCheckboxes);
    private readonly SettingRow _commonFolders = Row(AppLang.L("显示常用文件夹", "Show common folders"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowCommonFolders);
    private readonly SettingRow _noShield = Row(AppLang.L("去除快捷方式管理员盾牌", "Remove admin shield on shortcuts"), AppLang.L("显示", "Shown"), SettingCatalog.RemoveAdminShield);
    private readonly SettingRow _noSuffix = Row(AppLang.L("快捷方式不加「快捷方式」后缀", "No 'Shortcut' suffix"), AppLang.L("添加", "Add"), SettingCatalog.NoShortcutSuffix);
    private readonly SettingRow _win11Explorer = Row(AppLang.L("Win11 资源管理器布局", "Win11 Explorer layout"), AppLang.L("紧凑", "Compact"), SettingCatalog.Win11ExplorerStyle);
    private readonly SettingRow _classicMenu = Row(AppLang.L("Win10 经典右键菜单", "Win10 classic context menu"), AppLang.L("Win11 现代", "Win11 modern"), SettingCatalog.Win10ClassicContextMenu);
    private readonly SettingRow _tbSearch = Choice(AppLang.L("任务栏搜索", "Taskbar search"), AppLang.L("仅图标", "Icon only"), SettingCatalog.HideTaskbarSearch,
        [AppLang.L("隐藏", "Hidden"), AppLang.L("仅图标", "Icon only"), AppLang.L("搜索框", "Search box")], optimizedIndex: 0);
    private readonly SettingRow _tbLeft = Choice(AppLang.L("任务栏对齐", "Taskbar alignment"), AppLang.L("居中", "Center"), SettingCatalog.TaskbarAlignLeft,
        [AppLang.L("居中", "Center"), AppLang.L("靠左", "Left")], optimizedIndex: 1);
    private readonly SettingRow _tbCombine = Choice(AppLang.L("任务栏按钮合并", "Taskbar button combining"), AppLang.L("从不", "Never"), SettingCatalog.TaskbarCombineAlways,
        [AppLang.L("从不合并", "Never combine"), AppLang.L("始终合并", "Always combine")], optimizedIndex: 1);
    private readonly SettingRow _tbAutohide = Choice(AppLang.L("任务栏显示方式", "Taskbar visibility"), AppLang.L("一直显示", "Always show"), SettingCatalog.TaskbarAutoHide,
        [AppLang.L("一直显示", "Always show"), AppLang.L("自动隐藏", "Auto-hide")], optimizedIndex: 1);
    private readonly SettingRow _taskView = Row(AppLang.L("显示任务视图按钮", "Show Task View button"), AppLang.L("不显示", "Hidden"), SettingCatalog.ShowTaskViewButton);
    private readonly SettingRow _tbEndTask = Row(AppLang.L("任务栏右键结束任务", "Taskbar End task"), AppLang.L("关闭", "Off"), SettingCatalog.TaskbarEndTask);
    private readonly SettingRow _widgets = Row(AppLang.L("关闭任务栏小组件", "Disable Widgets"), AppLang.L("开启", "On"), SettingCatalog.DisableWidgets);

    private readonly SettingRow _animations = Row(AppLang.L("禁用窗口与任务栏动画", "Disable animations"), AppLang.L("开启", "On"), SettingCatalog.DisableAnimations);
    private readonly SettingRow _transparency = Row(AppLang.L("禁用透明效果", "Disable transparency"), AppLang.L("开启", "On"), SettingCatalog.DisableTransparency);
    private readonly SettingRow _tips = Row(AppLang.L("关闭 Windows 提示与建议", "Disable tips & suggestions"), AppLang.L("开启", "On"), SettingCatalog.DisableTips);
    private readonly SettingRow _autoplay = Row(AppLang.L("禁用所有驱动器自动播放", "Disable AutoPlay"), AppLang.L("开启", "On"), SettingCatalog.DisableAutoplay);
    private readonly SettingRow _activityHist = Row(AppLang.L("禁用活动历史记录", "Disable activity history"), AppLang.L("开启", "On"), SettingCatalog.DisableActivityHistory);
    private readonly SettingRow _storageSense = Row(AppLang.L("禁用存储感知", "Disable Storage Sense"), AppLang.L("开启", "On"), SettingCatalog.DisableStorageSense);
    private readonly SettingRow _backgroundApps = Row(AppLang.L("禁止应用在后台运行", "Block background apps"), AppLang.L("允许", "Allowed"), SettingCatalog.DisableBackgroundApps);
    private readonly SettingRow _searchHighlights = Row(AppLang.L("关闭搜索要点/亮点", "Disable search highlights"), AppLang.L("开启", "On"), SettingCatalog.DisableSearchHighlights);
    private readonly SettingRow _recommended = Row(AppLang.L("关闭开始菜单推荐", "Disable Start recommendations"), AppLang.L("开启", "On"), SettingCatalog.DisableRecommendedItems);
    private readonly SettingRow _adTracking = Row(AppLang.L("关闭广告标识符跟踪", "Disable advertising ID"), AppLang.L("开启", "On"), SettingCatalog.DisableAdTracking);
    private readonly SettingRow _searchHistory = Row(AppLang.L("关闭搜索历史记录", "Disable search history"), AppLang.L("开启", "On"), SettingCatalog.DisableSearchHistory);
    private readonly SettingRow _stickyKeys = Row(AppLang.L("禁用辅助功能键盘热键", "Disable sticky keys hotkeys"), AppLang.L("开启", "On"), SettingCatalog.DisableStickyKeys);

    private readonly SettingRow _rdp = Choice(AppLang.L("启用远程桌面（RDP）", "Enable Remote Desktop (RDP)"), AppLang.L("禁用", "Disabled"), SettingCatalog.EnableRdp,
        [AppLang.L("禁用", "Disabled"), AppLang.L("启用", "Enabled")], optimizedIndex: 1);
    private readonly SettingRow _rdpGpu = Choice(AppLang.L("RDP 硬件图形加速", "RDP GPU acceleration"), AppLang.L("关闭", "Off"), SettingCatalog.RdpGpuAccel,
        [AppLang.L("关闭", "Off"), AppLang.L("开启", "On")], optimizedIndex: 1);
    private readonly SettingRow _rdpFps = Choice(AppLang.L("RDP 提高远程帧率", "RDP higher frame rate"), AppLang.L("默认", "Default"), SettingCatalog.RdpHighRefresh,
        [AppLang.L("默认", "Default"), AppLang.L("提高", "Boost")], optimizedIndex: 1);
    private readonly SettingRow _rdpNla = Choice(AppLang.L("RDP 网络级身份验证 NLA", "RDP NLA"), AppLang.L("要求 NLA", "Require NLA"), SettingCatalog.RdpDisableNla,
        [AppLang.L("要求 NLA", "Require NLA"), AppLang.L("关闭 NLA", "Disable NLA")], optimizedIndex: 1);
    private readonly SettingRow _netDiscovery = Row(AppLang.L("启用网络发现与文件共享", "Enable network discovery & sharing"), AppLang.L("关闭", "Off"), SettingCatalog.EnableNetworkDiscovery);
    private readonly SettingRow _smRemoting = Row(AppLang.L("关闭 Server Manager 远程管理", "Disable Server Manager remoting"), AppLang.L("开启", "On"), SettingCatalog.DisableSmRemoting);

    private readonly SettingRow _svrMgr = Row(AppLang.L("登录时不自动启动服务器管理器", "Don't auto-start Server Manager"), AppLang.L("登录时启动", "Start at logon"), SettingCatalog.SkipServerManager);
    private readonly SettingRow _wacPrompt = Row(AppLang.L("不再显示「立即尝试 WAC/Azure Arc」弹窗", "Hide WAC/Azure Arc prompt"), AppLang.L("每次弹出", "Every time"), SettingCatalog.HideServerManagerWacPrompt);
    private readonly SettingRow _azure = Row(AppLang.L("禁止启动 Azure Arc 托盘", "Disable Azure Arc tray"), AppLang.L("允许启动", "Allow start"), SettingCatalog.DisableAzureArc);
    private readonly SettingRow _installer = Row(AppLang.L("Windows Installer 自动启动", "Windows Installer auto-start"), AppLang.L("手动", "Manual"), SettingCatalog.EnableInstaller);
    private readonly SettingRow _wia = Row(AppLang.L("启用 WIA（摄像头/扫描仪）", "Enable WIA"), AppLang.L("手动", "Manual"), SettingCatalog.EnableWia);
    private readonly SettingRow _mediaFeatures = Row(AppLang.L("开启桌面媒体组件（DISM）", "Enable desktop media features"), AppLang.L("未安装", "Not installed"), SettingCatalog.EnableDesktopMediaFeatures);
    private readonly SettingRow _bloatFeatures = Row(AppLang.L("关闭 Server 冗余组件（DISM）", "Disable Server bloat features"), AppLang.L("已安装", "Installed"), SettingCatalog.DisableServerBloatFeatures);

    private readonly SettingRow _pwd = Row(AppLang.L("禁用密码复杂性要求", "Disable password complexity"), AppLang.L("必须符合", "Required"), SettingCatalog.DisablePasswordComplexity);
    private readonly SettingRow _pwdExpire = Row(AppLang.L("密码永不过期", "Password never expires"), AppLang.L("42 天", "42 days"), SettingCatalog.PasswordNeverExpire);
    private readonly SettingRow _shutdownLogon = Row(AppLang.L("允许未登录时关机", "Shutdown without logon"), AppLang.L("不允许", "Not allowed"), SettingCatalog.ShutdownWithoutLogon);
    private readonly SettingRow _shutdownReason = Row(AppLang.L("关闭关机事件跟踪", "Disable shutdown reason UI"), AppLang.L("显示", "Shown"), SettingCatalog.DisableShutdownReason);
    private readonly SettingRow _noCad = Row(AppLang.L("无需 Ctrl+Alt+Del 登录", "No Ctrl+Alt+Del at logon"), AppLang.L("需要按键", "Require keys"), SettingCatalog.DisableCad);
    private readonly SettingRow _autologon = Row(AppLang.L("启用 Windows 自动登录（Autologon）", "Enable Autologon"), AppLang.L("未启用", "Off"), SettingCatalog.EnableAutologon);
    private readonly SettingRow _keyboardFilter = Row(AppLang.L("取消登录粘滞键/筛选键提示", "Disable sticky/filter key prompts"), AppLang.L("显示", "Shown"), SettingCatalog.DisableLoginKeyboardFilters);
    private AutologonSettings? _autologonSettings;

    private readonly HelpDetailPanel _helpDetail = new();
    private readonly SplitContainer _mainSplit = new();
    private readonly AppMenuStrip _appMenu = new();
    private readonly Panel _commandBar = new();
    private readonly Panel _workArea = new();
    private readonly Label _headerSubtitle = new();
    private HeaderResourceMeter? _headerMeter;
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 200 };
    private readonly Panel _contentHost = new BufferedPanel(composited: true);
    private readonly Label _status = new();
    private Optimizer.State? _baselineState;
    /// <summary>用户改过开关/载入预设等，尚未用系统读取覆盖界面。</summary>
    private bool _uiDirty;
    /// <summary>正在把 Optimizer.Read 写回开关，此时不把界面标脏。</summary>
    private bool _binding;
    /// <summary>异步 LoadState 代数，避免慢扫描覆盖更新的结果。</summary>
    private int _loadEpoch;
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
    private readonly ComboBox _presetCombo = new();
    private readonly NoScrollFlowLayoutPanel _commandFlow = new();
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
        AppLang.L("登录启动项", "Startup apps"),
        AppLang.L("服务优化", "Service optimize"),
        AppLang.L("DNS 设置", "DNS settings"),
        AppLang.L("自定义配置", "Custom config"),
    ];

    private static readonly string[] MenuItems =
    [
        AppLang.L("Server专属", "Server only"),
        AppLang.L("账户策略", "Account policy"),
        AppLang.L("资源管理器", "File Explorer"),
        AppLang.L("桌面外观", "Desktop look"),
        AppLang.L("远程与网络", "Remote & network"),
        AppLang.L("隐私与体验", "Privacy & UX"),
        AppLang.L("性能及安全", "Performance & security"),
        AppLang.L("登录启动项", "Startup apps"),
        AppLang.L("电源与后台", "Power & background"),
        AppLang.L("服务优化", "Service optimize"),
        AppLang.L("DNS 设置", "DNS settings"),
        AppLang.L("自定义配置", "Custom config"),
    ];

    private SettingRow[] AllRows =>
    [
        _cpu, _dep, _uac, _ie, _highPerf, _telemetry, _noUpdateReboot, _deliveryOpt, _wuNotify,
        _sysMain, _visualPerf, _powerThrottle, _boostMode, _hibernate, _neverSleep, _diskPerf, _tcp, _qosSpeed, _errorReport,
        _longPaths, _fastStartup, _autoMaint, _noDriverWu, _smb1, _remoteReg, _spooler,
        _largeCache, _reservedStorage, _srvSplit, _gpuSched, _pca, _wuPause2035, _wuPauseUx,
        _meltdown, _hvci, _wdac, _vbs, _bbr2, _ctcp, _sysRestore, _ceip, _dps,
        _memComp, _prelaunch, _pageCombine, _ucpd,
        _netThrottle, _mmcss, _keyboardLatency, _webDavLimit, _hpet, _ntfsStamp, _utc, _loginVerbose, _f8, _xbox, _fax, _wmpShare,
        _thisPc, _launchThisPc, _taskbar, _confirmDel, _audio, _fileExt, _themes, _search,
        _webSearch, _feedback, _noLockScreen, _hiddenFiles, _noArrow, _fullPath, _allTrayIcons,
        _taskbarClock, _desktopIcons, _smartScreen, _classicSearch, _searchEngine,
        _itemCheckboxes, _commonFolders, _noShield, _noSuffix, _win11Explorer, _classicMenu,
        _tbSearch, _tbLeft, _tbCombine, _tbAutohide, _taskView, _tbEndTask, _widgets,
        _hideOs, _iconsOnly, _emptyDrives, _recentFiles, _frequent, _officeCloud, _onedrive, _tbChat, _tbCopilot,
        _notepadWrap, _notepadStatus, _takeOwn, _openCmd, _copyMoveTo, _quickOps, _news,
        _noBrokenLnk, _sepProcess, _autoRestartShell, _hideSpotlight, _noDupDrives, _noRunMru,
        _mergeSvchost, _trkWks, _noLowDisk, _usbPowerOff, _autoReboot, _cliTelemetry,
        _diagMinimal, _noSigninReopen, _noSilentApps, _hideHomeGallery, _noSnapAssist, _darkMode,
        _noBitlockerAuto, _noCompanionApps, _noUpdateAsap, _hideSettingsHome, _extraAi,
        _nullSess, _anonEnum, _smbThrottle, _fastShutdown, _startupDelay, _menuDelay, _aeroShake,
        _netLocWizard, _settingSync, _finishSetup, _xferDetails, _telemTasks,
        _alwaysMenu, _hideMerge, _compColor, _infoTip, _statusBar, _noPersistFold, _navExpand, _noShareWiz,
        _driveLetters, _folderGroup, _folderSort,
        _animations, _transparency, _tips, _autoplay, _activityHist, _storageSense, _backgroundApps,
        _searchHighlights, _recommended, _adTracking, _searchHistory, _stickyKeys,
        _cloudSearch, _langList, _trackApps, _settingsSuggest, _inking,
        _msPinyinEn, _msPinyinCloud, _msPinyinBar, _msrt,
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
        MinimumSize = new Size(UiScale.S(1180), UiScale.S(720));
        ClientSize = new Size(UiScale.S(1280), UiScale.S(760));
        Font = UiFit.UiFont;
        // 列表坐标为手工布局；若再用 Dpi AutoScale 会与 UiScale 叠乘导致错位
        AutoScaleMode = AutoScaleMode.None;
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextMain;
        AppBrand.ApplyWindowIcon(this);
        KeyPreview = true;
        MainMenuStrip = _appMenu;

        // 批量分组顺序与 MenuItems 中分组项一致；组内可再分可折叠分区
        _groups.Add((AppLang.L("性能及安全", "Performance & security"), [
            (AppLang.L("常用开关", "Common"), [
                _ie, _uac, _highPerf,
            ]),
            (AppLang.L("性能加速", "Performance"), [
                _visualPerf, _powerThrottle, _boostMode, _gpuSched, _largeCache, _pca, _cpu, _mmcss,
            ]),
            (AppLang.L("Windows 更新", "Windows Update"), [
                _noUpdateReboot, _wuNotify, _noDriverWu, _wuPause2035, _wuPauseUx, _deliveryOpt, _msrt, _noUpdateAsap,
            ]),
            (AppLang.L("网络优化", "Network"), [
                _tcp, _qosSpeed, _bbr2, _ctcp, _smbThrottle, _netThrottle, _webDavLimit,
            ]),
            (AppLang.L("遥测与诊断", "Telemetry & diagnostics"), [
                _telemetry, _diagMinimal, _dps, _ceip, _errorReport, _telemTasks,
            ]),
            (AppLang.L("安全服务", "Security services"), [
                _smb1, _nullSess, _anonEnum, _remoteReg, _spooler, _dep,
            ]),
            (AppLang.L("进阶安全", "Advanced security"), [
                _meltdown, _hvci, _wdac, _vbs, _sysRestore, _noBitlockerAuto,
            ]),
            (AppLang.L("磁盘与文件", "Disk & files"), [
                _diskPerf, _longPaths, _ntfsStamp, _reservedStorage, _srvSplit, _mergeSvchost,
            ]),
            (AppLang.L("启动与维护", "Boot & maintenance"), [
                _fastShutdown, _startupDelay, _autoMaint, _utc, _hpet, _loginVerbose, _f8, _autoReboot,
            ]),
            (AppLang.L("少用服务", "Seldom-used services"), [
                _xbox, _fax, _wmpShare, _trkWks,
            ]),
        ]));
        _groups.Add((AppLang.L("桌面外观", "Desktop look"), [
            (AppLang.L("桌面图标", "Desktop icons"), [
                _thisPc, _desktopIcons, _confirmDel,
            ]),
            (AppLang.L("任务栏", "Taskbar"), [
                _tbAutohide, _taskbar, _allTrayIcons, _tbEndTask, _news,
            ]),
            (AppLang.L("桌面服务", "Desktop services"), [
                _themes, _search, _darkMode, _notepadWrap, _notepadStatus,
            ]),
            (AppLang.L("安全与锁屏", "Security & lock screen"), [
                _smartScreen, _noLockScreen, _feedback,
            ]),
            (AppLang.L("搜索模式", "Search mode"), [
                _classicSearch, _searchEngine,
            ]),
            (AppLang.L("右键菜单", "Context menu"), [
                _takeOwn, _openCmd, _copyMoveTo, _quickOps,
            ]),
        ]));
        _groups.Add((AppLang.L("资源管理器", "File Explorer"), [
            (AppLang.L("常用显示", "Common views"), [
                _fileExt, _hiddenFiles, _fullPath, _hideOs, _launchThisPc,
                _hideSpotlight, _noDupDrives, _noLowDisk, _hideHomeGallery, _noSnapAssist, _xferDetails,
            ]),
            (AppLang.L("快速访问", "Quick access"), [
                _recentFiles, _frequent, _officeCloud, _emptyDrives, _iconsOnly, _noRunMru,
            ]),
            (AppLang.L("文件夹选项", "Folder options"), [
                _alwaysMenu, _hideMerge, _compColor, _infoTip, _statusBar,
                _noPersistFold, _navExpand, _noShareWiz, _driveLetters,
                _folderGroup, _folderSort,
            ]),
            (AppLang.L("快捷方式与布局", "Shortcuts & layout"), [
                _noArrow, _noSuffix, _noShield, _noBrokenLnk, _sepProcess,
                _autoRestartShell, _win11Explorer, _classicMenu, _onedrive,
            ]),
            (AppLang.L("任务栏", "Taskbar"), [
                _tbSearch, _tbLeft, _tbCombine, _widgets, _tbChat, _tbCopilot,
                _taskView, _taskbarClock,
            ]),
        ]));
        _groups.Add((AppLang.L("远程与网络", "Remote & network"), [
            (AppLang.L("远程与网络", "Remote & network"), [
                _rdp, _rdpGpu, _rdpFps, _rdpNla,
                _netDiscovery, _smRemoting,
            ]),
        ]));
        _groups.Add((AppLang.L("电源与后台", "Power & background"), [
            (AppLang.L("远程协助", "Remote Assistance"), [
                _ra,
            ]),
            (AppLang.L("电源与休眠", "Power & hibernation"), [
                _neverSleep, _hibernate, _fastStartup, _usbPowerOff,
            ]),
            (AppLang.L("后台与内存", "Background & memory"), [
                _sysMain, _memComp, _prelaunch, _pageCombine, _ucpd,
            ]),
        ]));
        _groups.Add((AppLang.L("隐私与体验", "Privacy & UX"), [
            (AppLang.L("广告与推荐", "Ads & recommendations"), [
                _tips, _recommended, _searchHighlights, _adTracking, _settingsSuggest, _consumer,
                _noSilentApps, _hideSettingsHome,
            ]),
            (AppLang.L("搜索与助手", "Search & assistants"), [
                _cloudSearch, _webSearch, _searchHistory, _cortana, _copilotAi, _extraAi,
            ]),
            (AppLang.L("隐私数据", "Privacy data"), [
                _trackApps, _langList, _location, _activityHist, _clipCloud, _inking, _officeTel, _cliTelemetry,
                _noSigninReopen, _noCompanionApps, _settingSync, _finishSetup,
            ]),
            (AppLang.L("输入法与键盘", "IME & keyboard"), [
                _msPinyinEn, _msPinyinCloud, _msPinyinBar, _stickyKeys, _keyboardLatency,
            ]),
            (AppLang.L("界面体验", "UI experience"), [
                _animations, _transparency, _backgroundApps, _storageSense, _autoplay, _edgePre, _gameDvr,
                _menuDelay, _aeroShake, _netLocWizard,
            ]),
            (AppLang.L("商店与预览", "Store & Insider"), [
                _insider, _storeUpd, _teredo,
            ]),
        ]));
        _groups.Add((AppLang.L("Server专属", "Server only"), [
            (AppLang.L("Server专属", "Server only"), [
                _svrMgr, _wacPrompt, _azure,
                _mediaFeatures, _bloatFeatures,
                _audio, _installer, _wia,
            ]),
        ]));
        _groups.Add((AppLang.L("账户策略", "Account policy"), [
            (AppLang.L("账户策略", "Account policy"), [
                _autologon, _pwd, _pwdExpire, _noCad,
                _shutdownLogon, _shutdownReason, _keyboardFilter,
            ]),
        ]));

        foreach (var row in AllRows)
        {
            row.OnCheckedChanged = _ =>
            {
                if (_binding) return;
                _uiDirty = true;
                var cat = CurrentCategoryFilter();
                if (cat is RowCategoryFilter.Optimized or RowCategoryFilter.NotOptimized)
                    ApplySearchFilter();
            };
        }

        AttachTcpCongestionMutex();

        WireAppMenu();
        WirePresetMenu();
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
        Shown += (_, _) => TryShowFirstRunNotice();
        FormClosed += (_, _) =>
        {
            ClearPageCache();
            _toolTip.Dispose();
        };
        Resize += (_, _) => LayoutContent();
        KeyDown += OnFormKeyDown;
    }

    private void TryShowFirstRunNotice()
    {
        BeginInvoke(new Action(() =>
        {
            if (IsDisposed) return;
            if (FirstRunNotice.NeedShow())
            {
                using (var dlg = new FirstRunNoticeDialog())
                    dlg.ShowDialog(this);
                FirstRunNotice.MarkDone();
            }
            ScheduleSilentUpdateCheck();
        }));
    }

    private void ScheduleSilentUpdateCheck()
    {
        var prefs = UiPrefs.Load();
        if (prefs.DisableStartupUpdateCheck) return;

        Task.Run(() =>
        {
            try
            {
                var info = AppUpdate.CheckLatest();
                if (!info.IsNewer || IsDisposed) return;
                if (!string.IsNullOrWhiteSpace(prefs.SkippedUpdateTag) &&
                    string.Equals(prefs.SkippedUpdateTag, info.Tag, StringComparison.OrdinalIgnoreCase))
                    return;
                BeginInvoke(new Action(() => PromptSilentUpdate(info)));
            }
            catch
            {
                // 启动检查失败不打扰
            }
        });
    }

    private void PromptSilentUpdate(AppReleaseInfo info)
    {
        if (IsDisposed) return;
        var r = MessageBox.Show(this,
            AppLang.Lf(
                "发现新版本 v{0}（当前 v{1}）。\r\n\r\n立即下载并更新？选「否」则本版本不再提醒。",
                "New version v{0} (current v{1}).\r\n\r\nDownload and update now? Choose No to skip this version.",
                info.Version, AppBrand.VersionText),
            AppLang.L("检查更新", "Check for updates"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (r != DialogResult.Yes)
        {
            var p = UiPrefs.Load();
            p.SkippedUpdateTag = info.Tag;
            UiPrefs.Save(p);
            return;
        }

        using var dlg = new AppUpdateDialog(info);
        dlg.Shown += async (_, _) => await dlg.StartDownloadAsync(confirm: false);
        dlg.ShowDialog(this);
    }

    private void AttachTcpCongestionMutex()
    {
        void Hook(SettingRow self, SettingRow other)
        {
            var prev = self.OnCheckedChanged;
            self.OnCheckedChanged = r =>
            {
                prev?.Invoke(r);
                if (_binding) return;
                if (!self.Checked || !other.Checked) return;
                _binding = true;
                try { other.Checked = false; }
                finally { _binding = false; }
            };
        }

        Hook(_bbr2, _ctcp);
        Hook(_ctcp, _bbr2);
    }

    private void WireAppMenu()
    {
        _appMenu.FileImport.Click += (_, _) => ImportProfile();
        _appMenu.FileExport.Click += (_, _) => ExportProfile();
        _appMenu.FileSettings.Click += (_, _) => ShowAppSettings();
        _appMenu.FileExit.Click += (_, _) => Close();
        _appMenu.ToolAutologon.Click += (_, _) => ConfigureAutologon();
        _appMenu.ToolIdentity.Click += (_, _) => ConfigureComputerIdentity();
        _appMenu.ToolSystemInfo.Click += (_, _) => ShowSystemInfo();
        _appMenu.ToolHosts.Click += (_, _) => ShowHostsEditor();
        _appMenu.ToolEventViewer.Click += (_, _) => OpenEventViewer();
        _appMenu.ToolGroupPolicy.Click += (_, _) => ShowGroupPolicy();
        _appMenu.ToolCmd.Click += (_, _) => SystemToolLauncher.OpenCommandPrompt(this);
        _appMenu.ToolPowerShell.Click += (_, _) => SystemToolLauncher.OpenWindowsPowerShell(this);
        _appMenu.ToolTaskScheduler.Click += (_, _) => SystemToolLauncher.OpenTaskScheduler(this);
        _appMenu.ToolScheduledTaskOptimize.Click += (_, _) =>
        {
            using var dlg = new ScheduledTaskDialog();
            dlg.ShowDialog(this);
        };
        _appMenu.ToolComputerMgmt.Click += (_, _) => SystemToolLauncher.OpenComputerManagement(this);
        _appMenu.ToolFlushDns.Click += (_, _) => FlushDnsCache();
        _appMenu.ToolCommonSoftware.Click += (_, _) => ShowCommonSoftware();
        _appMenu.ToolCleanup.Click += (_, _) => { using var d = new CleanupDialog(); d.ShowDialog(this); };
        _appMenu.ToolShutdownTimer.Click += (_, _) => ShutdownTimerDialog.ShowOrActivate(this);
        _appMenu.ToolOptimizeAdvisor.Click += (_, _) => ShowOptimizeAdvisor();
        _appMenu.ToolPortExposure.Click += (_, _) => { using var d = new PortExposureDialog(); d.ShowDialog(this); };
        _appMenu.ToolOptHistory.Click += (_, _) => { using var d = new OptimizationHistoryDialog(); d.ShowDialog(this); };
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
        _appMenu.ToolSecurityCenter.Click += (_, _) =>
        {
            using var d = new SecurityCenterDialog();
            d.ShowDialog(this);
        };
        _appMenu.ToolEdgeManage.Click += (_, _) =>
        {
            using var d = new EdgeManageDialog();
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
        _appMenu.ToolRefresh.Click += (_, _) => LoadState(fullScan: true, forceUi: true);
        _appMenu.ToolRestoreDefaults.Click += (_, _) => RestoreDefaults();
        _appMenu.ViewAllOn.Click += (_, _) => SetVisibleAll(true);
        _appMenu.ViewAllOff.Click += (_, _) => SetVisibleAll(false);
        _appMenu.HelpCheckUpdate.Click += (_, _) =>
        {
            using var d = new AppUpdateDialog();
            d.Shown += async (_, _) => await d.CheckAsync(autoApply: false);
            d.ShowDialog(this);
        };
        _appMenu.HelpChangeLog.Click += (_, _) => OpenLogFile(ApplyLog.ChangeLogFilePath, AppLang.L("变更日志", "Change log"));
        _appMenu.HelpLog.Click += (_, _) => OpenLogFile(ApplyLog.LogFilePath, AppLang.L("操作日志", "Operation log"));
        _appMenu.HelpDebugLog.Click += (_, _) => OpenLogFile(ApplyLog.DebugLogFilePath, AppLang.L("调试日志", "Debug log"));
        _appMenu.HelpDisclaimer.Click += (_, _) =>
            LegalDocumentDialog.Show(this, AppLang.L("免责声明", "Disclaimer"), "SrvDesk.DISCLAIMER.md");
        _appMenu.HelpPrivacy.Click += (_, _) =>
            LegalDocumentDialog.Show(this, AppLang.L("隐私说明", "Privacy"), "SrvDesk.PRIVACY.md");
        _appMenu.HelpLicense.Click += (_, _) =>
            LegalDocumentDialog.Show(this, AppLang.L("许可证（MIT）", "License (MIT)"), "SrvDesk.LICENSE");
        _appMenu.HelpSupport.Click += (_, _) => ShowSupportDialog();

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
            SetConfigScriptPanelVisible(_appMenu.ViewHelpPanel.Checked);
            // 不写 ui-prefs：避免「视图」临时开关覆盖「配置脚本默认停靠」
            LayoutContent();
        };

        _helpDetail.DockRequested += dock => ApplyConfigScriptDock(dock, fromUser: true);
        _helpDetail.CloseRequested += () =>
        {
            if (_appMenu.ViewHelpPanel.Checked)
                _appMenu.ViewHelpPanel.Checked = false;
        };
    }

    private void ShowAppSettings()
    {
        using var d = new AppSettingsDialog();
        if (d.ShowDialog(this) != DialogResult.OK) return;

        var prefs = UiPrefs.Load();
        _hideIncompatible.Checked = prefs.HideIncompatibleByDefault;
        _appMenu.ViewHideIncompatible.Checked = prefs.HideIncompatibleByDefault;
        var show = UiPrefs.IsDefaultPanelVisible(prefs);
        if (show)
            ApplyConfigScriptDock(UiPrefs.GetDock(prefs), fromUser: false);
        else
        {
            // 默认关闭：布局仍记住上次 Right/Bottom，但不改 prefs 中的 Hidden
            _scriptDock = UiPrefs.GetDock(prefs);
            _helpDetail.SetActiveDock(_scriptDock);
        }

        if (_appMenu.ViewHelpPanel.Checked != show)
            _appMenu.ViewHelpPanel.Checked = show;
        else
            SetConfigScriptPanelVisible(show);

        _status.Text = prefs.EnableDebugLog
            ? AppLang.L("程序设置已保存 · 调试日志已开启（帮助 → 调试日志）", "Settings saved · debug log on (Help → Debug log)")
            : AppLang.L("程序设置已保存", "Settings saved");
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
                    ? AppLang.L(
                          "# 变更日志 — 仅记录优化时真正改动的值（原来从 xx 变成 yy）\r\n" +
                          "# 当前尚无变更记录。\r\n" +
                          "# 请先：勾选推荐项 → 点击底部「应用到系统」→ 再打开本文件。\r\n" +
                          "# 即时页（登录启动项/DNS 等）开关切换后也会写入。\r\n",
                          "# Change log — only values that actually changed (from xx to yy)\r\n" +
                          "# No changes yet.\r\n" +
                          "# Check recommended items → click Apply → reopen this file.\r\n" +
                          "# Instant pages (Startup/DNS, etc.) also write here when toggled.\r\n")
                    : $"# {title}\r\n# " + AppLang.L("尚无记录。", "No entries yet.") + "\r\n";
                File.WriteAllText(path, tip, new System.Text.UTF8Encoding(true));
            }

            if (isChangeLog && !ApplyLog.HasRealChangeEntries())
            {
                MessageBox.Show(
                    AppLang.L(
                        "变更日志里还没有「原来从 xx 变成 yy」的记录。\r\n\r\n" +
                        "请先点击底部「应用到系统」（或以管理员运行新版 SrvDesk.exe），\r\n" +
                        "应用成功后再打开「帮助 → 打开变更日志」。\r\n\r\n" +
                        "路径：\r\n",
                        "No from→to change entries yet.\r\n\r\n" +
                        "Click Apply first (or run SrvDesk.exe as admin),\r\n" +
                        "then open Help → Change log.\r\n\r\n" +
                        "Path:\r\n") + path,
                    AppLang.L("变更日志为空", "Change log empty"),
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
            MessageBox.Show(ex.Message, AppLang.L("无法打开", "Cannot open ") + title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1)
        {
            if (!_appMenu.ViewHelpPanel.Checked)
                _appMenu.ViewHelpPanel.Checked = true;
            _helpDetail.ShowUsageGuide();
            e.Handled = true;
        }
    }

    private int _scriptPanelSize = UiPrefs.DefaultHelpPanelWidth;
    private int _scriptPanelHeight = UiPrefs.DefaultHelpPanelHeight;
    private ConfigScriptDock _scriptDock = ConfigScriptDock.Right;
    private bool _applyingDock;
    private System.Windows.Forms.Timer? _saveSplitTimer;

    private void BuildWorkArea(Panel sidebar)
    {
        UiBuffer.Enable(_workArea);
        _workArea.BackColor = AppTheme.Surface;
        sidebar.Dock = DockStyle.Left;

        var prefs = UiPrefs.Load();
        var showPanel = UiPrefs.IsDefaultPanelVisible(prefs);
        _scriptPanelSize = UiPrefs.ClampWidth(prefs.HelpPanelWidth);
        _scriptPanelHeight = UiPrefs.ClampHeight(prefs.HelpPanelHeight);
        _scriptDock = UiPrefs.GetDock(prefs);

        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.FixedPanel = FixedPanel.Panel2;
        _mainSplit.SplitterWidth = 5;
        _mainSplit.BackColor = AppTheme.BorderLight;
        // 未布局前不要设过大的 MinSize，否则会抛 InvalidOperationException 导致进程直接退出
        _mainSplit.Panel1MinSize = 50;
        _mainSplit.Panel2MinSize = 50;
        _mainSplit.Panel1.BackColor = AppTheme.Surface;
        _mainSplit.Panel2.BackColor = AppTheme.SurfaceCard;
        // 默认收起：避免 HandleCreated 未触发时右侧脚本栏一直露着
        _mainSplit.Panel2Collapsed = true;

        _contentHost.Dock = DockStyle.Fill;
        _helpDetail.Dock = DockStyle.Fill;
        _mainSplit.Panel1.Controls.Add(_contentHost);
        _mainSplit.Panel2.Controls.Add(_helpDetail);

        _saveSplitTimer = new System.Windows.Forms.Timer { Interval = 350 };
        _saveSplitTimer.Tick += (_, _) =>
        {
            _saveSplitTimer!.Stop();
            PersistScriptPanelSize();
        };

        _mainSplit.SplitterMoved += (_, _) =>
        {
            _saveSplitTimer!.Stop();
            _saveSplitTimer.Start();
        };

        void ApplyStartupScriptLayout()
        {
            ApplyConfigScriptDock(_scriptDock, fromUser: false);
            SetConfigScriptPanelVisible(showPanel);
        }

        if (_mainSplit.IsHandleCreated)
            BeginInvoke(ApplyStartupScriptLayout);
        else
            _mainSplit.HandleCreated += (_, _) => BeginInvoke(ApplyStartupScriptLayout);

        _workArea.Controls.Add(_mainSplit);
        _workArea.Controls.Add(sidebar);

        // 菜单勾选与显隐立即对齐偏好（不依赖 HandleCreated）
        _appMenu.ViewHelpPanel.Checked = showPanel;
        _helpDetail.SetActiveDock(_scriptDock);
        _mainSplit.Orientation = _scriptDock == ConfigScriptDock.Bottom
            ? Orientation.Horizontal
            : Orientation.Vertical;
        SetConfigScriptPanelVisible(showPanel);
    }

    private void ApplyConfigScriptDock(ConfigScriptDock dock, bool fromUser)
    {
        if (_applyingDock) return;
        _applyingDock = true;
        try
        {
            _scriptDock = dock;
            _helpDetail.SetActiveDock(dock);
            if (fromUser)
                UiPrefs.SetHelpPanelDock(dock);

            // 仅以菜单勾选为准；勿用「当前是否已折叠」判断（启动时默认未折叠会误显示）
            var visible = _appMenu.ViewHelpPanel.Checked;
            try
            {
                _mainSplit.SuspendLayout();
                // 切换方向前先降 MinSize，避免约束冲突
                _mainSplit.Panel1MinSize = 50;
                _mainSplit.Panel2MinSize = 50;
                _mainSplit.Orientation = dock == ConfigScriptDock.Bottom
                    ? Orientation.Horizontal
                    : Orientation.Vertical;
                if (visible)
                    ApplyScriptPanelDistance();
                // 布局完成后再抬高最小值，限制拖得过小
                if (visible && _mainSplit.Width > 200 && _mainSplit.Height > 200)
                {
                    _mainSplit.Panel1MinSize = dock == ConfigScriptDock.Bottom ? 120 : 280;
                    _mainSplit.Panel2MinSize = dock == ConfigScriptDock.Bottom
                        ? UiPrefs.MinHelpPanelHeight
                        : UiPrefs.MinHelpPanelWidth;
                }
            }
            catch
            {
                /* 布局未就绪 */
            }
            finally
            {
                _mainSplit.ResumeLayout(true);
            }

            SetConfigScriptPanelVisible(visible);

            LayoutContent();
        }
        finally
        {
            _applyingDock = false;
        }
    }
    private void ApplyScriptPanelDistance()
    {
        if (_mainSplit.Panel2Collapsed) return;
        try
        {
            if (_scriptDock == ConfigScriptDock.Bottom)
            {
                var total = _mainSplit.Height;
                var h = Math.Min(_scriptPanelHeight, Math.Max(80, total - 120));
                h = Math.Max(80, h);
                if (total <= h + _mainSplit.SplitterWidth + 80) return;
                _mainSplit.SplitterDistance = total - h - _mainSplit.SplitterWidth;
            }
            else
            {
                var total = _mainSplit.Width;
                var w = Math.Min(_scriptPanelSize, Math.Max(120, total - 320));
                w = Math.Max(120, w);
                if (total <= w + _mainSplit.SplitterWidth + 200) return;
                _mainSplit.SplitterDistance = total - w - _mainSplit.SplitterWidth;
            }
        }
        catch
        {
            /* ignore */
        }
    }

    private void PersistScriptPanelSize()
    {
        if (_mainSplit.Panel2Collapsed) return;
        try
        {
            if (_scriptDock == ConfigScriptDock.Bottom)
            {
                var h = _mainSplit.Panel2.Height;
                if (h >= 80)
                {
                    _scriptPanelHeight = UiPrefs.ClampHeight(h);
                    UiPrefs.SetHelpPanelHeight(_scriptPanelHeight);
                }
            }
            else
            {
                var w = _mainSplit.Panel2.Width;
                if (w >= 120)
                {
                    _scriptPanelSize = UiPrefs.ClampWidth(w);
                    UiPrefs.SetHelpPanelWidth(_scriptPanelSize);
                }
            }
        }
        catch { /* ignore */ }
    }

    private void SetConfigScriptPanelVisible(bool visible)
    {
        try
        {
            _mainSplit.Panel2Collapsed = !visible;
            _helpDetail.Visible = visible;
            if (visible)
                BeginInvoke(ApplyScriptPanelDistance);
        }
        catch
        {
            _helpDetail.Visible = visible;
        }
    }

    private void InitializeRuntime()
    {
        if (!AdminHelper.IsRunningAsAdministrator())
        {
            _status.ForeColor = Color.FromArgb(163, 72, 0);
            _status.Text = AppLang.L("提示：当前进程未提升权限，部分系统级项可能写入失败（失败项会显示在状态栏）。", "Tip: not elevated — some system writes may fail (shown in status bar).");
            _headerSubtitle.Text = _systemFacts.Summary;
        }
        else if (!_systemFacts.IsServer)
        {
            _status.ForeColor = AppTheme.ScopeServer;
            _status.Text = AppLang.Lf("提示：当前不是 Windows Server（{0}）。部分「Server 专属」项可能无效。", "Tip: not Windows Server ({0}). Some Server-only items may not apply.", _systemFacts.Summary);
            _headerSubtitle.Text = _systemFacts.Summary + AppLang.L(" · 非 Server 环境", " · Non-Server");
        }
        else if (!_systemFacts.HasDesktopExperience)
        {
            _status.ForeColor = AppTheme.ScopeServer;
            _status.Text = AppLang.L("提示：检测到 Server Core（无桌面体验）。已默认隐藏「需桌面体验」项，可取消勾选过滤。", "Tip: Server Core detected. Desktop-Experience items are hidden; uncheck the filter to show them.");
            _headerSubtitle.Text = _systemFacts.Summary + " · Server Core";
            _hideIncompatible.Checked = true;
        }
        else
        {
            _status.Text = _systemFacts.Summary + AppLang.L(" · 正在加载…", " · Loading…");
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

        // 程序设置：默认隐藏不适用项（Server Core 上面已强制勾选）
        if (_systemFacts.HasDesktopExperience && UiPrefs.Load().HideIncompatibleByDefault)
            _hideIncompatible.Checked = true;

        ApplyLog.Write(AppLang.L("启动 ", "Start ") + _systemFacts.Summary);
        try
        {
            if (!ServerProfile.Load().ProfileConfigured)
                ServerRoleDetector.MergeDetectedIntoProfile(overwriteUser: false);
            HealthInspectionService.StartIfEnabled(this);
        }
        catch { /* ignore */ }
        if (UiPrefs.EnableDebugLog)
            ApplyLog.Debug(AppLang.L("启动调试会话 · ", "Debug session · ") + _systemFacts.Summary);
        UseWaitCursor = false;
        Cursor = Cursors.Default;
        // 后台预热常用软件状态，点击打开时尽量秒开
        CommonSoftwareHelper.WarmUpInBackground();
        System.Threading.Tasks.Task.Run(() =>
        {
            try { CommonSoftwareHelper.PrefetchStatuses(CommonSoftwareCatalog.All); }
            catch { /* ignore */ }
        });
        // 「设置操作」必须等于本机当前值：首帧同步读注册表/服务，再绑到开关/下拉。
        // 完整扫描（DISM 等慢项）仍后台补，且仅在用户未改开关时写回。
        BindFromSystem(fullScan: false);
        BeginInvoke(new Action(StartWarmupInstantPages));
        BeginInvoke(new Action(() => LoadState(fullScan: true, forceUi: false)));
    }

    /// <summary>空闲时分帧预创建即时页，并预热 MMAgent，减轻首次点左侧菜单的卡顿。</summary>
    private void StartWarmupInstantPages()
    {
        System.Threading.Tasks.Task.Run(EasySettingsTweaks.WarmupMmAgentCache);

        var titles = new[]
        {
            AppLang.L("登录启动项", "Startup apps"),
            AppLang.L("服务优化", "Service optimize"),
            AppLang.L("DNS 设置", "DNS settings"),
            AppLang.L("自定义配置", "Custom config"),
        };
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
                Form page = CreateEmbeddedPage(title);
                page.TopLevel = false;
                page.FormBorderStyle = FormBorderStyle.None;
                page.ControlBox = false;
                page.AutoScaleMode = AutoScaleMode.None;
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
        _commandBar.Height = UiFit.ControlHeight() + 20;
        _commandBar.BackColor = AppTheme.SurfaceCard;
        _commandBar.Padding = new Padding(12, 8, 12, 8);
        _commandBar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, 0, _commandBar.Height - 1, _commandBar.Width, _commandBar.Height - 1);
        };

        _commandFlow.Dock = DockStyle.Fill;
        _commandFlow.FlowDirection = FlowDirection.LeftToRight;
        _commandFlow.WrapContents = false;
        _commandFlow.AutoScroll = false;
        _commandFlow.BackColor = AppTheme.SurfaceCard;
        _commandFlow.Padding = new Padding(0);
        UiBuffer.ConfigureNoScrollRow(_commandFlow);

        _commandFlow.Controls.Add(BarLabel(AppLang.L("搜索", "Search")));
        _searchBox.Width = 200;
        _searchBox.Font = UiFit.UiFont;
        _searchBox.Height = UiFit.ControlHeight(_searchBox.Font, 28);
        _searchBox.Margin = new Padding(0, 2, 16, 0);
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.ForeColor = AppTheme.TextMain;
        _searchBox.TextChanged += (_, _) => ApplySearchFilter();
        _commandFlow.Controls.Add(_searchBox);

        _hideIncompatible.Text = AppLang.L("隐藏不适用项", "Hide incompatible");
        _hideIncompatible.AutoSize = true;
        _hideIncompatible.Margin = new Padding(0, 4, 16, 0);
        _hideIncompatible.ForeColor = AppTheme.TextMute;
        _hideIncompatible.CheckedChanged += (_, _) => ApplySearchFilter();
        _commandFlow.Controls.Add(_hideIncompatible);

        _commandFlow.Controls.Add(BarLabel(AppLang.L("分类", "Filter")));
        _categoryFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryFilter.Font = UiFit.UiFont;
        UiFit.FitCombo(_categoryFilter);
        _categoryFilter.Margin = new Padding(0, 2, 0, 0);
        _categoryFilter.Items.AddRange([
            AppLang.L("全部", "All"),
            AppLang.L("Server 推荐", "Server picks"),
            AppLang.L("优化推荐", "Recommended"),
            AppLang.L("已优化", "Optimized"),
            AppLang.L("未优化", "Not optimized"),
        ]);
        _categoryFilter.SelectedIndex = 0;
        _categoryFilter.SelectedIndexChanged += (_, _) => ApplySearchFilter();
        FitComboToItems(_categoryFilter, minWidth: 120, extra: 48);
        _toolTip.SetToolTip(_categoryFilter,
            AppLang.L("Server 推荐：Server 专属项\r\n优化推荐：通用桌面/性能/隐私项\r\n已优化 / 未优化：按当前开关状态筛选", "Server picks: Server-only\r\nRecommended: general desktop/perf/privacy\r\nOptimized / Not: by current toggle state"));
        _commandFlow.Controls.Add(_categoryFilter);

        _commandFlow.Controls.Add(BarLabel(AppLang.L("预设", "Preset")));
        _presetCombo.Font = UiFit.UiFont;
        UiFit.FitCombo(_presetCombo);
        _presetCombo.Margin = new Padding(0, 2, 8, 0);
        _presetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var p in OptPresets.All)
            _presetCombo.Items.Add(p);
        if (_presetCombo.Items.Count > 0)
            _presetCombo.SelectedIndex = 0;
        FitComboToItems(_presetCombo, minWidth: 220, extra: 48);
        _toolTip.SetToolTip(_presetCombo, AppLang.L("选择预设方案后点「载入」，再检查开关并应用到系统", "Pick a preset, click Load, review, then Apply"));
        _commandFlow.Controls.Add(_presetCombo);
        _commandFlow.Controls.Add(BarQuickButton(AppLang.L("载入", "Load"), AppLang.L("把所选预设勾选到界面（不会立刻写入系统）", "Apply preset to UI (does not write system yet)"), () =>
        {
            if (_presetCombo.SelectedItem is OptPresets.PresetInfo p)
                LoadPreset(p);
        }));

        // 顶部快捷入口
        var quickGap = new Label
        {
            Text = "",
            AutoSize = false,
            Width = 12,
            Height = 1,
            Margin = new Padding(0),
        };
        _commandFlow.Controls.Add(quickGap);
        _commandFlow.Controls.Add(BarQuickButton(AppLang.L("配置脚本", "Config script"), AppLang.L("显示或隐藏配置脚本面板（可查看/编辑）", "Show or hide config script panel"), ToggleConfigScriptPanel));
        _commandFlow.Controls.Add(BarQuickButton(AppLang.L("常用软件", "Apps"), AppLang.L("打开常用软件安装与更新", "Install or update common apps"), ShowCommonSoftware));
        _commandFlow.Controls.Add(BarQuickButton(AppLang.L("优化顾问", "Advisor"), AppLang.L("按本机状态查看并应用强烈推荐/推荐项", "Review and apply strongly recommended items for this PC"), ShowOptimizeAdvisor));

        // 即时页不再在此显示提示（统一走底部状态栏）
        _commandBar.Controls.Add(_commandFlow);
    }

    private void WirePresetMenu()
    {
        foreach (var p in OptPresets.All)
        {
            var info = p;
            var item = new ToolStripMenuItem(info.Title)
            {
                ToolTipText = info.Description,
            };
            item.Click += (_, _) =>
            {
                for (var i = 0; i < _presetCombo.Items.Count; i++)
                {
                    if (ReferenceEquals(_presetCombo.Items[i], info) ||
                        (_presetCombo.Items[i] is OptPresets.PresetInfo x && x.Id == info.Id))
                    {
                        _presetCombo.SelectedIndex = i;
                        break;
                    }
                }
                LoadPreset(info);
            };
            _appMenu.PresetRoot.DropDownItems.Add(item);
        }
    }

    private void LoadPreset(OptPresets.PresetInfo preset)
    {
        Bind(preset.Build(), updateCurrentValues: false);
        _uiDirty = true;
        _status.Text = AppLang.Lf("已载入预设「{0}」。请检查后点「应用到系统」。", "Loaded preset “{0}”. Review, then Apply.", preset.Title);
        ApplyLog.Write(AppLang.L("载入预设 ", "Load preset ") + preset.Id + " / " + preset.Title);
    }

    private Button BarQuickButton(string text, string tip, Action click)
    {
        var font = UiFit.UiFont;
        var h = UiFit.ControlHeight(font);
        var b = new Button
        {
            Text = text,
            Font = font,
            AutoSize = false,
            Size = UiFit.ButtonSize(text, h, font, minWidth: 72, padding: 20),
            Margin = new Padding(0, 2, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = AppTheme.TextMain,
            Cursor = Cursors.Hand,
            TabStop = false,
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false,
            Padding = Padding.Empty,
        };
        b.FlatAppearance.BorderColor = AppTheme.Border;
        b.FlatAppearance.BorderSize = 1;
        UiFit.EnableCenteredFlatText(b);
        b.MouseEnter += (_, _) => { b.BackColor = AppTheme.PrimaryPale; b.Invalidate(); };
        b.MouseLeave += (_, _) => { b.BackColor = Color.White; b.Invalidate(); };
        b.Click += (_, _) => click();
        _toolTip.SetToolTip(b, tip);
        return b;
    }

    /// <summary>快捷入口：显示/隐藏配置脚本面板（不停靠强制）。</summary>
    private void ToggleConfigScriptPanel()
    {
        _appMenu.ViewHelpPanel.Checked = !_appMenu.ViewHelpPanel.Checked;
    }

    private static Label BarLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 6, 6, 0),
        ForeColor = AppTheme.TextMute,
        BackColor = Color.Transparent,
    };

    private static void FitComboToItems(ComboBox box, int minWidth, int extra)
    {
        var w = minWidth;
        foreach (var item in box.Items)
        {
            var s = item?.ToString() ?? "";
            var tw = TextRenderer.MeasureText(s, box.Font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + extra;
            if (tw > w) w = tw;
        }

        box.Width = Math.Min(Math.Max(w, minWidth), 360);
        box.DropDownWidth = Math.Max(box.Width, w);
    }

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
            Filter = AppLang.L("SrvDesk 配置 (*.json)|*.json", "SrvDesk profile (*.json)|*.json"),
            FileName = AppLang.L("SrvDesk-配置.json", "SrvDesk-profile.json"),
            InitialDirectory = ProfileStore.DefaultProfileDir(),
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            ProfileStore.Save(dlg.FileName, CaptureState(), AppLang.L("用户导出", "User export"));
            _status.Text = AppLang.L("已导出全部配置（开关 + 脚本覆盖 + 自定义方案）：", "Exported full profile (toggles + scripts + packs): ") + dlg.FileName;
            ApplyLog.Write(AppLang.L("导出配置 ", "Export profile ") + dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, AppLang.L("导出失败", "Export failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportProfile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = AppLang.L("SrvDesk 配置 (*.json)|*.json", "SrvDesk profile (*.json)|*.json"),
            InitialDirectory = ProfileStore.DefaultProfileDir(),
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            var bundle = ProfileStore.LoadBundle(dlg.FileName);
            var parts = new List<string>();
            if (bundle.HasSettings)
            {
                Bind(bundle.State);
                _uiDirty = true;
                parts.Add(AppLang.L("开关", "Toggles"));
            }
            if (bundle.HasScriptOverrides || bundle.HasCustomPacks)
            {
                var tip = AppLang.L("将写入本机保存的配置脚本覆盖与自定义方案，覆盖现有本地内容。是否继续？", "This will overwrite local script overrides and custom packs. Continue?");
                if (MessageBox.Show(this, tip, AppLang.L("导入配置", "Import profile"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    if (parts.Count > 0)
                        _status.Text = AppLang.L("已导入开关到界面（未导入脚本）：", "Imported toggles to UI (scripts skipped): ") + dlg.FileName;
                    return;
                }
                ProfileStore.ApplyLocalData(bundle);
                if (bundle.HasScriptOverrides) parts.Add(AppLang.L("脚本覆盖", "Script overrides"));
                if (bundle.HasCustomPacks) parts.Add(AppLang.L("自定义方案", "Custom packs"));
                RefreshAfterProfileImport();
            }

            _status.Text = AppLang.L("已导入", "Imported ") + string.Join(AppLang.L("、", ", "), parts) + "：" + dlg.FileName
                           + (bundle.HasSettings ? AppLang.L("（开关需点「应用到系统」生效）", " (toggles need Apply)") : "");
            ApplyLog.Write(AppLang.L("导入配置 ", "Import profile ") + dlg.FileName + " [" + string.Join(",", parts) + "]");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, AppLang.L("导入失败", "Import failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshAfterProfileImport()
    {
        if (_pageCache.TryGetValue(AppLang.L("自定义配置", "Custom config"), out var page) && page is IEmbeddedSettingsPage embedded)
            embedded.RefreshFromSystem();
        _helpDetail.ReloadScriptsIfShowing();
    }

    private void ConfigureAutologon()
    {
        if (!ConfigureAutologonDialog()) return;
        _autologon.Checked = true;
        _status.Text = AppLang.Lf("Autologon 已配置：{0}（应用到系统后下次重启生效）", "Autologon set for {0} (takes effect after Apply + reboot)", _autologonSettings!.Username);
    }

    private void ConfigureComputerIdentity() => PromptComputerIdentity();

    private void ShowOptimizeAdvisor()
    {
        using var d = new HealthOverviewDialog(this);
        d.ShowDialog(this);
    }

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
                _status.Text = AppLang.L("已刷新 DNS 缓存（ipconfig /flushdns）。", "DNS cache flushed (ipconfig /flushdns).");
                MessageBox.Show(this, AppLang.L("DNS 解析缓存已清空。\r\n之后的域名解析会重新向 DNS 服务器查询。", "DNS cache cleared.\r\nNext lookups will query the DNS server again."),
                    AppLang.L("刷新 DNS 缓存", "Flush DNS"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, AppLang.L("ipconfig /flushdns 未成功完成。", "ipconfig /flushdns did not complete."),
                    AppLang.L("刷新 DNS 缓存", "Flush DNS"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, AppLang.L("无法刷新 DNS 缓存。\r\n\r\n", "Could not flush DNS cache.\r\n\r\n") + ex.Message,
                AppLang.L("刷新 DNS 缓存", "Flush DNS"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            ApplyLog.Write(AppLang.L("打开事件查看器", "Open Event Viewer"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, AppLang.L("无法打开事件查看器。\r\n\r\n", "Could not open Event Viewer.\r\n\r\n") + ex.Message,
                AppLang.L("事件查看器", "Event Viewer"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool EnsureAutologonReady()
    {
        if (!_autologon.Checked) return true;

        // 系统/基线已启用：只补齐会话缓存，绝不弹窗打扰其它优化项写入
        var alreadyOn = _baselineState?.EnableAutologon == true;
        if (alreadyOn)
        {
            if (!HasAutologonCredentials())
                TryHydrateAutologonFromSystem();
            return true;
        }

        // 新勾选启用：已有账户信息则直接用；否则弹配置窗
        if (HasAutologonCredentials()) return true;
        if (TryHydrateAutologonFromSystem()) return true;
        return ConfigureAutologonDialog();
    }

    private bool HasAutologonCredentials() =>
        _autologonSettings is not null
        && !string.IsNullOrWhiteSpace(_autologonSettings.Username);

    /// <summary>从当前系统 Winlogon/LSA 状态填充会话缓存（保留已有密码，不弹窗）。</summary>
    private bool TryHydrateAutologonFromSystem()
    {
        var status = AutologonHelper.Read();
        if (!status.Enabled || string.IsNullOrWhiteSpace(status.Username))
            return false;
        _autologonSettings = AutologonHelper.FromStatus(status);
        return true;
    }

    private bool ConfigureAutologonDialog()
    {
        var status = AutologonHelper.Read();
        var initial = _autologonSettings is not null
            ? new AutologonSettings
            {
                Domain = _autologonSettings.Domain,
                Username = _autologonSettings.Username,
                Password = _autologonSettings.Password,
                UpdatePassword = _autologonSettings.UpdatePassword,
            }
            : AutologonHelper.FromStatus(status);
        using var dlg = new AutologonDialog(initial, status.Enabled || HasAutologonCredentials());
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
        var rowH = SettingListLayout.RowHeight;
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
            _status.Text = AppLang.L("无匹配项，请调整搜索或分类筛选。", "No matches — adjust search or filter.");
        else if (_status.Text.StartsWith(AppLang.L("无匹配项", "No matches"), StringComparison.Ordinal))
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
            Location = new Point(header.Width - 532, 0),
        };

        // 蓝色顶栏左侧系统信息，右侧资源占用
        _headerSubtitle.AutoSize = false;
        _headerSubtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _headerSubtitle.Location = new Point(54, 0);
        _headerSubtitle.Height = 48;
        _headerSubtitle.ForeColor = AppTheme.TextOnPrimarySoft;
        _headerSubtitle.Font = UiFit.UiFont;
        _headerSubtitle.TextAlign = ContentAlignment.MiddleLeft;
        _headerSubtitle.BackColor = Color.Transparent;
        _headerSubtitle.Text = AppLang.L("Windows Server 桌面优化", "Windows Server desktop tweaks");

        header.Controls.Add(_headerSubtitle);
        header.Controls.Add(_headerMeter);
        header.Controls.Add(logo);
        void LayoutHeader()
        {
            if (_headerMeter is null) return;
            // 右侧多留一点边距，避免 IP/资源字被窗体边缘裁切
            const int rightPad = 28;
            _headerMeter.Left = Math.Max(200, header.ClientSize.Width - _headerMeter.Width - rightPad);
            _headerSubtitle.Width = Math.Max(120, _headerMeter.Left - _headerSubtitle.Left - 12);
        }
        header.Resize += (_, _) => LayoutHeader();
        _headerMeter.SizeChanged += (_, _) => LayoutHeader();
        LayoutHeader();
        return header;
    }

    private static Image? LoadLogo() => AppBrand.LoadLogoImage();

    private Panel BuildSidebar()
    {
        var sidebar = NavMenuStyle.CreateSidebar();
        NavMenuStyle.Apply(_menu);
        _menu.Items.AddRange(MenuItems);
        _menu.DrawItem += DrawMenuItem;
        _menu.SelectedIndexChanged += (_, _) => ShowGroup(_menu.SelectedIndex);
        NavMenuStyle.BindHover(_menu, () => _menuHover, v => _menuHover = v);
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

    private Form CreateEmbeddedPage(string title)
    {
        if (title == AppLang.L("登录启动项", "Startup apps"))
            return new StartupManagerDialog();
        if (title == AppLang.L("服务优化", "Service optimize"))
        {
            var page = new ServiceOptimizeDialog();
            page.SelectionChanged = row => _helpDetail.ShowServiceOptimize(row);
            return page;
        }
        if (title == AppLang.L("DNS 设置", "DNS settings"))
            return new DnsSwitcherDialog();
        if (title == AppLang.L("自定义配置", "Custom config"))
            return new CustomConfigDialog();
        throw new InvalidOperationException(title);
    }

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
                // 从即时页返回时不要重新读系统：会覆盖用户刚勾的开关
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
            // 从即时页返回时不要重新读系统：会覆盖用户刚勾的开关
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
                page = CreateEmbeddedPage(title);
                page.TopLevel = false;
                page.FormBorderStyle = FormBorderStyle.None;
                page.ControlBox = false;
                page.AutoScaleMode = AutoScaleMode.None;
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
        // 面板默认关闭；仅在已显示时刷新内容（勿自动勾选「视图 → 显示配置脚本」）
        _helpDetail.ShowSetting(row.ItemText, row.Help);
        if (_appMenu.ViewHelpPanel.Checked)
            _helpDetail.FocusRecipe();
    }

    private void ShowRecipeDialog(SettingRow row)
    {
        _selectedRow?.SetSelected(false);
        _selectedRow = row;
        row.SetSelected(true);
        SettingRecipeDialog.ShowFor(this, row.ItemText, row.Help);
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
        // 批量页显示搜索/预设命令栏；嵌入页（服务优化等）收起，避免顶部空一截
        _commandFlow.Visible = batch;
        _commandBar.Visible = batch;
        _commandBar.Height = batch ? UiFit.ControlHeight() + 20 : 0;

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
            Size = new Size(ContentWidth(), UiScale.S(h)),
            BackColor = AppTheme.PrimaryLight,
            Tag = "table-header",
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        header.Controls.Add(MakeHeaderLabel(AppLang.L("项目", "Item"), SettingListLayout.InfoX, SettingListLayout.RecommendHeaderX - SettingListLayout.InfoX - 4));
        header.Controls.Add(MakeHeaderLabel(AppLang.L("设置操作", "Action"), SettingListLayout.RecommendHeaderX, SettingListLayout.RecommendHeaderW, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderLabel(AppLang.L("系统默认值", "Default"), SettingListLayout.SystemX, SettingListLayout.SystemW, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderLabel(AppLang.L("系统当前值", "Current"), SettingListLayout.CurrentX, SettingListLayout.CurrentW, ContentAlignment.MiddleCenter));
        var levelHeader = MakeHeaderLabel(AppLang.L("推荐值", "Recommend"), SettingListLayout.LevelX, SettingListLayout.LevelW, ContentAlignment.MiddleCenter);
        levelHeader.Tag = "level-header";
        _toolTip.SetToolTip(levelHeader, RecommendLevelUi.LegendShort);
        header.Controls.Add(levelHeader);
        var noteHeader = MakeHeaderLabel(AppLang.L("说明", "Notes"), SettingListLayout.NoteX, SettingListLayout.NoteWidthFor(ContentWidth()));
        noteHeader.Tag = "note-header";
        header.Controls.Add(noteHeader);
        return header;
    }

    private static Label MakeHeaderLabel(string text, int x, int w, ContentAlignment align = ContentAlignment.MiddleLeft) =>
        new SingleLineLabel
        {
            Text = text,
            Location = new Point(x, 0),
            Size = new Size(w, UiScale.S(36)),
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFontBold(),
            TextAlign = align,
            BackColor = Color.Transparent,
        };

    /// <summary>分区内按推荐强度降序：必优化 → 强烈推荐 → 建议优化 → 可选；同级按标题。</summary>
    private static SettingRow[] OrderRowsByRecommend(SettingRow[] rows)
    {
        var ordered = (SettingRow[])rows.Clone();
        Array.Sort(ordered, (a, b) =>
        {
            var byLevel = b.EffectiveRecommend.CompareTo(a.EffectiveRecommend);
            if (byLevel != 0)
                return byLevel;
            return string.Compare(a.ItemText, b.ItemText, StringComparison.CurrentCultureIgnoreCase);
        });
        return ordered;
    }

    private Panel BuildGroupSection(string title, SettingRow[] rows)
    {
        const int headerH = 34;
        var rowH = SettingListLayout.RowHeight;
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
            Location = new Point(UiScale.S(12), UiScale.S(8)),
            AutoSize = true,
            ForeColor = AppTheme.PrimaryDark,
            Font = new Font(UiFit.UiFontFamily, 8F),
            BackColor = Color.Transparent,
        };
        var titleLabel = new Label
        {
            Text = title,
            Location = new Point(UiScale.S(32), 0),
            Size = new Size(section.Width - UiScale.S(120), headerH),
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFontBold(),
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
            rows[i].Mount(body, i * rowH, rowH, bg, section.Width, _toolTip, ShowHelp, ShowRecipeDialog);
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
            Text = AppLang.L("恢复本组默认", "Reset section"),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            LinkColor = AppTheme.PrimaryDark,
            ActiveLinkColor = AppTheme.Primary,
            VisitedLinkColor = AppTheme.PrimaryDark,
            BackColor = Color.Transparent,
        };
        restoreGroup.Click += (_, _) => RestoreGroup(title, rows);
        head.Controls.Add(restoreGroup);

        void LayoutSectionHeader()
        {
            head.Width = section.Width;
            body.Width = section.Width;
            var linkW = Math.Max(restoreGroup.PreferredSize.Width, UiFit.TextWidth(restoreGroup.Text) + 4);
            restoreGroup.Location = new Point(Math.Max(80, section.Width - linkW - 12), 8);
            titleLabel.Width = Math.Max(80, restoreGroup.Left - titleLabel.Left - 8);
            foreach (var row in rows)
                row.ApplyLayoutWidth(section.Width);
        }

        section.Controls.Add(body);
        section.Controls.Add(head);
        section.Tag = body;
        section.Resize += (_, _) => LayoutSectionHeader();
        LayoutSectionHeader();
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

        var actions = new NoScrollFlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0, 10, 14, 0),
            BackColor = AppTheme.SurfaceCard,
        };
        UiBuffer.ConfigureNoScrollRow(actions);
        _bottomActions = actions;

        // 底部三键始终占位：刷新 / 恢复默认 / 应用到系统（按页启用）
        _refreshBottom = ToolButton(AppLang.L("刷新", "Refresh"), () => LoadState(fullScan: true, forceUi: true));

        _restore.Text = AppLang.L("恢复默认", "Reset");
        _restore.AutoSize = false;
        _restore.Size = UiFit.ButtonSize(AppLang.L("恢复默认", "Reset"), 36, UiFit.UiFontBold(), padding: 28);
        _restore.Margin = new Padding(8, 0, 0, 0);
        _restore.FlatStyle = FlatStyle.Flat;
        _restore.BackColor = AppTheme.SurfaceCard;
        _restore.ForeColor = AppTheme.PrimaryDeep;
        _restore.Font = UiFit.UiFontBold();
        _restore.Cursor = Cursors.Hand;
        _restore.FlatAppearance.BorderColor = AppTheme.Border;
        _restore.Click += (_, _) => RestoreDefaults();
        _restore.MouseEnter += (_, _) => _restore.BackColor = AppTheme.PrimaryPale;
        _restore.MouseLeave += (_, _) => _restore.BackColor = AppTheme.SurfaceCard;

        _apply.Text = AppLang.L("应用到系统", "Apply");
        _apply.AutoSize = false;
        _apply.Size = UiFit.ButtonSize(AppLang.L("应用到系统", "Apply"), 36, UiFit.UiFontBold(), padding: 28);
        _apply.Margin = new Padding(8, 0, 0, 0);
        _apply.FlatStyle = FlatStyle.Flat;
        _apply.FlatAppearance.BorderSize = 0;
        _apply.BackColor = AppTheme.Primary;
        _apply.ForeColor = AppTheme.TextOnPrimary;
        _apply.Font = UiFit.UiFontBold();
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
        NavMenuStyle.DrawItem(
            e,
            MenuItems[e.Index],
            Font,
            e.Index == _menuHover,
            separator: MenuItems[e.Index] == AppLang.L("性能及安全", "Performance & security"));
    }

    /// <param name="forceUi">
    /// true：用系统状态覆盖界面开关（启动首读、用户点刷新、应用后回读）。
    /// false：若用户已改过开关则只丢弃本次后台读取，避免「开了又自己关」。
    /// </param>
    /// <summary>从本机读取状态并立刻写到「设置操作」/「系统当前值」。</summary>
    private bool BindFromSystem(bool fullScan)
    {
        try
        {
            Bind(Optimizer.Read(fullScan), updateCurrentValues: true);
            _uiDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            _status.Text = AppLang.L("读取当前配置失败：", "Failed to read settings: ") + ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 按侧栏标签页归类：本机实际未达推荐，且有效强度为强烈推荐/推荐的条目。
    /// </summary>
    internal IReadOnlyList<TabOptimizeGroup> CollectTabOptimizeFindings(bool refreshFromSystem)
    {
        if (refreshFromSystem)
            BindFromSystem(fullScan: false);

        var facts = _systemFacts;
        var hide = _hideIncompatible.Checked;
        var byTab = new Dictionary<string, List<TabOptimizeFinding>>(StringComparer.Ordinal);

        foreach (var (tabTitle, sections) in _groups)
        {
            foreach (var (sectionTitle, rows) in sections)
            {
                foreach (var row in rows)
                {
                    if (!row.MatchesFilter("", facts, hide))
                        continue;
                    if (row.Checked)
                        continue;
                    // 顾问只列：本机未达推荐，且有效强度 ≥ 推荐（强烈推荐/推荐）
                    var level = row.EffectiveRecommend;
                    if (level < RecommendRules.AdvisorMinLevel)
                        continue;

                    if (!byTab.TryGetValue(tabTitle, out var list))
                    {
                        list = [];
                        byTab[tabTitle] = list;
                    }

                    var hint = row.Help.WhenHint;
                    if (string.IsNullOrWhiteSpace(hint))
                        hint = row.Help.ListNote;
                    list.Add(new TabOptimizeFinding
                    {
                        TabTitle = tabTitle,
                        SectionTitle = sectionTitle,
                        ItemTitle = row.ItemText,
                        CurrentValue = row.CurrentValueText,
                        RecommendedValue = row.RecommendedValueText,
                        Level = level,
                        Hint = hint,
                        IsService = false,
                    });
                }
            }
        }

        var serviceTab = AppLang.L("服务优化", "Service optimize");
        try
        {
            foreach (var svc in ServiceOptimizeHelper.LoadApplicable(installedOnly: true))
            {
                if (!svc.CanOptimize)
                    continue;
                var level = svc.OptimizeLevelFor(facts);
                if (level < RecommendRules.AdvisorMinLevel)
                    continue;
                if (!byTab.TryGetValue(serviceTab, out var list))
                {
                    list = [];
                    byTab[serviceTab] = list;
                }

                var targetKind = svc.Recommend switch
                {
                    ServiceRecommend.Disable => ServiceStartTypeKind.Disabled,
                    ServiceRecommend.Manual => ServiceStartTypeKind.Manual,
                    ServiceRecommend.Auto => ServiceStartTypeKind.Automatic,
                    _ => ServiceStartTypeKind.Unknown,
                };
                var recommendText = targetKind == ServiceStartTypeKind.Unknown
                    ? "—"
                    : ServiceOptimizeHelper.StartTypeLabel(targetKind);
                list.Add(new TabOptimizeFinding
                {
                    TabTitle = serviceTab,
                    SectionTitle = AppLang.L(svc.Entry.CategoryZh, svc.Entry.CategoryEn),
                    ItemTitle = string.IsNullOrWhiteSpace(svc.DisplayName) ? svc.ActualServiceName : svc.DisplayName,
                    CurrentValue = ServiceOptimizeHelper.StartTypeLabel(svc.StartType),
                    RecommendedValue = recommendText,
                    Level = level,
                    Hint = string.IsNullOrWhiteSpace(svc.AdviceNote) ? svc.AdviceTag : svc.AdviceNote,
                    IsService = true,
                    ServiceName = svc.ActualServiceName,
                    ServiceTarget = targetKind,
                });
            }
        }
        catch
        {
            /* 服务枚举失败时仍返回开关诊断 */
        }

        var result = new List<TabOptimizeGroup>();
        foreach (var title in MenuItems)
        {
            if (!byTab.TryGetValue(title, out var findings) || findings.Count == 0)
                continue;
            findings.Sort((a, b) =>
            {
                var c = b.Level.CompareTo(a.Level);
                return c != 0 ? c : string.Compare(a.ItemTitle, b.ItemTitle, StringComparison.CurrentCultureIgnoreCase);
            });
            result.Add(new TabOptimizeGroup { TabTitle = title, Findings = findings });
        }

        foreach (var kv in byTab)
        {
            if (result.Exists(g => g.TabTitle == kv.Key))
                continue;
            if (kv.Value.Count == 0)
                continue;
            kv.Value.Sort((a, b) =>
            {
                var c = b.Level.CompareTo(a.Level);
                return c != 0 ? c : string.Compare(a.ItemTitle, b.ItemTitle, StringComparison.CurrentCultureIgnoreCase);
            });
            result.Add(new TabOptimizeGroup { TabTitle = kv.Key, Findings = kv.Value });
        }

        return result;
    }

    /// <summary>
    /// 优化顾问：把选中项设为推荐态。
    /// 开关只改主界面勾选；服务立即写启动类型（与服务优化页一致）。
    /// </summary>
    internal (int settings, int services, List<string> errors, List<string> resolvedKeys) ApplyRecommendedFindings(
        IReadOnlyList<TabOptimizeFinding> findings)
    {
        var settings = 0;
        var services = 0;
        var errors = new List<string>();
        var resolvedKeys = new List<string>();
        if (findings.Count == 0)
            return (0, 0, errors, resolvedKeys);

        _binding = true;
        try
        {
            foreach (var f in findings)
            {
                if (f.IsService)
                {
                    if (string.IsNullOrWhiteSpace(f.ServiceName)
                        || f.ServiceTarget is ServiceStartTypeKind.Unknown or ServiceStartTypeKind.Missing)
                        continue;
                    try
                    {
                        ServiceOptimizeHelper.SetStartType(f.ServiceName, f.ServiceTarget);
                        services++;
                        resolvedKeys.Add(TabOptimizeFinding.KeyOf(f));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(f.ItemTitle + ": " + ex.Message);
                    }
                    continue;
                }

                var row = FindSettingRowByItemTitle(f.ItemTitle);
                if (row is null)
                {
                    errors.Add(AppLang.Lf("未找到开关：{0}", "Toggle not found: {0}", f.ItemTitle));
                    continue;
                }

                if (!row.Checked)
                {
                    row.Checked = true;
                    settings++;
                }
                row.SyncCurrentValueFromState();
                resolvedKeys.Add(TabOptimizeFinding.KeyOf(f));
            }
        }
        finally
        {
            _binding = false;
        }

        if (settings > 0)
            _uiDirty = true;

        return (settings, services, errors, resolvedKeys);
    }

    /// <summary>优化顾问：静默写入（无还原点/变更计划弹窗），按当前勾选与系统差量立即生效。</summary>
    /// <returns>ok=未中止；wrote=确有写入；errors=单项失败信息。</returns>
    internal (bool ok, bool wrote, List<string> errors) ApplyToSystemFromAdvisorSilent()
    {
        var errors = new List<string>();
        var savedStatus = _status.Text;
        _apply.Enabled = false;
        _restore.Enabled = false;
        try
        {
            // 自动登录新勾选但缺账户：静默跳过该项，避免弹窗
            var skipAutologon = false;
            if (_autologon.Checked)
            {
                var alreadyOn = _baselineState?.EnableAutologon == true;
                if (!alreadyOn && !HasAutologonCredentials() && !TryHydrateAutologonFromSystem())
                {
                    skipAutologon = true;
                    _autologon.Checked = false;
                }
            }

            SyncInvisibleRowsFromSystem();
            var target = CaptureState();
            if (skipAutologon && _baselineState is not null)
                target.EnableAutologon = _baselineState.EnableAutologon;

            var baseline = _baselineState;
            if (baseline is null)
            {
                try { baseline = Optimizer.Read(fullScan: false); }
                catch { /* keep null */ }
            }

            _status.Text = AppLang.L("正在写入系统…", "Writing to system…");
            Application.DoEvents();

            ApplyLog.BeginBatch(AppLang.L("优化顾问静默应用", "Advisor silent apply"));
            errors.AddRange(Optimizer.Apply(target, baseline));
            ApplyLog.WriteApply(AppLang.L("优化顾问应用到系统", "Advisor apply to system"), errors);
            OptimizationHistory.Add(
                AppLang.L("优化顾问", "Advisor"),
                AppLang.Lf("尝试 {0} 项，错误 {1}", "Attempted {0}, errors {1}", Optimizer.LastApplyActionCount, errors.Count));

            var wrote = Optimizer.LastApplyActionCount > 0;
            if (wrote)
            {
                _uiDirty = false;
                BindFromSystem(fullScan: false);
                if (!_autologon.Checked) _autologonSettings = null;
                RefreshAutologonDisplay();
            }

            _status.Text = wrote
                ? AppLang.Lf("已写入 {0} 项。", "Wrote {0} item(s).", Optimizer.LastApplyActionCount)
                : (errors.Count > 0
                    ? AppLang.L("写入未完成。", "Write incomplete.")
                    : AppLang.L("没有需要写入的更改。", "No changes to write."));
            return (true, wrote, errors);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            _status.Text = AppLang.L("操作失败：", "Operation failed: ") + ex.Message;
            return (false, false, errors);
        }
        finally
        {
            UpdateBottomActionEnablement();
            if (string.IsNullOrEmpty(_status.Text))
                _status.Text = savedStatus;
        }
    }

    /// <summary>按侧栏分组查找开关行（与诊断列表同源）。</summary>
    private SettingRow? FindSettingRowByItemTitle(string itemTitle)
    {
        foreach (var (_, sections) in _groups)
        {
            foreach (var (_, rows) in sections)
            {
                foreach (var row in rows)
                {
                    if (string.Equals(row.ItemText, itemTitle, StringComparison.Ordinal))
                        return row;
                }
            }
        }
        return null;
    }


    private void LoadState(bool fullScan = false, bool forceUi = false)
    {
        if (fullScan && forceUi)
            _status.Text = AppLang.L("正在完整扫描系统状态（含 DISM，可能需要数十秒）…", "Full scan in progress (includes DISM; may take a while)…");

        var epoch = ++_loadEpoch;
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var state = Optimizer.Read(fullScan);
                BeginInvoke(new Action(() =>
                {
                    if (epoch != _loadEpoch)
                        return;

                    try
                    {
                        if (_uiDirty && !forceUi)
                        {
                            // 保留用户勾选；后台扫描结果不写回开关
                            if (fullScan &&
                                !_status.Text.StartsWith(AppLang.L("读取当前配置失败", "Failed to read settings"), StringComparison.Ordinal) &&
                                _status.Text.IndexOf(AppLang.L("已载入预设", "Loaded preset"), StringComparison.Ordinal) < 0 &&
                                _status.Text.IndexOf(AppLang.L("已导入", "Imported"), StringComparison.Ordinal) < 0)
                            {
                                _status.Text = _systemFacts.Summary +
                                    AppLang.L(" · 后台扫描完成（已保留你的勾选；点「刷新」可对齐系统）。", " · Background scan done (kept your toggles; Refresh to sync).");
                            }
                            return;
                        }

                        Bind(state, updateCurrentValues: true);
                        _uiDirty = false;
                    }
                    catch (Exception ex) { _status.Text = AppLang.L("读取当前配置失败：", "Failed to read settings: ") + ex.Message; }
                    finally
                    {
                        UseWaitCursor = false;
                        Cursor = Cursors.Default;
                        Application.UseWaitCursor = false;
                        RefreshEmbeddedPageIfVisible();
                        if (fullScan && forceUi &&
                            !_status.Text.StartsWith(AppLang.L("读取当前配置失败", "Failed to read settings"), StringComparison.Ordinal))
                            _status.Text = _systemFacts.Summary + AppLang.L(" · 状态已刷新。", " · Status refreshed.");
                    }
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    if (epoch != _loadEpoch) return;
                    _status.Text = AppLang.L("读取当前配置失败：", "Failed to read settings: ") + ex.Message;
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
        _copyMoveTo.Checked = ContextMenuTweaks.IsCopyMoveToOn();
        _quickOps.Checked = ContextMenuTweaks.IsQuickOpsMenuOn();
        _takeOwn.SyncCurrentValueFromState();
        _openCmd.SyncCurrentValueFromState();
        _copyMoveTo.SyncCurrentValueFromState();
        _quickOps.SyncCurrentValueFromState();
    }

    private void Bind(Optimizer.State s, bool updateCurrentValues = false)
    {
        _binding = true;
        try
        {
            BindCore(s, updateCurrentValues);
        }
        finally
        {
            _binding = false;
        }
    }

    private void BindCore(Optimizer.State s, bool updateCurrentValues)
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
        _boostMode.Checked = s.ShowProcessorBoostMode;
        _hibernate.Checked = s.DisableHibernate;
        _neverSleep.Checked = s.NeverSleepOrScreenOff;
        _diskPerf.Checked = s.EnableDiskPerfCounters;
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
        _tbSearch.ChoiceIndex = s.TaskbarSearchMode is >= 0 and <= 2 ? s.TaskbarSearchMode : 1;
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
        _wuPauseUx.Checked = s.PauseWindowsUpdatesUx;
        _meltdown.Checked = s.DisableMeltdownSpectre;
        _hvci.Checked = s.DisableMemoryIntegrity;
        _wdac.Checked = s.DisableWdac;
        _vbs.Checked = s.DisableVbs;
        _bbr2.Checked = s.EnableTcpBbr2;
        _ctcp.Checked = s.EnableTcpCtcp;
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
        _notepadWrap.Checked = s.NotepadWordWrap;
        _notepadStatus.Checked = s.NotepadStatusBar;
        _cloudSearch.Checked = s.DisableCloudSearch;
        _langList.Checked = s.DisableWebsiteLangList;
        _trackApps.Checked = s.DisableAppLaunchTracking;
        _settingsSuggest.Checked = s.DisableSettingsSuggestions;
        _inking.Checked = s.DisableInkingPersonalization;
        _msPinyinEn.Checked = s.MsPinyinDefaultEnglish;
        _msPinyinCloud.Checked = s.DisableMsPinyinCloudAndInsights;
        _msPinyinBar.Checked = s.DisableMsPinyinToolbar;
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
        _mmcss.Checked = s.OptimizeMultimediaScheduler;
        _keyboardLatency.Checked = s.OptimizeKeyboardLatency;
        _webDavLimit.Checked = s.LiftWebDavFileSizeLimit;
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
        _copyMoveTo.Checked = s.ContextMenuCopyMoveTo;
        _quickOps.Checked = s.ContextMenuQuickOps;
        _wmpShare.Checked = s.DisableMediaPlayerSharing;
        _insider.Checked = s.DisableInsiderService;
        _storeUpd.Checked = s.DisableStoreAutoUpdate;
        _news.Checked = s.DisableNewsInterests;
        _noBrokenLnk.Checked = s.DisableBrokenShortcutTracking;
        _sepProcess.Checked = s.ExplorerSeparateProcess;
        _autoRestartShell.Checked = s.AutoRestartExplorer;
        _hideSpotlight.Checked = s.HideDesktopSpotlight;
        _noDupDrives.Checked = s.HideDuplicateRemovableDrives;
        _noRunMru.Checked = s.DisableRunDialogHistory;
        _mergeSvchost.Checked = s.MergeSvchostProcesses;
        _trkWks.Checked = s.DisableDistributedLinkTracking;
        _noLowDisk.Checked = s.DisableLowDiskSpaceChecks;
        _usbPowerOff.Checked = s.UsbFullPowerOff;
        _autoReboot.Checked = s.AutoRebootOnCrash;
        _cliTelemetry.Checked = s.DisableDotNetPowerShellTelemetry;
        _diagMinimal.Checked = s.DiagnosticDataMinimal;
        _noSigninReopen.Checked = s.DisableSigninReopen;
        _noSilentApps.Checked = s.DisableSilentAppInstall;
        _hideHomeGallery.Checked = s.HideExplorerHomeGallery;
        _noSnapAssist.Checked = s.DisableSnapAssist;
        _darkMode.Checked = s.EnableDarkMode;
        _noBitlockerAuto.Checked = s.DisableBitLockerAutoEncrypt;
        _noCompanionApps.Checked = s.PreventDeviceCompanionApps;
        _noUpdateAsap.Checked = s.DisableUpdateAsap;
        _hideSettingsHome.Checked = s.HideSettingsHomeAds;
        _extraAi.Checked = s.DisableWin11ExtraAi;
        _nullSess.Checked = s.RestrictNullSessionShares;
        _anonEnum.Checked = s.RestrictAnonymousEnum;
        _smbThrottle.Checked = s.DisableSmbBandwidthThrottling;
        _fastShutdown.Checked = s.FasterShutdown;
        _startupDelay.Checked = s.DisableStartupAppDelay;
        _menuDelay.Checked = s.InstantMenuShow;
        _aeroShake.Checked = s.DisableAeroShake;
        _netLocWizard.Checked = s.DisableNetworkLocationWizard;
        _settingSync.Checked = s.DisableSettingSync;
        _finishSetup.Checked = s.DisableFinishSetupSuggestions;
        _xferDetails.Checked = s.ExplorerTransferDetails;
        _telemTasks.Checked = s.DisableTelemetryScheduledTasks;
        _alwaysMenu.Checked = s.AlwaysShowMenus;
        _hideMerge.Checked = s.HideMergeConflicts;
        _compColor.Checked = s.ShowCompColor;
        _infoTip.Checked = s.ShowInfoTip;
        _statusBar.Checked = s.ShowStatusBar;
        _noPersistFold.Checked = s.DisablePersistBrowsers;
        _navExpand.Checked = s.NavPaneExpandCurrent;
        _noShareWiz.Checked = s.DisableSharingWizard;
        _driveLetters.ChoiceIndex = s.ShowDriveLettersMode is >= 0 and <= 2 ? s.ShowDriveLettersMode : 0;
        _folderGroup.ChoiceIndex = s.FolderGroupByMode is >= 0 and <= 4 ? s.FolderGroupByMode : 0;
        _folderSort.ChoiceIndex = s.FolderSortByMode is >= 0 and <= 5 ? s.FolderSortByMode : 0;
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
        // 已启用时回填域/用户到会话缓存，避免「应用到系统」误弹窗，并与基线一致
        if (s.EnableAutologon)
            TryHydrateAutologonFromSystem();
        else
            _autologonSettings = null;
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
        ShowProcessorBoostMode = _boostMode.Checked,
        DisableHibernate = _hibernate.Checked,
        NeverSleepOrScreenOff = _neverSleep.Checked,
        EnableDiskPerfCounters = _diskPerf.Checked,
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
        PauseWindowsUpdatesUx = _wuPauseUx.Checked,
        DisableMeltdownSpectre = _meltdown.Checked,
        DisableMemoryIntegrity = _hvci.Checked,
        DisableWdac = _wdac.Checked,
        DisableVbs = _vbs.Checked,
        EnableTcpBbr2 = _bbr2.Checked,
        EnableTcpCtcp = _ctcp.Checked,
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
        NotepadWordWrap = _notepadWrap.Checked,
        NotepadStatusBar = _notepadStatus.Checked,
        DisableCloudSearch = _cloudSearch.Checked,
        DisableWebsiteLangList = _langList.Checked,
        DisableAppLaunchTracking = _trackApps.Checked,
        DisableSettingsSuggestions = _settingsSuggest.Checked,
        DisableInkingPersonalization = _inking.Checked,
        MsPinyinDefaultEnglish = _msPinyinEn.Checked,
        DisableMsPinyinCloudAndInsights = _msPinyinCloud.Checked,
        DisableMsPinyinToolbar = _msPinyinBar.Checked,
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
        OptimizeMultimediaScheduler = _mmcss.Checked,
        OptimizeKeyboardLatency = _keyboardLatency.Checked,
        LiftWebDavFileSizeLimit = _webDavLimit.Checked,
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
        ContextMenuCopyMoveTo = _copyMoveTo.Checked,
        ContextMenuQuickOps = _quickOps.Checked,
        DisableMediaPlayerSharing = _wmpShare.Checked,
        DisableInsiderService = _insider.Checked,
        DisableStoreAutoUpdate = _storeUpd.Checked,
        DisableNewsInterests = _news.Checked,
        DisableBrokenShortcutTracking = _noBrokenLnk.Checked,
        ExplorerSeparateProcess = _sepProcess.Checked,
        AutoRestartExplorer = _autoRestartShell.Checked,
        HideDesktopSpotlight = _hideSpotlight.Checked,
        HideDuplicateRemovableDrives = _noDupDrives.Checked,
        DisableRunDialogHistory = _noRunMru.Checked,
        MergeSvchostProcesses = _mergeSvchost.Checked,
        DisableDistributedLinkTracking = _trkWks.Checked,
        DisableLowDiskSpaceChecks = _noLowDisk.Checked,
        UsbFullPowerOff = _usbPowerOff.Checked,
        AutoRebootOnCrash = _autoReboot.Checked,
        DisableDotNetPowerShellTelemetry = _cliTelemetry.Checked,
        DiagnosticDataMinimal = _diagMinimal.Checked,
        DisableSigninReopen = _noSigninReopen.Checked,
        DisableSilentAppInstall = _noSilentApps.Checked,
        HideExplorerHomeGallery = _hideHomeGallery.Checked,
        DisableSnapAssist = _noSnapAssist.Checked,
        EnableDarkMode = _darkMode.Checked,
        DisableBitLockerAutoEncrypt = _noBitlockerAuto.Checked,
        PreventDeviceCompanionApps = _noCompanionApps.Checked,
        DisableUpdateAsap = _noUpdateAsap.Checked,
        HideSettingsHomeAds = _hideSettingsHome.Checked,
        DisableWin11ExtraAi = _extraAi.Checked,
        RestrictNullSessionShares = _nullSess.Checked,
        RestrictAnonymousEnum = _anonEnum.Checked,
        DisableSmbBandwidthThrottling = _smbThrottle.Checked,
        FasterShutdown = _fastShutdown.Checked,
        DisableStartupAppDelay = _startupDelay.Checked,
        InstantMenuShow = _menuDelay.Checked,
        DisableAeroShake = _aeroShake.Checked,
        DisableNetworkLocationWizard = _netLocWizard.Checked,
        DisableSettingSync = _settingSync.Checked,
        DisableFinishSetupSuggestions = _finishSetup.Checked,
        ExplorerTransferDetails = _xferDetails.Checked,
        DisableTelemetryScheduledTasks = _telemTasks.Checked,
        AlwaysShowMenus = _alwaysMenu.Checked,
        HideMergeConflicts = _hideMerge.Checked,
        ShowCompColor = _compColor.Checked,
        ShowInfoTip = _infoTip.Checked,
        ShowStatusBar = _statusBar.Checked,
        DisablePersistBrowsers = _noPersistFold.Checked,
        NavPaneExpandCurrent = _navExpand.Checked,
        DisableSharingWizard = _noShareWiz.Checked,
        ShowDriveLettersMode = _driveLetters.ChoiceIndex is >= 0 and <= 2 ? _driveLetters.ChoiceIndex : 0,
        FolderGroupByMode = _folderGroup.ChoiceIndex is >= 0 and <= 4 ? _folderGroup.ChoiceIndex : 0,
        FolderSortByMode = _folderSort.ChoiceIndex is >= 0 and <= 5 ? _folderSort.ChoiceIndex : 0,
        TaskbarSearchMode = _tbSearch.ChoiceIndex is >= 0 and <= 2 ? _tbSearch.ChoiceIndex : 1,
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

        void SyncChoice(SettingRow row, int index)
        {
            if (!vis.Contains(row)) row.ChoiceIndex = index;
        }

        Sync(_sysMain, s.DisableSysMain);
        Sync(_hibernate, s.DisableHibernate);
        Sync(_neverSleep, s.NeverSleepOrScreenOff);
        Sync(_diskPerf, s.EnableDiskPerfCounters);
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
        SyncChoice(_tbSearch, s.TaskbarSearchMode is >= 0 and <= 2 ? s.TaskbarSearchMode : 1);
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
        Sync(_notepadWrap, s.NotepadWordWrap);
        Sync(_notepadStatus, s.NotepadStatusBar);
        Sync(_takeOwn, s.ContextMenuTakeOwnership);
        Sync(_openCmd, s.ContextMenuOpenCmd);
        Sync(_copyMoveTo, s.ContextMenuCopyMoveTo);
        Sync(_quickOps, s.ContextMenuQuickOps);
    }

    private void SetRowsChecked(IEnumerable<SettingRow> rows, bool on)
    {
        foreach (var row in rows) row.Checked = on;
    }

    private void RestoreDefaults()
    {
        var answer = MessageBox.Show(
            AppLang.L(
                "将把全部设置项恢复为 Windows Server 出厂默认值（「系统默认值」列）。\n\n" +
                "所有优化建议开关将关闭并立即写入系统。部分项目需注销或重启后生效。\n\n是否继续？",
                "Reset all items to Windows Server factory defaults (Default column).\n\nAll optimization toggles will turn off and write to the system. Some need sign-out/reboot.\n\nContinue?"),
            AppLang.L("恢复出厂默认", "Factory reset"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        SetAll(false);
        RunApply(AppLang.L("正在恢复出厂默认…", "Restoring factory defaults…"), AppLang.L("已恢复为出厂默认。", "Restored to factory defaults."));
    }

    private void RestoreGroup(string title, SettingRow[] rows)
    {
        var answer = MessageBox.Show(
            AppLang.Lf("将「{0}」分组内的 {1} 项恢复为出厂默认。\n\n是否立即写入系统？", "Reset {1} items in “{0}” to factory defaults.\n\nWrite to system now?", title, rows.Length),
            AppLang.L("恢复本组默认", "Reset section"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        SetRowsChecked(rows, false);
        RunApply(AppLang.Lf("正在恢复「{0}」…", "Resetting “{0}”…", title), AppLang.Lf("「{0}」已恢复为出厂默认。", "“{0}” restored to factory defaults.", title));
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
        if (!RunApply(AppLang.L("正在写入系统…", "Writing to system…"), AppLang.L("已写入本次改动。", "Changes written.")))
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
                ? AppLang.L("计算机名/工作组已修改，系统将在 60 秒后重启（命令行执行 shutdown /a 可取消）。", "Computer name/workgroup changed. Restart in 60s (run shutdown /a to cancel).")
                : AppLang.L("计算机名/工作组已修改，请自行选择合适时间重启以完全生效。", "Computer name/workgroup changed. Restart when ready for full effect.");
            _status.Text = msg;
            MessageBox.Show(this, msg, AppLang.L("修改成功", "Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("计算机名", "Computer name"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>询问并尝试创建还原点。Cancel=中止应用；跳过/失败不挡写入。</summary>
    private bool TryCreateRestorePointBeforeApply()
    {
        if (UiPrefs.Load().DisableRestorePointPrompt)
            return true;

        var ask = MessageBox.Show(this,
            AppLang.L("建议先创建系统还原点，出问题可以回退。\r\n\r\n是 = 创建后继续写入\r\n否 = 跳过还原点直接写入\r\n取消 = 不应用", "Create a restore point first?\r\n\r\nYes = create then apply\r\nNo = skip and apply\r\nCancel = abort"),
            AppLang.L("应用到系统", "Apply"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);
        if (ask == DialogResult.Cancel)
            return false;
        if (ask != DialogResult.Yes)
            return true;

        _status.Text = AppLang.L("正在创建系统还原点…", "Creating restore point…");
        Application.DoEvents();
        SystemRestoreHelper.TryCreate(AppLang.L("SrvDesk 应用前", "SrvDesk before apply"), out var msg);
        if (!string.IsNullOrEmpty(msg))
            _status.Text = msg;
        return true;
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
                _status.Text = AppLang.L("已取消：启用自动登录需先配置账户。", "Cancelled: configure Autologon account first.");
                return false;
            }

            if (!TryCreateRestorePointBeforeApply())
            {
                _status.Text = AppLang.L("已取消应用。", "Apply cancelled.");
                return false;
            }

            SyncInvisibleRowsFromSystem();
            var target = CaptureState();
            var baseline = _baselineState;
            if (baseline is null)
            {
                try { baseline = Optimizer.Read(fullScan: false); }
                catch { /* keep null */ }
            }

            var level = ServerProfile.Level;
            if (level == OptimizationLevel.DetectOnly)
            {
                var dry = ChangePlanBuilder.FromToggleDiff(target, baseline, level);
                dry.DryRunOnly = true;
                using (var planDlg = new ChangePlanDialog(dry))
                    planDlg.ShowDialog(this);
                _status.Text = AppLang.L("当前为「仅检测」等级：未写入系统。可在「服务器用途」调整等级。",
                    "Detect-only level: nothing written. Change level in Server profile.");
                return true;
            }

            if (!UiPrefs.Load().DisableChangePlanPrompt)
            {
                var plan = ChangePlanBuilder.FromToggleDiff(target, baseline, level);
                if (plan.Items.Count > 0)
                {
                    using var planDlg = new ChangePlanDialog(plan);
                    if (planDlg.ShowDialog(this) != DialogResult.OK || !planDlg.Confirmed)
                    {
                        _status.Text = AppLang.L("已取消应用。", "Apply cancelled.");
                        return false;
                    }
                    // 未勾选的开关项：把 target 拉回 baseline，避免写入
                    foreach (var it in plan.Items.Where(i => i.Key.StartsWith("toggle:", StringComparison.Ordinal) && !i.Selected))
                    {
                        var field = it.Key.Substring("toggle:".Length);
                        var fi = typeof(Optimizer.State).GetField(field);
                        if (fi is null || baseline is null) continue;
                        fi.SetValue(target, fi.GetValue(baseline));
                    }
                }
            }

            ApplyLog.BeginBatch(working);
            var errors = Optimizer.Apply(target, baseline);
            ApplyLog.WriteApply(working, errors);
            OptimizationHistory.Add(
                AppLang.L("应用到系统", "Apply to system"),
                AppLang.Lf("尝试 {0} 项，错误 {1}", "Attempted {0}, errors {1}", Optimizer.LastApplyActionCount, errors.Count));

            if (Optimizer.LastApplyActionCount == 0 && errors.Count == 0)
            {
                _status.Text = AppLang.L("没有需要写入的更改（相对上次同步未改动）。", "No changes to write (unchanged since last sync).");
                ok = true;
                return true;
            }

            _uiDirty = false;
            LoadState(fullScan: true, forceUi: true);
            if (!_autologon.Checked) _autologonSettings = null;
            RefreshAutologonDisplay();
            var changed = ApplyLog.LastBatchRealChangeCount;
            var attempted = Optimizer.LastApplyActionCount;
            _status.Text = errors.Count == 0
                ? AppLang.Lf("{0} 已写入 {1} 项（{2} 条变更）→ 帮助「变更日志」。", "{0} Wrote {1} item(s) ({2} change(s)) → Help → Change log.", success, attempted, changed)
                : AppLang.L("部分失败：\r\n", "Some failed:\r\n") + string.Join("\r\n", errors) +
                  AppLang.Lf("\r\n（本次仅改动项 {0}，已写入变更 {1} 条，见帮助 → 变更日志）", "\r\n({0} attempted, {1} real changes — see Help → Change log)", attempted, changed);
            ok = true;
        }
        catch (Exception ex)
        {
            _status.Text = AppLang.L("操作失败：", "Operation failed: ") + ex.Message;
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

    private void ShowSupportDialog()
    {
        using var dlg = new SupportDialog();
        dlg.ShowDialog(this);
    }

    private static SettingRow Row(string item, string systemDefault, SettingHelpInfo help) =>
        new(item, systemDefault, help);

    private static SettingRow Choice(
        string item,
        string systemDefault,
        SettingHelpInfo help,
        string[] options,
        int optimizedIndex) =>
        new(item, systemDefault, help, options, optimizedIndex);

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
        private readonly ComboBox? _choice;
        private readonly int _optimizedIndex;
        private readonly int _offIndex;
        private readonly SingleLineLabel _item;
        private readonly SingleLineLabel _scope;
        private readonly PictureBox _info;
        private readonly PictureBox _script;
        private readonly Label _level;
        private readonly SingleLineLabel _note;
        private readonly SingleLineLabel _system;
        private readonly SingleLineLabel _current;
        private Panel? _wrap;
        private Color _normalBg;

        public string ItemText { get; }
        public SettingHelpInfo Help { get; }
        public RecommendLevel EffectiveRecommend => Help.EffectiveRecommend(SystemInfoHelper.Detect());
        public Action<SettingRow>? OnCheckedChanged { get; set; }
        public bool HasChoice => _choice is not null;

        public SettingRow(string item, string systemDefault, SettingHelpInfo help)
            : this(item, systemDefault, help, null, 1)
        {
        }

        public SettingRow(
            string item,
            string systemDefault,
            SettingHelpInfo help,
            string[]? options,
            int optimizedIndex)
        {
            ItemText = item;
            Help = help;
            _optimizedIndex = optimizedIndex;
            _offIndex = optimizedIndex == 0 ? 1 : 0;
            _item = new SingleLineLabel
            {
                Text = item,
                Font = SettingListLayout.ItemFont,
                ForeColor = AppTheme.TextMain,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            _scope = new SingleLineLabel
            {
                Text = help.Scope.FormatBadges(),
                ForeColor = help.Scope.ServerOnly ? AppTheme.ScopeServer : AppTheme.ScopeTag,
                Font = SettingListLayout.ScopeFont,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                Visible = help.Scope.HasBadge,
                Cursor = Cursors.Hand,
            };
            _info = new PictureBox
            {
                Image = MenuIcons.RowInfo,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Size = UiScale.Size(18, 18),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            _script = new PictureBox
            {
                Image = MenuIcons.Script,
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Size = new Size(SettingListLayout.ScriptW, UiScale.S(20)),
            };
            _level = new Label
            {
                Text = "",
                AutoSize = false,
                ForeColor = RecommendLevelUi.StarOn,
                Font = UiFit.UiFont,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            // 实心星金色、空心星灰色：自绘，保证五星对比一眼可读
            _level.Paint += DrawRecommendStars;
            _note = new SingleLineLabel
            {
                Text = help.ListNote,
                ForeColor = AppTheme.TextMute,
                Font = SettingListLayout.NoteFont,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            _toggle = new ToggleSwitch();
            _toggle.CheckedChanged += (_, _) => OnCheckedChanged?.Invoke(this);
            if (options is { Length: > 0 })
            {
                _choice = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font(UiFit.UiFontFamily, 8.25F),
                    FlatStyle = FlatStyle.Flat,
                    IntegralHeight = false,
                    Cursor = Cursors.Hand,
                };
                foreach (var opt in options)
                    _choice.Items.Add(opt);
                // 先落在系统默认档，等 Bind(Optimizer.Read) 再改成实际当前值。
                // 切勿初始化为 optimizedIndex，否则打开瞬间会显示「已优化」。
                var init = _offIndex < 0 ? 0 : (_offIndex >= options.Length ? options.Length - 1 : _offIndex);
                _choice.SelectedIndex = init;
                _choice.SelectedIndexChanged += (_, _) =>
                {
                    SyncCurrentValueFromState();
                    OnCheckedChanged?.Invoke(this);
                };
            }
            _system = new SingleLineLabel
            {
                Text = systemDefault,
                Font = SettingListLayout.ItemFont,
                ForeColor = AppTheme.TextMute,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _current = new SingleLineLabel
            {
                Text = "—",
                Font = SettingListLayout.ItemFont,
                ForeColor = AppTheme.TextMain,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
        }

        public bool Checked
        {
            get => HasChoice ? ChoiceIndex == _optimizedIndex : _toggle.Checked;
            set
            {
                if (HasChoice)
                {
                    var target = value ? _optimizedIndex : _offIndex;
                    if (_choice!.SelectedIndex != target)
                        _choice.SelectedIndex = target < 0 ? 0 : (target >= _choice.Items.Count ? _choice.Items.Count - 1 : target);
                    else
                        SyncCurrentValueFromState();
                }
                else
                {
                    _toggle.Checked = value;
                }
            }
        }

        public int ChoiceIndex
        {
            get
            {
                if (_choice is null)
                    return _toggle.Checked ? _optimizedIndex : _offIndex;
                return _choice.SelectedIndex < 0 ? _offIndex : _choice.SelectedIndex;
            }
            set
            {
                if (_choice is null)
                {
                    Checked = value == _optimizedIndex;
                    return;
                }

                if (_choice.Items.Count == 0) return;
                var idx = value < 0 ? 0 : (value >= _choice.Items.Count ? _choice.Items.Count - 1 : value);
                if (_choice.SelectedIndex != idx)
                    _choice.SelectedIndex = idx;
                else
                    SyncCurrentValueFromState();
            }
        }

        public void SetSystemDefault(string text) => _system.Text = text;

        /// <summary>
        /// 「系统当前值」与「设置操作」同一含义：本机是否已按该项优化打开。
        /// 下拉显示选中文案；开关显示开启/关闭。不再用系统默认值反推功能本身。
        /// </summary>
        public void SyncCurrentValueFromState()
        {
            if (HasChoice)
            {
                var idx = ChoiceIndex;
                _current.Text = idx >= 0 && idx < _choice!.Items.Count
                    ? _choice.Items[idx]?.ToString() ?? "—"
                    : "—";
            }
            else
            {
                _current.Text = Checked ? AppLang.L("开启", "On") : AppLang.L("关闭", "Off");
            }

            _current.ForeColor = Checked ? AppTheme.PrimaryDark : AppTheme.TextMute;
        }

        public string CurrentValueText
        {
            get
            {
                SyncCurrentValueFromState();
                return _current.Text;
            }
        }

        public string RecommendedValueText
        {
            get
            {
                if (HasChoice && _choice is not null
                    && _optimizedIndex >= 0 && _optimizedIndex < _choice.Items.Count)
                {
                    return _choice.Items[_optimizedIndex]?.ToString()
                        ?? AppLang.L("开启", "On");
                }
                return AppLang.L("开启", "On");
            }
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
                || RecommendLevelUi.Title(EffectiveRecommend).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
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
            LayoutRow(_wrap.Height, width);
        }

        public void SetSelected(bool selected)
        {
            if (_wrap is null) return;
            _wrap.BackColor = selected ? AppTheme.PrimaryPale : _normalBg;
            _item.ForeColor = selected ? AppTheme.PrimaryDeep : AppTheme.TextMain;
            _wrap.Invalidate(true);
        }

        public void Mount(
            Control parent,
            int y,
            int h,
            Color bg,
            int width,
            ToolTip toolTip,
            Action<SettingRow> onSelectHelp,
            Action<SettingRow> onShowRecipe)
        {
            _normalBg = bg;
            var wrap = new BufferedPanel
            {
                Location = new Point(0, y),
                Size = new Size(width, h),
                BackColor = bg,
            };
            _wrap = wrap;
            LayoutRow(h, width);
            _level.Text = ""; // 由 Paint 画五星，实心金 / 空心灰
            _note.Text = Help.ListNote;

            var hasScope = Help.Scope.HasBadge;
            var tip = Help.Summary;
            if (hasScope) tip += "\r\n[" + Help.Scope.FormatBadges() + "]";
            toolTip.SetToolTip(_item, tip);
            toolTip.SetToolTip(_info, AppLang.L("查看说明与配置脚本\r\n", "View notes & config script\r\n") + tip);
            toolTip.SetToolTip(_script, AppLang.L("配置脚本：查看/编辑开启与关闭脚本", "Config script: view/edit on/off scripts"));
            toolTip.SetToolTip(_level, RecommendLevelUi.Tip(EffectiveRecommend));
            toolTip.SetToolTip(_note,
                (Help.WhenHint.Length > 0 ? AppLang.L("建议：", "When: ") + Help.WhenHint + "\r\n" : "") +
                (Help.UiPlace.Length > 0 ? AppLang.L("对应：", "Where: ") + Help.UiPlace : Help.ListNote));
            if (hasScope) toolTip.SetToolTip(_scope, Help.Scope.FormatHelpSection());
            toolTip.SetToolTip(_system, AppLang.L("系统默认值（出厂）", "Factory default"));
            toolTip.SetToolTip(_current, AppLang.L("系统当前值：与左侧设置操作一致（读取自本机）", "Current value: matches Action (read from this PC)"));
            if (HasChoice)
                toolTip.SetToolTip(_choice!, AppLang.L("下拉选择设置值", "Choose a value"));

            void Select(object? _, EventArgs __) => onSelectHelp(this);
            _item.Click += Select;
            _info.Click += Select;
            _level.Click += Select;
            _note.Click += Select;
            if (hasScope) _scope.Click += Select;
            _script.Click += (_, _) => onShowRecipe(this);

            wrap.Controls.Add(_current);
            wrap.Controls.Add(_system);
            wrap.Controls.Add(_level);
            wrap.Controls.Add(_note);
            wrap.Controls.Add(_info);
            wrap.Controls.Add(_item);
            if (hasScope) wrap.Controls.Add(_scope);
            wrap.Controls.Add(_script);
            if (HasChoice)
            {
                wrap.Controls.Add(_choice!);
                _choice!.BringToFront();
            }
            else
            {
                wrap.Controls.Add(_toggle);
                _toggle.BringToFront();
            }

            wrap.Controls.Add(new Panel
            {
                BackColor = AppTheme.BorderLight,
                Dock = DockStyle.Bottom,
                Height = 1,
            });
            parent.Controls.Add(wrap);
        }

        /// <summary>标题与适用范围分两行实测高度排布，避免叠字。</summary>
        private void LayoutRow(int h, int width)
        {
            width = Math.Max(SettingListLayout.NoteX + 80, width);
            var itemX = SettingListLayout.ItemX;
            var scriptX = SettingListLayout.ScriptX;
            var textW = Math.Max(120, scriptX - itemX - 4);
            var hasScope = Help.Scope.HasBadge;

            _info.SetBounds(SettingListLayout.InfoX, (h - UiScale.S(18)) / 2, UiScale.S(18), UiScale.S(18));
            if (hasScope)
            {
                var titleH = UiFit.LineHeight(_item.Font);
                var scopeH = UiFit.LineHeight(_scope.Font);
                var gap = SettingListLayout.TitleScopeGap;
                var block = titleH + gap + scopeH;
                var inner = Math.Max(titleH + gap + UiScale.S(12), h - UiScale.S(4));
                if (block > inner)
                {
                    var scale = (float)inner / block;
                    titleH = Math.Max(UiScale.S(14), (int)Math.Floor(titleH * scale));
                    scopeH = Math.Max(UiScale.S(12), inner - gap - titleH);
                    block = titleH + gap + scopeH;
                }

                var top = Math.Max(UiScale.S(2), (h - block) / 2);
                _item.SetBounds(itemX, top, textW, titleH);
                _scope.SetBounds(itemX, top + titleH + gap, textW, scopeH);
            }
            else
            {
                _item.SetBounds(itemX, 0, textW, h);
            }

            _script.SetBounds(scriptX, (h - UiScale.S(20)) / 2, SettingListLayout.ScriptW, UiScale.S(20));
            if (HasChoice)
            {
                _choice!.Size = new Size(SettingListLayout.ChoiceW, UiScale.S(24));
                _choice.Location = new Point(SettingListLayout.ChoiceX, (h - _choice.Height) / 2);
            }
            else
            {
                _toggle.Size = new Size(SettingListLayout.ToggleW, UiScale.S(28));
                _toggle.Location = new Point(SettingListLayout.ToggleX, (h - _toggle.Height) / 2);
            }

            _system.SetBounds(SettingListLayout.SystemX, 0, SettingListLayout.SystemW, h);
            _current.SetBounds(SettingListLayout.CurrentX, 0, SettingListLayout.CurrentW, h);
            _level.SetBounds(SettingListLayout.LevelX, 0, SettingListLayout.LevelW, h);
            var noteW = SettingListLayout.NoteWidthFor(width);
            _note.SetBounds(SettingListLayout.NoteX, 0, noteW, h);
        }

        private void DrawRecommendStars(object? sender, PaintEventArgs e)
        {
            if (sender is not Label label) return;
            var g = e.Graphics;

            // 透明标签需铺底，避免残影；几何星绘制保证不变形、不溢列
            var bg = label.BackColor.A == 255
                ? label.BackColor
                : (label.Parent?.BackColor ?? AppTheme.SurfaceCard);
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, label.ClientRectangle);

            RecommendLevelUi.DrawStarsInBounds(g, EffectiveRecommend, label.ClientRectangle, paddingLeft: 2);
        }
    }
}
