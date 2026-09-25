using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace SrvDesk;

/// <summary>
/// 天翼云盘安装包为自绘壳（EcloudSetupWnd），普通 /S 无效，需勾选协议并点「安装」。
/// 通过找窗口 + 截屏识别勾选框/安装按钮并模拟鼠标点击。
/// </summary>
internal static class TianyiSetupUiDriver
{
    public static int Run(string setupExe, int timeoutMs = 600_000)
    {
        if (string.IsNullOrWhiteSpace(setupExe) || !File.Exists(setupExe))
            throw new FileNotFoundException("天翼安装包不存在", setupExe);

        // 先结束客户端，避免「请先关闭」挡住安装
        KillByName("eCloud", "ecloud", "Cloud189");

        var psi = new ProcessStartInfo
        {
            FileName = setupExe,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(setupExe) ?? Environment.CurrentDirectory,
        };
        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动天翼安装包");

        var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(30_000, timeoutMs));
        var hwnd = WaitForSetupWindow(p, TimeSpan.FromSeconds(60));
        if (hwnd == IntPtr.Zero)
        {
            try { p.Kill(); } catch { /* ignore */ }
            ApplyLog.Write("天翼安装：未出现安装窗口");
            return -3;
        }

        ApplyLog.Write("天翼安装：已出现窗口 hwnd=" + hwnd.ToInt64().ToString("X"));
        Thread.Sleep(1_500);

