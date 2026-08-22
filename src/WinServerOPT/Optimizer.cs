using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinOpt;

internal static class Optimizer
{
    internal const string IeEscAdmin = "{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string IeEscUser = "{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}";
    internal const string ClsidMyComputer = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
    internal const string AzureArcCommand = @"%windir%\AzureArcSetup\Systray\AzureArcSysTray.exe";
    internal const string PowerPlanHighPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    internal const string PowerPlanBalanced = "381b4222-f694-41f0-9685-ff5bb260df2e";
    internal const string IntlKey = @"Control Panel\International";
    internal const string ShortDateWithWeekday = "yyyy/MM/dd dddd";
    internal const string ShortDateDefault = "yyyy/M/d";
    internal const string QosPschedKey = @"SOFTWARE\Policies\Microsoft\Windows\Psched";
    internal const string QosPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\QoS";
    internal const string QosTcpAutotuningLevel = "Tcp Autotuning Level";

    internal sealed class State
    {
        public bool CpuProgramPriority;
        public bool Dep;
        public bool DisableUac;
        public bool DisableIeEsc;
        public bool HighPerfPower;
        public bool DisableTelemetry;
        public bool NoUpdateReboot;
        public bool DisableDeliveryOpt;
        public bool WuNotifyOnly;
        public bool DisableSysMain;
        public bool VisualBestPerf;
        public bool PowerThrottlingOff;
        public bool DisableHibernate;
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
        public bool EnableNetworkDiscovery;
        public bool DisableSmRemoting;

        public bool SkipServerManager;
        public bool DisableAzureArc;
        public bool EnableInstaller;
        public bool EnableWia;

        public bool DisablePasswordComplexity;
        public bool PasswordNeverExpire;
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
        public bool DisableRecommendedItems;
        public bool DisableAdTracking;
        public bool DisableSearchHistory;
        public bool DisableStickyKeys;
        public bool DisablePca;
        public bool PauseFeatureUpdatesUntil2035;

        public bool HideProtectedOsFiles;
        public bool AlwaysShowIconsNeverThumbnails;
        public bool ShowEmptyDrives;
        public bool ShowRecentFiles = true;
        public bool ShowFrequentPlaces = true;
        public bool HideOfficeCloudFiles;
        public bool DisableOneDrive;
        public bool HideTaskbarChat;
        public bool HideTaskbarCopilot;
        public int TaskbarSearchMode = -1;

        public bool DisableCloudSearch;
        public bool DisableWebsiteLangList;
        public bool DisableAppLaunchTracking;
        public bool DisableSettingsSuggestions;
        public bool DisableInkingPersonalization;
        public bool ExcludeMsrtFromWu;

        public bool DisableMeltdownSpectre;
        public bool DisableMemoryIntegrity;
        public bool DisableWdac;
        public bool DisableVbs;
        public bool EnableTcpBbr2;
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
        public bool DisableMediaPlayerSharing;
        public bool DisableInsiderService;
        public bool DisableStoreAutoUpdate;
        public bool DisableNewsInterests;
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

        var account = fullScan ? ReadAccountPolicyFlags() : (ComplexityOff: ServerDesktopTweaks.IsSamPasswordComplexityOff(), NeverExpire: false);

