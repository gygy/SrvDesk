namespace WinOpt;

/// <summary>应用顶栏菜单：文件 / 预设 / 工具 / 视图 / 帮助。</summary>
internal sealed class AppMenuStrip : MenuStrip
{
    public ToolStripMenuItem FileImport { get; }
    public ToolStripMenuItem FileExport { get; }
    public ToolStripMenuItem ToolAutologon { get; }
    public ToolStripMenuItem ToolIdentity { get; }
    public ToolStripMenuItem ToolSystemInfo { get; }
    public ToolStripMenuItem ToolHosts { get; }
    public ToolStripMenuItem ToolEventViewer { get; }
    public ToolStripMenuItem ToolGroupPolicy { get; }
    public ToolStripMenuItem ToolCmd { get; }
    public ToolStripMenuItem ToolPowerShell { get; }
    public ToolStripMenuItem ToolTaskScheduler { get; }
    public ToolStripMenuItem ToolComputerMgmt { get; }
    public ToolStripMenuItem ToolFlushDns { get; }
    public ToolStripMenuItem ToolCommonSoftware { get; }
    public ToolStripMenuItem ToolCleanup { get; }
    public ToolStripMenuItem ToolDesktopMaintenance { get; }
    public ToolStripMenuItem ToolPowerExtras { get; }
    public ToolStripMenuItem ToolWindowsFeatures { get; }
    public ToolStripMenuItem ToolContextMenu { get; }
    public ToolStripMenuItem ToolQuick { get; }
    public ToolStripMenuItem ToolRefresh { get; }
    public ToolStripMenuItem ToolRestoreDefaults { get; }
    public ToolStripMenuItem ViewAllOn { get; }
    public ToolStripMenuItem ViewAllOff { get; }
    public ToolStripMenuItem ViewHideIncompatible { get; }
    public ToolStripMenuItem ViewHelpPanel { get; }
    public ToolStripMenuItem HelpUsage { get; }
    public ToolStripMenuItem HelpLegend { get; }
    public ToolStripMenuItem HelpChangeLog { get; }
    public ToolStripMenuItem HelpLog { get; }
    public ToolStripMenuItem HelpAbout { get; }

    public AppMenuStrip(IReadOnlyList<OptPresets.PresetInfo> presets)
    {
        BackColor = AppTheme.SurfaceCard;
        ForeColor = AppTheme.TextMain;
        Renderer = new ToolStripProfessionalRenderer(new AppMenuColorTable());
        Padding = new Padding(4, 2, 0, 2);
        ImageScalingSize = new Size(16, 16);
        ShowItemToolTips = true;

        var file = new ToolStripMenuItem("文件(&F)");
        FileImport = new ToolStripMenuItem("导入配置(&O)...", null, null, Keys.Control | Keys.O);
        FileExport = new ToolStripMenuItem("导出配置(&S)...", null, null, Keys.Control | Keys.S);
        file.DropDownItems.AddRange([FileImport, FileExport]);

        var preset = new ToolStripMenuItem("预设(&P)");
        foreach (var p in presets)
        {
            var item = new ToolStripMenuItem(p.Title) { Tag = p };
            item.ToolTipText = p.Description;
            preset.DropDownItems.Add(item);
        }

        var tools = new ToolStripMenuItem("工具(&T)");
        ToolAutologon = Item("Autologon 配置...", MenuIcons.Autologon);
        ToolIdentity = Item("计算机名 / 工作组...", MenuIcons.Identity);
        ToolSystemInfo = Item("系统信息...", MenuIcons.SystemInfo);
        ToolHosts = Item("编辑 hosts...", MenuIcons.Hosts);
        ToolFlushDns = Item("刷新 DNS 缓存", MenuIcons.FlushDns);
        ToolEventViewer = Item("事件查看器", MenuIcons.EventViewer);
        ToolGroupPolicy = Item("组策略...", MenuIcons.GroupPolicy);
        ToolCmd = Item("命令提示符", MenuIcons.Cmd);
        ToolPowerShell = Item("Windows PowerShell", MenuIcons.PowerShell);
        ToolTaskScheduler = Item("计划任务", MenuIcons.TaskScheduler);
        ToolComputerMgmt = Item("计算机管理", MenuIcons.ComputerMgmt);
        ToolCommonSoftware = Item("常用软件...", MenuIcons.CommonSoftware);
        ToolCleanup = Item("垃圾清理...", MenuIcons.Cleanup);
        ToolDesktopMaintenance = Item("桌面维护...", MenuIcons.DesktopMaintenance);
        ToolPowerExtras = Item("高级设置...", MenuIcons.Advanced);
        ToolWindowsFeatures = Item("可选功能 / Capabilities...", MenuIcons.WindowsFeatures);
        ToolContextMenu = Item("右键菜单...", MenuIcons.ContextMenu);
        ToolQuick = Item("快速工具...", MenuIcons.Quick);
        ToolRefresh = Item("刷新当前状态", MenuIcons.Refresh, Keys.F5);
        ToolRestoreDefaults = Item("恢复出厂默认...", MenuIcons.Restore);

        // 分组：本机身份 → 网络 → 系统管理 → 维护优化 → 状态
        tools.DropDownItems.AddRange([
            ToolAutologon, ToolIdentity, ToolSystemInfo,
            new ToolStripSeparator(),
            ToolHosts, ToolFlushDns,
            new ToolStripSeparator(),
            ToolEventViewer, ToolGroupPolicy, ToolCmd, ToolPowerShell, ToolTaskScheduler, ToolComputerMgmt,
            new ToolStripSeparator(),
            ToolCommonSoftware, ToolCleanup, ToolDesktopMaintenance, ToolPowerExtras, ToolWindowsFeatures, ToolContextMenu,
            new ToolStripSeparator(),
            ToolQuick, ToolRefresh, ToolRestoreDefaults,
        ]);

        var view = new ToolStripMenuItem("视图(&V)");
        ViewAllOn = new ToolStripMenuItem("全部开启当前页");
        ViewAllOff = new ToolStripMenuItem("全部关闭当前页");
        ViewHideIncompatible = new ToolStripMenuItem("隐藏不适用项") { CheckOnClick = true };
        ViewHelpPanel = new ToolStripMenuItem("显示帮助面板") { CheckOnClick = true, Checked = false };
        view.DropDownItems.AddRange([
            ViewAllOn, ViewAllOff, new ToolStripSeparator(),
            ViewHideIncompatible, ViewHelpPanel
        ]);

        var help = new ToolStripMenuItem("帮助(&H)");
        HelpUsage = new ToolStripMenuItem("使用说明", null, null, Keys.F1);
        HelpLegend = new ToolStripMenuItem("标识图例...");
        HelpChangeLog = new ToolStripMenuItem("打开变更日志...");
        HelpLog = new ToolStripMenuItem("打开操作日志...");
        HelpAbout = new ToolStripMenuItem($"关于 {AppBrand.ProductName}...");
        help.DropDownItems.AddRange([
            HelpUsage, HelpLegend, new ToolStripSeparator(),
            HelpChangeLog, HelpLog, HelpAbout
        ]);

        Items.AddRange([file, preset, tools, view, help]);
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
