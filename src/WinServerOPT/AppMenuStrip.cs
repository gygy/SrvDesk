namespace WinOpt;

/// <summary>应用顶栏菜单：文件 / 预设 / 工具 / 视图 / 帮助。</summary>
internal sealed class AppMenuStrip : MenuStrip
{
    public ToolStripMenuItem FileImport { get; }
    public ToolStripMenuItem FileExport { get; }
    public ToolStripMenuItem PresetLoad { get; }
    public ToolStripMenuItem ToolAutologon { get; }
    public ToolStripMenuItem ToolIdentity { get; }
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
        ToolQuick = new ToolStripMenuItem("快速工具...");
        ToolRefresh = new ToolStripMenuItem("刷新当前状态", null, null, Keys.F5);
        tools.DropDownItems.AddRange([ToolAutologon, ToolIdentity, new ToolStripSeparator(), ToolQuick, ToolRefresh]);

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
