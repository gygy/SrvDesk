using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Win32;

namespace WinOpt;

internal sealed class CommonSoftwareStatus
{
    public bool Installed { get; set; }
    public string Version { get; set; } = "";
    public string? UninstallCommand { get; set; }
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
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
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

    public static string Install(CommonSoftwareItem item)
    {
        ApplyLog.Write("常用软件安装：" + item.Title);
        if (item.IsWingetBootstrap)
            return InstallWinget();

        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            var code = Run(ResolveWingetPath(),
                $"install -e --id {item.WingetId} --accept-package-agreements --accept-source-agreements");
            if (code == 0) return "";
            if (code == -1978335189) // 0x8A150013 already installed
                return "软件已安装或无需重复安装。";
        }

        OpenDownloadPage(item);
        return IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId)
            ? "winget 安装未成功，已在浏览器打开官方下载页，请手动安装。"
            : string.IsNullOrWhiteSpace(item.WingetId)
                ? "该软件暂无 winget 包，已在浏览器打开官方下载页。"
                : "本机未检测到 winget，已在浏览器打开官方下载页，请手动安装。";
    }

    public static string Uninstall(CommonSoftwareItem item)
    {
        ApplyLog.Write("常用软件卸载：" + item.Title);
        if (item.IsWingetBootstrap)
            return "winget（应用安装程序）为系统组件，不建议在此卸载。请在「设置 → 应用」中操作。";

        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            var code = Run(ResolveWingetPath(), $"uninstall -e --id {item.WingetId}");
            if (code == 0) return "";
        }

        var status = Query(item);
        var cmd = status.UninstallCommand;
        if (string.IsNullOrWhiteSpace(cmd))
            return "未找到可用的卸载命令。请在「设置 → 应用」中手动卸载。";

        RunShell(cmd!);
        return "";
    }

    public static string InstallWinget()
    {
        if (IsWingetAvailable())
            return "";

        ApplyLog.Write("安装 winget（App Installer）" + (Optimizer.IsWindowsServer() ? " [Server]" : ""));
        ResetWingetDiscovery();
        var notes = new List<string>();

        // Server：优先离线包 + 机器级 ProvisionedPackage，再尝试修复模块
        if (Optimizer.IsWindowsServer())
        {
            var bootstrap = TryBootstrapWingetPackages(preferProvisioned: true);
            if (bootstrap.Length > 0) notes.Add(bootstrap);
            ResetWingetDiscovery();
            if (IsWingetAvailable()) return FormatWingetReady(notes);

            var register = TryRegisterAppInstaller();
            if (register.Length > 0) notes.Add(register);
            ResetWingetDiscovery();
            if (IsWingetAvailable()) return FormatWingetReady(notes);
        }

        var repair = TryRepairWinGetPackageManager();
        if (repair.Length > 0) notes.Add(repair);
        ResetWingetDiscovery();
        if (IsWingetAvailable()) return FormatWingetReady(notes);

        if (!Optimizer.IsWindowsServer())
        {
            var bootstrap = TryBootstrapWingetPackages(preferProvisioned: false);
            if (bootstrap.Length > 0) notes.Add(bootstrap);
            ResetWingetDiscovery();
            if (IsWingetAvailable()) return FormatWingetReady(notes);

            var register = TryRegisterAppInstaller();
            if (register.Length > 0) notes.Add(register);
            ResetWingetDiscovery();
            if (IsWingetAvailable()) return FormatWingetReady(notes);
        }

        var wingetItem = CommonSoftwareCatalog.Find("winget");
        if (wingetItem is not null)
            OpenDownloadPage(wingetItem);

        notes.Add("自动安装未完成。已打开官方下载页。");
        notes.Add("下载目录：" + DownloadDir);
        notes.Add("Server 也可手动：以管理员 PowerShell 执行 Add-AppxPackage / Add-AppxProvisionedPackage 安装该目录下的 msixbundle。");
        return string.Join("\r\n", notes);
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

        var output = RunCapture(ResolveWingetPath(), "upgrade --include-unknown");
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
            if (IsAppInstallerPackagePresent())
            {
                return new CommonSoftwareStatus
                {
                    Installed = false,
                    Version = "已安装应用安装程序，winget 别名不可用（可点一键安装修复）",
                };
            }
            return new CommonSoftwareStatus();
        }

        var version = RunCapture(ResolveWingetPath(), "--version").Trim();
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

    private static string TryBootstrapWingetPackages(bool preferProvisioned)
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
                minBytes: 500_000);
            DownloadPackage(
                "https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx",
                uiXaml,
                minBytes: 500_000);

            // 优先 GitHub release 直链，再回退 aka.ms（避免短链下到 HTML）
            try
            {
                DownloadPackage(
                    "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle",
                    bundle,
                    minBytes: 1_000_000);
            }
            catch
            {
                DownloadPackage("https://aka.ms/getwinget", bundle, minBytes: 1_000_000);
            }

            AddAppxPackage(vcLibs, provisioned: false);
            AddAppxPackage(uiXaml, provisioned: false);
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

    private static void DownloadPackage(string url, string dest, long minBytes)
    {
        if (File.Exists(dest))
        {
            var len = new FileInfo(dest).Length;
            if (len >= minBytes && LooksLikeBinaryPackage(dest))
                return;
            try { File.Delete(dest); } catch { /* ignore */ }
        }

        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        using var wc = new WebClient();
        wc.Headers[HttpRequestHeader.UserAgent] = "SrvDesk/1.0";
        wc.DownloadFile(url, dest);

        var size = new FileInfo(dest).Length;
        if (size < minBytes || !LooksLikeBinaryPackage(dest))
        {
            try { File.Delete(dest); } catch { /* ignore */ }
            throw new InvalidOperationException("下载文件无效或过小：" + Path.GetFileName(dest) + "（" + size + " 字节）");
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

    private static int Run(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + file);
        p.WaitForExit(600_000);
        return p.ExitCode;
    }

    private static string RunCapture(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + file);
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(120_000);
        return output;
    }
}
