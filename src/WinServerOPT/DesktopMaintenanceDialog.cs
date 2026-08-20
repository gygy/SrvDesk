namespace WinOpt;

internal sealed class DesktopMaintenanceDialog : Form
{
    public DesktopMaintenanceDialog()
    {
        Text = "桌面维护";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 380);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.SurfaceCard;

        var tip = new Label
        {
            Text = "一键执行系统维护操作。部分资源管理器相关优化应用后若未生效，可点「重启资源管理器」。",
            Location = new Point(16, 12),
            Size = new Size(488, 36),
            ForeColor = AppTheme.TextMute,
        };

        var grid = new TableLayoutPanel
        {
            Location = new Point(16, 56),
            Size = new Size(488, 260),
            ColumnCount = 2,
            RowCount = 5,
            BackColor = AppTheme.SurfaceCard,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var i = 0; i < 5; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        AddBtn(grid, 0, 0, "重启资源管理器", () => { DesktopQuickActions.RestartExplorer(); Close(); });
        AddBtn(grid, 1, 0, "刷新图标缓存", () => DesktopQuickActions.RefreshIconCache(this));
        AddBtn(grid, 0, 1, "清空回收站", () => DesktopQuickActions.EmptyRecycleBin(this));
        AddBtn(grid, 1, 1, "性能选项", () => DesktopQuickActions.OpenPerformanceOptions(this));
        AddBtn(grid, 0, 2, "桌面图标设置", () => DesktopQuickActions.OpenDesktopIconSettings(this));
        AddBtn(grid, 1, 2, "控制面板", () => DesktopQuickActions.OpenControlPanel(this));
        AddBtn(grid, 0, 3, "磁盘管理", () => DesktopQuickActions.OpenDiskManagement(this));
        AddBtn(grid, 1, 3, "设备管理器", () => DesktopQuickActions.OpenDeviceManager(this));

        var close = ActionButton("关闭", () => Close(), true);
        close.DialogResult = DialogResult.Cancel;
        close.Location = new Point(424, 332);
        CancelButton = close;

        Controls.AddRange([tip, grid, close]);
    }

    private static void AddBtn(TableLayoutPanel grid, int col, int row, string text, Action click)
    {
        var b = ActionButton(text, click, false);
        b.Dock = DockStyle.Fill;
        b.Margin = new Padding(4);
        b.Height = 44;
        grid.Controls.Add(b, col, row);
    }

    private static Button ActionButton(string text, Action click, bool primary)
    {
        var b = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = primary ? AppTheme.Primary : AppTheme.SurfaceCard,
            ForeColor = primary ? AppTheme.TextOnPrimary : AppTheme.TextMain,
            Font = new Font("Microsoft YaHei UI", 9F),
        };
        if (primary) b.FlatAppearance.BorderSize = 0;
        else
        {
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.MouseEnter += (_, _) => b.BackColor = AppTheme.PrimaryPale;
            b.MouseLeave += (_, _) => b.BackColor = AppTheme.SurfaceCard;
        }
        b.Click += (_, _) => click();
        return b;
    }
}
