using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WinOpt;

internal sealed class CommonSoftwareStatus
{
    public bool Installed { get; set; }
    public string Version { get; set; } = "";
    public string? UninstallCommand { get; set; }
}

/// <summary>软件安装/卸载实时进度（Percent 为 0–100 估算）。</summary>
internal sealed class SoftwareInstallProgress
{
    public string Message { get; init; } = "";
    public int Percent { get; init; }
}

internal static class CommonSoftwareHelper
{
    private static string? _wingetPath;
    private static bool _wingetDiscoveryDone;

    private static readonly string[] KnownWingetPaths =
    [
        @"C:\Tools\winget\winget.exe",
        @"C:\Program Files\WinGet\winget.exe",
        @"C:\Program Files (x86)\WinGet\winget.exe",
    ];

    public static string DownloadDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinOpt", "software-downloads");

    public static bool IsWingetAvailable()
    {
        EnsureWingetDiscovered();
        return _wingetPath is not null;
    }

    public static void ResetWingetDiscovery()
    {
        _wingetPath = null;
        _wingetDiscoveryDone = false;
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
            yield return appx;

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
    }

    public static CommonSoftwareStatus Query(CommonSoftwareItem item)
    {
        if (item.IsWingetBootstrap)
            return QueryWingetStatus();

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

        return new CommonSoftwareStatus();
    }

    public static string Install(CommonSoftwareItem item, Action<SoftwareInstallProgress>? onProgress = null)
    {
        ApplyLog.Write("常用软件安装：" + item.Title);
        if (item.IsWingetBootstrap)
            return InstallWinget(onProgress);

        Report(onProgress, "准备安装 " + item.Title, 2);
        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            Report(onProgress, "正在调用 winget…", 5);
            var code = RunWinget(
                $"install -e --id {item.WingetId} --accept-package-agreements --accept-source-agreements --disable-interactivity",
                onProgress);
            if (code == 0)
            {
                Report(onProgress, "安装完成", 100);
                return "";
            }
            if (code == -1978335189) // 0x8A150013 already installed
            {
                Report(onProgress, "已安装", 100);
                return "软件已安装或无需重复安装。";
            }
        }

