using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SrvDesk;

/// <summary>
/// 天翼云盘安装包为自绘壳（EcloudSetupWnd），普通 /S 无效，需勾选协议并点「安装」。
/// 通过截屏识别勾选框/蓝色安装按钮并模拟鼠标点击。
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
        var hwnd = WaitForMainWindow(p, TimeSpan.FromSeconds(45));
        if (hwnd == IntPtr.Zero)
        {
            try { p.Kill(); } catch { /* ignore */ }
            ApplyLog.Write("天翼安装：未出现安装窗口");
            return -3;
        }

        ApplyLog.Write("天翼安装：已出现窗口，开始自动点击协议与安装");
        Thread.Sleep(1_200);

        var clickedInstall = false;
        var lastClick = DateTime.MinValue;
        while (DateTime.UtcNow < deadline && !p.HasExited)
        {
            p.Refresh();
            hwnd = p.MainWindowHandle;
            if (hwnd == IntPtr.Zero)
            {
                Thread.Sleep(400);
                continue;
            }

            if ((DateTime.UtcNow - lastClick).TotalMilliseconds >= 900)
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
                // 再等一会让安装程序自己收尾
                if (p.WaitForExit(15_000))
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
        if (!GetWindowRect(hwnd, out var rect))
            return false;

        var w = rect.Right - rect.Left;
        var h = rect.Bottom - rect.Top;
        if (w < 80 || h < 60)
            return false;

        SetForegroundWindow(hwnd);
        Thread.Sleep(120);

        using var bmp = CaptureWindow(rect);
        if (bmp is null)
            return false;

        // 小对话框（已安装提示等）：点中下部确认
        if (w < 420 || h < 260)
        {
            ClickScreen(rect.Left + w / 2, rect.Top + (int)(h * 0.72));
            return true;
        }

        // 先勾选协议（左下）
        if (TryFindCheckbox(bmp, out var cx, out var cy))
            ClickScreen(rect.Left + cx, rect.Top + cy);
        else
        {
            ClickScreen(rect.Left + (int)(w * 0.10), rect.Top + (int)(h * 0.88));
            ClickScreen(rect.Left + (int)(w * 0.14), rect.Top + (int)(h * 0.91));
        }

        Thread.Sleep(400);

        // 再点蓝色「安装」按钮
        if (TryFindBlueButton(bmp, out var bx, out var by))
        {
            ClickScreen(rect.Left + bx, rect.Top + by);
            clickedInstall = true;
            return true;
        }

        // 回退：覆盖常见按钮落点（中下 / 偏右）
        foreach (var (rx, ry) in new[]
                 {
                     (0.50, 0.55), (0.50, 0.60), (0.50, 0.66),
                     (0.50, 0.72), (0.68, 0.68), (0.72, 0.75),
                 })
        {
            ClickScreen(rect.Left + (int)(w * rx), rect.Top + (int)(h * ry));
            Thread.Sleep(180);
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
        var x1 = (int)(w * 0.28);
        var y0 = (int)(h * 0.78);
        var y1 = (int)(h * 0.96);
        var best = 0;
        var bestX = 0;
        var bestY = 0;
        for (var yy = y0; yy < y1; yy += 2)
        for (var xx = x0; xx < x1; xx += 2)
        {
            var c = bmp.GetPixel(xx, yy);
            var bright = (c.R + c.G + c.B) / 3;
            // 未勾选框边缘偏深；已勾选也可能偏蓝
            if (bright is > 30 and < 140 || (c.B > c.R + 20 && c.B > 100))
            {
                var score = 1;
                if (bright < 90) score += 2;
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

    /// <summary>找窗口下半部分最大的蓝色连通块中心（立即安装按钮）。</summary>
    private static bool TryFindBlueButton(Bitmap bmp, out int x, out int y)
    {
        x = y = 0;
        var w = bmp.Width;
        var h = bmp.Height;
        var y0 = (int)(h * 0.35);
        long sumX = 0, sumY = 0;
        var count = 0;
        for (var yy = y0; yy < h; yy += 2)
        for (var xx = 0; xx < w; xx += 2)
        {
            var c = bmp.GetPixel(xx, yy);
            // 天翼安装按钮常见青蓝/亮蓝
            if (c.B > 140 && c.B > c.R + 25 && c.B >= c.G - 10 && c.R < 180)
            {
                sumX += xx;
                sumY += yy;
                count++;
            }
        }

        if (count < 80) return false;
        x = (int)(sumX / count);
        y = (int)(sumY / count);
        return true;
    }

    private static IntPtr WaitForMainWindow(Process p, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                p.Refresh();
                if (p.HasExited) return IntPtr.Zero;
                if (p.MainWindowHandle != IntPtr.Zero)
                    return p.MainWindowHandle;
            }
            catch { /* ignore */ }
            Thread.Sleep(200);
        }

        return IntPtr.Zero;
    }

    private static void ClickScreen(int x, int y)
    {
        SetCursorPos(x, y);
        Thread.Sleep(30);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(35);
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
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
