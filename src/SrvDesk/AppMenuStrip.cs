namespace SrvDesk;

/// <summary>应用顶栏菜单：文件 / 工具 / 视图 / 帮助。</summary>
internal sealed class AppMenuStrip : MenuStrip
{
    public ToolStripMenuItem FileImport { get; }
    public ToolStripMenuItem FileExport { get; }
    public ToolStripMenuItem FileSettings { get; }
    public ToolStripMenuItem FileExit { get; }
    public ToolStripMenuItem ToolAutologon { get; }
    public ToolStripMenuItem ToolAccountIdentity { get; }
    public ToolStripMenuItem ToolSystemInfo { get; }
    public ToolStripMenuItem ToolHosts { get; }
    public ToolStripMenuItem ToolEventViewer { get; }
    public ToolStripMenuItem ToolGroupPolicy { get; }
    public ToolStripMenuItem ToolCmd { get; }
    public ToolStripMenuItem ToolPowerShell { get; }
    public ToolStripMenuItem ToolScheduledTaskOptimize { get; }
    public ToolStripMenuItem ToolComputerMgmt { get; }
    public ToolStripMenuItem ToolFlushDns { get; }
    public ToolStripMenuItem ToolCommonSoftware { get; }
    public ToolStripMenuItem ToolCleanup { get; }
    public ToolStripMenuItem ToolSystemRepair { get; }
    public ToolStripMenuItem ToolShutdownTimer { get; }
    public ToolStripMenuItem ToolOptimizeAdvisor { get; }
    public ToolStripMenuItem ToolPortExposure { get; }
    public ToolStripMenuItem ToolOptHistory { get; }
    public ToolStripMenuItem ToolDesktopMaintenance { get; }
    public ToolStripMenuItem ToolWindowsFeatures { get; }
    public ToolStripMenuItem ToolSecurityCenter { get; }
    public ToolStripMenuItem ToolEdgeManage { get; }
    public ToolStripMenuItem ToolContextMenu { get; }
    public ToolStripMenuItem ToolQuick { get; }
    public ToolStripMenuItem ToolRefresh { get; }
    public ToolStripMenuItem ToolRestoreDefaults { get; }
    public ToolStripMenuItem ViewAllOn { get; }
    public ToolStripMenuItem ViewAllOff { get; }
    public ToolStripMenuItem ViewHideIncompatible { get; }
    public ToolStripMenuItem ViewHelpPanel { get; }
    public ToolStripMenuItem PresetRoot { get; }
    public ToolStripMenuItem HelpChangeLog { get; }
    public ToolStripMenuItem HelpLog { get; }
    public ToolStripMenuItem HelpDebugLog { get; }
    public ToolStripMenuItem HelpDisclaimer { get; }
    public ToolStripMenuItem HelpPrivacy { get; }
    public ToolStripMenuItem HelpLicense { get; }
    public ToolStripMenuItem HelpCheckUpdate { get; }
    public ToolStripMenuItem HelpSupport { get; }

