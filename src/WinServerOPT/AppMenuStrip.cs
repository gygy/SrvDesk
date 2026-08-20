namespace WinOpt;

/// <summary>应用顶栏菜单：文件 / 预设 / 工具 / 视图 / 帮助。</summary>
internal sealed class AppMenuStrip : MenuStrip
{
    public ToolStripMenuItem FileImport { get; }
    public ToolStripMenuItem FileExport { get; }
    public ToolStripMenuItem PresetLoad { get; }
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
    public ToolStripMenuItem ToolExplorer { get; }
    public ToolStripMenuItem ToolPrivacy { get; }
    public ToolStripMenuItem ToolOther { get; }
    public ToolStripMenuItem ToolStartup { get; }
    public ToolStripMenuItem ToolDns { get; }
    public ToolStripMenuItem ToolCleanup { get; }
    public ToolStripMenuItem ToolDesktopMaintenance { get; }
    public ToolStripMenuItem ToolQuick { get; }
    public ToolStripMenuItem ToolRefresh { get; }
    public ToolStripMenuItem ViewHideIncompatible { get; }
    public ToolStripMenuItem ViewHelpPanel { get; }
    public ToolStripMenuItem HelpUsage { get; }
    public ToolStripMenuItem HelpLegend { get; }
    public ToolStripMenuItem HelpLog { get; }
    public ToolStripMenuItem HelpAbout { get; }

    public AppMenuStrip(IReadOnlyList<OptPresets.PresetInfo> presets)
    {
        BackColor = AppTheme.SurfaceCard;
        ForeColor = AppTheme.TextMain;
        Renderer = new ToolStripProfessionalRenderer(new AppMenuColorTable());
        Padding = new Padding(4, 2, 0, 2);

        var file = new ToolStripMenuItem("文件(&F)");
        FileImport = new ToolStripMenuItem("导入配置(&O)...", null, null, Keys.Control | Keys.O);
        FileExport = new ToolStripMenuItem("导出配置(&S)...", null, null, Keys.Control | Keys.S);
        file.DropDownItems.AddRange([FileImport, FileExport]);

        var preset = new ToolStripMenuItem("预设(&P)");
        PresetLoad = new ToolStripMenuItem("载入当前所选预设", null, null, Keys.Control | Keys.L);
        preset.DropDownItems.Add(PresetLoad);
        preset.DropDownItems.Add(new ToolStripSeparator());
        foreach (var p in presets)
        {
            var item = new ToolStripMenuItem(p.Title) { Tag = p };
            item.ToolTipText = p.Description;
            preset.DropDownItems.Add(item);
        }

        var tools = new ToolStripMenuItem("工具(&T)");
        ToolAutologon = new ToolStripMenuItem("Autologon 配置...");
        ToolIdentity = new ToolStripMenuItem("计算机名 / 工作组...");
        ToolSystemInfo = new ToolStripMenuItem("系统信息...");
        ToolHosts = new ToolStripMenuItem("编辑 hosts...");
        ToolEventViewer = new ToolStripMenuItem("事件查看器");
        ToolGroupPolicy = new ToolStripMenuItem("组策略...");
        ToolCmd = new ToolStripMenuItem("命令提示符");
        ToolPowerShell = new ToolStripMenuItem("Windows PowerShell");
        ToolTaskScheduler = new ToolStripMenuItem("计划任务");
        ToolComputerMgmt = new ToolStripMenuItem("计算机管理");
        ToolFlushDns = new ToolStripMenuItem("刷新 DNS 缓存");
        ToolCommonSoftware = new ToolStripMenuItem("常用软件...");
        ToolExplorer = new ToolStripMenuItem("Explorer 设置...");
        ToolPrivacy = new ToolStripMenuItem("隐私设置...");
        ToolOther = new ToolStripMenuItem("其他设置...");
        ToolStartup = new ToolStripMenuItem("启动项管理...");
        ToolDns = new ToolStripMenuItem("DNS 切换...");
        ToolCleanup = new ToolStripMenuItem("垃圾清理...");
        ToolDesktopMaintenance = new ToolStripMenuItem("桌面维护...");
        ToolQuick = new ToolStripMenuItem("快速工具...");
        ToolRefresh = new ToolStripMenuItem("刷新当前状态", null, null, Keys.F5);
        tools.DropDownItems.AddRange([
            ToolAutologon, ToolIdentity, ToolSystemInfo, ToolHosts,
            ToolEventViewer, ToolGroupPolicy, ToolCmd, ToolPowerShell, ToolTaskScheduler, ToolComputerMgmt,
            ToolFlushDns, ToolDns, ToolCleanup, ToolCommonSoftware, ToolExplorer, ToolPrivacy, ToolOther, ToolStartup, ToolDesktopMaintenance,
            new ToolStripSeparator(), ToolQuick, ToolRefresh
        ]);

        var view = new ToolStripMenuItem("视图(&V)");
        ViewHideIncompatible = new ToolStripMenuItem("隐藏不适用项") { CheckOnClick = true };
        ViewHelpPanel = new ToolStripMenuItem("显示帮助面板") { CheckOnClick = true, Checked = true };
        view.DropDownItems.AddRange([ViewHideIncompatible, ViewHelpPanel]);

        var help = new ToolStripMenuItem("帮助(&H)");
        HelpUsage = new ToolStripMenuItem("使用说明", null, null, Keys.F1);
        HelpLegend = new ToolStripMenuItem("标识图例...");
        HelpLog = new ToolStripMenuItem("打开操作日志");
        HelpAbout = new ToolStripMenuItem("关于 Win一键优化...");
        help.DropDownItems.AddRange([HelpUsage, HelpLegend, new ToolStripSeparator(), HelpLog, HelpAbout]);

        Items.AddRange([file, preset, tools, view, help]);
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
