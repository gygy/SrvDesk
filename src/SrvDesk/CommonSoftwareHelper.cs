using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace SrvDesk;

internal sealed class CommonSoftwareStatus
{
    public bool Installed { get; set; }
    public string Version { get; set; } = "";
    public string? UninstallCommand { get; set; }
}

/// <summary>软件安装/卸载实时进度（Percent 为 0–100 估算）。</summary>
internal sealed class SoftwareInstallProgress
{
    public string Message { get; set; } = "";
    public int Percent { get; set; }
}

/// <summary>常用软件可更新项。</summary>
internal sealed class SoftwareUpdateInfo
{
    public CommonSoftwareItem Item { get; set; } = null!;
    public string CurrentVersion { get; set; } = "";
    public string AvailableVersion { get; set; } = "";

    public string SummaryLine =>
        $"{Item.Title}  {CurrentVersion} → {AvailableVersion}";
}

internal static class CommonSoftwareHelper
{
    private static string? _wingetPath;
    private static bool _wingetDiscoveryDone;
    private static string? _wingetVersionCache;
    private static bool _speedSettingsApplied;
    private static bool _warmUpStarted;
    private static string LastPowerShellOutput = "";
    private static string LastProcessOutput = "";

    private static readonly string[] KnownWingetPaths =
    [
        @"C:\Tools\winget\winget.exe",
        @"C:\Program Files\WinGet\winget.exe",
        @"C:\Program Files (x86)\WinGet\winget.exe",
    ];

    /// <summary>Server 上管理员进程调用 WindowsApps 别名常因许可证失败；便携目录可绕过。</summary>
    private const string PortableWingetDir = @"C:\Tools\winget";
    private static string PortableWingetExe => Path.Combine(PortableWingetDir, "winget.exe");

    public static string DownloadDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SrvDesk", "software-downloads");

    /// <summary>便携工具目录，如 Codex CLI：%LocalAppData%\SrvDesk\tools\{id}\</summary>
    public static string ToolsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SrvDesk", "tools");

    public static bool IsWingetAvailable()
    {
        EnsureWingetDiscovered();
        return _wingetPath is not null;
    }

    public static void ResetWingetDiscovery()
    {
        _wingetPath = null;
        _wingetDiscoveryDone = false;
        _wingetVersionCache = null;
    }

