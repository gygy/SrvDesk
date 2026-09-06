namespace WinOpt;

/// <summary>
/// 各优化项的一键开启/关闭脚本目录（与 Optimizer / Tweaks Apply 逻辑对齐）。
/// 用户可在帮助面板复制或另存为 .reg/.cmd 手工执行。
/// </summary>
internal static class SettingRecipeCatalog
{
    static readonly Dictionary<SettingHelpInfo, SettingActionRecipe> Map = Build();

    public static SettingActionRecipe? Get(SettingHelpInfo help) =>
        Map.TryGetValue(help, out var r) ? r : null;

    static Dictionary<SettingHelpInfo, SettingActionRecipe> Build()
    {
        var m = new Dictionary<SettingHelpInfo, SettingActionRecipe>();

        void Add(SettingHelpInfo help, SettingActionRecipe recipe) => m[help] = recipe;

        // —— 性能 / 安全基础 ——
        Add(SettingCatalog.CpuProgramPriority, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38, 2));

        Add(SettingCatalog.Dep, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            "DataExecutionPrevention_S4UEnable", 1, 0));

        Add(SettingCatalog.DisableUac, ActionScript.Reg(
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"),
                ActionScript.Dword("ConsentPromptBehaviorAdmin", 0),
                ActionScript.Dword("PromptOnSecureDesktop", 0)),
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"),
                ActionScript.Dword("ConsentPromptBehaviorAdmin", 5),
                ActionScript.Dword("PromptOnSecureDesktop", 1),
                ActionScript.Dword("EnableLUA", 1)),
            "对齐「从不通知」滑块；关闭时恢复默认通知并确保 EnableLUA=1。"));

        Add(SettingCatalog.DisableIeEsc, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm($@"SOFTWARE\Microsoft\Active Setup\Installed Components\{Optimizer.IeEscAdmin}"),
                ActionScript.Dword("IsInstalled", 0)) +
            ActionScript.Block(ActionScript.HkLm($@"SOFTWARE\Microsoft\Active Setup\Installed Components\{Optimizer.IeEscUser}"),
                ActionScript.Dword("IsInstalled", 0)),
            ActionScript.Block(ActionScript.HkLm($@"SOFTWARE\Microsoft\Active Setup\Installed Components\{Optimizer.IeEscAdmin}"),
                ActionScript.Dword("IsInstalled", 1)) +
            ActionScript.Block(ActionScript.HkLm($@"SOFTWARE\Microsoft\Active Setup\Installed Components\{Optimizer.IeEscUser}"),
                ActionScript.Dword("IsInstalled", 1)),
            "Server IE 增强安全配置。"));

        Add(SettingCatalog.HighPerfPower, ActionScript.Cmd(
            $"powercfg.exe /setactive {Optimizer.PowerPlanHighPerf}",
            $"powercfg.exe /setactive {Optimizer.PowerPlanBalanced}"));

        Add(SettingCatalog.DisableTelemetry, ActionScript.Mixed(
            ActionScript.WrapReg(ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"),
                ActionScript.Dword("AllowTelemetry", 0))) +
            "\r\n--- 并执行 ---\r\n" +
            ActionScript.WrapCmd("sc stop DiagTrack\r\nsc config DiagTrack start= disabled"),
            ActionScript.WrapReg(ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"),
                ActionScript.Dword("AllowTelemetry", 1))) +
            "\r\n--- 并执行 ---\r\n" +
            ActionScript.WrapCmd("sc config DiagTrack start= auto\r\nsc start DiagTrack"),
            "注册表 + DiagTrack 服务。"));

        Add(SettingCatalog.NoUpdateReboot, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1, 0));

        Add(SettingCatalog.DisableDeliveryOpt, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 100, 1));

        Add(SettingCatalog.WuNotifyOnly, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", 2, 4));

        Add(SettingCatalog.DisableSysMain, ActionScript.Service("SysMain", enableMeansStart: false));

        Add(SettingCatalog.VisualBestPerf, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, 3));

        Add(SettingCatalog.PowerThrottlingOff, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1, 0));

        Add(SettingCatalog.DisableHibernate, ActionScript.Cmd(
            "powercfg.exe -h off",
            "powercfg.exe -h on"));

        Add(SettingCatalog.TcpOptimized, ActionScript.Cmd(
            "netsh.exe int tcp set global autotuninglevel=normal\r\nnetsh.exe int tcp set global timestamps=disabled\r\nnetsh.exe int tcp set global ecncapability=disabled",
            "netsh.exe int tcp set global autotuninglevel=normal\r\nnetsh.exe int tcp set global timestamps=enabled\r\nnetsh.exe int tcp set global ecncapability=default"));

        Add(SettingCatalog.QosSpeedOptimize, ActionScript.Mixed(
            ActionScript.WrapReg(
                ActionScript.Block(ActionScript.HkLm(Optimizer.QosPschedKey), ActionScript.Dword("NonBestEffortLimit", 0)) +
                ActionScript.Block(ActionScript.HkLm(Optimizer.QosPolicyKey), ActionScript.Sz(Optimizer.QosTcpAutotuningLevel, "normal"))) +
            "\r\n--- 可选 ---\r\nnetsh.exe int tcp set global autotuninglevel=normal\r\n",
            ActionScript.WrapReg(
                ActionScript.Block(ActionScript.HkLm(Optimizer.QosPschedKey), ActionScript.DeleteValue("NonBestEffortLimit")) +
                ActionScript.Block(ActionScript.HkLm(Optimizer.QosPolicyKey), ActionScript.DeleteValue(Optimizer.QosTcpAutotuningLevel))),
            "QoS 保留带宽设为 0。"));

        Add(SettingCatalog.DisableErrorReport, ActionScript.Service("WerSvc", enableMeansStart: false));

        Add(SettingCatalog.LongPathsEnabled, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1, 0));

        Add(SettingCatalog.DisableFastStartup, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0, 1));

        Add(SettingCatalog.DisableAutoMaintenance, ActionScript.DwordToggle(false,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", 1, 0));

        Add(SettingCatalog.ExcludeDriverUpdates, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1, 0));

        Add(SettingCatalog.DisableSmb1, ActionScript.Mixed(
            ActionScript.WrapCmd("sc config mrxsmb10 start= disabled") +
            "\r\n" + ActionScript.WrapReg(ActionScript.Block(
                ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"),
                ActionScript.Dword("SMB1", 0))),
            ActionScript.WrapCmd("sc config mrxsmb10 start= demand") +
            "\r\n" + ActionScript.WrapReg(ActionScript.Block(
                ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"),
                ActionScript.DeleteValue("SMB1"))),
            "禁用 SMB1。"));

        Add(SettingCatalog.DisableRemoteRegistry, ActionScript.Service("RemoteRegistry", enableMeansStart: false));
        Add(SettingCatalog.DisablePrintSpooler, ActionScript.Service("Spooler", enableMeansStart: false));

        // —— 桌面 / 资源管理器 ——
        Add(SettingCatalog.ShowThisPcIcon, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel",
            Optimizer.ClsidMyComputer, 0, 1));

        Add(SettingCatalog.LaunchExplorerThisPc, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, 2));

        Add(SettingCatalog.SmallTaskbar, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarSmallIcons", 1, 0));

        Add(SettingCatalog.ConfirmDelete, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ConfirmFileDelete", 1, 0));

        Add(SettingCatalog.EnableAudio, ActionScript.Cmd(
            "sc config Audiosrv start= auto\r\nsc start Audiosrv\r\nsc config AudioEndpointBuilder start= auto\r\nsc start AudioEndpointBuilder",
            "sc config Audiosrv start= demand\r\nsc stop Audiosrv",
            "启用音频相关服务。"));

        Add(SettingCatalog.ShowFileExtensions, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0, 1));

        Add(SettingCatalog.EnableThemes, ActionScript.Service("Themes", enableMeansStart: true, disableWhenOff: false));

        Add(SettingCatalog.EnableSearch, ActionScript.Service("WSearch", enableMeansStart: true, disableWhenOff: false,
            "若同时开启「禁用搜索功能」，软件会优先 DISM 卸载搜索组件。"));

        Add(SettingCatalog.DisableWebSearch, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                ActionScript.Dword("DisableWebSearch", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Search"),
                ActionScript.Dword("BingSearchEnabled", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                ActionScript.Dword("DisableWebSearch", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Search"),
                ActionScript.Dword("BingSearchEnabled", 1))));

        Add(SettingCatalog.DisableFeedback, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0, 1));

        Add(SettingCatalog.NoLockScreen, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", 1, 0));

        Add(SettingCatalog.ShowHiddenFiles, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1, 2));

        Add(SettingCatalog.NoShortcutArrow, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"),
                ActionScript.Sz("29", @"%LOCALAPPDATA%\WinOpt\blank.ico,0")),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"),
                ActionScript.DeleteValue("29")),
            "需先有 blank.ico（软件会生成）；导入后请重启资源管理器。"));

        Add(SettingCatalog.ExplorerFullPath, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "FullPath", 1, 0));

        Add(SettingCatalog.TaskbarAllIcons, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableAutoTray", 0, 1));

        Add(SettingCatalog.TaskbarClockWeekdaySeconds, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("ShowSecondsInSystemClock", 1)) +
            ActionScript.Block(ActionScript.HkCu(Optimizer.IntlKey),
                ActionScript.Sz("sShortDate", Optimizer.ShortDateWithWeekday)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("ShowSecondsInSystemClock", 0)) +
            ActionScript.Block(ActionScript.HkCu(Optimizer.IntlKey),
                ActionScript.Sz("sShortDate", Optimizer.ShortDateDefault)),
            "导入后建议注销或重启资源管理器。"));

        Add(SettingCatalog.DisableAnimations, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Desktop"), ActionScript.Sz("MinAnimate", "0")) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("TaskbarAnimations", 0),
                ActionScript.Dword("ListviewAlphaSelect", 0)),
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Desktop"), ActionScript.Sz("MinAnimate", "1")) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("TaskbarAnimations", 1),
                ActionScript.Dword("ListviewAlphaSelect", 1))));

        Add(SettingCatalog.DisableTransparency, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, 1));

        Add(SettingCatalog.DisableTips, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"),
                ActionScript.Dword("SubscribedContent-338388Enabled", 0),
                ActionScript.Dword("SubscribedContent-338389Enabled", 0),
                ActionScript.Dword("SoftLandingEnabled", 0)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"),
                ActionScript.Dword("SubscribedContent-338388Enabled", 1),
                ActionScript.Dword("SubscribedContent-338389Enabled", 1),
                ActionScript.Dword("SoftLandingEnabled", 1))));

        Add(SettingCatalog.DisableAutoplay, ActionScript.DwordToggle(false,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 255, 145));

        Add(SettingCatalog.DisableActivityHistory, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\System"),
                ActionScript.Dword("AllowPublishUserActivities", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Privacy"),
                ActionScript.Dword("PublishUserActivities", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\System"),
                ActionScript.DeleteValue("AllowPublishUserActivities")) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Privacy"),
                ActionScript.Dword("PublishUserActivities", 1))));

        Add(SettingCatalog.DisableStorageSense, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal", 0, 1));

        // —— 远程 / Server ——
        Add(SettingCatalog.EnableRdp, ActionScript.Mixed(
            ActionScript.WrapReg(ActionScript.Block(
                ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\Terminal Server"),
                ActionScript.Dword("fDenyTSConnections", 0))) +
            "\r\n--- 防火墙 ---\r\nnetsh advfirewall firewall set rule group=\"remote desktop\" new enable=Yes\r\n",
            ActionScript.WrapReg(ActionScript.Block(
                ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\Terminal Server"),
                ActionScript.Dword("fDenyTSConnections", 1))) +
            "\r\n--- 防火墙 ---\r\nnetsh advfirewall firewall set rule group=\"remote desktop\" new enable=No\r\n"));

        Add(SettingCatalog.RdpGpuAccel, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "UseAdvancedGraphics", 1, 0));

        Add(SettingCatalog.RdpHighRefresh, ActionScript.DwordOnDeleteOff(false,
            @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations", "DWMFRAMEINTERVAL", 15));

        Add(SettingCatalog.RdpDisableNla, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", 0, 1,
            "关闭 NLA 降低安全性，仅内网调试建议。"));

        Add(SettingCatalog.EnableNetworkDiscovery, ActionScript.Cmd(
            "sc config fdPHost start= auto\r\nsc start fdPHost\r\nsc config FDResPub start= auto\r\nsc start FDResPub\r\nnetsh advfirewall firewall set rule group=\"network discovery\" new enable=Yes\r\nnetsh advfirewall firewall set rule group=\"file and printer sharing\" new enable=Yes",
            "netsh advfirewall firewall set rule group=\"network discovery\" new enable=No\r\nnetsh advfirewall firewall set rule group=\"file and printer sharing\" new enable=No"));

        Add(SettingCatalog.DisableSmRemoting, ActionScript.Cmd(
            "\"%windir%\\system32\\Configure-SMRemoting.exe\" -disable",
            "\"%windir%\\system32\\Configure-SMRemoting.exe\" -enable",
            "若无此工具，可改用禁用/启用 WinRM 服务。"));

        Add(SettingCatalog.SkipServerManager, ActionScript.Mixed(
            ActionScript.WrapReg(
                ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\ServerManager"),
                    ActionScript.Dword("DoNotOpenServerManagerAtLogon", 1)) +
                ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\ServerManager"),
                    ActionScript.Dword("DoNotOpenServerManagerAtLogon", 1),
                    ActionScript.Dword("CheckedUnattendLaunchSetting", 0)) +
                ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\ServerManager"),
                    ActionScript.Dword("DoNotOpenAtLogon", 1))) +
            "\r\nschtasks /Change /TN \"\\Microsoft\\Windows\\Server Manager\\ServerManager\" /DISABLE\r\n",
            ActionScript.WrapReg(
                ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\ServerManager"),
                    ActionScript.Dword("DoNotOpenServerManagerAtLogon", 0)) +
                ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\ServerManager"),
                    ActionScript.Dword("DoNotOpenServerManagerAtLogon", 0),
                    ActionScript.Dword("CheckedUnattendLaunchSetting", 1)) +
                ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\ServerManager"),
                    ActionScript.Dword("DoNotOpenAtLogon", 0))) +
            "\r\nschtasks /Change /TN \"\\Microsoft\\Windows\\Server Manager\\ServerManager\" /ENABLE\r\n"));

        Add(SettingCatalog.HideServerManagerWacPrompt, ActionScript.DwordToggle(false,
            @"SOFTWARE\Microsoft\ServerManager", "DoNotPopWACConsoleAtSMLaunch", 1, 0));

        Add(SettingCatalog.DisableAzureArc, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
                ActionScript.DeleteValue("AzureArcSetup")),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
                ActionScript.Sz("AzureArcSetup", Optimizer.AzureArcCommand))));

        Add(SettingCatalog.EnableInstaller, ActionScript.Service("msiserver", enableMeansStart: true, disableWhenOff: false));
        Add(SettingCatalog.EnableWia, ActionScript.Service("stisvc", enableMeansStart: true, disableWhenOff: false));

        Add(SettingCatalog.DisablePasswordComplexity, ActionScript.Cmd(
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Netlogon\\Parameters\" /v RequireSignOrSeal /t REG_DWORD /d 0 /f >nul 2>&1\r\nreg add \"HKLM\\SAM\\SAM\\Domains\\Account\\Users\\000001F4\" /f >nul 2>&1\r\nrem 推荐：以管理员运行本软件应用，或使用 secedit/net accounts\r\nreg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /v LimitBlankPasswordUse /t REG_DWORD /d 0 /f >nul 2>&1\r\necho 密码复杂度：软件通过 net accounts / secedit / SAM 兜底写入 PasswordComplexity=0",
            "echo 恢复复杂度请用软件关闭本项，或 secedit 恢复默认策略",
            "账户策略建议优先用软件一键应用；手工脚本仅作参考。"));

        Add(SettingCatalog.PasswordNeverExpire, ActionScript.Cmd(
            "net accounts /maxpwage:unlimited",
            "net accounts /maxpwage:42"));

        Add(SettingCatalog.ShutdownWithoutLogon, ActionScript.DwordToggle(false,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ShutdownWithoutLogon", 1, 0));

        Add(SettingCatalog.DisableShutdownReason, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows NT\Reliability"),
                ActionScript.Dword("ShutdownReasonOn", 0),
                ActionScript.Dword("ShutdownReasonUI", 0)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability"),
                ActionScript.Dword("ShutdownReasonUI", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows NT\Reliability"),
                ActionScript.Dword("ShutdownReasonOn", 1),
                ActionScript.Dword("ShutdownReasonUI", 1)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability"),
                ActionScript.Dword("ShutdownReasonUI", 1))));

        Add(SettingCatalog.DisableCad, ActionScript.DwordToggle(false,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCAD", 1, 0));

        Add(SettingCatalog.EnableAutologon, ActionScript.Mixed(
            "开启自动登录涉及 DefaultUserName / DefaultPassword / AutoAdminLogon，\r\n请使用「工具 → 自动登录」或本软件开关；勿在明文 .reg 中保存密码。\r\n",
            ActionScript.WrapReg(
                ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"),
                    ActionScript.Sz("AutoAdminLogon", "0"),
                    ActionScript.DeleteValue("DefaultPassword"))),
            "关闭脚本可安全删除自动登录密码；开启请用软件界面。"));

        // —— Server 桌面增强 ——
        Add(SettingCatalog.DisableSmartScreenWarning, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer"),
                ActionScript.Dword("SmartScreenEnabled", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments"),
                ActionScript.Dword("SaveZoneInformation", 1)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer"),
                ActionScript.DeleteValue("SmartScreenEnabled")) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments"),
                ActionScript.DeleteValue("SaveZoneInformation"))));

        const string hideIcons = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
        const string clsidCpl = "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}";
        const string clsidBin = "{645FF040-5081-101B-9F08-00AA002F954E}";
        Add(SettingCatalog.ShowControlPanelRecycleBin, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(hideIcons),
                ActionScript.Dword(clsidCpl, 0),
                ActionScript.Dword(clsidBin, 0)),
            ActionScript.Block(ActionScript.HkCu(hideIcons),
                ActionScript.Dword(clsidCpl, 1),
                ActionScript.Dword(clsidBin, 1))));

        Add(SettingCatalog.LargeSystemCacheOptimize, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"),
                ActionScript.Dword("LargeSystemCache", 1),
                ActionScript.Dword("DisablePagingExecutive", 1)) +
            ActionScript.Block(ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\FileSystem"),
                ActionScript.Dword("NtfsMemoryUsage", 2)),
            ActionScript.Block(ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"),
                ActionScript.Dword("LargeSystemCache", 0),
                ActionScript.DeleteValue("DisablePagingExecutive")) +
            ActionScript.Block(ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\FileSystem"),
                ActionScript.DeleteValue("NtfsMemoryUsage")),
            "需重启生效。"));

        Add(SettingCatalog.DisableReservedStorage, ActionScript.DwordOnDeleteOff(false,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager", "ShippedWithReserves", 0));

        Add(SettingCatalog.DisableSrvSplit, ActionScript.DwordOnDeleteOff(false,
            @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SrvSplitThreshold", unchecked((int)0xFFFFFFFF)));

        Add(SettingCatalog.EnableGpuHwScheduling, ActionScript.DwordOnDeleteOff(false,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, "需重启。"));

        Add(SettingCatalog.DisableLoginKeyboardFilters, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\Keyboard Response"), ActionScript.Sz("Flags", "126")) +
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\StickyKeys"), ActionScript.Sz("Flags", "506")) +
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\ToggleKeys"), ActionScript.Sz("Flags", "58")) +
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\MouseKeys"), ActionScript.Sz("Flags", "0")),
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\Keyboard Response"), ActionScript.DeleteValue("Flags")) +
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\StickyKeys"), ActionScript.DeleteValue("Flags")) +
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\ToggleKeys"), ActionScript.DeleteValue("Flags")) +
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\MouseKeys"), ActionScript.DeleteValue("Flags"))));

        Add(SettingCatalog.DisableBackgroundApps, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"),
                ActionScript.Dword("LetAppsRunInBackground", 2)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"),
                ActionScript.Dword("GlobalUserDisabled", 1)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"),
                ActionScript.DeleteValue("LetAppsRunInBackground")) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"),
                ActionScript.DeleteValue("GlobalUserDisabled"))));

        Add(SettingCatalog.ClassicFileSearch, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                ActionScript.Dword("AllowCortana", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Search"),
                ActionScript.Dword("AllowSearchToUseLocation", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                ActionScript.DeleteValue("AllowCortana")) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Search"),
                ActionScript.DeleteValue("AllowSearchToUseLocation"))));

        Add(SettingCatalog.DisableSearchEngineFeature, ActionScript.Cmd(
            "dism.exe /online /Disable-Feature /FeatureName:SearchEngine-Client-Package /NoRestart",
            "dism.exe /online /Enable-Feature /FeatureName:SearchEngine-Client-Package /All /NoRestart",
            "DISM 功能；完成后建议重启。"));

        Add(SettingCatalog.EnableDesktopMediaFeatures, ActionScript.Cmd(
            "dism.exe /online /Enable-Feature /FeatureName:MediaPlayback /All /NoRestart\r\ndism.exe /online /Enable-Feature /FeatureName:WindowsMediaPlayer /All /NoRestart",
            "dism.exe /online /Disable-Feature /FeatureName:WindowsMediaPlayer /NoRestart",
            "功能名因系统版本可能略有差异，失败时请看 DISM 日志。"));

        Add(SettingCatalog.DisableServerBloatFeatures, ActionScript.Cmd(
            "echo 请使用本软件「应用到系统」批量 DISM 禁用 RSAT/WAC 等；手工列举过长且因版本而异。",
            "echo 恢复请在「启用或关闭 Windows 功能」中重新勾选所需组件。",
            "组合 DISM 操作，建议用软件一键应用。"));

        // —— Win11 桌面 ——
        Add(SettingCatalog.ShowItemCheckboxes, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "AutoCheckSelect", 1, 0));
        Add(SettingCatalog.ShowCommonFolders, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "NavPaneShowAllFolders", 1, 0));
        Add(SettingCatalog.RemoveAdminShield, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"),
                ActionScript.Sz("77", @"%LOCALAPPDATA%\WinOpt\blank.ico,0")),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"),
                ActionScript.DeleteValue("77")),
            "导入后重启资源管理器。"));
        Add(SettingCatalog.NoShortcutSuffix, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates"),
                ActionScript.Sz("ShortcutNameTemplate", "\"%s.lnk\"")) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer"),
                ActionScript.DeleteValue("Link")),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates"),
                ActionScript.DeleteValue("ShortcutNameTemplate")),
            "勿再使用旧的 Link=00,00,00,00（会导致桌面图标空白）。"));

        Add(SettingCatalog.Win11ExplorerStyle, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "UseCompactMode", 0, 1));

        Add(SettingCatalog.Win10ClassicContextMenu, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Classes\CLSID\{86ca1aa0-3389-4ff8-b098-4136676466e2}\InprocServer32"),
                "@=\"\""),
            "[-HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{86ca1aa0-3389-4ff8-b098-4136676466e2}]\r\n",
            "导入后重启资源管理器。"));

        Add(SettingCatalog.HideTaskbarSearch, TaskbarSearch(0));
        Add(SettingCatalog.TaskbarSearchBox, TaskbarSearch(2));

        Add(SettingCatalog.TaskbarAlignLeft, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", 0, 1));
        Add(SettingCatalog.TaskbarCombineAlways, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarGlomLevel", 0, 2));
        Add(SettingCatalog.TaskbarAutoHide, ActionScript.Cmd(
            "powershell -NoProfile -Command \"$p='HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3'; $b=(Get-ItemProperty $p).Settings; $b[8]=3; Set-ItemProperty $p Settings $b; Stop-Process -Name explorer -Force\"",
            "powershell -NoProfile -Command \"$p='HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3'; $b=(Get-ItemProperty $p).Settings; $b[8]=2; Set-ItemProperty $p Settings $b; Stop-Process -Name explorer -Force\"",
            "修改 StuckRects3 二进制第 9 字节（自动隐藏）。"));
        Add(SettingCatalog.ShowTaskViewButton, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 1, 0));
        Add(SettingCatalog.TaskbarEndTask, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EndTask", 1, 0));
        Add(SettingCatalog.DisableWidgets, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0, 1));
        Add(SettingCatalog.DisableSearchHighlights, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0, 1));
        Add(SettingCatalog.DisableRecommendedItems, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "Start_ShowRecentRecommendations", 0, 1));
        Add(SettingCatalog.DisableAdTracking, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, 1));
        Add(SettingCatalog.DisableSearchHistory, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Search", "HistoryViewEnabled", 0, 1));
        Add(SettingCatalog.DisableStickyKeys, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\StickyKeys"), ActionScript.Sz("Flags", "506")),
            ActionScript.Block(ActionScript.HkCu(@"Control Panel\Accessibility\StickyKeys"), ActionScript.Sz("Flags", "510"))));
        Add(SettingCatalog.DisablePca, ActionScript.Service("PcaSvc", enableMeansStart: false));
        Add(SettingCatalog.PauseFeatureUpdatesUntil2035, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"),
                ActionScript.Dword("PauseFeatureUpdates", 1),
                ActionScript.Dword("PauseFeatureUpdatesStartTime", (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                ActionScript.Dword("PauseFeatureUpdatesEndTime", 2051222400)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"),
                ActionScript.DeleteValue("PauseFeatureUpdates"),
                ActionScript.DeleteValue("PauseFeatureUpdatesStartTime"),
                ActionScript.DeleteValue("PauseFeatureUpdatesEndTime")),
            "结束时间约 2035-01-01 UTC。"));

        // —— 轻松设置 ——
        Add(SettingCatalog.HideProtectedOsFiles, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSuperHidden", 0, 1));
        Add(SettingCatalog.AlwaysShowIconsNeverThumbnails, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "IconsOnly", 1, 0));
        Add(SettingCatalog.ShowEmptyDrives, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideDrivesWithNoMedia", 0, 1));
        Add(SettingCatalog.ShowRecentFiles, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer"),
                ActionScript.Dword("ShowRecent", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("Start_TrackDocs", 1)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer"),
                ActionScript.Dword("ShowRecent", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("Start_TrackDocs", 0)),
            "开启=恢复显示；关闭=不显示（与开始屏幕 .reg 一致）。"));
        Add(SettingCatalog.NotepadWordWrap, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Notepad", "fWrap", 1, 0,
            "经典记事本；商店版记事本不读此键。"));
        Add(SettingCatalog.NotepadStatusBar, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Notepad", "StatusBar", 1, 0,
            "经典记事本；开启自动换行时状态栏可能被隐藏。"));
        Add(SettingCatalog.ShowFrequentPlaces, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowFrequent", 1, 0));
        Add(SettingCatalog.HideOfficeCloudFiles, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCloudFilesInQuickAccess", 0, 1));
        Add(SettingCatalog.DisableOneDrive, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1, 0));
        Add(SettingCatalog.HideTaskbarChat, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0, 1));
        Add(SettingCatalog.HideTaskbarCopilot, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarCo", 0, 1));
        Add(SettingCatalog.DisableCloudSearch, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCloudSearch", 0, 1));
        Add(SettingCatalog.DisableWebsiteLangList, ActionScript.DwordToggle(true,
            @"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1, 0));
        Add(SettingCatalog.DisableAppLaunchTracking, ActionScript.DwordToggle(true,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0, 1));
        Add(SettingCatalog.DisableSettingsSuggestions, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"),
                ActionScript.Dword("SubscribedContent-338393Enabled", 0),
                ActionScript.Dword("SystemPaneSuggestionsEnabled", 0)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"),
                ActionScript.Dword("SubscribedContent-338393Enabled", 1),
                ActionScript.Dword("SystemPaneSuggestionsEnabled", 1))));
        Add(SettingCatalog.DisableInkingPersonalization, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\InputPersonalization"),
                ActionScript.Dword("RestrictImplicitInkCollection", 1),
                ActionScript.Dword("RestrictImplicitTextCollection", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Input\Personalization"),
                ActionScript.Dword("RestrictImplicitInkCollection", 1),
                ActionScript.Dword("RestrictImplicitTextCollection", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Policies\TextInput"),
                ActionScript.Dword("AllowLinguisticDataCollection", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\InputPersonalization"),
                ActionScript.Dword("RestrictImplicitInkCollection", 0),
                ActionScript.Dword("RestrictImplicitTextCollection", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Input\Personalization"),
                ActionScript.Dword("RestrictImplicitInkCollection", 0),
                ActionScript.Dword("RestrictImplicitTextCollection", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Policies\TextInput"),
                ActionScript.Dword("AllowLinguisticDataCollection", 1))));
        Add(SettingCatalog.MsPinyinDefaultEnglish, ActionScript.DwordToggle(true,
            @"Software\Microsoft\InputMethod\Settings\CHS", "Default Mode", 1, 0));
        Add(SettingCatalog.DisableMsPinyinCloudAndInsights, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\InputMethod\Settings\CHS"),
                ActionScript.Dword("Enable Cloud Candidate", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\CPSS\Store\IME\Pinyin\Enable Cloud Candidate"),
                ActionScript.Dword("Value", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Input\Settings"),
                ActionScript.Dword("MultilingualEnabled", 0),
                ActionScript.Dword("EnableHwkbTextPrediction", 0),
                ActionScript.Dword("InsightsEnabled", 0),
                ActionScript.Dword("EnableTypingInsights", 0)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\InputMethod\Settings\CHS"),
                ActionScript.Dword("Enable Cloud Candidate", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\CPSS\Store\IME\Pinyin\Enable Cloud Candidate"),
                ActionScript.Dword("Value", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Input\Settings"),
                ActionScript.Dword("MultilingualEnabled", 1),
                ActionScript.Dword("EnableHwkbTextPrediction", 1),
                ActionScript.Dword("InsightsEnabled", 1),
                ActionScript.Dword("EnableTypingInsights", 1))));
        Add(SettingCatalog.DisableMsPinyinToolbar, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\InputMethod\Settings\CHS"),
                ActionScript.Dword("ToolBarEnabled", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\CTF\LangBar\ItemState\{ED9D5450-EBE6-4255-8289-F8A31E687228}"),
                ActionScript.Dword("DemoteLevel", 3)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Input\Settings"),
                ActionScript.Dword("DemoteLevel", 3)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\InputMethod\Settings\CHS"),
                ActionScript.Dword("ToolBarEnabled", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\CTF\LangBar\ItemState\{ED9D5450-EBE6-4255-8289-F8A31E687228}"),
                ActionScript.Dword("DemoteLevel", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Input\Settings"),
                ActionScript.Dword("DemoteLevel", 0))));
        Add(SettingCatalog.ExcludeMsrtFromWu, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\MRT", "DontOfferThroughWUAU", 1, 0));
        Add(SettingCatalog.DisableMeltdownSpectre, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"),
                ActionScript.Dword("FeatureSettingsOverride", 3),
                ActionScript.Dword("FeatureSettingsOverrideMask", 3)),
            ActionScript.Block(ActionScript.HkLm(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"),
                ActionScript.DeleteValue("FeatureSettingsOverride"),
                ActionScript.DeleteValue("FeatureSettingsOverrideMask")),
            "需重启；降低侧信道防护。"));
        Add(SettingCatalog.DisableMemoryIntegrity, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0, 1, "需重启。"));
        Add(SettingCatalog.DisableWdac, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "ConfigCIPolicyEnable", 0, 1));
        Add(SettingCatalog.DisableVbs, ActionScript.DwordOnDeleteOff(false,
            @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, "需重启。"));
        Add(SettingCatalog.EnableTcpBbr2, ActionScript.Cmd(
            "netsh int tcp set supplemental template=internet CongestionProvider=bbr2",
            "netsh int tcp set supplemental template=internet CongestionProvider=cubic"));
        Add(SettingCatalog.DisableSystemRestore, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableSR", 1, 0));
        Add(SettingCatalog.DisableCeip, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0, 1));
        Add(SettingCatalog.DisableDiagnosticPolicy, ActionScript.Service("DPS", enableMeansStart: false));
        Add(SettingCatalog.DisableRemoteAssistance, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "fAllowToGetHelp", 0, 1));
        Add(SettingCatalog.DisableMemoryCompression, ActionScript.Ps(
            "Disable-MMAgent -MemoryCompression",
            "Enable-MMAgent -MemoryCompression"));
        Add(SettingCatalog.DisableAppPrelaunch, ActionScript.Ps(
            "Disable-MMAgent -ApplicationPreLaunch",
            "Enable-MMAgent -ApplicationPreLaunch"));
        Add(SettingCatalog.DisablePageCombining, ActionScript.Ps(
            "Disable-MMAgent -PageCombining",
            "Enable-MMAgent -PageCombining"));
        Add(SettingCatalog.DisableUcpdDriver, ActionScript.Service("UCPD", enableMeansStart: false));

        // —— 竞品常用 ——
        Add(SettingCatalog.DisableCortana, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                ActionScript.Dword("AllowCortana", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Search"),
                ActionScript.Dword("CortanaConsent", 0),
                ActionScript.Dword("AllowSearchToUseLocation", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                ActionScript.Dword("AllowCortana", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Search"),
                ActionScript.Dword("CortanaConsent", 1),
                ActionScript.Dword("AllowSearchToUseLocation", 1))));
        Add(SettingCatalog.DisableCopilotAi, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"),
                ActionScript.Dword("TurnOffWindowsCopilot", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Policies\Microsoft\Windows\WindowsCopilot"),
                ActionScript.Dword("TurnOffWindowsCopilot", 1)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Edge"),
                ActionScript.Dword("HubsSidebarEnabled", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("ShowCopilotButton", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"),
                ActionScript.Dword("TurnOffWindowsCopilot", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Policies\Microsoft\Windows\WindowsCopilot"),
                ActionScript.Dword("TurnOffWindowsCopilot", 0)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Edge"),
                ActionScript.Dword("HubsSidebarEnabled", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                ActionScript.Dword("ShowCopilotButton", 1))));
        Add(SettingCatalog.DisableOfficeTelemetry, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Office\Common\ClientTelemetry"),
                ActionScript.Dword("DisableTelemetry", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Policies\Microsoft\office\16.0\osm"),
                ActionScript.Dword("enablelogging", 0),
                ActionScript.Dword("enableupload", 0)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Office\16.0\osm"),
                ActionScript.Dword("enablelogging", 0),
                ActionScript.Dword("enableupload", 0)),
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Office\Common\ClientTelemetry"),
                ActionScript.Dword("DisableTelemetry", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Policies\Microsoft\office\16.0\osm"),
                ActionScript.Dword("enablelogging", 1),
                ActionScript.Dword("enableupload", 1)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Office\16.0\osm"),
                ActionScript.Dword("enablelogging", 1),
                ActionScript.Dword("enableupload", 1))));
        Add(SettingCatalog.EnableUtcTime, ActionScript.DwordToggle(false,
            @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal", 1, 0));
        Add(SettingCatalog.DisableHpet, ActionScript.Cmd(
            "bcdedit /set useplatformclock false\r\nbcdedit /set disabledynamictick yes",
            "bcdedit /deletevalue useplatformclock\r\nbcdedit /deletevalue disabledynamictick",
            "需重启。"));
        Add(SettingCatalog.EnableLoginVerbose, ActionScript.DwordToggle(false,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", 1, 0));
        Add(SettingCatalog.DisableNetworkThrottling, ActionScript.DwordOnDeleteOff(false,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
            "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF)));
        Add(SettingCatalog.DisableGameDvr, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"),
                ActionScript.Dword("AllowGameDVR", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"System\GameConfigStore"),
                ActionScript.Dword("GameDVR_Enabled", 0)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\GameDVR"),
                ActionScript.Dword("AppCaptureEnabled", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"),
                ActionScript.Dword("AllowGameDVR", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"System\GameConfigStore"),
                ActionScript.Dword("GameDVR_Enabled", 1)) +
            ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\GameDVR"),
                ActionScript.Dword("AppCaptureEnabled", 1))));
        Add(SettingCatalog.DisableLocationTracking, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1, 0));
        Add(SettingCatalog.DisableConsumerFeatures, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1, 0));
        Add(SettingCatalog.DisableEdgePreload, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Edge"),
                ActionScript.Dword("StartupBoostEnabled", 0),
                ActionScript.Dword("BackgroundModeEnabled", 0)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main"),
                ActionScript.Dword("AllowPrelaunch", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Edge"),
                ActionScript.Dword("StartupBoostEnabled", 1),
                ActionScript.Dword("BackgroundModeEnabled", 1)) +
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main"),
                ActionScript.Dword("AllowPrelaunch", 1))));
        Add(SettingCatalog.DisableTeredo, ActionScript.Cmd(
            "netsh interface teredo set state disabled",
            "netsh interface teredo set state default"));
        Add(SettingCatalog.DisableClipboardCloud, ActionScript.Reg(
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\System"),
                ActionScript.Dword("AllowCrossDeviceClipboard", 0),
                ActionScript.Dword("AllowClipboardHistory", 0)),
            ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\System"),
                ActionScript.Dword("AllowCrossDeviceClipboard", 1),
                ActionScript.Dword("AllowClipboardHistory", 1))));
        Add(SettingCatalog.DisableNtfsLastAccess, ActionScript.Cmd(
            "fsutil behavior set disablelastaccess 1",
            "fsutil behavior set disablelastaccess 0"));
        Add(SettingCatalog.DisableXboxServices, ActionScript.Cmd(
            "sc stop XblAuthManager\r\nsc config XblAuthManager start= disabled\r\nsc stop XblGameSave\r\nsc config XblGameSave start= disabled\r\nsc stop XboxGipSvc\r\nsc config XboxGipSvc start= disabled\r\nsc stop XboxNetApiSvc\r\nsc config XboxNetApiSvc start= disabled",
            "sc config XblAuthManager start= demand\r\nsc config XblGameSave start= demand\r\nsc config XboxGipSvc start= demand\r\nsc config XboxNetApiSvc start= demand"));
        Add(SettingCatalog.DisableFaxService, ActionScript.Service("Fax", enableMeansStart: false));
        Add(SettingCatalog.EnableF8BootMenu, ActionScript.Cmd(
            "bcdedit /set {current} bootmenupolicy legacy",
            "bcdedit /set {current} bootmenupolicy standard"));
        Add(SettingCatalog.ContextMenuTakeOwnership, ActionScript.Cmd(
            "echo 右键「取得所有权」由 ContextMenuTweaks 写入 HKCR；请用软件开关一键应用。",
            "echo 关闭请用软件关闭本项。",
            "HKCR 组合项较多，建议软件应用。"));
        Add(SettingCatalog.ContextMenuOpenCmd, ActionScript.Reg(
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Classes\Directory\shell\WinOptOpenCmd"),
                "@=\"在此处打开命令提示符\"") +
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Classes\Directory\shell\WinOptOpenCmd\\command"),
                "@=\"cmd.exe /s /k pushd \\\"%V\\\"\"") +
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Classes\Directory\\Background\\shell\\WinOptOpenCmd"),
                "@=\"在此处打开命令提示符\"") +
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Classes\Directory\\Background\\shell\\WinOptOpenCmd\\command"),
                "@=\"cmd.exe /s /k pushd \\\"%V\\\"\""),
            "[-HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\Directory\\shell\\WinOptOpenCmd]\r\n" +
            "[-HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\Directory\\Background\\shell\\WinOptOpenCmd]\r\n" +
            "[-HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\Folder\\shell\\OpenDOSBox]\r\n",
            "开启用 Directory+Background；关闭同时清除 OpenDOSBox 旧键。"));
        Add(SettingCatalog.ContextMenuCopyMoveTo, ActionScript.Reg(
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\Copy To"),
                "@=\"{C2FBB630-2971-11D1-A18C-00C04FD75D13}\"") +
            ActionScript.Block(
                ActionScript.HkLm(@"SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\Move To"),
                "@=\"{C2FBB631-2971-11D1-A18C-00C04FD75D13}\""),
            "[-HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To]\r\n" +
            "[-HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Move To]\r\n",
            "开启后文件/文件夹右键出现「复制到文件夹」「移动到文件夹」。"));
        Add(SettingCatalog.ContextMenuQuickOps, ActionScript.Cmd(
            "echo 快捷操作组（QwhMenu + CommandStore）由 ContextMenuTweaks 写入；请用软件开关一键应用。",
            "echo 关闭请用软件关闭本项（对齐删除右键快捷操作组菜单.reg）。",
            "级联菜单 + 多条 CommandStore，建议软件应用。"));
        Add(SettingCatalog.DisableMediaPlayerSharing, ActionScript.Service("WMPNetworkSvc", enableMeansStart: false));
        Add(SettingCatalog.DisableInsiderService, ActionScript.Service("wisvc", enableMeansStart: false));
        Add(SettingCatalog.DisableStoreAutoUpdate, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2, 4));
        Add(SettingCatalog.DisableNewsInterests, ActionScript.DwordToggle(false,
            @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0, 1));

        return m;
    }

    static SettingActionRecipe TaskbarSearch(int modeOn)
    {
        // modeOn: 0=隐藏 2=搜索框；关闭时回落到 1=仅图标（与软件默认习惯接近）
        var modeOff = modeOn == 0 ? 1 : 1;
        string Body(int mode)
        {
            var pol = mode == 0
                ? ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                    ActionScript.Dword("ConfigureSearchOnTaskbarMode", 0))
                : ActionScript.Block(ActionScript.HkLm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"),
                    ActionScript.DeleteValue("ConfigureSearchOnTaskbarMode"));
            return ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Search"),
                       ActionScript.Dword("SearchboxTaskbarMode", mode),
                       ActionScript.Dword("SearchboxTaskbarModeCache", 1)) +
                   ActionScript.Block(ActionScript.HkCu(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"),
                       ActionScript.Dword("SearchboxTaskbarMode", mode)) +
                   pol;
        }

        return ActionScript.Reg(Body(modeOn), Body(modeOff), "导入后重启资源管理器。");
    }
}
