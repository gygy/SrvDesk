using System.Diagnostics;

namespace SrvDesk;

internal sealed class CleanupItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Group { get; set; } = "";
    public string Hint { get; set; } = "";
    public bool DefaultOn { get; set; } = true;
    public bool Heavy { get; set; }
}

internal sealed class CleanupProgress
{
    public int Files;
    public long Bytes;
    public int Failed;
    public string Current = "";
}

/// <summary>对照 ZyperWin++ 的清理项，用本机 API 删文件（不走 cmd）。</summary>
internal static class CleanupEngine
{
    public static IReadOnlyList<CleanupItem> Items { get; } =
    [
        Item("rdp-cache", "远程桌面缓存", "缓存文件",
            "Terminal Server Client 缓存，不影响已保存凭据。"),
        Item("wu-cache", "Windows 更新缓存", "缓存文件",
            "SoftwareDistribution\\Download。正在下载的更新会被跳过。"),
        Item("inet-cache", "网页缓存", "缓存文件",
            "IE / 旧版 WebView 的 INetCache。"),
        Item("cookies", "Cookies", "缓存文件",
            "会退出部分网站登录。", on: false),
        Item("thumbs", "缩略图缓存", "缓存文件",
            "资源管理器 thumbcache。下次浏览会重建。"),
        Item("d3d", "D3D 着色器缓存", "缓存文件",
            "%LocalAppData%\\D3DSCache。"),
        Item("delivery", "传递优化缓存", "缓存文件",
            "Delivery Optimization 下载缓存。"),
        Item("dotnet-ni", ".NET 程序集缓存", "缓存文件",
            "NativeImages，清理后首次启动会变慢。", on: false, heavy: true),
        Item("winsxs", "过时的 WinSxS 组件", "系统文件",
            "DISM StartComponentCleanup，可能需数分钟。不含 ResetBase。", on: false, heavy: true),
        Item("appx-error", "错误应用包", "系统文件",
            "卸载状态为 Error 的 Appx。Server 上常无此项。", on: false),
        Item("win-logs", "Windows 日志文件", "系统文件",
            "Windows\\Logs 下的日志，排障前勿清。", on: false),
        Item("wer", "Windows 错误报告", "系统文件",
            "WER ReportQueue。"),
        Item("diagnosis", "诊断数据", "系统文件",
            "ProgramData\\Microsoft\\Diagnosis。"),
        Item("minidump", "崩溃转储", "系统文件",
            "Minidump 与 memory.dmp。"),
        Item("defender-scan", "Defender 扫描缓存", "临时文件",
            "Windows Defender 扫描残留。"),
        Item("winsxs-temp", "WinSxS 临时文件", "临时文件",
            "WinSxS\\Temp。"),
        Item("temp", "用户与系统临时文件", "临时文件",
            "%TEMP% 与 Windows\\Temp。占用中的文件会跳过。"),
        Item("sys-dmp", "系统盘根目录 dmp", "临时文件",
            "系统盘根目录下的 *.dmp。"),
        Item("recycle", "回收站", "临时文件",
            "清空所有驱动器回收站。", on: false),
        Item("prefetch", "预读取文件", "临时文件",
            "Windows\\Prefetch。"),
        Item("recent", "最近打开的文件快捷方式", "临时文件",
            "开始菜单「最近使用的项目」。"),
    ];

    public static IEnumerable<string> Groups =>
        Items.Select(i => i.Group).Distinct();