    public AppMenuStrip()
    {
        BackColor = AppTheme.SurfaceCard;
        ForeColor = AppTheme.TextMain;
        Renderer = new ToolStripProfessionalRenderer(new AppMenuColorTable());
        Padding = new Padding(4, 2, 0, 2);
        ImageScalingSize = new Size(16, 16);
        ShowItemToolTips = true;

        var file = new ToolStripMenuItem(AppLang.L("文件(&F)", "File(&F)"));
        FileImport = Item(AppLang.L("导入配置(&O)...", "Import profile(&O)..."), MenuIcons.Import, Keys.Control | Keys.O);
        FileExport = Item(AppLang.L("导出配置(&S)...", "Export profile(&S)..."), MenuIcons.Export, Keys.Control | Keys.S);
        FileSettings = Item(AppLang.L("程序设置(&P)...", "Settings(&P)..."), MenuIcons.Advanced);
        FileExit = Item(AppLang.L("退出(&X)", "Exit(&X)"), MenuIcons.PanelClose, Keys.Alt | Keys.F4);
        file.DropDownItems.AddRange([
            FileImport, FileExport, new ToolStripSeparator(), FileSettings,
            new ToolStripSeparator(), FileExit,
        ]);

        var tools = new ToolStripMenuItem(AppLang.L("工具(&T)", "Tools(&T)"));
        ToolAutologon = Item(AppLang.L("Autologon 配置...", "Autologon..."), MenuIcons.Autologon);
        ToolAccountIdentity = Item(AppLang.L("账户与计算机名...", "Account / computer name..."), MenuIcons.Identity);
        ToolSystemInfo = Item(AppLang.L("系统信息...", "System info..."), MenuIcons.SystemInfo);
        ToolHosts = Item(AppLang.L("编辑 hosts...", "Edit hosts..."), MenuIcons.Hosts);
        ToolFlushDns = Item(AppLang.L("刷新 DNS 缓存", "Flush DNS cache"), MenuIcons.FlushDns);
        ToolEventViewer = Item(AppLang.L("事件查看器", "Event Viewer"), MenuIcons.EventViewer);
        ToolGroupPolicy = Item(AppLang.L("组策略...", "Group Policy..."), MenuIcons.GroupPolicy);
        ToolCmd = Item(AppLang.L("命令提示符", "Command Prompt"), MenuIcons.Cmd);
        ToolPowerShell = Item("Windows PowerShell", MenuIcons.PowerShell);
        ToolScheduledTaskOptimize = Item(AppLang.L("计划任务优化...", "Scheduled tasks optimize..."), MenuIcons.TaskScheduler);
        ToolComputerMgmt = Item(AppLang.L("计算机管理", "Computer Management"), MenuIcons.ComputerMgmt);
        ToolCommonSoftware = Item(AppLang.L("常用软件...", "Common software..."), MenuIcons.CommonSoftware);
        ToolCleanup = Item(AppLang.L("垃圾清理...", "Junk cleanup..."), MenuIcons.Cleanup);
        ToolSystemRepair = Item(AppLang.L("系统修复...", "System repair..."), MenuIcons.Advanced);
        ToolShutdownTimer = Item(AppLang.L("定时关机...", "Shutdown timer..."), MenuIcons.ShutdownTimer);
        ToolOptimizeAdvisor = Item(AppLang.L("优化顾问...", "Optimization advisor..."), MenuIcons.SystemInfo);
        ToolPortExposure = Item(AppLang.L("端口暴露...", "Port exposure..."), MenuIcons.SecurityCenter);
        ToolOptHistory = Item(AppLang.L("回滚优化...", "Rollback optimization..."), MenuIcons.Restore);
        ToolDesktopMaintenance = Item(AppLang.L("桌面维护...", "Desktop maintenance..."), MenuIcons.DesktopMaintenance);
        ToolWindowsFeatures = Item(AppLang.L("可选功能 / Capabilities...", "Optional features / Capabilities..."), MenuIcons.WindowsFeatures);
        ToolSecurityCenter = Item(AppLang.L("安全中心管理...", "Security Center..."), MenuIcons.SecurityCenter);
        ToolEdgeManage = Item(AppLang.L("MSEdge 管理...", "MSEdge management..."), MenuIcons.EdgeManage);
        ToolContextMenu = Item(AppLang.L("右键菜单...", "Context menu..."), MenuIcons.ContextMenu);
        ToolQuick = Item(AppLang.L("快速工具...", "Quick tools..."), MenuIcons.Quick);
        ToolRefresh = Item(AppLang.L("刷新当前状态", "Refresh status"), MenuIcons.Refresh, Keys.F5);
        ToolRestoreDefaults = Item(AppLang.L("恢复出厂默认...", "Restore defaults..."), MenuIcons.Restore);

        tools.DropDownItems.AddRange([
            ToolOptimizeAdvisor, ToolOptHistory,
            ToolAutologon, ToolAccountIdentity,
            ToolCommonSoftware, ToolContextMenu, ToolCleanup,
            ToolWindowsFeatures, ToolSecurityCenter, ToolEdgeManage,
            ToolHosts,
            new ToolStripSeparator(),
            ToolShutdownTimer, ToolSystemRepair, ToolDesktopMaintenance,
            new ToolStripSeparator(),
            ToolSystemInfo, ToolPortExposure, ToolFlushDns,
            new ToolStripSeparator(),
            ToolEventViewer, ToolGroupPolicy, ToolCmd, ToolPowerShell, ToolScheduledTaskOptimize, ToolComputerMgmt,
            new ToolStripSeparator(),
            ToolQuick, ToolRefresh, ToolRestoreDefaults,
        ]);

        var view = new ToolStripMenuItem(AppLang.L("视图(&V)", "View(&V)"));
        ViewAllOn = Item(AppLang.L("全部开启当前页", "Enable all on this page"), MenuIcons.ViewAllOn);
        ViewAllOff = Item(AppLang.L("全部关闭当前页", "Disable all on this page"), MenuIcons.ViewAllOff);
        ViewHideIncompatible = Item(AppLang.L("隐藏不适用项", "Hide incompatible items"), MenuIcons.ViewHide);
        ViewHideIncompatible.CheckOnClick = true;
        ViewHelpPanel = Item(AppLang.L("显示配置脚本", "Show config script"), MenuIcons.ViewHelpPanel);
        ViewHelpPanel.CheckOnClick = true;
        ViewHelpPanel.Checked = false;
        ViewHelpPanel.ToolTipText = AppLang.L(
            "显示配置脚本面板：查看/编辑开启与关闭脚本（停靠位置在面板顶部切换）",
            "Show the config script panel to view/edit on/off scripts (dock position switches at the top of the panel)");
        view.DropDownItems.AddRange([
            ViewAllOn, ViewAllOff, new ToolStripSeparator(),
            ViewHideIncompatible, ViewHelpPanel,
        ]);

        PresetRoot = new ToolStripMenuItem(AppLang.L("预设(&P)", "Presets(&P)"));

        var help = new ToolStripMenuItem(AppLang.L("帮助(&H)", "Help(&H)"));
        HelpCheckUpdate = Item(AppLang.L("检查更新...", "Check for updates..."), MenuIcons.HelpCheckUpdate);
        HelpChangeLog = Item(AppLang.L("变更日志...", "Change log..."), MenuIcons.HelpChangeLog);
        HelpLog = Item(AppLang.L("操作日志...", "Operation log..."), MenuIcons.HelpLog);
        HelpDebugLog = Item(AppLang.L("调试日志...", "Debug log..."), MenuIcons.HelpLog);
        HelpDisclaimer = Item(AppLang.L("免责声明...", "Disclaimer..."), MenuIcons.HelpUsage);
        HelpPrivacy = Item(AppLang.L("隐私说明...", "Privacy..."), MenuIcons.HelpLegend);
        HelpLicense = Item(AppLang.L("许可证...", "License..."), MenuIcons.HelpChangeLog);
        HelpSupport = Item(AppLang.L("支持", "Support"), MenuIcons.HelpAbout);
        help.DropDownItems.AddRange([
            HelpCheckUpdate, HelpChangeLog, HelpLog, HelpDebugLog,
            new ToolStripSeparator(),
            HelpDisclaimer, HelpPrivacy, HelpLicense,
            new ToolStripSeparator(),
            HelpSupport,
        ]);

        Items.AddRange([file, PresetRoot, tools, view, help]);
    }

    private static ToolStripMenuItem Item(string text, Image image, Keys shortcut = Keys.None)
    {
        var item = shortcut == Keys.None
            ? new ToolStripMenuItem(text, image)
            : new ToolStripMenuItem(text, image, null, shortcut);
        item.ImageScaling = ToolStripItemImageScaling.None;
        return item;
    }

    private sealed class AppMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => AppTheme.PrimaryPale;
        public override Color MenuItemSelectedGradientBegin => AppTheme.PrimaryPale;
        public override Color MenuItemSelectedGradientEnd => AppTheme.PrimaryPale;
        public override Color MenuItemBorder => AppTheme.BorderLight;
        public override Color MenuBorder => AppTheme.Border;
        public override Color ToolStripDropDownBackground => AppTheme.SurfaceCard;
        public override Color ImageMarginGradientBegin => AppTheme.SurfaceCard;
        public override Color ImageMarginGradientMiddle => AppTheme.SurfaceCard;
        public override Color ImageMarginGradientEnd => AppTheme.SurfaceCard;
    }
}
