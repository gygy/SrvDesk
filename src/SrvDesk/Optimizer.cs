using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SrvDesk;

internal static class Optimizer
{
    internal const string IeEscAdmin = "{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string IeEscUser = "{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string ClsidMyComputer = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
    internal const string AzureArcCommand = @"%windir%\AzureArcSetup\Systray\AzureArcSysTray.exe";
    internal const string PowerPlanHighPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    internal const string PowerPlanBalanced = "381b4222-f694-41f0-9685-ff5bb260df2e";
    /// <summary>卓越性能（需 powercfg -duplicatescheme 后才出现于本机）。</summary>
    internal const string PowerPlanUltimate = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    internal const string IntlKey = @"Control Panel\International";
    internal const string ShortDateWithWeekday = "yyyy/MM/dd dddd";
    internal const string ShortDateDefault = "yyyy/M/d";
    internal const string QosPschedKey = @"SOFTWARE\Policies\Microsoft\Windows\Psched";
    internal const string QosPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\QoS";
    internal const string QosTcpAutotuningLevel = "Tcp Autotuning Level";
    /// <summary>处理器电源管理 → 处理器性能提升模式（PERFBOOSTMODE）定义键。</summary>
    internal const string ProcessorBoostModeKey =
        @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7";

    internal sealed class State
    {
        public bool CpuProgramPriority;
        public bool Dep;
        public bool DisableUac;
        public bool DisableIeEsc;
        public bool HighPerfPower;
        /// <summary>卓越性能电源计划（与 HighPerfPower 互斥；笔记本通常不推荐）。</summary>
        public bool UltimatePerfPower;
        public bool DisableTelemetry;
        public bool NoUpdateReboot;
        public bool DisableDeliveryOpt;
        public bool WuNotifyOnly;
        public bool DisableSysMain;
        public bool VisualBestPerf;
        public bool PowerThrottlingOff;
        /// <summary>在电源选项高级设置中显示「处理器性能提升模式」(PERFBOOSTMODE)。</summary>
        public bool ShowProcessorBoostMode;
        public bool DisableHibernate;
        /// <summary>关屏/睡眠/休眠超时均为 0（永不）。</summary>
        public bool NeverSleepOrScreenOff;
        /// <summary>diskperf -y：任务管理器可显示硬盘。</summary>
        public bool EnableDiskPerfCounters;
        public bool TcpOptimized;
        public bool QosSpeedOptimize;

        public bool DisableErrorReport;

        public bool ShowThisPcIcon;
        public bool LaunchExplorerThisPc;
        public bool SmallTaskbar;
        public bool ConfirmDelete;
        public bool EnableAudio;
        public bool ShowFileExtensions;
        public bool EnableThemes;
        public bool EnableSearch;
        public bool DisableWebSearch;
        public bool DisableFeedback;
        public bool NoLockScreen;

        public bool EnableRdp;
        public bool RdpGpuAccel;
        public bool RdpHighRefresh;
        public bool RdpDisableNla;
        // RemoteFX / AVC 增强（Reddit TurboRemoteFX）
        public bool RdpAvc444;
        public bool RdpAvcHwEncode;
        public bool RdpHwGraphicsFirst;
        public bool RdpRemoteFxGraphics;
        public bool RdpLowLatency;
        public bool RdpDisableWddm;
        /// <summary>多用户同时登录（同账号多会话 + MaxSessions + RDS-RD-Server）。</summary>
        public bool RdpMultiUserLogin;
        public bool EnableNetworkDiscovery;
        public bool DisableSmRemoting;

        public bool SkipServerManager;
        public bool HideServerManagerWacPrompt;
        public bool DisableAzureArc;
        public bool EnableInstaller;
        public bool EnableWia;

        public bool DisablePasswordComplexity;
        public bool PasswordNeverExpire;
        /// <summary>关闭「强制密码历史」：PasswordHistorySize / uniquepw = 0。</summary>
        public bool DisablePasswordHistory;
        public bool ShutdownWithoutLogon;
        public bool DisableShutdownReason;
        public bool DisableCad;

        public bool EnableAutologon;
        public string AutologonDomain = "";
        public string AutologonUser = "";
        public string AutologonPassword = "";
        public bool AutologonUpdatePassword = true;

        // 竞品高频：性能/安全加固（Dism++、VDOT、WPD）
        public bool LongPathsEnabled;
        public bool DisableFastStartup;
        public bool DisableAutoMaintenance;
        public bool ExcludeDriverUpdates;
        public bool DisableSmb1;
        public bool DisableRemoteRegistry;
        public bool DisablePrintSpooler;

        // 竞品高频：资源管理器/桌面
        public bool ShowHiddenFiles;
        public bool NoShortcutArrow;
        public bool ExplorerFullPath;
        public bool TaskbarAllIcons;
        public bool TaskbarClockWeekdaySeconds;

        // 竞品高频：隐私与体验
        public bool DisableAnimations;
        public bool DisableTransparency;
        public bool DisableTips;
        public bool DisableAutoplay;
        public bool DisableActivityHistory;
        public bool DisableStorageSense;

        public bool DisableSmartScreenWarning;
        public bool ShowControlPanelRecycleBin;
        public bool LargeSystemCacheOptimize;
        public bool DisableReservedStorage;
        public bool DisableSrvSplit;
        public bool EnableGpuHwScheduling;
        public bool DisableLoginKeyboardFilters;
        public bool DisableBackgroundApps;
        public bool ClassicFileSearch;
        public bool DisableSearchEngineFeature;
        public bool EnableDesktopMediaFeatures;
        public bool DisableServerBloatFeatures;

        public bool ShowItemCheckboxes;
        public bool ShowCommonFolders;
        public bool RemoveAdminShield;
        public bool NoShortcutSuffix;
        public bool Win11ExplorerStyle;
        public bool Win10ClassicContextMenu;
        public bool TaskbarSearchBox;
        public bool TaskbarAlignLeft;
        public bool TaskbarCombineAlways;
        public bool TaskbarAutoHide;
        public bool ShowTaskViewButton;
        public bool TaskbarEndTask;
        public bool DisableWidgets;
        public bool DisableSearchHighlights;
        /// <summary>Policies\\Windows\\Explorer DisableSearchBoxSuggestions=1：去除搜索框建议/信息流。</summary>
        public bool DisableSearchBoxSuggestions;
        public bool DisableRecommendedItems;
        public bool DisableAdTracking;
        public bool DisableSearchHistory;
        public bool DisableStickyKeys;
        public bool DisablePca;
        public bool PauseFeatureUpdatesUntil2035;
        /// <summary>通过 WindowsUpdate\\UX\\Settings 长期暂停功能+质量更新（至约 2099）。</summary>
        public bool PauseWindowsUpdatesUx;

        public bool HideProtectedOsFiles;
        public bool AlwaysShowIconsNeverThumbnails;
        public bool ShowEmptyDrives;
        public bool ShowRecentFiles = true;
        public bool ShowFrequentPlaces = true;
        public bool HideOfficeCloudFiles;
        public bool DisableOneDrive;
        public bool HideTaskbarChat;
        public bool HideTaskbarCopilot;
        public bool NotepadWordWrap;
        public bool NotepadStatusBar;
        public int TaskbarSearchMode = -1;

        public bool DisableCloudSearch;
        public bool DisableWebsiteLangList;
        public bool DisableAppLaunchTracking;
        public bool DisableSettingsSuggestions;
        public bool DisableInkingPersonalization;
        /// <summary>微软拼音新建文档/窗口默认英文键盘（需 Shift 切中文）。</summary>
        public bool MsPinyinDefaultEnglish;
        /// <summary>关闭云候选、输入见解、多语言/硬件键盘预测。</summary>
        public bool DisableMsPinyinCloudAndInsights;
        /// <summary>关闭拼音候选窗工具条，并隐藏语言栏帮助按钮。</summary>
        public bool DisableMsPinyinToolbar;
        public bool ExcludeMsrtFromWu;

        public bool DisableMeltdownSpectre;
        public bool DisableMemoryIntegrity;
        public bool DisableWdac;
        public bool DisableVbs;
        public bool EnableTcpBbr2;
        /// <summary>TCP 拥塞控制 = CTCP（与 BBR2 互斥）。</summary>
        public bool EnableTcpCtcp;
        public bool DisableSystemRestore;
        public bool DisableCeip;
        public bool DisableDiagnosticPolicy;

        public bool DisableRemoteAssistance;
        public bool DisableMemoryCompression;
        public bool DisableAppPrelaunch;
        public bool DisablePageCombining;
        public bool DisableUcpdDriver;

        public bool DisableCortana;
        public bool DisableCopilotAi;
        public bool DisableOfficeTelemetry;
        public bool EnableUtcTime;
        public bool DisableHpet;
        public bool EnableLoginVerbose;
        public bool DisableNetworkThrottling;
        public bool OptimizeMultimediaScheduler;
        public bool OptimizeKeyboardLatency;
        public bool LiftWebDavFileSizeLimit;
        public bool DisableGameDvr;
        public bool DisableLocationTracking;
        public bool DisableConsumerFeatures;
        public bool DisableEdgePreload;
        public bool DisableTeredo;
        public bool DisableClipboardCloud;
        public bool DisableNtfsLastAccess;
        public bool DisableXboxServices;
        public bool DisableFaxService;
        public bool EnableF8BootMenu;
        public bool ContextMenuTakeOwnership;
        public bool ContextMenuOpenCmd;
        public bool ContextMenuCopyMoveTo;
        public bool ContextMenuQuickOps;
        public bool DisableMediaPlayerSharing;
        public bool DisableInsiderService;
        public bool DisableStoreAutoUpdate;
        public bool DisableNewsInterests;
        public bool DisableBrokenShortcutTracking;
        public bool ExplorerSeparateProcess;
        public bool AutoRestartExplorer;
        public bool HideDesktopSpotlight;
        public bool HideDuplicateRemovableDrives;
        public bool DisableRunDialogHistory;
        public bool MergeSvchostProcesses;
        public bool DisableDistributedLinkTracking;
        public bool DisableLowDiskSpaceChecks;
        public bool UsbFullPowerOff;
        public bool AutoRebootOnCrash;
        public bool DisableDotNetPowerShellTelemetry;
        public bool DiagnosticDataMinimal;
        public bool DisableSigninReopen;
        public bool DisableSilentAppInstall;
        public bool HideExplorerHomeGallery;
        public bool DisableSnapAssist;
        public bool EnableDarkMode;
        public bool DisableBitLockerAutoEncrypt;
        public bool PreventDeviceCompanionApps;
        public bool DisableUpdateAsap;
        public bool HideSettingsHomeAds;
        public bool DisableWin11ExtraAi;
        // Atlas 对齐
        public bool RestrictNullSessionShares;
        public bool RestrictAnonymousEnum;
        public bool DisableSmbBandwidthThrottling;
        public bool FasterShutdown;
        public bool DisableStartupAppDelay;
        public bool InstantMenuShow;
        public bool DisableAeroShake;
        public bool DisableNetworkLocationWizard;
        public bool DisableSettingSync;
        public bool DisableFinishSetupSuggestions;
        public bool ExplorerTransferDetails;
        public bool DisableTelemetryScheduledTasks;
        // WinUtil QoL
        public bool ShowTrayBatteryPercent;
        public bool AlwaysShowScrollbars;
        public bool NumLockOnBoot;
        public bool DisableMouseAcceleration;
        public bool DisableWpbt;
        public bool Win11StartMenuPreviousLayout;
        public bool AlwaysShowMenus;
        public bool HideMergeConflicts = true;
        public bool ShowCompColor = true;
        public bool ShowInfoTip = true;
        public bool ShowStatusBar = true;
        public bool DisablePersistBrowsers = true;
        public bool NavPaneExpandCurrent;
        public bool DisableSharingWizard;
        public int ShowDriveLettersMode;
        public int FolderGroupByMode;
        public int FolderSortByMode;
    }

    public static bool IsWindowsServer()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key is null) return false;
        var type = key.GetValue("InstallationType") as string ?? "";
        var name = key.GetValue("ProductName") as string ?? "";
        return type.Equals("Server", StringComparison.OrdinalIgnoreCase)
            || name.IndexOf("Windows Server", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static State Read(bool fullScan = true)
    {
        if (fullScan)
            ServerDesktopTweaks.ResetDismCache();

        var account = ReadAccountPolicyFlags();

        var state = new State
        {
            CpuProgramPriority = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38),
            Dep = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DataExecutionPrevention_S4UEnable", 1),
            DisableUac = IsUacNeverNotify(),
            DisableIeEsc = DwordEquals(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscAdmin}", "IsInstalled", 0),
            HighPerfPower = false,
            UltimatePerfPower = false,
            DisableTelemetry = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0),
            NoUpdateReboot = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1),
            DisableDeliveryOpt = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 100),
            WuNotifyOnly = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", 2),
            DisableSysMain = ServiceStartEquals("SysMain", 4),
            VisualBestPerf = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2),
            PowerThrottlingOff = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1),
            ShowProcessorBoostMode = DwordEquals(Hive.HkLm, ProcessorBoostModeKey, "Attributes", 2),
            DisableHibernate = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0),
            NeverSleepOrScreenOff = IsNeverSleepOrScreenOff(),
            EnableDiskPerfCounters = IsDiskPerfEnabled(),
            TcpOptimized = IsTcpOptimized(),
            QosSpeedOptimize = IsQosSpeedOptimized(),
            DisableErrorReport = ServiceStartEquals("WerSvc", 4),

            ShowThisPcIcon = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", ClsidMyComputer, 0),
            LaunchExplorerThisPc = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1),
            SmallTaskbar = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", 1),
            ConfirmDelete = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete", 1),
            EnableAudio = ServiceStartEquals("AudioSrv", 2),
            ShowFileExtensions = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0),
            EnableThemes = ServiceStartEquals("Themes", 2),
            EnableSearch = ServiceStartEquals("WSearch", 2),
            DisableWebSearch = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1),
            DisableFeedback = DwordEquals(Hive.HkCu, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0),
            NoLockScreen = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", 1),

            EnableRdp = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0),
            RdpGpuAccel = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "UseAdvancedGraphics", 1),
            RdpHighRefresh = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations", "DWMFRAMEINTERVAL", 15),
            RdpDisableNla = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", 0),
            EnableNetworkDiscovery = ServiceStartEquals("fdPHost", 2) && ServiceStartEquals("FDResPub", 2),
            DisableSmRemoting = ServiceStartEquals("WinRM", 4),

            SkipServerManager =
                DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", 1)
                || DwordEquals(Hive.HkCu, @"Software\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", 1)
                || DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\ServerManager", "DoNotOpenAtLogon", 1),
            HideServerManagerWacPrompt = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotPopWACConsoleAtSMLaunch", 1),
            DisableAzureArc = GetValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup") is null,
            EnableInstaller = ServiceStartEquals("msiserver", 2),
            EnableWia = ServiceStartEquals("stisvc", 2),

            DisablePasswordComplexity = account.ComplexityOff,
            PasswordNeverExpire = account.NeverExpire,
            DisablePasswordHistory = account.HistoryOff,
            ShutdownWithoutLogon = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", 1),
            DisableShutdownReason = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonOn", 0),
            DisableCad = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", 1),

            LongPathsEnabled = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1),
            DisableFastStartup = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0),
            DisableAutoMaintenance = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", 1),
            ExcludeDriverUpdates = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1),
            DisableSmb1 = ServiceStartEquals("mrxsmb10", 4),
            DisableRemoteRegistry = ServiceStartEquals("RemoteRegistry", 4),
            DisablePrintSpooler = ServiceStartEquals("Spooler", 4),

            ShowHiddenFiles = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1),
            NoShortcutArrow = IsShortcutArrowRemoved(),
            ExplorerFullPath = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath", 1),
            TaskbarAllIcons = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableAutoTray", 0),
            TaskbarClockWeekdaySeconds = IsTaskbarClockEnhanced(),

            DisableAnimations = IsAnimationsDisabled(),
            DisableTransparency = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0),
            DisableTips = AreTipsDisabled(),
            DisableAutoplay = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 255),
            DisableActivityHistory = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowPublishUserActivities", 0),
            DisableStorageSense = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal", 0),

            DisableSmartScreenWarning = ServerDesktopTweaks.IsSmartScreenOff() && ServerDesktopTweaks.IsOpenFileWarningOff(),
            ShowControlPanelRecycleBin = ServerDesktopTweaks.IsControlPanelIconShown() && ServerDesktopTweaks.IsRecycleBinIconShown(),
            LargeSystemCacheOptimize = ServerDesktopTweaks.IsLargeSystemCacheOn(),
            DisableReservedStorage = ServerDesktopTweaks.IsReservedStorageOff(),
            DisableSrvSplit = ServerDesktopTweaks.IsSrvSplitDisabled(),
            EnableGpuHwScheduling = ServerDesktopTweaks.IsGpuHwSchedulingOn(),
            DisableLoginKeyboardFilters = ServerDesktopTweaks.IsLoginKeyboardFilterOff(),
            DisableBackgroundApps = ServerDesktopTweaks.IsBackgroundAppsOff(),
            ClassicFileSearch = ServerDesktopTweaks.IsClassicSearchOn(),
            DisableSearchEngineFeature = fullScan
                ? ServerDesktopTweaks.IsSearchEngineFeatureOff()
                : ServiceStartEquals("WSearch", 4),
            EnableDesktopMediaFeatures = fullScan && ServerDesktopTweaks.IsDesktopMediaFeaturesOn(),
            DisableServerBloatFeatures = fullScan && ServerDesktopTweaks.IsServerBloatFeaturesOff(includeRsatScan: fullScan),

            ShowItemCheckboxes = Win11DesktopTweaks.IsShowItemCheckboxesOn(),
            ShowCommonFolders = Win11DesktopTweaks.IsShowCommonFoldersOn(),
            RemoveAdminShield = Win11DesktopTweaks.IsRemoveAdminShieldOn(),
            NoShortcutSuffix = Win11DesktopTweaks.IsNoShortcutSuffixOn(),
            Win11ExplorerStyle = Win11DesktopTweaks.IsWin11ExplorerStyleOn(),
            Win10ClassicContextMenu = Win11DesktopTweaks.IsWin10ClassicContextMenuOn(),
            TaskbarSearchBox = Win11DesktopTweaks.IsTaskbarSearchBoxOn(),
            TaskbarAlignLeft = Win11DesktopTweaks.IsTaskbarAlignLeftOn(),
            TaskbarCombineAlways = Win11DesktopTweaks.IsTaskbarCombineAlwaysOn(),
            TaskbarAutoHide = Win11DesktopTweaks.IsTaskbarAutoHideOn(),
            ShowTaskViewButton = Win11DesktopTweaks.IsShowTaskViewButtonOn(),
            TaskbarEndTask = Win11DesktopTweaks.IsTaskbarEndTaskOn(),
            DisableWidgets = Win11DesktopTweaks.IsDisableWidgetsOn(),
            DisableSearchHighlights = Win11DesktopTweaks.IsDisableSearchHighlightsOn(),
            DisableSearchBoxSuggestions = Win11DesktopTweaks.IsDisableSearchBoxSuggestionsOn(),
            DisableRecommendedItems = Win11DesktopTweaks.IsDisableRecommendedItemsOn(),
            DisableAdTracking = Win11DesktopTweaks.IsDisableAdTrackingOn(),
            DisableSearchHistory = Win11DesktopTweaks.IsDisableSearchHistoryOn(),
            DisableStickyKeys = Win11DesktopTweaks.IsDisableStickyKeysOn(),
            DisablePca = ServiceStartEquals("PcaSvc", 4),
            PauseFeatureUpdatesUntil2035 = Win11DesktopTweaks.IsPauseFeatureUpdatesUntil2035On(),
            PauseWindowsUpdatesUx = Win11DesktopTweaks.IsPauseWindowsUpdatesUxOn(),
            TaskbarSearchMode = -1,
        };
        EasySettingsTweaks.ReadInto(state);
        CompetitorTweaks.ReadInto(state);
        CommunityTweaks.ReadInto(state);
        SophiaGapTweaks.ReadInto(state);
        AtlasGapTweaks.ReadInto(state);
        WinUtilGapTweaks.ReadInto(state);
        RemoteFxTweaks.ReadInto(state);
        RdpMultiUserTweaks.ReadInto(state, fullScan);
        ReadPowerPlanInto(state);
        FolderViewTweaks.ReadInto(state);
        var auto = AutologonHelper.Read();
        state.EnableAutologon = auto.Enabled;
        if (auto.Enabled)
        {
            state.AutologonDomain = auto.Domain;
            state.AutologonUser = auto.Username;
            state.AutologonUpdatePassword = false;
        }
        var searchMode = EasySettingsTweaks.GetSearchboxMode();
        if (searchMode is 0 or 1 or 2)
            state.TaskbarSearchMode = searchMode;
        else
            state.TaskbarSearchMode = state.TaskbarSearchBox ? 2 : 1;
        state.TaskbarSearchBox = state.TaskbarSearchMode == 2;
        return state;
    }

    /// <summary>上次 Apply 实际尝试写入的项数（相对 baseline 有差异才会计入；baseline 为 null 则全部写入）。</summary>
    public static int LastApplyActionCount { get; private set; }

    /// <param name="baseline">为 null 时写入全部；否则只写入与 baseline 不同的项（本次改过的开关）。</param>
    public static List<string> Apply(State s, State? baseline = null)
    {
        // 整次应用合并资源管理器重启：只闪一次任务栏，避免「不断抖动」
        using (DesktopQuickActions.DeferRestarts())
            return ApplyCore(s, baseline);
    }

    private static List<string> ApplyCore(State s, State? baseline)
    {
        var errors = new List<string>();
        LastApplyActionCount = 0;
        ApplyLog.DebugStateDiff("应用到系统 · 相对基线的字段差分", baseline, s);

        bool Ch(Func<State, bool> f) => baseline is null || f(baseline) != f(s);
        bool ChS(Func<State, string> f) =>
            baseline is null || !string.Equals(f(baseline), f(s), StringComparison.Ordinal);

        void Do(bool need, string name, Action action)
        {
            if (!need)
            {
                ApplyLog.DebugItem(name, "跳过", "相对基线无差异");
                return;
            }

            ApplyLog.DebugItem(name, "开始写入",
                baseline is null ? "无基线（全量评估中的一项）" : "相对基线有差异");
            LastApplyActionCount++;
            var before = ApplyLog.CurrentBatchRealChanges;
            Try(errors, name, action);
            var delta = ApplyLog.CurrentBatchRealChanges - before;
            ApplyLog.DebugItem(name, "结束",
                errors.Count > 0 && errors[errors.Count - 1].StartsWith(name + "：", StringComparison.Ordinal)
                    ? "本项失败"
                    : $"本项实际变更 {delta} 条");
        }

        Do(Ch(x => x.CpuProgramPriority), "CPU资源分配", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", s.CpuProgramPriority ? 38 : 2));
        Do(Ch(x => x.Dep), "DEP", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DataExecutionPrevention_S4UEnable", s.Dep ? 1 : 0));
        Do(Ch(x => x.DisableUac), "UAC从不通知", () => SetUacNeverNotify(s.DisableUac));
        Do(Ch(x => x.DisableIeEsc), "IE增强安全", () =>
        {
            SetDword(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscAdmin}", "IsInstalled", s.DisableIeEsc ? 0 : 1);
            SetDword(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscUser}", "IsInstalled", s.DisableIeEsc ? 0 : 1);
        });
        Do(Ch(x => x.HighPerfPower) || Ch(x => x.UltimatePerfPower), "电源计划", () => ApplyPowerPlan(s));
        Do(Ch(x => x.DisableTelemetry), "遥测", () => SetTelemetry(!s.DisableTelemetry));
        Do(Ch(x => x.NoUpdateReboot), "更新重启", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", s.NoUpdateReboot ? 1 : 0));
        Do(Ch(x => x.DisableDeliveryOpt), "传递优化", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", s.DisableDeliveryOpt ? 100 : 1));
        Do(Ch(x => x.WuNotifyOnly), "更新通知", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", s.WuNotifyOnly ? 2 : 4));
        Do(Ch(x => x.DisableSysMain), "SysMain", () => SetService("SysMain", !s.DisableSysMain, disableWhenOff: true));
        Do(Ch(x => x.VisualBestPerf), "视觉效果", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", s.VisualBestPerf ? 2 : 3));
        Do(Ch(x => x.PowerThrottlingOff), "电源节流", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", s.PowerThrottlingOff ? 1 : 0));
        Do(Ch(x => x.ShowProcessorBoostMode), "处理器提升模式可见", () =>
            SetDword(Hive.HkLm, ProcessorBoostModeKey, "Attributes", s.ShowProcessorBoostMode ? 2 : 1));
        Do(Ch(x => x.DisableHibernate), "休眠", () => SetHibernate(!s.DisableHibernate));
        Do(Ch(x => x.NeverSleepOrScreenOff), "关屏与睡眠超时", () => SetNeverSleepOrScreenOff(s.NeverSleepOrScreenOff));
        Do(Ch(x => x.EnableDiskPerfCounters), "任务管理器硬盘", () => SetDiskPerfEnabled(s.EnableDiskPerfCounters));
        Do(Ch(x => x.TcpOptimized), "TCP优化", () => SetTcpOptimized(s.TcpOptimized));
        Do(Ch(x => x.QosSpeedOptimize), "QoS网速", () => SetQosSpeedOptimized(s.QosSpeedOptimize));
        Do(Ch(x => x.DisableErrorReport), "错误报告", () => SetService("WerSvc", !s.DisableErrorReport, disableWhenOff: true));

        Do(Ch(x => x.ShowThisPcIcon), "桌面此电脑", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", ClsidMyComputer, s.ShowThisPcIcon ? 0 : 1));
        Do(Ch(x => x.LaunchExplorerThisPc), "打开此电脑", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", s.LaunchExplorerThisPc ? 1 : 2));
        Do(Ch(x => x.SmallTaskbar), "小按钮任务栏", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", s.SmallTaskbar ? 1 : 0));
        Do(Ch(x => x.ConfirmDelete), "删除确认", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete", s.ConfirmDelete ? 1 : 0));
        Do(Ch(x => x.EnableAudio), "音频服务", () => SetAudio(s.EnableAudio));
        Do(Ch(x => x.ShowFileExtensions), "文件扩展名", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", s.ShowFileExtensions ? 0 : 1));
        Do(Ch(x => x.EnableThemes), "主题服务", () => SetService("Themes", s.EnableThemes, disableWhenOff: false));
        Do(Ch(x => x.DisableSearchEngineFeature) || Ch(x => x.EnableSearch), "Windows搜索", () =>
        {
            if (s.DisableSearchEngineFeature)
                ServerDesktopTweaks.ApplySearchEngineFeature(true);
            else
                SetService("WSearch", s.EnableSearch, disableWhenOff: false);
        });
        Do(Ch(x => x.DisableWebSearch), "Bing搜索", () =>
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", s.DisableWebSearch ? 1 : 0);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", s.DisableWebSearch ? 0 : 1);
        });
        Do(Ch(x => x.DisableFeedback), "体验反馈", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", s.DisableFeedback ? 0 : 1));
        Do(Ch(x => x.NoLockScreen), "锁屏", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", s.NoLockScreen ? 1 : 0));

        Do(Ch(x => x.EnableRdp), "远程桌面", () => SetRdp(s.EnableRdp));
        Do(Ch(x => x.RdpMultiUserLogin), "多用户同时登录", () => RdpMultiUserTweaks.Apply(s.RdpMultiUserLogin));
        Do(Ch(x => x.RdpGpuAccel), "RDP图形加速", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "UseAdvancedGraphics", s.RdpGpuAccel ? 1 : 0));
        Do(Ch(x => x.RdpHighRefresh), "RDP帧率", () =>
        {
            if (s.RdpHighRefresh)
                SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations", "DWMFRAMEINTERVAL", 15);
            else
                DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations", "DWMFRAMEINTERVAL");
        });
        Do(Ch(x => x.RdpDisableNla), "RDP NLA", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", s.RdpDisableNla ? 0 : 1));
        Do(Ch(x => x.EnableNetworkDiscovery), "网络发现", () => SetNetworkDiscovery(s.EnableNetworkDiscovery));
        Do(Ch(x => x.DisableSmRemoting), "Server远程管理", () => SetSmRemoting(!s.DisableSmRemoting));

        Do(Ch(x => x.SkipServerManager), "服务管理器", () => SetServerManager(s.SkipServerManager));
        Do(Ch(x => x.HideServerManagerWacPrompt), "WAC推广提示", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotPopWACConsoleAtSMLaunch",
                s.HideServerManagerWacPrompt ? 1 : 0));
        Do(Ch(x => x.DisableAzureArc), "Azure Arc", () =>
        {
            if (s.DisableAzureArc)
                DeleteValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup");
            else
                SetString(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup", AzureArcCommand);
        });
        Do(Ch(x => x.EnableInstaller), "Windows Installer", () => SetService("msiserver", s.EnableInstaller, disableWhenOff: false));
        Do(Ch(x => x.EnableWia), "WIA图像采集", () => SetService("stisvc", s.EnableWia, disableWhenOff: false));

        Do(Ch(x => x.DisablePasswordComplexity) || Ch(x => x.PasswordNeverExpire) || Ch(x => x.DisablePasswordHistory),
            "账户策略", () =>
            ApplyAccountPolicy(s.DisablePasswordComplexity, s.PasswordNeverExpire, s.DisablePasswordHistory));

        Do(Ch(x => x.ShutdownWithoutLogon), "未登录关机", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", s.ShutdownWithoutLogon ? 1 : 0));
        Do(Ch(x => x.DisableShutdownReason), "关机事件跟踪", () => SetShutdownReason(!s.DisableShutdownReason));
        Do(Ch(x => x.DisableCad), "Ctrl+Alt+Del", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", s.DisableCad ? 1 : 0));

        Do(Ch(x => x.EnableAutologon)
           || (s.EnableAutologon && (
               ChS(x => x.AutologonDomain) || ChS(x => x.AutologonUser)
               || Ch(x => x.AutologonUpdatePassword)
               || (s.AutologonUpdatePassword && ChS(x => x.AutologonPassword)))),
            "自动登录", () =>
            {
                if (s.EnableAutologon)
                {
                    AutologonHelper.Enable(new AutologonSettings
                    {
                        Domain = s.AutologonDomain,
                        Username = s.AutologonUser,
                        Password = s.AutologonPassword,
                        UpdatePassword = s.AutologonUpdatePassword,
                    });
                }
                else
                {
                    AutologonHelper.Disable();
                }
            });

        Do(Ch(x => x.LongPathsEnabled), "长路径支持", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", s.LongPathsEnabled ? 1 : 0));
        Do(Ch(x => x.DisableFastStartup), "快速启动", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", s.DisableFastStartup ? 0 : 1));
        Do(Ch(x => x.DisableAutoMaintenance), "自动维护", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", s.DisableAutoMaintenance ? 1 : 0));
        Do(Ch(x => x.ExcludeDriverUpdates), "驱动自动更新", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", s.ExcludeDriverUpdates ? 1 : 0));
        Do(Ch(x => x.DisableSmb1), "SMB1 协议", () => SetSmb1(!s.DisableSmb1));
        Do(Ch(x => x.DisableRemoteRegistry), "Remote Registry", () => SetService("RemoteRegistry", !s.DisableRemoteRegistry, disableWhenOff: true));
        Do(Ch(x => x.DisablePrintSpooler), "打印后台处理", () => SetService("Spooler", !s.DisablePrintSpooler, disableWhenOff: true));

        Do(Ch(x => x.ShowHiddenFiles), "显示隐藏文件", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", s.ShowHiddenFiles ? 1 : 2));
        Do(Ch(x => x.NoShortcutArrow), "快捷方式箭头", () => SetShortcutArrow(!s.NoShortcutArrow));
        Do(Ch(x => x.ExplorerFullPath), "标题栏完整路径", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath", s.ExplorerFullPath ? 1 : 0));
        Do(Ch(x => x.TaskbarAllIcons), "任务栏全部图标", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableAutoTray", s.TaskbarAllIcons ? 0 : 1));
        Do(Ch(x => x.TaskbarClockWeekdaySeconds), "任务栏时钟", () => SetTaskbarClockEnhanced(s.TaskbarClockWeekdaySeconds));

        Do(Ch(x => x.DisableAnimations), "窗口动画", () => SetAnimations(!s.DisableAnimations));
        Do(Ch(x => x.DisableTransparency), "透明效果", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", s.DisableTransparency ? 0 : 1));
        Do(Ch(x => x.DisableTips), "Windows 提示", () => SetTips(!s.DisableTips));
        Do(Ch(x => x.DisableAutoplay), "自动播放", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", s.DisableAutoplay ? 255 : 145));
        Do(Ch(x => x.DisableActivityHistory), "活动历史", () => SetActivityHistory(!s.DisableActivityHistory));
        Do(Ch(x => x.DisableStorageSense), "存储感知", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal", s.DisableStorageSense ? 0 : 1));

        Do(Ch(x => x.DisableSmartScreenWarning), "SmartScreen与打开警告", () => ServerDesktopTweaks.ApplySmartScreenAndOpenWarning(s.DisableSmartScreenWarning));
        Do(Ch(x => x.ShowControlPanelRecycleBin), "控制面板与回收站", () => ServerDesktopTweaks.ApplyDesktopIcons(s.ShowControlPanelRecycleBin));
        Do(Ch(x => x.LargeSystemCacheOptimize), "大系统缓存", () => ServerDesktopTweaks.ApplyLargeSystemCache(s.LargeSystemCacheOptimize));
        Do(Ch(x => x.DisableReservedStorage), "保留存储", () => ServerDesktopTweaks.ApplyReservedStorage(s.DisableReservedStorage));
        Do(Ch(x => x.DisableSrvSplit), "LanmanServer拆分", () => ServerDesktopTweaks.ApplySrvSplitThreshold(s.DisableSrvSplit));
        Do(Ch(x => x.EnableGpuHwScheduling), "GPU硬件调度", () => ServerDesktopTweaks.ApplyGpuHwScheduling(s.EnableGpuHwScheduling));
        Do(Ch(x => x.DisableLoginKeyboardFilters), "登录键盘筛选", () => ServerDesktopTweaks.ApplyLoginKeyboardFilters(s.DisableLoginKeyboardFilters));
        Do(Ch(x => x.DisableBackgroundApps), "后台应用", () => ServerDesktopTweaks.ApplyBackgroundApps(s.DisableBackgroundApps));
        Do(Ch(x => x.ClassicFileSearch), "传统搜索", () => ServerDesktopTweaks.ApplyClassicSearch(s.ClassicFileSearch));
        Do(Ch(x => x.EnableDesktopMediaFeatures), "桌面媒体组件", () => ServerDesktopTweaks.ApplyDesktopMediaFeatures(s.EnableDesktopMediaFeatures));
        Do(Ch(x => x.DisableServerBloatFeatures), "Server冗余组件", () => ServerDesktopTweaks.ApplyServerBloatFeatures(s.DisableServerBloatFeatures));
        Do(AnyWin11DesktopChanged(baseline, s), "Win11桌面体验", () =>
        {
            Win11DesktopTweaks.Apply(s, baseline);
            if (Win11DesktopTweaks.NeedsExplorerRestart(baseline, s))
                DesktopQuickActions.RestartExplorer();
        });
        Do(Ch(x => x.DisablePca), "程序兼容性助手", () => SetService("PcaSvc", !s.DisablePca, disableWhenOff: true));
        Do(AnyEasySettingsChanged(baseline, s), "轻松设置扩展项", () => EasySettingsTweaks.Apply(s, baseline));
        Do(AnyCompetitorChanged(baseline, s), "竞品常用项", () => CompetitorTweaks.Apply(s, baseline));
        Do(CommunityTweaks.AnyChanged(baseline, s), "社区对齐项", () => CommunityTweaks.Apply(s, baseline));
        Do(SophiaGapTweaks.AnyChanged(baseline, s), "Sophia对齐项", () => SophiaGapTweaks.Apply(s, baseline));
        Do(AtlasGapTweaks.AnyChanged(baseline, s), "Atlas对齐项", () => AtlasGapTweaks.Apply(s, baseline));
        Do(WinUtilGapTweaks.AnyChanged(baseline, s), "WinUtil对齐项", () => WinUtilGapTweaks.Apply(s, baseline));
        Do(RemoteFxTweaks.AnyChanged(baseline, s), "RemoteFX/AVC增强", () => RemoteFxTweaks.Apply(s, baseline));
        Do(FolderViewTweaks.AnyChanged(baseline, s), "文件夹选项", () => FolderViewTweaks.Apply(s, baseline));
        ApplyLog.Debug($"本批次计划写入优化项数：{LastApplyActionCount}；截至组写入前累计变更条数以各优化项结束日志为准");
        return errors;
    }

    private static bool AnyWin11DesktopChanged(State? b, State s)
    {
        if (b is null) return true;
        return b.ShowItemCheckboxes != s.ShowItemCheckboxes
            || b.ShowCommonFolders != s.ShowCommonFolders
            || b.RemoveAdminShield != s.RemoveAdminShield
            || b.NoShortcutSuffix != s.NoShortcutSuffix
            || b.Win11ExplorerStyle != s.Win11ExplorerStyle
            || b.Win10ClassicContextMenu != s.Win10ClassicContextMenu
            || b.TaskbarSearchBox != s.TaskbarSearchBox
            || b.TaskbarSearchMode != s.TaskbarSearchMode
            || b.TaskbarAlignLeft != s.TaskbarAlignLeft
            || b.TaskbarCombineAlways != s.TaskbarCombineAlways
            || b.TaskbarAutoHide != s.TaskbarAutoHide
            || b.ShowTaskViewButton != s.ShowTaskViewButton
            || b.TaskbarEndTask != s.TaskbarEndTask
            || b.DisableWidgets != s.DisableWidgets
            || b.DisableSearchHighlights != s.DisableSearchHighlights
            || b.DisableSearchBoxSuggestions != s.DisableSearchBoxSuggestions
            || b.DisableRecommendedItems != s.DisableRecommendedItems
            || b.DisableAdTracking != s.DisableAdTracking
            || b.DisableSearchHistory != s.DisableSearchHistory
            || b.DisableStickyKeys != s.DisableStickyKeys;
    }

    private static bool AnyEasySettingsChanged(State? b, State s)
    {
        if (b is null) return true;
        return b.HideProtectedOsFiles != s.HideProtectedOsFiles
            || b.AlwaysShowIconsNeverThumbnails != s.AlwaysShowIconsNeverThumbnails
            || b.ShowEmptyDrives != s.ShowEmptyDrives
            || b.ShowRecentFiles != s.ShowRecentFiles
            || b.ShowFrequentPlaces != s.ShowFrequentPlaces
            || b.HideOfficeCloudFiles != s.HideOfficeCloudFiles
            || b.DisableOneDrive != s.DisableOneDrive
            || b.HideTaskbarChat != s.HideTaskbarChat
            || b.HideTaskbarCopilot != s.HideTaskbarCopilot
            || b.NotepadWordWrap != s.NotepadWordWrap
            || b.NotepadStatusBar != s.NotepadStatusBar
            || b.DisableCloudSearch != s.DisableCloudSearch
            || b.DisableWebSearch != s.DisableWebSearch
            || b.DisableSearchHistory != s.DisableSearchHistory
            || b.DisableWebsiteLangList != s.DisableWebsiteLangList
            || b.DisableAppLaunchTracking != s.DisableAppLaunchTracking
            || b.DisableSettingsSuggestions != s.DisableSettingsSuggestions
            || b.DisableInkingPersonalization != s.DisableInkingPersonalization
            || b.MsPinyinDefaultEnglish != s.MsPinyinDefaultEnglish
            || b.DisableMsPinyinCloudAndInsights != s.DisableMsPinyinCloudAndInsights
            || b.DisableMsPinyinToolbar != s.DisableMsPinyinToolbar
            || b.DisableAdTracking != s.DisableAdTracking
            || b.DisableDeliveryOpt != s.DisableDeliveryOpt
            || b.ExcludeMsrtFromWu != s.ExcludeMsrtFromWu
            || b.PauseFeatureUpdatesUntil2035 != s.PauseFeatureUpdatesUntil2035
            || b.PauseWindowsUpdatesUx != s.PauseWindowsUpdatesUx
            || b.DisableMeltdownSpectre != s.DisableMeltdownSpectre
            || b.DisableMemoryIntegrity != s.DisableMemoryIntegrity
            || b.DisableWdac != s.DisableWdac
            || b.DisableVbs != s.DisableVbs
            || b.EnableTcpBbr2 != s.EnableTcpBbr2
            || b.EnableTcpCtcp != s.EnableTcpCtcp
            || b.DisableSystemRestore != s.DisableSystemRestore
            || b.DisableCeip != s.DisableCeip
            || b.DisableDiagnosticPolicy != s.DisableDiagnosticPolicy
            || b.DisableRemoteAssistance != s.DisableRemoteAssistance
            || b.DisableMemoryCompression != s.DisableMemoryCompression
            || b.DisableAppPrelaunch != s.DisableAppPrelaunch
            || b.DisablePageCombining != s.DisablePageCombining
            || b.DisableUcpdDriver != s.DisableUcpdDriver;
    }

    private static bool AnyCompetitorChanged(State? b, State s)
    {
        if (b is null) return true;
        return b.DisableCortana != s.DisableCortana
            || b.DisableCopilotAi != s.DisableCopilotAi
            || b.DisableOfficeTelemetry != s.DisableOfficeTelemetry
            || b.EnableUtcTime != s.EnableUtcTime
            || b.DisableHpet != s.DisableHpet
            || b.EnableLoginVerbose != s.EnableLoginVerbose
            || b.DisableNetworkThrottling != s.DisableNetworkThrottling
            || b.OptimizeMultimediaScheduler != s.OptimizeMultimediaScheduler
            || b.OptimizeKeyboardLatency != s.OptimizeKeyboardLatency
            || b.LiftWebDavFileSizeLimit != s.LiftWebDavFileSizeLimit
            || b.DisableGameDvr != s.DisableGameDvr
            || b.DisableLocationTracking != s.DisableLocationTracking
            || b.DisableConsumerFeatures != s.DisableConsumerFeatures
            || b.DisableEdgePreload != s.DisableEdgePreload
            || b.DisableTeredo != s.DisableTeredo
            || b.DisableClipboardCloud != s.DisableClipboardCloud
            || b.DisableNtfsLastAccess != s.DisableNtfsLastAccess
            || b.DisableXboxServices != s.DisableXboxServices
            || b.DisableFaxService != s.DisableFaxService
            || b.EnableF8BootMenu != s.EnableF8BootMenu
            || b.ContextMenuTakeOwnership != s.ContextMenuTakeOwnership
            || b.ContextMenuOpenCmd != s.ContextMenuOpenCmd
            || b.ContextMenuCopyMoveTo != s.ContextMenuCopyMoveTo
            || b.ContextMenuQuickOps != s.ContextMenuQuickOps
            || b.DisableMediaPlayerSharing != s.DisableMediaPlayerSharing
            || b.DisableInsiderService != s.DisableInsiderService
            || b.DisableStoreAutoUpdate != s.DisableStoreAutoUpdate
            || b.DisableNewsInterests != s.DisableNewsInterests;
    }

    private static bool IsShortcutArrowRemoved() => Win11DesktopTweaks.IsShortcutArrowHidden();

    private static bool IsAnimationsDisabled()
    {
        var minAnimate = GetValue(Hive.HkCu, @"Control Panel\Desktop", "MinAnimate") as string;
        return minAnimate == "0"
            && DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0);
    }

    private static bool AreTipsDisabled() =>
        DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0)
        && DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0)
        && DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0);

    private static void SetShortcutArrow(bool show) =>
        Win11DesktopTweaks.SetShortcutArrowHidden(!show);

    private static void SetAnimations(bool enable)
    {
        SetString(Hive.HkCu, @"Control Panel\Desktop", "MinAnimate", enable ? "1" : "0");
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", enable ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewAlphaSelect", enable ? 1 : 0);
    }

    private static bool IsTaskbarClockEnhanced()
    {
        var seconds = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", 1);
        var shortDate = GetString(Hive.HkCu, IntlKey, "sShortDate") ?? "";
        var hasWeekday = shortDate.IndexOf("dddd", StringComparison.OrdinalIgnoreCase) >= 0
            || shortDate.IndexOf("ddd", StringComparison.OrdinalIgnoreCase) >= 0;
        return seconds && hasWeekday;
    }

    private static void SetTaskbarClockEnhanced(bool enable)
    {
        if (enable)
        {
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", 1);
            SetString(Hive.HkCu, IntlKey, "sShortDate", ShortDateWithWeekday);
        }
        else
        {
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", 0);
            SetString(Hive.HkCu, IntlKey, "sShortDate", ShortDateDefault);
        }

        NotifyIntlChange();
        DesktopQuickActions.RestartExplorer();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);

    private static void NotifyIntlChange() =>
        _ = SendMessageTimeout(new IntPtr(0xffff), 0x001A, IntPtr.Zero, "intl", 2, 1000, out _);

    private static string? GetString(Hive hive, string key, string name) =>
        GetValue(hive, key, name) as string;

    private static void SetTips(bool enable)
    {
        var on = enable ? 1 : 0;
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", on);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", on);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", on);
        // 不写 SystemPaneSuggestionsEnabled：该键由「关闭设置应用建议内容」独立控制，避免互相覆盖。
    }

    private static void SetActivityHistory(bool enable)
    {
        if (enable)
        {
            DeleteValue(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowPublishUserActivities");
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "PublishUserActivities", 1);
        }
        else
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowPublishUserActivities", 0);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "PublishUserActivities", 0);
        }
    }

    private static void SetSmb1(bool enable)
    {
        if (enable)
        {
            Run("sc.exe", "config mrxsmb10 start= demand");
            DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SMB1");
        }
        else
        {
            Run("sc.exe", "config mrxsmb10 start= disabled");
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SMB1", 0);
        }
    }

    private static void SetServerManager(bool skipAtLogon)
    {
        // 等同「服务器管理器属性」→「在登录时不自动启动服务器管理器」
        SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", skipAtLogon ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", skipAtLogon ? 1 : 0);
        // 部分版本还会写该值；勾选时设为 0
        SetDword(Hive.HkCu, @"Software\Microsoft\ServerManager", "CheckedUnattendLaunchSetting", skipAtLogon ? 0 : 1);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\ServerManager", "DoNotOpenAtLogon", skipAtLogon ? 1 : 0);

        // 登录计划任务也会拉起服务器管理器；失败忽略（任务不存在等）
        try
        {
            Run("schtasks.exe", skipAtLogon
                ? "/Change /TN \"\\Microsoft\\Windows\\Server Manager\\ServerManager\" /DISABLE"
                : "/Change /TN \"\\Microsoft\\Windows\\Server Manager\\ServerManager\" /ENABLE");
        }
        catch
        {
            /* ignore */
        }
    }

    private const string UacPolicyKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

    /// <summary>UAC 滑块「从不通知」：ConsentPromptBehaviorAdmin=0 且 PromptOnSecureDesktop=0。
    /// 兼容旧版本工具写过的 EnableLUA=0。</summary>
    private static bool IsUacNeverNotify()
    {
        if (DwordEquals(Hive.HkLm, UacPolicyKey, "ConsentPromptBehaviorAdmin", 0)
            && DwordEquals(Hive.HkLm, UacPolicyKey, "PromptOnSecureDesktop", 0))
            return true;
        return DwordEquals(Hive.HkLm, UacPolicyKey, "EnableLUA", 0);
    }

    private static void SetUacNeverNotify(bool neverNotify)
    {
        if (neverNotify)
        {
            SetDword(Hive.HkLm, UacPolicyKey, "ConsentPromptBehaviorAdmin", 0);
            SetDword(Hive.HkLm, UacPolicyKey, "PromptOnSecureDesktop", 0);
            return;
        }

        // 恢复默认通知级别，并确保未彻底关掉 LUA（纠正旧版 EnableLUA=0）
        SetDword(Hive.HkLm, UacPolicyKey, "ConsentPromptBehaviorAdmin", 5);
        SetDword(Hive.HkLm, UacPolicyKey, "PromptOnSecureDesktop", 1);
        SetDword(Hive.HkLm, UacPolicyKey, "EnableLUA", 1);
    }

    private static void SetShutdownReason(bool enableUi)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonOn", enableUi ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonUI", enableUi ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability", "ShutdownReasonUI", enableUi ? 1 : 0);
    }

    private static void SetSmRemoting(bool enable)
    {
        var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "system32", "Configure-SMRemoting.exe");
        if (File.Exists(exe))
            Run(exe, enable ? "-enable" : "-disable");
        else
            SetService("WinRM", enable, disableWhenOff: true);
    }

    private static void SetTcpOptimized(bool enable)
    {
        if (enable)
        {
            Run("netsh.exe", "int tcp set global autotuninglevel=normal");
            Run("netsh.exe", "int tcp set global timestamps=disabled");
            Run("netsh.exe", "int tcp set global ecncapability=disabled");
        }
        else
        {
            Run("netsh.exe", "int tcp set global autotuninglevel=normal");
            Run("netsh.exe", "int tcp set global timestamps=enabled");
            Run("netsh.exe", "int tcp set global ecncapability=default");
        }
    }

    private static bool IsTcpOptimized()
    {
        try
        {
            var output = RunCapture("netsh.exe", "int tcp show global").ToLowerInvariant();
            return output.Contains("timestamps") && output.Contains("disabled")
                && output.Contains("autotuninglevel") && output.Contains("normal");
        }
        catch
        {
            return false;
        }
    }

    private static bool IsQosSpeedOptimized()
    {
        var zeroBandwidth = DwordEquals(Hive.HkLm, QosPschedKey, "NonBestEffortLimit", 0);
        var level = GetString(Hive.HkLm, QosPolicyKey, QosTcpAutotuningLevel);
        var maxInbound = level != null &&
            level.Equals("normal", StringComparison.OrdinalIgnoreCase);
        return zeroBandwidth && maxInbound;
    }

    private static void SetQosSpeedOptimized(bool enable)
    {
        if (enable)
        {
            SetDword(Hive.HkLm, QosPschedKey, "NonBestEffortLimit", 0);
            SetString(Hive.HkLm, QosPolicyKey, QosTcpAutotuningLevel, "normal");
            Run("netsh.exe", "int tcp set global autotuninglevel=normal");
        }
        else
        {
            DeleteValue(Hive.HkLm, QosPschedKey, "NonBestEffortLimit");
            DeleteValue(Hive.HkLm, QosPolicyKey, QosTcpAutotuningLevel);
        }
    }

    private static void SetHibernate(bool enable)
    {
        try
        {
            Run("powercfg.exe", enable ? "-h on" : "-h off");
        }
        catch (Exception ex) when (IsHibernateUnsupported(ex) && UiPrefs.SoftSkipUnsupported)
        {
            ApplyLog.SoftSkip("休眠",
                enable
                    ? "本机不支持开启休眠（固件/虚拟机）：" + TrimOneLine(ex.Message)
                    : "本机无法关闭休眠文件（固件/虚拟机不支持）：" + TrimOneLine(ex.Message));
        }
    }

    /// <summary>关屏/睡眠/休眠超时均为 0；关闭时恢复常见默认分钟数。</summary>
    private static void SetNeverSleepOrScreenOff(bool never)
    {
        if (never)
        {
            Run("powercfg.exe", "-change -monitor-timeout-ac 0");
            Run("powercfg.exe", "-change -monitor-timeout-dc 0");
            Run("powercfg.exe", "-change -standby-timeout-ac 0");
            Run("powercfg.exe", "-change -standby-timeout-dc 0");
            Run("powercfg.exe", "-change -hibernate-timeout-ac 0");
            Run("powercfg.exe", "-change -hibernate-timeout-dc 0");
            return;
        }

        // 关闭优化：恢复常见默认（分钟）
        Run("powercfg.exe", "-change -monitor-timeout-ac 10");
        Run("powercfg.exe", "-change -monitor-timeout-dc 5");
        Run("powercfg.exe", "-change -standby-timeout-ac 30");
        Run("powercfg.exe", "-change -standby-timeout-dc 15");
        Run("powercfg.exe", "-change -hibernate-timeout-ac 0");
        Run("powercfg.exe", "-change -hibernate-timeout-dc 180");
    }

    private static bool IsNeverSleepOrScreenOff()
    {
        try
        {
            return PowerTimeoutMinutesAcDcZero("monitor")
                && PowerTimeoutMinutesAcDcZero("standby")
                && PowerTimeoutMinutesAcDcZero("hibernate");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>用 powercfg /query 解析当前方案；AC/DC 索引均为 0 视为永不超时。</summary>
    private static bool PowerTimeoutMinutesAcDcZero(string kind)
    {
        // VIDEOIDLE / STANDBYIDLE / HIBERNATEIDLE 的索引单位为秒
        var (subgroup, setting) = kind switch
        {
            "monitor" => ("SUB_VIDEO", "VIDEOIDLE"),
            "standby" => ("SUB_SLEEP", "STANDBYIDLE"),
            _ => ("SUB_SLEEP", "HIBERNATEIDLE"),
        };
        var output = RunCapture("powercfg.exe", $"/query SCHEME_CURRENT {subgroup} {setting}");
        var ac = ExtractPowerIndex(output, ac: true);
        var dc = ExtractPowerIndex(output, ac: false);
        return ac == 0 && dc == 0;
    }

    private static uint? ExtractPowerIndex(string output, bool ac)
    {
        var marker = ac
            ? "Current AC Power Setting Index"
            : "Current DC Power Setting Index";
        // 中文系统：当前交流电源设置索引 / 当前直流电源设置索引
        var markerZh = ac ? "当前交流电源设置索引" : "当前直流电源设置索引";
        using var reader = new StringReader(output ?? "");
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0
                && line.IndexOf(markerZh, StringComparison.Ordinal) < 0)
                continue;
            var hex = line;
            var ix = hex.LastIndexOf("0x", StringComparison.OrdinalIgnoreCase);
            if (ix >= 0)
            {
                var token = hex.Substring(ix + 2).Trim();
                if (uint.TryParse(token, System.Globalization.NumberStyles.HexNumber, null, out var v))
                    return v;
            }
            // 偶发十进制尾部
            var colon = hex.LastIndexOf(':');
            if (colon >= 0)
            {
                var tail = hex.Substring(colon + 1).Trim();
                if (uint.TryParse(tail, out var d))
                    return d;
            }
        }
        return null;
    }

    private const string PartMgrKey = @"SYSTEM\CurrentControlSet\Services\PartMgr";

    private static bool IsDiskPerfEnabled() =>
        DwordEquals(Hive.HkLm, PartMgrKey, "EnableCounterForIoctl", 1);

    private static void SetDiskPerfEnabled(bool enable)
    {
        try
        {
            Run("diskperf.exe", enable ? "-Y" : "-N");
        }
        catch
        {
            // 回退注册表（与 diskperf 效果一致）
            if (enable)
                SetDword(Hive.HkLm, PartMgrKey, "EnableCounterForIoctl", 1);
            else
                SetDword(Hive.HkLm, PartMgrKey, "EnableCounterForIoctl", 0);
        }
    }

    private static bool IsHibernateUnsupported(Exception ex)
    {
        var m = ex.Message ?? "";
        return m.IndexOf("不支持该请求", StringComparison.Ordinal) >= 0
            || m.IndexOf("不支持休眠", StringComparison.Ordinal) >= 0
            || m.IndexOf("虚拟机", StringComparison.Ordinal) >= 0
            || m.IndexOf("hypervisor", StringComparison.OrdinalIgnoreCase) >= 0
            || m.IndexOf("firmware", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string TrimOneLine(string text)
    {
        var t = text.Replace("\r", " ").Replace("\n", " ").Trim();
        while (t.IndexOf("  ", StringComparison.Ordinal) >= 0)
            t = t.Replace("  ", " ");
        return t.Length > 160 ? t.Substring(0, 160) + "…" : t;
    }

    private static void SetRdp(bool enable)
    {
        SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", enable ? 0 : 1);
        // 中英文系统防火墙规则组名不同；注册表已写入，规则组匹配失败时软跳过
        SetFirewallGroup(enable,
            "remote desktop",
            "远程桌面",
            "Remote Desktop");
    }

    private static void SetNetworkDiscovery(bool enable)
    {
        SetService("fdPHost", enable, disableWhenOff: false);
        SetService("FDResPub", enable, disableWhenOff: false);
        SetFirewallGroup(enable,
            "network discovery",
            "网络发现",
            "Network Discovery");
        SetFirewallGroup(enable,
            "file and printer sharing",
            "文件和打印机共享",
            "File and Printer Sharing");
    }

    private static void SetFirewallGroup(bool enable, params string[] groupNames)
    {
        Exception? last = null;
        foreach (var g in groupNames)
        {
            try
            {
                Run("netsh.exe",
                    "advfirewall firewall set rule group=\"" + g + "\" new enable=" + (enable ? "Yes" : "No"));
                ApplyLog.Debug("防火墙组 OK：" + g + " → " + (enable ? "Yes" : "No"));
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                ApplyLog.Debug("防火墙组失败：" + g + " — " + ex.Message);
            }
        }

        if (last is null) return;
        if (UiPrefs.SoftSkipUnsupported)
        {
            ApplyLog.SoftSkip(ItemOr("防火墙"),
                "未找到匹配的规则组（已尝试：" + string.Join(" / ", groupNames) + "）。注册表项如已写入可忽略。" +
                TrimOneLine(last.Message));
            return;
        }

        throw last;
    }

    private static string ItemOr(string fallback) =>
        string.IsNullOrWhiteSpace(ApplyLog.CurrentContext) ? fallback : ApplyLog.CurrentContext!;

    private static void SetTelemetry(bool enable)
    {
        if (enable)
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1);
            SetService("DiagTrack", true, disableWhenOff: false);
        }
        else
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            SetService("DiagTrack", false, disableWhenOff: true);
        }
    }

    private static void ApplyAccountPolicy(bool disableComplexity, bool neverExpire, bool disableHistory)
    {
        // 期限 / 历史 / 最小长度：net accounts 优先
        Run("net.exe", neverExpire
            ? "accounts /maxpwage:unlimited"
            : "accounts /maxpwage:42");
        Run("net.exe", disableHistory
            ? "accounts /uniquepw:0"
            : "accounts /uniquepw:24");
        if (disableComplexity)
            Run("net.exe", "accounts /minpwlen:0");

        var id = Guid.NewGuid().ToString("N");
        var exportPath = Path.Combine(Path.GetTempPath(), "SrvDesk-secpol-export-" + id + ".inf");
        var cfg = Path.Combine(Path.GetTempPath(), "SrvDesk-secpol-cfg-" + id + ".inf");
        var db = Path.Combine(Path.GetTempPath(), "SrvDesk-secpol-" + id + ".sdb");
        var jfm = Path.ChangeExtension(db, ".jfm");
        TryDelete(exportPath);
        TryDelete(cfg);
        TryDelete(db);
        TryDelete(jfm);

        Exception? seceditError = null;
        try
        {
            // 导出当前策略再改键：比最小 INF 可靠；secedit 常在 100% 后仍返回退出码 1
            var (exportExit, _, exportErr) = RunProcess(
                "secedit.exe",
                $"/export /cfg \"{exportPath}\" /areas SECURITYPOLICY",
                timeoutMs: 25_000);
            if (exportExit is not (0 or 1) || !File.Exists(exportPath))
            {
                throw new InvalidOperationException(
                    "secedit 导出失败（退出码 " + exportExit + "）" +
                    (string.IsNullOrWhiteSpace(exportErr) ? "" : "：" + exportErr.Trim()));
            }
            var lines = File.Exists(exportPath)
                ? ReadInfLines(exportPath).ToList()
                : new List<string>();
            if (lines.Count == 0)
            {
                lines.AddRange([
                    "[Unicode]",
                    "Unicode=yes",
                    "[System Access]",
                    "[Version]",
                    "signature=\"$CHICAGO$\"",
                    "Revision=1",
                ]);
            }

            UpsertInfValue(lines, "PasswordComplexity", disableComplexity ? "0" : "1");
            UpsertInfValue(lines, "MaximumPasswordAge", neverExpire ? "-1" : "42");
            UpsertInfValue(lines, "PasswordHistorySize", disableHistory ? "0" : "24");
            if (disableComplexity)
                UpsertInfValue(lines, "MinimumPasswordLength", "0");

            File.WriteAllLines(cfg, lines, Encoding.Unicode);
            RunSeceditConfigure(db, cfg);
        }
        catch (Exception ex)
        {
            seceditError = ex;
            ApplyLog.Write("secedit 配置账户策略：" + ex.Message);
        }
        finally
        {
            TryDelete(exportPath);
            TryDelete(cfg);
            TryDelete(db);
            TryDelete(jfm);
        }

        ServerDesktopTweaks.ApplySamPasswordComplexity(disableComplexity);

        var applied = AccountPolicyLooksApplied(disableComplexity, neverExpire, disableHistory);
        if (disableComplexity && !ReadSecpolFlag("PasswordComplexity = 0"))
        {
            var hint = seceditError?.Message ?? "secpol 仍为已启用";
            throw new InvalidOperationException(
                "未能关闭「密码必须符合复杂性要求」。" + hint +
                "。可先关闭本机打开的「本地安全策略」(secpol.msc) 后再试。");
        }

        if (seceditError is not null && !applied)
            throw seceditError;

        if (seceditError is not null)
            ApplyLog.Write("secedit 返回非零但账户策略已生效，继续。");
    }

    /// <summary>在 INF 中写入/替换键（优先落在 [System Access] 段）。</summary>
    private static void UpsertInfValue(List<string> lines, string key, string value)
    {
        var prefix = key + " = ";
        var assign = prefix + value;
        for (var i = 0; i < lines.Count; i++)
        {
            var t = lines[i].Trim();
            if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = assign;
                return;
            }
        }

        var insertAt = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (string.Equals(lines[i].Trim(), "[System Access]", StringComparison.OrdinalIgnoreCase))
            {
                insertAt = i + 1;
                break;
            }
        }

        if (insertAt < 0)
        {
            lines.Add("[System Access]");
            lines.Add(assign);
            return;
        }

        lines.Insert(insertAt, assign);
    }

    private static void RunSeceditConfigure(string db, string cfg)
    {
        var (exit, stdout, stderr) = RunProcess(
            "secedit.exe",
            $"/configure /db \"{db}\" /cfg \"{cfg}\" /areas SECURITYPOLICY",
            timeoutMs: 25_000);
        // 0=成功；1=常带警告但仍可能已写入（进度可到 100%），以事后复核为准
        if (exit is 0 or 1) return;

        var detail = (stderr + " " + stdout).Trim();
        if (detail.Length > 240) detail = detail.Substring(0, 240) + "…";
        throw new InvalidOperationException(
            "secedit.exe 退出码 " + exit +
            (detail.Length > 0 ? "：" + detail : ""));
    }

    private static bool AccountPolicyLooksApplied(bool disableComplexity, bool neverExpire, bool disableHistory)
    {
        var flags = ReadAccountPolicyFlags();
        if (neverExpire != flags.NeverExpire) return false;
        if (disableHistory != flags.HistoryOff) return false;
        if (disableComplexity) return flags.ComplexityOff;
        // 恢复复杂性：secpol 必须不是 0
        return !ReadSecpolFlag("PasswordComplexity = 0");
    }

    private static (bool ComplexityOff, bool NeverExpire, bool HistoryOff) ReadAccountPolicyFlags()
    {
        var neverExpire = ReadPasswordNeverExpireFromNetAccounts()
            ?? ReadSecpolFlag("MaximumPasswordAge = 0")
            || ReadSecpolFlag("MaximumPasswordAge = -1");
        var historyOff = ReadPasswordHistoryOffFromNetAccounts()
            ?? ReadSecpolFlag("PasswordHistorySize = 0");
        // 以 secpol 为准。旧逻辑用 SAM OR，会在 secpol 仍启用时误显示「已关闭」。
        var complexity = TryReadSecpolInt("PasswordComplexity");
        var complexityOff = complexity is int c
            ? c == 0
            : ServerDesktopTweaks.IsSamPasswordComplexityOff();
        return (complexityOff, neverExpire, historyOff);
    }

    private static int? TryReadSecpolInt(string key)
    {
        var cfg = Path.Combine(Path.GetTempPath(), "SrvDesk-secpol-read.inf");
        try
        {
            Run("secedit.exe", $"/export /cfg \"{cfg}\"");
            if (!File.Exists(cfg)) return null;
            var prefix = key + " = ";
            foreach (var line in ReadInfLines(cfg))
            {
                var t = line.Trim();
                if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                var rest = t.Substring(prefix.Length).Trim();
                if (int.TryParse(rest, out var n))
                    return n;
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            TryDelete(cfg);
        }

        return null;
    }

    private static bool? ReadPasswordNeverExpireFromNetAccounts()
    {
        try
        {
            var output = RunCapture("net.exe", "accounts");
            foreach (var raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (!line.StartsWith("Maximum password age", StringComparison.OrdinalIgnoreCase) &&
                    !line.StartsWith("密码最长使用期限", StringComparison.Ordinal))
                    continue;

                if (line.IndexOf("Unlimited", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Never", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("无限制", StringComparison.Ordinal) >= 0 ||
                    line.IndexOf("永不", StringComparison.Ordinal) >= 0)
                    return true;
                if (Regex.IsMatch(line, @"\d+"))
                    return false;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>true = 历史长度为 0（已关闭强制密码历史）。</summary>
    private static bool? ReadPasswordHistoryOffFromNetAccounts()
    {
        try
        {
            var output = RunCapture("net.exe", "accounts");
            foreach (var raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (!line.StartsWith("Length of password history", StringComparison.OrdinalIgnoreCase) &&
                    !line.StartsWith("密码历史记录长度", StringComparison.Ordinal) &&
                    !line.StartsWith("维护的密码历史", StringComparison.Ordinal) &&
                    line.IndexOf("password history", StringComparison.OrdinalIgnoreCase) < 0 &&
                    line.IndexOf("密码历史", StringComparison.Ordinal) < 0)
                    continue;

                var m = Regex.Match(line, @"(\d+)");
                if (!m.Success) continue;
                return int.Parse(m.Groups[1].Value) == 0;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private static bool ReadSecpolFlag(string needle)
    {
        var cfg = Path.Combine(Path.GetTempPath(), "SrvDesk-secpol-read.inf");
        try
        {
            Run("secedit.exe", $"/export /cfg \"{cfg}\"");
            if (!File.Exists(cfg)) return false;
            return ReadInfLines(cfg).Any(line => line.IndexOf(needle, StringComparison.Ordinal) >= 0);
        }
        catch
        {
            return false;
        }
        finally
        {
            TryDelete(cfg);
        }
    }

    private static string[] ReadInfLines(string path)
    {
        try
        {
            // secedit 导出一般为 Unicode（UTF-16 LE）
            return File.ReadAllLines(path, Encoding.Unicode);
        }
        catch
        {
            return File.ReadAllLines(path, Encoding.Default);
        }
    }

    private static void SetPowerPlan(string guid) => Run("powercfg.exe", "/setactive " + guid);

    private static void ReadPowerPlanInto(State s)
    {
        if (IsActivePowerPlan(PowerPlanUltimate))
        {
            s.UltimatePerfPower = true;
            s.HighPerfPower = false;
        }
        else if (IsActivePowerPlan(PowerPlanHighPerf))
        {
            s.HighPerfPower = true;
            s.UltimatePerfPower = false;
        }
        else
        {
            s.HighPerfPower = false;
            s.UltimatePerfPower = false;
        }
    }

    private static void ApplyPowerPlan(State s)
    {
        if (s.UltimatePerfPower)
        {
            EnsureUltimatePowerPlan();
            SetPowerPlan(PowerPlanUltimate);
            return;
        }

        SetPowerPlan(s.HighPerfPower ? PowerPlanHighPerf : PowerPlanBalanced);
    }

    private static void EnsureUltimatePowerPlan()
    {
        try
        {
            var listed = RunCapture("powercfg.exe", "/list");
            if (listed.IndexOf(PowerPlanUltimate, StringComparison.OrdinalIgnoreCase) >= 0)
                return;
        }
        catch { /* fall through to duplicate */ }

        // 复制内置卓越性能方案（多数 SKU 默认不列出）
        try { Run("powercfg.exe", "/duplicatescheme " + PowerPlanUltimate); }
        catch (Exception ex) { ApplyLog.Debug("duplicatescheme Ultimate：" + ex.Message); }
    }

    private static bool IsActivePowerPlan(string guid)
    {
        try
        {
            var output = RunCapture("powercfg.exe", "/getactivescheme");
            return output.IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool DwordEquals(Hive hive, string key, string name, int expected) =>
        GetDword(hive, key, name) == expected;

    private static bool ServiceStartEquals(string service, int expected) =>
        GetDword(Hive.HkLm, $@"SYSTEM\CurrentControlSet\Services\{service}", "Start") == expected;

    private enum Hive { HkLm, HkCu }

    private static void Try(List<string> errors, string name, Action action)
    {
        try
        {
            using (ApplyLog.PushContext(name))
                action();
        }
        catch (Exception ex)
        {
            errors.Add($"{name}：{ex.Message}");
            ApplyLog.Write($"失败：{name} — {ex.Message}");
            ApplyLog.WriteChange($"【失败】{name}：{ex.Message}");
            ApplyLog.DebugItem(name, "异常", ex.Message);
        }
    }

    private static RegistryKey OpenBase(Hive hive) =>
        RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);

    private static int? GetDword(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) switch
        {
            int i => i,
            byte b => b,
            _ => null,
        };
    }

    private static object? GetValue(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name);
    }

    private static void SetDword(Hive hive, string key, string name, int value)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDword(HiveName(hive), key, name, old, value);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(Hive hive, string key, string name, string value)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryString(HiveName(hive), key, name, old, value);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDelete(HiveName(hive), key, name, old);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static string HiveName(Hive hive) => hive == Hive.HkLm ? "HKLM" : "HKCU";

    private static void SetService(string name, bool enable, bool disableWhenOff)
    {
        var oldStart = GetDword(Hive.HkLm, $@"SYSTEM\CurrentControlSet\Services\{name}", "Start");
        var newStart = enable ? 2 : (disableWhenOff ? 4 : 3);
        var detail = enable
            ? "sc config start= auto + start"
            : $"sc stop + config start= {(disableWhenOff ? "disabled" : "demand")}";
        ApplyLog.ServiceChange(name, detail, ApplyLog.StartTypeLabel(oldStart), ApplyLog.StartTypeLabel(newStart));

        // 启动类型已一致：不必再 sc config；启用时尝试 start（已在运行则忽略）
        if (oldStart == newStart)
        {
            if (enable)
            {
                ApplyLog.Debug($"服务 {name} 启动类型已是目标，仅尝试 start");
                RunScAllowBenign($"start {name}");
            }
            return;
        }

        if (enable)
        {
            RunScAllowBenign($"config {name} start= auto");
            RunScAllowBenign($"start {name}");
        }
        else
        {
            RunScAllowBenign($"stop {name}");
            RunScAllowBenign($"config {name} start= {(disableWhenOff ? "disabled" : "demand")}");
        }
    }

    private static void SetAudio(bool enable)
    {
        ApplyLog.ServiceChange("AudioSrv/AudioEndpointBuilder",
            enable ? "启用音频服务" : "禁用音频服务");
        if (enable)
        {
            RunScAllowBenign("config AudioSrv start= auto");
            RunScAllowBenign("config AudioEndpointBuilder start= auto");
            RunScAllowBenign("start AudioSrv");
        }
        else
        {
            RunScAllowBenign("stop AudioSrv");
            RunScAllowBenign("stop AudioEndpointBuilder");
            RunScAllowBenign("config AudioSrv start= disabled");
            RunScAllowBenign("config AudioEndpointBuilder start= disabled");
        }
    }

    private static void RunScAllowBenign(string arguments)
    {
        try
        {
            Run("sc.exe", arguments);
        }
        catch (Exception ex) when (IsScAccessDenied(ex) && UiPrefs.SoftSkipUnsupported)
        {
            // msiserver 等受保护服务：改注册表 Start 作兜底
            ApplyLog.Debug("sc 拒绝访问，尝试注册表兜底：" + arguments + " | " + ex.Message);
            if (!TryApplyServiceStartFromScArgs(arguments))
                throw;
            ApplyLog.SoftSkip("服务", "sc 拒绝访问，已用注册表写入 Start：" + arguments);
        }
    }

    private static bool IsScAccessDenied(Exception ex)
    {
        var m = ex.Message ?? "";
        return m.IndexOf("退出码 5", StringComparison.Ordinal) >= 0
            || m.IndexOf("拒绝访问", StringComparison.Ordinal) >= 0
            || m.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryApplyServiceStartFromScArgs(string arguments)
    {
        // 例：config msiserver start= auto | demand | disabled
        var parts = arguments.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4 || !parts[0].Equals("config", StringComparison.OrdinalIgnoreCase))
            return false;
        var svc = parts[1];
        var startIdx = Array.FindIndex(parts, p => p.Equals("start=", StringComparison.OrdinalIgnoreCase));
        if (startIdx < 0 || startIdx + 1 >= parts.Length) return false;
        var mode = parts[startIdx + 1];
        var dword = mode.Equals("auto", StringComparison.OrdinalIgnoreCase) ? 2
            : mode.Equals("demand", StringComparison.OrdinalIgnoreCase) ? 3
            : mode.Equals("disabled", StringComparison.OrdinalIgnoreCase) ? 4
            : -1;
        if (dword < 0) return false;
        SetDword(Hive.HkLm, $@"SYSTEM\CurrentControlSet\Services\{svc}", "Start", dword);
        return true;
    }

    private static void Run(string fileName, string arguments)
    {
        ApplyLog.Debug("Run " + fileName + " " + arguments);
        var (exit, stdout, stderr) = RunProcess(fileName, arguments, timeoutMs: 60_000);
        if (exit == 0) return;

        // sc：已启动/已停止/依赖占用停止/服务不存在 等常见无害码
        var isSc = fileName.EndsWith("sc.exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "sc", StringComparison.OrdinalIgnoreCase);
        if (isSc && exit is 1056 or 1051 or 1060 or 1062 or 1072)
        {
            ApplyLog.Debug($"sc 无害退出码 {exit}：{arguments}");
            return;
        }

        var detail = (stderr + " " + stdout).Trim();
        if (detail.Length > 240) detail = detail.Substring(0, 240) + "…";
        throw new InvalidOperationException(
            $"{Path.GetFileName(fileName)} 退出码 {exit}" +
            (detail.Length > 0 ? "：" + detail : ""));
    }

    private static string RunCapture(string fileName, string arguments)
    {
        var (_, stdout, _) = RunProcess(fileName, arguments, timeoutMs: 60_000, stdoutEncoding: Encoding.Default);
        return stdout;
    }

    /// <summary>
    /// 并行排空 stdout/stderr，超时杀进程，避免重定向管道死锁导致界面一直「正在写入」。
    /// </summary>
    private static (int ExitCode, string StdOut, string StdErr) RunProcess(
        string fileName,
        string arguments,
        int timeoutMs,
        Encoding? stdoutEncoding = null)
    {
        using var p = new Process();
        p.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = stdoutEncoding ?? Encoding.Default,
            StandardErrorEncoding = Encoding.Default,
        };
        if (!p.Start())
            throw new InvalidOperationException("无法启动 " + fileName);

        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(); } catch { /* ignore */ }
            try { p.WaitForExit(5_000); } catch { /* ignore */ }
            throw new TimeoutException($"{Path.GetFileName(fileName)} 超过 {timeoutMs / 1000} 秒未结束，已终止。");
        }

        var stdout = stdoutTask.Status == TaskStatus.RanToCompletion ? stdoutTask.Result : "";
        var stderr = stderrTask.Status == TaskStatus.RanToCompletion ? stderrTask.Result : "";
        try
        {
            // 给异步读一点时间收尾
            Task.WaitAll(new Task[] { stdoutTask, stderrTask }, 3_000);
            stdout = stdoutTask.Result;
            stderr = stderrTask.Result;
        }
        catch { /* 已有部分输出即可 */ }

        return (p.ExitCode, stdout ?? "", stderr ?? "");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* ignore */ }
    }
}
