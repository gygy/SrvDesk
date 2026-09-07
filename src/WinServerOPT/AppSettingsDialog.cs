using System.Diagnostics;

namespace WinOpt;

/// <summary>程序设置：界面偏好与调试日志。</summary>
internal sealed class AppSettingsDialog : Form
{
    private readonly CheckBox _showScript = new() { AutoSize = true, Text = "启动时显示配置脚本面板" };
    private readonly ComboBox _dock = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _hideIncompatible = new() { AutoSize = true, Text = "启动时默认「隐藏不适用项」" };
    private readonly CheckBox _debugLog = new() { AutoSize = true, Text = "开启调试日志（debug.log）" };
    private readonly CheckBox _softSkip = new()
    {
        AutoSize = true,
        Text = "环境不支持时记为「跳过」而非「失败」（休眠/防火墙组名/受保护服务等）",
    };
    private readonly Label _hint = new()
    {
        AutoSize = true,
        MaximumSize = new Size(520, 0),
        ForeColor = AppTheme.TextMute,
        Text = "调试日志会记录执行的命令与软跳过原因，便于对照操作日志排查。" +
               "日志目录：%LocalAppData%\\WinOpt\\",
    };

    public AppSettingsDialog()
    {
        Text = "程序设置";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 420);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
        };

        body.Controls.Add(Section("界面"));
        body.Controls.Add(Pad(_showScript));
        body.Controls.Add(Pad(DockRow()));
        body.Controls.Add(Pad(_hideIncompatible));
        body.Controls.Add(Section("诊断"));
        body.Controls.Add(Pad(_debugLog));
        body.Controls.Add(Pad(_softSkip));
        body.Controls.Add(Pad(_hint));

        var logBtns = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 12, 0, 0),
        };
        logBtns.Controls.Add(LinkBtn("打开日志目录", () => OpenPath(ApplyLog.LogDirectory)));
        logBtns.Controls.Add(LinkBtn("操作日志", () => OpenPath(ApplyLog.LogFilePath)));
        logBtns.Controls.Add(LinkBtn("变更日志", () => OpenPath(ApplyLog.ChangeLogFilePath)));
        logBtns.Controls.Add(LinkBtn("调试日志", () => OpenPath(ApplyLog.DebugLogFilePath)));
        body.Controls.Add(Pad(logBtns));

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
        };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 88, Height = 32 };
        var ok = new Button
        {
            Text = "保存",
            Width = 88,
            Height = 32,
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
        root.Controls.Add(bottom, 0, 2);
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
            WrapContents = false,
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
            Margin = new Padding(0, 0, 8, 0),
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
