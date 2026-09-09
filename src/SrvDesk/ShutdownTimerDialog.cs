namespace SrvDesk;

/// <summary>
/// 定时关机对话框：布局对齐 Shutdown Agent（配置区 + In/At + 强制/闪烁 + 启用按钮 + 底栏状态），
/// 视觉风格使用 SrvDesk Theme。
/// </summary>
internal sealed class ShutdownTimerDialog : Form
{
    private static ShutdownTimerDialog? _instance;

    private readonly ComboBox _action = new();
    private readonly RadioButton _optIn = new();
    private readonly RadioButton _optAt = new();
    private readonly NumericUpDown _period = new();
    private readonly ComboBox _unit = new();
    private readonly NumericUpDown _hour = new();
    private readonly NumericUpDown _min = new();
    private readonly ComboBox _ampm = new();
    private readonly CheckBox _force = new();
    private readonly CheckBox _blink = new();
    private readonly NumericUpDown _blinkSec = new();
    private readonly Label _blinkUnit = new();
    private readonly Button _toggle;
    private readonly Label _status = new();
    private readonly GroupBox _frame;
    private readonly Panel _configBody;
    private bool _syncing;

    public static void ShowOrActivate(IWin32Window? owner)
    {
        if (owner is Control { IsDisposed: true })
            owner = null;
        if (owner is null)
        {
            foreach (Form f in Application.OpenForms)
            {
                if (f is MainForm) { owner = f; break; }
            }
        }

        if (_instance is { IsDisposed: false })
        {
            if (!_instance.Visible) _instance.Show(owner);
            if (_instance.WindowState == FormWindowState.Minimized)
                _instance.WindowState = FormWindowState.Normal;
            _instance.Activate();
            return;
        }

        var dlg = new ShutdownTimerDialog();
        _instance = dlg;
        dlg.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_instance, dlg)) _instance = null;
        };
        dlg.Show(owner);
    }

    public ShutdownTimerDialog()
    {
        Text = AppLang.L("定时关机", "Shutdown timer");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = true;
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;
        AutoScaleMode = AutoScaleMode.None;

        var pad = UiScale.S(12);
        var rowH = UiFit.ControlHeight();
        var clientW = UiScale.S(340);
        ClientSize = new Size(clientW, UiScale.S(390));

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.Padding = new Padding(pad, pad, pad, pad);
        body.AutoScroll = false;

        _frame = new GroupBox
        {
            Text = AppLang.L(" 配置 ", " Configuration "),
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFontBold(9.5F),
            Padding = new Padding(UiScale.S(10), UiScale.S(8), UiScale.S(10), UiScale.S(8)),
        };

        _configBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
        };

        // —— Action ——
        var lblAction = Lbl(AppLang.L("操作", "Action"));
        _action.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (ShutdownPowerAction a in Enum.GetValues(typeof(ShutdownPowerAction)))
            _action.Items.Add(ShutdownPowerActions.DisplayName(a));
        _action.SelectedIndex = 0;
        UiFit.FitCombo(_action);

        // —— In / At ——
        _optIn.Text = AppLang.L("之后", "In");
        _optIn.AutoSize = true;
        _optIn.Checked = true;
        _optIn.ForeColor = AppTheme.TextMain;
        _optAt.Text = AppLang.L("定时", "At");
        _optAt.AutoSize = true;
        _optAt.ForeColor = AppTheme.TextMain;

        _period.Minimum = 1;
        _period.Maximum = 999;
        _period.Value = 1;
        _period.Width = UiScale.S(56);

        _unit.DropDownStyle = ComboBoxStyle.DropDownList;
        _unit.Items.AddRange([
            AppLang.L("小时", "Hours"),
            AppLang.L("分钟", "Minutes"),
            AppLang.L("秒", "Seconds"),
        ]);
        _unit.SelectedIndex = 0;
        UiFit.FitCombo(_unit);
        _unit.Width = Math.Max(_unit.Width, UiScale.S(100));

        _hour.Minimum = 1;
        _hour.Maximum = 12;
        _hour.Value = ((DateTime.Now.Hour + 11) % 12) + 1;
        _hour.Width = UiScale.S(48);
        _hour.Enabled = false;

        _min.Minimum = 0;
        _min.Maximum = 59;
        _min.Value = DateTime.Now.Minute;
        _min.Width = UiScale.S(48);
        _min.Enabled = false;

        _ampm.DropDownStyle = ComboBoxStyle.DropDownList;
        _ampm.Items.AddRange(["AM", "PM"]);
        _ampm.SelectedIndex = DateTime.Now.Hour >= 12 ? 1 : 0;
        _ampm.Enabled = false;
        UiFit.FitCombo(_ampm);
        _ampm.Width = Math.Max(_ampm.Width, UiScale.S(56));

        _optIn.CheckedChanged += (_, _) => SyncMode();
        _optAt.CheckedChanged += (_, _) => SyncMode();

        // —— Force / Blink ——
        _force.Text = AppLang.L("强制结束进程", "Force processes to terminate");
        _force.AutoSize = true;
        _force.Checked = true;
        _force.ForeColor = AppTheme.TextMain;

        _blink.Text = AppLang.L("执行前托盘图标闪烁，提前", "Icon blink before execution by");
        _blink.AutoSize = true;
        _blink.Checked = true;
        _blink.ForeColor = AppTheme.TextMain;
        _blink.CheckedChanged += (_, _) =>
        {
            _blinkSec.Enabled = _blink.Checked && !_syncing && !ShutdownTimerService.IsArmed;
        };

        _blinkSec.Minimum = 1;
        _blinkSec.Maximum = 999;
        _blinkSec.Value = 30;
        _blinkSec.Width = UiScale.S(56);

        _blinkUnit.Text = AppLang.L("秒", "Seconds");
        _blinkUnit.AutoSize = true;
        _blinkUnit.ForeColor = AppTheme.TextMute;
        _blinkUnit.Padding = new Padding(0, UiScale.S(6), 0, 0);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 6,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(UiScale.S(4), UiScale.S(6), UiScale.S(4), UiScale.S(4)),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiScale.S(56)));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiScale.S(64)));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiScale.S(56)));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH + UiScale.S(6)));

        // row0 action
        grid.Controls.Add(lblAction, 0, 0);
        grid.SetColumnSpan(_action, 3);
        _action.Dock = DockStyle.Fill;
        grid.Controls.Add(_action, 1, 0);

        // row1 In
        _optIn.Dock = DockStyle.Left;
        grid.Controls.Add(_optIn, 0, 1);
        _period.Dock = DockStyle.Left;
        grid.Controls.Add(_period, 1, 1);
        _unit.Dock = DockStyle.Fill;
        grid.SetColumnSpan(_unit, 2);
        grid.Controls.Add(_unit, 2, 1);

        // row2 At
        _optAt.Dock = DockStyle.Left;
        grid.Controls.Add(_optAt, 0, 2);
        _hour.Dock = DockStyle.Left;
        grid.Controls.Add(_hour, 1, 2);
        var atRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        _min.Margin = new Padding(0, 0, UiScale.S(6), 0);
        _ampm.Margin = Padding.Empty;
        atRight.Controls.Add(_min);
        atRight.Controls.Add(_ampm);
        grid.SetColumnSpan(atRight, 2);
        grid.Controls.Add(atRight, 2, 2);

        // row3 force
        grid.SetColumnSpan(_force, 4);
        grid.Controls.Add(_force, 0, 3);

        // row4 blink label
        grid.SetColumnSpan(_blink, 4);
        grid.Controls.Add(_blink, 0, 4);

        // row5 blink value
        var blinkRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(UiScale.S(4), 0, 0, 0),
        };
        blinkRow.Controls.Add(_blinkSec);
        blinkRow.Controls.Add(_blinkUnit);
        grid.SetColumnSpan(blinkRow, 4);
        grid.Controls.Add(blinkRow, 0, 5);

        _configBody.Controls.Add(grid);
        _frame.Controls.Add(_configBody);

        _toggle = ThemedSettingsChrome.CreateButton(AppLang.L("启用定时", "Enable Timer"), true);
        _toggle.Dock = DockStyle.Bottom;
        _toggle.Height = rowH + UiScale.S(4);
        _toggle.Margin = new Padding(0, UiScale.S(10), 0, UiScale.S(6));
        _toggle.Click += (_, _) => ToggleTimer();

        var toggleHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = _toggle.Height + UiScale.S(12),
            Padding = new Padding(0, UiScale.S(8), 0, 0),
            BackColor = Color.Transparent,
        };
        _toggle.Dock = DockStyle.Fill;
        toggleHost.Controls.Add(_toggle);

        _status.Dock = DockStyle.Bottom;
        _status.Height = UiScale.S(24);
        _status.TextAlign = ContentAlignment.MiddleCenter;
        _status.BackColor = AppTheme.PrimaryDeep;
        _status.ForeColor = AppTheme.TextOnPrimary;
        _status.Font = UiFit.UiFontSmall;
        _status.Text = AppLang.L("定时未启用", "Timer Disabled");

        // Dock order: bottom first
        body.Controls.Add(_frame);
        body.Controls.Add(toggleHost);
        body.Controls.Add(_status);

        Controls.Add(body);

        ShutdownTimerService.Changed += OnServiceChanged;
        FormClosed += (_, _) => ShutdownTimerService.Changed -= OnServiceChanged;
        Load += (_, _) => RefreshFromService();
        FormClosing += OnFormClosing;
    }

    private static Label Lbl(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = AppTheme.TextHeader,
        Anchor = AnchorStyles.Left,
        Padding = new Padding(0, UiScale.S(6), 0, 0),
    };

    private void SyncMode()
    {
        var at = _optAt.Checked;
        _period.Enabled = !at && !ShutdownTimerService.IsArmed;
        _unit.Enabled = !at && !ShutdownTimerService.IsArmed;
        _hour.Enabled = at && !ShutdownTimerService.IsArmed;
        _min.Enabled = at && !ShutdownTimerService.IsArmed;
        _ampm.Enabled = at && !ShutdownTimerService.IsArmed;
    }

    private void SetConfigEnabled(bool enabled)
    {
        _configBody.Enabled = enabled;
        _action.Enabled = enabled;
        _optIn.Enabled = enabled;
        _optAt.Enabled = enabled;
        _force.Enabled = enabled;
        _blink.Enabled = enabled;
        _blinkSec.Enabled = enabled && _blink.Checked;
        if (enabled) SyncMode();
        else
        {
            _period.Enabled = false;
            _unit.Enabled = false;
            _hour.Enabled = false;
            _min.Enabled = false;
            _ampm.Enabled = false;
        }
    }

    private void ToggleTimer()
    {
        if (ShutdownTimerService.IsArmed)
        {
            ShutdownTimerService.Disarm();
            return;
        }

        try
        {
            var action = (ShutdownPowerAction)_action.SelectedIndex;
            var when = ResolveExecutionTime();
            if (when <= DateTime.Now.AddSeconds(1))
            {
                MessageBox.Show(this,
                    AppLang.L("执行时间必须晚于当前时间。", "Execution time must be in the future."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ShutdownTimerService.Arm(
                this,
                action,
                when,
                _force.Checked,
                _blink.Checked,
                (int)_blinkSec.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private DateTime ResolveExecutionTime()
    {
        if (_optIn.Checked)
        {
            var n = (double)_period.Value;
            return _unit.SelectedIndex switch
            {
                0 => DateTime.Now.AddHours(n),
                1 => DateTime.Now.AddMinutes(n),
                _ => DateTime.Now.AddSeconds(n),
            };
        }

        var hour12 = (int)_hour.Value;
        var minute = (int)_min.Value;
        var pm = _ampm.SelectedIndex == 1;
        var hour24 = hour12 % 12;
        if (pm) hour24 += 12;
        var at = DateTime.Today.AddHours(hour24).AddMinutes(minute);
        if (at <= DateTime.Now) at = at.AddDays(1);
        return at;
    }

    private void OnServiceChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(OnServiceChanged);
            return;
        }
        RefreshFromService();
    }

    private void RefreshFromService()
    {
        _syncing = true;
        try
        {
            if (ShutdownTimerService.IsArmed)
            {
                SetConfigEnabled(false);
                _toggle.Text = AppLang.L("取消定时", "Disable Timer");
                _toggle.BackColor = AppTheme.SurfaceCard;
                _toggle.ForeColor = AppTheme.TextMain;
                _toggle.FlatAppearance.BorderSize = 1;
                _toggle.FlatAppearance.BorderColor = AppTheme.Border;
                var remain = ShutdownTimerService.RemainingSeconds();
                _status.Text = AppLang.L($"{remain} 秒后执行", $"{remain} Seconds to Execution");
                if (ShutdownTimerService.BlinkEnabled && remain <= ShutdownTimerService.BlinkSeconds)
                {
                    // 接近执行时状态栏闪烁
                    var flash = (Environment.TickCount / 500) % 2 == 0;
                    _status.BackColor = flash ? AppTheme.Primary : AppTheme.PrimaryDeep;
                }
                else
                {
                    _status.BackColor = AppTheme.PrimaryDeep;
                }
            }
            else
            {
                SetConfigEnabled(true);
                _toggle.Text = AppLang.L("启用定时", "Enable Timer");
                _toggle.BackColor = AppTheme.Primary;
                _toggle.ForeColor = AppTheme.TextOnPrimary;
                _toggle.FlatAppearance.BorderSize = 0;
                _status.BackColor = AppTheme.PrimaryDeep;
                _status.Text = AppLang.L("定时未启用", "Timer Disabled");
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!ShutdownTimerService.IsArmed) return;
        if (e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
            return;

        var r = MessageBox.Show(this,
            AppLang.L(
                "定时仍在运行。\r\n是：取消定时并关闭\r\n否：后台继续运行（托盘可打开）\r\n取消：返回",
                "Timer is still running.\r\nYes: cancel and close\r\nNo: keep running in tray\r\nCancel: go back"),
            Text,
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (r == DialogResult.Cancel)
        {
            e.Cancel = true;
            return;
        }
        if (r == DialogResult.Yes)
            ShutdownTimerService.Disarm();
        // No → keep armed, just close dialog
    }
}
