namespace SrvDesk;

internal sealed class CleanupDialog : Form
{
    private readonly CheckBox _temp = Mk("用户与系统临时文件（%TEMP%、Windows\\Temp）", true);
    private readonly CheckBox _recent = Mk("最近打开的文件快捷方式", true);
    private readonly CheckBox _recycle = Mk("回收站", false);
    private readonly CheckBox _prefetch = Mk("预取文件（Prefetch）", false);
    private readonly CheckBox _thumb = Mk("缩略图缓存", false);
    private readonly Label _result = new();

    public CleanupDialog()
    {
        Text = "垃圾清理";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(540, 400);

        var body = ThemedSettingsChrome.CreateBodyPanel();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(4, 4, 4, 4),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var opts = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8),
        };
        opts.Controls.AddRange([_temp, _recent, _recycle, _prefetch, _thumb]);

        var run = ThemedSettingsChrome.CreateButton("开始清理", true);
        run.Margin = new Padding(0, 0, 8, 0);
        run.Click += (_, _) => RunCleanup();

        var repair = ThemedSettingsChrome.CreateButton("修复被锁系统组件", false);
        repair.Margin = new Padding(0);
        repair.Click += (_, _) =>
        {
            CompetitorTweaks.RepairLockedComponents();
            MessageBox.Show(this, "已尝试恢复任务管理器、CMD、注册表编辑器、控制面板等。", "策略修复",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8),
        };
        row.Controls.Add(run);
        row.Controls.Add(repair);

        _result.Dock = DockStyle.Fill;
        _result.AutoSize = false;
        _result.ForeColor = AppTheme.TextMute;
        _result.TextAlign = ContentAlignment.TopLeft;

        layout.Controls.Add(opts, 0, 0);
        layout.Controls.Add(row, 0, 1);
        layout.Controls.Add(_result, 0, 2);
        body.Controls.Add(layout);

        ThemedSettingsChrome.MountModal(
            this,
            "垃圾清理",
            "临时文件 · 缩略图 · 预取 · 不清理浏览器密码",
            body,
            "清理不可恢复，请先确认勾选项。");
    }

    private static CheckBox Mk(string text, bool on) => new()
    {
        Text = text,
        AutoSize = true,
        Checked = on,
        ForeColor = AppTheme.TextMain,
        Margin = new Padding(0, 4, 0, 4),
        MaximumSize = new Size(480, 0),
    };

    private void RunCleanup()
    {
        if (!_temp.Checked && !_recent.Checked && !_recycle.Checked && !_prefetch.Checked && !_thumb.Checked)
        {
            MessageBox.Show(this, "请先勾选要清理的项目。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var files = 0;
        var bytes = 0L;
        try
        {
            if (_temp.Checked)
            {
                Sweep(Path.GetTempPath(), ref files, ref bytes);
                Sweep(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), ref files, ref bytes);
            }
            if (_recent.Checked)
                Sweep(Environment.GetFolderPath(Environment.SpecialFolder.Recent), ref files, ref bytes);
            if (_prefetch.Checked)
                Sweep(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"), ref files, ref bytes);
            if (_thumb.Checked)
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Sweep(Path.Combine(local, @"Microsoft\Windows\Explorer"), ref files, ref bytes, "thumbcache_*.db");
            }
            if (_recycle.Checked)
                DesktopQuickActions.EmptyRecycleBin(this);

            ApplyLog.Write($"垃圾清理：{files} 个文件，约 {bytes / 1024} KB");
            _result.Text = $"已处理约 {files} 个文件（{bytes / 1024} KB）。部分占用中的文件会跳过。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "垃圾清理", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void Sweep(string dir, ref int files, ref long bytes, string pattern = "*")
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly))
        {
            try
            {
                var info = new FileInfo(file);
                var size = info.Length;
                info.Delete();
                files++;
                bytes += size;
            }
            catch { /* in use */ }
        }
    }
}