        var clickedInstall = false;
        var lastClick = DateTime.MinValue;
        while (DateTime.UtcNow < deadline && !p.HasExited)
        {
            p.Refresh();
            hwnd = FindSetupWindow(p);
            if (hwnd == IntPtr.Zero)
            {
                Thread.Sleep(400);
                // 主进程已退出但安装可能已完成
                if (p.HasExited) break;
                continue;
            }

            if ((DateTime.UtcNow - lastClick).TotalMilliseconds >= 1_100)
            {
                if (TryAutoClick(hwnd, ref clickedInstall))
                    lastClick = DateTime.UtcNow;
            }

            if (p.WaitForExit(400))
                break;

            // 安装过程中主程序可能已落盘
            if (File.Exists(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "ecloud", "ecloud", "eCloud.exe")))
            {
                if (p.WaitForExit(20_000))
                    break;
            }
        }

        if (!p.HasExited)
        {
            try { p.Kill(); p.WaitForExit(3_000); } catch { /* ignore */ }
            ApplyLog.Write("天翼安装：超时结束安装进程");
            return -2;
        }

        KillByName("eCloud", "ecloud", "Cloud189");
        return p.ExitCode;
    }

    private static bool TryAutoClick(IntPtr hwnd, ref bool clickedInstall)
    {
        if (!IsWindow(hwnd) || !GetWindowRect(hwnd, out var rect))
            return false;

        var w = rect.Right - rect.Left;
        var h = rect.Bottom - rect.Top;
        if (w < 80 || h < 60)
            return false;

        ForceForeground(hwnd);
        Thread.Sleep(180);

        // 小对话框（已安装提示 / UAC 类确认）：点中下部
        if (w < 420 || h < 280)
        {
            ClickScreen(rect.Left + w / 2, rect.Top + (int)(h * 0.72));
            ApplyLog.Write("天翼安装：点击小对话框确认");
            return true;
        }

        using (var bmp1 = CaptureWindow(rect))
        {
            if (bmp1 is null) return false;

            // 1) 勾选协议（左下）
            if (TryFindCheckbox(bmp1, out var cx, out var cy))
            {
                ClickScreen(rect.Left + cx, rect.Top + cy);
                ApplyLog.Write("天翼安装：勾选协议 @" + cx + "," + cy);
            }
            else
            {
                ClickScreen(rect.Left + (int)(w * 0.10), rect.Top + (int)(h * 0.88));
                Thread.Sleep(80);
                ClickScreen(rect.Left + (int)(w * 0.12), rect.Top + (int)(h * 0.90));
                ApplyLog.Write("天翼安装：协议勾选回退坐标");
            }
        }

        Thread.Sleep(550);
        ForceForeground(hwnd);
        Thread.Sleep(120);

        // 勾选后按钮常会变亮，必须重新截屏再找
        if (!GetWindowRect(hwnd, out rect))
            return false;
        w = rect.Right - rect.Left;
        h = rect.Bottom - rect.Top;

        using (var bmp2 = CaptureWindow(rect))
        {
            if (bmp2 is not null && TryFindInstallButton(bmp2, out var bx, out var by))
            {
                ClickScreen(rect.Left + bx, rect.Top + by);
                clickedInstall = true;
                ApplyLog.Write("天翼安装：点击安装按钮 @" + bx + "," + by);
                return true;
            }
        }

        // 回退：覆盖常见「立即安装 / 下一步」落点（中下偏右）
        ApplyLog.Write("天翼安装：安装按钮未识别，使用坐标回退");
        foreach (var (rx, ry) in new[]
                 {
                     (0.50, 0.58), (0.50, 0.62), (0.50, 0.68),
                     (0.55, 0.72), (0.62, 0.70), (0.70, 0.74),
                     (0.50, 0.78), (0.72, 0.80),
                 })
        {
            ClickScreen(rect.Left + (int)(w * rx), rect.Top + (int)(h * ry));
            Thread.Sleep(160);
        }

        clickedInstall = true;
        return true;
    }

    private static Bitmap? CaptureWindow(RECT rect)
    {
        try
        {
            var w = rect.Right - rect.Left;
            var h = rect.Bottom - rect.Top;
            if (w <= 0 || h <= 0) return null;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(w, h));
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>在左下角区域找偏暗的小方块（未勾选复选框）。</summary>
    private static bool TryFindCheckbox(Bitmap bmp, out int x, out int y)
    {
        x = y = 0;
        var w = bmp.Width;
        var h = bmp.Height;
        var x0 = (int)(w * 0.04);
        var x1 = (int)(w * 0.32);
        var y0 = (int)(h * 0.75);
        var y1 = (int)(h * 0.97);
        var best = 0;
        var bestX = 0;
        var bestY = 0;
        for (var yy = y0; yy < y1; yy += 2)
        for (var xx = x0; xx < x1; xx += 2)
        {
            var c = bmp.GetPixel(xx, yy);
            var bright = (c.R + c.G + c.B) / 3;
            if (bright is > 25 and < 150 || (c.B > c.R + 15 && c.B > 90))
            {
                var score = bright < 90 ? 3 : 1;
                if (score > best)
                {
                    best = score;
                    bestX = xx;
                    bestY = yy;
                }
            }
        }

        if (best <= 0) return false;
        x = bestX;
        y = bestY;
        return true;
    }

    /// <summary>找窗口下半部分最大的蓝/青/主题色按钮中心。</summary>
    private static bool TryFindInstallButton(Bitmap bmp, out int x, out int y)
    {
        x = y = 0;
        var w = bmp.Width;
        var h = bmp.Height;
        var y0 = (int)(h * 0.40);
        long sumX = 0, sumY = 0;
        var count = 0;
        for (var yy = y0; yy < h; yy += 2)
        for (var xx = (int)(w * 0.15); xx < (int)(w * 0.85); xx += 2)
        {
            var c = bmp.GetPixel(xx, yy);
            if (IsInstallButtonPixel(c))
            {
                sumX += xx;
                sumY += yy;
                count++;
            }
        }

        if (count < 60) return false;
        x = (int)(sumX / count);
        y = (int)(sumY / count);
        return true;
    }

    private static bool IsInstallButtonPixel(Color c)
    {
        // 天翼常见：亮蓝 / 青蓝 / 偏绿蓝
        if (c.B > 130 && c.B > c.R + 20 && c.B >= c.G - 15 && c.R < 190)
            return true;
        // 部分皮肤：偏青 (G≈B)
        if (c.G > 140 && c.B > 140 && c.R < 120 && Math.Abs(c.G - c.B) < 40)
            return true;
        return false;
    }

    private static IntPtr WaitForSetupWindow(Process p, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (p.HasExited) return IntPtr.Zero;
                var hwnd = FindSetupWindow(p);
                if (hwnd != IntPtr.Zero)
                    return hwnd;
            }
            catch { /* ignore */ }
            Thread.Sleep(250);
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindSetupWindow(Process installer)
    {
        try
        {
            installer.Refresh();
            if (installer.MainWindowHandle != IntPtr.Zero
                && IsLikelySetupWindow(installer.MainWindowHandle))
                return installer.MainWindowHandle;
        }
        catch { /* ignore */ }

        IntPtr found = IntPtr.Zero;
        var installerPid = 0;
        try { installerPid = installer.Id; } catch { /* ignore */ }

        EnumWindows((h, _) =>
        {
            if (!IsWindowVisible(h) || !IsLikelySetupWindow(h))
                return true;
            if (!GetWindowRect(h, out var r)) return true;
            var ww = r.Right - r.Left;
            var hh = r.Bottom - r.Top;
            if (ww < 200 || hh < 150) return true;

            GetWindowThreadProcessId(h, out var windowPid);
            // 同 PID 优先；否则接受标题/类名已匹配的可见大窗（子进程壳）
            if (installerPid != 0 && windowPid != (uint)installerPid)
            {
                // 仅当类名明确是天翼壳时才跨进程认
                var cls = GetClassName(h);
                if (cls.IndexOf("EcloudSetupWnd", StringComparison.OrdinalIgnoreCase) < 0
                    && cls.IndexOf("eCloud", StringComparison.OrdinalIgnoreCase) < 0)
                    return true;
            }

            found = h;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private static bool IsLikelySetupWindow(IntPtr hwnd)
    {
        var cls = GetClassName(hwnd);
        if (cls.IndexOf("EcloudSetupWnd", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (cls.IndexOf("eCloud", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var title = GetWindowText(hwnd);
        if (title.IndexOf("天翼", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (title.IndexOf("eCloud", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (title.IndexOf("Cloud189", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (title.IndexOf("安装", StringComparison.Ordinal) >= 0
            && title.IndexOf("云盘", StringComparison.Ordinal) >= 0)
            return true;
        return false;
    }

    private static string GetClassName(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        _ = GetClassName(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetWindowText(IntPtr hwnd)
    {
        var len = GetWindowTextLength(hwnd);
        if (len <= 0) return "";
        var sb = new StringBuilder(len + 2);
        _ = GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static void ForceForeground(IntPtr hwnd)
    {
        try
        {
            var fg = GetForegroundWindow();
            var pidFg = 0u;
            var tidFg = fg != IntPtr.Zero ? GetWindowThreadProcessId(fg, out pidFg) : 0u;
            var tidTarget = GetWindowThreadProcessId(hwnd, out _);
            var tidCurrent = GetCurrentThreadId();
            if (tidFg != 0 && tidFg != tidCurrent)
                AttachThreadInput(tidFg, tidCurrent, true);
            if (tidTarget != 0 && tidTarget != tidCurrent)
                AttachThreadInput(tidTarget, tidCurrent, true);

            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            BringWindowToTop(hwnd);

            if (tidFg != 0 && tidFg != tidCurrent)
                AttachThreadInput(tidFg, tidCurrent, false);
            if (tidTarget != 0 && tidTarget != tidCurrent)
                AttachThreadInput(tidTarget, tidCurrent, false);
        }
        catch
        {
            try { SetForegroundWindow(hwnd); } catch { /* ignore */ }
        }
    }

    private static void ClickScreen(int x, int y)
    {
        SetCursorPos(x, y);
        Thread.Sleep(40);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(45);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    private static void KillByName(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
                continue;
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (!p.HasExited)
                            p.Kill();
                    }
                    catch { /* ignore */ }
                    finally
                    {
                        try { p.Dispose(); } catch { /* ignore */ }
                    }
                }
            }
            catch { /* ignore */ }
        }
    }

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const int SW_RESTORE = 9;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
}
