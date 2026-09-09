namespace SrvDesk;

/// <summary>后台定时关机服务：对话框关闭后仍可继续倒计时，托盘可闪烁提醒。</summary>
internal static class ShutdownTimerService
{
    private static readonly object Gate = new();
    private static System.Windows.Forms.Timer? _timer;
    private static NotifyIcon? _tray;
    private static Icon? _trayIcon;
    private static Icon? _blankIcon;
    private static bool _blinkShow;
    private static Form? _owner;

    public static bool IsArmed { get; private set; }
    public static ShutdownPowerAction Action { get; private set; }
    public static bool Force { get; private set; }
    public static bool BlinkEnabled { get; private set; }
    public static int BlinkSeconds { get; private set; }
    public static DateTime ExecutionTime { get; private set; }

    public static event Action? Changed;

    public static void Arm(
        Form owner,
        ShutdownPowerAction action,
        DateTime executionTime,
        bool force,
        bool blink,
        int blinkSeconds)
    {
        lock (Gate)
        {
            _owner = owner;
            Action = action;
            Force = force;
            BlinkEnabled = blink;
            BlinkSeconds = blinkSeconds < 1 ? 1 : (blinkSeconds > 999 ? 999 : blinkSeconds);
            ExecutionTime = executionTime;
            IsArmed = true;
            EnsureTimer();
            EnsureTray();
            UpdateTrayTip();
            ApplyLog.Write($"定时关机：已启用「{ShutdownPowerActions.DisplayName(action)}」于 {executionTime:yyyy-MM-dd HH:mm:ss}");
        }
        Changed?.Invoke();
    }

    public static void Disarm(string? reason = null)
    {
        lock (Gate)
        {
            if (!IsArmed && _timer is null) return;
            IsArmed = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            DisposeTray();
            if (!string.IsNullOrWhiteSpace(reason))
                ApplyLog.Write("定时关机：" + reason);
            else
                ApplyLog.Write("定时关机：已取消");
        }
        Changed?.Invoke();
    }

    public static int RemainingSeconds()
    {
        if (!IsArmed) return 0;
        var s = (int)Math.Ceiling((ExecutionTime - DateTime.Now).TotalSeconds);
        return Math.Max(0, s);
    }

    private static void EnsureTimer()
    {
        if (_timer is not null)
        {
            _timer.Start();
            return;
        }
        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
    }

    private static void OnTick()
    {
        if (!IsArmed)
        {
            Changed?.Invoke();
            return;
        }

        var remain = RemainingSeconds();
        UpdateTrayBlink(remain);
        Changed?.Invoke();

        if (DateTime.Now < ExecutionTime) return;

        var action = Action;
        var force = Force;
        Disarm("执行 " + ShutdownPowerActions.DisplayName(action));
        try
        {
            ShutdownPowerActions.Execute(action, force);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("定时关机失败：" + ex.Message);
            try
            {
                var owner = _owner is { IsDisposed: false } ? _owner : null;
                MessageBox.Show(owner, ex.Message,
                    AppLang.L("定时关机", "Shutdown timer"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch { /* ignore */ }
        }
    }

    private static void EnsureTray()
    {
        if (_tray is not null) return;
        _trayIcon ??= CreatePadlockIcon(AppTheme.Primary);
        _blankIcon ??= CreateBlankIcon();
        _tray = new NotifyIcon
        {
            Icon = _trayIcon,
            Visible = true,
            Text = AppLang.L("SrvDesk 定时关机", "SrvDesk shutdown timer"),
        };
        _tray.DoubleClick += (_, _) => ShutdownTimerDialog.ShowOrActivate(_owner);
        var menu = new ContextMenuStrip();
        menu.Items.Add(AppLang.L("打开...", "Open..."), null, (_, _) => ShutdownTimerDialog.ShowOrActivate(_owner));
        menu.Items.Add(AppLang.L("取消定时", "Cancel timer"), null, (_, _) => Disarm());
        _tray.ContextMenuStrip = menu;
    }

    private static void UpdateTrayTip()
    {
        if (_tray is null) return;
        var tip = IsArmed
            ? $"{ShutdownPowerActions.DisplayName(Action)} · {ExecutionTime:yyyy-MM-dd HH:mm:ss}"
            : AppLang.L("未设置定时", "No action set");
        if (tip.Length > 60) tip = tip.Substring(0, 60);
        _tray.Text = tip;
    }

    private static void UpdateTrayBlink(int remain)
    {
        if (_tray is null || _trayIcon is null || _blankIcon is null) return;
        UpdateTrayTip();
        if (!BlinkEnabled || remain > BlinkSeconds)
        {
            _tray.Icon = _trayIcon;
            return;
        }
        _blinkShow = !_blinkShow;
        _tray.Icon = _blinkShow ? _trayIcon : _blankIcon;
    }

    private static void DisposeTray()
    {
        if (_tray is null) return;
        _tray.Visible = false;
        _tray.Dispose();
        _tray = null;
    }

    private static Icon CreatePadlockIcon(Color accent)
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(accent, 1.6f);
            using var brush = new SolidBrush(accent);
            g.DrawArc(pen, 4, 2, 8, 8, 200, 140);
            g.FillRectangle(brush, 3, 7, 10, 7);
        }
        var h = bmp.GetHicon();
        var icon = Icon.FromHandle(h);
        // Clone so we can free the HICON copy safely after DestroyIcon if needed — keep bmp alive via icon clone
        var clone = (Icon)icon.Clone();
        NativeMethods.DestroyIcon(h);
        bmp.Dispose();
        icon.Dispose();
        return clone;
    }

    private static Icon CreateBlankIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.Transparent);
        var h = bmp.GetHicon();
        var icon = Icon.FromHandle(h);
        var clone = (Icon)icon.Clone();
        NativeMethods.DestroyIcon(h);
        bmp.Dispose();
        icon.Dispose();
        return clone;
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }
}
