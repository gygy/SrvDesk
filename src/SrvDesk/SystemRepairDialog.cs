using System.Diagnostics;
using System.Text;

namespace SrvDesk;

/// <summary>系统修复工具流：SFC / DISM / 重置 Windows Update / 重置网络（对齐 WinUtil Config 精华）。</summary>
internal sealed class SystemRepairDialog : Form
{
    private readonly TextBox _log = new();
    private readonly ProgressBar _bar = new();
    private readonly Label _status = new();
    private readonly Button[] _actionButtons;
    private CancellationTokenSource? _cts;
    private bool _busy;

    public SystemRepairDialog()
    {
        Text = AppLang.L("系统修复", "System repair");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(780, 560);
        MinimumSize = new Size(680, 480);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.AutoScroll = false;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = UiFit.ControlHeight() * 2 + 28,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 8),
        };

        var btnSfc = ThemedSettingsChrome.CreateButton(AppLang.L("SFC 扫描修复", "SFC scan"), true);
        btnSfc.Click += async (_, _) => await RunConfirmedAsync(
            AppLang.L("SFC 扫描修复", "SFC scan"),
            AppLang.L(
                "将运行 sfc /scannow，可能需要较长时间。建议先关闭其它安装/更新操作。继续？",
                "Runs sfc /scannow and may take a long time. Close other install/update work first. Continue?"),
            token => RunProcessAsync("sfc.exe", "/scannow", token, timeoutMs: 0));

        var btnDism = ThemedSettingsChrome.CreateButton(AppLang.L("DISM 修复映像", "DISM restore"), false);
        btnDism.Margin = new Padding(8, 0, 0, 0);
        btnDism.Click += async (_, _) => await RunConfirmedAsync(
            AppLang.L("DISM 修复映像", "DISM restore"),
            AppLang.L(
                "将运行 DISM /RestoreHealth，需联网且耗时更长。继续？",
                "Runs DISM /RestoreHealth; needs network and takes longer. Continue?"),
            token => RunProcessAsync(
                "dism.exe",
                "/Online /Cleanup-Image /RestoreHealth",
                token,
                timeoutMs: 0));

        var btnWu = ThemedSettingsChrome.CreateButton(AppLang.L("重置 Windows Update", "Reset Windows Update"), false);
        btnWu.Margin = new Padding(8, 0, 0, 0);
        btnWu.Click += async (_, _) => await RunConfirmedAsync(
            AppLang.L("重置 Windows Update", "Reset Windows Update"),
            AppLang.L(
                "将停止更新相关服务，并重命名 SoftwareDistribution / catroot2 缓存目录。进行中的更新会中断。继续？",
                "Stops update services and renames SoftwareDistribution / catroot2. In-progress updates will stop. Continue?"),
            ResetWindowsUpdateAsync);

        var btnNet = ThemedSettingsChrome.CreateButton(AppLang.L("重置网络栈", "Reset network"), false);
        btnNet.Margin = new Padding(8, 0, 0, 0);
        btnNet.Click += async (_, _) => await RunConfirmedAsync(
            AppLang.L("重置网络栈", "Reset network"),
            AppLang.L(
                "将执行 winsock / IP 栈重置并刷新 DNS。通常需要重启后完全生效，短暂断网属正常。继续？",
                "Resets winsock/IP stack and flushes DNS. A reboot is usually required. Brief disconnect is normal. Continue?"),
            ResetNetworkAsync);

        var btnStop = ThemedSettingsChrome.CreateButton(AppLang.L("停止", "Stop"), false);
        btnStop.Margin = new Padding(16, 0, 0, 0);
        btnStop.Click += (_, _) => _cts?.Cancel();

        _actionButtons = [btnSfc, btnDism, btnWu, btnNet];
        actions.Controls.AddRange([btnSfc, btnDism, btnWu, btnNet, btnStop]);

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Both;
        _log.Dock = DockStyle.Fill;
        _log.Font = new Font("Consolas", 9f);
        _log.BackColor = AppTheme.SurfaceCard;
        _log.ForeColor = AppTheme.TextMain;
        _log.BorderStyle = BorderStyle.FixedSingle;

        var progress = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(0, 8, 0, 0),
        };
        _bar.Dock = DockStyle.Top;
        _bar.Height = 16;
        _bar.Style = ProgressBarStyle.Marquee;
        _bar.MarqueeAnimationSpeed = 0;
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = AppTheme.TextMute;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Text = AppLang.L(
            "每项需确认后执行。日志写入下方；SFC/DISM 可能需数十分钟。",
            "Each action asks for confirm. Logs appear below; SFC/DISM may take tens of minutes.");
        progress.Controls.Add(_status);
        progress.Controls.Add(_bar);

        body.Controls.Add(_log);
        body.Controls.Add(progress);
        body.Controls.Add(actions);
        actions.BringToFront();
        progress.BringToFront();
        _log.SendToBack();

        ThemedSettingsChrome.MountModal(
            this,
            AppLang.L("系统修复", "System repair"),
            AppLang.L("SFC · DISM · 重置更新 · 重置网络", "SFC · DISM · Reset Update · Reset network"),
            body,
            AppLang.L(
                "修复不保证解决所有故障；重置更新/网络后建议重启。",
                "Repair does not fix every issue; reboot after reset update/network."));

        FormClosing += (_, e) =>
        {
            if (!_busy) return;
            e.Cancel = true;
            _cts?.Cancel();
        };
    }

    private async Task RunConfirmedAsync(string title, string confirm, Func<CancellationToken, Task> work)
    {
        if (_busy) return;
        if (MessageBox.Show(this, confirm, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _busy = true;
        _cts = new CancellationTokenSource();
        foreach (var b in _actionButtons) b.Enabled = false;
        _bar.MarqueeAnimationSpeed = 30;
        _status.Text = title + "…";
        AppendLog("==== " + title + " ====");
        try
        {
            await work(_cts.Token).ConfigureAwait(true);
            AppendLog(AppLang.L("完成。", "Done."));
            _status.Text = AppLang.L("完成：", "Done: ") + title;
        }
        catch (OperationCanceledException)
        {
            AppendLog(AppLang.L("已取消。", "Cancelled."));
            _status.Text = AppLang.L("已取消", "Cancelled");
        }
        catch (Exception ex)
        {
            AppendLog(AppLang.L("失败：", "Failed: ") + ex.Message);
            _status.Text = AppLang.L("失败：", "Failed: ") + title;
            ApplyLog.Write("系统修复失败：" + title + " — " + ex.Message);
        }
        finally
        {
            _busy = false;
            _cts?.Dispose();
            _cts = null;
            foreach (var b in _actionButtons) b.Enabled = true;
            _bar.MarqueeAnimationSpeed = 0;
        }
    }

    private async Task ResetWindowsUpdateAsync(CancellationToken token)
    {
        string[] services = ["bits", "wuauserv", "cryptsvc", "msiserver", "usosvc", "dosvc"];
        foreach (var svc in services)
        {
            token.ThrowIfCancellationRequested();
            await RunProcessAsync("sc.exe", "stop " + svc, token, timeoutMs: 30_000, ignoreExitCode: true)
                .ConfigureAwait(true);
        }

        var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var sd = Path.Combine(windir, "SoftwareDistribution");
        var cat = Path.Combine(windir, "System32", "catroot2");
        var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        RenameDirSafe(sd, sd + ".bak-" + stamp);
        RenameDirSafe(cat, cat + ".bak-" + stamp);

        foreach (var svc in services)
        {
            token.ThrowIfCancellationRequested();
            await RunProcessAsync("sc.exe", "start " + svc, token, timeoutMs: 30_000, ignoreExitCode: true)
                .ConfigureAwait(true);
        }
    }

    private async Task ResetNetworkAsync(CancellationToken token)
    {
        await RunProcessAsync("netsh.exe", "winsock reset", token, timeoutMs: 60_000).ConfigureAwait(true);
        await RunProcessAsync("netsh.exe", "int ip reset", token, timeoutMs: 60_000, ignoreExitCode: true)
            .ConfigureAwait(true);
        await RunProcessAsync("ipconfig.exe", "/flushdns", token, timeoutMs: 30_000).ConfigureAwait(true);
        AppendLog(AppLang.L("提示：重置网络后建议重启计算机。", "Tip: reboot after network reset."));
    }

    private void RenameDirSafe(string path, string backup)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                AppendLog(AppLang.L("跳过（不存在）：", "Skip (missing): ") + path);
                return;
            }

            if (Directory.Exists(backup))
                Directory.Delete(backup, recursive: true);
            Directory.Move(path, backup);
            AppendLog(AppLang.L("已重命名：", "Renamed: ") + path + " → " + backup);
            ApplyLog.WriteChange("系统修复：重命名 " + path + " → " + backup);
        }
        catch (Exception ex)
        {
            AppendLog(AppLang.L("重命名失败：", "Rename failed: ") + path + " — " + ex.Message);
            throw;
        }
    }

    private async Task RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken token,
        int timeoutMs,
        bool ignoreExitCode = false)
    {
        AppendLog("> " + fileName + " " + arguments);
        ApplyLog.Debug("系统修复 Run " + fileName + " " + arguments);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default,
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 " + fileName);
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        using (token.Register(() =>
               {
                   try { if (!p.HasExited) p.Kill(); } catch { /* ignore */ }
               }))
        {
            if (timeoutMs > 0)
            {
                var exited = await Task.Run(() => p.WaitForExit(timeoutMs), token).ConfigureAwait(true);
                if (!exited)
                {
                    try { p.Kill(); } catch { /* ignore */ }
                    throw new TimeoutException(fileName + " 超时");
                }
            }
            else
            {
                await Task.Run(p.WaitForExit, token).ConfigureAwait(true);
            }
        }

        token.ThrowIfCancellationRequested();
        var stdout = await stdoutTask.ConfigureAwait(true);
        var stderr = await stderrTask.ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(stdout)) AppendLog(stdout.TrimEnd());
        if (!string.IsNullOrWhiteSpace(stderr)) AppendLog(stderr.TrimEnd());

        if (p.ExitCode != 0 && !ignoreExitCode)
            throw new InvalidOperationException(Path.GetFileName(fileName) + " 退出码 " + p.ExitCode);
        if (p.ExitCode != 0)
            AppendLog(AppLang.L("退出码（已忽略）：", "Exit code (ignored): ") + p.ExitCode);
    }

    private void AppendLog(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AppendLog(line)));
            return;
        }

        if (_log.TextLength > 0) _log.AppendText(Environment.NewLine);
        _log.AppendText(line);
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }
}