        Report(onProgress, "打开官方下载页…", 95);
        OpenDownloadPage(item);
        return IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId)
            ? "winget 安装未成功，已在浏览器打开官方下载页，请手动安装。"
            : string.IsNullOrWhiteSpace(item.WingetId)
                ? "该软件暂无 winget 包，已在浏览器打开官方下载页。"
                : "本机未检测到 winget，已在浏览器打开官方下载页，请手动安装。";
    }

    public static string Uninstall(CommonSoftwareItem item, Action<SoftwareInstallProgress>? onProgress = null)
    {
        ApplyLog.Write("常用软件卸载：" + item.Title);
        if (item.IsWingetBootstrap)
            return "winget（应用安装程序）为系统组件，不建议在此卸载。请在「设置 → 应用」中操作。";

        Report(onProgress, "准备卸载 " + item.Title, 5);
        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            var code = RunWinget($"uninstall -e --id {item.WingetId} --disable-interactivity", onProgress);
            if (code == 0)
            {
                Report(onProgress, "卸载完成", 100);
                return "";
            }
        }

        var status = Query(item);
        var cmd = status.UninstallCommand;
        if (string.IsNullOrWhiteSpace(cmd))
            return "未找到可用的卸载命令。请在「设置 → 应用」中手动卸载。";

        Report(onProgress, "执行卸载命令…", 40);
        RunShell(cmd!);
        Report(onProgress, "卸载命令已执行", 100);
        return "";
    }

    public static string InstallWinget(Action<SoftwareInstallProgress>? onProgress = null)
    {
        if (IsWingetAvailable())
        {
            Report(onProgress, "winget 已可用", 100);
            return "";
        }

        ApplyLog.Write("安装/修复 winget（App Installer）" + (Optimizer.IsWindowsServer() ? " [Server]" : ""));
        ResetWingetDiscovery();
        var notes = new List<string>();

        if (IsAppInstallerPackagePresent())
        {
            Report(onProgress, "注册 App Installer 别名…", 15);
            var register = TryRegisterAppInstaller();
            if (register.Length > 0) notes.Add(register);
            ResetWingetDiscovery();
            if (TryBindWingetFromAppx())
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }
        }

        if (Optimizer.IsWindowsServer())
        {
            Report(onProgress, "下载并安装 App Installer（Server）…", 20);
            var bootstrap = TryBootstrapWingetPackages(preferProvisioned: true, onProgress);
            if (bootstrap.Length > 0) notes.Add(bootstrap);
            ResetWingetDiscovery();
            if (IsWingetAvailable() || TryBindWingetFromAppx())
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }

            Report(onProgress, "再次注册别名…", 88);
            var register = TryRegisterAppInstaller();
            if (register.Length > 0) notes.Add(register);
            ResetWingetDiscovery();
            if (IsWingetAvailable() || TryBindWingetFromAppx())
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }
        }

        Report(onProgress, "通过 WinGet 模块修复…", 55);
        var repair = TryRepairWinGetPackageManager();
        if (repair.Length > 0) notes.Add(repair);
        ResetWingetDiscovery();
        if (IsWingetAvailable() || TryBindWingetFromAppx())
        {
            Report(onProgress, "winget 已就绪", 100);
            return FormatWingetReady(notes);
        }

        if (!Optimizer.IsWindowsServer())
        {
            Report(onProgress, "下载并安装 App Installer…", 60);
            var bootstrap = TryBootstrapWingetPackages(preferProvisioned: false, onProgress);
            if (bootstrap.Length > 0) notes.Add(bootstrap);
            ResetWingetDiscovery();
            if (IsWingetAvailable() || TryBindWingetFromAppx())
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }

            var register = TryRegisterAppInstaller();
            if (register.Length > 0) notes.Add(register);
            ResetWingetDiscovery();
            if (IsWingetAvailable() || TryBindWingetFromAppx())
            {
                Report(onProgress, "winget 已就绪", 100);
                return FormatWingetReady(notes);
            }
        }

        var wingetItem = CommonSoftwareCatalog.Find("winget");
        if (wingetItem is not null)
            OpenDownloadPage(wingetItem);

        notes.Add("自动修复未完成。已打开官方下载页。");
        notes.Add("下载目录：" + DownloadDir);
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

    public static string CheckUpdates(IReadOnlyList<CommonSoftwareItem> items)
    {
        if (!IsWingetAvailable())
            return "未检测到 winget，无法批量检查更新。可在必备列表中安装 winget，或逐一点击「一键安装」。";

        var output = RunCaptureWinget("upgrade --include-unknown");
        var upgradable = items.Count(i =>
        {
            if (i.IsWingetBootstrap || string.IsNullOrWhiteSpace(i.WingetId)) return false;
            var status = Query(i);
            return status.Installed &&
                output.IndexOf(i.WingetId, StringComparison.OrdinalIgnoreCase) >= 0;
        });
        return upgradable > 0
            ? $"检测到 {upgradable} 款已安装软件可能有更新（详见 winget upgrade）。"
            : "已检查：当前列表中的已安装软件未发现 winget 可用更新。";
    }

    private static CommonSoftwareStatus QueryWingetStatus()
    {
        ResetWingetDiscovery();
        if (!IsWingetAvailable())
        {
            // 包已装但别名坏了：再尝试直接定位 WindowsApps 内 winget.exe
            var direct = TryGetAppxWingetPath();
            if (!string.IsNullOrWhiteSpace(direct) && ProbeWinget(direct!))
            {
                _wingetPath = direct;
                _wingetDiscoveryDone = true;
                var ver = RunCaptureWinget("--version").Trim();
                if (ver.StartsWith("v", StringComparison.OrdinalIgnoreCase) && ver.Length > 1)
                    ver = ver.Substring(1).Trim();
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

        var version = RunCaptureWinget("--version").Trim();
        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase) && version.Length > 1)
            version = version.Substring(1).Trim();
        return new CommonSoftwareStatus
        {
            Installed = true,
            Version = version.Length > 0 ? "v" + version : "已就绪",
        };
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

    private static string TryRepairWinGetPackageManager()
    {
        const string script = @"
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
if (-not (Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue)) {
  Register-PSRepository -Default -ErrorAction SilentlyContinue
}
Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
if (-not (Get-Module -ListAvailable -Name Microsoft.WinGet.Client)) {
  Install-Module Microsoft.WinGet.Client -Force -AllowClobber -Scope AllUsers
}
Import-Module Microsoft.WinGet.Client -Force
Repair-WinGetPackageManager -AllUsers
";
        try
        {
            var code = RunPowerShell(script);
            return code == 0
                ? "已通过 Microsoft.WinGet.Client 修复 winget。"
                : "Microsoft.WinGet.Client 修复返回码 " + code + "。";
        }
        catch (Exception ex)
        {
            return "Microsoft.WinGet.Client 修复失败：" + ex.Message;
        }
    }

    private static string TryBootstrapWingetPackages(bool preferProvisioned, Action<SoftwareInstallProgress>? onProgress = null)
    {
        Directory.CreateDirectory(DownloadDir);
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        var vcLibs = Path.Combine(DownloadDir, "Microsoft.VCLibs.x64.14.00.Desktop.appx");
        var uiXaml = Path.Combine(DownloadDir, "Microsoft.UI.Xaml.2.8.x64.appx");
        var bundle = Path.Combine(DownloadDir, "Microsoft.DesktopAppInstaller.msixbundle");

        try
        {
            DownloadPackage(
                "https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx",
                vcLibs,
                minBytes: 500_000,
                onProgress,
                percentBase: 20,
                percentSpan: 15);
            DownloadPackage(
                "https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx",
                uiXaml,
                minBytes: 500_000,
                onProgress,
                percentBase: 35,
                percentSpan: 15);

            try
            {
                DownloadPackage(
                    "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle",
                    bundle,
                    minBytes: 1_000_000,
                    onProgress,
                    percentBase: 50,
                    percentSpan: 25);
            }
            catch
            {
                DownloadPackage("https://aka.ms/getwinget", bundle, minBytes: 1_000_000,
                    onProgress, percentBase: 50, percentSpan: 25);
            }

            Report(onProgress, "安装 VCLibs…", 78);
            AddAppxPackage(vcLibs, provisioned: false);
            Report(onProgress, "安装 UI.Xaml…", 84);
            AddAppxPackage(uiXaml, provisioned: false);
            Report(onProgress, "安装 App Installer…", 90);
            AddAppxPackage(bundle, provisioned: preferProvisioned);

            return preferProvisioned
                ? "已按 Server 路径下载并安装 App Installer（含依赖）。"
                : "已下载并安装 App Installer 依赖与主包。";
        }
        catch (Exception ex)
        {
            return "离线包安装失败：" + ex.Message;
        }
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
        if (File.Exists(dest))
        {
            var len = new FileInfo(dest).Length;
            if (len >= minBytes && LooksLikeBinaryPackage(dest))
            {
                Report(onProgress, "已缓存 " + Path.GetFileName(dest), percentBase + percentSpan);
                return;
            }
            try { File.Delete(dest); } catch { /* ignore */ }
        }

        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        var name = Path.GetFileName(dest);
        Report(onProgress, "开始下载 " + name, percentBase);

        using var wc = new WebClient();
        wc.Headers[HttpRequestHeader.UserAgent] = "SrvDesk/1.0";
        var lastPct = -1;
        wc.DownloadProgressChanged += (_, e) =>
        {
            if (e.ProgressPercentage == lastPct) return;
            lastPct = e.ProgressPercentage;
            var mapped = percentBase + (int)(e.ProgressPercentage / 100.0 * percentSpan);
            Report(onProgress, $"下载 {name} {e.ProgressPercentage}%", mapped);
        };
        try
        {
            wc.DownloadFileTaskAsync(new Uri(url), dest).GetAwaiter().GetResult();
        }
        catch
        {
            // 部分环境 TaskAsync 异常时回退同步下载
            wc.DownloadFile(url, dest);
        }

        var size = new FileInfo(dest).Length;
        if (size < minBytes || !LooksLikeBinaryPackage(dest))
        {
            try { File.Delete(dest); } catch { /* ignore */ }
            throw new InvalidOperationException("下载文件无效或过小：" + Path.GetFileName(dest) + "（" + size + " 字节）");
        }

        Report(onProgress, "下载完成 " + name, percentBase + percentSpan);
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

    private static void AddAppxPackage(string path, bool provisioned)
    {
        var escaped = path.Replace("'", "''");
        string script;
        if (provisioned)
        {
            // Server：机器级安装；无 license 时退回当前用户 Add-AppxPackage
            script = $@"
$ErrorActionPreference = 'Stop'
$path = '{escaped}'
try {{
  Add-AppxProvisionedPackage -Online -PackagePath $path -SkipLicense | Out-Null
}} catch {{
  Add-AppxPackage -Path $path -ErrorAction Stop
}}
";
        }
        else
        {
            script = $@"
$ErrorActionPreference = 'Stop'
Add-AppxPackage -Path '{escaped}'
";
        }

        var code = RunPowerShell(script);
        if (code != 0)
            throw new InvalidOperationException("安装包失败，退出码 " + code + "：" + Path.GetFileName(path));
    }

    private static bool Matches(string displayName, string[] patterns)
    {
        foreach (var p in patterns)
        {
            if (displayName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

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

    private static int RunPowerShell(string script)
    {
        var file = Path.Combine(DownloadDir, "winget-install-" + Guid.NewGuid().ToString("N") + ".ps1");
        Directory.CreateDirectory(DownloadDir);
        File.WriteAllText(file, script, Encoding.UTF8);
        try
        {
            return Run("powershell.exe",
                "-NoProfile -File \"" + file + "\"");
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

    private static int RunWinget(string args, Action<SoftwareInstallProgress>? onProgress = null) =>
        RunStreaming(ResolveWingetPath(), args, setWorkingDirForExe: true, onProgress);

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
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (setWorkingDirForExe && !string.Equals(file, "winget.exe", StringComparison.OrdinalIgnoreCase))
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                psi.WorkingDirectory = dir!;
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 " + file);
        var parser = new WingetProgressParser();
        var lastReport = DateTime.MinValue;
        var lastPercent = -1;
        var lastMessage = "";

        void HandleText(string text)
        {
            foreach (var line in SplitProgressLines(text))
            {
                var update = parser.Feed(line);
                if (update is null) continue;
                var now = DateTime.UtcNow;
                if (update.Percent == lastPercent &&
                    update.Message == lastMessage &&
                    (now - lastReport).TotalMilliseconds < 120)
                    continue;
                if (update.Percent < lastPercent && update.Percent < 100)
                    continue; // 进度只前进

                lastPercent = update.Percent;
                lastMessage = update.Message;
                lastReport = now;
                onProgress?.Invoke(update);
            }
        }

        var stdout = System.Threading.Tasks.Task.Run(() => DrainStream(p.StandardOutput, HandleText));
        var stderr = System.Threading.Tasks.Task.Run(() => DrainStream(p.StandardError, HandleText));
        p.WaitForExit(600_000);
        System.Threading.Tasks.Task.WaitAll(new[] { stdout, stderr }, 15_000);
        return p.ExitCode;
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

    private static IEnumerable<string> SplitProgressLines(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch is '\r' or '\n')
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }
            else
            {
                sb.Append(ch);
            }
        }

        if (sb.Length > 0)
            yield return sb.ToString();
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
            // 去掉进度条字符噪音
            line = Regex.Replace(line, @"[█▒░■□▪▫]+", " ").Trim();
            if (line.Length == 0) return null;

            var lower = line.ToLowerInvariant();
            if (lower.Contains("found "))
            {
                _phase = "已找到包";
                _percent = Math.Max(_percent, 8);
            }
            else if (lower.Contains("downloading") || lower.Contains("download"))
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
                     lower.Contains("正在安装") || lower.Contains("extract"))
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
            if (pctMatch.Success && int.TryParse(pctMatch.Groups[1].Value, out var pct) && pct is >= 0 and <= 100)
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
            else if (line.Length is > 0 and < 80 &&
                     !line.StartsWith("─", StringComparison.Ordinal) &&
                     line.IndexOf("http", StringComparison.OrdinalIgnoreCase) < 0)
                msg = _phase + " · " + line;

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

    private static int Run(string file, string args, bool setWorkingDirForExe = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
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
        p.WaitForExit(600_000);
        System.Threading.Tasks.Task.WaitAll(new[] { stdout, stderr }, 10_000);
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
}