    public static void Run(IEnumerable<string> ids, Action<CleanupProgress> progress, CancellationToken cancel)
    {
        var set = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        var stat = new CleanupProgress();
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var sysDrive = Path.GetPathRoot(windows) ?? @"C:\";

        void Step(string title)
        {
            cancel.ThrowIfCancellationRequested();
            stat.Current = title;
            progress(stat);
        }

        foreach (var item in Items)
        {
            if (!set.Contains(item.Id)) continue;
            Step(item.Title);
            try
            {
                switch (item.Id)
                {
                    case "rdp-cache":
                        Sweep(Path.Combine(local, @"Microsoft\Terminal Server Client\Cache"), stat, cancel);
                        break;
                    case "wu-cache":
                        Sweep(Path.Combine(windows, @"SoftwareDistribution\Download"), stat, cancel);
                        break;
                    case "inet-cache":
                        Sweep(Path.Combine(local, @"Microsoft\Windows\INetCache"), stat, cancel);
                        break;
                    case "cookies":
                        Sweep(Path.Combine(local, @"Microsoft\Windows\INetCookies"), stat, cancel);
                        break;
                    case "thumbs":
                        Sweep(Path.Combine(local, @"Microsoft\Windows\Explorer"), stat, cancel,
                            "thumbcache_*.db", recursive: false);
                        break;
                    case "d3d":
                        Sweep(Path.Combine(local, "D3DSCache"), stat, cancel);
                        break;
                    case "delivery":
                        Sweep(Path.Combine(windows, @"SoftwareDistribution\DeliveryOptimization"), stat, cancel);
                        Sweep(Path.Combine(windows,
                            @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache"),
                            stat, cancel);
                        break;
                    case "dotnet-ni":
                        Sweep(Path.Combine(windows, @"assembly\NativeImages_v4.0.30319_64"), stat, cancel);
                        Sweep(Path.Combine(windows, @"assembly\NativeImages_v4.0.30319_32"), stat, cancel);
                        break;
                    case "winsxs":
                        RunDismComponentCleanup(stat, cancel);
                        break;
                    case "appx-error":
                        RemoveBrokenAppx(stat, cancel);
                        break;
                    case "win-logs":
                        Sweep(Path.Combine(windows, "Logs"), stat, cancel);
                        break;
                    case "wer":
                        Sweep(Path.Combine(programData, @"Microsoft\Windows\WER\ReportQueue"), stat, cancel);
                        break;
                    case "diagnosis":
                        Sweep(Path.Combine(programData, @"Microsoft\Diagnosis"), stat, cancel);
                        break;
                    case "minidump":
                        Sweep(Path.Combine(windows, "Minidump"), stat, cancel, "*.dmp", recursive: false);
                        DeleteOne(Path.Combine(windows, "MEMORY.DMP"), stat);
                        DeleteOne(Path.Combine(windows, "memory.dmp"), stat);
                        break;
                    case "defender-scan":
                        Sweep(Path.Combine(programData, @"Microsoft\Windows Defender\Scans"), stat, cancel);
                        break;
                    case "winsxs-temp":
                        Sweep(Path.Combine(windows, @"WinSxS\Temp"), stat, cancel);
                        break;
                    case "temp":
                        Sweep(Path.GetTempPath(), stat, cancel);
                        Sweep(Path.Combine(windows, "Temp"), stat, cancel);
                        break;
                    case "sys-dmp":
                        Sweep(sysDrive.TrimEnd('\\'), stat, cancel, "*.dmp", recursive: false);
                        break;
                    case "recycle":
                        DesktopQuickActions.EmptyRecycleBin(null, notify: false);
                        break;
                    case "prefetch":
                        Sweep(Path.Combine(windows, "Prefetch"), stat, cancel, recursive: false);
                        break;
                    case "recent":
                        Sweep(Environment.GetFolderPath(Environment.SpecialFolder.Recent), stat, cancel);
                        break;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                stat.Failed++;
                ApplyLog.Write($"垃圾清理跳过 {item.Title}：{ex.Message}");
            }
            progress(stat);
        }
    }

    private static CleanupItem Item(string id, string title, string group, string hint,
        bool on = true, bool heavy = false) => new()
    {
        Id = id,
        Title = title,
        Group = group,
        Hint = hint,
        DefaultOn = on,
        Heavy = heavy,
    };

    private static void Sweep(string dir, CleanupProgress stat, CancellationToken cancel,
        string pattern = "*", bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        Walk(dir, pattern, recursive, stat, cancel);
    }

    private static void Walk(string dir, string pattern, bool recursive, CleanupProgress stat, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        try
        {
            foreach (var file in Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                DeleteOne(file, stat);
        }
        catch { /* 无权限 */ }

        if (!recursive) return;
        try
        {
            foreach (var sub in Directory.EnumerateDirectories(dir))
                Walk(sub, pattern, true, stat, cancel);
        }
        catch { /* 无权限 */ }
    }

    private static void DeleteOne(string file, CleanupProgress stat)
    {
        try
        {
            if (!File.Exists(file)) return;
            var info = new FileInfo(file);
            var size = info.Length;
            info.Attributes &= ~(FileAttributes.ReadOnly | FileAttributes.Hidden);
            info.Delete();
            stat.Files++;
            stat.Bytes += size;
        }
        catch
        {
            stat.Failed++;
        }
    }

    private static void RunDismComponentCleanup(CleanupProgress stat, CancellationToken cancel)
    {
        var dism = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dism.exe");
        if (!File.Exists(dism))
        {
            stat.Failed++;
            return;
        }

        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = dism,
            Arguments = "/Online /Cleanup-Image /StartComponentCleanup",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (p is null)
        {
            stat.Failed++;
            return;
        }

        while (!p.WaitForExit(500))
        {
            if (!cancel.IsCancellationRequested) continue;
            try { if (!p.HasExited) p.Kill(); } catch { /* ignore */ }
            cancel.ThrowIfCancellationRequested();
        }

        if (p.ExitCode != 0)
            stat.Failed++;
    }

    private static void RemoveBrokenAppx(CleanupProgress stat, CancellationToken cancel)
    {
        var ps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"WindowsPowerShell\v1.0\powershell.exe");
        if (!File.Exists(ps))
        {
            stat.Failed++;
            return;
        }

        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = ps,
            Arguments = "-NoProfile -NonInteractive -Command \"Get-AppxPackage -AllUsers | " +
                        "Where-Object { $_.Status -eq 'Error' } | Remove-AppxPackage -ErrorAction SilentlyContinue\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (p is null)
        {
            stat.Failed++;
            return;
        }

        while (!p.WaitForExit(400))
        {
            if (!cancel.IsCancellationRequested) continue;
            try { if (!p.HasExited) p.Kill(); } catch { /* ignore */ }
            cancel.ThrowIfCancellationRequested();
        }
    }
}
