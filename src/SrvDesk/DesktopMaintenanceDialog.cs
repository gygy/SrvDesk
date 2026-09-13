namespace SrvDesk;

internal sealed class DesktopMaintenanceDialog : Form
{
    public DesktopMaintenanceDialog()
    {
        Text = AppLang.L("桌面维护", "Desktop maintenance");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(720, 520);
        MinimumSize = UiScale.Size(620, 460);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var rowH = Math.Max(UiScale.S(48), UiFit.ControlHeight() + UiScale.S(16));
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 0, 0, UiScale.S(8)),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var i = 0; i < 4; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));

        AddBtn(grid, 0, 0, AppLang.L("重启资源管理器", "Restart Explorer"), () => { DesktopQuickActions.RestartExplorer(); Close(); });
        AddBtn(grid, 1, 0, AppLang.L("刷新图标缓存", "Refresh icon cache"), () => DesktopQuickActions.RefreshIconCache(this));
        AddBtn(grid, 0, 1, AppLang.L("清空回收站", "Empty Recycle Bin"), () => DesktopQuickActions.EmptyRecycleBin(this));
        AddBtn(grid, 1, 1, AppLang.L("性能选项", "Performance options"), () => DesktopQuickActions.OpenPerformanceOptions(this));
        AddBtn(grid, 0, 2, AppLang.L("桌面图标设置", "Desktop icon settings"), () => DesktopQuickActions.OpenDesktopIconSettings(this));
        AddBtn(grid, 1, 2, AppLang.L("控制面板", "Control Panel"), () => DesktopQuickActions.OpenControlPanel(this));
        AddBtn(grid, 0, 3, AppLang.L("磁盘管理", "Disk Management"), () => DesktopQuickActions.OpenDiskManagement(this));
        AddBtn(grid, 1, 3, AppLang.L("设备管理器", "Device Manager"), () => DesktopQuickActions.OpenDeviceManager(this));

        body.Controls.Add(grid);

        ThemedSettingsChrome.MountModal(
            this,
            AppLang.L("桌面维护", "Desktop maintenance"),
            "",
            body,
            "",
            showHeader: false);
    }

    private static void AddBtn(TableLayoutPanel grid, int col, int row, string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        UiFit.FitButton(b, padding: 28);
        b.Dock = DockStyle.Fill;
        b.Margin = new Padding(UiScale.S(4));
        b.Click += (_, _) => click();
        grid.Controls.Add(b, col, row);
    }
}