        var state = new State
        {
            CpuProgramPriority = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38),
            Dep = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DataExecutionPrevention_S4UEnable", 1),
            DisableUac = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 0),
            DisableIeEsc = DwordEquals(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscAdmin}", "IsInstalled", 0),
            HighPerfPower = fullScan && IsActivePowerPlan(PowerPlanHighPerf),
            DisableTelemetry = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0),
            NoUpdateReboot = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1),
            DisableDeliveryOpt = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 100),
            WuNotifyOnly = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", 2),
            DisableSysMain = ServiceStartEquals("SysMain", 4),
            VisualBestPerf = DwordEquals(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2),
            PowerThrottlingOff = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1),
            DisableHibernate = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0),
            TcpOptimized = fullScan && IsTcpOptimized(),
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

            SkipServerManager = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", 1),
            DisableAzureArc = GetValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup") is null,
            EnableInstaller = ServiceStartEquals("msiserver", 2),
            EnableWia = ServiceStartEquals("stisvc", 2),

            DisablePasswordComplexity = account.ComplexityOff,
            PasswordNeverExpire = account.NeverExpire,
            ShutdownWithoutLogon = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", 1),
            DisableShutdownReason = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability", "ShutdownReasonOn", 0),
            DisableCad = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", 1),

            EnableAutologon = AutologonHelper.Read().Enabled,

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
            DisableRecommendedItems = Win11DesktopTweaks.IsDisableRecommendedItemsOn(),
            DisableAdTracking = Win11DesktopTweaks.IsDisableAdTrackingOn(),
            DisableSearchHistory = Win11DesktopTweaks.IsDisableSearchHistoryOn(),
            DisableStickyKeys = Win11DesktopTweaks.IsDisableStickyKeysOn(),
            DisablePca = ServiceStartEquals("PcaSvc", 4),
            PauseFeatureUpdatesUntil2035 = Win11DesktopTweaks.IsPauseFeatureUpdatesUntil2035On(),
            TaskbarSearchMode = Win11DesktopTweaks.IsTaskbarSearchBoxOn() ? 2 : 1,
        };
        EasySettingsTweaks.ReadInto(state);
        CompetitorTweaks.ReadInto(state);
        var searchMode = EasySettingsTweaks.GetSearchboxMode();
        if (searchMode is 0 or 1 or 2)
            state.TaskbarSearchMode = searchMode;
        return state;
    }

    public static List<string> Apply(State s)
    {
        var errors = new List<string>();
        Try(errors, "CPU资源分配", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", s.CpuProgramPriority ? 38 : 2));
        Try(errors, "DEP", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DataExecutionPrevention_S4UEnable", s.Dep ? 1 : 0));
        Try(errors, "UAC", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", s.DisableUac ? 0 : 1));
        Try(errors, "IE增强安全", () =>
        {
            SetDword(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscAdmin}", "IsInstalled", s.DisableIeEsc ? 0 : 1);
            SetDword(Hive.HkLm, $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{IeEscUser}", "IsInstalled", s.DisableIeEsc ? 0 : 1);
        });
        Try(errors, "电源计划", () => SetPowerPlan(s.HighPerfPower ? PowerPlanHighPerf : PowerPlanBalanced));
        Try(errors, "遥测", () => SetTelemetry(!s.DisableTelemetry));
        Try(errors, "更新重启", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", s.NoUpdateReboot ? 1 : 0));
        Try(errors, "传递优化", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", s.DisableDeliveryOpt ? 100 : 1));
        Try(errors, "更新通知", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", s.WuNotifyOnly ? 2 : 4));
        Try(errors, "SysMain", () => SetService("SysMain", !s.DisableSysMain, disableWhenOff: true));
        Try(errors, "视觉效果", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", s.VisualBestPerf ? 2 : 3));
        Try(errors, "电源节流", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", s.PowerThrottlingOff ? 1 : 0));
        Try(errors, "休眠", () => Run("powercfg.exe", s.DisableHibernate ? "-h off" : "-h on"));
        Try(errors, "TCP优化", () => SetTcpOptimized(s.TcpOptimized));
        Try(errors, "QoS网速", () => SetQosSpeedOptimized(s.QosSpeedOptimize));
        Try(errors, "错误报告", () => SetService("WerSvc", !s.DisableErrorReport, disableWhenOff: true));

        Try(errors, "桌面此电脑", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", ClsidMyComputer, s.ShowThisPcIcon ? 0 : 1));
        Try(errors, "打开此电脑", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", s.LaunchExplorerThisPc ? 1 : 2));
        Try(errors, "小按钮任务栏", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", s.SmallTaskbar ? 1 : 0));
        Try(errors, "删除确认", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete", s.ConfirmDelete ? 1 : 0));
        Try(errors, "音频服务", () => SetAudio(s.EnableAudio));
        Try(errors, "文件扩展名", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", s.ShowFileExtensions ? 0 : 1));
        Try(errors, "主题服务", () => SetService("Themes", s.EnableThemes, disableWhenOff: false));
        Try(errors, "Windows搜索", () =>
        {
            if (s.DisableSearchEngineFeature)
                ServerDesktopTweaks.ApplySearchEngineFeature(true);
            else
                SetService("WSearch", s.EnableSearch, disableWhenOff: false);
        });
        Try(errors, "Bing搜索", () =>
        {
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", s.DisableWebSearch ? 1 : 0);
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", s.DisableWebSearch ? 0 : 1);
        });
        Try(errors, "体验反馈", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", s.DisableFeedback ? 0 : 1));
        Try(errors, "锁屏", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", s.NoLockScreen ? 1 : 0));

        Try(errors, "远程桌面", () => SetRdp(s.EnableRdp));
        Try(errors, "RDP图形加速", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "UseAdvancedGraphics", s.RdpGpuAccel ? 1 : 0));
        Try(errors, "RDP帧率", () =>
        {
            if (s.RdpHighRefresh)
                SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations", "DWMFRAMEINTERVAL", 15);
            else
                DeleteValue(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations", "DWMFRAMEINTERVAL");
        });
        Try(errors, "RDP NLA", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", s.RdpDisableNla ? 0 : 1));
        Try(errors, "网络发现", () => SetNetworkDiscovery(s.EnableNetworkDiscovery));
        Try(errors, "Server远程管理", () => SetSmRemoting(!s.DisableSmRemoting));

        Try(errors, "服务管理器", () => SetServerManager(s.SkipServerManager));
        Try(errors, "Azure Arc", () =>
        {
            if (s.DisableAzureArc)
                DeleteValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup");
            else
                SetString(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AzureArcSetup", AzureArcCommand);
        });
        Try(errors, "Windows Installer", () => SetService("msiserver", s.EnableInstaller, disableWhenOff: false));
        Try(errors, "WIA图像采集", () => SetService("stisvc", s.EnableWia, disableWhenOff: false));

        Try(errors, "账户策略", () => ApplyAccountPolicy(s.DisablePasswordComplexity, s.PasswordNeverExpire));

        Try(errors, "未登录关机", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", s.ShutdownWithoutLogon ? 1 : 0));
        Try(errors, "关机事件跟踪", () => SetShutdownReason(!s.DisableShutdownReason));
        Try(errors, "Ctrl+Alt+Del", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", s.DisableCad ? 1 : 0));

        Try(errors, "自动登录", () =>
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

        Try(errors, "长路径支持", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", s.LongPathsEnabled ? 1 : 0));
        Try(errors, "快速启动", () =>
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", s.DisableFastStartup ? 0 : 1));
        Try(errors, "自动维护", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", s.DisableAutoMaintenance ? 1 : 0));
        Try(errors, "驱动自动更新", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", s.ExcludeDriverUpdates ? 1 : 0));
        Try(errors, "SMB1 协议", () => SetSmb1(!s.DisableSmb1));
        Try(errors, "Remote Registry", () => SetService("RemoteRegistry", !s.DisableRemoteRegistry, disableWhenOff: true));
        Try(errors, "打印后台处理", () => SetService("Spooler", !s.DisablePrintSpooler, disableWhenOff: true));

        Try(errors, "显示隐藏文件", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", s.ShowHiddenFiles ? 1 : 2));
        Try(errors, "快捷方式箭头", () => SetShortcutArrow(!s.NoShortcutArrow));
        Try(errors, "标题栏完整路径", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath", s.ExplorerFullPath ? 1 : 0));
        Try(errors, "任务栏全部图标", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableAutoTray", s.TaskbarAllIcons ? 0 : 1));
        Try(errors, "任务栏时钟", () => SetTaskbarClockEnhanced(s.TaskbarClockWeekdaySeconds));

        Try(errors, "窗口动画", () => SetAnimations(!s.DisableAnimations));
        Try(errors, "透明效果", () =>
            SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", s.DisableTransparency ? 0 : 1));
        Try(errors, "Windows 提示", () => SetTips(!s.DisableTips));
        Try(errors, "自动播放", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", s.DisableAutoplay ? 255 : 145));
        Try(errors, "活动历史", () => SetActivityHistory(!s.DisableActivityHistory));
        Try(errors, "存储感知", () =>
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal", s.DisableStorageSense ? 0 : 1));

        Try(errors, "SmartScreen与打开警告", () => ServerDesktopTweaks.ApplySmartScreenAndOpenWarning(s.DisableSmartScreenWarning));
        Try(errors, "控制面板与回收站", () => ServerDesktopTweaks.ApplyDesktopIcons(s.ShowControlPanelRecycleBin));
        Try(errors, "大系统缓存", () => ServerDesktopTweaks.ApplyLargeSystemCache(s.LargeSystemCacheOptimize));
        Try(errors, "保留存储", () => ServerDesktopTweaks.ApplyReservedStorage(s.DisableReservedStorage));
        Try(errors, "LanmanServer拆分", () => ServerDesktopTweaks.ApplySrvSplitThreshold(s.DisableSrvSplit));
        Try(errors, "GPU硬件调度", () => ServerDesktopTweaks.ApplyGpuHwScheduling(s.EnableGpuHwScheduling));
        Try(errors, "登录键盘筛选", () => ServerDesktopTweaks.ApplyLoginKeyboardFilters(s.DisableLoginKeyboardFilters));
        Try(errors, "后台应用", () => ServerDesktopTweaks.ApplyBackgroundApps(s.DisableBackgroundApps));
        Try(errors, "传统搜索", () => ServerDesktopTweaks.ApplyClassicSearch(s.ClassicFileSearch));
        Try(errors, "桌面媒体组件", () => ServerDesktopTweaks.ApplyDesktopMediaFeatures(s.EnableDesktopMediaFeatures));
        Try(errors, "Server冗余组件", () => ServerDesktopTweaks.ApplyServerBloatFeatures(s.DisableServerBloatFeatures));
        Try(errors, "Win11桌面体验", () =>
        {
            Win11DesktopTweaks.Apply(s);
            DesktopQuickActions.RestartExplorer();
        });
        Try(errors, "程序兼容性助手", () => SetService("PcaSvc", !s.DisablePca, disableWhenOff: true));
        Try(errors, "轻松设置扩展项", () => EasySettingsTweaks.Apply(s));
        Try(errors, "竞品常用项", () => CompetitorTweaks.Apply(s));
        return errors;
    }

    private static bool IsShortcutArrowRemoved()
    {
        var val = GetValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", "29");
        return val is string s && s.Length == 0;
    }

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

    private static void SetShortcutArrow(bool show)
    {
        if (show)
            DeleteValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", "29");
        else
            SetString(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", "29", "");
    }

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

    private static void SetServerManager(bool skip)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotOpenServerManagerAtLogon", skip ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "DoNotPopWACConsoleAtSMLaunch", skip ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\ServerManager", "RefreshInterval", skip ? 14400 : 3600);
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

    private static void SetRdp(bool enable)
    {
        SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", enable ? 0 : 1);
        Run("netsh.exe", enable
            ? "advfirewall firewall set rule group=\"remote desktop\" new enable=Yes"
            : "advfirewall firewall set rule group=\"remote desktop\" new enable=No");
    }

    private static void SetNetworkDiscovery(bool enable)
    {
        SetService("fdPHost", enable, disableWhenOff: false);
        SetService("FDResPub", enable, disableWhenOff: false);
        Run("netsh.exe", enable
            ? "advfirewall firewall set rule group=\"network discovery\" new enable=Yes"
            : "advfirewall firewall set rule group=\"network discovery\" new enable=No");
        Run("netsh.exe", enable
            ? "advfirewall firewall set rule group=\"file and printer sharing\" new enable=Yes"
            : "advfirewall firewall set rule group=\"file and printer sharing\" new enable=No");
    }

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

    private static void ApplyAccountPolicy(bool disableComplexity, bool neverExpire)
    {
        var cfg = Path.Combine(Path.GetTempPath(), "WinOpt-secpol.inf");
        Run("secedit.exe", $"/export /cfg \"{cfg}\"");
        if (!File.Exists(cfg)) throw new InvalidOperationException("secedit 导出失败");
        var text = File.ReadAllText(cfg);
        text = ReplaceSecpolLine(text, "PasswordComplexity", disableComplexity ? 0 : 1);
        text = ReplaceSecpolLine(text, "MaximumPasswordAge", neverExpire ? 0 : 42);
        File.WriteAllText(cfg, text);
        Run("secedit.exe", $"/configure /db C:\\Windows\\security\\local.sdb /cfg \"{cfg}\" /areas SECURITYPOLICY");
        TryDelete(cfg);
        ServerDesktopTweaks.ApplySamPasswordComplexity(disableComplexity);
    }

    private static string ReplaceSecpolLine(string text, string key, int value)
    {
        var line = $"{key} = {value}";
        if (text.Contains($"{key} = 0")) return text.Replace($"{key} = 0", line);
        if (text.Contains($"{key} = 1")) return text.Replace($"{key} = 1", line);
        if (text.Contains($"{key} = 42")) return text.Replace($"{key} = 42", line);
        return text + Environment.NewLine + line;
    }

    private static (bool ComplexityOff, bool NeverExpire) ReadAccountPolicyFlags()
    {
        var cfg = Path.Combine(Path.GetTempPath(), "WinOpt-secpol-read.inf");
        try
        {
            Run("secedit.exe", $"/export /cfg \"{cfg}\"");
            if (!File.Exists(cfg))
                return (ServerDesktopTweaks.IsSamPasswordComplexityOff(), false);

            var lines = File.ReadAllLines(cfg);
            var complexityOff = lines.Any(line => line.IndexOf("PasswordComplexity = 0", StringComparison.Ordinal) >= 0)
                || ServerDesktopTweaks.IsSamPasswordComplexityOff();
            var neverExpire = lines.Any(line => line.IndexOf("MaximumPasswordAge = 0", StringComparison.Ordinal) >= 0);
            return (complexityOff, neverExpire);
        }
        catch
        {
            return (ServerDesktopTweaks.IsSamPasswordComplexityOff(), false);
        }
        finally
        {
            TryDelete(cfg);
        }
    }

    private static bool ReadSecpolFlag(string needle)
    {
        var cfg = Path.Combine(Path.GetTempPath(), "WinOpt-secpol-read.inf");
        try
        {
            Run("secedit.exe", $"/export /cfg \"{cfg}\"");
            if (!File.Exists(cfg)) return false;
            return File.ReadAllLines(cfg).Any(line => line.IndexOf(needle, StringComparison.Ordinal) >= 0);
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

    private static void SetPowerPlan(string guid) => Run("powercfg.exe", "/setactive " + guid);

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
        try { action(); }
        catch (Exception ex) { errors.Add($"{name}：{ex.Message}"); }
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
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(Hive hive, string key, string name, string value)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true)
            ?? throw new InvalidOperationException("无法写入注册表：" + key);
        k.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void SetService(string name, bool enable, bool disableWhenOff)
    {
        if (enable)
        {
            Run("sc.exe", $"config {name} start= auto");
            Run("sc.exe", $"start {name}");
        }
        else
        {
            Run("sc.exe", $"stop {name}");
            Run("sc.exe", $"config {name} start= {(disableWhenOff ? "disabled" : "demand")}");
        }
    }

    private static void SetAudio(bool enable)
    {
        if (enable)
        {
            Run("sc.exe", "config AudioSrv start= auto");
            Run("sc.exe", "config AudioEndpointBuilder start= auto");
            Run("sc.exe", "start AudioSrv");
        }
        else
        {
            Run("sc.exe", "stop AudioSrv");
            Run("sc.exe", "stop AudioEndpointBuilder");
            Run("sc.exe", "config AudioSrv start= disabled");
            Run("sc.exe", "config AudioEndpointBuilder start= disabled");
        }
    }

    private static void Run(string fileName, string arguments)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + fileName);
        p.WaitForExit(60_000);
    }

    private static string RunCapture(string fileName, string arguments)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.Default,
        }) ?? throw new InvalidOperationException("无法启动 " + fileName);
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(60_000);
        return output;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* ignore */ }
    }
}
