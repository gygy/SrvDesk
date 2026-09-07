using System.Diagnostics;

namespace SrvDesk;

/// <summary>程序设置：界面偏好与调试日志（固定对话框，无滚动条）。</summary>
internal sealed class AppSettingsDialog : Form
{
    private readonly CheckBox _showScript = new() { AutoSize = true, Text = "启动时显示配置脚本面板" };
    private readonly ComboBox _dock = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _hideIncompatible = new() { AutoSize = true, Text = "启动时默认「隐藏不适用项」" };
    private readonly CheckBox _debugLog = new() { AutoSize = true, Text = "开启调试日志（debug.log）" };
    private readonly CheckBox _softSkip = new()
    {
        AutoSize = true,
        MaximumSize = new Size(480, 0),
        Text = "环境不支持时记为「跳过」而非「失败」",
    };
    private readonly Label _hint = new()
    {
        AutoSize = true,
        MaximumSize = new Size(480, 0),
        ForeColor = AppTheme.TextMute,
        Text = "开启后记录：字段差分、每个优化项跳过/写入、注册表与服务细节。" +
               "日志目录：%LocalAppData%\\SrvDesk\\debug.log",
    };

    public AppSettingsDialog()
    {
        Text = "程序设置";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16, 12, 16, 12),
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var body = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
        };

        body.Controls.Add(Section("界面"));
        body.Controls.Add(Pad(_showScript));
        body.Controls.Add(Pad(DockRow()));
        body.Controls.Add(Pad(_hideIncompatible));
        body.Controls.Add(Section("诊断"));
        body.Controls.Add(Pad(_debugLog));
        body.Controls.Add(Pad(_softSkip));
        body.Controls.Add(Pad(_hint));
        body.Controls.Add(Pad(LogButtons()));

        var bottom = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoScroll = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0),
            Padding = new Padding(0, 4, 0, 0),
        };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 88, Height = 32 };
        var ok = new Button
        {
            Text = "保存",
            Width = 88,
            Height = 32,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        ok.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) =>
        {
            Save();
            DialogResult = DialogResult.OK;
            Close();
        };
        bottom.Controls.Add(cancel);
        bottom.Controls.Add(ok);

        root.Controls.Add(body, 0, 0);
        root.Controls.Add(bottom, 0, 1);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;

        LoadPrefs();
    }

    private Control DockRow()
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0),
        };
        row.Controls.Add(new Label
        {
            Text = "配置脚本默认停靠",
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0),
        });
        _dock.Items.AddRange(["右侧", "底部"]);
        _dock.Width = 120;
        row.Controls.Add(_dock);
        return row;
    }

    private Control LogButtons()
    {
        var logBtns = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            AutoScroll = false,
            MaximumSize = new Size(480, 0),
            Margin = new Padding(0, 4, 0, 0),
        };
        logBtns.Controls.Add(LinkBtn("打开日志目录", () => OpenPath(ApplyLog.LogDirectory)));
        logBtns.Controls.Add(LinkBtn("操作日志", () => OpenPath(ApplyLog.LogFilePath)));
        logBtns.Controls.Add(LinkBtn("变更日志", () => OpenPath(ApplyLog.ChangeLogFilePath)));
        logBtns.Controls.Add(LinkBtn("调试日志", () => OpenPath(ApplyLog.DebugLogFilePath)));
        return logBtns;
    }

    private void LoadPrefs()
    {
        var p = UiPrefs.Load();
        _showScript.Checked = p.ShowHelpPanel;
        _dock.SelectedIndex = UiPrefs.GetDock(p) == ConfigScriptDock.Bottom ? 1 : 0;
        _hideIncompatible.Checked = p.HideIncompatibleByDefault;
        _debugLog.Checked = p.EnableDebugLog;
        _softSkip.Checked = p.SoftSkipUnsupported;
    }

    private void Save()
    {
        var p = UiPrefs.Load();
        p.ShowHelpPanel = _showScript.Checked;
        p.HelpPanelDock = _dock.SelectedIndex == 1
            ? (int)ConfigScriptDock.Bottom
            : (int)ConfigScriptDock.Right;
        p.HideIncompatibleByDefault = _hideIncompatible.Checked;
        p.EnableDebugLog = _debugLog.Checked;
        p.SoftSkipUnsupported = _softSkip.Checked;
        UiPrefs.Save(p);
        ApplyLog.Write("程序设置已保存" +
            (p.EnableDebugLog ? "（调试日志已开启）" : "（调试日志关闭）"));
        if (p.EnableDebugLog)
            ApplyLog.Debug("调试日志已启用");
    }

    private static Label Section(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
        ForeColor = AppTheme.TextMain,
        Margin = new Padding(0, 8, 0, 6),
    };

    private static Control Pad(Control c)
    {
        c.Margin = new Padding(0, 0, 0, 8);
        return c;
    }

    private static Button LinkBtn(string text, Action click)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            ForeColor = AppTheme.Primary,
            BackColor = AppTheme.SurfaceCard,
            Margin = new Padding(0, 0, 8, 4),
            Height = 28,
        };
        b.FlatAppearance.BorderColor = AppTheme.Border;
        b.Click += (_, _) => click();
        return b;
    }

    private static void OpenPath(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                return;
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(path))
                File.WriteAllText(path, "", new System.Text.UTF8Encoding(true));
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "打开失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
