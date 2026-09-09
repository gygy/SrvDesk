using System.Diagnostics;

namespace SrvDesk;

/// <summary>程序设置：界面偏好与调试日志（固定对话框，无滚动条）。</summary>
internal sealed class AppSettingsDialog : Form
{
    private readonly CheckBox _showScript = new() { AutoSize = true };
    private readonly ComboBox _dock = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _hideIncompatible = new() { AutoSize = true };
    private readonly CheckBox _checkUpdate = new() { AutoSize = true };
    private readonly CheckBox _restorePoint = new()
    {
        AutoSize = true,
        MaximumSize = new Size(480, 0),
    };
    private readonly CheckBox _debugLog = new() { AutoSize = true };
    private readonly CheckBox _softSkip = new()
    {
        AutoSize = true,
        MaximumSize = new Size(480, 0),
    };
    private readonly Label _hint = new()
    {
        AutoSize = true,
        MaximumSize = new Size(480, 0),
        ForeColor = AppTheme.TextMute,
    };
    private readonly Label _langHint = new()
    {
        AutoSize = true,
        MaximumSize = new Size(480, 0),
        ForeColor = AppTheme.TextMute,
    };

    private string _loadedLanguage = "auto";

    public AppSettingsDialog()
    {
        Text = AppLang.L("程序设置", "Settings");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        _showScript.Text = AppLang.L("启动时显示配置脚本面板", "Show config script panel at startup");
        _hideIncompatible.Text = AppLang.L("启动时默认「隐藏不适用项」", "Hide incompatible items by default");
        _checkUpdate.Text = AppLang.L("启动时检查程序更新", "Check for updates at startup");
        _restorePoint.Text = AppLang.L("应用到系统前询问是否创建还原点", "Ask to create a restore point before applying");
        _debugLog.Text = AppLang.L("开启调试日志（debug.log）", "Enable debug log (debug.log)");
        _softSkip.Text = AppLang.L("环境不支持时记为「跳过」而非「失败」", "Treat unsupported environments as skip, not failure");
        _hint.Text = AppLang.L(
            "开启后记录：字段差分、每个优化项跳过/写入、注册表与服务细节。" +
            "日志目录：%LocalAppData%\\SrvDesk\\debug.log",
            "When enabled, logs field diffs, per-item skip/write, registry and service details. " +
            "Log folder: %LocalAppData%\\SrvDesk\\debug.log");
        _langHint.Text = AppLang.L(
            "更改语言后将重启程序以应用。",
            "The app will restart to apply a language change.");

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

        body.Controls.Add(Section(AppLang.L("界面", "Interface")));
        body.Controls.Add(Pad(LanguageRow()));
        body.Controls.Add(Pad(_langHint));
        body.Controls.Add(Pad(_showScript));
        body.Controls.Add(Pad(DockRow()));
        body.Controls.Add(Pad(_hideIncompatible));
        body.Controls.Add(Pad(_checkUpdate));
        body.Controls.Add(Pad(_restorePoint));
        body.Controls.Add(Section(AppLang.L("诊断", "Diagnostics")));
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
        UiBuffer.ConfigureNoScrollRow(bottom);
        var cancel = new Button
        {
            Text = AppLang.L("取消", "Cancel"),
            DialogResult = DialogResult.Cancel,
            Width = 88,
            Height = 32,
        };
        var ok = new Button
        {
            Text = AppLang.L("保存", "Save"),
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
            var restart = Save();
            DialogResult = DialogResult.OK;
            Close();
            if (restart)
            {
                try { Application.Restart(); }
                catch { /* ignore */ }
                Application.Exit();
            }
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

    private Control LanguageRow()
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0),
        };
        UiBuffer.ConfigureNoScrollRow(row);
        row.Controls.Add(new Label
        {
            Text = AppLang.L("界面语言", "Language"),
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0),
        });
        _language.Items.AddRange([
            AppLang.ModeDisplayName(AppLanguageMode.Auto),
            AppLang.ModeDisplayName(AppLanguageMode.ZhHans),
            AppLang.ModeDisplayName(AppLanguageMode.En),
        ]);
        _language.Width = 200;
        row.Controls.Add(_language);
        return row;
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
        UiBuffer.ConfigureNoScrollRow(row);
        row.Controls.Add(new Label
        {
            Text = AppLang.L("配置脚本默认停靠", "Config script dock"),
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0),
        });
        _dock.Items.AddRange([
            AppLang.L("右侧", "Right"),
            AppLang.L("底部", "Bottom"),
        ]);
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
        logBtns.Controls.Add(LinkBtn(AppLang.L("打开日志目录", "Open log folder"), () => OpenPath(ApplyLog.LogDirectory)));
        logBtns.Controls.Add(LinkBtn(AppLang.L("操作日志", "Operation log"), () => OpenPath(ApplyLog.LogFilePath)));
        logBtns.Controls.Add(LinkBtn(AppLang.L("变更日志", "Change log"), () => OpenPath(ApplyLog.ChangeLogFilePath)));
        logBtns.Controls.Add(LinkBtn(AppLang.L("调试日志", "Debug log"), () => OpenPath(ApplyLog.DebugLogFilePath)));
        return logBtns;
    }

    private void LoadPrefs()
    {
        var p = UiPrefs.Load();
        _showScript.Checked = p.ShowHelpPanel;
        _dock.SelectedIndex = UiPrefs.GetDock(p) == ConfigScriptDock.Bottom ? 1 : 0;
        _hideIncompatible.Checked = p.HideIncompatibleByDefault;
        _checkUpdate.Checked = !p.DisableStartupUpdateCheck;
        _restorePoint.Checked = !p.DisableRestorePointPrompt;
        _debugLog.Checked = p.EnableDebugLog;
        _softSkip.Checked = p.SoftSkipUnsupported;
        _loadedLanguage = AppLang.ToPrefsValue(AppLang.ParseMode(p.Language));
        _language.SelectedIndex = AppLang.ParseMode(_loadedLanguage) switch
        {
            AppLanguageMode.ZhHans => 1,
            AppLanguageMode.En => 2,
            _ => 0,
        };
    }

    /// <returns>是否需要重启以应用语言。</returns>
    private bool Save()
    {
        var p = UiPrefs.Load();
        p.ShowHelpPanel = _showScript.Checked;
        p.HelpPanelDock = _dock.SelectedIndex == 1
            ? (int)ConfigScriptDock.Bottom
            : (int)ConfigScriptDock.Right;
        p.HideIncompatibleByDefault = _hideIncompatible.Checked;
        p.DisableStartupUpdateCheck = !_checkUpdate.Checked;
        p.DisableRestorePointPrompt = !_restorePoint.Checked;
        p.EnableDebugLog = _debugLog.Checked;
        p.SoftSkipUnsupported = _softSkip.Checked;
        var newLang = _language.SelectedIndex switch
        {
            1 => "zh-Hans",
            2 => "en",
            _ => "auto",
        };
        p.Language = newLang;
        UiPrefs.Save(p);
        ApplyLog.Write(AppLang.L("程序设置已保存", "Settings saved") +
            (p.EnableDebugLog
                ? AppLang.L("（调试日志已开启）", " (debug log on)")
                : AppLang.L("（调试日志关闭）", " (debug log off)")));
        if (p.EnableDebugLog)
            ApplyLog.Debug("调试日志已启用");
        return !string.Equals(_loadedLanguage, newLang, StringComparison.OrdinalIgnoreCase);
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
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            ForeColor = AppTheme.Primary,
            BackColor = AppTheme.SurfaceCard,
            Margin = new Padding(0, 0, 8, 4),
            Size = UiFit.ButtonSize(text, UiFit.ControlHeight(), minWidth: 64, padding: 18),
        };
        b.FlatAppearance.BorderColor = AppTheme.Border;
        UiFit.EnableCenteredFlatText(b);
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
            MessageBox.Show(ex.Message, AppLang.L("打开失败", "Failed to open"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