    /// <summary>打开常用软件时后台预热：写入加速设置并更新源索引，避免首次安装卡住。</summary>
    public static void WarmUpInBackground()
    {
        if (_warmUpStarted) return;
        _warmUpStarted = true;
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                EnsureWingetSpeedSettings();
                if (!IsWingetAvailable()) return;
                // 安装时关闭自动源更新；此处后台刷一次索引
                Run(ResolveWingetPath(), "source update --disable-interactivity", setWorkingDirForExe: true);
            }
            catch
            {
                /* ignore */
            }
        });
    }

    /// <summary>降低每次 install 前同步源索引的等待；静默安装更顺畅。</summary>
    public static void EnsureWingetSpeedSettings()
    {
        if (_speedSettingsApplied) return;
        _speedSettingsApplied = true;
        try
        {
            foreach (var path in WingetSettingsPaths())
            {
                try
                {
                    var dir = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(dir)) continue;
                    Directory.CreateDirectory(dir!);
                    MergeWingetSpeedSettings(path!);
                }
                catch
                {
                    /* ignore one path */
                }
            }
        }
        catch
        {
            /* ignore */
        }
    }

    private static IEnumerable<string> WingetSettingsPaths()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(local, "Microsoft", "WinGet", "Settings", "settings.json");
        yield return Path.Combine(local, "Packages",
            "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe", "LocalState", "settings.json");
    }

    private static void MergeWingetSpeedSettings(string path)
    {
        // 关闭安装前自动源更新（0）；后台 WarmUp 会手动 update
        // downloader=do 通常比默认更快；telemetry 关闭略减开销
        const string optimized = @"{
  ""$schema"": ""https://aka.ms/winget-settings.schema.json"",
  ""source"": {
    ""autoUpdateIntervalInMinutes"": 0
  },
  ""network"": {
    ""downloader"": ""do""
  },
  ""telemetry"": {
    ""disable"": true
  },
  ""installBehavior"": {
    ""preferences"": {
      ""scope"": ""machine""
    }
  }
}
";
        if (!File.Exists(path))
        {
            File.WriteAllText(path, optimized, Encoding.UTF8);
            ApplyLog.Write("已写入 winget 加速设置：" + path);
            return;
        }

        var text = File.ReadAllText(path, Encoding.UTF8);
        if (text.IndexOf("\"autoUpdateIntervalInMinutes\": 0", StringComparison.Ordinal) >= 0 ||
            text.IndexOf("\"autoUpdateIntervalInMinutes\":0", StringComparison.Ordinal) >= 0)
            return;

        var updated = Regex.Replace(
            text,
            "\"autoUpdateIntervalInMinutes\"\\s*:\\s*\\d+",
            "\"autoUpdateIntervalInMinutes\": 0");
        if (updated == text)
        {
            // 无该键：在首个 { 后插入 source 段
            var idx = text.IndexOf('{');
            if (idx < 0) return;
            updated = text.Substring(0, idx + 1) +
                      "\r\n  \"source\": { \"autoUpdateIntervalInMinutes\": 0 }," +
                      text.Substring(idx + 1);
        }

        File.WriteAllText(path, updated, Encoding.UTF8);
        ApplyLog.Write("已优化 winget 源更新间隔：" + path);
    }

    public static string ResolveWingetPath()
    {
        EnsureWingetDiscovered();
        return _wingetPath ?? "winget.exe";
    }

    private static void EnsureWingetDiscovered()
    {
        if (_wingetDiscoveryDone) return;
        _wingetDiscoveryDone = true;

        if (_wingetPath is not null && ProbeWinget(_wingetPath))
            return;

        _wingetPath = null;
        foreach (var candidate in DiscoverWingetCandidates())
        {
            if (!ProbeWinget(candidate)) continue;
            _wingetPath = candidate;
            ApplyLog.Write("检测到 winget：" + candidate);
            return;
        }
    }

    private static IEnumerable<string> DiscoverWingetCandidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var known in KnownWingetPaths)
        {
            if (seen.Add(known))
                yield return known;
        }

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
        {
            var path = Path.Combine(dir.Trim(), "winget.exe");
            if (seen.Add(path))
                yield return path;
        }

        var whereHits = new List<string>();
        try
        {
            var output = RunCapture("where.exe", "winget.exe");
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                whereHits.Add(line.Trim().Trim('"'));
        }
        catch { /* ignore */ }

        foreach (var path in whereHits)
        {
            if (seen.Add(path))
                yield return path;
        }

        var appx = TryGetAppxWingetPath();
        if (!string.IsNullOrWhiteSpace(appx) && seen.Add(appx!))
            yield return appx!;

        foreach (var path in TryFindWindowsAppsWingetPaths())
        {
            if (seen.Add(path))
                yield return path;
        }

        var alias = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "winget.exe");
        if (seen.Add(alias))
            yield return alias;
    }

    private static string? TryGetAppxWingetPath()
    {
        try
        {
            var output = RunCapture("powershell.exe",
                "-NoProfile -Command \"(Get-AppxPackage -AllUsers -Name Microsoft.DesktopAppInstaller | Select-Object -First 1 -ExpandProperty InstallLocation)\"").Trim();
            if (output.Length == 0)
            {
                output = RunCapture("powershell.exe",
                    "-NoProfile -Command \"(Get-AppxPackage -Name Microsoft.DesktopAppInstaller | Select-Object -First 1 -ExpandProperty InstallLocation)\"").Trim();
            }
            if (output.Length == 0) return null;
            var path = Path.Combine(output, "winget.exe");
            return File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> TryFindWindowsAppsWingetPaths()
    {
        const string root = @"C:\Program Files\WindowsApps";
        if (!Directory.Exists(root)) yield break;

        string[] dirs;
        try
        {
            dirs = Directory.GetDirectories(root, "Microsoft.DesktopAppInstaller_*");
        }
        catch
        {
            yield break;
        }

        foreach (var dir in dirs)
        {
            var path = Path.Combine(dir, "winget.exe");
            if (File.Exists(path))
                yield return path;
        }
    }

    private static bool ProbeWinget(string path)
    {
        if (path != "winget.exe" && !File.Exists(path))
            return false;

        // 缺 VC 运行库时直接判定失败，避免弹出「找不到 VCRUNTIME140.dll」
        if (!IsVcRuntime140Present())
            return false;

        var previousErrorMode = SetErrorMode(SemFailCriticalErrors | SemNoOpenFileErrorBox);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Utf8NoBom,
                StandardErrorEncoding = Utf8NoBom,
            };
            // WindowsApps 内的 winget 依赖同目录 DLL，必须指定工作目录
            if (path != "winget.exe")
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    psi.WorkingDirectory = dir!;
            }

            using var p = Process.Start(psi);
            if (p is null) return false;
            var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(15_000);
            return p.ExitCode == 0 &&
                (output.IndexOf('v') >= 0 ||
                 output.IndexOf("Windows Package Manager", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch
        {
            return false;
        }
        finally
        {
            SetErrorMode(previousErrorMode);
        }
    }

    private static bool IsVcRuntime140Present()
    {
        var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
        return File.Exists(Path.Combine(sys, "VCRUNTIME140.dll"));
    }

    /// <summary>安装 VC++ 2015-2022 x64（winget / 便携 winget 依赖 VCRUNTIME140.dll）。</summary>
    private static string EnsureVcRedistX64(Action<SoftwareInstallProgress>? onProgress = null)
    {
        if (IsVcRuntime140Present())
            return "";

        Report(onProgress, "安装 Visual C++ 运行库（winget 依赖）…", 6);
        Directory.CreateDirectory(DownloadDir);
        var dest = Path.Combine(DownloadDir, "vc_redist.x64.exe");
        try
        {
            DownloadFromMirrors(
                dest,
                minBytes: 5_000_000,
                onProgress,
                percentBase: 6,
                percentSpan: 10,
                requireZipMagic: false,
                "https://aka.ms/vs/17/release/vc_redist.x64.exe",
                "https://aka.ms/vs/16/release/vc_redist.x64.exe");
        }
        catch (Exception ex)
        {
            ApplyLog.Write("VC++ 运行库下载失败：" + ex.Message);
            return "VC++ 运行库下载失败：" + ShortNetError(ex) +
                   "。请先安装 https://aka.ms/vs/17/release/vc_redist.x64.exe 后再试。";
        }

        Report(onProgress, "正在静默安装 VC++ 运行库…", 16);
        var code = Run(dest, "/install /quiet /norestart", timeoutMs: 300_000);
        // 0=成功；1638=已安装更高版本；3010=成功需重启
        if (code is 0 or 1638 or 3010 || IsVcRuntime140Present())
        {
            ApplyLog.Write("已安装 VC++ x64 Redistributable，退出码 " + code);
            // 给加载器一点时间刷新
            System.Threading.Thread.Sleep(500);
            return IsVcRuntime140Present()
                ? "已安装 Visual C++ 2015-2022 x64 运行库。"
                : "已执行 VC++ 安装（退出码 " + code + "），若仍缺 DLL 请重启后再试。";
        }

        ApplyLog.Write("VC++ 运行库安装失败，退出码 " + code);
        return "VC++ 运行库安装退出码 " + code + "。";
    }

    private static Dictionary<string, CommonSoftwareStatus>? _statusCache;
    private static readonly object StatusCacheLock = new();

    public static void InvalidateStatusCache()
    {
        lock (StatusCacheLock)
            _statusCache = null;
    }

    /// <summary>一次扫描卸载注册表，缓存全部常用软件安装状态（打开窗口时后台调用）。</summary>
    public static void PrefetchStatuses(IReadOnlyList<CommonSoftwareItem> items)
    {
        var map = new Dictionary<string, CommonSoftwareStatus>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (item.IsWingetBootstrap)
            {
                map[item.Id] = QueryWingetStatus();
                continue;
            }

            map[item.Id] = new CommonSoftwareStatus();
        }

        foreach (var keyPath in UninstallKeyPaths())
        {
            using var baseKey = RegistryKey.OpenBaseKey(keyPath.Hive, RegistryView.Registry64);
            using var uninstall = baseKey.OpenSubKey(keyPath.SubKey);
            if (uninstall is null) continue;

            foreach (var subName in uninstall.GetSubKeyNames())
            {
                using var sub = uninstall.OpenSubKey(subName);
                if (sub is null) continue;
                var display = sub.GetValue("DisplayName") as string ?? "";
                if (display.Length == 0) continue;

                foreach (var item in items)
                {
                    if (item.IsWingetBootstrap) continue;
                    if (map.TryGetValue(item.Id, out var existing) && existing.Installed) continue;
                    if (!Matches(display, item.DetectPatterns)) continue;

                    map[item.Id] = new CommonSoftwareStatus
                    {
                        Installed = true,
                        Version = sub.GetValue("DisplayVersion") as string ?? "",
                        UninstallCommand = sub.GetValue("QuietUninstallString") as string
                            ?? sub.GetValue("UninstallString") as string,
                    };
                }
            }
        }

        foreach (var item in items)
        {
            if (item.IsWingetBootstrap) continue;
            if (string.IsNullOrWhiteSpace(item.AppxPackageName)) continue;
            if (map.TryGetValue(item.Id, out var existing) && existing.Installed) continue;
            var appx = QueryAppxStatus(item.AppxPackageName);
            if (appx.Installed) map[item.Id] = appx;
        }

        foreach (var item in items)
        {
            if (item.IsWingetBootstrap) continue;
            if (map.TryGetValue(item.Id, out var existing) && existing.Installed) continue;
            var byExe = QueryByExeNames(item);
            if (byExe.Installed) map[item.Id] = byExe;
        }

        lock (StatusCacheLock)
            _statusCache = map;
    }

    public static CommonSoftwareStatus Query(CommonSoftwareItem item)
    {
        lock (StatusCacheLock)
        {
            if (_statusCache is not null &&
                _statusCache.TryGetValue(item.Id, out var cached))
                return cached;
        }

        if (item.IsWingetBootstrap)
            return QueryWingetStatus();

        if (!string.IsNullOrWhiteSpace(item.AppxPackageName))
        {
            var appx = QueryAppxStatus(item.AppxPackageName);
            if (appx.Installed) return appx;
        }

        foreach (var keyPath in UninstallKeyPaths())
        {
            using var baseKey = RegistryKey.OpenBaseKey(
                keyPath.Hive, RegistryView.Registry64);
            using var uninstall = baseKey.OpenSubKey(keyPath.SubKey);
            if (uninstall is null) continue;

            foreach (var subName in uninstall.GetSubKeyNames())
            {
                using var sub = uninstall.OpenSubKey(subName);
                if (sub is null) continue;
                var display = sub.GetValue("DisplayName") as string ?? "";
                if (!Matches(display, item.DetectPatterns)) continue;

                return new CommonSoftwareStatus
                {
                    Installed = true,
                    Version = sub.GetValue("DisplayVersion") as string ?? "",
                    UninstallCommand = sub.GetValue("QuietUninstallString") as string
                        ?? sub.GetValue("UninstallString") as string,
                };
            }
        }

        var byExe = QueryByExeNames(item);
        if (byExe.Installed) return byExe;

        return new CommonSoftwareStatus();
    }

    private static CommonSoftwareStatus QueryAppxStatus(string packageName)
    {
        try
        {
            var output = RunCapture("powershell.exe",
                "-NoProfile -Command \"Get-AppxPackage -Name '" +
                packageName.Replace("'", "''") +
                "*' | Select-Object -First 1 -ExpandProperty Version\"");
            output = output.Trim();
            if (output.Length == 0) return new CommonSoftwareStatus();
            return new CommonSoftwareStatus { Installed = true, Version = output };
        }
        catch
        {
            return new CommonSoftwareStatus();
        }
    }

    public static string Install(CommonSoftwareItem item, Action<SoftwareInstallProgress>? onProgress = null)
    {
        ApplyLog.Write("常用软件安装：" + item.Title);
        if (item.IsWingetBootstrap)
            return InstallWinget(onProgress);

        Report(onProgress, "准备安装 " + item.Title, 2);
        var preferOffline = PreferOfflineNow(item);

        // Server 无商店：优先离线自动安装（Appx/Msix + 依赖，与手工放包一致）
        if (item.PreferAppxSideload && !string.IsNullOrWhiteSpace(item.StoreProductId))
        {
            try
            {
                var appxMsg = InstallFromAppxSideload(item, onProgress);
                if (appxMsg is not null)
                    return appxMsg;
            }
            catch (Exception ex)
            {
                ApplyLog.Write("离线 Appx 安装失败，尝试其它方式：" + ex.Message);
                Report(onProgress, "离线 Appx 安装失败，尝试其它方式…", 35);
            }
        }

        // Server 优先 / 显式 PreferOffline：离线包（含便携 EXE）
        if (preferOffline && !string.IsNullOrWhiteSpace(item.OfflineInstallerUrl))
        {
            try
            {
                var offlineMsg = TryInstallOffline(item, onProgress);
                if (offlineMsg is not null)
                    return offlineMsg;
            }
            catch (Exception ex)
            {
                ApplyLog.Write("离线安装失败，尝试其它方式：" + ex.Message);
                Report(onProgress, "离线安装失败，尝试其它方式…", 45);
            }
        }

        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            EnsureWingetSpeedSettings();
            Report(onProgress, "正在调用 winget（静默）…", 5);
            // --silent：跳过安装包 UI；--source winget：避开较慢的 msstore；
            // 源自动更新已在设置中关闭，避免每次 install 先同步索引
            var code = RunWinget(BuildWingetInstallArgs(item.WingetId, preferWingetSource: true), onProgress);
            if (code == 0)
            {
                Report(onProgress, "安装完成", 100);
                InvalidateStatusCache();
                return "";
            }
            if (code == -1978335189) // 0x8A150013 already installed
            {
                Report(onProgress, "已安装", 100);
                InvalidateStatusCache();
                return "软件已安装或无需重复安装。";
            }

            // PreferAppx/Offline 的包跳过 msstore，避免 Server 挂死
            if (!SkipMsStoreRetry(item))
            {
                Report(onProgress, "winget 源未命中，改用默认源重试…", 8);
                code = RunWinget(BuildWingetInstallArgs(item.WingetId, preferWingetSource: false), onProgress);
                if (code == 0)
                {
                    Report(onProgress, "安装完成", 100);
                    InvalidateStatusCache();
                    return "";
                }
                if (code == -1978335189)
                {
                    Report(onProgress, "已安装", 100);
                    InvalidateStatusCache();
                    return "软件已安装或无需重复安装。";
                }
            }
        }

        if (!preferOffline && !string.IsNullOrWhiteSpace(item.OfflineInstallerUrl))
        {
            try
            {
                var offlineMsg = TryInstallOffline(item, onProgress);
                if (offlineMsg is not null)
                    return offlineMsg;
            }
            catch (Exception ex)
            {
                ApplyLog.Write("离线安装失败：" + ex.Message);
            }
        }

        try
        {
            Report(onProgress, "正在解析官网最新安装包…", 70);
            var officialMsg = TryInstallFromOfficialLatest(item, onProgress);
            if (officialMsg is not null)
                return officialMsg;
        }
        catch (Exception ex)
        {
            ApplyLog.Write("官网自动安装失败：" + ex.Message);
            Report(onProgress, "官网自动安装失败，打开下载页…", 90);
        }

        Report(onProgress, "打开官方下载页…", 95);
        OpenDownloadPage(item);
        return IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId)
            ? "自动安装未成功，已在浏览器打开官方下载页，请手动安装。"
            : string.IsNullOrWhiteSpace(item.WingetId)
                ? "未能从官网解析到安装包，已在浏览器打开下载页。"
                : "本机未检测到 winget，且官网自动安装未成功，已打开下载页。";
    }

    /// <summary>解析官网/GitHub 最新直链并静默安装。成功返回文案；无法解析或安装失败返回 null。</summary>
    private static string? TryInstallFromOfficialLatest(
        CommonSoftwareItem item,
        Action<SoftwareInstallProgress>? onProgress)
    {
        var url = OfficialInstallerResolver.TryResolve(item);
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (string.Equals(url.Trim(), item.OfflineInstallerUrl?.Trim(), StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(item.OfflineInstallerUrl))
            return null; // 已经用这条直链试过了

        ApplyLog.Write("官网最新包：" + item.Title + " → " + url);
        Report(onProgress, "已解析到最新安装包，开始下载…", 72);
        var copy = CloneForOfflineInstall(item, url);
        return TryInstallOffline(copy, onProgress);
    }

    private static CommonSoftwareItem CloneForOfflineInstall(CommonSoftwareItem item, string url)
    {
        var args = (item.OfflineInstallArgs ?? "").Trim();
        if (args.Length == 0)
            args = OfficialInstallerResolver.GuessSilentArgs(url);
        return new CommonSoftwareItem
        {
            Id = item.Id,
            Title = item.Title,
            Category = item.Category,
            WingetId = item.WingetId,
            DetectPatterns = item.DetectPatterns,
            DetectExeNames = item.DetectExeNames,
            DownloadUrl = item.DownloadUrl,
            OfflineInstallerUrl = url,
            OfflineInstallArgs = args,
            OfflinePortable = item.OfflinePortable,
        };
    }

    private static bool PreferOfflineNow(CommonSoftwareItem item) =>
        item.PreferOfflineInstall
        || (item.PreferOfflineOnServer && Optimizer.IsWindowsServer());

    private static bool SkipMsStoreRetry(CommonSoftwareItem item) =>
        item.PreferOfflineInstall
        || (item.PreferOfflineOnServer && Optimizer.IsWindowsServer())
        // Appx 旁加载仅在 Server 上跳过 msstore，桌面仍可用商店源装 Codex Desktop 等
        || (item.PreferAppxSideload && Optimizer.IsWindowsServer());

    /// <summary>离线安装入口：便携 EXE 或静默安装包。成功返回文案；失败返回 null。</summary>
    private static string? TryInstallOffline(
        CommonSoftwareItem item,
        Action<SoftwareInstallProgress>? onProgress) =>
        item.OfflinePortable
            ? InstallPortableOffline(item, onProgress)
            : InstallFromOfflinePackage(item, onProgress);

    /// <summary>
    /// 下载便携 EXE 到 %LocalAppData%\SrvDesk\tools\{id}\，并加入用户 PATH。
    /// 用于 Server 上无 winget/商店时安装 Codex CLI 等工具。
    /// </summary>
    private static string? InstallPortableOffline(
        CommonSoftwareItem item,
        Action<SoftwareInstallProgress>? onProgress)
    {
        var url = item.OfflineInstallerUrl.Trim();
        if (url.Length == 0) return null;

        var exeName = ResolvePortableExeName(item);
        var toolDir = Path.Combine(ToolsDir, item.Id);
        Directory.CreateDirectory(toolDir);
        var dest = Path.Combine(toolDir, exeName);

        Report(onProgress, "下载便携程序…", 5);
        DownloadInstaller(url, dest, minBytes: 100_000, onProgress, percentBase: 5, percentSpan: 70);

        EnsureUserPathContains(toolDir);
        PrependProcessPath(toolDir);

        InvalidateStatusCache();
        Report(onProgress, "安装完成", 100);
        ApplyLog.Write("便携安装：" + dest);
        return "已安装到 " + dest + "，并已加入用户 PATH。请新开终端后使用 "
            + Path.GetFileNameWithoutExtension(exeName) + "。";
    }

    private static string ResolvePortableExeName(CommonSoftwareItem item)
    {
        if (item.DetectExeNames.Length > 0)
        {
            var name = item.DetectExeNames[0].Trim();
            if (name.Length > 0)
            {
                return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? name
                    : name + ".exe";
            }
        }

        return item.Id + ".exe";
    }

    private static CommonSoftwareStatus QueryByExeNames(CommonSoftwareItem item)
    {
        if (item.DetectExeNames.Length == 0)
            return new CommonSoftwareStatus();

        foreach (var raw in item.DetectExeNames)
        {
            var name = raw.Trim();
            if (name.Length == 0) continue;
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                name += ".exe";

            var inTools = Path.Combine(ToolsDir, item.Id, name);
            if (File.Exists(inTools))
            {
                return new CommonSoftwareStatus
                {
                    Installed = true,
                    UninstallCommand = "portable:" + inTools,
                };
            }

            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                var trimmed = dir.Trim();
                if (trimmed.Length == 0) continue;
                var candidate = Path.Combine(trimmed, name);
                if (!File.Exists(candidate)) continue;
                return new CommonSoftwareStatus
                {
                    Installed = true,
                    UninstallCommand = "portable:" + candidate,
                };
            }

            try
            {
                var whereHit = RunCapture("where.exe", name)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().Trim('"'))
                    .FirstOrDefault(File.Exists);
                if (!string.IsNullOrWhiteSpace(whereHit))
                {
                    return new CommonSoftwareStatus
                    {
                        Installed = true,
                        UninstallCommand = "portable:" + whereHit,
                    };
                }
            }
            catch { /* ignore */ }
        }

        return new CommonSoftwareStatus();
    }

    private static void EnsureUserPathContains(string dir)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey("Environment", writable: true)
                ?? Registry.CurrentUser.CreateSubKey("Environment");
            if (key is null) return;

            var current = key.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
            var parts = current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
            if (parts.Any(p => string.Equals(p, dir, StringComparison.OrdinalIgnoreCase)))
                return;

            parts.Add(dir);
            key.SetValue("Path", string.Join(";", parts), RegistryValueKind.ExpandString);
            ApplyLog.Write("已写入用户 PATH：" + dir);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("写入用户 PATH 失败：" + ex.Message);
        }
    }

    private static void RemoveUserPathEntry(string dir)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey("Environment", writable: true);
            if (key is null) return;
            var current = key.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
            var parts = current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0
                    && !string.Equals(p, dir, StringComparison.OrdinalIgnoreCase))
                .ToList();
            key.SetValue("Path", string.Join(";", parts), RegistryValueKind.ExpandString);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("移除用户 PATH 失败：" + ex.Message);
        }
    }

    private static void PrependProcessPath(string dir)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var part in path.Split(';'))
        {
            if (string.Equals(part.Trim(), dir, StringComparison.OrdinalIgnoreCase))
                return;
        }
        Environment.SetEnvironmentVariable("PATH", dir + ";" + path);
    }

    /// <summary>下载并静默运行离线安装包。成功返回提示文案（空串=成功）；失败返回 null 以便上层回退。</summary>
    private static string? InstallFromOfflinePackage(
        CommonSoftwareItem item,
        Action<SoftwareInstallProgress>? onProgress)
    {
        var url = item.OfflineInstallerUrl.Trim();
        if (url.Length == 0) return null;

        Directory.CreateDirectory(DownloadDir);
        Uri uri;
        try { uri = new Uri(url); }
        catch { return null; }
        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName)
            || !OfficialInstallerResolver.HasInstallerExtension(fileName))
            fileName = item.Id + (url.IndexOf(".msi", StringComparison.OrdinalIgnoreCase) >= 0 ? "-setup.msi" : "-setup.exe");
        var dest = Path.Combine(DownloadDir, fileName);

        Report(onProgress, "下载离线安装包…", 5);
        DownloadInstaller(url, dest, minBytes: 80_000, onProgress, percentBase: 5, percentSpan: 55);

        var args = string.IsNullOrWhiteSpace(item.OfflineInstallArgs)
            ? OfficialInstallerResolver.GuessSilentArgs(url)
            : item.OfflineInstallArgs.Trim();
        var attempts = BuildSilentArgAttempts(dest, args);
        Report(onProgress, "正在静默安装离线包…", 65);
        ApplyLog.Write("离线安装：" + dest + " " + args);

        var code = -1;
        foreach (var one in attempts)
        {
            code = RunInstaller(dest, one);
            InvalidateStatusCache();
            if (code == 0) break;
            var mid = Query(item);
            if (mid.Installed) break;
        }
        InvalidateStatusCache();
        if (code == 0)
        {
            Report(onProgress, "安装完成", 100);
            return "";
        }

        // 部分安装程序用非 0 表示需重启等；若已能检测到则视为成功
        var status = Query(item);
        if (status.Installed)
        {
            Report(onProgress, "安装完成（需按提示重启时请自行安排）", 100);
            return code != 0 ? "安装已完成；退出码 " + code + "（如提示重启请自行安排）。" : "";
        }

        ApplyLog.Write("离线安装退出码：" + code);
        return null;
    }

    private static List<string> BuildSilentArgAttempts(string dest, string primary)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(primary)) list.Add(primary);
        var msi = dest.EndsWith(".msi", StringComparison.OrdinalIgnoreCase);
        if (msi)
        {
            if (!list.Contains("/qn /norestart")) list.Add("/qn /norestart");
            return list;
        }

        if (list.All(a => a.IndexOf("VERYSILENT", StringComparison.OrdinalIgnoreCase) < 0))
            list.Add("/VERYSILENT /SUPPRESSMSGBOXES /NORESTART");
        if (list.All(a => a != "/S"))
            list.Add("/S");
        return list;
    }

    private static int RunInstaller(string dest, string args)
    {
        if (dest.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            return Run("msiexec.exe", "/i \"" + dest + "\" " + args, timeoutMs: 600_000);
        return Run(dest, args);
    }

    /// <summary>
    /// 商店 Appx/Msix 旁加载：与手工安装的 AppleInc.iCloud_*.Appx + VCLibs/WindowsAppRuntime 一致。
    /// 成功返回提示；失败返回 null 以便回退 EXE/winget。
    /// </summary>
    private static string? InstallFromAppxSideload(
        CommonSoftwareItem item,
        Action<SoftwareInstallProgress>? onProgress)
    {
        var productId = item.StoreProductId.Trim();
        var packageName = string.IsNullOrWhiteSpace(item.AppxPackageName)
            ? item.Id
            : item.AppxPackageName.Trim();
        if (productId.Length == 0) return null;

        EnsureAppxSideloadAllowed();
        var dir = Path.Combine(DownloadDir, item.Id + "-appx");
        Directory.CreateDirectory(dir);

        Report(onProgress, "解析商店 Appx 直链（离线自动安装）…", 8);
        var files = ResolveStoreAppxFiles(productId, packageName);
        if (files.Count == 0)
        {
            // 复用已下载到目录的包（与用户手工放置的文件兼容）
            files = DiscoverLocalAppxFiles(dir, packageName);
        }

        if (files.Count == 0)
            throw new InvalidOperationException("未能解析到 Appx 安装包，请检查网络或将 .Appx/.Msix 放入：" + dir);

        Report(onProgress, $"准备下载 {files.Count} 个包…", 12);
        var localPaths = new List<string>();
        for (var i = 0; i < files.Count; i++)
        {
            var f = files[i];
            var dest = Path.Combine(dir, f.FileName);
            var basePct = 12 + (int)(i / (double)files.Count * 55);
            var span = Math.Max(5, 55 / files.Count);
            if (!string.IsNullOrWhiteSpace(f.Url))
            {
                Report(onProgress, "下载 " + f.FileName, basePct);
                DownloadPackage(f.Url!, dest, minBytes: 50_000, onProgress, basePct, span);
            }
            else if (!File.Exists(dest))
            {
                continue;
            }

            if (File.Exists(dest) && LooksLikeBinaryPackage(dest))
                localPaths.Add(dest);
        }

        if (localPaths.Count == 0)
            throw new InvalidOperationException("Appx 下载失败或目录为空：" + dir);

        var main = localPaths.FirstOrDefault(p =>
            Path.GetFileName(p).StartsWith(packageName, StringComparison.OrdinalIgnoreCase));
        if (main is null)
            throw new InvalidOperationException("未找到主包 " + packageName + "_*.Appx，目录：" + dir);

        var deps = localPaths
            .Where(p => !p.Equals(main, StringComparison.OrdinalIgnoreCase))
            .OrderBy(AppxInstallOrder)
            .ToList();

        Report(onProgress, "正在离线安装 Appx（含依赖）…", 72);
        ApplyLog.Write("离线 Appx 安装：" + main + " deps=" + deps.Count);
        AddAppxPackageWithDependencies(main, deps);

        InvalidateStatusCache();
        var status = Query(item);
        if (status.Installed)
        {
            Report(onProgress, "安装完成", 100);
            return "已通过离线自动安装完成（无需微软商店）。";
        }

        // 安装命令成功但检测延迟时仍视为完成
        Report(onProgress, "安装命令已执行", 100);
        return "离线自动安装命令已执行。若列表未刷新，请点刷新。";
    }

    private static int AppxInstallOrder(string path)
    {
        var name = Path.GetFileName(path);
        if (name.StartsWith("Microsoft.VCLibs.140.00.UWPDesktop", StringComparison.OrdinalIgnoreCase)) return 2;
        if (name.StartsWith("Microsoft.VCLibs", StringComparison.OrdinalIgnoreCase)) return 1;
        if (name.IndexOf("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase) >= 0) return 3;
        if (name.IndexOf("UI.Xaml", StringComparison.OrdinalIgnoreCase) >= 0) return 4;
        return 10;
    }

    private static void EnsureAppxSideloadAllowed()
    {
        try
        {
            using var k = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            k?.SetValue("AllowAllTrustedApps", 1, RegistryValueKind.DWord);
            k?.SetValue("AllowDevelopmentWithoutDevLicense", 1, RegistryValueKind.DWord);
        }
        catch
        {
            /* 非管理员时忽略，后续安装可能仍成功 */
        }
    }

    private sealed class StoreAppxFile
    {
        public string FileName { get; set; } = "";
        public string? Url { get; set; }
        public Version? Version { get; set; }
    }

    /// <summary>通过 store.rg-adguard 解析商店包直链（需先访问首页拿 Cookie）。</summary>
    private static List<StoreAppxFile> ResolveStoreAppxFiles(string productId, string mainPackageName)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        const string ua =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";

        var cookies = new CookieContainer();
        var handler = new System.Net.Http.HttpClientHandler
        {
            CookieContainer = cookies,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        using var client = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromMinutes(2) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);

        client.GetAsync("https://store.rg-adguard.net/").GetAwaiter().GetResult().Dispose();

        using var form = new System.Net.Http.FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["type"] = "ProductId",
            ["url"] = productId,
            ["ring"] = "Retail",
        });
        using var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post,
            "https://store.rg-adguard.net/api/GetFiles")
        {
            Content = form,
        };
        req.Headers.Referrer = new Uri("https://store.rg-adguard.net/");
        req.Headers.TryAddWithoutValidation("Origin", "https://store.rg-adguard.net");

        using var resp = client.SendAsync(req).GetAwaiter().GetResult();
        var html = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!resp.IsSuccessStatusCode || html.IndexOf(mainPackageName, StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("解析商店直链失败（HTTP " + (int)resp.StatusCode + "）。");

        var all = new List<StoreAppxFile>();
        var re = new Regex(
            @"href\s*=\s*""(?<url>https?://[^""]+)""[^>]*>\s*(?<name>[^<]+\.(?:appx|msix|appxbundle|msixbundle))\s*<",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (Match m in re.Matches(html))
        {
            var name = m.Groups["name"].Value.Trim();
            var url = m.Groups["url"].Value.Trim();
            if (name.IndexOf(".eappx", StringComparison.OrdinalIgnoreCase) >= 0) continue; // 加密包跳过
            if (name.IndexOf("_arm64_", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (name.IndexOf("_x86_", StringComparison.OrdinalIgnoreCase) >= 0 &&
                name.IndexOf(mainPackageName, StringComparison.OrdinalIgnoreCase) >= 0)
                continue; // 主包只要 x64

            all.Add(new StoreAppxFile
            {
                FileName = name,
                Url = url,
                Version = ParseVersionFromAppxName(name),
            });
        }

        return SelectLatestAppxSet(all, mainPackageName);
    }

    private static List<StoreAppxFile> DiscoverLocalAppxFiles(string dir, string mainPackageName)
    {
        if (!Directory.Exists(dir)) return [];
        var all = Directory.EnumerateFiles(dir)
            .Where(p =>
            {
                var ext = Path.GetExtension(p);
                return ext.Equals(".appx", StringComparison.OrdinalIgnoreCase)
                       || ext.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                       || ext.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase)
                       || ext.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase);
            })
            .Select(p => new StoreAppxFile
            {
                FileName = Path.GetFileName(p),
                Url = null,
                Version = ParseVersionFromAppxName(Path.GetFileName(p)),
            })
            .ToList();
        return SelectLatestAppxSet(all, mainPackageName);
    }

    private static List<StoreAppxFile> SelectLatestAppxSet(List<StoreAppxFile> all, string mainPackageName)
    {
        if (all.Count == 0) return [];

        // 依赖：每个包族取最新 x64/neutral
        string FamilyKey(string name)
        {
            var idx = name.IndexOf('_');
            return idx > 0 ? name.Substring(0, idx) : name;
        }

        bool IsUsefulDep(string name) =>
            name.StartsWith("Microsoft.VCLibs", StringComparison.OrdinalIgnoreCase)
            || name.IndexOf("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("UI.Xaml", StringComparison.OrdinalIgnoreCase) >= 0
            || name.StartsWith(mainPackageName, StringComparison.OrdinalIgnoreCase);

        var picked = new List<StoreAppxFile>();
        foreach (var group in all.Where(f => IsUsefulDep(f.FileName)).GroupBy(f => FamilyKey(f.FileName), StringComparer.OrdinalIgnoreCase))
        {
            var best = group
                .OrderByDescending(f => f.FileName.IndexOf("_x64_", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenByDescending(f => f.Version ?? new Version(0, 0))
                .ThenByDescending(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                .First();
            picked.Add(best);
        }

        if (!picked.Any(f => f.FileName.StartsWith(mainPackageName, StringComparison.OrdinalIgnoreCase)))
            return [];

        return picked;
    }

    private static Version? ParseVersionFromAppxName(string name)
    {
        var m = Regex.Match(name, @"_(\d+\.\d+\.\d+\.\d+)_");
        if (!m.Success) return null;
        return Version.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    private static void AddAppxPackageWithDependencies(string mainPath, List<string> dependencyPaths)
    {
        var mainEsc = mainPath.Replace("'", "''");
        string script;
        if (dependencyPaths.Count == 0)
        {
            script = $@"
$ErrorActionPreference = 'Stop'
Add-AppxPackage -Path '{mainEsc}'
";
        }
        else
        {
            var deps = string.Join(",", dependencyPaths.Select(p => "'" + p.Replace("'", "''") + "'"));
            script = $@"
$ErrorActionPreference = 'Stop'
$deps = @({deps})
Add-AppxPackage -Path '{mainEsc}' -DependencyPath $deps -ErrorAction Stop
";
        }

        var code = RunPowerShell(script, timeoutMs: 600_000);
        if (code != 0 && !IsAlreadyInstalledAppxOutput(LastPowerShellOutput))
            throw new InvalidOperationException(FormatAppxFailure("Add-AppxPackage", code, mainPath));
    }

    public static string Uninstall(CommonSoftwareItem item, Action<SoftwareInstallProgress>? onProgress = null)
    {
        ApplyLog.Write("常用软件卸载：" + item.Title);
        if (item.IsWingetBootstrap)
            return "winget（应用安装程序）为系统组件，不建议在此卸载。请在「设置 → 应用」中操作。";

        Report(onProgress, "准备卸载 " + item.Title, 5);

        if (item.OfflinePortable || item.DetectExeNames.Length > 0)
        {
            var toolDir = Path.Combine(ToolsDir, item.Id);
            if (Directory.Exists(toolDir))
            {
                try
                {
                    Report(onProgress, "移除便携目录…", 40);
                    Directory.Delete(toolDir, recursive: true);
                    RemoveUserPathEntry(toolDir);
                    Report(onProgress, "卸载完成", 100);
                    InvalidateStatusCache();
                    return "";
                }
                catch (Exception ex)
                {
                    ApplyLog.Write("便携卸载失败：" + ex.Message);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(item.AppxPackageName))
        {
            try
            {
                var name = item.AppxPackageName.Replace("'", "''");
                var code = RunPowerShell($@"
$ErrorActionPreference = 'Stop'
Get-AppxPackage -Name '{name}*' | Remove-AppxPackage
", timeoutMs: 180_000);
                if (code == 0)
                {
                    Report(onProgress, "卸载完成", 100);
                    InvalidateStatusCache();
                    return "";
                }
            }
            catch (Exception ex)
            {
                ApplyLog.Write("Appx 卸载失败：" + ex.Message);
            }
        }

        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            var code = RunWinget($"uninstall -e --id {item.WingetId} --disable-interactivity", onProgress);
            if (code == 0)
            {
                Report(onProgress, "卸载完成", 100);
                InvalidateStatusCache();
                return "";
            }
        }

        var status = Query(item);
        var cmd = status.UninstallCommand;
        if (!string.IsNullOrWhiteSpace(cmd) && cmd!.StartsWith("portable:", StringComparison.OrdinalIgnoreCase))
        {
            var path = cmd.Substring("portable:".Length);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) &&
                    dir.StartsWith(ToolsDir, StringComparison.OrdinalIgnoreCase))
                {
                    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                    RemoveUserPathEntry(dir!);
                }
                Report(onProgress, "卸载完成", 100);
                InvalidateStatusCache();
                return "";
            }
            catch (Exception ex)
            {
                return "便携程序卸载失败：" + ex.Message;
            }
        }

        if (string.IsNullOrWhiteSpace(cmd))
            return "未找到可用的卸载命令。请在「设置 → 应用」中手动卸载。";

        Report(onProgress, "执行卸载命令…", 40);
        RunShell(cmd!);
        Report(onProgress, "卸载命令已执行", 100);
        InvalidateStatusCache();
        return "";
    }

    private enum WingetOsLane
    {
        Unsupported,
        Repair,
        Provisioned,
        Portable,
    }

    public static string InstallWinget(Action<SoftwareInstallProgress>? onProgress = null)
    {
        if (IsWingetAvailable())
        {
            Report(onProgress, "winget 已可用", 100);
            return "";
        }

        var lane = DetectWingetOsLane();
        var isServer = Optimizer.IsWindowsServer();
        ApplyLog.Write("安装/修复 winget（对齐 asheroto/winget-install）[" + lane + "]");
        if (lane == WingetOsLane.Unsupported)
            return "当前系统不支持 winget（需要 Windows 10 1809+ 或 Windows Server 2019+）。";

        ResetWingetDiscovery();
        var notes = new List<string>();

        var vcNote = EnsureVcRedistX64(onProgress);
        if (vcNote.Length > 0) notes.Add(vcNote);
        ResetWingetDiscovery();
        if (IsWingetAvailable())
        {
            Report(onProgress, "winget 已就绪", 100);
            return FormatWingetReady(notes);
        }

        if (IsAppInstallerPackagePresent())
        {
            Report(onProgress, "注册 App Installer 并修正 PATH…", 12);
            var register = TryRegisterAppInstaller();
            if (register.Length > 0) notes.Add(register);
            ApplyWingetPathAndPermissions(notes);
            ResetWingetDiscovery();
            if (TryBindWingetFromAppx() ||
                (lane != WingetOsLane.Repair && TryStagePortableWinget(onProgress, notes)))
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }
        }

        TryCloseWingetLockingProcesses();

        if (lane == WingetOsLane.Repair)
        {
            Report(onProgress, "Repair-WinGetPackageManager（微软官方修复，约 1–2 分钟）…", 18);
            var repair = TryRepairWinGetPackageManager(onProgress);
            if (repair.Length > 0) notes.Add(repair);
            AddWindowsAppsUserPath();
            TryRegisterAppInstaller();
            ApplyWingetPathAndPermissions(notes);
            ResetWingetDiscovery();
            if (IsWingetAvailable() || TryBindWingetFromAppx())
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }

            notes.Add("主路径未就绪，改用离线预配（与 asheroto -AlternateInstallMethod 相同）。");
            lane = WingetOsLane.Provisioned;
        }

        if (lane == WingetOsLane.Provisioned)
        {
            Report(onProgress, "离线预配：依赖 + License1.xml + App Installer…", 22);
            var alt = TryAsherotoProvisionedInstall(onProgress);
            if (alt.Length > 0) notes.Add(alt);
            ApplyWingetPathAndPermissions(notes);
            System.Threading.Thread.Sleep(3000);
            ResetWingetDiscovery();
            if (IsWingetAvailable() || TryBindWingetFromAppx())
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }
        }

        if (lane == WingetOsLane.Portable || isServer)
        {
            Report(onProgress, "部署便携 winget…", 70);
            if (TryStagePortableWinget(onProgress, notes))
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }
        }

        Report(onProgress, "回退：多镜像下载 App Installer…", 75);
        var bootstrap = TryBootstrapWingetPackages(
            preferProvisioned: isServer || lane == WingetOsLane.Provisioned,
            onProgress);
        if (bootstrap.Length > 0) notes.Add(bootstrap);
        ApplyWingetPathAndPermissions(notes);
        ResetWingetDiscovery();
        if (IsWingetAvailable() || TryBindWingetFromAppx() ||
            (isServer && TryStagePortableWinget(onProgress, notes)))
        {
            Report(onProgress, "winget 已就绪", 100);
            return FormatWingetReady(notes);
        }

        Report(onProgress, "注册别名…", 90);
        var registerAgain = TryRegisterAppInstaller();
        if (registerAgain.Length > 0) notes.Add(registerAgain);
        ApplyWingetPathAndPermissions(notes);
        ResetWingetDiscovery();
        if (IsWingetAvailable() || TryBindWingetFromAppx() ||
            (isServer && TryStagePortableWinget(onProgress, notes)))
        {
            Report(onProgress, "winget 已就绪", 100);
            return FormatWingetReady(notes);
        }

        try
        {
            Directory.CreateDirectory(DownloadDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = DownloadDir,
                UseShellExecute = true,
            });
        }
        catch { /* ignore */ }

        var hasBundle = Directory.EnumerateFiles(DownloadDir, "*DesktopAppInstaller*.msixbundle")
            .Any(f => new FileInfo(f).Length > 1_000_000);
        var appxPresent = IsAppInstallerPackagePresent();
        var appxPath = TryGetAppxWingetPath();

        notes.Add("自动修复未完成。");
        if (appxPresent || !string.IsNullOrWhiteSpace(appxPath))
        {
            notes.Add("已检测到 App Installer 包，但未能启动 winget.exe。");
            if (!IsVcRuntime140Present())
                notes.Add("本机缺少 VCRUNTIME140.dll，请安装 Visual C++ 2015-2022 x64：https://aka.ms/vs/17/release/vc_redist.x64.exe");
            if (isServer)
            {
                notes.Add("Server 常见原因：管理员进程调用 WindowsApps 别名报「找不到适用的应用许可证」。");
                notes.Add("本程序会尝试部署便携目录：" + PortableWingetDir);
                notes.Add("也可手动把 WindowsApps\\Microsoft.DesktopAppInstaller_* 目录内容复制到该路径。");
            }
            else
            {
                notes.Add("请再试：设置 → 应用 → 应用执行别名 → 打开 winget.exe；或注销后重开本程序。");
            }
            if (!string.IsNullOrWhiteSpace(appxPath))
                notes.Add("检测到路径：" + appxPath);
        }
        else if (hasBundle)
        {
            notes.Add("下载目录里已有安装包，但安装后仍不可用。可删除目录内损坏文件后重试，或手动 Add-AppxPackage。");
            notes.Add("目录：" + DownloadDir);
        }
        else
        {
            notes.Add("程序自动下载失败（浏览器能打开≠进程能下载：常见于管理员进程未走系统代理）。");
            notes.Add("请在浏览器打开：https://github.com/microsoft/winget-cli/releases/latest");
            notes.Add("下载并拷到：" + DownloadDir);
            notes.Add("  · Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle");
            notes.Add("  · DesktopAppInstaller_Dependencies.zip（解压后把 x64 下 .appx 放入同目录）");
            notes.Add("然后再次点击「一键安装 winget」。");
        }

        notes.Add("也可在「设置 → 应用 → 应用执行别名」中开启 winget.exe。");
        Report(onProgress, "自动修复未完成", 100);
        return string.Join("\r\n", notes);
    }

    /// <summary>从 AppX 安装目录绑定 winget 完整路径（不依赖 WindowsApps 别名）。</summary>
    private static bool TryBindWingetFromAppx()
    {
        var direct = TryGetAppxWingetPath();
        if (string.IsNullOrWhiteSpace(direct)) return false;
        if (!ProbeWinget(direct!)) return false;
        _wingetPath = direct;
        _wingetDiscoveryDone = true;
        ApplyLog.Write("已绑定 AppX winget：" + direct);
        return true;
    }

    public static void OpenDownloadPage(CommonSoftwareItem item)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = item.DownloadUrl,
            UseShellExecute = true,
        });
        ApplyLog.Write("打开下载页：" + item.Title);
    }

    public static void ClearDownloadCache()
    {
        if (!Directory.Exists(DownloadDir)) return;
        Directory.Delete(DownloadDir, recursive: true);
        ApplyLog.Write("已删除常用软件下载临时文件");
    }

    private static List<SoftwareUpdateInfo> _lastUpdates = [];

    public static IReadOnlyList<SoftwareUpdateInfo> LastUpdateCheck => _lastUpdates;

    public static SoftwareUpdateInfo? FindPendingUpdate(string itemId) =>
        _lastUpdates.FirstOrDefault(u => u.Item.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));

    /// <summary>检查常用软件目录中可更新的项（含当前版本 / 新版本）。</summary>
    public static List<SoftwareUpdateInfo> ListAvailableUpdates(
        IReadOnlyList<CommonSoftwareItem> items,
        Action<SoftwareInstallProgress>? onProgress = null)
    {
        _lastUpdates = [];
        if (!IsWingetAvailable())
            throw new InvalidOperationException("未检测到 winget，请先在必备列表中安装/修复 winget。");

        EnsureWingetSpeedSettings();
        Report(onProgress, "正在查询可更新软件…", 10);
        var output = RunCaptureWinget(
            "upgrade --include-unknown --disable-interactivity --accept-source-agreements");
        if (output.IndexOf("No applicable update", StringComparison.OrdinalIgnoreCase) < 0 &&
            output.IndexOf("没有适用的升级", StringComparison.Ordinal) < 0 &&
            output.Trim().Length < 20)
        {
            // 部分环境 upgrade 列表为空时再试 list
            Report(onProgress, "换用 list 再查一次…", 40);
            output += "\r\n" + RunCaptureWinget(
                "list --upgrade-available --disable-interactivity --accept-source-agreements");
        }

        Report(onProgress, "解析更新列表…", 70);
        var result = ParseUpgradeList(output, items);
        _lastUpdates = result;
        Report(onProgress, result.Count > 0 ? $"发现 {result.Count} 款可更新" : "没有可用更新", 100);
        ApplyLog.Write($"检查软件更新：目录内可更新 {result.Count} 款");
        return result;
    }

    public static string Upgrade(CommonSoftwareItem item, Action<SoftwareInstallProgress>? onProgress = null)
    {
        if (item.IsWingetBootstrap)
            return "winget 自身请使用「修复安装」。";
        if (!IsWingetAvailable() || string.IsNullOrWhiteSpace(item.WingetId))
        {
            try
            {
                Report(onProgress, "无 winget 包，改从官网下载最新安装包…", 10);
                var officialOnly = TryInstallFromOfficialLatest(item, onProgress);
                if (officialOnly is not null)
                    return officialOnly.Length == 0 ? "" : officialOnly;
            }
            catch (Exception ex)
            {
                ApplyLog.Write("官网更新失败：" + ex.Message);
            }
            return "无法更新：本机无 winget 或未能从官网解析到安装包。";
        }

        EnsureWingetSpeedSettings();
        Report(onProgress, "正在更新 " + item.Title, 5);
        var code = RunWinget(
            $"upgrade -e --id {item.WingetId} --include-unknown --silent " +
            "--accept-package-agreements --accept-source-agreements --disable-interactivity",
            onProgress);
        if (code == 0)
        {
            Report(onProgress, "更新完成", 100);
            InvalidateStatusCache();
            _lastUpdates.RemoveAll(u => u.Item.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            return "";
        }

        // 常见：已是最新 / 无可升级
        if (code is -1978335189 or -1978335212 or -1978335135)
        {
            Report(onProgress, "已是最新", 100);
            InvalidateStatusCache();
            _lastUpdates.RemoveAll(u => u.Item.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            return "已是最新版本或不需要更新。";
        }

        try
        {
            Report(onProgress, "winget 更新未成功，改从官网下载最新包…", 40);
            var official = TryInstallFromOfficialLatest(item, onProgress);
            if (official is not null)
            {
                _lastUpdates.RemoveAll(u => u.Item.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
                return official.Length == 0 ? "" : official;
            }
        }
        catch (Exception ex)
        {
            ApplyLog.Write("官网更新失败：" + ex.Message);
        }

        return "更新未成功（winget 退出码 " + code + "）。";
    }

    private static List<SoftwareUpdateInfo> ParseUpgradeList(string output, IReadOnlyList<CommonSoftwareItem> items)
    {
        var catalog = items
            .Where(i => !i.IsWingetBootstrap && !string.IsNullOrWhiteSpace(i.WingetId))
            .ToList();
        var found = new Dictionary<string, SoftwareUpdateInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length < 8) continue;
            if (line.StartsWith("-", StringComparison.Ordinal) || line.StartsWith("─", StringComparison.Ordinal))
                continue;
            if (line.IndexOf("Version", StringComparison.OrdinalIgnoreCase) >= 0 &&
                line.IndexOf("Available", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            if (line.IndexOf("版本", StringComparison.Ordinal) >= 0 &&
                line.IndexOf("可用", StringComparison.Ordinal) >= 0)
                continue;

            foreach (var item in catalog)
            {
                var id = item.WingetId;
                var idx = line.IndexOf(id, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;

                // Id 后应为：当前版本  可用版本  [源]
                var after = line.Substring(idx + id.Length).Trim();
                after = Regex.Replace(after, @"\s+", " ");
                var parts = after.Split(' ');
                if (parts.Length < 2) continue;

                var current = parts[0].Trim();
                var available = parts[1].Trim();
                if (available.Equals("winget", StringComparison.OrdinalIgnoreCase) ||
                    available.Equals("msstore", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(current, available, StringComparison.OrdinalIgnoreCase))
                    continue;

                found[item.Id] = new SoftwareUpdateInfo
                {
                    Item = item,
                    CurrentVersion = current,
                    AvailableVersion = available,
                };
            }
        }

        return found.Values.OrderBy(u => u.Item.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static CommonSoftwareStatus QueryWingetStatus()
    {
        if (!IsWingetAvailable())
        {
            // 包已装但别名坏了：再尝试直接定位 WindowsApps 内 winget.exe
            var direct = TryGetAppxWingetPath();
            if (!string.IsNullOrWhiteSpace(direct) && ProbeWinget(direct!))
            {
                _wingetPath = direct;
                _wingetDiscoveryDone = true;
                var ver = CachedWingetVersion();
                return new CommonSoftwareStatus
                {
                    Installed = true,
                    Version = ver.Length > 0 ? "v" + ver : "已就绪",
                };
            }

            if (IsAppInstallerPackagePresent())
            {
                return new CommonSoftwareStatus
                {
                    Installed = false,
                    Version = "需修复 winget（点「修复安装」）",
                };
            }
            return new CommonSoftwareStatus();
        }

        var version = CachedWingetVersion();
        return new CommonSoftwareStatus
        {
            Installed = true,
            Version = version.Length > 0 ? "v" + version : "已就绪",
        };
    }

    private static string CachedWingetVersion()
    {
        if (!string.IsNullOrWhiteSpace(_wingetVersionCache))
            return _wingetVersionCache!;

        var version = RunCaptureWinget("--version").Trim();
        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase) && version.Length > 1)
            version = version.Substring(1).Trim();
        _wingetVersionCache = version;
        return version;
    }

    private static bool IsAppInstallerPackagePresent()
    {
        try
        {
            var output = RunCapture("powershell.exe",
                "-NoProfile -Command \"@(Get-AppxPackage -AllUsers -Name Microsoft.DesktopAppInstaller | Select-Object -First 1 -ExpandProperty Name); @(Get-AppxPackage -Name Microsoft.DesktopAppInstaller | Select-Object -First 1 -ExpandProperty Name)\"");
            return output.IndexOf("DesktopAppInstaller", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static WingetOsLane DetectWingetOsLane()
    {
        var type = NtCurrentVersion("InstallationType");
        var releaseId = ParseInt(NtCurrentVersion("ReleaseId"));
        var build = ParseInt(NtCurrentVersion("CurrentBuildNumber"));
        if (build <= 0) build = ParseInt(NtCurrentVersion("CurrentBuild"));

        if (Optimizer.IsWindowsServer())
        {
            if (type.Equals("Server Core", StringComparison.OrdinalIgnoreCase))
                return WingetOsLane.Portable;
            if (build > 0 && build < 17763)
                return WingetOsLane.Unsupported;
            // asheroto：2019/2022 走预配+许可证；2025+ 走 Repair
            return build >= 26000 ? WingetOsLane.Repair : WingetOsLane.Provisioned;
        }

        if (releaseId > 0 && releaseId < 1809 && build < 17763)
            return WingetOsLane.Unsupported;
        return WingetOsLane.Repair;
    }

    private static string NtCurrentVersion(string name)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return k?.GetValue(name) as string ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static int ParseInt(string s) =>
        int.TryParse(s, out var n) ? n : 0;

    private static string TryRepairWinGetPackageManager(Action<SoftwareInstallProgress>? onProgress = null)
    {
        // 对齐 asheroto：NuGet + Microsoft.WinGet.Client + Repair-WinGetPackageManager -AllUsers -Force -Latest
        const string script = @"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$ConfirmPreference = 'None'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
try {
  Get-PackageProvider -Name NuGet -ErrorAction Stop | Out-Null
} catch {
  Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -ForceBootstrap | Out-Null
}
if (-not (Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue)) {
  Register-PSRepository -Default -ErrorAction SilentlyContinue
}
Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
Install-Module Microsoft.WinGet.Client -Force -AllowClobber -Repository PSGallery -Confirm:$false -ErrorAction SilentlyContinue
Import-Module Microsoft.WinGet.Client -Force
try {
  Repair-WinGetPackageManager -AllUsers -Force -Latest
} catch {
  Repair-WinGetPackageManager -AllUsers -Force
}
";
        try
        {
            Report(onProgress, "WinGet 模块修复中（最多 3 分钟）…", 20);
            var code = RunPowerShell(script, timeoutMs: 180_000);
            if (code == -2)
                return "Microsoft.WinGet.Client 修复超时已跳过。";
            return code == 0
                ? "已通过 Repair-WinGetPackageManager 安装/修复 winget。"
                : "Repair-WinGetPackageManager 返回码 " + code +
                  (LastPowerShellOutput.Length > 0 ? "：" + TrimOutput(LastPowerShellOutput, 160) : "");
        }
        catch (Exception ex)
        {
            return "Repair-WinGetPackageManager 失败：" + ex.Message;
        }
    }

    /// <summary>asheroto -AlternateInstallMethod：依赖 zip 中的 x64.appx + License1.xml + Add-AppxProvisionedPackage -LicensePath。</summary>
    private static string TryAsherotoProvisionedInstall(Action<SoftwareInstallProgress>? onProgress)
    {
        Directory.CreateDirectory(DownloadDir);
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        EnsureAppxSideloadAllowed();
        TryCloseWingetLockingProcesses();

        try
        {
            var depsZip = Path.Combine(DownloadDir, "DesktopAppInstaller_Dependencies.zip");
            Report(onProgress, "下载 DesktopAppInstaller_Dependencies.zip…", 24);
            var depUrl = GetWingetCliAssetUrl("DesktopAppInstaller_Dependencies.zip");
            var depUrls = string.IsNullOrWhiteSpace(depUrl)
                ? WingetDependencyZipMirrors()
                : WithGithubMirrors(depUrl!).ToArray();
            DownloadFromMirrors(depsZip, 1_000_000, onProgress, 24, 12, requireZipMagic: true, depUrls);
            TryExtractWingetDependencies(depsZip, DownloadDir, onProgress);

            var extractDir = Path.Combine(DownloadDir, "winget-deps");
            var archAppx = new List<string>();
            if (Directory.Exists(extractDir))
            {
                foreach (var file in Directory.EnumerateFiles(extractDir, "*.appx", SearchOption.AllDirectories))
                {
                    var n = Path.GetFileName(file);
                    if (n.IndexOf("x64", StringComparison.OrdinalIgnoreCase) < 0 &&
                        n.IndexOf("neutral", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (n.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        n.IndexOf("x64", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (LooksLikeBinaryPackage(file))
                        archAppx.Add(file);
                }
            }

            foreach (var extra in new[]
                     {
                         Path.Combine(DownloadDir, "Microsoft.VCLibs.x64.14.00.Desktop.appx"),
                         Path.Combine(DownloadDir, "Microsoft.UI.Xaml.2.8.x64.appx"),
                     })
            {
                if (File.Exists(extra) && LooksLikeBinaryPackage(extra) &&
                    !archAppx.Any(p => p.Equals(extra, StringComparison.OrdinalIgnoreCase)))
                    archAppx.Add(extra);
            }

            foreach (var lib in archAppx)
            {
                try
                {
                    Report(onProgress, "安装依赖 " + Path.GetFileName(lib) + "…", 40);
                    AddAppxPackage(lib, provisioned: true);
                }
                catch (Exception ex)
                {
                    ApplyLog.Write("依赖 " + Path.GetFileName(lib) + "：" + ex.Message);
                }
            }

            var license = DownloadWingetLicenseXml(onProgress);
            var bundle = Path.Combine(DownloadDir, "Microsoft.DesktopAppInstaller.msixbundle");
            Report(onProgress, "下载 App Installer msixbundle…", 55);
            var bundleUrl = GetWingetCliAssetUrl("Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle");
            var bundleUrls = string.IsNullOrWhiteSpace(bundleUrl)
                ? WingetBundleMirrors()
                : WithGithubMirrors(bundleUrl!).Concat(["https://aka.ms/getwinget"]).ToArray();
            DownloadFromMirrors(bundle, 1_000_000, onProgress, 55, 20, requireZipMagic: true, bundleUrls);

            if (string.IsNullOrWhiteSpace(license) || !File.Exists(license))
                return "已下载安装包，但未能取得 License1.xml，无法按 asheroto 路径预配。";

            Report(onProgress, "预配安装 App Installer（带许可证）…", 80);
            AddAppxProvisionedWithLicense(bundle, license!);
            return "已按 asheroto 离线预配安装 App Installer（依赖 + License1.xml）。";
        }
        catch (Exception ex)
        {
            return "离线预配失败：" + ShortNetError(ex);
        }
    }

    private static void AddAppxProvisionedWithLicense(string bundlePath, string licensePath)
    {
        var escB = bundlePath.Replace("'", "''");
        var escL = licensePath.Replace("'", "''");
        var script = $@"
$ErrorActionPreference = 'Stop'
Add-AppxProvisionedPackage -Online -PackagePath '{escB}' -LicensePath '{escL}' | Out-Null
";
        var code = RunPowerShell(script, timeoutMs: 300_000);
        if (code == 0 || IsAlreadyInstalledAppxOutput(LastPowerShellOutput))
            return;
        throw new InvalidOperationException(FormatAppxFailure("预配安装失败", code, bundlePath));
    }

    private static string? DownloadWingetLicenseXml(Action<SoftwareInstallProgress>? onProgress)
    {
        var dest = Path.Combine(DownloadDir, "DesktopAppInstaller_License1.xml");
        if (File.Exists(dest) && new FileInfo(dest).Length > 100)
            return dest;

        var url = GetWingetCliAssetUrl("License1.xml");
        if (string.IsNullOrWhiteSpace(url))
            url = GetWingetCliAssetUrl("License");
        if (string.IsNullOrWhiteSpace(url))
            return Directory.EnumerateFiles(DownloadDir, "*License*.xml")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

        Report(onProgress, "下载 License1.xml…", 50);
        ConfigureHttps();
        foreach (var u in WithGithubMirrors(url!))
        {
            try
            {
                if (TryDownloadHttpClient(u, dest, onProgress, 50, 4) &&
                    File.Exists(dest) && new FileInfo(dest).Length > 100)
                    return dest;
            }
            catch (Exception ex)
            {
                ApplyLog.Write("License 下载失败 [" + u + "]：" + ex.Message);
            }
        }

        return File.Exists(dest) && new FileInfo(dest).Length > 100 ? dest : null;
    }

    private static string? GetWingetCliAssetUrl(string nameContains)
    {
        try
        {
            ConfigureHttps();
            var handler = new System.Net.Http.HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseProxy = true,
                Proxy = WebRequest.GetSystemWebProxy(),
            };
            using var client = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "SrvDesk");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            var json = client.GetStringAsync("https://api.github.com/repos/microsoft/winget-cli/releases/latest")
                .GetAwaiter().GetResult();
            foreach (Match m in Regex.Matches(
                         json,
                         "\"name\"\\s*:\\s*\"([^\"]+)\"[\\s\\S]{0,800}?\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"",
                         RegexOptions.IgnoreCase))
            {
                if (m.Groups[1].Value.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    return m.Groups[2].Value.Replace("\\/", "/");
            }
        }
        catch (Exception ex)
        {
            ApplyLog.Write("GitHub API 解析失败（" + nameContains + "）：" + ex.Message);
        }

        return null;
    }

    private static void TryCloseWingetLockingProcesses()
    {
        foreach (var name in new[] { "winget", "WindowsPackageManagerServer", "AppInstaller", "WinGetServer" })
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(); }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }
    }

    private static void AddWindowsAppsUserPath()
    {
        try
        {
            AddToEnvironmentPath(@"%LOCALAPPDATA%\Microsoft\WindowsApps", EnvironmentVariableTarget.User);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("写入用户 PATH 失败：" + ex.Message);
        }
    }

    private static void ApplyWingetPathAndPermissions(List<string> notes)
    {
        try
        {
            AddWindowsAppsUserPath();
            var folder = TryFindDesktopAppInstallerFolder();
            if (string.IsNullOrWhiteSpace(folder))
            {
                notes.Add("已把 %LOCALAPPDATA%\\Microsoft\\WindowsApps 加入用户 PATH。");
                return;
            }

            TryGrantAdministratorsFullControl(folder!);
            AddToEnvironmentPath(folder!, EnvironmentVariableTarget.Machine);
            notes.Add("已修正 App Installer 目录权限并加入系统 PATH：" + folder);
        }
        catch (Exception ex)
        {
            notes.Add("PATH/权限调整失败：" + ex.Message);
            ApplyLog.Write("PATH/权限：" + ex.Message);
        }
    }

    private static string? TryFindDesktopAppInstallerFolder()
    {
        var exe = TryGetAppxWingetPath();
        if (!string.IsNullOrWhiteSpace(exe))
        {
            var dir = Path.GetDirectoryName(exe);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                return dir;
        }

        const string root = @"C:\Program Files\WindowsApps";
        if (!Directory.Exists(root)) return null;
        try
        {
            return Directory.GetDirectories(root, "Microsoft.DesktopAppInstaller_*_*x64*__8wekyb3d8bbwe")
                .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static void TryGrantAdministratorsFullControl(string folder)
    {
        try
        {
            var dir = new DirectoryInfo(folder);
            var acl = dir.GetAccessControl();
            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var rule = new FileSystemAccessRule(
                sid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);
            acl.AddAccessRule(rule);
            dir.SetAccessControl(acl);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("设置目录 ACL 失败：" + ex.Message);
        }
    }

    private static void AddToEnvironmentPath(string pathToAdd, EnvironmentVariableTarget scope)
    {
        var current = Environment.GetEnvironmentVariable("Path", scope) ?? "";
        var parts = current.Split([';'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(p => p.Trim().Equals(pathToAdd, StringComparison.OrdinalIgnoreCase)))
        {
            EnsureProcessPath(pathToAdd);
            return;
        }

        var next = string.IsNullOrWhiteSpace(current)
            ? pathToAdd
            : current.Trim().TrimEnd(';') + ";" + pathToAdd;
        Environment.SetEnvironmentVariable("Path", next, scope);
        EnsureProcessPath(pathToAdd);
        ApplyLog.Write("PATH += " + pathToAdd + " (" + scope + ")");
    }

    private static void EnsureProcessPath(string pathToAdd)
    {
        var expanded = Environment.ExpandEnvironmentVariables(pathToAdd);
        var proc = Environment.GetEnvironmentVariable("Path") ?? "";
        if (proc.Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Any(p => p.Trim().Equals(expanded, StringComparison.OrdinalIgnoreCase) ||
                      p.Trim().Equals(pathToAdd, StringComparison.OrdinalIgnoreCase)))
            return;
        Environment.SetEnvironmentVariable("Path", proc.Trim().TrimEnd(';') + ";" + expanded, EnvironmentVariableTarget.Process);
    }

    private static string TryBootstrapWingetPackages(bool preferProvisioned, Action<SoftwareInstallProgress>? onProgress = null)
    {
        Directory.CreateDirectory(DownloadDir);
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        EnsureAppxSideloadAllowed();

        var vcLibs = Path.Combine(DownloadDir, "Microsoft.VCLibs.x64.14.00.Desktop.appx");
        var uiXaml = Path.Combine(DownloadDir, "Microsoft.UI.Xaml.2.8.x64.appx");
        var bundle = Path.Combine(DownloadDir, "Microsoft.DesktopAppInstaller.msixbundle");
        // 兼容用户从 GitHub 原名拷贝的文件
        var bundleAlt = Path.Combine(DownloadDir,
            "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle");
        if (!File.Exists(bundle) && File.Exists(bundleAlt))
        {
            try { File.Copy(bundleAlt, bundle, overwrite: true); } catch { bundle = bundleAlt; }
        }

        var depsZip = Path.Combine(DownloadDir, "DesktopAppInstaller_Dependencies.zip");
        var errors = new List<string>();

        try
        {
            // 1) 尽量下载官方 Dependencies.zip（含 VCLibs/UI.Xaml），再下主包
            try
            {
                DownloadFromMirrors(
                    depsZip,
                    minBytes: 1_000_000,
                    onProgress,
                    percentBase: 20,
                    percentSpan: 20,
                    requireZipMagic: true,
                    WingetDependencyZipMirrors());
                TryExtractWingetDependencies(depsZip, DownloadDir, onProgress);
            }
            catch (Exception ex)
            {
                errors.Add("Dependencies.zip：" + ShortNetError(ex));
                ApplyLog.Write("Dependencies.zip 下载失败：" + ex.Message);
            }

            try
            {
                DownloadFromMirrors(
                    vcLibs,
                    minBytes: 500_000,
                    onProgress,
                    percentBase: 40,
                    percentSpan: 10,
                    requireZipMagic: true,
                    WingetVcLibsMirrors());
            }
            catch (Exception ex)
            {
                errors.Add("VCLibs：" + ShortNetError(ex));
            }

            try
            {
                DownloadFromMirrors(
                    uiXaml,
                    minBytes: 500_000,
                    onProgress,
                    percentBase: 50,
                    percentSpan: 10,
                    requireZipMagic: true,
                    WingetUiXamlMirrors());
            }
            catch (Exception ex)
            {
                errors.Add("UI.Xaml：" + ShortNetError(ex));
            }

            try
            {
                DownloadFromMirrors(
                    bundle,
                    minBytes: 1_000_000,
                    onProgress,
                    percentBase: 60,
                    percentSpan: 20,
                    requireZipMagic: true,
                    WingetBundleMirrors());
            }
            catch (Exception ex)
            {
                errors.Add("AppInstaller：" + ShortNetError(ex));
            }

            // 本机已有缓存包时，即使部分下载失败也可继续安装
            var hasVc = File.Exists(vcLibs) && LooksLikeBinaryPackage(vcLibs);
            var hasXaml = File.Exists(uiXaml) && LooksLikeBinaryPackage(uiXaml);
            var hasBundle = File.Exists(bundle) && LooksLikeBinaryPackage(bundle);
            if (!hasBundle && File.Exists(bundleAlt) && LooksLikeBinaryPackage(bundleAlt))
            {
                bundle = bundleAlt;
                hasBundle = true;
            }

            if (!hasBundle)
            {
                var detail = errors.Count > 0 ? string.Join("；", errors) : "未知网络错误";
                return "离线包安装失败：无法下载 App Installer（" + detail + "）。可将安装包手动放入：" + DownloadDir;
            }

            if (hasVc)
            {
                Report(onProgress, "安装 VCLibs…", 82);
                try { AddAppxPackage(vcLibs, provisioned: false); }
                catch (Exception ex) { ApplyLog.Write("VCLibs 安装：" + ex.Message); }
            }

            if (hasXaml)
            {
                Report(onProgress, "安装 UI.Xaml…", 86);
                try { AddAppxPackage(uiXaml, provisioned: false); }
                catch (Exception ex) { ApplyLog.Write("UI.Xaml 安装：" + ex.Message); }
            }

            // Dependencies.zip 里其它 x64 框架（如 WindowsAppRuntime）一并安装
            foreach (var extra in CollectExtraWingetDependencyPackages(DownloadDir, vcLibs, uiXaml))
            {
                try
                {
                    Report(onProgress, "安装依赖 " + Path.GetFileName(extra) + "…", 87);
                    AddAppxPackage(extra, provisioned: false);
                }
                catch (Exception ex)
                {
                    ApplyLog.Write("依赖安装 " + Path.GetFileName(extra) + "：" + ex.Message);
                }
            }

            Report(onProgress, "安装 App Installer…", 90);
            var depPaths = new List<string>();
            if (hasVc) depPaths.Add(vcLibs);
            if (hasXaml) depPaths.Add(uiXaml);
            depPaths.AddRange(CollectExtraWingetDependencyPackages(DownloadDir, vcLibs, uiXaml));

            Exception? installError = null;
            try
            {
                AddAppxPackage(bundle, provisioned: preferProvisioned, depPaths);
            }
            catch (Exception ex)
            {
                installError = ex;
                ApplyLog.Write("AppX 安装失败：" + ex.Message);
                if (preferProvisioned)
                {
                    TryDismProvision(bundle, depPaths);
                    ResetWingetDiscovery();
                }
            }

            // latest（当前 1.29）在 Server 2019/2022 常因 WindowsAppRuntime / 最低系统版本失败
            if (preferProvisioned && !IsAppInstallerPackagePresent() && !IsWingetAvailable())
            {
                try
                {
                    var pin = Path.Combine(DownloadDir, "Microsoft.DesktopAppInstaller.server.msixbundle");
                    Report(onProgress, "改用 Server 兼容版 App Installer（1.11）…", 91);
                    DownloadFromMirrors(
                        pin,
                        minBytes: 1_000_000,
                        onProgress,
                        percentBase: 91,
                        percentSpan: 4,
                        requireZipMagic: true,
                        ServerWingetBundleMirrors());
                    AddAppxPackage(pin, provisioned: true, depPaths);
                    bundle = pin;
                    installError = null;
                }
                catch (Exception ex)
                {
                    installError = ex;
                    ApplyLog.Write("Server 兼容包安装失败：" + ex.Message);
                }
            }

            try
            {
                TryInstallWingetLicense(bundle, onProgress);
            }
            catch (Exception ex)
            {
                ApplyLog.Write("License：" + ex.Message);
            }

            System.Threading.Thread.Sleep(800);
            ResetWingetDiscovery();
            TryBindWingetFromAppx();
            TryEnableWingetAppAlias();
            if (preferProvisioned)
                TryStagePortableWinget(onProgress, notes: null, fromBundlePath: bundle);

            ResetWingetDiscovery();
            if (IsWingetAvailable() || TryBindWingetFromAppx())
            {
                return preferProvisioned
                    ? "已按 Server 路径安装 App Installer（依赖 + 许可证 + 便携目录 C:\\Tools\\winget）。"
                    : "已下载并安装 App Installer 依赖与主包。";
            }

            if (installError is not null)
                return "离线包安装失败：" + ShortNetError(installError);
            return preferProvisioned
                ? "已按 Server 路径安装 App Installer（依赖 + 许可证 + 便携目录 C:\\Tools\\winget）。"
                : "已下载并安装 App Installer 依赖与主包。";
        }
        catch (Exception ex)
        {
            if (preferProvisioned)
            {
                try
                {
                    TryStagePortableWinget(onProgress, notes: null, fromBundlePath: bundle);
                    ResetWingetDiscovery();
                    if (IsWingetAvailable())
                        return "AppX 安装未成功，已改用便携 winget：" + PortableWingetExe;
                }
                catch { /* fall through */ }
            }
            return "离线包安装失败：" + ShortNetError(ex);
        }
    }

    private static IEnumerable<string> CollectExtraWingetDependencyPackages(
        string destDir, string vcLibs, string uiXaml)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { vcLibs, uiXaml };
        var extractDir = Path.Combine(destDir, "winget-deps");
        if (!Directory.Exists(extractDir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(extractDir, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!ext.Equals(".appx", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".msix", StringComparison.OrdinalIgnoreCase))
                continue;

            var lower = file.Replace('\\', '/').ToLowerInvariant();
            // 只要 x64；跳过 arm/x86
            if (lower.Contains("/arm") || lower.Contains("\\arm") || lower.Contains("arm64"))
                continue;
            if ((lower.Contains("/x86") || lower.Contains("\\x86") || lower.Contains(".x86.")) &&
                !lower.Contains("x64"))
                continue;
            if (skip.Contains(file)) continue;
            if (!LooksLikeBinaryPackage(file)) continue;
            yield return file;
        }
    }

    /// <summary>
    /// Server：把可执行的 winget 部署到 C:\Tools\winget。
    /// 本机实测管理员进程调用 WindowsApps 别名常失败，而该便携目录可正常 --version。
    /// </summary>
    private static bool TryStagePortableWinget(
        Action<SoftwareInstallProgress>? onProgress = null,
        List<string>? notes = null,
        string? fromBundlePath = null)
    {
        try
        {
            if (File.Exists(PortableWingetExe) && ProbeWinget(PortableWingetExe))
            {
                _wingetPath = PortableWingetExe;
                _wingetDiscoveryDone = true;
                notes?.Add("已使用便携 winget：" + PortableWingetExe);
                return true;
            }

            Report(onProgress, "Server：部署便携 winget 到 C:\\Tools\\winget…", 96);
            EnsureVcRedistX64(onProgress);

            // 1) 从已安装 Appx 目录复制（与本机 C:\Tools\winget 来源一致）
            var appxExe = TryGetAppxWingetPath();
            if (!string.IsNullOrWhiteSpace(appxExe))
            {
                var srcDir = Path.GetDirectoryName(appxExe);
                if (!string.IsNullOrWhiteSpace(srcDir) && Directory.Exists(srcDir) &&
                    TryCopyWingetTree(srcDir!, PortableWingetDir) &&
                    ProbeWinget(PortableWingetExe))
                {
                    _wingetPath = PortableWingetExe;
                    _wingetDiscoveryDone = true;
                    ApplyLog.Write("已从 Appx 部署便携 winget：" + PortableWingetExe);
                    notes?.Add("已部署便携 winget（来自 Appx）：" + PortableWingetExe);
                    return true;
                }
            }

            // 2) 从已下载的 msixbundle 解出 x64 包再展开
            var bundle = fromBundlePath;
            if (string.IsNullOrWhiteSpace(bundle) || !File.Exists(bundle))
            {
                bundle = Path.Combine(DownloadDir, "Microsoft.DesktopAppInstaller.msixbundle");
                var alt = Path.Combine(DownloadDir, "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle");
                if ((!File.Exists(bundle) || !LooksLikeBinaryPackage(bundle)) && File.Exists(alt))
                    bundle = alt;
            }

            if (!string.IsNullOrWhiteSpace(bundle) && File.Exists(bundle) &&
                TryExtractPortableWingetFromBundle(bundle!, PortableWingetDir, onProgress) &&
                ProbeWinget(PortableWingetExe))
            {
                _wingetPath = PortableWingetExe;
                _wingetDiscoveryDone = true;
                ApplyLog.Write("已从 msixbundle 部署便携 winget：" + PortableWingetExe);
                notes?.Add("已部署便携 winget（来自离线包）：" + PortableWingetExe);
                return true;
            }
        }
        catch (Exception ex)
        {
            ApplyLog.Write("便携 winget 部署失败：" + ex.Message);
            notes?.Add("便携 winget 部署失败：" + ShortNetError(ex));
        }

        return false;
    }

    private static bool TryCopyWingetTree(string srcDir, string destDir)
    {
        try
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.EnumerateFiles(srcDir, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
                }
                catch
                {
                    /* WindowsApps ACL 可能拦个别文件 */
                }
            }

            foreach (var sub in Directory.EnumerateDirectories(srcDir))
            {
                var name = Path.GetFileName(sub);
                if (name.Equals("AppxMetadata", StringComparison.OrdinalIgnoreCase))
                    continue;
                try { CopyDirectoryRecursive(sub, Path.Combine(destDir, name)); }
                catch { /* ignore one subtree */ }
            }

            return File.Exists(Path.Combine(destDir, "winget.exe")) &&
                   File.Exists(Path.Combine(destDir, "WindowsPackageManager.dll"));
        }
        catch
        {
            return false;
        }
    }

    private static void CopyDirectoryRecursive(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(src))
        {
            try { File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true); }
            catch { /* ignore */ }
        }
        foreach (var dir in Directory.EnumerateDirectories(src))
            CopyDirectoryRecursive(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }

    private static bool TryExtractPortableWingetFromBundle(
        string bundlePath,
        string destDir,
        Action<SoftwareInstallProgress>? onProgress)
    {
        var work = Path.Combine(DownloadDir, "winget-bundle-extract-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(work);
            Report(onProgress, "从 msixbundle 解出便携 winget…", 97);

            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(bundlePath, work);
            }
            catch (Exception ex)
            {
                ApplyLog.Write("解压 msixbundle 失败：" + ex.Message);
                return false;
            }

            static bool IsResourcePack(string n) =>
                n.IndexOf("language", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("scale-", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("resources", StringComparison.OrdinalIgnoreCase) >= 0
                || (n.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0
                    && n.IndexOf("x64", StringComparison.OrdinalIgnoreCase) < 0);

            var packages = Directory.EnumerateFiles(work, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f);
                    return ext.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                        || ext.Equals(".appx", StringComparison.OrdinalIgnoreCase);
                })
                .Where(f => !IsResourcePack(Path.GetFileName(f)))
                .ToList();

            var msix = packages
                .Where(f => Path.GetFileName(f).IndexOf("x64", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(f =>
                {
                    var n = Path.GetFileName(f);
                    long score = new FileInfo(f).Length;
                    if (n.IndexOf("AppInstaller", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("DesktopAppInstaller", StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 1_000_000_000L;
                    return score;
                })
                .FirstOrDefault()
                ?? packages.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();

            if (msix is null)
            {
                ApplyLog.Write("msixbundle 内未找到 x64 主包");
                return false;
            }

            var unpack = Path.Combine(work, "msix-unpacked");
            Directory.CreateDirectory(unpack);
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(msix, unpack);
            }
            catch (Exception ex)
            {
                ApplyLog.Write("解压 msix 失败：" + ex.Message);
                return false;
            }

            return TryCopyWingetTree(unpack, destDir);
        }
        finally
        {
            try { if (Directory.Exists(work)) Directory.Delete(work, recursive: true); }
            catch { /* ignore */ }
        }
    }

    private static void TryInstallWingetLicense(string bundlePath, Action<SoftwareInstallProgress>? onProgress)
    {
        var license = DownloadWingetLicenseXml(onProgress);
        if (string.IsNullOrWhiteSpace(license) || !File.Exists(license))
            return;
        AddAppxProvisionedWithLicense(bundlePath, license!);
    }

    private static void TryEnableWingetAppAlias()
    {
        try
        {
            // 应用执行别名：部分系统用此开关控制 WindowsApps\winget.exe 桩
            using var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\App Paths\winget.exe", true);
            var appx = TryGetAppxWingetPath();
            if (!string.IsNullOrWhiteSpace(appx) && k is not null)
            {
                k.SetValue("", appx);
                k.SetValue("Path", Path.GetDirectoryName(appx!) ?? "");
            }
        }
        catch { /* ignore */ }
    }

    private static string ShortNetError(Exception ex)
    {
        var msg = ex.Message ?? "";
        if (ex.InnerException is not null && msg.IndexOf("远程", StringComparison.Ordinal) < 0)
            msg = ex.InnerException.Message;
        if (msg.IndexOf("无法连接到远程服务器", StringComparison.Ordinal) >= 0
            || msg.IndexOf("Unable to connect", StringComparison.OrdinalIgnoreCase) >= 0)
            return "无法连接到远程服务器（请检查外网/代理，或改用手动拷贝安装包）";
        if (msg.Length > 120) msg = msg.Substring(0, 117) + "…";
        return msg;
    }

    private static IEnumerable<string> WithGithubMirrors(string githubUrl)
    {
        yield return githubUrl;
        // 国内常见加速（失败则跳过）
        yield return "https://ghproxy.net/" + githubUrl;
        yield return "https://mirror.ghproxy.com/" + githubUrl;
        yield return "https://ghfast.top/" + githubUrl;
    }

    private static string[] ServerWingetBundleMirrors() =>
        new[]
        {
            "https://github.com/microsoft/winget-cli/releases/download/v1.11.510/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle",
            "https://github.com/microsoft/winget-cli/releases/download/v1.10.340/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle",
        }
        .SelectMany(WithGithubMirrors)
        .ToArray();

    private static string[] WingetBundleMirrors() =>
        WithGithubMirrors(
                "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle")
            .Concat([
                "https://aka.ms/getwinget",
            ])
            .ToArray();

    private static string[] WingetDependencyZipMirrors() =>
        WithGithubMirrors(
                "https://github.com/microsoft/winget-cli/releases/latest/download/DesktopAppInstaller_Dependencies.zip")
            .ToArray();

    private static string[] WingetVcLibsMirrors() =>
    [
        "https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx",
        "https://aka.ms/Microsoft.VCLibs.x64.14.00.appx",
    ];

    private static string[] WingetUiXamlMirrors() =>
        WithGithubMirrors(
                "https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx")
            .ToArray();

    private static void TryExtractWingetDependencies(
        string zipPath,
        string destDir,
        Action<SoftwareInstallProgress>? onProgress)
    {
        if (!File.Exists(zipPath)) return;
        Report(onProgress, "解压 Dependencies…", 38);
        var extractDir = Path.Combine(destDir, "winget-deps");
        try
        {
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
        }
        catch { /* ignore */ }

        Directory.CreateDirectory(extractDir);
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

        // 把 x64 依赖拷到下载目录，供后续安装复用
        foreach (var file in Directory.EnumerateFiles(extractDir, "*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            var ext = Path.GetExtension(file);
            if (!ext.Equals(".appx", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".msix", StringComparison.OrdinalIgnoreCase))
                continue;

            var targetName = name;
            if (name.IndexOf("VCLibs", StringComparison.OrdinalIgnoreCase) >= 0 &&
                name.IndexOf("Desktop", StringComparison.OrdinalIgnoreCase) >= 0)
                targetName = "Microsoft.VCLibs.x64.14.00.Desktop.appx";
            else if (name.IndexOf("UI.Xaml", StringComparison.OrdinalIgnoreCase) >= 0)
                targetName = "Microsoft.UI.Xaml.2.8.x64.appx";
            else if (name.IndexOf("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase) >= 0)
                targetName = name; // 保留原名，供 CollectExtra 安装

            var target = Path.Combine(destDir, targetName);
            try { File.Copy(file, target, overwrite: true); }
            catch { /* ignore one file */ }
        }
    }

    private static void DownloadFromMirrors(
        string dest,
        long minBytes,
        Action<SoftwareInstallProgress>? onProgress,
        int percentBase,
        int percentSpan,
        bool requireZipMagic,
        params string[] urls)
    {
        if (File.Exists(dest))
        {
            var len = new FileInfo(dest).Length;
            if (len >= minBytes && (requireZipMagic ? LooksLikeBinaryPackage(dest) : LooksLikeExeOrMsi(dest)))
            {
                Report(onProgress, "已缓存 " + Path.GetFileName(dest), percentBase + percentSpan);
                return;
            }
        }

        Exception? last = null;
        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            try
            {
                Report(onProgress, "尝试下载 " + Path.GetFileName(dest) + "…", percentBase);
                DownloadToFile(url, dest, minBytes, onProgress, percentBase, percentSpan, requireZipMagic);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                ApplyLog.Write("下载失败 [" + url + "]：" + ex.Message);
                try { if (File.Exists(dest)) File.Delete(dest); } catch { /* ignore */ }
            }
        }

        throw last ?? new InvalidOperationException("无可用下载地址：" + Path.GetFileName(dest));
    }

    private static string TryRegisterAppInstaller()
    {
        const string script = @"
$ErrorActionPreference = 'SilentlyContinue'
Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe
Get-AppxPackage -AllUsers -Name Microsoft.DesktopAppInstaller | Out-Null
";
        try
        {
            RunPowerShell(script);
            return "已尝试注册 App Installer 应用别名。";
        }
        catch (Exception ex)
        {
            return "注册 App Installer 失败：" + ex.Message;
        }
    }

    private static string FormatWingetReady(List<string> notes)
    {
        ResetWingetDiscovery();
        EnsureWingetDiscovered();
        notes.Add("winget 已可用，可继续一键安装其它软件。");
        return string.Join("\r\n", notes.Where(n => n.Length > 0));
    }

    private static void DownloadPackage(
        string url,
        string dest,
        long minBytes,
        Action<SoftwareInstallProgress>? onProgress = null,
        int percentBase = 0,
        int percentSpan = 20)
    {
        DownloadToFile(url, dest, minBytes, onProgress, percentBase, percentSpan, requireZipMagic: true);
    }

    /// <summary>下载 EXE/MSI 等安装程序（校验 MZ 头，而非 AppX 的 PK）。</summary>
    private static void DownloadInstaller(
        string url,
        string dest,
        long minBytes,
        Action<SoftwareInstallProgress>? onProgress = null,
        int percentBase = 0,
        int percentSpan = 20)
    {
        DownloadToFile(url, dest, minBytes, onProgress, percentBase, percentSpan, requireZipMagic: false);
    }

    private static void DownloadToFile(
        string url,
        string dest,
        long minBytes,
        Action<SoftwareInstallProgress>? onProgress,
        int percentBase,
        int percentSpan,
        bool requireZipMagic)
    {
        if (File.Exists(dest))
        {
            var len = new FileInfo(dest).Length;
            if (len >= minBytes && (requireZipMagic ? LooksLikeBinaryPackage(dest) : LooksLikeExeOrMsi(dest)))
            {
                Report(onProgress, "已缓存 " + Path.GetFileName(dest), percentBase + percentSpan);
                return;
            }
            try { File.Delete(dest); } catch { /* ignore */ }
        }

        ConfigureHttps();
        var name = Path.GetFileName(dest);
        Report(onProgress, "开始下载 " + name, percentBase);
        Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? DownloadDir);

        Exception? last = null;
        // 1) HttpClient（浏览器 UA + 系统代理）  2) curl.exe  3) WebClient
        foreach (var attempt in new Func<bool>[]
                 {
                     () => TryDownloadHttpClient(url, dest, onProgress, percentBase, percentSpan),
                     () => TryDownloadCurl(url, dest),
                     () => TryDownloadWebClient(url, dest, onProgress, percentBase, percentSpan),
                 })
        {
            try
            {
                if (!attempt()) continue;
                var size = new FileInfo(dest).Length;
                var looksOk = requireZipMagic ? LooksLikeBinaryPackage(dest) : LooksLikeExeOrMsi(dest);
                if (size >= minBytes && looksOk)
                {
                    Report(onProgress, "下载完成 " + name, percentBase + percentSpan);
                    return;
                }

                try { File.Delete(dest); } catch { /* ignore */ }
                last = new InvalidOperationException("下载文件无效或过小：" + name + "（" + size + " 字节）");
            }
            catch (Exception ex)
            {
                last = ex;
                try { if (File.Exists(dest)) File.Delete(dest); } catch { /* ignore */ }
            }
        }

        throw last ?? new InvalidOperationException("下载失败：" + name);
    }

    private static void ConfigureHttps()
    {
        try
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12 | (SecurityProtocolType)3072 /*Tls13*/;
        }
        catch
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        try
        {
            // 管理员进程常丢用户代理：强制使用系统代理 + 默认凭据
            var proxy = WebRequest.GetSystemWebProxy();
            proxy.Credentials = CredentialCache.DefaultCredentials;
            WebRequest.DefaultWebProxy = proxy;
        }
        catch { /* ignore */ }
    }

    private const string BrowserUa =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";

    private static bool TryDownloadHttpClient(
        string url,
        string dest,
        Action<SoftwareInstallProgress>? onProgress,
        int percentBase,
        int percentSpan)
    {
        var handler = new System.Net.Http.HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseProxy = true,
            Proxy = WebRequest.GetSystemWebProxy(),
            UseDefaultCredentials = true,
        };
        try { handler.Proxy!.Credentials = CredentialCache.DefaultCredentials; }
        catch { /* ignore */ }

        using var client = new System.Net.Http.HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(15),
        };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", BrowserUa);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");

        using var resp = client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead)
            .GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1L;
        using var input = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var output = File.Create(dest);
        var buffer = new byte[81920];
        long readTotal = 0;
        var lastPct = -1;
        int n;
        while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, n);
            readTotal += n;
            if (total <= 0 || onProgress is null) continue;
            var pct = (int)(readTotal * 100 / total);
            if (pct == lastPct) continue;
            lastPct = pct;
            Report(onProgress, $"下载 {Path.GetFileName(dest)} {pct}%",
                percentBase + (int)(pct / 100.0 * percentSpan));
        }

        return File.Exists(dest) && new FileInfo(dest).Length > 0;
    }

    private static bool TryDownloadCurl(string url, string dest)
    {
        var curl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "curl.exe");
        if (!File.Exists(curl))
            curl = "curl.exe";

        // -L 跟随跳转；-A 浏览器 UA；--proxy-default 跟随系统（Win10+ curl）
        var args =
            "-L --retry 3 --connect-timeout 20 --max-time 900 " +
            "-A \"" + BrowserUa + "\" " +
            "-o \"" + dest + "\" \"" + url + "\"";
        var code = Run(curl, args, timeoutMs: 920_000);
        return code == 0 && File.Exists(dest) && new FileInfo(dest).Length > 0;
    }

    private static bool TryDownloadWebClient(
        string url,
        string dest,
        Action<SoftwareInstallProgress>? onProgress,
        int percentBase,
        int percentSpan)
    {
        using var wc = new WebClient();
        wc.Proxy = WebRequest.GetSystemWebProxy();
        wc.Proxy.Credentials = CredentialCache.DefaultCredentials;
        wc.Headers[HttpRequestHeader.UserAgent] = BrowserUa;
        wc.Headers[HttpRequestHeader.Accept] = "*/*";
        var lastPct = -1;
        wc.DownloadProgressChanged += (_, e) =>
        {
            if (e.ProgressPercentage == lastPct) return;
            lastPct = e.ProgressPercentage;
            Report(onProgress, $"下载 {Path.GetFileName(dest)} {e.ProgressPercentage}%",
                percentBase + (int)(e.ProgressPercentage / 100.0 * percentSpan));
        };
        try
        {
            wc.DownloadFileTaskAsync(new Uri(url), dest).GetAwaiter().GetResult();
        }
        catch
        {
            wc.DownloadFile(url, dest);
        }

        return File.Exists(dest) && new FileInfo(dest).Length > 0;
    }

    private static bool LooksLikeExeOrMsi(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 2) return false;
            var b0 = fs.ReadByte();
            var b1 = fs.ReadByte();
            // PE EXE: MZ；MSI 为复合文档，常见 D0 CF，此处放宽到非纯文本即可
            if (b0 == 'M' && b1 == 'Z') return true;
            if (b0 == 0xD0 && b1 == 0xCF) return true;
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool LooksLikeBinaryPackage(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 4) return false;
            var b0 = fs.ReadByte();
            var b1 = fs.ReadByte();
            // ZIP/Office Open XML / msix/appx 均为 PK..
            return b0 == 'P' && b1 == 'K';
        }
        catch
        {
            return false;
        }
    }

    private static void AddAppxPackage(string path, bool provisioned, IReadOnlyList<string>? dependencies = null)
    {
        var escaped = path.Replace("'", "''");
        var deps = (dependencies ?? [])
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var depPs = deps.Count == 0
            ? "$deps = @()"
            : "$deps = @(" + string.Join(",", deps.Select(p => "'" + p.Replace("'", "''") + "'")) + ")";

        string script;
        if (provisioned)
        {
            script = $@"
$ErrorActionPreference = 'Stop'
$path = '{escaped}'
{depPs}
try {{
  if ($deps.Count -gt 0) {{
    Add-AppxProvisionedPackage -Online -PackagePath $path -SkipLicense -DependencyPackagePath $deps | Out-Null
  }} else {{
    Add-AppxProvisionedPackage -Online -PackagePath $path -SkipLicense | Out-Null
  }}
}} catch {{
  $prov = $_
  try {{
    if ($deps.Count -gt 0) {{
      Add-AppxPackage -Path $path -DependencyPath $deps -ForceApplicationShutdown -ForceUpdateFromAnyVersion -ErrorAction Stop
    }} else {{
      Add-AppxPackage -Path $path -ForceApplicationShutdown -ForceUpdateFromAnyVersion -ErrorAction Stop
    }}
  }} catch {{
    Write-Error ($prov.Exception.Message + ' | ' + $_.Exception.Message)
    throw
  }}
}}
";
        }
        else
        {
            script = $@"
$ErrorActionPreference = 'Stop'
$path = '{escaped}'
{depPs}
if ($deps.Count -gt 0) {{
  Add-AppxPackage -Path $path -DependencyPath $deps -ForceApplicationShutdown -ForceUpdateFromAnyVersion
}} else {{
  Add-AppxPackage -Path $path -ForceApplicationShutdown -ForceUpdateFromAnyVersion
}}
";
        }

        var code = RunPowerShell(script);
        if (code == 0 || IsAlreadyInstalledAppxOutput(LastPowerShellOutput))
            return;
        throw new InvalidOperationException(FormatAppxFailure("安装包失败", code, path));
    }

    private static void TryDismProvision(string bundle, IReadOnlyList<string> dependencies)
    {
        try
        {
            var args = "/Online /Add-ProvisionedAppxPackage /PackagePath=\"" + bundle + "\" /SkipLicense /Quiet /NoRestart";
            foreach (var dep in dependencies.Where(File.Exists))
                args += " /DependencyPackagePath=\"" + dep + "\"";
            ApplyLog.Write("DISM 预配 App Installer…");
            var code = Run("dism.exe", args, timeoutMs: 600_000);
            ApplyLog.Write("DISM 退出码 " + code + (LastProcessOutput.Length > 0 ? "：" + TrimOutput(LastProcessOutput, 400) : ""));
        }
        catch (Exception ex)
        {
            ApplyLog.Write("DISM 预配失败：" + ex.Message);
        }
    }

    private static bool IsAlreadyInstalledAppxOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return false;
        return output.IndexOf("0x80073CFB", StringComparison.OrdinalIgnoreCase) >= 0
            || output.IndexOf("0x80073D06", StringComparison.OrdinalIgnoreCase) >= 0
            || output.IndexOf("0x80073CF0", StringComparison.OrdinalIgnoreCase) >= 0
            || output.IndexOf("already installed", StringComparison.OrdinalIgnoreCase) >= 0
            || output.IndexOf("已安装", StringComparison.Ordinal) >= 0
            || output.IndexOf("更高版本", StringComparison.Ordinal) >= 0;
    }

    private static string FormatAppxFailure(string prefix, int code, string path)
    {
        var name = Path.GetFileName(path);
        var detail = TrimOutput(LastPowerShellOutput, 240);
        if (detail.Length == 0)
            return prefix + "，退出码 " + code + "：" + name;
        return prefix + "，退出码 " + code + "：" + name + "\r\n" + detail;
    }

    private static string TrimOutput(string text, int max)
    {
        var t = Regex.Replace(text ?? "", @"\s+", " ").Trim();
        if (t.Length <= max) return t;
        return t.Substring(0, max - 1) + "…";
    }

    private static bool Matches(string displayName, string[] patterns)
    {
        foreach (var p in patterns)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (IsAsciiShort(p))
            {
                if (IsAsciiWordMatch(displayName, p)) return true;
                continue;
            }

            if (displayName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static bool IsAsciiShort(string p)
    {
        if (p.Length > 3) return false;
        foreach (var c in p)
        {
            if (c > 127) return false;
        }
        return true;
    }

    /// <summary>短英文模式按词匹配，避免 "pi" 命中 Epic / Appium 等。</summary>
    private static bool IsAsciiWordMatch(string displayName, string pattern)
    {
        var start = 0;
        while (start <= displayName.Length - pattern.Length)
        {
            var i = displayName.IndexOf(pattern, start, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return false;
            var beforeOk = i == 0 || !IsAsciiLetterOrDigit(displayName[i - 1]);
            var after = i + pattern.Length;
            var afterOk = after >= displayName.Length || !IsAsciiLetterOrDigit(displayName[after]);
            if (beforeOk && afterOk) return true;
            start = i + 1;
        }
        return false;
    }

    private static bool IsAsciiLetterOrDigit(char c) =>
        c <= 127 && char.IsLetterOrDigit(c);

    private static IEnumerable<(RegistryHive Hive, string SubKey)> UninstallKeyPaths()
    {
        const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        yield return (RegistryHive.LocalMachine, sub);
        yield return (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        yield return (RegistryHive.CurrentUser, sub);
    }

    private static void RunShell(string command)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + command,
            UseShellExecute = false,
            CreateNoWindow = true,
        })?.WaitForExit(600_000);
    }

    private static int RunPowerShell(string script, int timeoutMs = 600_000)
    {
        var file = Path.Combine(DownloadDir, "winget-install-" + Guid.NewGuid().ToString("N") + ".ps1");
        Directory.CreateDirectory(DownloadDir);
        File.WriteAllText(file, script, Encoding.UTF8);
        try
        {
            return Run("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -File \"" + file + "\"",
                timeoutMs: timeoutMs);
        }
        finally
        {
            try { File.Delete(file); } catch { /* ignore */ }
        }
    }

    private static void Report(Action<SoftwareInstallProgress>? onProgress, string message, int percent)
    {
        if (onProgress is null) return;
        onProgress(new SoftwareInstallProgress
        {
            Message = message,
            Percent = Math.Max(0, Math.Min(100, percent)),
        });
    }

    /// <summary>
    /// 含点且无空格 → 按包 ID 精确安装；微软商店 ProductId（如 9PLM9XGG6VKS）同样按 --id；
    /// 否则按名称搜索安装（自定义项常用）。
    /// </summary>
    private static string BuildWingetInstallArgs(string wingetIdOrName, bool preferWingetSource)
    {
        var raw = (wingetIdOrName ?? "").Trim();
        var quoted = QuoteArg(raw);
        var looksLikeId = LooksLikeWingetPackageId(raw);
        var target = looksLikeId ? $"-e --id {quoted}" : $"--name {quoted} --exact";
        // 商店 ProductId 不在 winget 社区源，强制走默认源（含 msstore）
        var useWingetSource = preferWingetSource && !LooksLikeMsStoreProductId(raw);
        var source = useWingetSource ? " --source winget" : "";
        return $"install {target}{source} --silent " +
               "--accept-package-agreements --accept-source-agreements --disable-interactivity";
    }

    private static bool LooksLikeWingetPackageId(string raw) =>
        raw.Length > 0
        && raw.IndexOf(' ') < 0
        && (raw.IndexOf('.') > 0 || LooksLikeMsStoreProductId(raw));

    /// <summary>微软商店 ProductId：以数字开头的短字母数字串，如 9PLM9XGG6VKS。</summary>
    private static bool LooksLikeMsStoreProductId(string raw)
    {
        if (raw.Length is < 10 or > 16) return false;
        if (!char.IsDigit(raw[0])) return false;
        foreach (var c in raw)
        {
            if (!char.IsLetterOrDigit(c)) return false;
        }
        return true;
    }

    private static string QuoteArg(string value)
    {
        if (value.Length == 0) return "\"\"";
        if (value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return value;
        return "\"" + value.Replace("\"", "") + "\"";
    }

    private static readonly SemaphoreSlim WingetGate = new(1, 1);

    private static int RunWinget(string args, Action<SoftwareInstallProgress>? onProgress = null)
    {
        // winget 进程间互斥：并行调用常报「另一安装正在进行」；离线包安装不受此锁影响
        WingetGate.Wait();
        try
        {
            return RunStreaming(ResolveWingetPath(), args, setWorkingDirForExe: true, onProgress);
        }
        finally
        {
            WingetGate.Release();
        }
    }

    private static string RunCaptureWinget(string args) =>
        RunCapture(ResolveWingetPath(), args, setWorkingDirForExe: true);

    private static int RunStreaming(
        string file,
        string args,
        bool setWorkingDirForExe,
        Action<SoftwareInstallProgress>? onProgress)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
        };
        if (setWorkingDirForExe && !string.Equals(file, "winget.exe", StringComparison.OrdinalIgnoreCase))
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                psi.WorkingDirectory = dir!;
        }

        var previousErrorMode = SetErrorMode(SemFailCriticalErrors | SemNoOpenFileErrorBox);
        try
        {
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 " + file);
            var parser = new WingetProgressParser();
            var lastReport = DateTime.MinValue;
            var lastPercent = -1;
            var lastMessage = "";
            var lineBuf = new StringBuilder();
            var sync = new object();

            void HandleText(string text)
            {
                lock (sync)
                {
                    foreach (var ch in text)
                    {
                        if (ch is '\r' or '\n')
                        {
                            if (lineBuf.Length == 0) continue;
                            var line = lineBuf.ToString();
                            lineBuf.Clear();
                            Emit(line);
                        }
                        else
                        {
                            lineBuf.Append(ch);
                        }
                    }
                }
            }

            void Emit(string line)
            {
                var update = parser.Feed(line);
                if (update is null) return;
                var now = DateTime.UtcNow;
                if (update.Percent == lastPercent &&
                    update.Message == lastMessage &&
                    (now - lastReport).TotalMilliseconds < 120)
                    return;
                if (update.Percent < lastPercent && update.Percent < 100)
                    return;

                lastPercent = update.Percent;
                lastMessage = update.Message;
                lastReport = now;
                onProgress?.Invoke(update);
            }

            var stdout = System.Threading.Tasks.Task.Run(() => DrainStream(p.StandardOutput, HandleText));
            var stderr = System.Threading.Tasks.Task.Run(() => DrainStream(p.StandardError, HandleText));
            p.WaitForExit(600_000);
            System.Threading.Tasks.Task.WaitAll(new[] { stdout, stderr }, 15_000);
            lock (sync)
            {
                if (lineBuf.Length > 0)
                    Emit(lineBuf.ToString());
            }
            return p.ExitCode;
        }
        finally
        {
            SetErrorMode(previousErrorMode);
        }
    }

    private static void DrainStream(StreamReader reader, Action<string> onChunk)
    {
        var buffer = new char[1024];
        try
        {
            int n;
            while ((n = reader.Read(buffer, 0, buffer.Length)) > 0)
                onChunk(new string(buffer, 0, n));
        }
        catch
        {
            /* ignore */
        }
    }

    private sealed class WingetProgressParser
    {
        private int _percent = 5;
        private string _phase = "准备";
        private static readonly Regex Ansi = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);
        private static readonly Regex Pct = new(@"(\d{1,3})\s*%", RegexOptions.Compiled);
        private static readonly Regex Size =
            new(@"([\d.,]+)\s*(K|M|G)i?B\s*/\s*([\d.,]+)\s*(K|M|G)i?B", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SoftwareInstallProgress? Feed(string raw)
        {
            var line = Ansi.Replace(raw, "").Trim();
            if (line.Length == 0) return null;
            line = Regex.Replace(line, @"[█▒░■□▪▫]+", " ").Trim();
            if (line.Length == 0) return null;

            var lower = line.ToLowerInvariant();
            if (lower.Contains("found "))
            {
                _phase = "已找到包";
                _percent = Math.Max(_percent, 8);
            }
            else if (lower.Contains("downloading") || lower.Contains("download") ||
                     line.Contains("正在下载") || line.Contains("下载中"))
            {
                _phase = "下载中";
                _percent = Math.Max(_percent, 15);
            }
            else if (lower.Contains("hash") || lower.Contains("verif") || lower.Contains("校验"))
            {
                _phase = "校验中";
                _percent = Math.Max(_percent, 72);
            }
            else if (lower.Contains("starting package install") || lower.Contains("installing") ||
                     lower.Contains("正在安装") || lower.Contains("extract") || lower.Contains("正在解压"))
            {
                _phase = "安装中";
                _percent = Math.Max(_percent, 78);
            }
            else if (lower.Contains("successfully installed") || lower.Contains("已成功安装") ||
                     lower.Contains("successfully uninstalled") || lower.Contains("已成功卸载"))
            {
                _phase = "完成";
                _percent = 100;
            }
            else if (lower.Contains("uninstall"))
            {
                _phase = "卸载中";
                _percent = Math.Max(_percent, 30);
            }

            var sizeMatch = Size.Match(line);
            if (sizeMatch.Success)
            {
                var cur = ToBytes(sizeMatch.Groups[1].Value, sizeMatch.Groups[2].Value);
                var total = ToBytes(sizeMatch.Groups[3].Value, sizeMatch.Groups[4].Value);
                if (total > 0)
                {
                    var ratio = Math.Max(0, Math.Min(1, cur / total));
                    var mapped = 15 + (int)(ratio * 55);
                    _percent = Math.Max(_percent, mapped);
                    _phase = $"下载 {sizeMatch.Groups[1].Value}{sizeMatch.Groups[2].Value}B / {sizeMatch.Groups[3].Value}{sizeMatch.Groups[4].Value}B";
                }
            }

            var pctMatch = Pct.Match(line);
            if (pctMatch.Success && int.TryParse(pctMatch.Groups[1].Value, out var pct) && pct >= 0 && pct <= 100)
            {
                int mapped;
                if (_phase.StartsWith("下载", StringComparison.Ordinal) || _phase == "下载中")
                    mapped = 15 + (int)(pct / 100.0 * 55);
                else if (_phase == "安装中" || _phase == "卸载中")
                    mapped = 78 + (int)(pct / 100.0 * 18);
                else
                    mapped = pct;
                _percent = Math.Max(_percent, Math.Min(99, mapped));
            }

            var msg = _phase;
            if (pctMatch.Success)
                msg = _phase + " " + pctMatch.Groups[1].Value + "%";
            // 不拼接 winget 原始行：编码不对时会出现乱码（如「下载中 · 姝…」）

            return new SoftwareInstallProgress { Message = msg, Percent = _percent };
        }

        private static double ToBytes(string num, string unit)
        {
            if (!double.TryParse(num.Replace(",", "."),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var n))
                return 0;
            return unit.ToUpperInvariant() switch
            {
                "K" => n * 1024,
                "M" => n * 1024 * 1024,
                "G" => n * 1024 * 1024 * 1024,
                _ => n,
            };
        }
    }

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static int Run(string file, string args, bool setWorkingDirForExe = false, int timeoutMs = 600_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
        };
        if (setWorkingDirForExe && !string.Equals(file, "winget.exe", StringComparison.OrdinalIgnoreCase))
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                psi.WorkingDirectory = dir!;
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 " + file);
        // 避免管道缓冲区塞满导致死锁
        var stdout = System.Threading.Tasks.Task.Run(() => p.StandardOutput.ReadToEnd());
        var stderr = System.Threading.Tasks.Task.Run(() => p.StandardError.ReadToEnd());
        if (!p.WaitForExit(Math.Max(5_000, timeoutMs)))
        {
            try
            {
                p.Kill();
                p.WaitForExit(5_000);
            }
            catch { /* ignore */ }
            ApplyLog.Write("进程超时已终止：" + file + "（" + timeoutMs + "ms）");
            return -2;
        }

        System.Threading.Tasks.Task.WaitAll(new[] { stdout, stderr }, 10_000);
        LastProcessOutput = ((stdout.Status == TaskStatus.RanToCompletion ? stdout.Result : "") + "\r\n" +
                             (stderr.Status == TaskStatus.RanToCompletion ? stderr.Result : "")).Trim();
        if (file.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0)
            LastPowerShellOutput = LastProcessOutput;
        return p.ExitCode;
    }

    private static string RunCapture(string file, string args, bool setWorkingDirForExe = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
        };
        if (setWorkingDirForExe && !string.Equals(file, "winget.exe", StringComparison.OrdinalIgnoreCase))
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                psi.WorkingDirectory = dir!;
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 " + file);
        var stdout = System.Threading.Tasks.Task.Run(() => p.StandardOutput.ReadToEnd());
        var stderr = System.Threading.Tasks.Task.Run(() => p.StandardError.ReadToEnd());
        p.WaitForExit(120_000);
        System.Threading.Tasks.Task.WaitAll(new[] { stdout, stderr }, 10_000);
        return stdout.Result + stderr.Result;
    }

    private const uint SemFailCriticalErrors = 0x0001;
    private const uint SemNoOpenFileErrorBox = 0x8000;

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint SetErrorMode(uint uMode);
}
